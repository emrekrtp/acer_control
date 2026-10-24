using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace AcerKontrol.Battery;

/// <summary>WMI'da tasarım kapasitesi okunamadığında `powercfg /batteryreport` yedeği.</summary>
public static class PowercfgReport
{
    public static uint? ParseDesignCapacity(string xml)
    {
        try
        {
            var value = XDocument.Parse(xml).Descendants().FirstOrDefault(e => e.Name.LocalName == "DesignCapacity")?.Value;
            return uint.TryParse(value, out var mwh) && mwh > 0 ? mwh : null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    public static uint? ReadDesignCapacity()
    {
        var file = Path.Combine(Path.GetTempPath(), $"acerkontrol-battery-{Environment.ProcessId}.xml");
        try
        {
            using var p = Process.Start(new ProcessStartInfo("powercfg", $"/batteryreport /xml /output \"{file}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            if (p is null || !p.WaitForExit(15_000) || !File.Exists(file)) return null;
            return ParseDesignCapacity(File.ReadAllText(file));
        }
        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }
}
