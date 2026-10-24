namespace AcerKontrol.Battery;

/// <summary>
/// Değeri ilk başarılı çağrıdan sonra önbelleğe alır (null dahil). İstisna atan çağrı önbelleğe
/// alınmaz ve sonraki Get'te yeniden denenir. Get hiçbir zaman istisna atmaz.
/// </summary>
public sealed class OnceOnSuccess<T>(Func<T> factory)
{
    private bool done;
    private T? value;

    public T? Get()
    {
        if (done) return value;
        try
        {
            value = factory();
            done = true;
        }
        catch (Exception)
        {
            // Sonraki çağrıda tekrar denenecek.
        }
        return value;
    }
}
