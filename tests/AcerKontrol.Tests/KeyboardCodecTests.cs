using System.Drawing;
using AcerKontrol.Keyboard;
using Xunit;

namespace AcerKontrol.Tests;

public sealed class KeyboardCodecTests
{
    [Fact]
    public void EncodeBacklightPayload_StaticForcesZeroSpeedAndSetsBytes()
    {
        var payload = KeyboardCodec.EncodeBacklightPayload(
            KeyboardEffect.Static,
            brightness: 80,
            speed: 7,
            KeyboardDirection.LeftToRight,
            Color.FromArgb(0x11, 0x22, 0x33));

        Assert.Equal(16, payload.Length);
        Assert.Equal((byte)KeyboardEffect.Static, payload[0]);
        Assert.Equal(0, payload[1]); // Static hızı her zaman 0
        Assert.Equal(80, payload[2]);
        Assert.Equal(0, payload[3]);
        Assert.Equal((byte)KeyboardDirection.LeftToRight, payload[4]);
        Assert.Equal(0x11, payload[5]);
        Assert.Equal(0x22, payload[6]);
        Assert.Equal(0x33, payload[7]);
        Assert.Equal(1, payload[8]);
    }

    [Fact]
    public void EncodeBacklightPayload_WaveSetsWaveSubModeAndClampsValues()
    {
        var payload = KeyboardCodec.EncodeBacklightPayload(
            KeyboardEffect.Wave,
            brightness: 150,
            speed: 99,
            KeyboardDirection.RightToLeft,
            Color.Red);

        Assert.Equal((byte)KeyboardEffect.Wave, payload[0]);
        Assert.Equal(9, payload[1]);
        Assert.Equal(100, payload[2]);
        Assert.Equal(8, payload[3]);
        Assert.Equal((byte)KeyboardDirection.RightToLeft, payload[4]);
    }

    [Theory]
    [InlineData(0, 0x01UL)]
    [InlineData(1, 0x02UL)]
    [InlineData(2, 0x04UL)]
    [InlineData(3, 0x08UL)]
    public void EncodeZoneColor_PacksZoneMaskAndRgb(int zoneIndex, ulong expectedMask)
    {
        var packed = KeyboardCodec.EncodeZoneColor(zoneIndex, Color.FromArgb(0xAA, 0xBB, 0xCC));
        Assert.Equal(0xCCBB_AA00UL | expectedMask, packed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void EncodeZoneColor_OutOfRangeThrows(int zoneIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KeyboardCodec.EncodeZoneColor(zoneIndex, Color.White));
    }

    [Fact]
    public void HexColor_RoundTripsAndFallsBackOnInvalid()
    {
        var c = KeyboardCodec.ParseHexColor("#00D4FF", Color.Red);
        Assert.Equal(Color.FromArgb(0x00, 0xD4, 0xFF), c);
        Assert.Equal("#00D4FF", KeyboardCodec.ToHex(c));

        Assert.Equal(Color.Red, KeyboardCodec.ParseHexColor("gecersiz", Color.Red));
        Assert.Equal(Color.Red, KeyboardCodec.ParseHexColor(null, Color.Red));
    }

    [Fact]
    public void Presets_ContainExpectedEntries()
    {
        Assert.True(KeyboardPresets.All.Count >= 6);
        Assert.Contains(KeyboardPresets.All, p => p.Settings.Brightness == 0);
        Assert.Contains(KeyboardPresets.All, p => p.Settings.Effect == KeyboardEffect.Wave);
    }
}
