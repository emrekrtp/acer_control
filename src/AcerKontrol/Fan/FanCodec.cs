namespace AcerKontrol.Fan;

/// <summary>AcerGamingFunction WMI fan kontrol bit paketleyici/çözücü.</summary>
public static class FanCodec
{
    public const ushort CpuFanMask = 0x0001;
    public const ushort GpuFanMask = 0x0008;
    public const ushort AllFansMask = CpuFanMask | GpuFanMask;

    /// <summary>
    /// SetGamingFanBehavior için 64-bit payload üretir.
    /// Alt 16 bit hedef fan maskesi (CPU=bit 0, GPU=bit 3),
    /// üst bitler her fan için 2-bit mod alanı (CPU: bit 16..17, GPU: bit 22..23).
    /// </summary>
    public static ulong EncodeBehavior(FanMode cpuMode, FanMode gpuMode)
    {
        ulong payload = AllFansMask;
        payload |= ((ulong)(byte)cpuMode & 0x03UL) << 16;
        payload |= ((ulong)(byte)gpuMode & 0x03UL) << 22;
        return payload;
    }

    public static ulong EncodeBehavior(FanMode mode) => EncodeBehavior(mode, mode);

    /// <summary>
    /// SetGamingFanSpeed için 64-bit payload üretir: (yüzde &lt;&lt; 8) | fanIndex.
    /// Yüzde 0..100 aralığına kırpılır.
    /// </summary>
    public static ulong EncodeSpeed(FanTarget target, int percent)
    {
        var clamped = (byte)Math.Clamp(percent, 0, 100);
        return ((ulong)clamped << 8) | (byte)target;
    }

    /// <summary>
    /// GetGamingSysInfo RPM çıktısını çözer. İlk bayt 0 (başarılı) olmalıdır;
    /// RPM değeri 1. ve 2. baytlarda little-endian 16-bit tamsayı olarak gelir.
    /// </summary>
    public static int? DecodeRpm(ulong raw)
    {
        if ((raw & 0xFF) != 0) return null;
        var rpm = (int)((raw >> 8) & 0xFFFF);
        return rpm is > 0 and < 15_000 ? rpm : null;
    }

    /// <summary>
    /// GetGamingSysInfo sıcaklık çıktısını çözer. İlk bayt 0 (başarılı) olmalıdır;
    /// sıcaklık 1. baytta °C cinsinden gelir.
    /// </summary>
    public static int? DecodeTemperatureC(ulong raw)
    {
        if ((raw & 0xFF) != 0) return null;
        var temp = (int)((raw >> 8) & 0xFF);
        return temp is > 0 and < 130 ? temp : null;
    }
}
