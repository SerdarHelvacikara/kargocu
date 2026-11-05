# Kurmalı Kargo v0.9.0 bölge ve garaj notları

Oyuncu ana menüde Garaj ve şehir düğmesini açar. Bölgeler sekmesi başlangıç mahallesini ve üç yeni teslimat haritasını gösterir. Oyuncu kilitli kartın bedelini onaylar. Sonra açılmış bölgenin Tura başla düğmesine basar. Aktif teslimatın yerine yeni tur açılacaksa ayrıca onay gösterilir.

| Bölge | Açma bedeli | Teslimat başına temel ek ücret | Süre katsayısı |
| --- | ---: | ---: | ---: |
| Başlangıç | 0 | 0 | 1,00 |
| Çarşı | 450 | 12 | 0,97 |
| Çin Mahallesi | 900 | 24 | 0,90 |
| Plaza | 1.600 | 42 | 0,81 |

Bölgeler garajdan seçilen ayrı haritalardır. Aktif bölgenin kendi depo, alış ve teslimat noktaları vardır. Diğer bölge geometrileri pasiftir. Yeni tur kaydı bölgeyi taşır. v6 ve daha eski kayıtlar başlangıç mahallesinde açılır.

uGUI garajı araç, boya, dekor, drone, iş, mağaza, bölge ve depo sekmelerini bir araya getirir. Canlı 3D kamera araçları ve boyaları satın almadan denetler. Azaltılmış hareket ayarı önizleme dönüşünü durdurur. Mağaza fiyatı cihaz mağazası tarafından geldiğinde gösterilir. Windows deneme profili TEST fiyatlarını kullanır.

Depo 10 seviyelidir. Her kademe garaj önizlemesine yeni bir lojistik kasası ekler, teslimata başlamadan önceki yükleme beklemesini 0,045 saniye kısaltır, darbe hasarı katsayısını 0,025 düşürür ve drone çalışma hızını 0,035 artırır. Çoklu paket ve çalışan kurye, daha geniş depo kapsamına kalır.
