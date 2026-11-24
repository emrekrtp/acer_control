using System.Text.Json.Serialization;
using AcerKontrol.Fan;
using AcerKontrol.Keyboard;
using AcerKontrol.SystemControl;

namespace AcerKontrol.Settings;

public sealed record AppSettings(
    bool ChargeLimitEnabled = true,
    FanMode FanMode = FanMode.Auto,
    int CpuFanPercent = 50,
    int GpuFanPercent = 50,
    bool CoolBoostEnabled = false,
    PerformanceProfile PerformanceProfile = PerformanceProfile.Balanced,
    KeyboardLightingSettings? Keyboard = null,
    bool LcdOverdriveEnabled = false,
    bool UsbChargingEnabled = false,
    bool ReapplyOnWake = true,
    bool KeyboardTimeoutEnabled = true)
{
    [JsonIgnore]
    public KeyboardLightingSettings EffectiveKeyboard => Keyboard ?? new KeyboardLightingSettings();
}
