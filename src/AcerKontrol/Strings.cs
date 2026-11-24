namespace AcerKontrol;

/// <summary>Kullanıcıya görünen tüm metinler (TR / EN canlı dil desteği).</summary>
public static class Strings
{
    public static bool IsEnglish { get; set; }

    public static void SetLanguage(string? code)
        => IsEnglish = string.Equals(code, "en", StringComparison.OrdinalIgnoreCase);

    public static string LanguageCode => IsEnglish ? "en" : "tr";

    private static string T(string tr, string en) => IsEnglish ? en : tr;

    public const string Unknown = "—";

    public static string AppName => T("Acer Kontrol", "Acer Control");
    public static string AppSubtitle => T("Nitro 5 Kontrol Merkezi", "Nitro 5 Control Center");

    // Tepsi menüsü
    public static string MenuDashboard => T("Kontrol Panelini Aç", "Open Control Panel");
    public static string MenuLimit => T("Pil sınırı (%80)", "Battery Limit (80%)");
    public static string MenuStatus => T("Durumu göster", "Show Status");
    public static string MenuFanMode => T("Fan Modu", "Fan Mode");
    public static string MenuProfile => T("Performans Profili", "Performance Profile");
    public static string MenuKeyboard => T("Klavye Işığı", "Keyboard Lighting");
    public static string MenuExit => T("Çıkış", "Exit");

    // Genel durumlar
    public static string LimitOn => T("Açık", "On");
    public static string LimitOff => T("Kapalı", "Off");
    public static string LimitUnknown => T("Bilinmiyor", "Unknown");
    public static string Ready => T("Hazır", "Ready");

    public static string Charging => T("Şarj oluyor", "Charging");
    public static string NotCharging => T("Şarj olmuyor", "Not charging");
    public static string PluggedNotCharging => T("Prizde, şarj olmuyor", "Plugged in, not charging");
    public static string OnBattery => T("Pilden çalışıyor", "On battery");

    public static string NotifyFailTitle => T("Pil sınırı uygulanamadı", "Battery limit could not be applied");
    public static string NotifyFailBody => T(
        "Acer arayüzü yanıt vermedi. Bir dakika sonra tekrar denenecek.",
        "Acer hardware interface did not respond. Will retry in one minute.");

    public static string LabelBattery => T("Pil", "Battery");
    public static string LabelState => T("Durum", "Status");
    public static string LabelHealth => T("Sağlık", "Health");
    public static string LabelCycles => T("Döngü sayısı", "Cycle count");
    public static string LabelTemperature => T("Sıcaklık", "Temperature");
    public static string LabelLimit => T("Pil sınırı", "Charge limit");

    // Fan & Performans
    public static string FanAuto => T("Otomatik", "Auto");
    public static string FanMax => T("Maksimum", "Max");
    public static string FanCustom => T("Özel", "Custom");

    public static string ProfileQuiet => T("Sessiz", "Quiet");
    public static string ProfileBalanced => T("Dengeli", "Balanced");
    public static string ProfilePerformance => T("Performans", "Performance");
    public static string ProfileTurbo => T("Turbo", "Turbo");

    // Klavye efektleri & yön
    public static string EffectStatic => T("Sabit Renk", "Static");
    public static string EffectBreathing => T("Nefes Alma", "Breathing");
    public static string EffectNeon => T("Neon", "Neon");
    public static string EffectWave => T("Dalga", "Wave");
    public static string EffectShifting => T("Kaydırma", "Shifting");
    public static string EffectZoom => T("Yakınlaştırma", "Zoom");

    public static string DirectionRightToLeft => T("Sağdan Sola", "Right to Left");
    public static string DirectionLeftToRight => T("Soldan Sağa", "Left to Right");

    public static string PowerPlanSaver => T("Güç Tasarrufu", "Power Saver");
    public static string PowerPlanBalanced => T("Dengeli", "Balanced");
    public static string PowerPlanHigh => T("Yüksek Performans", "High Performance");

    // Kenar çubuğu & Navigasyon
    public static string NavOverview => T("Genel Bakış", "Overview");
    public static string NavFan => T("Fan ve Performans", "Fan & Performance");
    public static string NavKeyboard => T("Klavye Işığı", "Keyboard RGB");
    public static string NavSystem => T("Pil ve Sistem", "Battery & System");

