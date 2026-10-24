# AcerKontrol — Tasarım

## Amaç ve neden
Acer Nitro 5 AN515-58 (Windows 11, BIOS V2.21) için Acer Care Center'ın yerine geçen hafif bir pil sınırı aracı. Care Center'da pil sınırı ayarı kalıcı olmuyor ve bazen seçenek hiç görünmüyor. Kullanıcı sınırın her zaman, kendisi uğraşmadan yerinde durmasını istiyor.

## Hedefler
- Pil sınırını (%80, aç/kapat) tek tıkla değiştirmek.
- Seçimi hatırlamak ve her oturum açılışında, uykudan uyanınca, şarj kablosu takılıp çıkınca ve her 60 saniyede bir yeniden uygulamak (bekçi modu).
- Pil durumunu göstermek: yüzde, şarj oluyor/olmuyor, sağlık (tam şarj kapasitesi / tasarım kapasitesi), döngü sayısı, sıcaklık.
- Sistem tepsisi simgesi: pil yüzdesi rakamı; sınır açıkken yeşil, kapalıyken gri.
- Bildirim sadece sorun olduğunda (arayüz tekrar denemelerde yanıt vermez veya ayar uygulanamazsa).
- Windows açılışında yönetici yetkisiyle, UAC sormadan otomatik başlama (Görev Zamanlayıcı).
- Türkçe arayüz.

## Hedef dışı (bu sürüm)
- Fan, performans modu, klavye ışığı (sonra eklenebilir şekilde ayrı sınıflar; şimdi yok).
- Pil kalibrasyonu.
- Başka modeller, kurulum paketi, kod imzalama, dağıtım.
- Acer yazılımlarını kaldırmak (Care Center ve NitroSense kurulu kalır).

## Anti-hedefler
- Ayarı sessizce bozuk bırakmak (uygulanamadığı halde "açık" göstermek).
- Her yeniden uygulamada bildirim yağdırmak.
- Arayüz yanıt vermeyince çökmek.

## Kısıtlar (bulgular)
- Donanım arayüzü: WMI `root\wmi`, sınıf `BatteryControl`.
  - `GetBatteryHealthControlStatus(uBatteryNo:UInt8, uFunctionQuery:UInt8, uReserved:UInt8[2]) -> uFunctionList:UInt8, uReturn:UInt8[2], uFunctionStatus:UInt8[5]`
  - `SetBatteryHealthControl(uBatteryNo:UInt8, uFunctionMask:UInt8, uFunctionStatus:UInt8, uReservedIn:UInt8[5]) -> uReturn:UInt16, uReservedOut:UInt16`
  - Varsayım (Linux `acer-wmi-battery` sürücüsünden; bu makinede henüz doğrulanmadı): pil no `1`, sağlık modu fonksiyonu `1` (maske/sorgu), `uFunctionStatus[0]` = 1 açık / 0 kapalı, `uFunctionList` bit0 = destekleniyor, dönüş `0` = başarı.
- Sınır sabit %80; başka eşik donanımda yok.
- WMI erişimi yönetici yetkisi ister (okuma dahil; normal kullanıcıda "Access denied").
- `ACCSvc` (Acer Care Center servisi) çalışıyor; ayarı geri alabilir — bekçi modu bunu karşılar.
- Makinede .NET 9 SDK kurulu.

## Yaklaşımlar
1. **C# / .NET 9 WinForms tepsi uygulaması** — seçildi. Hafif, `NotifyIcon` hazır, `System.Management` ile WMI doğrudan.
2. Python + pystray — paketleme zahmetli, ağır.
3. PowerShell betiği + Görev Zamanlayıcı — tepsi arayüzü kırılgan, durum penceresi zor.

## Kararlaştırılanlar
- Kapsam: pil sınırı + pil durumu. Varsayılan durum: sınır açık.
- Tepsi simgesi + tıklayınca küçük durum penceresi.
- Bekçi: 60 sn + uyanma + güç kaynağı değişimi; başarısızlıkta sessiz tekrar.
- Sadece sorun bildirimi; Türkçe; yalnızca bu bilgisayar.

## Uygulayıcıya kalanlar (benim kararlarım)
- Ayarlar `%APPDATA%\AcerKontrol\settings.json`; bozuksa varsayılan.
- Tekrar: döngü başına 3 deneme, aralarında 2 sn. Başarısız seri başına tek bildirim.
- Kurulum yeri `%ProgramFiles%\AcerKontrol` (son incelemede `%LOCALAPPDATA%`'dan taşındı: yükseltilmiş çalışan exe kullanıcının yazabildiği bir klasörde olmamalı), zamanlanmış görev adı `AcerKontrol`.
- Tek örnek (mutex).
