using System.Diagnostics;
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

/// <summary>Acer Nitro 5 için koyu temalı kapsamlı kontrol paneli. Kapatıldığında tepsiye gizlenir.</summary>
public sealed class DashboardForm : Form
{
    private readonly Panel[] pages;
    private readonly Button[] navButtons;
    private readonly Label feedbackLabel;
    private readonly Label wmiStatusLabel;

    // Genel Bakış
    private readonly Label ovCpuTemp = MetricLabel(), ovCpuSub = SubLabel();
    private readonly SmoothProgressBar ovCpuBar = new() { FillColor = DarkTheme.Cyan, Dock = DockStyle.Bottom };
    private readonly Label ovGpuTemp = MetricLabel(), ovGpuSub = SubLabel();
    private readonly Label ovRamVal = MetricLabel(), ovRamSub = SubLabel();
    private readonly SmoothProgressBar ovRamBar = new() { FillColor = DarkTheme.Amber, Dock = DockStyle.Bottom };
    private readonly Label ovBatVal = MetricLabel(), ovBatSub = SubLabel();
    private readonly SmoothProgressBar ovBatBar = new() { FillColor = DarkTheme.Green, Dock = DockStyle.Bottom };
    private readonly Dictionary<PerformanceProfile, Button> ovProfileBtns = [];
    private readonly Dictionary<FanMode, Button> ovFanBtns = [];
    private readonly Button ovLimitBtn;
    private readonly Button ovKbToggleBtn;

    // Fan & Performans
    private readonly Dictionary<PerformanceProfile, Button> fanProfileBtns = [];
    private readonly Dictionary<FanMode, Button> fanModeBtns = [];
    private readonly CheckBox coolBoostCheck;
    private readonly Label fanLiveSummary = SubLabel();
    private readonly TrackBar cpuFanSlider, gpuFanSlider;
    private readonly Label cpuFanPctLabel, gpuFanPctLabel;
    private readonly CheckBox syncFansCheck;
    private readonly Button applyCustomFanBtn;
    private readonly Dictionary<WindowsPowerPlan, Button> powerPlanBtns = [];

    // Klavye Işığı
    private readonly Dictionary<KeyboardEffect, Button> effectBtns = [];
    private readonly TrackBar kbBrightnessSlider, kbSpeedSlider;
    private readonly Label kbBrightnessLabel, kbSpeedLabel;
    private readonly Button dirRtlBtn, dirLtrBtn;
    private readonly Button[] zoneButtons = new Button[4];
    private readonly Button applyKeyboardBtn;
    private KeyboardLightingSettings editingKeyboard = new();

    // Pil & Sistem
    private readonly Label batPct = DetailVal(), batState = DetailVal(), batHealth = DetailVal(),
        batCap = DetailVal(), batCycles = DetailVal(), batTemp = DetailVal(), batLimit = DetailVal();
    private readonly CheckBox sysLimitCheck, kbTimeoutCheck, lcdOverdriveCheck, usbChargingCheck, reapplyWakeCheck;
    private readonly TrackBar displayBrightnessSlider;
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

    public DashboardForm(string settingsPath)
    {
        Text = $"{Strings.AppName} — {Strings.AppSubtitle}";
        Size = new Size(880, 610);
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = DarkTheme.BgWindow;
        ForeColor = DarkTheme.TextPrimary;
        Font = DarkTheme.FontBody;
        DoubleBuffered = true;

        HandleCreated += (_, _) => DarkTheme.EnableDarkTitleBar(Handle);

        // Sol kenar çubuğu
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = DarkTheme.BgSidebar,
            Padding = new Padding(12, 16, 12, 12),
        };

        var titleLbl = new Label
        {
            Text = "ACER KONTROL",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = DarkTheme.Accent,
            AutoSize = true,
            Location = new Point(14, 16),
        };
        var subLbl = new Label
        {
            Text = Strings.AppSubtitle,
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(15, 42),
        };
        sidebar.Controls.Add(titleLbl);
        sidebar.Controls.Add(subLbl);

