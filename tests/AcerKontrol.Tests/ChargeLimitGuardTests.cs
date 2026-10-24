using AcerKontrol.ChargeLimit;

namespace AcerKontrol.Tests;

public class ChargeLimitGuardTests
{
    /// <summary>Read sonuçları sıradan çekilir: ChargeLimitState ya da Exception.</summary>
    private sealed class FakeDevice(params object[] reads) : IChargeLimitDevice
    {
        private readonly Queue<object> queue = new(reads);
        public List<bool> Writes { get; } = [];

        public void Enqueue(params object[] more) { foreach (var r in more) queue.Enqueue(r); }

        public ChargeLimitState Read() => queue.Dequeue() switch
        {
            Exception e => throw e,
            ChargeLimitState s => s,
            var x => throw new InvalidOperationException($"bad script item {x}"),
        };

        public void Write(bool enabled) => Writes.Add(enabled);
    }

    private int sleeps;
    private ChargeLimitGuard Guard(FakeDevice d) => new(d, _ => sleeps++);
    private static Exception Boom() => new InvalidOperationException("WMI yanıt vermedi");

    [Fact]
    public void Enforce_AlreadyMatches_InSyncNoWrite()
    {
        var d = new FakeDevice(ChargeLimitState.On);
        var r = Guard(d).Enforce(true);
        Assert.Equal(GuardOutcome.InSync, r.Outcome);
        Assert.Empty(d.Writes);
        Assert.False(r.ShouldNotify);
    }

    [Fact]
    public void Enforce_Mismatch_WritesAndVerifies()
    {
        var d = new FakeDevice(ChargeLimitState.Off, ChargeLimitState.On);
        var r = Guard(d).Enforce(true);
        Assert.Equal(GuardOutcome.Reapplied, r.Outcome);
        Assert.Equal(ChargeLimitState.On, r.Actual);
        Assert.Equal([true], d.Writes);
    }

    [Fact]
    public void Enforce_DeviceThrowsEveryTime_ReturnsFailedAfterThreeAttempts()
    {
        var d = new FakeDevice(Boom(), Boom(), Boom());
        var r = Guard(d).Enforce(true);
        Assert.Equal(GuardOutcome.Failed, r.Outcome);
        Assert.True(r.ShouldNotify);
        Assert.NotNull(r.Error);
        Assert.Equal(2, sleeps);
    }

    [Fact]
    public void Enforce_Unsupported_ReturnsFailedWithoutSetting()
    {
        var d = new FakeDevice(ChargeLimitState.Unsupported, ChargeLimitState.Unsupported, ChargeLimitState.Unsupported);
        var r = Guard(d).Enforce(true);
        Assert.Equal(GuardOutcome.Failed, r.Outcome);
        Assert.Equal(ChargeLimitState.Unsupported, r.Actual);
        Assert.Empty(d.Writes);
    }

    [Fact]
    public void Enforce_TransientThenOk_Reapplied()
    {
        var d = new FakeDevice(Boom(), ChargeLimitState.Off, ChargeLimitState.On);
        var r = Guard(d).Enforce(true);
        Assert.Equal(GuardOutcome.Reapplied, r.Outcome);
        Assert.False(r.ShouldNotify);
    }

    [Fact]
    public void Enforce_WriteDoesNotStick_Failed()
    {
        var d = new FakeDevice(ChargeLimitState.Off, ChargeLimitState.Off, ChargeLimitState.Off,
                               ChargeLimitState.Off, ChargeLimitState.Off, ChargeLimitState.Off);
        var r = Guard(d).Enforce(true);
        Assert.Equal(GuardOutcome.Failed, r.Outcome);
        Assert.Equal(ChargeLimitState.Off, r.Actual);
        Assert.Equal(3, d.Writes.Count);
    }

    [Fact]
    public void Enforce_SecondConsecutiveFailure_DoesNotNotifyAgain()
    {
        var d = new FakeDevice(Boom(), Boom(), Boom(), Boom(), Boom(), Boom());
        var g = Guard(d);
        Assert.True(g.Enforce(true).ShouldNotify);
        Assert.False(g.Enforce(true).ShouldNotify);
    }

    [Fact]
    public void Enforce_FailureThenSuccessThenFailure_NotifiesAgain()
    {
        var d = new FakeDevice(Boom(), Boom(), Boom(), ChargeLimitState.On, Boom(), Boom(), Boom());
        var g = Guard(d);
        Assert.True(g.Enforce(true).ShouldNotify);
        Assert.False(g.Enforce(true).ShouldNotify);
        Assert.True(g.Enforce(true).ShouldNotify);
    }

    [Fact]
    public void Enforce_RevertedEveryCycle_ReappliesWithoutNotification()
    {
        var d = new FakeDevice();
        var g = Guard(d);
        for (var i = 0; i < 3; i++)
        {
            d.Enqueue(ChargeLimitState.Off, ChargeLimitState.On);
            var r = g.Enforce(true);
            Assert.Equal(GuardOutcome.Reapplied, r.Outcome);
            Assert.False(r.ShouldNotify);
        }
        Assert.Equal([true, true, true], d.Writes);
    }
}

public class ChargeLimitGuardQuietTests
{
    private sealed class AlwaysThrows : IChargeLimitDevice
    {
        public ChargeLimitState Read() => throw new InvalidOperationException("uyanıyor");
        public void Write(bool enabled) { }
    }

    [Fact]
    public void Enforce_QuietFailure_DoesNotNotify()
        => Assert.False(new ChargeLimitGuard(new AlwaysThrows(), _ => { }).Enforce(true, quiet: true).ShouldNotify);

    [Fact]
    public void Enforce_QuietFailureThenRealFailure_NotifiesOnRealOne()
    {
        var g = new ChargeLimitGuard(new AlwaysThrows(), _ => { });
        g.Enforce(true, quiet: true);
        Assert.True(g.Enforce(true).ShouldNotify);
    }
}
