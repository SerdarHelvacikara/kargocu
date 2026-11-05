# Kurmalı Kargo geliştirme sırası

Başlangıç noktası v0.8.0. Doğrulanmış sürüm, üç teslimatlık turlar, değişken paket konumları, altı araç, drone, eski kayıtların taşınması ve mock mağaza akışını içeriyor. Güncel derleme ve test kanıtı Gelistirme-Devam.md dosyasında. Canlı reklam ve ödeme cihaz doğrulaması bekliyor.

## A · Oynanış

- A1: CityLandmarks.StreetLife.cs, ana şehir saatine yaya, yetişkin bisikletli, sokak kedisi, yaklaşma tepkili güvercinler, çay köşesi, çamaşır ve simit tezgâhı ekler. Mevcut konuşma balonları kullanılır. Dekorlar çarpışma eklemez. Yeni kayıt şeması gerekmez.
- A2: v0.8.3, beş tohumlu sokak olayını oynanabilir sürüme ekler. Olay saati ve ördek ödülü kayıtla devam eder.
- A3: v0.8.4 acil, kırılgan, soğuk zincir, zincir, VIP ve sürpriz siparişleri ekler. Son test sonuçları Gelistirme-Devam.md dosyasında.
- A4–A8: turlar arası seri, güçlendirmeler, rakip, hava ve müzik. Olaylar geniş araç ve hedef erişimi korunarak yerleştirilmeli. Eylem/ödül durumu kaydedilmeli. Başlangıçta okul yerine kargo fabrikası korunur.

## B–D · İlerleme, bölgeler ve depo

- B: CourierGame.Business ve LocalProgress ile XP, rütbe, cihaz takvimine bağlı görev/ödül, koleksiyon, rekor ve Kış Şenliği. Cihaz saati geri alınırsa ödemeyi yeniden açma.
- C: v0.9.0 Çarşı, Çin Mahallesi ve Plaza için garajdan seçilen ayrı haritalar, farklı adresler, süreler ve ücretler ekler. Bölgeler arası kesintisiz yol, rütbe, bölgeye özel müzik ve ek NPC kapsamı açık. Her alanın ayrı rota, durak, ekonomi ve olay tablosu olur. Geniş gövde taramaları ile sınır ve kamera kontrollerini birlikte genişlet. Tur kaydını sürümle, eski turları başlangıç mahallesinde aç. Dışarıdaki aktörleri mesafeye göre uyut.
- D: v0.9.0 10 jetonla açılan depo seviyesi, görünür kasa birikimi, yükleme, darbe koruması ve drone hızı ekler. İşletme dalları ve çalışan kuryeler açık. Çoklu paket taşıma, aktif turun görev ve ödeme kurallarını değiştirdiği için önce tam kayıt ve makbuz testi. Ortalama teslimat gelirini gerçek sürüş süresiyle ölç, fiyat tablosunu bu veriye bağla.

## E–F · Arayüz ve ilk deneyim

- E: v0.9.0 uGUI ile yeni garaj, mağaza, işler ve depo sekmeleri, araç ve boya 3B önizlemesi. Geniş ürün kataloğu, koleksiyon ve özel teklif takvimi açık. GarageVisual sahnesini ayrı 3B ön izlemeye bağla. Canvas düzeni ve kaydırma dikey/yatay/tablet sınamalarına uygun. Yeni ürün kartları ortak arayüz kullanır. Alışverişte yerel fiyat ve açık onay. Mevcut HUD korunarak renk ve tipografi eşleştirilir.
- F: Okumadan öğreten başlangıç teslimatı, menü, ikon, açılış görseli, erişilebilirlik ve kalite. Gerçek cihaz dokunmatik ve kare süresi ölçümü.

## G · Teslim ve yayın

Her dilimde smoke ve persistence çalıştır. A1 testi mevcut -smokeTest -v08Test senaryosunun sonunda yürür. Git için yalnız kaynak/ayarlar ve belgeleri hazırla. Lisans, repo görünürlüğü ve uzak depoya gönderimi kullanıcıyla kararlaştır. Mağaza yükleme ve hesap adımları kullanıcı/Claude devrinde kalır.



