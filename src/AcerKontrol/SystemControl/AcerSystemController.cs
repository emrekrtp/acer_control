using System.Diagnostics;
using System.Management;
using AcerKontrol.Diagnostics;
using AcerKontrol.Fan;

namespace AcerKontrol.SystemControl;

/// <summary>Performans profili, Acer donanım anahtarları, Windows güç planı ve ekran parlaklığı kontrolü.</summary>
public sealed class AcerSystemController : ISystemController
{
    public void SetPerformanceProfile(PerformanceProfile profile)
        => InvokeMiscSetting(MiscSettingCodec.EncodeProfile(profile));

    public void SetCoolBoost(bool enabled)
        => InvokeMiscSetting(MiscSettingCodec.EncodeToggle(MiscSettingCodec.CoolBoostIndex, enabled));

    public void SetKeyboardTimeout(bool enabled)
        => InvokeMiscSetting(MiscSettingCodec.EncodeToggle(MiscSettingCodec.KeyboardTimeoutIndex, enabled));

    public void SetLcdOverdrive(bool enabled)
        => InvokeMiscSetting(MiscSettingCodec.EncodeToggle(MiscSettingCodec.LcdOverdriveIndex, enabled));

    public void SetUsbCharging(bool enabled)
        => InvokeMiscSetting(MiscSettingCodec.EncodeToggle(MiscSettingCodec.UsbChargingIndex, enabled));

    public void SetWindowsPowerPlan(WindowsPowerPlan plan)
    {
        var guid = MiscSettingCodec.WindowsPowerPlanGuid(plan);
        DiagLog.Info("SYS ", $"SetWindowsPowerPlan({plan} / {guid})");
        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = $"/setactive {guid}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("powercfg başlatılamadı");

        if (!proc.WaitForExit(5000))
        {
            try { proc.Kill(); } catch { }
            throw new TimeoutException("powercfg zaman aşımına uğradı");
        }
        DiagLog.Info("SYS ", $"powercfg çıkış kodu: {proc.ExitCode}");
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"Güç planı değiştirilemedi (kod {proc.ExitCode})");
    }

    public int? GetDisplayBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
            var first = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
            return first?["CurrentBrightness"] is null ? null : Convert.ToInt32(first["CurrentBrightness"]);
        }
        catch
        {
            return null;
        }
    }

    public void SetDisplayBrightness(int percent)
    {
        var clamped = (byte)Math.Clamp(percent, 0, 100);
        using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
        var instance = searcher.Get().Cast<ManagementObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("Ekran parlaklığı arayüzü bulunamadı");
        using (instance)
        {
            instance.InvokeMethod("WmiSetBrightness", [1u, clamped]);
        }
    }

    private static void InvokeMiscSetting(ulong gmInput)
    {
        var inputDesc = $"0x{gmInput:X16}";
        try
        {
            using var instance = AcerWmiFanController.GetInstance();
            using var input = instance.GetMethodParameters("SetGamingMiscSetting");
            input["gmInput"] = gmInput;
            using var output = instance.InvokeMethod("SetGamingMiscSetting", input, null);
            var ret = Convert.ToUInt32(output["gmOutput"]);
            DiagLog.WmiCall("SetGamingMiscSetting", inputDesc, ret);
            if (!MiscSettingCodec.IsSuccess(ret))
                throw new InvalidOperationException($"SetGamingMiscSetting başarısız (kod={ret & 0xFF})");
        }
        catch (Exception ex)
        {
            DiagLog.WmiCall("SetGamingMiscSetting", inputDesc, null, ex);
            throw;
        }
    }
}
