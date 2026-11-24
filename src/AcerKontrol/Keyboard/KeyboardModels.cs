namespace AcerKontrol.Keyboard;

public enum KeyboardEffect : byte
{
    Static = 0,
    Breathing = 1,
    Neon = 2,
    Wave = 3,
    Shifting = 4,
    Zoom = 5,
}

public enum KeyboardDirection : byte
{
    RightToLeft = 1,
    LeftToRight = 2,
}

public sealed record KeyboardLightingSettings(
    KeyboardEffect Effect = KeyboardEffect.Static,
    int Brightness = 100,
    int Speed = 4,
    KeyboardDirection Direction = KeyboardDirection.LeftToRight,
    string Zone1Hex = "#FF0000",
    string Zone2Hex = "#FF0000",
    string Zone3Hex = "#FF0000",
    string Zone4Hex = "#FF0000",
    string EffectColorHex = "#FF0000")
{
    public string[] ZoneHexes => [Zone1Hex, Zone2Hex, Zone3Hex, Zone4Hex];

    public KeyboardLightingSettings WithZoneHex(int index, string hex) => index switch
    {
        0 => this with { Zone1Hex = hex },
        1 => this with { Zone2Hex = hex },
        2 => this with { Zone3Hex = hex },
        3 => this with { Zone4Hex = hex },
        _ => this,
    };

    public KeyboardLightingSettings WithAllZones(string hex) => this with
    {
        Zone1Hex = hex,
        Zone2Hex = hex,
        Zone3Hex = hex,
        Zone4Hex = hex,
        EffectColorHex = hex,
    };
}

public sealed record KeyboardPreset(string Name, KeyboardLightingSettings Settings);

public static class KeyboardPresets
{
    public static IReadOnlyList<KeyboardPreset> All { get; } =
    [
        new("Nitro Kırmızı", new KeyboardLightingSettings(KeyboardEffect.Static, 100, 4, KeyboardDirection.LeftToRight, "#FF1E1E", "#FF1E1E", "#FF1E1E", "#FF1E1E", "#FF1E1E")),
        new("Siber Mavi", new KeyboardLightingSettings(KeyboardEffect.Static, 100, 4, KeyboardDirection.LeftToRight, "#00D4FF", "#00D4FF", "#00D4FF", "#00D4FF", "#00D4FF")),
        new("Zümrüt", new KeyboardLightingSettings(KeyboardEffect.Static, 100, 4, KeyboardDirection.LeftToRight, "#00E676", "#00E676", "#00E676", "#00E676", "#00E676")),
        new("Mor Neon", new KeyboardLightingSettings(KeyboardEffect.Static, 100, 4, KeyboardDirection.LeftToRight, "#A855F7", "#A855F7", "#A855F7", "#A855F7", "#A855F7")),
        new("Gökkuşağı", new KeyboardLightingSettings(KeyboardEffect.Static, 100, 4, KeyboardDirection.LeftToRight, "#FF2A2A", "#FFB800", "#00E676", "#00B0FF", "#FF2A2A")),
        new("Buz Beyazı", new KeyboardLightingSettings(KeyboardEffect.Static, 100, 4, KeyboardDirection.LeftToRight, "#E2E8F0", "#E2E8F0", "#E2E8F0", "#E2E8F0", "#E2E8F0")),
        new("Dalga", new KeyboardLightingSettings(KeyboardEffect.Wave, 100, 5, KeyboardDirection.LeftToRight, "#FF1E1E", "#FFB800", "#00E676", "#00B0FF", "#00D4FF")),
        new("Kapalı", new KeyboardLightingSettings(KeyboardEffect.Static, 0, 0, KeyboardDirection.LeftToRight, "#000000", "#000000", "#000000", "#000000", "#000000")),
    ];
}
