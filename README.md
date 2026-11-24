# Acer Kontrol

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6.svg)](https://www.microsoft.com/windows)

Acer Nitro 5 (AN515-58) için **NitroSense** ve **Acer Care Center**'ın temel özelliklerini hafif, koyu temalı ve kullanıcı dostu tek bir sistem tepsisi + kontrol paneli uygulamasında birleştiren açık kaynaklı Windows aracı.

## Özellikler

- **Genel Bakış Paneli:** Canlı CPU/GPU sıcaklığı, CPU/RAM kullanımı, CPU/GPU fan hızı (RPM), pil durumu ve tek tıkla hızlı kontroller.
- **Fan ve Performans Kontrolü:**
  - Performans profilleri: **Sessiz**, **Dengeli**, **Performans**, **Turbo**.
  - Fan modları: **Otomatik**, **Maksimum**, **Özel** (CPU ve GPU için %0–100 bağımsız veya eşitlenmiş hız kaydırıcıları).
  - **CoolBoost** anahtarı ve **Windows Güç Planı** (Güç Tasarrufu / Dengeli / Yüksek Performans) hızlı seçici.
- **4 Bölgeli RGB Klavye Işığı:**
  - Efektler: **Sabit Renk**, **Nefes Alma**, **Neon**, **Dalga**, **Kaydırma**, **Yakınlaştırma**.
  - Parlaklık (%0–100), animasyon hızı (1–9), yön (Soldan Sağa / Sağdan Sola) ve 4 bağımsız bölge için renk seçici.
  - Tek tıkla hazır temalar (Nitro Kırmızı, Siber Mavi, Zümrüt, Mor Neon, Gökkuşağı, Buz Beyazı, Dalga, Kapalı).
- **Pil Sınırı (%80) ve Sistem Araçları:**
  - %80 pil şarj sınırını her dakika, uykudan uyanınca ve şarj kablosu takılıp çıkınca otomatik korur.
  - Canlı **Ekran Parlaklığı** kaydırıcısı, **LCD Overdrive**, **USB Kapalıyken Şarj** ve uyanışta ayarları otomatik yeniden uygulama.
- **Zengin Sistem Tepsisi:**
  - Sol tıkla kontrol panelini açar/gizler (pencere kapalıyken arka planda neredeyse sıfır kaynak tüketir).
  - Sağ tık menüsünden pil sınırı, fan modu, performans profili ve klavye temalarına anında erişim sağlar.

## Gereksinimler

- **İşletim Sistemi:** Windows 10 veya Windows 11 (64-bit)
- **Çalışma Zamanı / SDK:** [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (kaynaktan derlemek için .NET 9.0 SDK)
- **Yetki:** Acer WMI donanım arayüzüne erişim için **Yönetici (Administrator)** yetkisi
- **Donanım:** Acer Nitro 5 (AN515-58) üzerinde geliştirilmiş ve test edilmiştir. Aynı Acer WMI arayüzünü kullanan diğer Nitro / Predator modellerinde de kısmen veya tamamen çalışabilir.

## Kurulum / Kaldırma

PowerShell üzerinden kurulum script'ini çalıştırabilirsiniz (gerekirse otomatik olarak yönetici yetkisi ister):

```powershell
pwsh scripts\install.ps1     # derler, kurar, oturum açılışında tepside otomatik başlatır
pwsh scripts\uninstall.ps1   # durdurur ve kaldırır
```

- **Kurulum dizini:** `%ProgramFiles%\AcerKontrol`
- **Ayar dosyası:** `%APPDATA%\AcerKontrol\settings.json`

## Geliştirme ve Testler

Projeyi yerelde derlemek ve birim testlerini çalıştırmak için:

```powershell
dotnet build
dotnet test
```

### Proje Yapısı

```text
Acer/
├── src/AcerKontrol/       # Ana Windows Forms uygulaması (WMI, fan, RGB, pil, tepsi, UI)
├── tests/AcerKontrol.Tests/ # Birim testleri (codec, pil, ayarlar, durum metinleri)
├── scripts/               # Kurulum ve kaldırma PowerShell betikleri
└── docs/                  # Tasarım ve plan dokümanları
```

## Katkıda Bulunma

Hata bildirimleri, yeni model uyumluluk raporları ve Pull Request'ler her zaman memnuniyetle karşılanır! Ayrıntılar için [CONTRIBUTING.md](CONTRIBUTING.md) dosyasına göz atabilirsiniz.

## Sorumluluk Reddi

Bu proje **resmi bir Acer ürünü değildir** ve Acer Inc. ile herhangi bir bağlantısı yoktur. Uygulama donanım ayarlarını (fan hızı, güç profili, şarj sınırı vb.) doğrudan WMI üzerinden kontrol eder; kullanım sorumluluğu kullanıcıya aittir.

## Lisans

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.
