using System.Globalization;
using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;

namespace AcerKontrol.Tray;

/// <summary>Tepsi ipucu ve durum penceresi için değer biçimlendirme.</summary>
public static class StatusText
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Tooltip(int? percent, ChargeLimitState? limit)
        => $"%{percent?.ToString() ?? Strings.Unknown} · Sınır: {Limit(limit)}";

    public static string Limit(ChargeLimitState? limit) => limit switch
    {
        ChargeLimitState.On => Strings.LimitOn,
        ChargeLimitState.Off => Strings.LimitOff,
        _ => Strings.LimitUnknown,
    };

    public static string Percent(int? value) => value is int v ? $"%{v}" : Strings.Unknown;

    public static string Power(BatterySnapshot s) => (s.IsCharging, s.OnAcPower) switch
    {
        (true, _) => Strings.Charging,
        (false, true) => Strings.PluggedNotCharging,
        (false, false) => Strings.OnBattery,
        (false, null) => Strings.NotCharging,
        _ => Strings.Unknown,
    };

    public static string Temperature(double? celsius)
        => celsius is double c ? c.ToString("0.0", Tr) + " °C" : Strings.Unknown;

    public static string Count(uint? value) => value?.ToString(Tr) ?? Strings.Unknown;
}
