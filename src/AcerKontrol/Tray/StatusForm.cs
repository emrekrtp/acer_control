using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;

namespace AcerKontrol.Tray;

/// <summary>Pil durumunu gösteren küçük pencere. Kapatınca sadece gizlenir.</summary>
public sealed class StatusForm : Form
{
    private readonly Label battery = Value(), state = Value(), health = Value(), cycles = Value(), temperature = Value(), limit = Value();
    private readonly CheckBox limitBox = new() { Text = Strings.MenuLimit, AutoCheck = false, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };

    public event Action? LimitToggled;

    public StatusForm()
    {
        Text = Strings.AppName;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(14);
        Font = new Font("Segoe UI", 10f);

        var table = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        void Row(string caption, Control value)
        {
            table.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(0, 3, 16, 3) });
            table.Controls.Add(value);
        }
        Row(Strings.LabelBattery, battery);
        Row(Strings.LabelState, state);
        Row(Strings.LabelHealth, health);
        Row(Strings.LabelCycles, cycles);
        Row(Strings.LabelTemperature, temperature);
        Row(Strings.LabelLimit, limit);
        table.Controls.Add(limitBox);
        table.SetColumnSpan(limitBox, 2);
        Controls.Add(table);

        limitBox.Click += (_, _) => LimitToggled?.Invoke();
    }

    public void UpdateView(BatterySnapshot? snapshot, ChargeLimitState? actual, bool desired)
    {
        battery.Text = StatusText.Percent(snapshot?.ChargePercent);
        state.Text = snapshot is null ? Strings.Unknown : StatusText.Power(snapshot);
        health.Text = StatusText.Percent(snapshot?.HealthPercent);
        cycles.Text = StatusText.Count(snapshot?.CycleCount);
        temperature.Text = StatusText.Temperature(snapshot?.TemperatureC);
        limit.Text = StatusText.Limit(actual);
        limitBox.Checked = desired;
    }

    public void ShowNearTray()
    {
        Show();
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(this);
        Location = new Point(area.Right - Width - 12, area.Bottom - Height - 12);
        Activate();
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

    private static Label Value() => new() { AutoSize = true, Margin = new Padding(0, 3, 0, 3), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
}
