using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;

namespace AcerKontrol.Tray;

/// <summary>Bir bekçi döngüsü: sınırı zorlar ve pil durumunu okur. Hiçbir zaman istisna atmaz.</summary>
public static class TrayCycle
{
    public static (GuardResult Result, BatterySnapshot? Snapshot) Run(
        ChargeLimitGuard guard, IBatteryInfoSource battery, bool desired, bool quiet)
    {
        var result = guard.Enforce(desired, quiet);
        BatterySnapshot? snapshot;
        try
        {
            snapshot = battery.Read();
        }
        catch (Exception)
        {
            snapshot = null;
        }
        return (result, snapshot);
    }
}
