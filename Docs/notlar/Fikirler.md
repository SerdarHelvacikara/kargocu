# Kurmalı Kargo — oyun fikirleri

## v0.8 kaynak kapsamı

- Sekiz tohumlu alış noktası, uzaklığa bağlı süre ve son saniyelerde tik tak. Önceki tur kayıtları depodan devam eder.
- Köpekli ara sokaklarda uyarı, havlama, kısa panik kaçışı ve sıyrılma bonusu. Dinozor köpekleri ürkütür.
- Bacalı fabrika ve daha iyi kazandıran ağır yük, belediye meydanı, çeşitli evler ve yetişkinlerin oynadığı halı saha.
- Müşteri yüz ifadeleri, konuşma balonları, teslimat serisi ve konfeti. Azaltılmış hareket seçeneği.
- Jetonla ejderha ve dinozor açma. Kiralama ve gerçek para ekranlarının canlı servis kurulumu ile cihaz doğrulaması yayın devrinde. Windows deneme profilinde mock prova var.
- Güncel Windows test durumu Gelistirme-Devam.md dosyasında.

## v0.7 tasarım arşivi

Aşağıdaki maddeler v0.7 tasarımını kaydeder. v0.8 değişiklikleri yukarıdadır.

## Uygulandı

### Temel teslimat ve komedi

- Tek parmak/fare sürükleme veya WASD / ok tuşlarıyla kolay sürüş.
- Pasta, saksı ve çay takımından oluşan üç teslimatlık tur.
- Zaman yalnızca bonus; geç kalmak temel teslimat ücretini silmez.
- Görünür hasar: kayan pasta katları ve krema, eksilen yapraklar, sallanan çay takımı.
- Her paket için sağlam / hafif hasarlı / ağır hasarlı olmak üzere dokuz müşteri tepkisi.
- Türkçe ve İngilizce metinler, cihaz diliyle başlangıç ve kaydedilen dil tercihi.

### Kendi garajın

- Ayrı, üstü açık 3D atölye/garaj ve seçilen aracın park görünümü.
- Araç, drone, dekor ve işletme işi sayfaları ile oda önizlemesi.
- 14 sabit dekor: ilk dokuz ürüne Filiz dost posteri, Bulut dost posteri, Boğaz manzarası, Sağlam paket kupası ve Paket otomatı eklendi.
- Dekor fiyatları 40 / 65 / 85 / 110 / 135 / 160 / 180 / 210 / 240 / 275 / 300 / 340 / 380 / 440 jeton.
- Yeni minik dostlar özgün karakterlerdir; Pokémon veya başka oyunun karakter/markaları değildir. Manzara köprü, vapur ve gün batımıyla koddan oluşturulur.
- Satın alma anında odada görünme, ücretsiz göster/gizle ve kalıcı sahiplik.
- Serbest yerleştirme veya yeni mülk/bina sistemi bu sürümde yok.

### Dört araç ve düşen kargo

- Tatlı scooter: ücretsiz, 40 temel jeton, dengeli sürüş.
- Fırlama scooter: ücretsiz, %32 hızlı, 35 temel jeton, darbeden daha fazla hasar ve sert çarpışmada düşen paket riski.
- Mini kargo van: 300 jetonla açılan farklı 3D model; **(45 temel + hız + sağlamlık) ×2** toplam teslimat kazancı. Hız ×0,88; hızlanma ×0,65; darbe hasarı ×0,65.
- Koca teker: 700 jeton, 40 temel ücret, ×1 teslimat çarpanı. Daha geniş gövde ve dönüşler; hafif darbeleri yumuşatırken sert çarpışmada hasar artar. Kargo bağlı kalır.
- Paket yüklüyken veya yerdeyken araç değiştirilemez; teslimattan sonra sonraki paket için seçilir.
- Düşen paket erişilebilir bir yere konur, işaretçi ona döner. Geri dönüp almadan teslimat olmaz. Geri almak hasarı/süreyi yenilemez.
- Araç rengi, görünüm ve sahiplik kaydedilir. Aynı ürüne dönüşte yeniden ücret kesilmez.

### Drone yardımcısı

- 250 oyun jetonuyla açılan otomatik yardımcı; ayrı uçuş kontrolü yok.
- Garajdan göreve gönderilir. Oyun sürüş durumundayken 30 saniye ilerler; menü/mola/ayarlar ve kapalı uygulamada ilerlemez.
- Bir tamamlanan görev bir kez 25 jeton verir; ardından yeniden görev gönderilebilir.
- Sahiplik ve görev ilerlemesi tur kaydından ayrı tutulur. Eski turu yükleyerek tamamlanmış drone ödülü yeniden alınamaz.
- 3D uçuş ve taşınan paket görünümü vardır. Şarj standı bağımsız bir dekor ürünüdür.

### Bazen değişen sokaklar

