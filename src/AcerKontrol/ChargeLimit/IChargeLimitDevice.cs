namespace AcerKontrol.ChargeLimit;

public enum ChargeLimitState { Off, On, Unsupported }

/// <summary>%80 pil sınırına erişim. Her iki metot da istisna atabilir.</summary>
public interface IChargeLimitDevice
{
    ChargeLimitState Read();
    void Write(bool enabled);
}
