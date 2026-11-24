using System.Management;
using System.Security.Principal;
using System.Text;
using AcerKontrol.Fan;

namespace AcerKontrol.Diagnostics;

/// <summary>Tüm WMI çağrılarını, dönüş kodlarını ve başlangıç donanım taramasını %APPDATA%\AcerKontrol\wmi.log dosyasına yazar.</summary>
public static class DiagLog
{
    private static readonly object Sync = new();
    private const long MaxBytes = 512 * 1024;

    public static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AcerKontrol", "wmi.log");

    public static void Info(string category, string message) => Write("INFO", category, message);

    public static void Error(string category, string message, Exception? ex = null)
        => Write("ERR ", category, ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");

    public static void WmiCall(string method, string inputDesc, object? outputValue, Exception? ex = null)
    {
        if (ex is not null)
        {
            Error("WMI ", $"{method}({inputDesc}) -> EXCEPTION", ex);
            return;
        }
        var outStr = outputValue switch
        {
            null => "null",
            ulong u64 => $"0x{u64:X16} ({u64})",
            uint u32 => $"0x{u32:X8} ({u32})",
            byte[] arr => $"[{string.Join(",", arr.Select(b => $"0x{b:X2}"))}]",
            _ => outputValue.ToString() ?? "null",
        };
        Info("WMI ", $"{method}({inputDesc}) -> {outStr}");
    }

    public static void RunStartupProbe()
    {
        try
        {
            var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            Info("INIT", $"=== AcerKontrol Başlatıldı | Admin={isAdmin} | OS={Environment.OSVersion} ===");

            using var instance = AcerWmiFanController.GetInstance();
            Info("INIT", $"AcerGamingFunction bulundu: {instance["InstanceName"]}");

            // GetGamingSysInfo taraması (0x00..0x10 ve sık kullanılan 0x0101..0x0601)
            uint[] sysQueries =
            [
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x0101, 0x0201, 0x0501, 0x0601,
            ];
            foreach (var q in sysQueries)
                ProbeUInt32InUInt64Out(instance, "GetGamingSysInfo", q);

            // Fan & Profil sorguları
            foreach (var q in new uint[] { 0x0001, 0x0008, 0x0009, 0xFFFF })
                ProbeUInt32InUInt64Out(instance, "GetGamingFanBehavior", q);

            foreach (var q in new uint[] { 0x01, 0x04 })
                ProbeUInt32InUInt64Out(instance, "GetGamingFanSpeed", q);

            foreach (var q in new uint[] { 0x00, 0x01 })
                ProbeUInt32InUInt64Out(instance, "GetGamingProfile", q);

            // MiscSetting sorguları (0x00..0x0F)
            for (uint i = 0x00; i <= 0x0F; i++)
                ProbeUInt32InUInt64Out(instance, "GetGamingMiscSetting", i);

            // Klavye sorguları
            try
            {
                using var input = instance.GetMethodParameters("GetGamingKBBacklight");
                input["gmInput"] = 0u;
                using var output = instance.InvokeMethod("GetGamingKBBacklight", input, null);
                var ret = output["gmReturn"];
                var bytes = (byte[]?)output["gmOutput"] ?? [];
                Info("PROB", $"GetGamingKBBacklight(0) -> ret={ret}, bytes=[{string.Join(",", bytes.Select(b => $"0x{b:X2}"))}]");
            }
            catch (Exception ex)
            {
                Error("PROB", "GetGamingKBBacklight(0)", ex);
            }

            foreach (var q in new uint[] { 0x00, 0x01, 0x02, 0x04, 0x08, 0x0F })
                ProbeUInt32InUInt64Out(instance, "GetGamingRgbKb", q);

            Info("INIT", "=== Başlangıç WMI Taraması Tamamlandı ===");
        }
        catch (Exception ex)
        {
            Error("INIT", "Başlangıç WMI taraması başarısız", ex);
        }
    }

    private static void ProbeUInt32InUInt64Out(ManagementObject instance, string method, uint query)
    {
        try
        {
            using var input = instance.GetMethodParameters(method);
            input["gmInput"] = query;
            using var output = instance.InvokeMethod(method, input, null);
            var raw = Convert.ToUInt64(output["gmOutput"]);
            var bytes = BitConverter.GetBytes(raw);
            Info("PROB", $"{method}(0x{query:X}) -> 0x{raw:X16} [{string.Join(",", bytes.Select(b => $"0x{b:X2}"))}]");
        }
        catch (Exception ex)
        {
            Error("PROB", $"{method}(0x{query:X})", ex);
        }
    }

    private static void Write(string level, string category, string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxBytes)
                {
                    var prev = LogPath + ".old";
                    File.Move(LogPath, prev, overwrite: true);
                }
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{category}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Loglama asla uygulamayı düşürmez.
        }
    }
}
