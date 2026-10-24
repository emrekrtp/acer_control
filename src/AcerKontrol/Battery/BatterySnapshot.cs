namespace AcerKontrol.Battery;

public sealed record BatterySnapshot(int? ChargePercent, bool? IsCharging, bool? OnAcPower,
    uint? FullChargedCapacity, uint? DesignedCapacity, uint? CycleCount, double? TemperatureC)
{
    public int? HealthPercent =>
        FullChargedCapacity is uint full && DesignedCapacity is uint design && design != 0
            ? (int)Math.Round(100.0 * full / design)
            : null;
}