    // Sayfa başlıkları
    public static string OverviewSubtitle => T(
        "Anlık sistem sıcaklığı, donanım telemetrisi ve hızlı kontroller",
        "Real-time system temperatures, hardware telemetry, and quick controls");
    public static string FanSubtitle => T(
        "Soğutma profili, fan hızları ve Windows güç planı yönetimi",
        "Thermal profile, fan speed curves, and Windows power plan");
    public static string KeyboardSubtitle => T(
        "4 bölgeli RGB renk özelleştirme, efektler ve hazır temalar",
        "4-zone RGB customization, lighting effects, and color presets");
    public static string SystemSubtitle => T(
        "Pil sağlığı, %80 şarj koruması, ekran ve donanım seçenekleri",
        "Battery health, 80% charge guard, display, and hardware options");

    // Genel Bakış kartları
    public static string MetricCpu => T("İŞLEMCİ (CPU)", "PROCESSOR (CPU)");
    public static string MetricGpu => T("EKRAN KARTI (GPU)", "GRAPHICS (GPU)");
    public static string MetricRam => T("BELLEK (RAM)", "MEMORY (RAM)");
    public static string MetricBattery => T("PİL DURUMU", "BATTERY STATUS");
    public static string GpuSleep => T("Uyku (dGPU)", "Idle (dGPU)");
    public static string UsageLabel => T("Kullanım", "Usage");
    public static string FanLabel => T("Fan", "Fan");
    public static string ModeLabel => T("Mod", "Mode");
    public static string CardQuickControls => T("Hızlı Kontroller", "Quick Controls");
    public static string QuickLimitPrefix => T("Pil Sınırı (%80)", "Battery Limit (80%)");
    public static string QuickKbPrefix => T("Klavye Işığı", "Keyboard Light");

    // Fan sayfası
    public static string ProfileHint => T(
        "Sessiz: düşük fan sesi · Dengeli: günlük kullanım · Performans / Turbo: maksimum güç ve soğutma",
        "Quiet: low noise · Balanced: daily use · Performance / Turbo: maximum power and cooling");
    public static string CardFanControl => T("Fan Kontrolü", "Fan Control");
    public static string CoolBoostSub => T(
        "Yüksek yük altında daha agresif fan soğutması uygular",
        "Applies more aggressive fan cooling under heavy load");
    public static string CardCustomFan => T("Özel Fan Hızı (%0 – %100)", "Custom Fan Speed (0% – 100%)");
    public static string LabelCpuFan => T("CPU Fanı", "CPU Fan");
    public static string LabelGpuFan => T("GPU Fanı", "GPU Fan");
    public static string SyncFansText => T("CPU ve GPU fan hızlarını eşitle", "Sync CPU and GPU fan speeds");
    public static string ApplyCustomFan => T("Özel Fan Hızını Uygula", "Apply Custom Fan Speed");
    public static string CardPowerPlan => T("Windows Güç Planı", "Windows Power Plan");

    // Klavye sayfası
    public static string CardZonePreview => T(
        "4 Bölgeli Klavye Önizleme (Renk değiştirmek için bölgeye tıklayın)",
        "4-Zone Keyboard Preview (Click a zone to pick a color)");
    public static string ZoneLabel(int num) => T($"Bölge {num}", $"Zone {num}");
    public static string PickAllZones => T("Tüm Bölgelere Tek Renk Seç...", "Pick One Color for All Zones...");
    public static string ApplyKeyboard => T("Klavyeye Uygula", "Apply to Keyboard");
    public static string CardEffects => T("Aydınlatma Efekti", "Lighting Effect");
    public static string CardKbControls => T("Parlaklık, Hız ve Yön", "Brightness, Speed & Direction");
    public static string LabelBrightness => T("Parlaklık", "Brightness");
    public static string LabelSpeed => T("Hız", "Speed");
    public static string LabelDirection => T("Yön", "Direction");
    public static string SpeedHint => T("Animasyon efektleri için", "For animated effects");
    public static string CardPresets => T("Hazır Renk Temaları", "Color Presets");

