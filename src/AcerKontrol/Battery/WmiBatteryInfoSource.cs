using System.Management;

namespace AcerKontrol.Battery;

/// <summary>Pil bilgisini WMI'dan okur; hiçbir zaman istisna atmaz, okunamayan alanlar null kalır.</summary>
public sealed class WmiBatteryInfoSource : IBatteryInfoSource
{
    // Tasarım kapasitesi değişmez; yedek kaynak (powercfg) yavaş olduğundan bir kez okunur.
    private readonly Lazy<uint?> designCapacityFallback = new(PowercfgReport.ReadDesignCapacity);

    public BatterySnapshot Read()
    {
        var status = First(@"root\wmi", "BatteryStatus");
        var tempTenthsKelvin = ToUInt(First(@"root\wmi", "BatteryTemperature")?["Temperature"]);

        return new BatterySnapshot(
            ChargePercent: (int?)ToUInt(First(@"root\cimv2", "Win32_Battery")?["EstimatedChargeRemaining"]),
            IsCharging: status?["Charging"] as bool?,
            OnAcPower: status?["PowerOnline"] as bool?,
            FullChargedCapacity: ToUInt(First(@"root\wmi", "BatteryFullChargedCapacity")?["FullChargedCapacity"]),
            DesignedCapacity: ZeroAsUnknown(ToUInt(First(@"root\wmi", "BatteryStaticData")?["DesignedCapacity"]))
                ?? designCapacityFallback.Value,
            CycleCount: ZeroAsUnknown(ToUInt(First(@"root\wmi", "BatteryCycleCount")?["CycleCount"])),
            TemperatureC: tempTenthsKelvin is uint t and > 0 ? Math.Round(t / 10.0 - 273.15, 1) : null);
    }

    private static ManagementBaseObject? First(string scope, string className)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, $"SELECT * FROM {className}");
            return searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
        }
        catch (Exception e) when (e is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    /// <summary>Bu firmware bilinmeyen değerler için 0 döndürüyor.</summary>
    public static uint? ZeroAsUnknown(uint? value) => value == 0 ? null : value;

    private static uint? ToUInt(object? value) => value is null ? null : Convert.ToUInt32(value);
}
