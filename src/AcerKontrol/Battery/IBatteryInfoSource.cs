namespace AcerKontrol.Battery;

public interface IBatteryInfoSource
{
    BatterySnapshot Read();
}