- Yaklaşık iki kat alan, dış sokak halkası ve iki tuğlalı ara sokak. Altı adresten her tur için üçlü rota seçilir; normal tur bir merkez ve en az bir dış mahalle adresi içerir.
- Her yeni tur tohumla seçilen dört durumdan biri: sakin / yalnız kamyon / yalnız belediye çalışması / ikisi birlikte.
- Çalışma, üç sabit güvenli sokak noktasından birinde başlar. Hareket sırasında oyuncunun altında yeni yol kapatılmaz.
- Kamyon varsa 8–18 saniyelik başlangıç beklemesi, toplama durakları ve oyuncuya yol verme davranışı vardır.
- Güzergâh, seçilen çalışma ve mevcut sabit engelleri dolaşacak şekilde belirlenir.
- Depo/teslimat halkaları ve alternatif yollar korunur. Olaylar mini haritada görünür.
- Aynı turun kaydı devam ettirilince sokak seçimi ve saati korunur. Menü/mola/ayarlar olayları dondurur.

### Köpekli kestirmeler ve komik takip

- İki isteğe bağlı ara sokakta tur tohumuyla seçilen köpek varlığı.
- Pati işareti, görünür köpek ve 1,1 saniyelik uyarıdan sonra yaklaşık 2,7 saniyelik takip.
- Kısa sentezlenmiş komik müzik; ısırma, can veya kargo hasarı yok. Yakalanma bir kez, iki saniyelik 0,78× hız etkisi verir.
- Güvenli alternatif sokaklar, yolunu geri izleyen köpekler ve menülerde donan hareket/etki saati.

### Tekrarlanabilir işletme işleri

- 3 teslimat → 90 jeton.
- En az %90 sağlamlıkla 2 teslimat → 80 jeton.
- Dış mahalleye 2 teslimat → 120 jeton.
- 2 drone görevi → 70 jeton.
- Ödül alırken gereken ilerleme düşülür, fazlası korunur. İş yeniden tamamlanabilir; aynı ilerleme sınırsız ödül üretmez. Sayaçlar turdan ayrı saklanır.

### Bol jetonla elle deneme

- Bol-Jetonlu-Deneme.cmd, `-playtest` ile ayrı `__kk_playtest__` profili açar.
- İlk açılış 50.000 jeton; menüde yalnız bu profile çalışan +50.000 jeton düğmesi.
- Normal profil ve ekonomisi ayrı kalır. Gerçek ilerlemeyi değerlendirmek için standart Windows-v07 EXE açılır.

### Tamamlama ve kullanılabilirlik

- Yarım turu, paketi, hasarı, süreyi ve ara sonuç ekranını yerel kayıtla devam ettirme.
- v5 tur kaydında üç adresli rota ve teslimat çarpanı. v1/v2 sakin sokaklarla, v3/v4 mevcut sokak durumuyla taşınır; eski v4 makbuzu ×1 kalır ve geriye dönük iki kat ödeme oluşturmaz.
- Mola, ses düzeyi, hareket azaltma, üç sayfalı öğretici, iki onaylı sıfırlama.
- Dar ekranlar için mantıksal yüksekliği 360 birim olan kompakt düzen, büyük dokunma alanları, kaydırma ve oda önizlemesi.
- Güncel çevrimdışı gizlilik açıklaması ve doğrulanmış destek e-postası.
- Windows çalışma ve ayrı süreçte kayıt testi altyapısı. v0.6'nın testleri tarihsel kayıttır; v0.7 son tam çalışma/telefon sonucu PENDING. Gerçek iPhone testi henüz yapılmadı.

## Henüz uygulanmadı

### Gerçek gelir entegrasyonu

Mevcut dükkân teslimat jetonu kullanır. Reklam veya gerçek para ile satın alma SDK'sı eklenmedi. Kullanıcının sonraki hedefi basit satın almalar ve reklam geliridir.

Küçük kapsam için adaylar: doğrudan fiyatı görünen dekor/tema paketleri, scooter/drone kaplamaları ve isteğe bağlı ödüllü reklam. Bunlar mağaza ürünleri, hak geri yükleme, ödülün tek kez verilmesi ve gerçek cihaz testleri tamamlanınca canlı özellik olur.

### Müşteriyi idare etme

Sorunlu teslimatta yolda yaşananlarla bağlantılı iki-üç komik cevap. Seçilen açıklama müşteri tepkisini etkileyebilir; fiziksel hasarı ortadan kaldırmaz. Mevcut müşteri tepkileri hazır metindir; cevap seçme sistemi yoktur.

### Küçük işletmeyi büyütme

Yeni depo/atölye veya başka mülk, garaj alanı genişlemesi ve yeni mahalleler düşünülebilir. Serbest eşya yerleştirme editörü, genel trafik, çevrim içi çok oyunculu ve büyük ekonomi sistemi bu küçük sürümün kapsamına alınmadı.

### Diğer komedi fikirleri

Köpekli kestirme ve kısa takip v0.7'de eklendi. Koli tekmeleme, müşteri önünde başka komik sakarlıklar ve özel teslimat konuşmaları ileride değerlendirilebilir; cevap seçme sistemi henüz yoktur.

### UFO aracı

Yalnız gelecekte değerlendirilebilecek bir adaydır; şu anda sürülebilir UFO veya UFO satın alma ürünü uygulanmadı.

Sonraki öncelik yeni sistem eklemekten önce gerçek iPhone kontrolü, performans/ergonomi ve yayın paketinin tamamlanmasıdır. Yayın devri Claude-Yayin-Devir.md içindedir.

