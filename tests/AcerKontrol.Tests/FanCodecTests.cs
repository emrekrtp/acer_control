using AcerKontrol.Fan;
using Xunit;

namespace AcerKontrol.Tests;

public sealed class FanCodecTests
{
    [Fact]
    public void EncodeBehavior_SetsExpectedMasksAndModes()
    {
        Assert.Equal(0x0041_0009UL, FanCodec.EncodeBehavior(FanMode.Auto));
        Assert.Equal(0x0082_0009UL, FanCodec.EncodeBehavior(FanMode.Max));
        Assert.Equal(0x00C3_0009UL, FanCodec.EncodeBehavior(FanMode.Custom));
    }

    [Fact]
    public void EncodeBehavior_IndependentCpuAndGpuModes()
    {
        // CPU=Max (2 << 16), GPU=Auto (1 << 22), Mask=0x0009
        Assert.Equal(0x0042_0009UL, FanCodec.EncodeBehavior(FanMode.Max, FanMode.Auto));
    }

    [Theory]
    [InlineData(FanTarget.Cpu, 0, 0x0001UL)]
    [InlineData(FanTarget.Cpu, 50, 0x3201UL)]
    [InlineData(FanTarget.Cpu, 100, 0x6401UL)]
    [InlineData(FanTarget.Gpu, 75, 0x4B04UL)]
    [InlineData(FanTarget.Cpu, -10, 0x0001UL)]
    [InlineData(FanTarget.Gpu, 150, 0x6404UL)]
    public void EncodeSpeed_ClampsAndPacksTarget(FanTarget target, int percent, ulong expected)
    {
        Assert.Equal(expected, FanCodec.EncodeSpeed(target, percent));
    }

    [Fact]
    public void DecodeRpm_ValidAndInvalidPayloads()
    {
        // status=0x00, rpm=3200 (0x0C80) -> (0x0C80 << 8) | 0x00
        Assert.Equal(3200, FanCodec.DecodeRpm(0x000C_8000UL));
        Assert.Null(FanCodec.DecodeRpm(0x000C_8001UL)); // hata baytı != 0
        Assert.Null(FanCodec.DecodeRpm(0x0000_0000UL)); // 0 RPM
        Assert.Null(FanCodec.DecodeRpm(0x00FF_FF00UL)); // aralık dışı
    }

    [Fact]
    public void DecodeTemperatureC_ValidAndInvalidPayloads()
    {
        // status=0x00, temp=68°C (0x44)
        Assert.Equal(68, FanCodec.DecodeTemperatureC(0x0000_4400UL));
        Assert.Null(FanCodec.DecodeTemperatureC(0x0000_4401UL)); // hata baytı != 0
        Assert.Null(FanCodec.DecodeTemperatureC(0x0000_0000UL)); // 0°C
        Assert.Null(FanCodec.DecodeTemperatureC(0x0000_F000UL)); // 240°C geçersiz
    }
}
