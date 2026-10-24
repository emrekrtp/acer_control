namespace AcerKontrol.ChargeLimit;

/// <summary>Acer BatteryControl WMI dönüş değerlerinin yorumu.</summary>
public static class ChargeLimitCodec
{
    private const byte HealthModeBit = 1;

    public static ChargeLimitState Decode(byte functionList, byte[] ret, byte[] status)
    {
        if ((functionList & HealthModeBit) == 0 || ret.Any(b => b != 0) || status.Length == 0)
            return ChargeLimitState.Unsupported;
        return status[0] == 1 ? ChargeLimitState.On : ChargeLimitState.Off;
    }

    public static bool IsWriteSuccess(ushort ret) => ret == 0;
}
