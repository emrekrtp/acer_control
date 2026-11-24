# Acer Kontrol'e Katkıda Bulunma

Acer Kontrol'e katkıda bulunmayı düşündüğünüz için teşekkürler!

## Nasıl Katkıda Bulunabilirsiniz?

- **Hata Bildirimi:** Karşılaştığınız bir hatayı laptop modeliniz (örn. AN515-58), Windows sürümünüz ve yeniden oluşturma adımlarıyla birlikte Issue olarak açın.
- **Özellik Önerisi:** Yeni bir özellik veya başka bir Acer modeli için WMI desteği önermek için Issue açın.
- **Kod Katkısı (Pull Request):**
  1. Depoyu fork'layın ve kendi dalınızı (`branch`) oluşturun:
     ```bash
     git checkout -b feat/yeni-ozellik
     ```
  2. Değişikliklerinizi yapın ve yeni davranışlar için mümkünse birim testi ekleyin.
  3. Tüm testlerin geçtiğinden emin olun:
     ```powershell
     dotnet build
     dotnet test
     ```
  4. Anlamlı commit mesajları kullanın (örn. `feat:`, `fix:`, `docs:`, `refactor:`).
  5. `main` dalına yönelik bir Pull Request açın ve yaptığınız değişikliği kısaca açıklayın.

## Kodlama Kuralları

- Proje **.NET 9.0** (`net9.0-windows`) ve **C#** nullable referans tipleri (`<Nullable>enable</Nullable>`) ile yazılmıştır.
- Donanım/WMI bağımlılığı olmayan kodlayıcı (codec) ve iş mantığı sınıflarını test edilebilir tutmaya özen gösterin.
- Kişisel yollar, kimlik bilgileri veya yerel IDE/araç dosyaları commit'lenmemelidir.