        string[] navTitles = ["  Genel Bakış", "  Fan ve Performans", "  Klavye Işığı", "  Pil ve Sistem"];
        navButtons = new Button[navTitles.Length];
        for (var i = 0; i < navTitles.Length; i++)
        {
            var idx = i;
            var btn = new Button
            {
                Text = navTitles[i],
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.FontBodyBold,
                ForeColor = DarkTheme.TextPrimary,
                BackColor = DarkTheme.BgSidebar,
                Cursor = Cursors.Hand,
                Location = new Point(12, 82 + i * 44),
                Size = new Size(176, 38),
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = DarkTheme.BgCard;
            btn.Click += (_, _) => SelectPage(idx);
            sidebar.Controls.Add(btn);
            navButtons[i] = btn;
        }

        wmiStatusLabel = new Label
        {
            Text = "WMI: Kontrol ediliyor...",
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.TextSecondary,
            Dock = DockStyle.Bottom,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        feedbackLabel = new Label
        {
            Text = "Hazır",
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.Green,
            Dock = DockStyle.Bottom,
            Height = 40,
            TextAlign = ContentAlignment.BottomLeft,
        };
        sidebar.Controls.Add(feedbackLabel);
        sidebar.Controls.Add(wmiStatusLabel);

        // Sağ içerik alanı
        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkTheme.BgWindow,
            Padding = new Padding(16),
        };

        // Sayfa 0: Genel Bakış
        ovLimitBtn = DarkTheme.CreatePillButton("Pil Sınırı (%80): Açık", true, DarkTheme.Green);
        ovLimitBtn.Click += (_, _) => LimitToggled?.Invoke();
        ovKbToggleBtn = DarkTheme.CreatePillButton("Klavye Işığı: Açık", true, DarkTheme.Cyan);
        ovKbToggleBtn.Click += (_, _) => ToggleKeyboardQuick();
        var overviewPage = BuildOverviewPage();

        // Sayfa 1: Fan ve Performans
        coolBoostCheck = StyledCheckBox("CoolBoost (Yüksek yükte agresif fan soğutması)");
        coolBoostCheck.CheckedChanged += (_, _) => { if (!suppressEvents) CoolBoostToggled?.Invoke(coolBoostCheck.Checked); };
        cpuFanSlider = StyledTrackBar(0, 100, 50);
        gpuFanSlider = StyledTrackBar(0, 100, 50);
        cpuFanPctLabel = ValueBadge("%50");
        gpuFanPctLabel = ValueBadge("%50");
        syncFansCheck = StyledCheckBox("CPU ve GPU fan hızlarını eşitle");
        syncFansCheck.Checked = true;
        applyCustomFanBtn = DarkTheme.CreateAccentButton("Özel Fan Hızını Uygula");
        applyCustomFanBtn.Width = 190;
        applyCustomFanBtn.Click += (_, _) => CustomFanSpeedsApplied?.Invoke(cpuFanSlider.Value, gpuFanSlider.Value);
        cpuFanSlider.ValueChanged += (_, _) =>
        {
            cpuFanPctLabel.Text = $"%{cpuFanSlider.Value}";
            if (!suppressEvents && syncFansCheck.Checked && gpuFanSlider.Value != cpuFanSlider.Value)
                gpuFanSlider.Value = cpuFanSlider.Value;
        };
        gpuFanSlider.ValueChanged += (_, _) =>
        {
            gpuFanPctLabel.Text = $"%{gpuFanSlider.Value}";
            if (!suppressEvents && syncFansCheck.Checked && cpuFanSlider.Value != gpuFanSlider.Value)
                cpuFanSlider.Value = gpuFanSlider.Value;
        };
        var fanPage = BuildFanPage();

        // Sayfa 2: Klavye Işığı
        kbBrightnessSlider = StyledTrackBar(0, 100, 100);
        kbSpeedSlider = StyledTrackBar(1, 9, 4);
        kbBrightnessLabel = ValueBadge("%100");
        kbSpeedLabel = ValueBadge("4");
        dirRtlBtn = DarkTheme.CreatePillButton(Strings.DirectionRightToLeft);
        dirLtrBtn = DarkTheme.CreatePillButton(Strings.DirectionLeftToRight, true, DarkTheme.Cyan);
        applyKeyboardBtn = DarkTheme.CreateAccentButton("Klavyeye Uygula");
        applyKeyboardBtn.Width = 170;
        kbBrightnessSlider.ValueChanged += (_, _) =>
        {
            kbBrightnessLabel.Text = $"%{kbBrightnessSlider.Value}";
            if (suppressEvents) return;
            editingKeyboard = editingKeyboard with { Brightness = kbBrightnessSlider.Value };
            if (kbBrightnessSlider.Value > 0) lastNonZeroKbBrightness = kbBrightnessSlider.Value;
        };
        kbSpeedSlider.ValueChanged += (_, _) =>
        {
            kbSpeedLabel.Text = kbSpeedSlider.Value.ToString();
            if (suppressEvents) return;
            editingKeyboard = editingKeyboard with { Speed = kbSpeedSlider.Value };
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
        sysLimitCheck = StyledCheckBox("Pil şarjını %80 ile sınırla (pil ömrünü korur)");
        sysLimitCheck.AutoCheck = false;
        sysLimitCheck.Click += (_, _) => LimitToggled?.Invoke();
        kbTimeoutCheck = StyledCheckBox("Klavye ışığını 30 sn hareketsizlikte otomatik kapat");
        kbTimeoutCheck.CheckedChanged += (_, _) => { if (!suppressEvents) KeyboardTimeoutToggled?.Invoke(kbTimeoutCheck.Checked); };
        lcdOverdriveCheck = StyledCheckBox("LCD Overdrive (daha düşük ekran tepki süresi)");
        lcdOverdriveCheck.CheckedChanged += (_, _) => { if (!suppressEvents) LcdOverdriveToggled?.Invoke(lcdOverdriveCheck.Checked); };
        usbChargingCheck = StyledCheckBox("Bilgisayar kapalıyken USB şarjı (Power-Off USB Charging)");
        usbChargingCheck.CheckedChanged += (_, _) => { if (!suppressEvents) UsbChargingToggled?.Invoke(usbChargingCheck.Checked); };
        reapplyWakeCheck = StyledCheckBox("Uykudan uyanınca pil sınırı, fan ve klavye ayarlarını otomatik uygula");
        reapplyWakeCheck.CheckedChanged += (_, _) => { if (!suppressEvents) ReapplyOnWakeToggled?.Invoke(reapplyWakeCheck.Checked); };
        displayBrightnessSlider = StyledTrackBar(0, 100, 75);
        displayBrightnessLabel = ValueBadge("%75");
        displayBrightnessSlider.Scroll += (_, _) =>
        {
            displayBrightnessLabel.Text = $"%{displayBrightnessSlider.Value}";
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

        SelectPage(0);
    }

    private Panel BuildOverviewPage()
    {
        var page = new Panel { BackColor = DarkTheme.BgWindow };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 250,
            ColumnCount = 2,
            RowCount = 2,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        grid.Controls.Add(MetricCard("İŞLEMCİ (CPU)", ovCpuTemp, ovCpuSub, ovCpuBar), 0, 0);
        grid.Controls.Add(MetricCard("EKRAN KARTI (GPU)", ovGpuTemp, ovGpuSub, null), 1, 0);
        grid.Controls.Add(MetricCard("BELLEK (RAM)", ovRamVal, ovRamSub, ovRamBar), 0, 1);
        grid.Controls.Add(MetricCard("PİL DURUMU", ovBatVal, ovBatSub, ovBatBar), 1, 1);

        // Hızlı Kontroller Kartı
        var quickCard = DarkTheme.CreateCard("Hızlı Kontroller");
        quickCard.Dock = DockStyle.Fill;

        var quickLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(0, 26, 0, 0),
        };
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        quickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        quickLayout.Controls.Add(CaptionLabel("Performans Profili"), 0, 0);
        quickLayout.Controls.Add(BuildProfileRow(ovProfileBtns), 0, 1);
        quickLayout.Controls.Add(CaptionLabel("Fan Modu"), 0, 2);
        quickLayout.Controls.Add(BuildFanModeRow(ovFanBtns), 0, 3);

        var togglesRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
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

        var spacer = new Panel { Dock = DockStyle.Top, Height = 12 };
        page.Controls.Add(quickCard);
        page.Controls.Add(spacer);
        page.Controls.Add(grid);
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
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Performans Profili
        var profCard = DarkTheme.CreateCard("Performans Profili");
        profCard.Dock = DockStyle.Top;
        profCard.Height = 112;
        var profInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0) };
        var profRow = BuildProfileRow(fanProfileBtns);
        profRow.Dock = DockStyle.Top;
        profRow.Height = 38;
        var profHint = new Label
        {
            Text = "Sessiz: düşük fan sesi · Dengeli: günlük kullanım · Performans / Turbo: yüksek güç ve soğutma",
            Font = DarkTheme.FontSmall,
            ForeColor = DarkTheme.TextSecondary,
            Dock = DockStyle.Bottom,
            Height = 20,
        };
        profInner.Controls.Add(profRow);
        profInner.Controls.Add(profHint);
        profCard.Controls.Add(profInner);

