using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AcerKontrol.UI;

/// <summary>Modern koyu Nitro tema renk sistemi, tipografi, çizim yardımcıları ve Windows 11 DWM entegrasyonu.</summary>
public static class DarkTheme
{
    // Derin koyu yüzey paleti
    public static readonly Color BgWindow = Color.FromArgb(13, 16, 23);
    public static readonly Color BgSidebar = Color.FromArgb(17, 21, 31);
    public static readonly Color BgCard = Color.FromArgb(24, 29, 42);
    public static readonly Color BgCardHover = Color.FromArgb(30, 36, 52);
    public static readonly Color BgInput = Color.FromArgb(34, 41, 58);
    public static readonly Color BgInputHover = Color.FromArgb(44, 53, 74);
    public static readonly Color Border = Color.FromArgb(44, 53, 74);
    public static readonly Color BorderSubtle = Color.FromArgb(32, 39, 55);

    // Vurgu renkleri
    public static readonly Color Accent = Color.FromArgb(239, 35, 60);
    public static readonly Color AccentHover = Color.FromArgb(255, 61, 86);
    public static readonly Color Cyan = Color.FromArgb(14, 196, 236);
    public static readonly Color Green = Color.FromArgb(16, 185, 129);
    public static readonly Color Amber = Color.FromArgb(245, 158, 11);
    public static readonly Color Violet = Color.FromArgb(139, 92, 246);

    // Metin hiyerarşisi
    public static readonly Color TextPrimary = Color.FromArgb(244, 247, 252);
    public static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
    public static readonly Color TextMuted = Color.FromArgb(94, 109, 134);

    // Tipografi
    public static Font FontPageTitle { get; } = CreateFont(17f, FontStyle.Bold);
    public static Font FontTitle { get; } = CreateFont(13.5f, FontStyle.Bold);
    public static Font FontSection { get; } = CreateFont(10.5f, FontStyle.Bold);
    public static Font FontMetric { get; } = CreateFont(21f, FontStyle.Bold);
    public static Font FontBody { get; } = CreateFont(9.5f, FontStyle.Regular);
    public static Font FontBodyBold { get; } = CreateFont(9.5f, FontStyle.Bold);
    public static Font FontSmall { get; } = CreateFont(8.5f, FontStyle.Regular);
    public static Font FontSmallBold { get; } = CreateFont(8.2f, FontStyle.Bold);

    private static Font CreateFont(float size, FontStyle style)
    {
        try
        {
            using var probe = new Font("Segoe UI Variable Text", size, style);
            if (probe.Name.StartsWith("Segoe UI Variable", StringComparison.OrdinalIgnoreCase))
                return new Font("Segoe UI Variable Text", size, style);
        }
        catch
        {
            // Windows 10 ve öncesi için Segoe UI kullanılır.
        }
        return new Font("Segoe UI", size, style);
    }

    public static void EnableDarkTitleBar(IntPtr handle)
    {
        try
        {
            // DWMWA_USE_IMMERSIVE_DARK_MODE (20)
            var useDark = 1;
            DwmSetWindowAttribute(handle, 20, ref useDark, sizeof(int));

            // DWMWA_WINDOW_CORNER_PREFERENCE (33) = DWMWCP_ROUND (2)
            var cornerPref = 2;
            DwmSetWindowAttribute(handle, 33, ref cornerPref, sizeof(int));

            // DWMWA_CAPTION_COLOR (35) — COLORREF (0x00BBGGRR)
            var captionColor = (BgWindow.B << 16) | (BgWindow.G << 8) | BgWindow.R;
            DwmSetWindowAttribute(handle, 35, ref captionColor, sizeof(int));
        }
        catch
        {
            // Eski Windows sürümlerinde sessizce yoksayılır.
        }
    }

    public static RoundedCard CreateCard(string? title = null, Padding? padding = null, Color? accentBar = null)
    {
        return new RoundedCard
        {
            Title = title,
            BackColor = BgCard,
            BorderColor = Border,
            AccentBarColor = accentBar,
            CornerRadius = 12,
            Padding = padding ?? new Padding(16, 14, 16, 14),
            Margin = new Padding(0, 0, 0, 12),
        };
    }

    public static ModernButton CreatePillButton(string text, bool active = false, Color? activeColor = null)
    {
        var btn = new ModernButton
        {
            Text = text,
            Font = FontBodyBold,
            Height = 36,
            CornerRadius = 9,
        };
        btn.SetActive(active, activeColor ?? Accent);
        return btn;
    }

    public static ModernButton CreateAccentButton(string text)
    {
        var btn = new ModernButton
        {
            Text = text,
            Font = FontBodyBold,
            Height = 36,
            CornerRadius = 9,
            IsPrimaryAction = true,
        };
        btn.SetActive(true, Accent);
        return btn;
    }

    public static Color ThermalColor(int? tempC, Color fallback)
    {
        if (tempC is not int t) return fallback;
        if (t >= 85) return Accent;
        if (t >= 72) return Amber;
        if (t >= 58) return Cyan;
        return Green;
    }

    public static Color ContrastText(Color bg)
    {
        var luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
        return luminance > 0.58 ? Color.FromArgb(15, 23, 42) : Color.White;
    }

    public static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)Math.Round(a.A + (b.A - a.A) * t),
            (int)Math.Round(a.R + (b.R - a.R) * t),
            (int)Math.Round(a.G + (b.G - a.G) * t),
            (int)Math.Round(a.B + (b.B - a.B) * t));
    }

    public static Color WithAlpha(Color c, int alpha) =>
        Color.FromArgb(Math.Clamp(alpha, 0, 255), c.R, c.G, c.B);

    public static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2f, Math.Min(rect.Width, rect.Height));
        if (d <= 0.5f)
        {
            path.AddRectangle(rect);
            return path;
        }
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
