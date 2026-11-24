using System.Management;
using AcerKontrol.Diagnostics;

namespace AcerKontrol.Fan;

/// <summary>root\wmi AcerGamingFunction üzerinden CPU/GPU fan modu ve hızı kontrolü.</summary>
public sealed class AcerWmiFanController : IFanController
{
    public void SetMode(FanMode mode)
    {
        InvokeUInt64("SetGamingFanBehavior", FanCodec.EncodeBehavior(mode));
    }

    public void SetCustomSpeeds(int cpuPercent, int gpuPercent)
    {
        InvokeUInt64("SetGamingFanBehavior", FanCodec.EncodeBehavior(FanMode.Custom));
        InvokeUInt64("SetGamingFanSpeed", FanCodec.EncodeSpeed(FanTarget.Cpu, cpuPercent));
        InvokeUInt64("SetGamingFanSpeed", FanCodec.EncodeSpeed(FanTarget.Gpu, gpuPercent));
    }

    private static void InvokeUInt64(string methodName, ulong gmInput)
    {
        var inputDesc = $"0x{gmInput:X16}";
        try
        {
            using var instance = GetInstance();
            using var input = instance.GetMethodParameters(methodName);
            input["gmInput"] = gmInput;
            using var output = instance.InvokeMethod(methodName, input, null);
            var ret = Convert.ToUInt32(output["gmOutput"]);
            DiagLog.WmiCall(methodName, inputDesc, ret);
            if ((ret & 0xFF) != 0)
                throw new InvalidOperationException($"{methodName} başarısız (kod={ret & 0xFF})");
        }
        catch (Exception ex)
        {
            DiagLog.WmiCall(methodName, inputDesc, null, ex);
            throw;
        }
    }

    internal static ManagementObject GetInstance()
    {
        using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM AcerGamingFunction");
        return searcher.Get().Cast<ManagementObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("AcerGamingFunction WMI örneği bulunamadı");
    }
}
