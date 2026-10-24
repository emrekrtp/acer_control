using AcerKontrol.ChargeLimit;

namespace AcerKontrol.Tray;

public sealed record TrayIconStyle(string Text, Color Foreground)
{
    public static TrayIconStyle For(int? percent, ChargeLimitState? limit) => new(
        percent?.ToString() ?? "?",
        limit switch
        {
            ChargeLimitState.On => Color.LimeGreen,
            ChargeLimitState.Off => Color.Gray,
            _ => Color.OrangeRed,
        });
}
