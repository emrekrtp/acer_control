using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Diagnostics;
using AcerKontrol.Fan;
using AcerKontrol.Keyboard;
using AcerKontrol.Settings;
using AcerKontrol.SystemControl;
using AcerKontrol.UI;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace AcerKontrol.Tray;

/// <summary>Tepsi simgesi, kontrol paneli, bekçi zamanlayıcısı ve donanım kontrollerini birbirine bağlar.</summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly SettingsStore store;
    private readonly IBatteryInfoSource battery;
    private readonly ISystemTelemetrySource telemetrySource;
    private readonly IFanController fanController;
    private readonly IKeyboardController keyboardController;
    private readonly ISystemController systemController;
    private readonly ChargeLimitGuard guard;
    private readonly SynchronizationContext ui;

    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem openItem;
    private readonly ToolStripMenuItem limitItem;
    private readonly ToolStripMenuItem fanMenu;
    private readonly ToolStripMenuItem profileMenu;
    private readonly ToolStripMenuItem kbMenu;
    private readonly ToolStripMenuItem exitItem;
    private readonly Dictionary<FanMode, ToolStripMenuItem> fanMenuItems = [];
    private readonly Dictionary<PerformanceProfile, ToolStripMenuItem> profileMenuItems = [];
    private readonly List<(KeyboardPreset Preset, ToolStripMenuItem Item)> kbPresetMenuItems = [];
    private readonly Timer guardTimer = new() { Interval = 60_000 };
    private readonly Timer liveTimer = new() { Interval = 2_000 };
    private readonly DashboardForm dashboard;

    private AppSettings settings;
    private BatterySnapshot? lastSnapshot;
    private ChargeLimitState? lastState;
    private SystemTelemetry? lastTelemetry;
    private int? lastDisplayBrightness;
    private bool ticking, tickPending, tickPendingQuiet = true;
    private bool livePolling;

    public TrayAppContext(
        SettingsStore store,
        IBatteryInfoSource battery,
        IChargeLimitDevice device,
        IFanController fanController,
        IKeyboardController keyboardController,
        ISystemController systemController,
        ISystemTelemetrySource telemetrySource,
        bool startMinimized = false)
    {
        this.store = store;
        this.battery = battery;
        this.fanController = fanController;
        this.keyboardController = keyboardController;
        this.systemController = systemController;
        this.telemetrySource = telemetrySource;
        guard = new ChargeLimitGuard(device, Thread.Sleep);
        settings = store.Load();
        Strings.SetLanguage(settings.Language);

        dashboard = new DashboardForm(SettingsStore.DefaultPath);
        dashboard.VisibleChanged += OnDashboardVisibleChanged;
        dashboard.LimitToggled += ToggleLimit;
        dashboard.FanModeSelected += ApplyFanMode;
        dashboard.CustomFanSpeedsApplied += ApplyCustomFanSpeeds;
        dashboard.CoolBoostToggled += ApplyCoolBoost;
        dashboard.PerformanceProfileSelected += ApplyPerformanceProfile;
        dashboard.WindowsPowerPlanSelected += ApplyWindowsPowerPlan;
        dashboard.KeyboardApplied += ApplyKeyboard;
        dashboard.KeyboardTimeoutToggled += ApplyKeyboardTimeout;
        dashboard.LcdOverdriveToggled += ApplyLcdOverdrive;
        dashboard.UsbChargingToggled += ApplyUsbCharging;
        dashboard.ReapplyOnWakeToggled += ToggleReapplyOnWake;
        dashboard.DisplayBrightnessChanged += ApplyDisplayBrightness;
        dashboard.LanguageSelected += ApplyLanguage;

        openItem = new ToolStripMenuItem(Strings.MenuDashboard, null, (_, _) => OpenDashboard())
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        limitItem = new ToolStripMenuItem(Strings.MenuLimit) { Checked = settings.ChargeLimitEnabled };
        limitItem.Click += (_, _) => ToggleLimit();

        fanMenu = new ToolStripMenuItem(Strings.MenuFanMode);
        foreach (var mode in Enum.GetValues<FanMode>())
        {
            var m = mode;
            var item = new ToolStripMenuItem(StatusText.FanModeName(m), null, (_, _) => ApplyFanMode(m));
            fanMenuItems[m] = item;
            fanMenu.DropDownItems.Add(item);
        }

        profileMenu = new ToolStripMenuItem(Strings.MenuProfile);
        foreach (var profile in Enum.GetValues<PerformanceProfile>())
        {
            var p = profile;
            var item = new ToolStripMenuItem(StatusText.ProfileName(p), null, (_, _) => ApplyPerformanceProfile(p));
            profileMenuItems[p] = item;
            profileMenu.DropDownItems.Add(item);
        }

        kbMenu = new ToolStripMenuItem(Strings.MenuKeyboard);
        foreach (var preset in KeyboardPresets.All)
        {
            var p = preset;
            var item = new ToolStripMenuItem(p.DisplayName, null, (_, _) => ApplyKeyboard(p.Settings));
            kbPresetMenuItems.Add((p, item));
            kbMenu.DropDownItems.Add(item);
        }

        exitItem = new ToolStripMenuItem(Strings.MenuExit, null, (_, _) => Exit());

        var menu = new ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(limitItem);
        menu.Items.Add(fanMenu);
        menu.Items.Add(profileMenu);
        menu.Items.Add(kbMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        // İlk kontrol oluşturulduğunda WinForms senkronizasyon bağlamı kurulmuş olur.
        ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = TrayIconRenderer.Render(TrayIconStyle.For(null, null)),
            Text = StatusText.Tooltip(null, null),
            Visible = true,
        };
        notifyIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) OpenDashboard(); };

        guardTimer.Tick += (_, _) => Tick();
        guardTimer.Start();
        liveTimer.Tick += (_, _) => PollLiveTelemetry();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        _ = Task.Run(DiagLog.RunStartupProbe);
        Render();
        Tick();

        if (!startMinimized)
            OpenDashboard();
    }

    private void ApplyLanguage(string langCode)
    {
        Strings.SetLanguage(langCode);
        settings = settings with { Language = Strings.LanguageCode };
        store.TrySave(settings);

        openItem.Text = Strings.MenuDashboard;
        limitItem.Text = Strings.MenuLimit;
        fanMenu.Text = Strings.MenuFanMode;
        profileMenu.Text = Strings.MenuProfile;
        kbMenu.Text = Strings.MenuKeyboard;
        exitItem.Text = Strings.MenuExit;

        foreach (var kv in fanMenuItems) kv.Value.Text = StatusText.FanModeName(kv.Key);
        foreach (var kv in profileMenuItems) kv.Value.Text = StatusText.ProfileName(kv.Key);
        foreach (var (preset, item) in kbPresetMenuItems) item.Text = preset.DisplayName;

        dashboard.ApplyLanguage();
        Render();
        dashboard.ShowFeedback(Strings.FbLanguageChanged);
    }

    private void OpenDashboard()
    {
        dashboard.ShowOrFocus();
        PollLiveTelemetry();
    }

    private void OnDashboardVisibleChanged(object? sender, EventArgs e)
    {
        if (dashboard.Visible)
        {
            liveTimer.Start();
            PollLiveTelemetry();
        }
        else
        {
            liveTimer.Stop();
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        // Uyanışta Acer arayüzü birkaç saniye hazır olmayabilir; o döngü bildirim göstermez.
        if (e.Mode == PowerModes.Resume)
        {
            ui.Post(_ =>
            {
                Tick(quiet: true);
                if (settings.ReapplyOnWake) ReapplySavedHardwareSettingsQuietly();
            }, null);
        }
        else if (e.Mode == PowerModes.StatusChange)
        {
            ui.Post(_ => Tick(), null);
        }
    }

    private void ToggleLimit()
    {
        settings = settings with { ChargeLimitEnabled = !settings.ChargeLimitEnabled };
        store.TrySave(settings);
        Render();
        Tick();
    }

    private void ApplyFanMode(FanMode mode)
    {
        settings = settings with { FanMode = mode };
        store.TrySave(settings);
        Render();
        RunHardwareAction(
            () =>
            {
                if (mode == FanMode.Custom)
                    fanController.SetCustomSpeeds(settings.CpuFanPercent, settings.GpuFanPercent);
                else
                    fanController.SetMode(mode);
            },
            Strings.FbFanMode(StatusText.FanModeName(mode)));
    }

    private void ApplyCustomFanSpeeds(int cpuPercent, int gpuPercent)
    {
        settings = settings with
        {
            FanMode = FanMode.Custom,
            CpuFanPercent = Math.Clamp(cpuPercent, 0, 100),
            GpuFanPercent = Math.Clamp(gpuPercent, 0, 100),
        };
        store.TrySave(settings);
        Render();
        RunHardwareAction(
            () => fanController.SetCustomSpeeds(settings.CpuFanPercent, settings.GpuFanPercent),
            Strings.FbCustomFan(settings.CpuFanPercent, settings.GpuFanPercent));
    }

    private void ApplyCoolBoost(bool enabled)
    {
        settings = settings with { CoolBoostEnabled = enabled };
        store.TrySave(settings);
        RunHardwareAction(
            () => systemController.SetCoolBoost(enabled),
            Strings.FbCoolBoost(enabled));
    }

    private void ApplyPerformanceProfile(PerformanceProfile profile)
    {
        settings = settings with { PerformanceProfile = profile };
        store.TrySave(settings);
        Render();
        RunHardwareAction(
            () => systemController.SetPerformanceProfile(profile),
            Strings.FbProfile(StatusText.ProfileName(profile)));
    }

    private void ApplyWindowsPowerPlan(WindowsPowerPlan plan)
    {
        RunHardwareAction(
            () => systemController.SetWindowsPowerPlan(plan),
            Strings.FbPowerPlan);
    }

    private void ApplyKeyboard(KeyboardLightingSettings kb)
    {
        settings = settings with { Keyboard = kb };
        store.TrySave(settings);
        Render();
        RunHardwareAction(
            () => keyboardController.Apply(kb),
            kb.Brightness == 0 ? Strings.FbKbOff : Strings.FbKbApplied(StatusText.EffectName(kb.Effect), kb.Brightness));
    }

    private void ApplyKeyboardTimeout(bool enabled)
    {
        settings = settings with { KeyboardTimeoutEnabled = enabled };
        store.TrySave(settings);
        RunHardwareAction(
            () => systemController.SetKeyboardTimeout(enabled),
            Strings.FbKbTimeout(enabled));
    }

    private void ApplyLcdOverdrive(bool enabled)
    {
        settings = settings with { LcdOverdriveEnabled = enabled };
        store.TrySave(settings);
        RunHardwareAction(
            () => systemController.SetLcdOverdrive(enabled),
            Strings.FbLcdOverdrive(enabled));
    }

    private void ApplyUsbCharging(bool enabled)
    {
        settings = settings with { UsbChargingEnabled = enabled };
        store.TrySave(settings);
        RunHardwareAction(
            () => systemController.SetUsbCharging(enabled),
            Strings.FbUsbCharging(enabled));
    }

    private void ToggleReapplyOnWake(bool enabled)
    {
        settings = settings with { ReapplyOnWake = enabled };
        store.TrySave(settings);
        dashboard.ShowFeedback(Strings.FbReapplyWake(enabled));
    }

    private void ApplyDisplayBrightness(int percent)
    {
        lastDisplayBrightness = percent;
        RunHardwareAction(
            () => systemController.SetDisplayBrightness(percent),
            Strings.FbDisplayBrightness(percent));
    }

    private async void RunHardwareAction(Action action, string successMessage)
    {
        DiagLog.Info("ACT ", $"Başlatılıyor: {successMessage}");
        try
        {
            await Task.Run(action);
            DiagLog.Info("ACT ", $"Başarılı: {successMessage}");
            dashboard.ShowFeedback(successMessage);
        }
        catch (Exception ex)
        {
            DiagLog.Error("ACT ", $"Başarısız: {successMessage}", ex);
            dashboard.ShowFeedback(Strings.FbFailed(ex.Message), isError: true);
        }
    }

    private async void ReapplySavedHardwareSettingsQuietly()
    {
        var current = settings;
        await Task.Run(() =>
        {
            Thread.Sleep(2500);
            try { systemController.SetPerformanceProfile(current.PerformanceProfile); } catch { }
            try
            {
                if (current.FanMode == FanMode.Custom)
                    fanController.SetCustomSpeeds(current.CpuFanPercent, current.GpuFanPercent);
                else
                    fanController.SetMode(current.FanMode);
            }
            catch { }
            try { keyboardController.Apply(current.EffectiveKeyboard); } catch { }
        });
    }

    private async void PollLiveTelemetry()
    {
        if (livePolling) return;
        livePolling = true;
        try
        {
            var (snap, tel, bright) = await Task.Run(() =>
            {
                BatterySnapshot? b = null;
                try { b = battery.Read(); } catch { }
                var t = telemetrySource.Read();
                var br = systemController.GetDisplayBrightness();
                return (b, t, br);
            });
            if (snap is not null) lastSnapshot = snap;
            lastTelemetry = tel;
            if (bright is not null) lastDisplayBrightness = bright;
            Render();
        }
        catch
        {
            // Bir sonraki 2 saniyelik döngüde tekrar denenir.
        }
        finally
        {
            livePolling = false;
        }
    }

    private async void Tick(bool quiet = false)
    {
        if (ticking)
        {
            tickPending = true;
            tickPendingQuiet &= quiet;
            return;
        }
        ticking = true;
        try
        {
            var desired = settings.ChargeLimitEnabled;
            var (result, snapshot) = await Task.Run(() => TrayCycle.Run(guard, battery, desired, quiet));
            DiagLog.Info("BATT", $"Tick(desired={desired}, quiet={quiet}) -> Outcome={result.Outcome}, Actual={result.Actual}, Charge={snapshot?.ChargePercent}%, Error={result.Error ?? "-"}");
            lastState = result.Actual;
            lastSnapshot = snapshot;
            Render();
            if (result.ShouldNotify)
                notifyIcon.ShowBalloonTip(10_000, Strings.NotifyFailTitle, Strings.NotifyFailBody, ToolTipIcon.Warning);
        }
        catch (Exception)
        {
            // async void'den kaçan istisna uygulamayı düşürür; bir sonraki döngü yeniden dener.
        }
        finally
        {
            ticking = false;
        }
        if (tickPending)
        {
            var pendingQuiet = tickPendingQuiet;
            tickPending = false;
            tickPendingQuiet = true;
            Tick(pendingQuiet);
        }
    }

    private void Render()
    {
        var old = notifyIcon.Icon;
        notifyIcon.Icon = TrayIconRenderer.Render(TrayIconStyle.For(lastSnapshot?.ChargePercent, lastState));
        old?.Dispose();
        notifyIcon.Text = StatusText.Tooltip(lastSnapshot?.ChargePercent, lastState);

        limitItem.Checked = settings.ChargeLimitEnabled;
        foreach (var kv in fanMenuItems) kv.Value.Checked = kv.Key == settings.FanMode;
        foreach (var kv in profileMenuItems) kv.Value.Checked = kv.Key == settings.PerformanceProfile;

        dashboard.UpdateView(settings, lastSnapshot, lastState, lastTelemetry, lastDisplayBrightness);
    }

    private void Exit()
    {
        guardTimer.Stop();
        liveTimer.Stop();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        dashboard.Dispose();
        ExitThread();
    }
}
