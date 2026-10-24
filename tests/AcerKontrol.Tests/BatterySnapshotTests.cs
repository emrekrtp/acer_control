using AcerKontrol.Battery;

namespace AcerKontrol.Tests;

public class BatterySnapshotTests
{
    private static BatterySnapshot Snap(uint? full, uint? design)
        => new(ChargePercent: 80, IsCharging: false, OnAcPower: true,
               FullChargedCapacity: full, DesignedCapacity: design, CycleCount: 100, TemperatureC: 30);

    [Fact] public void HealthPercent_Computes() => Assert.Equal(88, Snap(full: 44000, design: 50000).HealthPercent);
    [Fact] public void HealthPercent_DesignCapacityZero_ReturnsNull() => Assert.Null(Snap(full: 44000, design: 0).HealthPercent);
    [Fact] public void HealthPercent_MissingData_ReturnsNull() => Assert.Null(Snap(full: null, design: 50000).HealthPercent);
}

public class WmiBatteryInfoSourceTests
{
    [Fact] public void ZeroAsUnknown_Zero_ReturnsNull() => Assert.Null(WmiBatteryInfoSource.ZeroAsUnknown(0));
    [Fact] public void ZeroAsUnknown_Value_Kept() => Assert.Equal(12u, WmiBatteryInfoSource.ZeroAsUnknown(12));
}
