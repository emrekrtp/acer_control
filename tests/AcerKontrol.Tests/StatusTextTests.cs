using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Tray;

namespace AcerKontrol.Tests;

public class StatusTextTests
{
    private static BatterySnapshot Snap(bool? charging = false, bool? ac = true, double? temp = null)
        => new(76, charging, ac, 46108, 57488, null, temp);

    [Fact] public void Tooltip_Known() => Assert.Equal("%76 · Sınır: Açık", StatusText.Tooltip(76, ChargeLimitState.On));
    [Fact] public void Tooltip_Unknown() => Assert.Equal("%— · Sınır: Bilinmiyor", StatusText.Tooltip(null, null));
    [Fact] public void Limit_Unsupported_Unknown() => Assert.Equal(Strings.LimitUnknown, StatusText.Limit(ChargeLimitState.Unsupported));
    [Fact] public void Percent_Null_Dash() => Assert.Equal("—", StatusText.Percent(null));
    [Fact] public void Percent_Value() => Assert.Equal("%80", StatusText.Percent(80));
    [Fact] public void Power_Charging() => Assert.Equal(Strings.Charging, StatusText.Power(Snap(charging: true)));
    [Fact] public void Power_PluggedNotCharging() => Assert.Equal(Strings.PluggedNotCharging, StatusText.Power(Snap(charging: false, ac: true)));
    [Fact] public void Power_OnBattery() => Assert.Equal(Strings.OnBattery, StatusText.Power(Snap(charging: false, ac: false)));
    [Fact] public void Power_Unknown() => Assert.Equal("—", StatusText.Power(Snap(charging: null, ac: null)));
    [Fact] public void Temperature_Formats() => Assert.Equal("31,5 °C", StatusText.Temperature(31.5));
    [Fact] public void Temperature_Null_Dash() => Assert.Equal("—", StatusText.Temperature(null));
    [Fact] public void Count_Null_Dash() => Assert.Equal("—", StatusText.Count(null));
}
