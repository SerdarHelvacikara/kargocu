# Kurmalı Kargo — Yeni Yön (v1.0 hedefi)

Bu belge oyunun yönünü belirler. Kararlar burada yazılı; kod bu belgeye uyar.
Bir karar değişirse önce burası güncellenir.

## 1. Neden değişiyoruz

v0.9.1'de çok özellik var ama oyun "oynatmıyor". Üç temel eksik:

1. **Kaybetmek yok.** Gerilim yok, reklamla devam etme anı da yok.
2. **Tur içinde güçlenme yok.** Tur başında ne isen sonunda da osun.
3. **Döngü kısa ve tekdüze.** 3 teslimat → sonuç ekranı → aynı yollar.

Ayrıca: masaüstü düzeni (yatay, büyük paneller), ayar sayfasına benzeyen mağaza,
yoldan geniş araçlar ve 9 sürüm boyunca üst üste binmiş kayıt doğrulamaları
yeni özellik eklemeyi yavaşlatıyor.

**Karar:** Tema, karakterler, araçlar ve şehir görselleri kalıyor. Oynanış
çekirdeği, arayüz ve kayıt sistemi yeniden yazılıyor.

## 2. Oyunun tek cümlesi

> Tek parmakla sür, şehirde yağan siparişleri yetiştir, her seviyede bir güç
> seç, yıldızların bitmeden vardiyanı tamamla.

## 3. Çekirdek döngü: Vardiya

- **Süre:** 4 dakikalık vardiya. Bitirince "Vardiya tamam!" ve büyük ödül.
- **Siparişler:** Şehrin farklı noktalarında sürekli belirir. Haritada ikon,
  ekran kenarında yön oku. Her siparişin kendi geri sayımı var.
- **Taşıma:** Başta 1 paket. Güçlerle 3'e kadar çıkar.
- **Kaybetme:** 3 yıldız. Geciken veya tamamen kırılan paket 1 yıldız götürür.
  Yıldız biterse vardiya biter.
- **Devam:** Vardiya biterse bir kez "Reklam izle, 1 yıldızla devam et".
  Ana reklam gelir anı burası.
- **Seviye atlama:** Her teslimat XP verir. Seviye atlayınca oyun durur, 3 kart
  çıkar, biri seçilir (bkz. bölüm 4).
- **Zorluk eğrisi:** Dakika ilerledikçe sipariş sıklığı artar, süreler kısalır;
  köpekler, trafik ve sokak olayları yoğunlaşır. Son 30 saniye "Yoğun saat":
  müzik hızlanır, ödüller ×2.
- **Kombo:** Arka arkaya zamanında teslimat seriyi büyütür (×2, ×3, ×5).
  Gecikme seriyi bozar.

## 4. Güç kartları (vardiya içinde)

Her kartın 3 seviyesi var. Aynı kartı tekrar seçmek onu güçlendirir.

| Kart | Etki |
| --- | --- |
| Turbo | Hız +12% |
| Büyük çanta | +1 paket kapasitesi |
| Mıknatıs | Alma/teslim yarıçapı +40% |
| Sağlam bant | Darbe hasarı −25% |
| Fazla mesai | Sipariş süreleri +15% |
| Köpek maması | Köpekler kovalamaz, peşinden neşeyle gelir |
| Yardımcı drone | Uzaktaki küçük siparişleri kendi taşır |
| Bahşiş kavanozu | Jeton +20% |
| Kalkan | Bir sonraki çarpmayı yok sayar (dolar) |
| Kestirme haritası | Hedefe en kısa yolu gösteren çizgi |

## 5. Kontroller ve ekran

- **Yatay ekran (landscape) — karar 5 Kasım 2026'da değişti.** İzometrik şehir
  yatayda çok daha iyi okunuyor ve bütün ekranlar (garaj, ayarlar, telefon
  düzeni) zaten yatay için yapılmış. Dikeye geçmek şimdilik ertelendi.
- Ekranın herhangi bir yerine dokun-sürükle: yüzen joystick.
- Oyun HUD'u ince: üstte yıldızlar, vardiya süresi, jeton, XP çubuğu;
  altta joystick alanı. Oyun alanı ekranın en az %80'i.
- Arayüz uGUI ile yeniden yapılır. IMGUI yalnızca hata ayıklama için kalır.

## 6. Şehir: oynadıkça büyüyen tek dünya

- Bölgeler menüden **seçilmez**. Tek, kesintisiz bir şehir var.
- Kilitli bölge haritada görünür, bariyerin arkasındadır
  ("Kurye rütbesi 5'te açılır").
- Açılınca bariyer kalkar, kamera yeni bölgeye kısa bir tur atar; o vardiyadan
  itibaren siparişler oraya da düşer.
- Sıra: Başlangıç Mahallesi → Çarşı → Çin Mahallesi → Plaza → Liman.
- Uzak ve zor bölgeler daha çok öder. Plaza en kârlı ama en kalabalık bölge.
- Kamera vardiyada oyuncuyu takip eder; şehir ekrandan büyüktür.

## 7. Araç kuralı

