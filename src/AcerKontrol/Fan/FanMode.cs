namespace AcerKontrol.Fan;

public enum FanMode
{
    Auto = 1,
    Max = 2,
    Custom = 3,
}

public enum FanTarget : byte
{
    Cpu = 1,
    Gpu = 4,
}
