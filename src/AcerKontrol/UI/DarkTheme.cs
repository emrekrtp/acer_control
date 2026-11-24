using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AcerKontrol.UI;

/// <summary>Koyu Nitro tema renkleri, özel bileşenler ve Windows 11 koyu başlık çubuğu yardımcıları.</summary>
public static class DarkTheme
{
    public static readonly Color BgWindow = Color.FromArgb(18, 20, 26);
    public static readonly Color BgSidebar = Color.FromArgb(24, 27, 36);
    public static readonly Color BgCard = Color.FromArgb(30, 34, 45);
    public static readonly Color BgInput = Color.FromArgb(40, 46, 61);
    public static readonly Color Border = Color.FromArgb(50, 57, 74);

    public static readonly Color Accent = Color.FromArgb(229, 37, 42);
    public static readonly Color AccentHover = Color.FromArgb(255, 62, 68);
    public static readonly Color Cyan = Color.FromArgb(0, 200, 255);
    public static readonly Color Green = Color.FromArgb(34, 197, 94);
    public static readonly Color Amber = Color.FromArgb(245, 158, 11);

    public static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);
    public static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);

    public static Font FontTitle { get; } = new("Segoe UI", 14f, FontStyle.Bold);
    public static Font FontSection { get; } = new("Segoe UI", 10.5f, FontStyle.Bold);
    public static Font FontMetric { get; } = new("Segoe UI", 18f, FontStyle.Bold);
    public static Font FontBody { get; } = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static Font FontBodyBold { get; } = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static Font FontSmall { get; } = new("Segoe UI", 8.5f, FontStyle.Regular);

    public static void EnableDarkTitleBar(IntPtr handle)
    {
        try
        {
            var useDark = 1;
            DwmSetWindowAttribute(handle, 20, ref useDark, sizeof(int));
        }
        catch
        {
            // Eski Windows sürümlerinde sessizce yoksayılır.
        }
    }

    public static Panel CreateCard(string? title = null, Padding? padding = null)
    {
        var card = new BorderedPanel
        {
            BackColor = BgCard,
            BorderColor = Border,
            Padding = padding ?? new Padding(14),
            Margin = new Padding(0, 0, 0, 10),
        };
        if (!string.IsNullOrEmpty(title))
        {
            var lbl = new Label
            {
                Text = title,
                Font = FontSection,
                ForeColor = TextPrimary,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 8),
            };
            card.Controls.Add(lbl);
        }
        return card;
    }

    public static Button CreatePillButton(string text, bool active = false, Color? activeColor = null)
    {
        var btn = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = FontBodyBold,
            Cursor = Cursors.Hand,
            Height = 34,
            AutoEllipsis = true,
        };
        StylePillButton(btn, active, activeColor);
        return btn;
    }

    public static void StylePillButton(Button btn, bool active, Color? activeColor = null)
    {
        var accent = activeColor ?? Accent;
        btn.BackColor = active ? accent : BgInput;
        btn.ForeColor = TextPrimary;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = active ? accent : Border;
        btn.FlatAppearance.MouseOverBackColor = active ? AccentHover : Color.FromArgb(52, 60, 78);
    }

    public static Button CreateAccentButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = FontBodyBold,
            ForeColor = TextPrimary,
            BackColor = Accent,
            Cursor = Cursors.Hand,
            Height = 34,
            AutoSize = false,
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = AccentHover;
        return btn;
    }

    public static Color ContrastText(Color bg)
    {
        var luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
        return luminance > 0.58 ? Color.FromArgb(15, 23, 42) : Color.White;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}

public sealed class BorderedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = DarkTheme.Border;

    public BorderedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(BorderColor, 1f);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

public sealed class SmoothProgressBar : Control
{
    private int value;
    private Color fillColor = DarkTheme.Cyan;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => value;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (this.value == clamped) return;
            this.value = clamped;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => fillColor;
        set
        {
            if (fillColor == value) return;
            fillColor = value;
            Invalidate();
        }
    }

    public SmoothProgressBar()
    {
        DoubleBuffered = true;
        Height = 8;
        BackColor = DarkTheme.BgInput;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var trackBrush = new SolidBrush(DarkTheme.BgInput);
        e.Graphics.FillRectangle(trackBrush, 0, 0, Width, Height);

        if (value > 0)
        {
            var fillWidth = Math.Max(2, (int)Math.Round(Width * (value / 100.0)));
            using var barBrush = new SolidBrush(fillColor);
            e.Graphics.FillRectangle(barBrush, 0, 0, fillWidth, Height);
        }
    }
}
