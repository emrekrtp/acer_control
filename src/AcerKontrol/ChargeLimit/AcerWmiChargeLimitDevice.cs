using System.Management;

namespace AcerKontrol.ChargeLimit;

/// <summary>root\wmi BatteryControl üzerinden Acer "Battery Charge Limit" (sağlık modu). Yönetici yetkisi ister.</summary>
public sealed class AcerWmiChargeLimitDevice : IChargeLimitDevice
{
    private const byte BatteryNo = 1;
    private const byte HealthModeFunction = 1;

    public ChargeLimitState Read()
    {
        using var instance = GetInstance();
        using var input = instance.GetMethodParameters("GetBatteryHealthControlStatus");
        input["uBatteryNo"] = BatteryNo;
        input["uFunctionQuery"] = HealthModeFunction;
        input["uReserved"] = new byte[2];
        using var output = instance.InvokeMethod("GetBatteryHealthControlStatus", input, null);
        return ChargeLimitCodec.Decode(
            Convert.ToByte(output["uFunctionList"]),
            (byte[]?)output["uReturn"] ?? [],
            (byte[]?)output["uFunctionStatus"] ?? []);
    }

    public void Write(bool enabled)
    {
        using var instance = GetInstance();
        using var input = instance.GetMethodParameters("SetBatteryHealthControl");
        input["uBatteryNo"] = BatteryNo;
        input["uFunctionMask"] = HealthModeFunction;
        input["uFunctionStatus"] = (byte)(enabled ? 1 : 0);
        input["uReservedIn"] = new byte[5];
        using var output = instance.InvokeMethod("SetBatteryHealthControl", input, null);
        var ret = Convert.ToUInt16(output["uReturn"]);
        if (!ChargeLimitCodec.IsWriteSuccess(ret))
            throw new InvalidOperationException($"uReturn={ret}");
    }

    private static ManagementObject GetInstance()
    {
        using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM BatteryControl");
        return searcher.Get().Cast<ManagementObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("BatteryControl WMI örneği bulunamadı");
    }
}
