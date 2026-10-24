namespace AcerKontrol.ChargeLimit;

public enum GuardOutcome { InSync, Reapplied, Failed }

public sealed record GuardResult(GuardOutcome Outcome, ChargeLimitState? Actual, bool ShouldNotify, string? Error);

/// <summary>
/// İstenen sınır durumunu cihazda zorlar. Her denemede okur, farklıysa yazar ve doğrular;
/// başarısız seri başına yalnızca bir kez bildirim ister. <c>quiet</c> denemeleri (ör. uykudan uyanış,
/// arayüz henüz hazır değilken) hiç bildirim istemez ve seriyi başlatmaz.
/// </summary>
public sealed class ChargeLimitGuard(IChargeLimitDevice device, Action<TimeSpan> sleep, int attempts = 3, TimeSpan? delay = null)
{
    private readonly TimeSpan delay = delay ?? TimeSpan.FromSeconds(2);
    private bool inFailureStreak;

    public GuardResult Enforce(bool desired, bool quiet = false)
    {
        var want = desired ? ChargeLimitState.On : ChargeLimitState.Off;
        ChargeLimitState? actual = null;
        string? error = null;

        for (var i = 0; i < attempts; i++)
        {
            if (i > 0) sleep(this.delay);
            try
            {
                actual = device.Read();
                if (actual == want) return Succeed(GuardOutcome.InSync, actual);
                if (actual == ChargeLimitState.Unsupported) { error = "Unsupported"; continue; }

                device.Write(desired);
                actual = device.Read();
                if (actual == want) return Succeed(GuardOutcome.Reapplied, actual);
                error = $"Yazıldı ama durum {actual}";
            }
            catch (Exception e)
            {
                actual = null;
                error = e.Message;
            }
        }

        if (quiet) return new GuardResult(GuardOutcome.Failed, actual, ShouldNotify: false, error);

        var notify = !inFailureStreak;
        inFailureStreak = true;
        return new GuardResult(GuardOutcome.Failed, actual, notify, error);
    }

    private GuardResult Succeed(GuardOutcome outcome, ChargeLimitState? actual)
    {
        inFailureStreak = false;
        return new GuardResult(outcome, actual, ShouldNotify: false, Error: null);
    }
}
