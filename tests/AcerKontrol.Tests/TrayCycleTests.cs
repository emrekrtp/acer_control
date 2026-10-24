using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Tray;

namespace AcerKontrol.Tests;

public class TrayCycleTests
{
    private sealed class OnDevice : IChargeLimitDevice
    {
        public ChargeLimitState Read() => ChargeLimitState.On;
        public void Write(bool enabled) { }
    }

    private sealed class ThrowingBattery : IBatteryInfoSource
    {
        public BatterySnapshot Read() => throw new OverflowException("bozuk değer");
    }

    [Fact]
    public void Run_BatteryThrows_ReturnsNullSnapshotAndGuardResult()
    {
        var (result, snapshot) = TrayCycle.Run(new ChargeLimitGuard(new OnDevice(), _ => { }), new ThrowingBattery(), desired: true, quiet: false);
        Assert.Null(snapshot);
        Assert.Equal(GuardOutcome.InSync, result.Outcome);
    }
}

public class OnceOnSuccessTests
{
    [Fact]
    public void Get_Throws_DoesNotThrowAndRetriesNextTime()
    {
        var calls = 0;
        var once = new OnceOnSuccess<uint?>(() => { calls++; throw new UnauthorizedAccessException("kilitli"); });
        Assert.Null(once.Get());
        Assert.Null(once.Get());
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Get_Succeeds_CachesEvenNull()
    {
        var calls = 0;
        var once = new OnceOnSuccess<uint?>(() => { calls++; return null; });
        once.Get();
        once.Get();
        Assert.Equal(1, calls);
    }
}
