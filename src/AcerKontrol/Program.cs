using AcerKontrol.Battery;
using AcerKontrol.ChargeLimit;
using AcerKontrol.Settings;
using AcerKontrol.Tray;

namespace AcerKontrol;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, @"Global\AcerKontrol", out var createdNew);
        if (!createdNew) return;

        ApplicationConfiguration.Initialize();
        // Son çare: beklenmeyen bir UI istisnası WinForms hata penceresi açmasın, tepsi çalışmaya devam etsin.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => { };
        Application.Run(new TrayAppContext(
            new SettingsStore(SettingsStore.DefaultPath),
            new WmiBatteryInfoSource(),
            new AcerWmiChargeLimitDevice()));
    }
}
