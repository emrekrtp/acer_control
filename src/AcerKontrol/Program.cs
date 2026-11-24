using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Fan;
using AcerKontrol.Keyboard;
using AcerKontrol.Settings;
using AcerKontrol.SystemControl;
using AcerKontrol.Tray;

namespace AcerKontrol;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, @"Global\AcerKontrol", out var createdNew);
        if (!createdNew) return;

        var startMinimized = args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

        ApplicationConfiguration.Initialize();
        // Son çare: beklenmeyen bir UI istisnası WinForms hata penceresi açmasın, tepsi çalışmaya devam etsin.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => { };
        Application.Run(new TrayAppContext(
            new SettingsStore(SettingsStore.DefaultPath),
            new WmiBatteryInfoSource(),
            new AcerWmiChargeLimitDevice(),
            new AcerWmiFanController(),
            new AcerWmiKeyboardController(),
            new AcerSystemController(),
            new WmiSystemTelemetrySource(),
            startMinimized));
    }
}