- Tüm araçlar **aynı gövde ölçüsüne** sığar: en fazla 1,2 × 2,2 birim.
  Monster truck dahil.
- Yollar en az 4 birim genişliğindedir (en geniş aracın 3 katından fazla).
- Fantastik araçlar sürüşte kanat/kuyruk katlar; açık hâl yalnız kalkış ve
  teslim animasyonunda görünür.
- Araçlar istatistikle ayrılır (hız, ivme, dayanıklılık, kapasite, özel yetenek),
  boyutla değil.

## 8. Kalıcı ilerleme (meta)

- **Kurye rütbesi (1–50):** Vardiya XP'si birikir. Rütbe bölge, araç ve kart açar.
- **Araçlar:** Her aracın 5 yükseltme seviyesi (jetonla).
- **Depo:** 10 seviye; vardiya başı bonusları (başlangıç kalkanı, +1 yıldız
  şansı, ilk kartı seçme hakkı).
- **Günlük görevler (3)** ve **haftalık görev**.
- **7 günlük giriş takvimi.**
- **Koleksiyon albümü:** müşteriler, binalar, olaylar.

## 9. Para birimleri ve gelir

- **Jeton:** Oynayarak kazanılır. Yükseltmeler ve araçlar.
- **Elmas:** Az miktarda oynayarak (görev, takvim) ve satın alarak. Kozmetik,
  hızlandırma, ikinci "devam" hakkı.
- **Ödüllü reklam (oyuncu ister):**
  - Vardiya bitti → devam et (vardiya başına 1)
  - Vardiya sonu ödülü ×2
  - Günlük ücretsiz sandık
  - Fantastik aracı 1 vardiya dene
- **Geçiş reklamı:** Yalnız vardiya sonu ekranından sonra, en sık 4 dakikada bir,
  ilk 3 vardiyada hiç. Vardiya içinde asla.
- **Satın alma:** Reklamları kaldır, elmas paketleri, başlangıç paketi, ileride
  sezon kartı. Fantastik araçlar kalıcı satın alınabilir; jetonla da (uzun yoldan)
  açılabilir.
- Hedef yaş: 12+ genel kitle. Çocuklara yönelik reklam kuralları gerekirse
  yayın öncesi yeniden değerlendirilir.

## 10. Lobi ve mağaza (A kalite hedefi)

- Ana ekran = lobi. Ortada kuryen ve aracın, arkada canlı mahalle.
  Büyük **VARDİYAYA BAŞLA** düğmesi.
- Altta 5 sekme: **Mağaza · Garaj · Başla · Şehir · Görevler**.
- Üst bar: rütbe rozeti, XP, jeton, elmas, ayarlar.
- Mağaza: büyük görselli teklif kartı en üstte; altında günlük ücretsiz
  (reklamlı) sandık; elmas ve jeton paketleri ızgarası; "reklamları kaldır".
  "Satın alımları geri yükle" Ayarlar'da.
- Garaj: araç ızgarası, nadirlik çerçeveleri, istatistik çubukları, büyük 3D
  önizleme, yükselt düğmesi.
- Kırmızı rozetler, akan sayaçlar, uçan jeton ikonları, düğme yaylanması.
  Azaltılmış hareket ayarında hepsi sadeleşir.

## 11. Kayıt

- Yeni ve sade kayıt: `kr_` önekli anahtarlar, sürüm alanı, tek JSON.
- Eski oyunculardan taşınanlar: jeton, sahip olunan araçlar ve boyalar,
  satın alınmış ürünler. Yarım kalmış eski turlar taşınmaz.
- Vardiya ortasında uygulama kapanırsa vardiya biter; o ana kadarki teslimat
  jetonları zaten cüzdandadır.

## 12. Yol haritası

Her adım sonunda oynanabilir bir Windows sürümü ve test sonuçları olur.

| Adım | İçerik | Bitti sayılır |
| --- | --- | --- |
| M1 | Vardiya çekirdeği: sürekli siparişler, yıldız, kaybetme, devam, XP ve 3 kart (6 kart), dikey ekran, yeni HUD | 10 kişiden çoğu "bir daha" diyor |
| M2 | Araç ölçü kuralı, yol genişliği, kamera takibi, büyük başlangıç haritası | Hiçbir araç yoldan taşmıyor |
| M3 | Lobi, alt sekmeler, yeni mağaza ve garaj (uGUI) | Ekran görüntüleri A kalite referanslara yakın |
| M4 | Rütbe, bölge açılışları (Çarşı, Çin Mahallesi, Plaza), günlük görev, takvim | Oyuncu 3. günde de geri geliyor |
| M5 | Reklam ve satın alma bağlantısı, gizlilik metni, izin akışları | Gerçek cihazda test satın alma ve reklam geçiyor |
| M6 | Cila, ses, performans, mağaza görselleri, Android/iOS derleme | Yayın kontrol listesi tamam |

## 13. Ölçüler (yumuşak lansmandan sonra)

- 1. gün geri dönüş ≥ %35, 7. gün ≥ %10
- Ortalama oturum ≥ 8 dakika
- Ödüllü reklam: günlük aktif oyuncu başına ≥ 2
