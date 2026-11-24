using System.Management;
using System.Runtime.InteropServices;
using AcerKontrol.Fan;

namespace AcerKontrol.SystemControl;

public sealed record SystemTelemetry(
    int? CpuTempC,
    int? GpuTempC,
    int? CpuFanRpm,
    int? GpuFanRpm,
    int? CpuUsagePercent,
    double? RamUsedGb,
    double? RamTotalGb,
    int? RamUsagePercent,
    bool GamingWmiAvailable,
    bool LcdOverdriveSupported = true);

public interface ISystemTelemetrySource
{
    SystemTelemetry Read();
}

/// <summary>CPU/GPU sıcaklık &amp; fan RPM (AcerGamingFunction) ile CPU/RAM kullanımını okur. İstisna atmaz.</summary>
public sealed class WmiSystemTelemetrySource : ISystemTelemetrySource
{
    // Acer Nitro 5 AN515-58 GetGamingSysInfo sorgu kodları (wmi.log ile doğrulandı)
    private const uint QueryCpuTemp = 0x0101;
    private const uint QueryCpuFanRpm = 0x0201;
    private const uint QueryGpuTemp = 0x0501;
    private const uint QueryGpuFanRpm = 0x0601;

    private ulong prevIdle, prevKernel, prevUser;
    private bool hasPrevCpu;
    private bool? lcdOverdriveSupported;

    public SystemTelemetry Read()
    {
        int? cpuTemp = null, gpuTemp = null, cpuRpm = null, gpuRpm = null;
        var wmiAvailable = false;

        try
        {
            using var instance = AcerWmiFanController.GetInstance();
            wmiAvailable = true;
            cpuTemp = FanCodec.DecodeTemperatureC(QuerySysInfo(instance, QueryCpuTemp));
            cpuRpm = FanCodec.DecodeRpm(QuerySysInfo(instance, QueryCpuFanRpm));
            gpuTemp = FanCodec.DecodeTemperatureC(QuerySysInfo(instance, QueryGpuTemp));
            gpuRpm = FanCodec.DecodeRpm(QuerySysInfo(instance, QueryGpuFanRpm));
            lcdOverdriveSupported ??= ProbeMiscSupported(instance, MiscSettingCodec.LcdOverdriveIndex);
        }
        catch
        {
            // Yönetici yetkisi yoksa veya WMI yanıt vermediyse diğer telemetri yine de okunur.
        }

        cpuTemp ??= ReadAcpiThermalFallback();

        var cpuUsage = ReadCpuUsagePercent();
        var (ramUsed, ramTotal, ramPct) = ReadMemory();

        return new SystemTelemetry(
            cpuTemp, gpuTemp, cpuRpm, gpuRpm, cpuUsage, ramUsed, ramTotal, ramPct,
            wmiAvailable, lcdOverdriveSupported ?? true);
    }

    private static bool ProbeMiscSupported(ManagementObject instance, byte index)
    {
        try
        {
            using var input = instance.GetMethodParameters("GetGamingMiscSetting");
            input["gmInput"] = (uint)index;
            using var output = instance.InvokeMethod("GetGamingMiscSetting", input, null);
            return MiscSettingCodec.IsSupportedValue(Convert.ToUInt64(output["gmOutput"]));
        }
        catch
        {
            return false;
        }
    }

    private static ulong QuerySysInfo(ManagementObject instance, uint queryId)
    {
        try
        {
            using var input = instance.GetMethodParameters("GetGamingSysInfo");
            input["gmInput"] = queryId;
            using var output = instance.InvokeMethod("GetGamingSysInfo", input, null);
            return Convert.ToUInt64(output["gmOutput"]);
        }
        catch
        {
            return 0xFF;
        }
    }

    private static int? ReadAcpiThermalFallback()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            var first = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
            if (first?["CurrentTemperature"] is null) return null;
            var tenthsKelvin = Convert.ToUInt32(first["CurrentTemperature"]);
            if (tenthsKelvin == 0) return null;
            var c = (int)Math.Round(tenthsKelvin / 10.0 - 273.15);
            return c is > 0 and < 130 ? c : null;
        }
        catch
        {
            return null;
        }
    }

    private int? ReadCpuUsagePercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return null;
        var idleTicks = ToUInt64(idle);
        var kernelTicks = ToUInt64(kernel);
        var userTicks = ToUInt64(user);

        if (!hasPrevCpu)
        {
            prevIdle = idleTicks;
            prevKernel = kernelTicks;
            prevUser = userTicks;
            hasPrevCpu = true;
            return null;
        }

        var dIdle = idleTicks - prevIdle;
        var dKernel = kernelTicks - prevKernel;
        var dUser = userTicks - prevUser;
        prevIdle = idleTicks;
        prevKernel = kernelTicks;
        prevUser = userTicks;

        var total = dKernel + dUser;
        if (total == 0) return 0;
        var busy = total > dIdle ? total - dIdle : 0;
        return (int)Math.Clamp(Math.Round(busy * 100.0 / total), 0, 100);
    }

    private static (double? UsedGb, double? TotalGb, int? Percent) ReadMemory()
    {
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref mem) || mem.ullTotalPhys == 0)
            return (null, null, null);

        const double gb = 1024.0 * 1024.0 * 1024.0;
        var total = Math.Round(mem.ullTotalPhys / gb, 1);
        var used = Math.Round((mem.ullTotalPhys - mem.ullAvailPhys) / gb, 1);
        return (used, total, (int)mem.dwMemoryLoad);
    }

    private static ulong ToUInt64(FILETIME ft) => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
