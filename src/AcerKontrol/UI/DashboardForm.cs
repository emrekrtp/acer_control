using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Security.Principal;
using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Diagnostics;
using AcerKontrol.Fan;
using AcerKontrol.Keyboard;
using AcerKontrol.Settings;
using AcerKontrol.SystemControl;
using AcerKontrol.Tray;

namespace AcerKontrol.UI;

/// <summary>Acer Nitro 5 için modern koyu temalı, TR/EN destekli kontrol paneli. Kapatıldığında tepsiye gizlenir.</summary>
public sealed class DashboardForm : Form
{
    private readonly List<Action> localizeActions = [];
    private readonly Panel[] pages;
    private readonly ModernNavButton[] navButtons;
    private readonly ModernButton langTrBtn, langEnBtn;
    private readonly Label feedbackLabel;
    private readonly StatusPill wmiStatusPill;

    // Genel Bakış
    private readonly Label ovCpuTemp = MetricLabel(), ovCpuSub = SubLabel();
    private readonly SmoothProgressBar ovCpuBar = new() { FillColor = DarkTheme.Cyan, Dock = DockStyle.Bottom };
    private readonly Label ovGpuTemp = MetricLabel(), ovGpuSub = SubLabel();
    private readonly SmoothProgressBar ovGpuBar = new() { FillColor = DarkTheme.Violet, Dock = DockStyle.Bottom };
    private readonly Label ovRamVal = MetricLabel(), ovRamSub = SubLabel();
    private readonly SmoothProgressBar ovRamBar = new() { FillColor = DarkTheme.Amber, Dock = DockStyle.Bottom };
    private readonly Label ovBatVal = MetricLabel(), ovBatSub = SubLabel();
    private readonly SmoothProgressBar ovBatBar = new() { FillColor = DarkTheme.Green, Dock = DockStyle.Bottom };
    private readonly Dictionary<PerformanceProfile, ModernButton> ovProfileBtns = [];
    private readonly Dictionary<FanMode, ModernButton> ovFanBtns = [];
    private readonly ModernButton ovLimitBtn;
    private readonly ModernButton ovKbToggleBtn;

    // Fan & Performans
    private readonly Dictionary<PerformanceProfile, ModernButton> fanProfileBtns = [];
    private readonly Dictionary<FanMode, ModernButton> fanModeBtns = [];
    private readonly ModernToggleSwitch coolBoostToggle;
    private readonly StatusPill fanLivePill;
    private readonly ModernSlider cpuFanSlider, gpuFanSlider;
    private readonly Label cpuFanPctLabel, gpuFanPctLabel;
    private readonly ModernToggleSwitch syncFansToggle;
    private readonly ModernButton applyCustomFanBtn;
    private readonly Dictionary<WindowsPowerPlan, ModernButton> powerPlanBtns = [];

    // Klavye Işığı
    private readonly Dictionary<KeyboardEffect, ModernButton> effectBtns = [];
    private readonly ModernSlider kbBrightnessSlider, kbSpeedSlider;
    private readonly Label kbBrightnessLabel, kbSpeedLabel;
    private readonly ModernButton dirRtlBtn, dirLtrBtn;
    private readonly KeyboardZonePreview zonePreview;
    private readonly ModernButton applyKeyboardBtn;
    private KeyboardLightingSettings editingKeyboard = new();

    // Pil & Sistem
    private readonly Label batPct = DetailVal(), batState = DetailVal(), batHealth = DetailVal(),
        batCap = DetailVal(), batCycles = DetailVal(), batTemp = DetailVal(), batLimit = DetailVal();
    private readonly ModernToggleSwitch sysLimitToggle, kbTimeoutToggle, lcdOverdriveToggle, usbChargingToggle, reapplyWakeToggle;
    private readonly ModernSlider displayBrightnessSlider;
    private readonly Label displayBrightnessLabel;
    private readonly Label diagAdminLabel = DetailVal(), diagWmiLabel = DetailVal(), diagPathLabel = DetailVal();

    private bool suppressEvents;
    private int lastNonZeroKbBrightness = 100;

    public event Action? LimitToggled;
    public event Action<FanMode>? FanModeSelected;
    public event Action<int, int>? CustomFanSpeedsApplied;
    public event Action<bool>? CoolBoostToggled;
    public event Action<PerformanceProfile>? PerformanceProfileSelected;
    public event Action<WindowsPowerPlan>? WindowsPowerPlanSelected;
    public event Action<KeyboardLightingSettings>? KeyboardApplied;
    public event Action<bool>? KeyboardTimeoutToggled;
    public event Action<bool>? LcdOverdriveToggled;
    public event Action<bool>? UsbChargingToggled;
    public event Action<bool>? ReapplyOnWakeToggled;
    public event Action<int>? DisplayBrightnessChanged;
    public event Action<string>? LanguageSelected;

    public DashboardForm(string settingsPath)
    {
        Size = new Size(980, 680);
        MinimumSize = new Size(920, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = DarkTheme.BgWindow;
        ForeColor = DarkTheme.TextPrimary;
        Font = DarkTheme.FontBody;
        DoubleBuffered = true;

        Localize(() => Text = $"{Strings.AppName} — {Strings.AppSubtitle}");
        HandleCreated += (_, _) => DarkTheme.EnableDarkTitleBar(Handle);

        // Sol kenar çubuğu
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 224,
            BackColor = DarkTheme.BgSidebar,
            Padding = new Padding(14, 18, 14, 14),
        };
        sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(DarkTheme.BorderSubtle, 1f);
            e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
        };

        var brandPanel = BuildBrandHeader();
        brandPanel.Location = new Point(14, 16);
        brandPanel.Size = new Size(196, 54);
        sidebar.Controls.Add(brandPanel);

        (Func<string> Title, NavIconKind Icon)[] navItems =
        [
            (() => Strings.NavOverview, NavIconKind.Overview),
            (() => Strings.NavFan, NavIconKind.Fan),
            (() => Strings.NavKeyboard, NavIconKind.Keyboard),
            (() => Strings.NavSystem, NavIconKind.System),
        ];
        navButtons = new ModernNavButton[navItems.Length];
        for (var i = 0; i < navItems.Length; i++)
        {
            var idx = i;
            var titleFn = navItems[i].Title;
            var btn = new ModernNavButton
            {
                IconKind = navItems[i].Icon,
                Location = new Point(14, 92 + i * 48),
                Size = new Size(196, 42),
            };
            Localize(() => btn.Text = titleFn());
            btn.Click += (_, _) => SelectPage(idx);
            sidebar.Controls.Add(btn);
            navButtons[i] = btn;
        }

        // Alt durum alanı (WMI + Geri Bildirim + TR/EN Dil Seçici)
        wmiStatusPill = new StatusPill
        {
            Text = Strings.WmiChecking,
            DotColor = DarkTheme.TextSecondary,
            Dock = DockStyle.Bottom,
            Height = 32,
        };

        var feedbackCard = new RoundedCard
        {
            BackColor = DarkTheme.BgCard,
            BorderColor = DarkTheme.BorderSubtle,
            CornerRadius = 9,
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(10, 6, 10, 6),
        };
        feedbackLabel = new Label
        {
            Text = Strings.Ready,
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.Green,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };
        feedbackCard.Controls.Add(feedbackLabel);

