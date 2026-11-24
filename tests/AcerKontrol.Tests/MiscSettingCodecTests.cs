using AcerKontrol.Fan;
using AcerKontrol.Keyboard;
using AcerKontrol.Settings;
using AcerKontrol.SystemControl;
using Xunit;

namespace AcerKontrol.Tests;

public sealed class MiscSettingCodecTests
{
    [Fact]
    public void EncodeProfile_PacksIndex0x0B()
    {
        Assert.Equal(0x000BUL, MiscSettingCodec.EncodeProfile(PerformanceProfile.Quiet));
        Assert.Equal(0x010BUL, MiscSettingCodec.EncodeProfile(PerformanceProfile.Balanced));
        Assert.Equal(0x040BUL, MiscSettingCodec.EncodeProfile(PerformanceProfile.Performance));
        Assert.Equal(0x050BUL, MiscSettingCodec.EncodeProfile(PerformanceProfile.Turbo));
    }

    [Fact]
    public void EncodeToggle_PacksBooleanValueAndIndex()
    {
        Assert.Equal(0x0101UL, MiscSettingCodec.EncodeToggle(MiscSettingCodec.CoolBoostIndex, true));
        Assert.Equal(0x0001UL, MiscSettingCodec.EncodeToggle(MiscSettingCodec.CoolBoostIndex, false));
        Assert.Equal(0x0102UL, MiscSettingCodec.EncodeToggle(MiscSettingCodec.KeyboardTimeoutIndex, true));
        Assert.Equal(0x0104UL, MiscSettingCodec.EncodeToggle(MiscSettingCodec.LcdOverdriveIndex, true));
        Assert.Equal(0x0106UL, MiscSettingCodec.EncodeToggle(MiscSettingCodec.UsbChargingIndex, true));
    }

    [Theory]
    [InlineData(0x0000_0000u, true)]
    [InlineData(0x0000_0100u, true)]
    [InlineData(0x0000_0400u, true)]
    [InlineData(0x0000_0500u, true)]
    [InlineData(0x0000_0003u, false)]
    [InlineData(0x0000_0102u, false)]
    public void IsSuccess_ChecksLowByteOnly(uint gmOutput, bool expected)
    {
        Assert.Equal(expected, MiscSettingCodec.IsSuccess(gmOutput));
    }

    [Fact]
    public void IsSupportedValue_DetectsUnsupported0xFF()
    {
        Assert.True(MiscSettingCodec.IsSupportedValue(0x0000_0000_0000_0100UL));
        Assert.True(MiscSettingCodec.IsSupportedValue(0x0000_0000_0000_0000UL));
        Assert.False(MiscSettingCodec.IsSupportedValue(0x0000_0000_0000_FF00UL));
        Assert.False(MiscSettingCodec.IsSupportedValue(0x0000_0000_0000_FF03UL));
    }

    [Fact]
    public void SettingsStore_LoadsLegacyJsonAndRoundTripsExtendedSettings()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AcerKontrol_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "settings.json");

            // Eski sürüm settings.json geriye uyumluluğu
            File.WriteAllText(path, """{"ChargeLimitEnabled":false}""");
            var store = new SettingsStore(path);
            var loaded = store.Load();
            Assert.False(loaded.ChargeLimitEnabled);
            Assert.Equal(FanMode.Auto, loaded.FanMode);
            Assert.Equal(PerformanceProfile.Balanced, loaded.PerformanceProfile);
            Assert.Equal(100, loaded.EffectiveKeyboard.Brightness);

            // Genişletilmiş ayarların tam kayıt ve geri okuma testi
            var extended = loaded with
            {
                FanMode = FanMode.Custom,
                CpuFanPercent = 70,
                GpuFanPercent = 85,
                CoolBoostEnabled = true,
                PerformanceProfile = PerformanceProfile.Turbo,
                Keyboard = new KeyboardLightingSettings(KeyboardEffect.Wave, 80, 6, KeyboardDirection.RightToLeft, "#112233", "#445566", "#778899", "#AABBCC", "#00D4FF"),
                LcdOverdriveEnabled = true,
                UsbChargingEnabled = true,
            };
            store.Save(extended);
            var roundTripped = store.Load();
            Assert.Equal(extended, roundTripped);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
