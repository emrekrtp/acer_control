namespace AcerKontrol.SystemControl;

public enum PerformanceProfile : byte
{
    Quiet = 0x00,
    Balanced = 0x01,
    Performance = 0x04,
    Turbo = 0x05,
}

public enum WindowsPowerPlan
{
    PowerSaver,
    Balanced,
    HighPerformance,
}

/// <summary>AcerGamingFunction SetGamingMiscSetting / GetGamingMiscSetting bit paketleyici.</summary>
public static class MiscSettingCodec
{
    public const byte CoolBoostIndex = 0x01;
    public const byte KeyboardTimeoutIndex = 0x02;
    public const byte LcdOverdriveIndex = 0x04;
    public const byte UsbChargingIndex = 0x06;
    public const byte SupportedProfilesIndex = 0x0A;
    public const byte PlatformProfileIndex = 0x0B;

    /// <summary>SetGamingMiscSetting için (value &lt;&lt; 8) | index paketler.</summary>
    public static ulong Encode(byte index, byte value) => ((ulong)value << 8) | index;

    public static ulong EncodeProfile(PerformanceProfile profile) => Encode(PlatformProfileIndex, (byte)profile);

    public static ulong EncodeToggle(byte index, bool enabled) => Encode(index, (byte)(enabled ? 1 : 0));

    /// <summary>Acer WMI dönüş kodunun alt baytı 0 ise işlem başarılıdır (üst bayt yazılan değeri yansıtabilir).</summary>
    public static bool IsSuccess(uint gmOutput) => (gmOutput & 0xFF) == 0;

    /// <summary>GetGamingMiscSetting çıktısında alt bayt 0 ve değer baytı 0xFF değilse özellik destekleniyordur.</summary>
    public static bool IsSupportedValue(ulong gmOutput)
        => (gmOutput & 0xFF) == 0 && ((gmOutput >> 8) & 0xFF) != 0xFF;

    public static string WindowsPowerPlanGuid(WindowsPowerPlan plan) => plan switch
    {
        WindowsPowerPlan.PowerSaver => "a1841308-3541-4fab-bc81-f71556f20b4a",
        WindowsPowerPlan.Balanced => "381b4222-f694-41f0-9685-ff5bb260df2e",
        WindowsPowerPlan.HighPerformance => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
        _ => "381b4222-f694-41f0-9685-ff5bb260df2e",
    };
}