        var langRow = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            ColumnCount = 2,
            BackColor = Color.Transparent,
        };
        langRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        langRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        langTrBtn = DarkTheme.CreatePillButton("TR", !Strings.IsEnglish, DarkTheme.Accent);
        langTrBtn.Dock = DockStyle.Fill;
        langTrBtn.Margin = new Padding(0, 0, 3, 0);
        langTrBtn.Click += (_, _) => { if (Strings.IsEnglish) LanguageSelected?.Invoke("tr"); };
        langEnBtn = DarkTheme.CreatePillButton("EN", Strings.IsEnglish, DarkTheme.Cyan);
        langEnBtn.Dock = DockStyle.Fill;
        langEnBtn.Margin = new Padding(3, 0, 0, 0);
        langEnBtn.Click += (_, _) => { if (!Strings.IsEnglish) LanguageSelected?.Invoke("en"); };
        langRow.Controls.Add(langTrBtn, 0, 0);
        langRow.Controls.Add(langEnBtn, 1, 0);
        Localize(() =>
        {
            langTrBtn.SetActive(!Strings.IsEnglish, DarkTheme.Accent);
            langEnBtn.SetActive(Strings.IsEnglish, DarkTheme.Cyan);
        });

        var spacer1 = new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = Color.Transparent };
        var spacer2 = new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = Color.Transparent };
        sidebar.Controls.Add(langRow);
        sidebar.Controls.Add(spacer1);
        sidebar.Controls.Add(feedbackCard);
        sidebar.Controls.Add(spacer2);
        sidebar.Controls.Add(wmiStatusPill);

        // Sağ içerik alanı
        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkTheme.BgWindow,
            Padding = new Padding(22, 18, 22, 18),
        };

        // Sayfa 0: Genel Bakış
        ovLimitBtn = DarkTheme.CreatePillButton("", true, DarkTheme.Green);
        ovLimitBtn.LeadingDotColor = DarkTheme.Green;
        ovLimitBtn.Click += (_, _) => LimitToggled?.Invoke();
        ovKbToggleBtn = DarkTheme.CreatePillButton("", true, DarkTheme.Cyan);
        ovKbToggleBtn.LeadingDotColor = DarkTheme.Cyan;
        ovKbToggleBtn.Click += (_, _) => ToggleKeyboardQuick();
        var overviewPage = BuildOverviewPage();

        // Sayfa 1: Fan ve Performans
        coolBoostToggle = StyledToggle("CoolBoost", Strings.CoolBoostSub, DarkTheme.Cyan);
        Localize(() => { coolBoostToggle.Subtitle = Strings.CoolBoostSub; coolBoostToggle.Invalidate(); });
        coolBoostToggle.CheckedChanged += (_, _) => { if (!suppressEvents) CoolBoostToggled?.Invoke(coolBoostToggle.Checked); };
        fanLivePill = new StatusPill
        {
            Text = "CPU: —  ·  GPU: —",
            DotColor = DarkTheme.Cyan,
            Dock = DockStyle.Bottom,
            Height = 30,
        };
        cpuFanSlider = StyledSlider(0, 100, 50, DarkTheme.Accent);
        gpuFanSlider = StyledSlider(0, 100, 50, DarkTheme.Cyan);
        cpuFanPctLabel = ValueBadge(StatusText.Percent(50), DarkTheme.Accent);
        gpuFanPctLabel = ValueBadge(StatusText.Percent(50), DarkTheme.Cyan);
        syncFansToggle = StyledToggle(Strings.SyncFansText, null, DarkTheme.Cyan);
        Localize(() => syncFansToggle.Text = Strings.SyncFansText);
        syncFansToggle.Checked = true;
        applyCustomFanBtn = DarkTheme.CreateAccentButton(Strings.ApplyCustomFan);
        Localize(() => applyCustomFanBtn.Text = Strings.ApplyCustomFan);
        applyCustomFanBtn.Width = 196;
        applyCustomFanBtn.Click += (_, _) => CustomFanSpeedsApplied?.Invoke(cpuFanSlider.Value, gpuFanSlider.Value);
        cpuFanSlider.ValueChanged += (_, _) =>
        {
            cpuFanPctLabel.Text = StatusText.Percent(cpuFanSlider.Value);
            if (!suppressEvents && syncFansToggle.Checked && gpuFanSlider.Value != cpuFanSlider.Value)
                gpuFanSlider.Value = cpuFanSlider.Value;
        };
        gpuFanSlider.ValueChanged += (_, _) =>
        {
            gpuFanPctLabel.Text = StatusText.Percent(gpuFanSlider.Value);
            if (!suppressEvents && syncFansToggle.Checked && cpuFanSlider.Value != gpuFanSlider.Value)
                cpuFanSlider.Value = gpuFanSlider.Value;
        };
        var fanPage = BuildFanPage();

        // Sayfa 2: Klavye Işığı
        kbBrightnessSlider = StyledSlider(0, 100, 100, DarkTheme.Cyan);
        kbSpeedSlider = StyledSlider(1, 9, 4, DarkTheme.Violet);
        kbBrightnessLabel = ValueBadge(StatusText.Percent(100), DarkTheme.Cyan);
        kbSpeedLabel = ValueBadge("4", DarkTheme.Violet);
        dirRtlBtn = DarkTheme.CreatePillButton(Strings.DirectionRightToLeft);
        dirLtrBtn = DarkTheme.CreatePillButton(Strings.DirectionLeftToRight, true, DarkTheme.Cyan);
        Localize(() =>
        {
            dirRtlBtn.Text = Strings.DirectionRightToLeft;
            dirLtrBtn.Text = Strings.DirectionLeftToRight;
        });
        zonePreview = new KeyboardZonePreview { Dock = DockStyle.Top, Height = 78 };
        zonePreview.ZoneClicked += PickZoneColor;
        applyKeyboardBtn = DarkTheme.CreateAccentButton(Strings.ApplyKeyboard);
        Localize(() => applyKeyboardBtn.Text = Strings.ApplyKeyboard);
        applyKeyboardBtn.Width = 176;
        kbBrightnessSlider.ValueChanged += (_, _) =>
        {
            kbBrightnessLabel.Text = StatusText.Percent(kbBrightnessSlider.Value);
            if (suppressEvents) return;
            editingKeyboard = editingKeyboard with { Brightness = kbBrightnessSlider.Value };
            if (kbBrightnessSlider.Value > 0) lastNonZeroKbBrightness = kbBrightnessSlider.Value;
            zonePreview.SetZones(editingKeyboard.ZoneHexes, editingKeyboard.Brightness);
        };
        kbBrightnessSlider.ValueCommitted += (_, _) =>
        {
            if (suppressEvents) return;
            editingKeyboard = editingKeyboard with { Brightness = kbBrightnessSlider.Value };
            if (kbBrightnessSlider.Value > 0) lastNonZeroKbBrightness = kbBrightnessSlider.Value;
            KeyboardApplied?.Invoke(editingKeyboard);
        };
        kbSpeedSlider.ValueChanged += (_, _) =>
        {
            kbSpeedLabel.Text = kbSpeedSlider.Value.ToString();
            if (suppressEvents) return;
            editingKeyboard = editingKeyboard with { Speed = kbSpeedSlider.Value };
        };
        kbSpeedSlider.ValueCommitted += (_, _) =>
        {
            if (suppressEvents) return;
            editingKeyboard = editingKeyboard with { Speed = kbSpeedSlider.Value };
            KeyboardApplied?.Invoke(editingKeyboard);
        };
        dirRtlBtn.Click += (_, _) =>
        {
            editingKeyboard = editingKeyboard with { Direction = KeyboardDirection.RightToLeft };
            RefreshKeyboardEditorUi();
            KeyboardApplied?.Invoke(editingKeyboard);
        };
        dirLtrBtn.Click += (_, _) =>
        {
            editingKeyboard = editingKeyboard with { Direction = KeyboardDirection.LeftToRight };
            RefreshKeyboardEditorUi();
            KeyboardApplied?.Invoke(editingKeyboard);
        };
        applyKeyboardBtn.Click += (_, _) => KeyboardApplied?.Invoke(editingKeyboard);
        var keyboardPage = BuildKeyboardPage();

        // Sayfa 3: Pil ve Sistem
        sysLimitToggle = StyledToggle(Strings.ToggleLimitTitle, Strings.ToggleLimitSub, DarkTheme.Green);
        Localize(() => { sysLimitToggle.Text = Strings.ToggleLimitTitle; sysLimitToggle.Subtitle = Strings.ToggleLimitSub; sysLimitToggle.Invalidate(); });
        sysLimitToggle.AutoCheck = false;
        sysLimitToggle.Click += (_, _) => LimitToggled?.Invoke();

        kbTimeoutToggle = StyledToggle(Strings.ToggleKbTimeoutTitle, Strings.ToggleKbTimeoutSub, DarkTheme.Cyan);
        Localize(() => { kbTimeoutToggle.Text = Strings.ToggleKbTimeoutTitle; kbTimeoutToggle.Subtitle = Strings.ToggleKbTimeoutSub; kbTimeoutToggle.Invalidate(); });
        kbTimeoutToggle.CheckedChanged += (_, _) => { if (!suppressEvents) KeyboardTimeoutToggled?.Invoke(kbTimeoutToggle.Checked); };

        lcdOverdriveToggle = StyledToggle(Strings.ToggleLcdTitle, Strings.ToggleLcdSub, DarkTheme.Cyan);
        Localize(() => { lcdOverdriveToggle.Text = Strings.ToggleLcdTitle; lcdOverdriveToggle.Invalidate(); });
        lcdOverdriveToggle.CheckedChanged += (_, _) => { if (!suppressEvents) LcdOverdriveToggled?.Invoke(lcdOverdriveToggle.Checked); };

        usbChargingToggle = StyledToggle(Strings.ToggleUsbTitle, Strings.ToggleUsbSub, DarkTheme.Cyan);
        Localize(() => { usbChargingToggle.Text = Strings.ToggleUsbTitle; usbChargingToggle.Subtitle = Strings.ToggleUsbSub; usbChargingToggle.Invalidate(); });
        usbChargingToggle.CheckedChanged += (_, _) => { if (!suppressEvents) UsbChargingToggled?.Invoke(usbChargingToggle.Checked); };

        reapplyWakeToggle = StyledToggle(Strings.ToggleWakeTitle, Strings.ToggleWakeSub, DarkTheme.Green);
        Localize(() => { reapplyWakeToggle.Text = Strings.ToggleWakeTitle; reapplyWakeToggle.Subtitle = Strings.ToggleWakeSub; reapplyWakeToggle.Invalidate(); });
        reapplyWakeToggle.CheckedChanged += (_, _) => { if (!suppressEvents) ReapplyOnWakeToggled?.Invoke(reapplyWakeToggle.Checked); };

        displayBrightnessSlider = StyledSlider(0, 100, 75, DarkTheme.Amber);
        displayBrightnessLabel = ValueBadge(StatusText.Percent(75), DarkTheme.Amber);
        displayBrightnessSlider.Scroll += (_, _) =>
        {
            displayBrightnessLabel.Text = StatusText.Percent(displayBrightnessSlider.Value);
            if (!suppressEvents) DisplayBrightnessChanged?.Invoke(displayBrightnessSlider.Value);
        };
        var systemPage = BuildSystemPage(settingsPath);

        pages = [overviewPage, fanPage, keyboardPage, systemPage];
        foreach (var p in pages)
        {
            p.Dock = DockStyle.Fill;
            p.Visible = false;
            contentHost.Controls.Add(p);
        }

        Controls.Add(contentHost);
        Controls.Add(sidebar);

        ApplyLanguage();
        SelectPage(0);
    }

    private void Localize(Action action) => localizeActions.Add(action);

    public void ApplyLanguage()
    {
        foreach (var act in localizeActions) act();
        zonePreview.Invalidate();
    }

    private Panel BuildBrandHeader()
    {
        var panel = new Panel { BackColor = Color.Transparent };
        panel.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var badgeRect = new RectangleF(0, 6, 36, 36);
            using (var badgePath = DarkTheme.CreateRoundedRect(badgeRect, 10))
            using (var grad = new LinearGradientBrush(badgeRect, DarkTheme.AccentHover, DarkTheme.Blend(DarkTheme.Accent, Color.Black, 0.28f), LinearGradientMode.ForwardDiagonal))
                g.FillPath(grad, badgePath);

            using var pen = new Pen(Color.White, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(pen, [new PointF(11, 31), new PointF(11, 17), new PointF(25, 31), new PointF(25, 17)]);

            using var sepBrush = new LinearGradientBrush(
                new RectangleF(0, 52, panel.Width, 1),
                DarkTheme.Border,
                Color.Transparent,
                LinearGradientMode.Horizontal);
            g.FillRectangle(sepBrush, 0, 52, panel.Width, 1);
        };

        var titleLbl = new Label
        {
            Font = new Font(DarkTheme.FontTitle.FontFamily, 11.5f, FontStyle.Bold),
            ForeColor = DarkTheme.TextPrimary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(46, 7),
        };
        var subLbl = new Label
        {
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.TextSecondary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(47, 27),
        };
        Localize(() =>
        {
            titleLbl.Text = Strings.AppName.ToUpperInvariant();
            subLbl.Text = Strings.AppSubtitle;
        });
        panel.Controls.Add(titleLbl);
        panel.Controls.Add(subLbl);
        return panel;
    }

    private Panel PageHeader(Func<string> titleFn, Func<string> subtitleFn)
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 10),
        };
        var t = new Label
        {
            Font = DarkTheme.FontPageTitle,
            ForeColor = DarkTheme.TextPrimary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, 0),
        };
        var s = new Label
        {
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.TextSecondary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(1, 30),
        };
        Localize(() => { t.Text = titleFn(); s.Text = subtitleFn(); });
        header.Controls.Add(t);
        header.Controls.Add(s);
        return header;
    }

    private RoundedCard LocalizedCard(Func<string> titleFn, Color? accentBar = null)
    {
        var card = DarkTheme.CreateCard(titleFn(), accentBar: accentBar);
        Localize(() => { card.Title = titleFn(); card.Invalidate(); });
        return card;
    }

    private Label LocalizedCaption(Func<string> textFn)
    {
        var lbl = CaptionLabel(textFn());
        Localize(() => lbl.Text = textFn());
        return lbl;
    }

    private Panel BuildOverviewPage()
    {
        var page = new Panel { BackColor = DarkTheme.BgWindow };
        var header = PageHeader(() => Strings.NavOverview, () => Strings.OverviewSubtitle);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 264,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        grid.Controls.Add(MetricCard(() => Strings.MetricCpu, ovCpuTemp, ovCpuSub, ovCpuBar, DarkTheme.Cyan), 0, 0);
        grid.Controls.Add(MetricCard(() => Strings.MetricGpu, ovGpuTemp, ovGpuSub, ovGpuBar, DarkTheme.Violet), 1, 0);
        grid.Controls.Add(MetricCard(() => Strings.MetricRam, ovRamVal, ovRamSub, ovRamBar, DarkTheme.Amber), 0, 1);
        grid.Controls.Add(MetricCard(() => Strings.MetricBattery, ovBatVal, ovBatSub, ovBatBar, DarkTheme.Green), 1, 1);

        var quickCard = LocalizedCard(() => Strings.CardQuickControls);
        quickCard.Dock = DockStyle.Fill;

        var quickLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(0, 26, 0, 0),
            BackColor = Color.Transparent,
        };
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        quickLayout.Controls.Add(LocalizedCaption(() => Strings.MenuProfile), 0, 0);
        quickLayout.Controls.Add(BuildProfileRow(ovProfileBtns), 0, 1);
        quickLayout.Controls.Add(LocalizedCaption(() => Strings.MenuFanMode), 0, 2);
        quickLayout.Controls.Add(BuildFanModeRow(ovFanBtns), 0, 3);

        var togglesRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        togglesRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        togglesRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ovLimitBtn.Dock = DockStyle.Fill;
        ovLimitBtn.Margin = new Padding(0, 0, 6, 0);
        ovKbToggleBtn.Dock = DockStyle.Fill;
        ovKbToggleBtn.Margin = new Padding(6, 0, 0, 0);
        togglesRow.Controls.Add(ovLimitBtn, 0, 0);
        togglesRow.Controls.Add(ovKbToggleBtn, 1, 0);
        quickLayout.Controls.Add(togglesRow, 0, 5);

        quickCard.Controls.Add(quickLayout);

        var spacer = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Color.Transparent };
        page.Controls.Add(quickCard);
        page.Controls.Add(spacer);
        page.Controls.Add(grid);
        page.Controls.Add(header);
        return page;
    }

    private Panel BuildFanPage()
    {
        var page = new Panel { BackColor = DarkTheme.BgWindow, AutoScroll = true };
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Performans Profili
        var profCard = LocalizedCard(() => Strings.MenuProfile, accentBar: DarkTheme.Accent);
        profCard.Dock = DockStyle.Top;
        profCard.Height = 120;
        var profInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0), BackColor = Color.Transparent };
        var profRow = BuildProfileRow(fanProfileBtns);
        profRow.Dock = DockStyle.Top;
        profRow.Height = 38;
        var profHint = new Label
        {
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.TextSecondary,
            BackColor = Color.Transparent,
            Dock = DockStyle.Bottom,
            Height = 20,
        };
        Localize(() => profHint.Text = Strings.ProfileHint);
        profInner.Controls.Add(profRow);
        profInner.Controls.Add(profHint);
        profCard.Controls.Add(profInner);

        // 2. Fan Modu & CoolBoost
        var fanCard = LocalizedCard(() => Strings.CardFanControl, accentBar: DarkTheme.Cyan);
        fanCard.Dock = DockStyle.Top;
        fanCard.Height = 178;
        var fanInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0), BackColor = Color.Transparent };
        var fanRow = BuildFanModeRow(fanModeBtns);
        fanRow.Dock = DockStyle.Top;
        fanRow.Height = 38;
        coolBoostToggle.Dock = DockStyle.Top;
        coolBoostToggle.Height = 44;
        coolBoostToggle.Margin = new Padding(0, 8, 0, 6);
        var fanMidSpacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };
        fanInner.Controls.Add(fanLivePill);
        fanInner.Controls.Add(coolBoostToggle);
        fanInner.Controls.Add(fanMidSpacer);
        fanInner.Controls.Add(fanRow);
        fanCard.Controls.Add(fanInner);

        // 3. Özel Fan Hızı
        var customCard = LocalizedCard(() => Strings.CardCustomFan);
        customCard.Dock = DockStyle.Top;
        customCard.Height = 186;
        var customGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.Transparent,
        };
        customGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        customGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        customGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        customGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        customGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        customGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        customGrid.Controls.Add(LocalizedCaption(() => Strings.LabelCpuFan), 0, 0);
        customGrid.Controls.Add(cpuFanSlider, 1, 0);
        customGrid.Controls.Add(cpuFanPctLabel, 2, 0);

        customGrid.Controls.Add(LocalizedCaption(() => Strings.LabelGpuFan), 0, 1);
        customGrid.Controls.Add(gpuFanSlider, 1, 1);
        customGrid.Controls.Add(gpuFanPctLabel, 2, 1);

        var bottomBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        syncFansToggle.Dock = DockStyle.Left;
        syncFansToggle.Width = 280;
        applyCustomFanBtn.Dock = DockStyle.Right;
        bottomBar.Controls.Add(syncFansToggle);
        bottomBar.Controls.Add(applyCustomFanBtn);
        customGrid.Controls.Add(bottomBar, 0, 2);
        customGrid.SetColumnSpan(bottomBar, 3);
        customCard.Controls.Add(customGrid);

        // 4. Windows Güç Planı
        var powerCard = LocalizedCard(() => Strings.CardPowerPlan);
        powerCard.Dock = DockStyle.Top;
        powerCard.Height = 100;
        var powerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
            BackColor = Color.Transparent,
        };
        powerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        powerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        powerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));

        (WindowsPowerPlan Plan, Func<string> LabelFn)[] plans =
        [
            (WindowsPowerPlan.PowerSaver, () => Strings.PowerPlanSaver),
            (WindowsPowerPlan.Balanced, () => Strings.PowerPlanBalanced),
            (WindowsPowerPlan.HighPerformance, () => Strings.PowerPlanHigh),
        ];
        for (var i = 0; i < plans.Length; i++)
        {
            var plan = plans[i].Plan;
            var labelFn = plans[i].LabelFn;
            var btn = DarkTheme.CreatePillButton(labelFn(), false, DarkTheme.Cyan);
            Localize(() => btn.Text = labelFn());
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 4, 0, i == plans.Length - 1 ? 0 : 4, 0);
            btn.Click += (_, _) =>
            {
                foreach (var kv in powerPlanBtns) kv.Value.SetActive(kv.Key == plan, DarkTheme.Cyan);
                WindowsPowerPlanSelected?.Invoke(plan);
            };
            powerPlanBtns[plan] = btn;
            powerRow.Controls.Add(btn, i, 0);
        }
        powerCard.Controls.Add(powerRow);

        stack.Controls.Add(PageHeader(() => Strings.NavFan, () => Strings.FanSubtitle));
        stack.Controls.Add(profCard);
        stack.Controls.Add(fanCard);
        stack.Controls.Add(customCard);
        stack.Controls.Add(powerCard);
        page.Controls.Add(stack);
        return page;
    }

    private Panel BuildKeyboardPage()
    {
        var page = new Panel { BackColor = DarkTheme.BgWindow, AutoScroll = true };
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. 4 Bölgeli Canlı Önizleme
        var zoneCard = LocalizedCard(() => Strings.CardZonePreview, accentBar: DarkTheme.Violet);
        zoneCard.Dock = DockStyle.Top;
        zoneCard.Height = 178;
        var zoneWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0), BackColor = Color.Transparent };

        var zoneActions = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = Color.Transparent };
        var pickAllBtn = DarkTheme.CreatePillButton(Strings.PickAllZones);
        Localize(() => pickAllBtn.Text = Strings.PickAllZones);
        pickAllBtn.Width = 236;
        pickAllBtn.Dock = DockStyle.Left;
        pickAllBtn.Click += (_, _) => PickAllZonesColor();
        applyKeyboardBtn.Dock = DockStyle.Right;
        zoneActions.Controls.Add(pickAllBtn);
        zoneActions.Controls.Add(applyKeyboardBtn);

        zoneWrapper.Controls.Add(zoneActions);
        zoneWrapper.Controls.Add(zonePreview);
        zoneCard.Controls.Add(zoneWrapper);

        // 2. Efekt Seçici
        var effectCard = LocalizedCard(() => Strings.CardEffects);
        effectCard.Dock = DockStyle.Top;
        effectCard.Height = 104;
        var effRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 6,
            BackColor = Color.Transparent,
        };
        var effects = Enum.GetValues<KeyboardEffect>();
        for (var i = 0; i < effects.Length; i++)
        {
            effRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / effects.Length));
            var eff = effects[i];
            var btn = DarkTheme.CreatePillButton(StatusText.EffectName(eff));
            Localize(() => btn.Text = StatusText.EffectName(eff));
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 3, 0, i == effects.Length - 1 ? 0 : 3, 0);
            btn.Click += (_, _) =>
            {
                editingKeyboard = editingKeyboard with { Effect = eff };
                RefreshKeyboardEditorUi();
                KeyboardApplied?.Invoke(editingKeyboard);
            };
            effectBtns[eff] = btn;
            effRow.Controls.Add(btn, i, 0);
        }
        effectCard.Controls.Add(effRow);

        // 3. Parlaklık, Hız ve Yön
        var ctrlCard = LocalizedCard(() => Strings.CardKbControls);
        ctrlCard.Dock = DockStyle.Top;
        ctrlCard.Height = 176;
        var ctrlGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 4,
            RowCount = 3,
            BackColor = Color.Transparent,
        };
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 198));
        ctrlGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        ctrlGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        ctrlGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        ctrlGrid.Controls.Add(LocalizedCaption(() => Strings.LabelBrightness), 0, 0);
        ctrlGrid.Controls.Add(kbBrightnessSlider, 1, 0);
        ctrlGrid.Controls.Add(kbBrightnessLabel, 2, 0);

        var quickBright = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
        for (var i = 0; i < 3; i++) quickBright.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        (string Text, int Val)[] bSteps = [("0%", 0), ("50%", 50), ("100%", 100)];
        for (var i = 0; i < bSteps.Length; i++)
        {
            var val = bSteps[i].Val;
            var b = DarkTheme.CreatePillButton(bSteps[i].Text);
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(2, 2, 2, 2);
            b.Click += (_, _) =>
            {
                editingKeyboard = editingKeyboard with { Brightness = val };
                if (val > 0) lastNonZeroKbBrightness = val;
                RefreshKeyboardEditorUi();
                KeyboardApplied?.Invoke(editingKeyboard);
            };
            quickBright.Controls.Add(b, i, 0);
        }
        ctrlGrid.Controls.Add(quickBright, 3, 0);

        ctrlGrid.Controls.Add(LocalizedCaption(() => Strings.LabelSpeed), 0, 1);
        ctrlGrid.Controls.Add(kbSpeedSlider, 1, 1);
        ctrlGrid.Controls.Add(kbSpeedLabel, 2, 1);
        var speedNote = SubLabel(Strings.SpeedHint);
        Localize(() => speedNote.Text = Strings.SpeedHint);
        speedNote.TextAlign = ContentAlignment.MiddleLeft;
        speedNote.Dock = DockStyle.Fill;
        ctrlGrid.Controls.Add(speedNote, 3, 1);

        ctrlGrid.Controls.Add(LocalizedCaption(() => Strings.LabelDirection), 0, 2);
        var dirRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        dirRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dirRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dirLtrBtn.Dock = DockStyle.Fill;
        dirLtrBtn.Margin = new Padding(0, 2, 4, 2);
        dirRtlBtn.Dock = DockStyle.Fill;
        dirRtlBtn.Margin = new Padding(4, 2, 0, 2);
        dirRow.Controls.Add(dirLtrBtn, 0, 0);
        dirRow.Controls.Add(dirRtlBtn, 1, 0);
        ctrlGrid.Controls.Add(dirRow, 1, 2);
        ctrlGrid.SetColumnSpan(dirRow, 3);
        ctrlCard.Controls.Add(ctrlGrid);

        // 4. Hazır Temalar
        var presetCard = LocalizedCard(() => Strings.CardPresets);
        presetCard.Dock = DockStyle.Top;
        presetCard.Height = 136;
        var presetGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
        };
        for (var c = 0; c < 4; c++) presetGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var r = 0; r < 2; r++) presetGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        for (var i = 0; i < KeyboardPresets.All.Count && i < 8; i++)
        {
            var preset = KeyboardPresets.All[i];
            var swatch = KeyboardCodec.ParseHexColor(preset.Settings.Zone1Hex, DarkTheme.Accent);
            var btn = DarkTheme.CreatePillButton(preset.DisplayName);
            Localize(() => btn.Text = preset.DisplayName);
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(4);
            btn.LeadingDotColor = preset.Settings.Brightness == 0 ? DarkTheme.TextMuted : swatch;
            btn.Click += (_, _) =>
            {
                editingKeyboard = preset.Settings;
                if (editingKeyboard.Brightness > 0) lastNonZeroKbBrightness = editingKeyboard.Brightness;
                RefreshKeyboardEditorUi();
                KeyboardApplied?.Invoke(editingKeyboard);
            };
            presetGrid.Controls.Add(btn, i % 4, i / 4);
        }
        presetCard.Controls.Add(presetGrid);

        stack.Controls.Add(PageHeader(() => Strings.NavKeyboard, () => Strings.KeyboardSubtitle));
        stack.Controls.Add(zoneCard);
        stack.Controls.Add(effectCard);
        stack.Controls.Add(ctrlCard);
        stack.Controls.Add(presetCard);
        page.Controls.Add(stack);
        return page;
    }

    private Panel BuildSystemPage(string settingsPath)
    {
        var page = new Panel { BackColor = DarkTheme.BgWindow, AutoScroll = true };
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Pil Detayları
        var batCard = LocalizedCard(() => Strings.CardBatteryDetails, accentBar: DarkTheme.Green);
        batCard.Dock = DockStyle.Top;
        batCard.Height = 232;
        var batGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 4,
            RowCount = 5,
            BackColor = Color.Transparent,
        };
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var r = 0; r < 4; r++) batGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        batGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        void AddPair(int row, int colPair, Func<string> capFn, Label val)
        {
            batGrid.Controls.Add(LocalizedCaption(capFn), colPair * 2, row);
            batGrid.Controls.Add(val, colPair * 2 + 1, row);
        }
        AddPair(0, 0, () => Strings.CapCharge, batPct);
        AddPair(0, 1, () => Strings.LabelState, batState);
        AddPair(1, 0, () => Strings.LabelHealth, batHealth);
        AddPair(1, 1, () => Strings.CapCapacity, batCap);
        AddPair(2, 0, () => Strings.CapCycles, batCycles);
        AddPair(2, 1, () => Strings.LabelTemperature, batTemp);
        AddPair(3, 0, () => Strings.CapLimitStatus, batLimit);

        sysLimitToggle.Dock = DockStyle.Fill;
        sysLimitToggle.Margin = new Padding(0, 6, 0, 0);
        batGrid.Controls.Add(sysLimitToggle, 0, 4);
        batGrid.SetColumnSpan(sysLimitToggle, 4);
        batCard.Controls.Add(batGrid);

        // 2. Ekran Parlaklığı
        var dispCard = LocalizedCard(() => Strings.CardDisplayBrightness, accentBar: DarkTheme.Amber);
        dispCard.Dock = DockStyle.Top;
        dispCard.Height = 100;
        var dispGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
            BackColor = Color.Transparent,
        };
        dispGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        dispGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dispGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        dispGrid.Controls.Add(LocalizedCaption(() => Strings.LabelBrightness), 0, 0);
        dispGrid.Controls.Add(displayBrightnessSlider, 1, 0);
        dispGrid.Controls.Add(displayBrightnessLabel, 2, 0);
        dispCard.Controls.Add(dispGrid);

        // 3. Donanım ve Davranış
        var hwCard = LocalizedCard(() => Strings.CardHardwareOptions);
        hwCard.Dock = DockStyle.Top;
        hwCard.Height = 236;
        var hwGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
        };
        hwGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var r = 0; r < 4; r++) hwGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        kbTimeoutToggle.Dock = DockStyle.Fill;
        usbChargingToggle.Dock = DockStyle.Fill;
        lcdOverdriveToggle.Dock = DockStyle.Fill;
        reapplyWakeToggle.Dock = DockStyle.Fill;
        hwGrid.Controls.Add(kbTimeoutToggle, 0, 0);
        hwGrid.Controls.Add(usbChargingToggle, 0, 1);
        hwGrid.Controls.Add(lcdOverdriveToggle, 0, 2);
        hwGrid.Controls.Add(reapplyWakeToggle, 0, 3);
        hwCard.Controls.Add(hwGrid);

        // 4. Tanılama
        var diagCard = LocalizedCard(() => Strings.CardDiagnostics);
        diagCard.Dock = DockStyle.Top;
        diagCard.Height = 154;
        var diagGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.Transparent,
        };
        diagGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        diagGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        diagGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        for (var r = 0; r < 3; r++) diagGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        Localize(() => diagAdminLabel.Text = isAdmin ? Strings.AdminYes : Strings.AdminNo);
        diagAdminLabel.ForeColor = isAdmin ? DarkTheme.Green : DarkTheme.Amber;
        diagWmiLabel.Text = Strings.WmiChecking;
        diagPathLabel.Text = settingsPath;
        diagPathLabel.Font = DarkTheme.FontSmall;

        var openFolderBtn = DarkTheme.CreatePillButton(Strings.BtnOpenFolder);
        Localize(() => openFolderBtn.Text = Strings.BtnOpenFolder);
        openFolderBtn.Dock = DockStyle.Fill;
        openFolderBtn.Margin = new Padding(6, 2, 0, 2);
        openFolderBtn.Click += (_, _) =>
        {
            var dir = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
        };
        var openLogBtn = DarkTheme.CreatePillButton(Strings.BtnOpenLog);
        Localize(() => openLogBtn.Text = Strings.BtnOpenLog);
        openLogBtn.Dock = DockStyle.Fill;
        openLogBtn.Margin = new Padding(6, 2, 0, 2);
        openLogBtn.Click += (_, _) =>
        {
            if (File.Exists(DiagLog.LogPath))
                Process.Start(new ProcessStartInfo(DiagLog.LogPath) { UseShellExecute = true });
            else
                ShowFeedback(Strings.NoLogYet, isError: true);
        };

        diagGrid.Controls.Add(LocalizedCaption(() => Strings.CapPrivileges), 0, 0);
        diagGrid.Controls.Add(diagAdminLabel, 1, 0);
        diagGrid.Controls.Add(openLogBtn, 2, 0);
        diagGrid.Controls.Add(CaptionLabel("Acer WMI"), 0, 1);
        diagGrid.Controls.Add(diagWmiLabel, 1, 1);
        diagGrid.Controls.Add(LocalizedCaption(() => Strings.CapSettingsFile), 0, 2);
        diagGrid.Controls.Add(diagPathLabel, 1, 2);
        diagGrid.Controls.Add(openFolderBtn, 2, 2);
        diagCard.Controls.Add(diagGrid);

        stack.Controls.Add(PageHeader(() => Strings.NavSystem, () => Strings.SystemSubtitle));
        stack.Controls.Add(batCard);
        stack.Controls.Add(dispCard);
        stack.Controls.Add(hwCard);
        stack.Controls.Add(diagCard);
        page.Controls.Add(stack);
        return page;
    }

    private TableLayoutPanel BuildProfileRow(Dictionary<PerformanceProfile, ModernButton> map)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Color.Transparent };
        PerformanceProfile[] profiles =
        [
            PerformanceProfile.Quiet,
            PerformanceProfile.Balanced,
            PerformanceProfile.Performance,
            PerformanceProfile.Turbo,
        ];
        for (var i = 0; i < profiles.Length; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            var profile = profiles[i];
            var btn = DarkTheme.CreatePillButton(StatusText.ProfileName(profile));
            Localize(() => btn.Text = StatusText.ProfileName(profile));
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 4, 0, i == profiles.Length - 1 ? 0 : 4, 0);
            btn.Click += (_, _) => PerformanceProfileSelected?.Invoke(profile);
            map[profile] = btn;
            row.Controls.Add(btn, i, 0);
        }
        return row;
    }

    private TableLayoutPanel BuildFanModeRow(Dictionary<FanMode, ModernButton> map)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
        FanMode[] modes = [FanMode.Auto, FanMode.Max, FanMode.Custom];
        for (var i = 0; i < modes.Length; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            var mode = modes[i];
            var btn = DarkTheme.CreatePillButton(StatusText.FanModeName(mode));
            Localize(() => btn.Text = StatusText.FanModeName(mode));
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 4, 0, i == modes.Length - 1 ? 0 : 4, 0);
            btn.Click += (_, _) => FanModeSelected?.Invoke(mode);
            map[mode] = btn;
            row.Controls.Add(btn, i, 0);
        }
        return row;
    }

    private RoundedCard MetricCard(Func<string> titleFn, Label bigValue, Label subValue, SmoothProgressBar? bar, Color accent)
    {
        var card = DarkTheme.CreateCard(padding: new Padding(16, 13, 16, 14), accentBar: accent);
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(5);

        var cap = new Label
        {
            Font = DarkTheme.FontSmallBold,
            ForeColor = DarkTheme.TextSecondary,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 18,
        };
        Localize(() => cap.Text = titleFn());
        bigValue.Dock = DockStyle.Top;
        bigValue.Height = 40;
        subValue.Dock = DockStyle.Top;
        subValue.Height = 22;

        if (bar is not null) card.Controls.Add(bar);
        card.Controls.Add(subValue);
        card.Controls.Add(bigValue);
        card.Controls.Add(cap);
        return card;
    }

    private void SelectPage(int index)
    {
        for (var i = 0; i < pages.Length; i++)
        {
            var active = i == index;
            pages[i].Visible = active;
            navButtons[i].IsSelected = active;
        }
    }

    private void PickZoneColor(int zoneIndex)
    {
        var current = KeyboardCodec.ParseHexColor(editingKeyboard.ZoneHexes[zoneIndex], Color.Red);
        using var dialog = new ColorDialog { Color = current, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var hex = KeyboardCodec.ToHex(dialog.Color);
        editingKeyboard = editingKeyboard.WithZoneHex(zoneIndex, hex);
        if (editingKeyboard.Effect != KeyboardEffect.Static)
            editingKeyboard = editingKeyboard with { EffectColorHex = hex };
        if (editingKeyboard.Brightness == 0)
            editingKeyboard = editingKeyboard with { Brightness = lastNonZeroKbBrightness };

        RefreshKeyboardEditorUi();
        KeyboardApplied?.Invoke(editingKeyboard);
    }

    private void PickAllZonesColor()
    {
        var current = KeyboardCodec.ParseHexColor(editingKeyboard.Zone1Hex, Color.Red);
        using var dialog = new ColorDialog { Color = current, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var hex = KeyboardCodec.ToHex(dialog.Color);
        editingKeyboard = editingKeyboard.WithAllZones(hex);
        if (editingKeyboard.Brightness == 0)
            editingKeyboard = editingKeyboard with { Brightness = lastNonZeroKbBrightness };

        RefreshKeyboardEditorUi();
        KeyboardApplied?.Invoke(editingKeyboard);
    }

    private void ToggleKeyboardQuick()
    {
        if (editingKeyboard.Brightness > 0)
        {
            lastNonZeroKbBrightness = editingKeyboard.Brightness;
            editingKeyboard = editingKeyboard with { Brightness = 0 };
        }
        else
        {
            editingKeyboard = editingKeyboard with { Brightness = Math.Clamp(lastNonZeroKbBrightness, 20, 100) };
        }
        RefreshKeyboardEditorUi();
        KeyboardApplied?.Invoke(editingKeyboard);
    }

    public void UpdateView(
        AppSettings settings,
        BatterySnapshot? battery,
        ChargeLimitState? actualLimit,
        SystemTelemetry? telemetry,
        int? displayBrightness)
    {
        suppressEvents = true;
        try
        {
            // Genel Bakış Kartları
            ovCpuTemp.Text = telemetry?.CpuTempC is int ct ? $"{ct} °C" : StatusText.Percent(telemetry?.CpuUsagePercent);
            ovCpuTemp.ForeColor = DarkTheme.ThermalColor(telemetry?.CpuTempC, DarkTheme.TextPrimary);
            ovCpuSub.Text = $"{Strings.UsageLabel}: {StatusText.Percent(telemetry?.CpuUsagePercent)}  ·  {Strings.FanLabel}: {StatusText.Rpm(telemetry?.CpuFanRpm)}";
            ovCpuBar.Value = telemetry?.CpuUsagePercent ?? 0;
            ovCpuBar.FillColor = DarkTheme.ThermalColor(telemetry?.CpuTempC, DarkTheme.Cyan);

            ovGpuTemp.Text = telemetry?.GpuTempC is int gt
                ? $"{gt} °C"
                : (telemetry?.GamingWmiAvailable == true ? Strings.GpuSleep : Strings.Unknown);
            ovGpuTemp.ForeColor = telemetry?.GpuTempC is int ? DarkTheme.ThermalColor(telemetry.GpuTempC, DarkTheme.TextPrimary) : DarkTheme.TextSecondary;
            ovGpuSub.Text = $"{Strings.FanLabel}: {StatusText.Rpm(telemetry?.GpuFanRpm)}  ·  {Strings.ModeLabel}: {StatusText.FanModeName(settings.FanMode)}";
            ovGpuBar.Value = telemetry?.GpuTempC is int gTemp ? Math.Clamp(gTemp, 0, 100) : 0;
            ovGpuBar.FillColor = DarkTheme.ThermalColor(telemetry?.GpuTempC, DarkTheme.Violet);

            ovRamVal.Text = StatusText.Percent(telemetry?.RamUsagePercent);
            ovRamSub.Text = StatusText.Ram(telemetry?.RamUsedGb, telemetry?.RamTotalGb, telemetry?.RamUsagePercent);
            ovRamBar.Value = telemetry?.RamUsagePercent ?? 0;

            ovBatVal.Text = StatusText.Percent(battery?.ChargePercent);
            ovBatSub.Text = battery is null
                ? Strings.Unknown
                : $"{StatusText.Power(battery)}  ·  {Strings.LabelHealth} {StatusText.Percent(battery.HealthPercent)}";
            ovBatBar.Value = battery?.ChargePercent ?? 0;
            ovBatBar.FillColor = actualLimit == ChargeLimitState.On ? DarkTheme.Green : DarkTheme.Cyan;

            // Hızlı & Fan Profilleri
            foreach (var kv in ovProfileBtns) kv.Value.SetActive(kv.Key == settings.PerformanceProfile, DarkTheme.Accent);
            foreach (var kv in fanProfileBtns) kv.Value.SetActive(kv.Key == settings.PerformanceProfile, DarkTheme.Accent);
            foreach (var kv in ovFanBtns) kv.Value.SetActive(kv.Key == settings.FanMode, DarkTheme.Cyan);
            foreach (var kv in fanModeBtns) kv.Value.SetActive(kv.Key == settings.FanMode, DarkTheme.Cyan);

            ovLimitBtn.Text = $"{Strings.QuickLimitPrefix}: {(settings.ChargeLimitEnabled ? Strings.LimitOn : Strings.LimitOff)}";
            ovLimitBtn.LeadingDotColor = settings.ChargeLimitEnabled ? Color.White : DarkTheme.TextMuted;
            ovLimitBtn.SetActive(settings.ChargeLimitEnabled, DarkTheme.Green);

            var kb = settings.EffectiveKeyboard;
            var kbDragging = kbBrightnessSlider.IsDragging || kbSpeedSlider.IsDragging;
            if (!kbDragging)
            {
                editingKeyboard = kb;
                if (kb.Brightness > 0) lastNonZeroKbBrightness = kb.Brightness;
            }
            ovKbToggleBtn.Text = kb.Brightness > 0
                ? $"{Strings.QuickKbPrefix}: {StatusText.Percent(kb.Brightness)}"
                : $"{Strings.QuickKbPrefix}: {Strings.LimitOff}";
            ovKbToggleBtn.LeadingDotColor = kb.Brightness > 0 ? Color.White : DarkTheme.TextMuted;
            ovKbToggleBtn.SetActive(kb.Brightness > 0, DarkTheme.Cyan);

            // Fan detayları
            coolBoostToggle.Checked = settings.CoolBoostEnabled;
            if (!cpuFanSlider.IsDragging && !gpuFanSlider.IsDragging)
            {
                cpuFanSlider.Value = Math.Clamp(settings.CpuFanPercent, 0, 100);
                gpuFanSlider.Value = Math.Clamp(settings.GpuFanPercent, 0, 100);
                cpuFanPctLabel.Text = StatusText.Percent(cpuFanSlider.Value);
                gpuFanPctLabel.Text = StatusText.Percent(gpuFanSlider.Value);
            }
            fanLivePill.Text =
                $"CPU: {StatusText.WholeTemperature(telemetry?.CpuTempC)} ({StatusText.Rpm(telemetry?.CpuFanRpm)})   ·   " +
                $"GPU: {StatusText.WholeTemperature(telemetry?.GpuTempC)} ({StatusText.Rpm(telemetry?.GpuFanRpm)})";
            fanLivePill.DotColor = DarkTheme.ThermalColor(telemetry?.CpuTempC, DarkTheme.Cyan);

            // Klavye editörü
            if (!kbDragging) RefreshKeyboardEditorUi();

            // Pil & Sistem
            batPct.Text = StatusText.Percent(battery?.ChargePercent);
            batState.Text = battery is null ? Strings.Unknown : StatusText.Power(battery);
            batHealth.Text = StatusText.Percent(battery?.HealthPercent);
            batCap.Text = battery is { FullChargedCapacity: uint f, DesignedCapacity: uint d }
                ? $"{StatusText.CapacityMwh(f)} / {StatusText.CapacityMwh(d)}"
                : Strings.Unknown;
            batCycles.Text = StatusText.Count(battery?.CycleCount);
            batTemp.Text = StatusText.Temperature(battery?.TemperatureC);
            batLimit.Text = StatusText.Limit(actualLimit);
            sysLimitToggle.Checked = settings.ChargeLimitEnabled;

            kbTimeoutToggle.Checked = settings.KeyboardTimeoutEnabled;
            var lcdSupported = telemetry?.LcdOverdriveSupported ?? true;
            lcdOverdriveToggle.Enabled = lcdSupported;
            lcdOverdriveToggle.Subtitle = lcdSupported ? Strings.ToggleLcdSub : Strings.ToggleLcdUnsupported;
            lcdOverdriveToggle.Invalidate();
            lcdOverdriveToggle.Checked = lcdSupported && settings.LcdOverdriveEnabled;
            usbChargingToggle.Checked = settings.UsbChargingEnabled;
            reapplyWakeToggle.Checked = settings.ReapplyOnWake;

            if (displayBrightness is int b && !displayBrightnessSlider.IsDragging)
            {
                displayBrightnessSlider.Value = Math.Clamp(b, 0, 100);
                displayBrightnessLabel.Text = StatusText.Percent(displayBrightnessSlider.Value);
            }

            var wmiOk = telemetry?.GamingWmiAvailable ?? false;
            wmiStatusPill.Text = wmiOk ? Strings.WmiConnected : Strings.WmiNoResponse;
            wmiStatusPill.DotColor = wmiOk ? DarkTheme.Green : DarkTheme.Amber;
            diagWmiLabel.Text = wmiOk ? Strings.WmiAccessible : Strings.WmiInaccessible;
            diagWmiLabel.ForeColor = wmiOk ? DarkTheme.Green : DarkTheme.Amber;
        }
        finally
        {
            suppressEvents = false;
        }
    }

    private void RefreshKeyboardEditorUi()
    {
        foreach (var kv in effectBtns)
            kv.Value.SetActive(kv.Key == editingKeyboard.Effect, DarkTheme.Accent);

        kbBrightnessSlider.Value = Math.Clamp(editingKeyboard.Brightness, 0, 100);
        kbBrightnessLabel.Text = StatusText.Percent(kbBrightnessSlider.Value);

        var animated = editingKeyboard.Effect != KeyboardEffect.Static;
        kbSpeedSlider.Enabled = animated;
        kbSpeedSlider.Value = Math.Clamp(editingKeyboard.Speed, 1, 9);
        kbSpeedLabel.Text = kbSpeedSlider.Value.ToString();

        dirLtrBtn.Enabled = animated;
        dirRtlBtn.Enabled = animated;
        dirLtrBtn.SetActive(editingKeyboard.Direction == KeyboardDirection.LeftToRight, DarkTheme.Cyan);
        dirRtlBtn.SetActive(editingKeyboard.Direction == KeyboardDirection.RightToLeft, DarkTheme.Cyan);

        zonePreview.SetZones(editingKeyboard.ZoneHexes, editingKeyboard.Brightness);
    }

    public void ShowFeedback(string message, bool isError = false)
    {
        feedbackLabel.Text = message;
        feedbackLabel.ForeColor = isError ? DarkTheme.Amber : DarkTheme.Green;
    }

    public void ShowOrFocus()
    {
        if (!Visible)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }
        else if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }
        Activate();
        BringToFront();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    private static Label MetricLabel() => new()
    {
        Text = Strings.Unknown,
        Font = DarkTheme.FontMetric,
        ForeColor = DarkTheme.TextPrimary,
        BackColor = Color.Transparent,
        AutoEllipsis = true,
    };

    private static Label SubLabel(string text = Strings.Unknown) => new()
    {
        Text = text,
        Font = DarkTheme.FontSmall,
        ForeColor = DarkTheme.TextSecondary,
        BackColor = Color.Transparent,
        AutoEllipsis = true,
    };

    private static Label CaptionLabel(string text) => new()
    {
        Text = text,
        Font = DarkTheme.FontBody,
        ForeColor = DarkTheme.TextSecondary,
        BackColor = Color.Transparent,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Anchor = AnchorStyles.Left,
    };

    private static Label DetailVal() => new()
    {
        Text = Strings.Unknown,
        Font = DarkTheme.FontBodyBold,
        ForeColor = DarkTheme.TextPrimary,
        BackColor = Color.Transparent,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
    };

    private static Label ValueBadge(string text, Color? color = null) => new()
    {
        Text = text,
        Font = DarkTheme.FontBodyBold,
        ForeColor = color ?? DarkTheme.Cyan,
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleRight,
        Dock = DockStyle.Fill,
    };

    private static ModernToggleSwitch StyledToggle(string text, string? subtitle, Color accent) => new()
    {
        Text = text,
        Subtitle = subtitle,
        AccentColor = accent,
        Height = string.IsNullOrEmpty(subtitle) ? 34 : 42,
    };

    private static ModernSlider StyledSlider(int min, int max, int value, Color accent) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        AccentColor = accent,
        Dock = DockStyle.Fill,
    };
}
