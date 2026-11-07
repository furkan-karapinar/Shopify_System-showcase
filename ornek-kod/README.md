# Örnek kod — PricingCore

Bu klasör, asıl (gizli) projeden alınmış küçük ve **işletmeye özgü hiçbir bilgi içermeyen** bir parçadır: atomik ayar dosyası yazımı (`AtomicFile`) ve fiyat değişikliği doğrulama kuralları (`PriceValidator`).
API anahtarı, mağaza adresi, müşteri verisi ya da ticari iş mantığı içermez; tek başına derlenir ve test edilir.

| Dosya | Ne yapar |
|---|---|
| [`AtomicFile.cs`](src/PricingCore/AtomicFile.cs) | Ayar dosyasını önce geçici dosyaya yazar, sonra `File.Move(overwrite)` ile hedefin üstüne taşır. Güç kesilse ya da servis ile masaüstü uygulaması aynı anda dosyaya erişse bile dosya yarım/boş görünmez. Kilitli dosyada kısa yeniden denemeler yapar. |
| [`PriceValidator.cs`](src/PricingCore/PriceValidator.cs) | Toplu fiyat değişikliğinde hataları (boş, ≤ 0, 2'den fazla ondalık) kaydı engelleyen *hata*, şüpheli durumları (%50'den büyük değişim, üstü çizili fiyat ≤ fiyat) onay penceresinde gösterilen *uyarı* olarak ayırır. |
| [`PriceValues.cs`](src/PricingCore/PriceValues.cs) | Doğrulayıcının kullandığı küçük değer tipi. |

Testler: eşzamanlı okuyucunun hiçbir zaman yarım içerik görmediğini, BOM yazılmadığını ve sınır değer fiyat kurallarını doğrular.

## Çalıştırma

Gerekli: [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
cd ornek-kod
dotnet test
```
