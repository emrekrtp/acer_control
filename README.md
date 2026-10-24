# Acer Kontrol

Acer Nitro 5 AN515-58 için Acer Care Center'ın pil sınırı ayarının yerine geçen küçük bir tepsi uygulaması.

- Pil sınırını (%80) tek tıkla açıp kapatır, seçiminizi hatırlar.
- Ayarı her dakika, uykudan uyanınca ve şarj kablosu takılıp çıkınca yeniden uygular. Care Center ayarı geri alsa bile düzeltir.
- Tepsi simgesi pil yüzdesini gösterir: sınır açıkken yeşil, kapalıyken gri, Acer arayüzü yanıt vermiyorsa turuncu `?`.
- Simgeye tıklayınca pil durumu (yüzde, şarj, sağlık, döngü, sıcaklık) görünür. Bu modelde döngü ve sıcaklık bilgisi gelmiyor ("—").
- Sadece bir sorun olduğunda bildirim gösterir.

**Kısıt:** Donanım yalnızca sabit %80 sınırını destekliyor; başka bir eşik seçilemez.

## Kurulum / kaldırma

```powershell
pwsh scripts\install.ps1     # derler, kurar, oturum açılışında otomatik başlatır
pwsh scripts\uninstall.ps1   # durdurur ve kaldırır
```

Kurulum yeri `%ProgramFiles%\AcerKontrol` (yalnızca yöneticiler yazabilir; uygulama yönetici yetkisiyle çalıştığı için), ayarlar `%APPDATA%\AcerKontrol\settings.json`. Acer Care Center ve NitroSense kurulu kalabilir. Gerekirse .NET 9 SDK ile `dotnet test` testleri çalıştırır.
