using System.Globalization;

namespace AcerKontrol.Keyboard;

/// <summary>Acer 4 bölgeli RGB klavye WMI payload paketleyicisi.</summary>
public static class KeyboardCodec
{
    public const int BacklightPayloadLength = 16;

    /// <summary>
    /// SetGamingKBBacklight için 16 baytlık payload oluşturur:
    /// [0]=Efekt, [1]=Hız(0..9), [2]=Parlaklık(0..100), [3]=DalgaModu(0/8),
    /// [4]=Yön(1/2), [5]=R, [6]=G, [7]=B, [8]=1(uygula), [9..15]=0.
    /// </summary>
    public static byte[] EncodeBacklightPayload(
        KeyboardEffect effect,
        int brightness,
        int speed,
        KeyboardDirection direction,
        Color color)
    {
        var payload = new byte[BacklightPayloadLength];
        payload[0] = (byte)effect;
        payload[1] = effect == KeyboardEffect.Static ? (byte)0 : (byte)Math.Clamp(speed, 1, 9);
        payload[2] = (byte)Math.Clamp(brightness, 0, 100);
        payload[3] = effect == KeyboardEffect.Wave ? (byte)8 : (byte)0;
        payload[4] = (byte)direction;
        payload[5] = color.R;
        payload[6] = color.G;
        payload[7] = color.B;
        payload[8] = 1;
        return payload;
    }

    /// <summary>
    /// SetGamingRgbKb için 4 bölgeli renk paketleyici:
    /// (B &lt;&lt; 24) | (G &lt;&lt; 16) | (R &lt;&lt; 8) | zoneMask (bölge 0..3 -&gt; 1 &lt;&lt; zoneIndex).
    /// </summary>
    public static ulong EncodeZoneColor(int zoneIndex, Color color)
    {
        if (zoneIndex is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(zoneIndex), "Bölge 0..3 aralığında olmalıdır");

        var mask = (byte)(1 << zoneIndex);
        return ((ulong)color.B << 24) | ((ulong)color.G << 16) | ((ulong)color.R << 8) | mask;
    }

    public static Color ParseHexColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        var s = hex.Trim();
        if (s.StartsWith('#')) s = s[1..];
        if (s.Length != 6) return fallback;
        if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return fallback;

        return Color.FromArgb(
            (int)((rgb >> 16) & 0xFF),
            (int)((rgb >> 8) & 0xFF),
            (int)(rgb & 0xFF));
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
