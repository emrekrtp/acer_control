using System.Globalization;
using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Fan;
using AcerKontrol.Keyboard;
using AcerKontrol.SystemControl;

namespace AcerKontrol.Tray;

/// <summary>Tepsi ipucu ve kontrol paneli için yerel dile duyarlı değer biçimlendirme.</summary>
public static class StatusText
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CultureInfo En = CultureInfo.InvariantCulture;

    private static CultureInfo Culture => Strings.IsEnglish ? En : Tr;

    public static string Tooltip(int? percent, ChargeLimitState? limit) => Strings.IsEnglish
        ? $"{(percent is int p ? $"{p}%" : Strings.Unknown)} · Limit: {Limit(limit)}"
        : $"%{percent?.ToString() ?? Strings.Unknown} · Sınır: {Limit(limit)}";

    public static string Limit(ChargeLimitState? limit) => limit switch
    {
        ChargeLimitState.On => Strings.LimitOn,
        ChargeLimitState.Off => Strings.LimitOff,
        _ => Strings.LimitUnknown,
    };

    public static string Percent(int? value) => value is int v
        ? (Strings.IsEnglish ? $"{v}%" : $"%{v}")
        : Strings.Unknown;

    public static string Power(BatterySnapshot s) => (s.IsCharging, s.OnAcPower) switch
    {
        (true, _) => Strings.Charging,
        (false, true) => Strings.PluggedNotCharging,
        (false, false) => Strings.OnBattery,
        (false, null) => Strings.NotCharging,
        _ => Strings.Unknown,
    };

    public static string Temperature(double? celsius)
        => celsius is double c ? c.ToString("0.0", Culture) + " °C" : Strings.Unknown;

    public static string WholeTemperature(int? celsius)
        => celsius is int c ? $"{c} °C" : Strings.Unknown;

    public static string Rpm(int? rpm)
        => rpm is int r ? $"{r.ToString("N0", Culture)} RPM" : Strings.Unknown;

    public static string Ram(double? usedGb, double? totalGb, int? percent)
        => usedGb is double u && totalGb is double t
            ? $"{u.ToString("0.0", Culture)} / {t.ToString("0.0", Culture)} GB ({Percent(percent)})"
            : Strings.Unknown;

    public static string CapacityMwh(uint? mwh)
        => mwh is uint v ? $"{v.ToString("N0", Culture)} mWh" : Strings.Unknown;

    public static string Count(uint? value) => value?.ToString(Culture) ?? Strings.Unknown;

    public static string FanModeName(FanMode mode) => mode switch
    {
        FanMode.Auto => Strings.FanAuto,
        FanMode.Max => Strings.FanMax,
        FanMode.Custom => Strings.FanCustom,
        _ => Strings.Unknown,
    };

    public static string ProfileName(PerformanceProfile profile) => profile switch
    {
        PerformanceProfile.Quiet => Strings.ProfileQuiet,
        PerformanceProfile.Balanced => Strings.ProfileBalanced,
        PerformanceProfile.Performance => Strings.ProfilePerformance,
        PerformanceProfile.Turbo => Strings.ProfileTurbo,
        _ => Strings.Unknown,
    };

    public static string EffectName(KeyboardEffect effect) => effect switch
    {
        KeyboardEffect.Static => Strings.EffectStatic,
        KeyboardEffect.Breathing => Strings.EffectBreathing,
        KeyboardEffect.Neon => Strings.EffectNeon,
        KeyboardEffect.Wave => Strings.EffectWave,
        KeyboardEffect.Shifting => Strings.EffectShifting,
        KeyboardEffect.Zoom => Strings.EffectZoom,
        _ => Strings.Unknown,
    };
}
