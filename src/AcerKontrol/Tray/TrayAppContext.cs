using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Settings;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace AcerKontrol.Tray;

/// <summary>Tepsi simgesi, bekçi zamanlayıcısı ve güç olaylarını birbirine bağlar.</summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly SettingsStore store;
    private readonly IBatteryInfoSource battery;
    private readonly ChargeLimitGuard guard;
    private readonly SynchronizationContext ui;
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem limitItem;
    private readonly Timer timer = new() { Interval = 60_000 };
    private readonly StatusForm statusForm = new();

    private AppSettings settings;
    private BatterySnapshot? lastSnapshot;
    private ChargeLimitState? lastState;
    private bool ticking, tickPending;

    public TrayAppContext(SettingsStore store, IBatteryInfoSource battery, IChargeLimitDevice device)
    {
        this.store = store;
        this.battery = battery;
        guard = new ChargeLimitGuard(device, Thread.Sleep);
        settings = store.Load();

        limitItem = new ToolStripMenuItem(Strings.MenuLimit) { Checked = settings.ChargeLimitEnabled };
        limitItem.Click += (_, _) => ToggleLimit();
        var menu = new ContextMenuStrip();
        menu.Items.Add(limitItem);
        menu.Items.Add(Strings.MenuStatus, null, (_, _) => statusForm.ShowNearTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Strings.MenuExit, null, (_, _) => Exit());

        // İlk kontrol oluşturulduğunda WinForms senkronizasyon bağlamı kurulmuş olur.
        ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = TrayIconRenderer.Render(TrayIconStyle.For(null, null)),
            Text = StatusText.Tooltip(null, null),
            Visible = true,
        };
        notifyIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) statusForm.ShowNearTray(); };
        statusForm.LimitToggled += ToggleLimit;

        timer.Tick += (_, _) => Tick();
        timer.Start();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        Tick();
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is PowerModes.Resume or PowerModes.StatusChange)
            ui.Post(_ => Tick(), null);
    }

    private void ToggleLimit()
    {
        settings = settings with { ChargeLimitEnabled = !settings.ChargeLimitEnabled };
        store.Save(settings);
        limitItem.Checked = settings.ChargeLimitEnabled;
        Tick();
    }

    private async void Tick()
    {
        if (ticking) { tickPending = true; return; }
        ticking = true;
        try
        {
            var desired = settings.ChargeLimitEnabled;
            var (result, snapshot) = await Task.Run(() => (guard.Enforce(desired), battery.Read()));
            lastState = result.Actual;
            lastSnapshot = snapshot;
            Render();
            if (result.ShouldNotify)
                notifyIcon.ShowBalloonTip(10_000, Strings.NotifyFailTitle, Strings.NotifyFailBody, ToolTipIcon.Warning);
        }
        finally
        {
            ticking = false;
        }
        if (tickPending) { tickPending = false; Tick(); }
    }

    private void Render()
    {
        var old = notifyIcon.Icon;
        notifyIcon.Icon = TrayIconRenderer.Render(TrayIconStyle.For(lastSnapshot?.ChargePercent, lastState));
        old?.Dispose();
        notifyIcon.Text = StatusText.Tooltip(lastSnapshot?.ChargePercent, lastState);
        limitItem.Checked = settings.ChargeLimitEnabled;
        statusForm.UpdateView(lastSnapshot, lastState, settings.ChargeLimitEnabled);
    }

    private void Exit()
    {
        timer.Stop();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        statusForm.Dispose();
        ExitThread();
    }
}
