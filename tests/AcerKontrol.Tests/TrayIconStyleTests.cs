using System.Drawing;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Tray;

namespace AcerKontrol.Tests;

public class TrayIconStyleTests
{
    [Fact]
    public void For_LimitOn_Green()
    {
        var s = TrayIconStyle.For(76, ChargeLimitState.On);
        Assert.Equal("76", s.Text);
        Assert.Equal(Color.LimeGreen, s.Foreground);
    }

    [Fact] public void For_LimitOff_Gray() => Assert.Equal(Color.Gray, TrayIconStyle.For(50, ChargeLimitState.Off).Foreground);
    [Fact] public void For_Unsupported_OrangeRed() => Assert.Equal(Color.OrangeRed, TrayIconStyle.For(50, ChargeLimitState.Unsupported).Foreground);
    [Fact] public void For_Hundred_KeepsThreeDigits() => Assert.Equal("100", TrayIconStyle.For(100, ChargeLimitState.On).Text);

    [Fact]
    public void For_Unknown_OrangeQuestion()
    {
        var s = TrayIconStyle.For(null, null);
        Assert.Equal("?", s.Text);
        Assert.Equal(Color.OrangeRed, s.Foreground);
    }

    [Fact]
    public void Render_ReturnsIconOfRequestedSize()
    {
        using var i = TrayIconRenderer.Render(TrayIconStyle.For(100, ChargeLimitState.On));
        Assert.Equal(32, i.Width);
    }
}
