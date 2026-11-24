using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using AcerKontrol.Keyboard;

namespace AcerKontrol.UI;

/// <summary>Yuvarlatılmış köşeli, isteğe bağlı üst vurgu şeritli modern kart konteyneri.</summary>
public sealed class RoundedCard : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = DarkTheme.Border;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 12;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? AccentBarColor { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Title { get; set; }

    public RoundedCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = DarkTheme.BgCard;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var parentBg = Parent?.BackColor ?? DarkTheme.BgWindow;
        using var bgBrush = new SolidBrush(parentBg);
        e.Graphics.FillRectangle(bgBrush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = DarkTheme.CreateRoundedRect(rect, CornerRadius);
        using var fill = new SolidBrush(BackColor);
        g.FillPath(fill, path);

        if (AccentBarColor is Color accent)
        {
            g.SetClip(path);
            using var barBrush = new LinearGradientBrush(
                new RectangleF(0, 0, Width, 3),
                accent,
                DarkTheme.WithAlpha(accent, 90),
                LinearGradientMode.Horizontal);
            g.FillRectangle(barBrush, 0, 0, Width, 3);
            g.ResetClip();
        }

        if (!string.IsNullOrEmpty(Title))
        {
            var titleRect = new Rectangle(Padding.Left, Padding.Top - 1, Width - Padding.Horizontal, 24);
            TextRenderer.DrawText(g, Title, DarkTheme.FontSection, titleRect, DarkTheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        using var pen = new Pen(BorderColor, 1f);
        g.DrawPath(pen, path);
    }
}

/// <summary>Yuvarlatılmış, hover/aktif efektli ve isteğe bağlı renk noktalı modern buton.</summary>
public sealed class ModernButton : Control
{
    private bool isActive;
    private bool isHovered;
    private bool isPressed;
    private Color activeColor = DarkTheme.Accent;
    private Color? leadingDotColor;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 9;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsPrimaryAction { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? LeadingDotColor
    {
        get => leadingDotColor;
        set { leadingDotColor = value; Invalidate(); }
    }

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        Height = 36;
        Font = DarkTheme.FontBodyBold;
        ForeColor = DarkTheme.TextPrimary;
        BackColor = Color.Transparent;
    }

    public void SetActive(bool active, Color? color = null)
    {
        isActive = active;
        if (color is Color c) activeColor = c;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { isHovered = false; isPressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled) { isPressed = true; Focus(); Invalidate(); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (isPressed) { isPressed = false; Invalidate(); }
        base.OnMouseUp(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = DarkTheme.CreateRoundedRect(rect, CornerRadius);

        Color fill;
        Color border;
        Color textCol;

        if (!Enabled)
        {
            fill = DarkTheme.WithAlpha(DarkTheme.BgInput, 120);
            border = DarkTheme.BorderSubtle;
            textCol = DarkTheme.TextMuted;
        }
        else if (IsPrimaryAction)
        {
            fill = isPressed ? DarkTheme.Blend(activeColor, Color.Black, 0.18f)
                 : isHovered ? DarkTheme.Blend(activeColor, Color.White, 0.12f)
                 : activeColor;
            border = fill;
            textCol = DarkTheme.ContrastText(fill);
        }
        else if (isActive)
        {
            fill = isPressed ? DarkTheme.Blend(activeColor, DarkTheme.BgCard, 0.25f)
                 : isHovered ? DarkTheme.Blend(activeColor, Color.White, 0.10f)
                 : activeColor;
            border = DarkTheme.Blend(activeColor, Color.White, 0.15f);
            textCol = DarkTheme.ContrastText(fill);
        }
        else
        {
            fill = isPressed ? DarkTheme.BgCard
                 : isHovered ? DarkTheme.BgInputHover
                 : DarkTheme.BgInput;
            border = isHovered ? DarkTheme.Blend(DarkTheme.Border, DarkTheme.TextSecondary, 0.25f) : DarkTheme.Border;
            textCol = DarkTheme.TextPrimary;
        }

        using (var brush = new SolidBrush(fill))
            g.FillPath(brush, path);

        using (var pen = new Pen(border, 1f))
            g.DrawPath(pen, path);

        var textRect = new Rectangle(4, 0, Width - 8, Height);
        if (leadingDotColor is Color dotCol)
        {
            const int dotSize = 8;
            var dotY = (Height - dotSize) / 2;
            var dotX = 12;
            using var dotBrush = new SolidBrush(Enabled ? dotCol : DarkTheme.TextMuted);
            g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            textRect = new Rectangle(dotX + dotSize + 7, 0, Width - (dotX + dotSize + 15), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textCol,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, textRect, textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}

public enum NavIconKind { Overview, Fan, Keyboard, System }

/// <summary>Sol kenar çubuğu için özel vektör ikonlu ve aktif göstergeli navigasyon öğesi.</summary>
public sealed class ModernNavButton : Control
{
    private bool isSelected;
    private bool isHovered;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public NavIconKind IconKind { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => isSelected;
        set { if (isSelected != value) { isSelected = value; Invalidate(); } }
    }

    public ModernNavButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Cursor = Cursors.Hand;
        Height = 42;
        Font = DarkTheme.FontBodyBold;
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (isSelected || isHovered)
        {
            var bgRect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using var path = DarkTheme.CreateRoundedRect(bgRect, 10);
            var bgCol = isSelected ? DarkTheme.BgCard : DarkTheme.WithAlpha(DarkTheme.BgCard, 130);
            using var brush = new SolidBrush(bgCol);
            g.FillPath(brush, path);

            if (isSelected)
            {
                using var borderPen = new Pen(DarkTheme.Border, 1f);
                g.DrawPath(borderPen, path);

                // Sol aktif vurgu çubuğu
                using var pill = DarkTheme.CreateRoundedRect(new RectangleF(4, 10, 3.5f, Height - 20), 2f);
                using var accentBrush = new SolidBrush(DarkTheme.Accent);
                g.FillPath(accentBrush, pill);
            }
        }

        var iconColor = isSelected ? DarkTheme.Accent : (isHovered ? DarkTheme.TextPrimary : DarkTheme.TextSecondary);
        DrawVectorIcon(g, IconKind, new RectangleF(16, (Height - 16) / 2f, 16, 16), iconColor);

        var textColor = isSelected ? DarkTheme.TextPrimary : (isHovered ? DarkTheme.TextPrimary : DarkTheme.TextSecondary);
        var textRect = new Rectangle(42, 0, Width - 50, Height);
        TextRenderer.DrawText(g, Text, Font, textRect, textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void DrawVectorIcon(Graphics g, NavIconKind kind, RectangleF r, Color color)
    {
        using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);

        switch (kind)
        {
            case NavIconKind.Overview:
                g.DrawRoundedRect(pen, new RectangleF(r.X + 1, r.Y + 1, 6, 6), 1.5f);
                g.DrawRoundedRect(pen, new RectangleF(r.X + 9, r.Y + 1, 6, 6), 1.5f);
                g.DrawRoundedRect(pen, new RectangleF(r.X + 1, r.Y + 9, 6, 6), 1.5f);
                g.DrawRoundedRect(pen, new RectangleF(r.X + 9, r.Y + 9, 6, 6), 1.5f);
                break;

            case NavIconKind.Fan:
                g.DrawEllipse(pen, r.X + 1, r.Y + 1, 14, 14);
                var cx = r.X + 8;
                var cy = r.Y + 8;
                g.DrawLine(pen, cx, cy - 4.5f, cx, cy + 4.5f);
                g.DrawLine(pen, cx - 4.5f, cy, cx + 4.5f, cy);
                g.FillEllipse(brush, cx - 2, cy - 2, 4, 4);
                break;

            case NavIconKind.Keyboard:
                g.DrawRoundedRect(pen, new RectangleF(r.X + 0.5f, r.Y + 3, 15, 10), 2f);
                g.FillRectangle(brush, r.X + 3, r.Y + 6, 2, 1.8f);
                g.FillRectangle(brush, r.X + 6.5f, r.Y + 6, 2, 1.8f);
                g.FillRectangle(brush, r.X + 10, r.Y + 6, 2, 1.8f);
                g.FillRectangle(brush, r.X + 4.5f, r.Y + 9.5f, 7, 1.6f);
                break;

            case NavIconKind.System:
                g.DrawRoundedRect(pen, new RectangleF(r.X + 1, r.Y + 4, 12, 8), 2f);
                g.FillRectangle(brush, r.X + 13.5f, r.Y + 6.5f, 2, 3);
                g.FillRectangle(brush, r.X + 3.5f, r.Y + 6.2f, 5.5f, 3.6f);
                break;
        }
    }
}

/// <summary>Yuvarlatılmış, gradyan dolgulu modern ilerleme çubuğu.</summary>
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
        set { if (fillColor != value) { fillColor = value; Invalidate(); } }
    }

    public SmoothProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Height = 7;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var trackRect = new RectangleF(0, 0, Width - 0.5f, Height - 0.5f);
        var radius = Height / 2f;
        using var trackPath = DarkTheme.CreateRoundedRect(trackRect, radius);
        using var trackBrush = new SolidBrush(DarkTheme.BgInput);
        g.FillPath(trackBrush, trackPath);

        if (value > 0)
        {
            var fillWidth = Math.Max(Height, Width * (value / 100f));
            var fillRect = new RectangleF(0, 0, Math.Min(Width - 0.5f, fillWidth), Height - 0.5f);
            using var fillPath = DarkTheme.CreateRoundedRect(fillRect, radius);
            using var grad = new LinearGradientBrush(
                fillRect,
                DarkTheme.Blend(fillColor, Color.Black, 0.12f),
                DarkTheme.Blend(fillColor, Color.White, 0.16f),
                LinearGradientMode.Horizontal);
            g.FillPath(grad, fillPath);
        }
    }
}

/// <summary>Klasik Win32 TrackBar yerine yuvarlatılmış, parlak izli ve sürüklenebilir modern kaydırıcı.</summary>
public sealed class ModernSlider : Control
{
    private int minimum;
    private int maximum = 100;
    private int value = 50;
    private bool dragging;
    private bool hovered;
    private Color accentColor = DarkTheme.Cyan;

    public event EventHandler? ValueChanged;
    public event EventHandler? Scroll;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum
    {
        get => minimum;
        set { minimum = value; if (this.value < minimum) Value = minimum; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => maximum;
        set { maximum = Math.Max(minimum + 1, value); if (this.value > maximum) Value = maximum; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => value;
        set
        {
            var clamped = Math.Clamp(value, minimum, maximum);
            if (this.value == clamped) return;
            this.value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => accentColor;
        set { accentColor = value; Invalidate(); }
    }

    public ModernSlider()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        Height = 32;
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled)
        {
            dragging = true;
            Focus();
            UpdateFromMouseX(e.X);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (dragging && Enabled) UpdateFromMouseX(e.X);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        dragging = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (!Enabled) return;
        var step = Math.Max(1, (maximum - minimum) / 20);
        var next = value + (e.Delta > 0 ? step : -step);
        SetFromUser(next);
        base.OnMouseWheel(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;
        if (e.KeyCode is Keys.Left or Keys.Down) { SetFromUser(value - 1); e.Handled = true; }
        else if (e.KeyCode is Keys.Right or Keys.Up) { SetFromUser(value + 1); e.Handled = true; }
        base.OnKeyDown(e);
    }

    private void UpdateFromMouseX(int x)
    {
        const float pad = 10f;
        var usable = Math.Max(1f, Width - pad * 2);
        var ratio = Math.Clamp((x - pad) / usable, 0f, 1f);
        var target = (int)Math.Round(minimum + ratio * (maximum - minimum));
        SetFromUser(target);
    }

    private void SetFromUser(int target)
    {
        var clamped = Math.Clamp(target, minimum, maximum);
        if (clamped == value) return;
        value = clamped;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
        Scroll?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        const float pad = 10f;
        const float trackH = 6f;
        var trackY = (Height - trackH) / 2f;
        var usableW = Math.Max(1f, Width - pad * 2);
        var ratio = (float)(value - minimum) / Math.Max(1, maximum - minimum);
        var thumbX = pad + ratio * usableW;

        var trackRect = new RectangleF(pad, trackY, usableW, trackH);
        using (var trackPath = DarkTheme.CreateRoundedRect(trackRect, trackH / 2f))
        using (var trackBrush = new SolidBrush(DarkTheme.BgInput))
            g.FillPath(trackBrush, trackPath);

        var activeCol = Enabled ? accentColor : DarkTheme.TextMuted;
        if (ratio > 0f)
        {
            var fillRect = new RectangleF(pad, trackY, Math.Max(trackH, thumbX - pad), trackH);
            using var fillPath = DarkTheme.CreateRoundedRect(fillRect, trackH / 2f);
            using var fillBrush = new SolidBrush(activeCol);
            g.FillPath(fillBrush, fillPath);
        }

        var thumbRadius = dragging ? 8.5f : (hovered && Enabled ? 8f : 7f);
        var cy = Height / 2f;

        if ((hovered || dragging) && Enabled)
        {
            using var halo = new SolidBrush(DarkTheme.WithAlpha(activeCol, 55));
            g.FillEllipse(halo, thumbX - thumbRadius - 4, cy - thumbRadius - 4, (thumbRadius + 4) * 2, (thumbRadius + 4) * 2);
        }

        using var outerBrush = new SolidBrush(activeCol);
        g.FillEllipse(outerBrush, thumbX - thumbRadius, cy - thumbRadius, thumbRadius * 2, thumbRadius * 2);

        var innerR = thumbRadius - 2.6f;
        using var innerBrush = new SolidBrush(Enabled ? DarkTheme.TextPrimary : DarkTheme.BgCard);
        g.FillEllipse(innerBrush, thumbX - innerR, cy - innerR, innerR * 2, innerR * 2);
    }
}

/// <summary>Klasik CheckBox yerine Fluent tarzı satır içi modern anahtar (toggle switch).</summary>
public sealed class ModernToggleSwitch : Control
{
    private bool isChecked;
    private bool hovered;
    private Color accentColor = DarkTheme.Green;

    public event EventHandler? CheckedChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => isChecked;
        set
        {
            if (isChecked == value) return;
            isChecked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AutoCheck { get; set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => accentColor;
        set { accentColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Subtitle { get; set; }

    public ModernToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        Height = 36;
        Font = DarkTheme.FontBody;
        ForeColor = DarkTheme.TextPrimary;
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnClick(EventArgs e)
    {
        if (!Enabled) return;
        if (AutoCheck) Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (hovered && Enabled)
        {
            using var hoverPath = DarkTheme.CreateRoundedRect(new RectangleF(0, 1, Width - 1, Height - 2), 8);
            using var hoverBrush = new SolidBrush(DarkTheme.WithAlpha(DarkTheme.BgInput, 110));
            g.FillPath(hoverBrush, hoverPath);
        }

        const int swW = 42;
        const int swH = 22;
        var swX = Width - swW - 6;
        var swY = (Height - swH) / 2;

        var trackRect = new RectangleF(swX, swY, swW, swH);
        using var trackPath = DarkTheme.CreateRoundedRect(trackRect, swH / 2f);
        var trackColor = !Enabled ? DarkTheme.BorderSubtle
            : isChecked ? accentColor
            : DarkTheme.BgInput;
        using (var trackBrush = new SolidBrush(trackColor))
            g.FillPath(trackBrush, trackPath);

        if (!isChecked)
        {
            using var borderPen = new Pen(DarkTheme.Border, 1f);
            g.DrawPath(borderPen, trackPath);
        }

        const int knob = 16;
        var knobX = isChecked ? swX + swW - knob - 3 : swX + 3;
        var knobY = swY + (swH - knob) / 2;
        using (var knobBrush = new SolidBrush(Enabled ? Color.White : DarkTheme.TextMuted))
            g.FillEllipse(knobBrush, knobX, knobY, knob, knob);

        var textColor = Enabled ? DarkTheme.TextPrimary : DarkTheme.TextMuted;
        var textWidth = Math.Max(20, swX - 16);
        if (string.IsNullOrEmpty(Subtitle))
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(6, 0, textWidth, Height), textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else
        {
            TextRenderer.DrawText(g, Text, DarkTheme.FontBodyBold, new Rectangle(6, 2, textWidth, 20), textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, Subtitle, DarkTheme.FontSmall, new Rectangle(6, 20, textWidth, 18), DarkTheme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}

/// <summary>Canlı durum noktalı yuvarlatılmış bilgi rozeti (örn. Acer WMI durumu).</summary>
public sealed class StatusPill : Control
{
    private Color dotColor = DarkTheme.TextSecondary;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DotColor
    {
        get => dotColor;
        set { dotColor = value; Invalidate(); }
    }

    public StatusPill()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Height = 30;
        Font = DarkTheme.FontSmallBold;
        ForeColor = DarkTheme.TextPrimary;
        BackColor = Color.Transparent;
    }

    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = DarkTheme.CreateRoundedRect(rect, 9);
        using (var bg = new SolidBrush(DarkTheme.BgCard))
            g.FillPath(bg, path);
        using (var pen = new Pen(DarkTheme.BorderSubtle, 1f))
            g.DrawPath(pen, path);

        var cy = Height / 2f;
        using (var halo = new SolidBrush(DarkTheme.WithAlpha(dotColor, 60)))
            g.FillEllipse(halo, 9, cy - 6, 12, 12);
        using (var dot = new SolidBrush(dotColor))
            g.FillEllipse(dot, 12, cy - 3, 6, 6);

        TextRenderer.DrawText(g, Text, Font, new Rectangle(26, 0, Width - 32, Height), ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>4 bölgeli RGB klavye için etkileşimli, parlayan klavye şeridi önizlemesi.</summary>
public sealed class KeyboardZonePreview : Control
{
    private Color[] zoneColors = [DarkTheme.Accent, DarkTheme.Accent, DarkTheme.Accent, DarkTheme.Accent];
    private int brightness = 100;
    private int hoveredZone = -1;

    public event Action<int>? ZoneClicked;

    public KeyboardZonePreview()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Cursor = Cursors.Hand;
        Height = 76;
        BackColor = Color.Transparent;
    }

    public void SetZones(IReadOnlyList<string> hexColors, int brightnessPercent)
    {
        var next = new Color[4];
        for (var i = 0; i < 4; i++)
            next[i] = i < hexColors.Count ? KeyboardCodec.ParseHexColor(hexColors[i], DarkTheme.Accent) : DarkTheme.Accent;
        zoneColors = next;
        brightness = Math.Clamp(brightnessPercent, 0, 100);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var idx = HitTestZone(e.X);
        if (idx != hoveredZone) { hoveredZone = idx; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (hoveredZone != -1) { hoveredZone = -1; Invalidate(); }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var idx = HitTestZone(e.X);
            if (idx >= 0) ZoneClicked?.Invoke(idx);
        }
        base.OnMouseClick(e);
    }

    private int HitTestZone(int x)
    {
        if (Width <= 0) return -1;
        return Math.Clamp(x * 4 / Width, 0, 3);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // Klavye kasası arka planı
        var shell = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var shellPath = DarkTheme.CreateRoundedRect(shell, 11))
        using (var shellBrush = new SolidBrush(DarkTheme.BgWindow))
        using (var shellPen = new Pen(DarkTheme.Border, 1f))
        {
            g.FillPath(shellBrush, shellPath);
            g.DrawPath(shellPen, shellPath);
        }

        const float gap = 8f;
        const float pad = 8f;
        var zoneW = (Width - pad * 2 - gap * 3) / 4f;
        var zoneH = Height - pad * 2;
        var dimFactor = brightness == 0 ? 0.14f : 0.35f + (brightness / 100f) * 0.65f;

        for (var i = 0; i < 4; i++)
        {
            var raw = zoneColors[i];
            var lit = DarkTheme.Blend(DarkTheme.BgCard, raw, dimFactor);
            var zx = pad + i * (zoneW + gap);
            var zRect = new RectangleF(zx, pad, zoneW, zoneH);
            var isHov = i == hoveredZone;

            using var zPath = DarkTheme.CreateRoundedRect(zRect, 8);
            using (var grad = new LinearGradientBrush(
                zRect,
                DarkTheme.Blend(lit, Color.White, isHov ? 0.16f : 0.06f),
                DarkTheme.Blend(lit, Color.Black, 0.18f),
                LinearGradientMode.Vertical))
            {
                g.FillPath(grad, zPath);
            }

            // İnce tuş sırası silüeti
            using (var keyPen = new Pen(DarkTheme.WithAlpha(DarkTheme.ContrastText(lit), 32), 1f))
            {
                g.DrawLine(keyPen, zx + 10, pad + 11, zx + zoneW - 10, pad + 11);
                g.DrawLine(keyPen, zx + 14, pad + zoneH - 10, zx + zoneW - 14, pad + zoneH - 10);
            }

            using (var border = new Pen(isHov ? Color.White : DarkTheme.WithAlpha(raw, 180), isHov ? 1.8f : 1f))
                g.DrawPath(border, zPath);

            var textCol = DarkTheme.ContrastText(lit);
            TextRenderer.DrawText(g, $"Bölge {i + 1}", DarkTheme.FontBodyBold,
                new Rectangle((int)zx, (int)pad + 12, (int)zoneW, 20), textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, KeyboardCodec.ToHex(raw), DarkTheme.FontSmall,
                new Rectangle((int)zx, (int)pad + 31, (int)zoneW, 18), DarkTheme.WithAlpha(textCol, 220),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}

internal static class GraphicsExtensions
{
    public static void DrawRoundedRect(this Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = DarkTheme.CreateRoundedRect(rect, radius);
        g.DrawPath(pen, path);
    }
}