        // 2. Fan Modu & CoolBoost
        var fanCard = DarkTheme.CreateCard("Fan Kontrolü");
        fanCard.Dock = DockStyle.Top;
        fanCard.Height = 148;
        var fanInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0) };
        var fanRow = BuildFanModeRow(fanModeBtns);
        fanRow.Dock = DockStyle.Top;
        fanRow.Height = 38;
        coolBoostCheck.Dock = DockStyle.Top;
        coolBoostCheck.Padding = new Padding(0, 8, 0, 0);
        fanLiveSummary.Dock = DockStyle.Bottom;
        fanLiveSummary.Height = 22;
        fanInner.Controls.Add(fanLiveSummary);
        fanInner.Controls.Add(coolBoostCheck);
        fanInner.Controls.Add(fanRow);
        fanCard.Controls.Add(fanInner);

        // 3. Özel Fan Hızı
        var customCard = DarkTheme.CreateCard("Özel Fan Hızı (%0 – %100)");
        customCard.Dock = DockStyle.Top;
        customCard.Height = 175;
        var customGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
            RowCount = 3,
        };
        customGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        customGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        customGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
        customGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        customGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        customGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        customGrid.Controls.Add(CaptionLabel("CPU Fanı"), 0, 0);
        customGrid.Controls.Add(cpuFanSlider, 1, 0);
        customGrid.Controls.Add(cpuFanPctLabel, 2, 0);

        customGrid.Controls.Add(CaptionLabel("GPU Fanı"), 0, 1);
        customGrid.Controls.Add(gpuFanSlider, 1, 1);
        customGrid.Controls.Add(gpuFanPctLabel, 2, 1);

        var bottomBar = new Panel { Dock = DockStyle.Fill };
        syncFansCheck.Dock = DockStyle.Left;
        applyCustomFanBtn.Dock = DockStyle.Right;
        bottomBar.Controls.Add(syncFansCheck);
        bottomBar.Controls.Add(applyCustomFanBtn);
        customGrid.Controls.Add(bottomBar, 0, 2);
        customGrid.SetColumnSpan(bottomBar, 3);
        customCard.Controls.Add(customGrid);

        // 4. Windows Güç Planı
        var powerCard = DarkTheme.CreateCard("Windows Güç Planı");
        powerCard.Dock = DockStyle.Top;
        powerCard.Height = 96;
        var powerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
        };
        powerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        powerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        powerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));

        (WindowsPowerPlan Plan, string Label)[] plans =
        [
            (WindowsPowerPlan.PowerSaver, Strings.PowerPlanSaver),
            (WindowsPowerPlan.Balanced, Strings.PowerPlanBalanced),
            (WindowsPowerPlan.HighPerformance, Strings.PowerPlanHigh),
        ];
        for (var i = 0; i < plans.Length; i++)
        {
            var plan = plans[i].Plan;
            var btn = DarkTheme.CreatePillButton(plans[i].Label, false, DarkTheme.Cyan);
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 4, 0, i == plans.Length - 1 ? 0 : 4, 0);
            btn.Click += (_, _) =>
            {
                foreach (var kv in powerPlanBtns) DarkTheme.StylePillButton(kv.Value, kv.Key == plan, DarkTheme.Cyan);
                WindowsPowerPlanSelected?.Invoke(plan);
            };
            powerPlanBtns[plan] = btn;
            powerRow.Controls.Add(btn, i, 0);
        }
        powerCard.Controls.Add(powerRow);

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
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Efekt Seçici
        var effectCard = DarkTheme.CreateCard("Aydınlatma Efekti");
        effectCard.Dock = DockStyle.Top;
        effectCard.Height = 104;
        var effRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 6,
        };
        var effects = Enum.GetValues<KeyboardEffect>();
        for (var i = 0; i < effects.Length; i++)
        {
            effRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / effects.Length));
            var eff = effects[i];
            var btn = DarkTheme.CreatePillButton(StatusText.EffectName(eff));
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

        // 2. Parlaklık, Hız ve Yön
        var ctrlCard = DarkTheme.CreateCard("Parlaklık, Hız ve Yön");
        ctrlCard.Dock = DockStyle.Top;
        ctrlCard.Height = 165;
        var ctrlGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 4,
            RowCount = 3,
        };
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        ctrlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        ctrlGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        ctrlGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        ctrlGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        ctrlGrid.Controls.Add(CaptionLabel("Parlaklık"), 0, 0);
        ctrlGrid.Controls.Add(kbBrightnessSlider, 1, 0);
        ctrlGrid.Controls.Add(kbBrightnessLabel, 2, 0);

        var quickBright = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        for (var i = 0; i < 3; i++) quickBright.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        (string Text, int Val)[] bSteps = [("0%", 0), ("50%", 50), ("100%", 100)];
        for (var i = 0; i < bSteps.Length; i++)
        {
            var val = bSteps[i].Val;
            var b = DarkTheme.CreatePillButton(bSteps[i].Text);
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(2, 1, 2, 1);
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

        ctrlGrid.Controls.Add(CaptionLabel("Hız"), 0, 1);
        ctrlGrid.Controls.Add(kbSpeedSlider, 1, 1);
        ctrlGrid.Controls.Add(kbSpeedLabel, 2, 1);
        ctrlGrid.Controls.Add(SubLabel("Animasyon efektleri için"), 3, 1);

        ctrlGrid.Controls.Add(CaptionLabel("Yön"), 0, 2);
        var dirRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        dirRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dirRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dirLtrBtn.Dock = DockStyle.Fill;
        dirLtrBtn.Margin = new Padding(0, 1, 4, 1);
        dirRtlBtn.Dock = DockStyle.Fill;
        dirRtlBtn.Margin = new Padding(4, 1, 0, 1);
        dirRow.Controls.Add(dirLtrBtn, 0, 0);
        dirRow.Controls.Add(dirRtlBtn, 1, 0);
        ctrlGrid.Controls.Add(dirRow, 1, 2);
        ctrlGrid.SetColumnSpan(dirRow, 3);
        ctrlCard.Controls.Add(ctrlGrid);

        // 3. 4 Bölgeli Renk Seçici
        var zoneCard = DarkTheme.CreateCard("4 Bölgeli Klavye Renkleri (Değiştirmek için bölgeye tıklayın)");
        zoneCard.Dock = DockStyle.Top;
        zoneCard.Height = 148;
        var zoneWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0) };

        var zoneRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 52, ColumnCount = 4 };
        for (var i = 0; i < 4; i++)
        {
            zoneRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            var idx = i;
            var btn = new Button
            {
                Text = $"Bölge {i + 1}",
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.FontBodyBold,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Margin = new Padding(i == 0 ? 0 : 4, 0, i == 3 ? 0 : 4, 0),
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = DarkTheme.Border;
            btn.Click += (_, _) => PickZoneColor(idx);
            zoneButtons[i] = btn;
            zoneRow.Controls.Add(btn, i, 0);
        }

        var zoneActions = new Panel { Dock = DockStyle.Bottom, Height = 36 };
        var pickAllBtn = DarkTheme.CreatePillButton("Tüm Bölgelere Tek Renk Seç...");
        pickAllBtn.Width = 220;
        pickAllBtn.Dock = DockStyle.Left;
        pickAllBtn.Click += (_, _) => PickAllZonesColor();
        applyKeyboardBtn.Dock = DockStyle.Right;
        zoneActions.Controls.Add(pickAllBtn);
        zoneActions.Controls.Add(applyKeyboardBtn);

        zoneWrapper.Controls.Add(zoneActions);
        zoneWrapper.Controls.Add(zoneRow);
        zoneCard.Controls.Add(zoneWrapper);

        // 4. Hazır Temalar
        var presetCard = DarkTheme.CreateCard("Hazır Renk Temaları");
        presetCard.Dock = DockStyle.Top;
        presetCard.Height = 128;
        var presetGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 4,
            RowCount = 2,
        };
        for (var c = 0; c < 4; c++) presetGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var r = 0; r < 2; r++) presetGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        for (var i = 0; i < KeyboardPresets.All.Count && i < 8; i++)
        {
            var preset = KeyboardPresets.All[i];
            var swatch = KeyboardCodec.ParseHexColor(preset.Settings.Zone1Hex, DarkTheme.Accent);
            var btn = DarkTheme.CreatePillButton(preset.Name);
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(3);
            btn.FlatAppearance.BorderColor = preset.Settings.Brightness == 0 ? DarkTheme.Border : swatch;
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

        stack.Controls.Add(effectCard);
        stack.Controls.Add(ctrlCard);
        stack.Controls.Add(zoneCard);
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
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Pil Detayları
        var batCard = DarkTheme.CreateCard("Pil Sağlığı ve Şarj Sınırı");
        batCard.Dock = DockStyle.Top;
        batCard.Height = 205;
        var batGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 4,
            RowCount = 5,
        };
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        batGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        void AddPair(int row, int colPair, string cap, Label val)
        {
            batGrid.Controls.Add(CaptionLabel(cap), colPair * 2, row);
            batGrid.Controls.Add(val, colPair * 2 + 1, row);
        }
        AddPair(0, 0, "Şarj", batPct);
        AddPair(0, 1, "Durum", batState);
        AddPair(1, 0, "Sağlık", batHealth);
        AddPair(1, 1, "Kapasite", batCap);
        AddPair(2, 0, "Döngü", batCycles);
        AddPair(2, 1, "Sıcaklık", batTemp);
        AddPair(3, 0, "Sınır Durumu", batLimit);

        sysLimitCheck.Dock = DockStyle.Fill;
        batGrid.Controls.Add(sysLimitCheck, 0, 4);
        batGrid.SetColumnSpan(sysLimitCheck, 4);
        batCard.Controls.Add(batGrid);

        // 2. Ekran Parlaklığı
        var dispCard = DarkTheme.CreateCard("Ekran Parlaklığı");
        dispCard.Dock = DockStyle.Top;
        dispCard.Height = 96;
        var dispGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
        };
        dispGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        dispGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dispGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
        dispGrid.Controls.Add(CaptionLabel("Parlaklık"), 0, 0);
        dispGrid.Controls.Add(displayBrightnessSlider, 1, 0);
        dispGrid.Controls.Add(displayBrightnessLabel, 2, 0);
        dispCard.Controls.Add(dispGrid);

        // 3. Donanım ve Davranış
        var hwCard = DarkTheme.CreateCard("Donanım Seçenekleri ve Otomasyon");
        hwCard.Dock = DockStyle.Top;
        hwCard.Height = 166;
        var hwStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 26, 0, 0),
        };
        hwStack.Controls.Add(kbTimeoutCheck);
        hwStack.Controls.Add(usbChargingCheck);
        hwStack.Controls.Add(lcdOverdriveCheck);
        hwStack.Controls.Add(reapplyWakeCheck);
        hwCard.Controls.Add(hwStack);

        // 4. Tanılama
        var diagCard = DarkTheme.CreateCard("Sistem ve Tanılama");
        diagCard.Dock = DockStyle.Top;
        diagCard.Height = 138;
        var diagGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 26, 0, 0),
            ColumnCount = 3,
            RowCount = 3,
        };
        diagGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        diagGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        diagGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));

        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        diagAdminLabel.Text = isAdmin ? "Yönetici (Tam Yetki)" : "Standart Kullanıcı (Kısıtlı)";
        diagAdminLabel.ForeColor = isAdmin ? DarkTheme.Green : DarkTheme.Amber;
        diagWmiLabel.Text = "Kontrol ediliyor...";
        diagPathLabel.Text = settingsPath;
        diagPathLabel.Font = DarkTheme.FontSmall;

        var openFolderBtn = DarkTheme.CreatePillButton("Klasörü Aç");
        openFolderBtn.Dock = DockStyle.Fill;
        openFolderBtn.Click += (_, _) =>
        {
            var dir = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
        };
        var openLogBtn = DarkTheme.CreatePillButton("Logu Aç");
        openLogBtn.Dock = DockStyle.Fill;
        openLogBtn.Click += (_, _) =>
        {
            if (File.Exists(DiagLog.LogPath))
                Process.Start(new ProcessStartInfo(DiagLog.LogPath) { UseShellExecute = true });
            else
                ShowFeedback("Henüz log dosyası oluşmadı", isError: true);
        };

        diagGrid.Controls.Add(CaptionLabel("Yetki Durumu"), 0, 0);
        diagGrid.Controls.Add(diagAdminLabel, 1, 0);
        diagGrid.Controls.Add(openLogBtn, 2, 0);
        diagGrid.Controls.Add(CaptionLabel("Acer WMI"), 0, 1);
        diagGrid.Controls.Add(diagWmiLabel, 1, 1);
        diagGrid.Controls.Add(CaptionLabel("Ayar Dosyası"), 0, 2);
        diagGrid.Controls.Add(diagPathLabel, 1, 2);
        diagGrid.Controls.Add(openFolderBtn, 2, 2);
        diagCard.Controls.Add(diagGrid);

        stack.Controls.Add(batCard);
        stack.Controls.Add(dispCard);
        stack.Controls.Add(hwCard);
        stack.Controls.Add(diagCard);
        page.Controls.Add(stack);
        return page;
    }

    private TableLayoutPanel BuildProfileRow(Dictionary<PerformanceProfile, Button> map)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
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
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 4, 0, i == profiles.Length - 1 ? 0 : 4, 0);
            btn.Click += (_, _) => PerformanceProfileSelected?.Invoke(profile);
            map[profile] = btn;
            row.Controls.Add(btn, i, 0);
        }
        return row;
    }

    private TableLayoutPanel BuildFanModeRow(Dictionary<FanMode, Button> map)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        FanMode[] modes = [FanMode.Auto, FanMode.Max, FanMode.Custom];
        for (var i = 0; i < modes.Length; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            var mode = modes[i];
            var btn = DarkTheme.CreatePillButton(StatusText.FanModeName(mode));
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(i == 0 ? 0 : 4, 0, i == modes.Length - 1 ? 0 : 4, 0);
            btn.Click += (_, _) => FanModeSelected?.Invoke(mode);
            map[mode] = btn;
            row.Controls.Add(btn, i, 0);
        }
        return row;
    }

    private static Panel MetricCard(string title, Label bigValue, Label subValue, SmoothProgressBar? bar)
    {
        var card = DarkTheme.CreateCard();
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(4);
        card.Padding = new Padding(14, 10, 14, 12);

        var cap = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = DarkTheme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 18,
        };
        bigValue.Dock = DockStyle.Top;
        bigValue.Height = 36;
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
            navButtons[i].BackColor = active ? DarkTheme.BgCard : DarkTheme.BgSidebar;
            navButtons[i].ForeColor = active ? DarkTheme.TextPrimary : DarkTheme.TextSecondary;
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
            ovCpuSub.Text = $"Kullanım: {StatusText.Percent(telemetry?.CpuUsagePercent)} · Fan: {StatusText.Rpm(telemetry?.CpuFanRpm)}";
            ovCpuBar.Value = telemetry?.CpuUsagePercent ?? 0;

            ovGpuTemp.Text = telemetry?.GpuTempC is int gt
                ? $"{gt} °C"
                : (telemetry?.GamingWmiAvailable == true ? "Uyku (dGPU)" : Strings.Unknown);
            ovGpuSub.Text = $"Fan: {StatusText.Rpm(telemetry?.GpuFanRpm)} · Mod: {StatusText.FanModeName(settings.FanMode)}";

            ovRamVal.Text = StatusText.Percent(telemetry?.RamUsagePercent);
            ovRamSub.Text = StatusText.Ram(telemetry?.RamUsedGb, telemetry?.RamTotalGb, telemetry?.RamUsagePercent);
            ovRamBar.Value = telemetry?.RamUsagePercent ?? 0;

            ovBatVal.Text = StatusText.Percent(battery?.ChargePercent);
            ovBatSub.Text = battery is null
                ? Strings.Unknown
                : $"{StatusText.Power(battery)} · Sağlık {StatusText.Percent(battery.HealthPercent)}";
            ovBatBar.Value = battery?.ChargePercent ?? 0;
            ovBatBar.FillColor = actualLimit == ChargeLimitState.On ? DarkTheme.Green : DarkTheme.Cyan;

            // Hızlı & Fan Profilleri
            foreach (var kv in ovProfileBtns) DarkTheme.StylePillButton(kv.Value, kv.Key == settings.PerformanceProfile);
            foreach (var kv in fanProfileBtns) DarkTheme.StylePillButton(kv.Value, kv.Key == settings.PerformanceProfile);
            foreach (var kv in ovFanBtns) DarkTheme.StylePillButton(kv.Value, kv.Key == settings.FanMode, DarkTheme.Cyan);
            foreach (var kv in fanModeBtns) DarkTheme.StylePillButton(kv.Value, kv.Key == settings.FanMode, DarkTheme.Cyan);

            ovLimitBtn.Text = $"Pil Sınırı (%80): {(settings.ChargeLimitEnabled ? Strings.LimitOn : Strings.LimitOff)}";
            DarkTheme.StylePillButton(ovLimitBtn, settings.ChargeLimitEnabled, DarkTheme.Green);

            var kb = settings.EffectiveKeyboard;
            editingKeyboard = kb;
            if (kb.Brightness > 0) lastNonZeroKbBrightness = kb.Brightness;
            ovKbToggleBtn.Text = kb.Brightness > 0 ? $"Klavye Işığı: %{kb.Brightness}" : "Klavye Işığı: Kapalı";
            DarkTheme.StylePillButton(ovKbToggleBtn, kb.Brightness > 0, DarkTheme.Cyan);

            // Fan detayları
            coolBoostCheck.Checked = settings.CoolBoostEnabled;
            cpuFanSlider.Value = Math.Clamp(settings.CpuFanPercent, 0, 100);
            gpuFanSlider.Value = Math.Clamp(settings.GpuFanPercent, 0, 100);
            cpuFanPctLabel.Text = $"%{cpuFanSlider.Value}";
            gpuFanPctLabel.Text = $"%{gpuFanSlider.Value}";
            fanLiveSummary.Text =
                $"CPU: {StatusText.WholeTemperature(telemetry?.CpuTempC)} ({StatusText.Rpm(telemetry?.CpuFanRpm)})   |   " +
                $"GPU: {StatusText.WholeTemperature(telemetry?.GpuTempC)} ({StatusText.Rpm(telemetry?.GpuFanRpm)})";

            // Klavye editörü
            RefreshKeyboardEditorUi();

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
            sysLimitCheck.Checked = settings.ChargeLimitEnabled;

            kbTimeoutCheck.Checked = settings.KeyboardTimeoutEnabled;
            var lcdSupported = telemetry?.LcdOverdriveSupported ?? true;
            lcdOverdriveCheck.Enabled = lcdSupported;
            lcdOverdriveCheck.Text = lcdSupported
                ? "LCD Overdrive (daha düşük ekran tepki süresi)"
                : "LCD Overdrive (bu panelde desteklenmiyor)";
            lcdOverdriveCheck.Checked = lcdSupported && settings.LcdOverdriveEnabled;
            usbChargingCheck.Checked = settings.UsbChargingEnabled;
            reapplyWakeCheck.Checked = settings.ReapplyOnWake;

            if (displayBrightness is int b)
            {
                displayBrightnessSlider.Value = Math.Clamp(b, 0, 100);
                displayBrightnessLabel.Text = $"%{displayBrightnessSlider.Value}";
            }

            var wmiOk = telemetry?.GamingWmiAvailable ?? false;
            wmiStatusLabel.Text = wmiOk ? "Acer WMI: Bağlı" : "Acer WMI: Yanıt Yok";
            wmiStatusLabel.ForeColor = wmiOk ? DarkTheme.Green : DarkTheme.Amber;
            diagWmiLabel.Text = wmiOk ? "AcerGamingFunction erişilebilir" : "Erişilemedi (Yönetici yetkisi gerekir)";
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
            DarkTheme.StylePillButton(kv.Value, kv.Key == editingKeyboard.Effect);

        kbBrightnessSlider.Value = Math.Clamp(editingKeyboard.Brightness, 0, 100);
        kbBrightnessLabel.Text = $"%{kbBrightnessSlider.Value}";

        var animated = editingKeyboard.Effect != KeyboardEffect.Static;
        kbSpeedSlider.Enabled = animated;
        kbSpeedSlider.Value = Math.Clamp(editingKeyboard.Speed, 1, 9);
        kbSpeedLabel.Text = kbSpeedSlider.Value.ToString();

        dirLtrBtn.Enabled = animated;
        dirRtlBtn.Enabled = animated;
        DarkTheme.StylePillButton(dirLtrBtn, editingKeyboard.Direction == KeyboardDirection.LeftToRight, DarkTheme.Cyan);
        DarkTheme.StylePillButton(dirRtlBtn, editingKeyboard.Direction == KeyboardDirection.RightToLeft, DarkTheme.Cyan);

        var hexes = editingKeyboard.ZoneHexes;
        for (var i = 0; i < 4; i++)
        {
            var c = KeyboardCodec.ParseHexColor(hexes[i], Color.Red);
            zoneButtons[i].BackColor = c;
            zoneButtons[i].ForeColor = DarkTheme.ContrastText(c);
            zoneButtons[i].Text = $"Bölge {i + 1}\n{KeyboardCodec.ToHex(c)}";
        }
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
        AutoEllipsis = true,
    };

    private static Label SubLabel(string text = Strings.Unknown) => new()
    {
        Text = text,
        Font = DarkTheme.FontSmall,
        ForeColor = DarkTheme.TextSecondary,
        AutoEllipsis = true,
    };

    private static Label CaptionLabel(string text) => new()
    {
        Text = text,
        Font = DarkTheme.FontBody,
        ForeColor = DarkTheme.TextSecondary,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Anchor = AnchorStyles.Left,
    };

    private static Label DetailVal() => new()
    {
        Text = Strings.Unknown,
        Font = DarkTheme.FontBodyBold,
        ForeColor = DarkTheme.TextPrimary,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
    };

    private static Label ValueBadge(string text) => new()
    {
        Text = text,
        Font = DarkTheme.FontBodyBold,
        ForeColor = DarkTheme.Cyan,
        TextAlign = ContentAlignment.MiddleRight,
        Dock = DockStyle.Fill,
    };

    private static CheckBox StyledCheckBox(string text) => new()
    {
        Text = text,
        ForeColor = DarkTheme.TextPrimary,
        Font = DarkTheme.FontBody,
        AutoSize = true,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 4, 0, 4),
    };

    private static TrackBar StyledTrackBar(int min, int max, int value) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        TickStyle = TickStyle.None,
        Dock = DockStyle.Fill,
        BackColor = DarkTheme.BgCard,
    };
}
