using System.Management;
using AcerKontrol.Diagnostics;
using AcerKontrol.Fan;

namespace AcerKontrol.Keyboard;

/// <summary>root\wmi AcerGamingFunction üzerinden 4 bölgeli RGB klavye kontrolü.</summary>
public sealed class AcerWmiKeyboardController : IKeyboardController
{
    public void Apply(KeyboardLightingSettings settings)
    {
        using var instance = AcerWmiFanController.GetInstance();

        if (settings.Effect == KeyboardEffect.Static)
        {
            var zones = settings.ZoneHexes;
            for (var i = 0; i < 4; i++)
            {
                var color = KeyboardCodec.ParseHexColor(zones[i], Color.Red);
                InvokeUInt64(instance, "SetGamingRgbKb", KeyboardCodec.EncodeZoneColor(i, color));
            }
        }

        var primary = settings.Effect == KeyboardEffect.Static
            ? KeyboardCodec.ParseHexColor(settings.Zone1Hex, Color.Red)
            : KeyboardCodec.ParseHexColor(settings.EffectColorHex, Color.Red);

        var payload = KeyboardCodec.EncodeBacklightPayload(
            settings.Effect,
            settings.Brightness,
            settings.Speed,
            settings.Direction,
            primary);

        InvokeByteArray(instance, "SetGamingKBBacklight", payload);
    }

    private static void InvokeUInt64(ManagementObject instance, string methodName, ulong gmInput)
    {
        var inputDesc = $"0x{gmInput:X16}";
        try
        {
            using var input = instance.GetMethodParameters(methodName);
            input["gmInput"] = gmInput;
            using var output = instance.InvokeMethod(methodName, input, null);
            var ret = Convert.ToUInt32(output["gmOutput"]);
            DiagLog.WmiCall(methodName, inputDesc, ret);
            if ((ret & 0xFF) != 0)
                throw new InvalidOperationException($"{methodName} başarısız (kod={ret & 0xFF})");
        }
        catch (Exception ex)
        {
            DiagLog.WmiCall(methodName, inputDesc, null, ex);
            throw;
        }
    }

    private static void InvokeByteArray(ManagementObject instance, string methodName, byte[] gmInput)
    {
        var inputDesc = $"[{string.Join(",", gmInput.Select(b => $"0x{b:X2}"))}]";
        try
        {
            using var input = instance.GetMethodParameters(methodName);
            input["gmInput"] = gmInput;
            using var output = instance.InvokeMethod(methodName, input, null);
            var ret = Convert.ToUInt32(output["gmOutput"]);
            DiagLog.WmiCall(methodName, inputDesc, ret);
            if ((ret & 0xFF) != 0)
                throw new InvalidOperationException($"{methodName} başarısız (kod={ret & 0xFF})");
        }
        catch (Exception ex)
        {
            DiagLog.WmiCall(methodName, inputDesc, null, ex);
            throw;
        }
    }
}
