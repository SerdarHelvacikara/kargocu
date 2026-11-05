# Kurmalı Kargo

Sevimli, izometrik bir kurye oyunu. Unity 6, tamamen kodla kurulan dünya.

Oyunun yönü ve kararlar: [Docs/TASARIM.md](Docs/TASARIM.md).
Eski sürüm notları ve inceleme belgeleri: [Docs/notlar](Docs/notlar).
Her adımın ekran görüntüleri: `Docs/m1` … `Docs/m6`.

## Başka bir bilgisayarda açmak

1. **Unity 6000.6.7f1** kur (Unity Hub → Installs). Başka bir sürüm projeyi
   yükseltmek ister; aynı sürümü kullan.
2. Repoyu indir:
   ```
   git clone https://github.com/SerdarHelvacikara/kargocu.git
   ```
3. Unity Hub → **Add project from disk** → indirdiğin `kargocu` klasörü.
   İlk açılış paketleri indirdiği için birkaç dakika sürer.
4. `Assets/Scenes/Neighborhood.unity` sahnesini aç ve **Play**'e bas.

Windows sürümü almak için menüden **Kurmalı Kargo → Build Windows prototype**.
Çıktı, projenin bir üst klasöründe `Windows-v010/KurmaliKargo.exe` olur.

## Komut satırı bayrakları

| Bayrak | Ne yapar |
| --- | --- |
| `-playtest` | Ayrı kayıt alanında 50.000 jetonla açar, reklamlar TEST olarak anında geçer |
| `-smokeTest` | Eski tur sistemi için otomatik test |
| `-smokeTest -shiftTest` | Vardiya, lobi, mağaza, bölgeler ve köpek testi |
| `-smokeTest -v08Test` | Satın alma ve reklam testi |
| `-compactUI` | Telefon düzenini masaüstünde gösterir |
| `-testOutput <yol>` | Test raporunun yazılacağı JSON dosyası |
| `-shotDir <klasör>` | Vardiya testi sırasında ekran görüntüsü kaydeder |

Örnek: `KurmaliKargo.exe -smokeTest -shiftTest -testOutput sonuc.json`

## Kodun yerleri

- `CourierGame*.cs` — oyunun beyni (partial sınıf). Vardiya: `CourierGame.Shift.cs`,
  sokak köpeği: `CourierGame.StrayDog.cs`, rütbe ve hediye: `CourierGame.Lobby.cs`,
  araç fotoğrafları: `CourierGame.Photos.cs`.
- `ShiftHUD.cs` — vardiya ekranı, kart seçimi, vardiya sonu.
- `LobbyUI.cs` — lobi, mağaza, garaj ve şehir sayfaları. Ortak çizim: `ToyGui.cs`.
- `WorldBuilder*.cs` — haritalar; bölgeler `WorldBuilder.Districts.cs`,
  bölge insanları `DistrictLife.cs`.
- Reklam ve mağaza: `Commerce*.cs`, `UnityCommerceAdapters.cs`. Gerçek reklam ve
  ürün kimlikleri `CommerceConfiguration.cs` içinde, şimdilik boş.

## Repoda olmayanlar

Derlenmiş `.exe`'ler, eski sürüm zip'leri, Unity'nin `Library/` önbelleği ve
imzalama anahtarları bilerek repo dışında (`.gitignore`).