    // Pil & Sistem sayfası
    public static string CardBatteryDetails => T("Pil Sağlığı ve Şarj Sınırı", "Battery Health & Charge Limit");
    public static string CapCharge => T("Şarj", "Charge");
    public static string CapCapacity => T("Kapasite", "Capacity");
    public static string CapCycles => T("Döngü", "Cycles");
    public static string CapLimitStatus => T("Sınır Durumu", "Limit Status");
    public static string ToggleLimitTitle => T("Pil şarjını %80 ile sınırla", "Limit battery charge to 80%");
    public static string ToggleLimitSub => T("Pil ömrünü uzatmak için şarjı %80'de durdurur", "Stops charging at 80% to extend battery lifespan");
    public static string CardDisplayBrightness => T("Ekran Parlaklığı", "Display Brightness");
    public static string CardHardwareOptions => T("Donanım Seçenekleri ve Otomasyon", "Hardware Options & Automation");
    public static string ToggleKbTimeoutTitle => T("Klavye ışığı zaman aşımı (30 sn)", "Keyboard backlight timeout (30s)");
    public static string ToggleKbTimeoutSub => T("Hareketsizlikte klavye aydınlatmasını otomatik kapatır", "Automatically turns off keyboard backlight when idle");
    public static string ToggleUsbTitle => T("Kapalıyken USB Şarjı", "Power-Off USB Charging");
    public static string ToggleUsbSub => T("Bilgisayar kapalıyken Power-Off USB portu üzerinden şarj sağlar", "Enables charging via the Power-Off USB port while shut down");
    public static string ToggleLcdTitle => "LCD Overdrive";
    public static string ToggleLcdSub => T("Daha düşük ekran tepki süresi (panel desteği gerekir)", "Faster display response time (requires panel support)");
    public static string ToggleLcdUnsupported => T("Bu ekran panelinde desteklenmiyor", "Not supported on this display panel");
    public static string ToggleWakeTitle => T("Uyanışta ayarları yeniden uygula", "Reapply settings on wake");
    public static string ToggleWakeSub => T("Uykudan çıkınca pil sınırı, fan ve klavye profilini geri yükler", "Restores battery limit, fan, and keyboard profile after sleep");
    public static string CardDiagnostics => T("Sistem ve Tanılama", "System & Diagnostics");
    public static string CapPrivileges => T("Yetki Durumu", "Privileges");
    public static string CapSettingsFile => T("Ayar Dosyası", "Settings File");
    public static string AdminYes => T("Yönetici (Tam Yetki)", "Administrator (Full Access)");
    public static string AdminNo => T("Standart Kullanıcı (Kısıtlı)", "Standard User (Limited)");
    public static string WmiChecking => T("Kontrol ediliyor...", "Checking...");
    public static string WmiConnected => T("Acer WMI: Bağlı", "Acer WMI: Connected");
    public static string WmiNoResponse => T("Acer WMI: Yanıt Yok", "Acer WMI: No Response");
    public static string WmiAccessible => T("AcerGamingFunction erişilebilir", "AcerGamingFunction accessible");
    public static string WmiInaccessible => T("Erişilemedi (Yönetici yetkisi gerekir)", "Unavailable (Administrator required)");
    public static string BtnOpenLog => T("Logu Aç", "Open Log");
    public static string BtnOpenFolder => T("Klasörü Aç", "Open Folder");
    public static string NoLogYet => T("Henüz log dosyası oluşmadı", "No log file created yet");

    // Geri bildirim mesajları
    public static string FbFanMode(string mode) => T($"Fan modu: {mode}", $"Fan mode: {mode}");
    public static string FbCustomFan(int cpu, int gpu) => T($"Özel fan hızı: CPU %{cpu}, GPU %{gpu}", $"Custom fan speed: CPU {cpu}%, GPU {gpu}%");
    public static string FbCoolBoost(bool on) => $"CoolBoost: {(on ? LimitOn : LimitOff)}";
    public static string FbProfile(string profile) => T($"Performans profili: {profile}", $"Performance profile: {profile}");
    public static string FbPowerPlan => T("Windows güç planı güncellendi", "Windows power plan updated");
    public static string FbKbOff => T("Klavye ışığı kapatıldı", "Keyboard backlight turned off");
    public static string FbKbApplied(string eff, int br) => T($"Klavye: {eff} (%{br})", $"Keyboard: {eff} ({br}%)");
    public static string FbKbTimeout(bool on) => T($"Klavye 30 sn kapanma: {(on ? LimitOn : LimitOff)}", $"Keyboard 30s timeout: {(on ? LimitOn : LimitOff)}");
    public static string FbLcdOverdrive(bool on) => $"LCD Overdrive: {(on ? LimitOn : LimitOff)}";
    public static string FbUsbCharging(bool on) => T($"USB Kapalıyken Şarj: {(on ? LimitOn : LimitOff)}", $"Power-Off USB Charging: {(on ? LimitOn : LimitOff)}");
    public static string FbReapplyWake(bool on) => T($"Uyanışta otomatik uygulama: {(on ? LimitOn : LimitOff)}", $"Reapply on wake: {(on ? LimitOn : LimitOff)}");
    public static string FbDisplayBrightness(int p) => T($"Ekran parlaklığı: %{p}", $"Display brightness: {p}%");
    public static string FbFailed(string msg) => T($"Uygulanamadı: {msg}", $"Failed: {msg}");
    public static string FbLanguageChanged => T("Dil: Türkçe", "Language: English");
}
