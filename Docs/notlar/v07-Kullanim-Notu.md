# Kurmalı Kargo — Windows v0.7.0

Mahallede pasta, saksı ve çay takımı teslim et. Paketini sağlam götür, jeton kazan; garajını dekore et, drone yardımcını ve minivanını aç.

## Hemen oyna

**Windows-v07/KurmaliKargo.exe dosyasını aç.** Unity editörü veya internet gerekmez. ZIP kullanıyorsan önce klasörün tamamını çıkar; EXE, DLL ve KurmaliKargo_Data birlikte kalmalı.

Önceki Windows sürümleri arşiv olarak korunur. Güncel oyun için Windows-v07 kullan. Onizleme.html eski v0.1 Canvas önizlemesidir; bu Unity sürümü değildir.

**Bütün ürünleri hızlı denemek için Bol-Jetonlu-Deneme.cmd kullan.** Bu başlatıcı `-playtest` modunu açar: ayrı `__kk_playtest__` profili ilk kez 50.000 jetonla başlar ve ana menüde **Deneme: +50.000 jeton** düğmesi görünür. Bu mod kendi ilerlemesini saklar; normal oyuncu profiline jeton veya ürün eklemez. Normal ekonomiyi görmek için standart EXE'yi aç; orada kazanç teslimat, drone ve işletme işlerinden gelir.

## İlk turun

1. Teslimata başla ile çık. İlk turda kısa öğretici açılır.
2. Depodaki ışıklı halkadan paketini al.
3. WASD / ok tuşlarıyla veya oyun alanına basıp sürükleyerek sür. Bırakınca durursun.
4. Hedef evin halkasına girince teslimat otomatik yapılır.
5. Sonraki paket için depoya dön. Üç teslimat bir turdur.

Süre yalnızca hız bonusudur. Geç kalınca oyun bitmez. Teslimat ücreti, araç temel ücreti + en fazla 25 hız + en fazla 35 sağlamlık jetonudur. Esc / Mola ile duraklatabilirsin; menüde Teslimata devam et yarım turu geri açar.

## Garaj: araçlar, drone, dekor ve işletme işleri

### Araçlar

| Araç | Açma bedeli | Temel teslimat ücreti | Sürüş |
|---|---:|---:|---|
| Tatlı scooter | Ücretsiz | 40 jeton | Dengeli başlangıç aracı. |
| Fırlama scooter | Ücretsiz | 35 jeton | %32 daha hızlı; sert darbede paket düşebilir. |
| Mini kargo van | 300 jeton | 45 jeton | Temel ücret ve bütün bonusları ×2; daha ağır ve korunaklı. |
| Koca teker | 700 jeton | 40 jeton | Büyük gövde, geniş dönüşler; hafif darbeye koruma, sert çarpışmada daha çok hasar. |

Minivan kazancı **(45 + hız bonusu + sağlamlık bonusu) × 2** olur. Örneğin 20 hız ve 30 sağlamlık bonusuyla 190 jeton verir. Hız çarpanı 0,88; hızlanma ve darbe hasarı çarpanları 0,65'tir. Koca tekerin hızı ×1,10, hızlanması ×0,78'dir; geniş dönüş ister ve sert çarpışmalar kargoya pahalıya mal olur. Bu araçta kargo bağlı kalır; teslimat çarpanı ×1'dir.

Araç renkleri ücretsiz / 120 / 180 jetondur; seçilen renk diğer araçlara da uygulanır. Satın alınmış araç veya renk yeniden ücretlendirilmez.

Paket taşırken, yere düşmüşken veya kaydedilmiş turda paket yüklüyken araç değişimi kilitlidir. Paket teslim edildikten sonra sonraki paket için araç seçebilirsin.

### Drone

250 jetonla açılır. Garaj → Drone → Göreve gönder ile otomatik teslimata çıkar; uçuş kontrolü gerekmez.

- Görev, teslimat oyunu açıkken geçen 30 aktif oyun saniyesinde tamamlanır.
- Menü, garaj, mola, ayarlar ve kapalı uygulama görev süresini ilerletmez.
- Tamamlanan görev bir kez 25 jeton verir. Yeni görev garajdan gönderilir.
- Sahiplik ve devam eden görev tur kaydından ayrı korunur. Eski turu devam ettirmek drone ödülünü yeniden üretmez.

### Dekor

14 sabit eşya alınca 3D odada görünür. Sahip olduğun dekoru ücretsiz gizleyip tekrar gösterebilirsin.

| Dekor | Jeton |
|---|---:|
| Saksı | 40 |
| Lamba | 65 |
| Halı | 85 |
| Tezgâh | 110 |
| Raf | 135 |
| Kargo tabelası | 160 |
| Koltuk | 180 |
| Kargo posteri | 210 |
| Drone şarj standı | 240 |
| Filiz dost posteri | 275 |
| Bulut dost posteri | 300 |
| Boğaz manzarası | 340 |
| Sağlam paket kupası | 380 |
| Paket otomatı | 440 |

Şarj standı bir dekor ürünüdür; drone yardımcısının sahipliğinden ayrıdır. Serbest eşya yerleştirme editörü yoktur.

Filiz ve bulut dostları özgün karakterlerdir. Manzara köprü, vapur ve gün batımı içerir; başka oyunun karakteri veya markası kullanılmaz.

### İşletme işleri

| İş | Gereken | Ek ödül |
|---|---|---:|
| Mahalle anlaşması | 3 teslimat | 90 jeton |
| Nazik eller | En az %90 sağlamlıkla 2 teslimat | 80 jeton |
| Yeni sokaklar | Dış mahalleye 2 teslimat | 120 jeton |
| Hava kargo | 2 tamamlanmış drone görevi | 70 jeton |

Şart dolunca ödül garajdan alınır. Gereken ilerleme düşülür, fazlası korunur; iş tekrar yapılabilir. Aynı ilerlemeyi tekrar ödüllendirmez. Sayaçlar turdan ayrı kaydedilir.

## Mahalle ve paketler

Mahalle yaklaşık iki kat büyüdü. Dış sokak halkası ve iki tuğlalı ara sokakla altı adres var: Limon Evi, Lavanta Evi, Nane Evi, Kiremit Apartmanı, Saatçi Atölyesi ve Çınar Köşkü. Her normal tur yine üç teslimattır: bir merkez adresi, bir dış adres ve farklı bir üçüncü adres seçilir. Kaydedilen rota devam ederken değişmez.

Her yeni tur için bir tohum seçilir. Sokaklar dört durumdan biriyle başlar: sakin / yalnız kamyon / yalnız çalışma / ikisi birlikte. Belediye çalışması üç önceden belirlenmiş güvenli konumdan birindedir; kamyon varsa başlangıç beklemesi 8–18 saniyedir. Alternatif sokaklar, depo ve teslimat alanları açık kalır.

Kamyon sabit güzergâhta kısa toplama durakları yapar ve oyuncuya yol verir. Çalışma ve kamyon mini haritada gösterilir. Yarım turu devam ettirince aynı sokak durumu ve olay saati korunur; tekrar rastgele seçilmez. Mola ve menülerde sokak hareketi durur.

Tuğlalı kestirmelerde bazen çizgi film köpekleri bulunur. Pati işareti ve kısa görünür uyarıdan sonra yaklaşık 2,7 saniyelik komik takip başlar; sentezlenmiş kısa müzik eşlik eder. Isırma veya paket hasarı yoktur. Yakalanmak en fazla bir kez, iki saniyelik 0,78× hız etkisi oluşturur. Sakin alternatif sokaklar açıktır; mola ve menülerde köpekler de durur.

Pasta virajda sallanır, darbede krema saçar. Saksı daha ağır hızlanır, hasarda yaprak kaybeder. Çay takımı daha kırılgandır. Üç paketin üç hasar seviyesi için dokuz müşteri tepkisi Türkçe/İngilizce bulunur.

Fırlama scooter ile sert çarpışmada paket arkaya düşebilir. İşaretçiyi takip edip geri al; hasar ve geçen süre korunur. Geri alınmadan müşteri teslimatı veya ödülü oluşmaz. Paket yerdeyken bonus saati işler. Duvara sürekli yaslanmak aynı darbeyi tekrar tekrar saymaz.

## Ekranlar ve kayıt

- Küçük ekranlarda mantıksal yüksekliği 360 birim olan kompakt sayfalar, büyük düğmeler ve kaydırılabilir listeler kullanılır.
- Garajın Araçlar / Drone / Dekor sekmeleri ve oda önizlemesi bulunur.
- Güvenli ekran alanı, dokunma ve hedef göstergeleri aynı koordinat dönüşümünü kullanır.
- Dil, ses, hareket azaltma, jetonlar, ürün sahipliği ve yarım tur bu cihazda saklanır.
- Kayıt, paket hasarını/süresini ve ara teslimat sonucunu korur; sonuç ekranını açmak tekrar ödeme yapmaz.
- Yeni v5 tur kaydı üç adresli rotayı ve sonuç çarpanını korur. Eski v1/v2 turlar sakin sokaklarla taşınır; v3/v4 sokak durumu korunur. Eski v4 teslimat makbuzu ×1 kalır: sonradan minivan çarpanı uygulanıp geriye dönük para verilmez.
- Ayarlar → İlerlemeyi sıfırla, iki açık onaydan sonra yalnız bu oyunun yerel ilerlemesini sıfırlar.

Destek adresi helvacikaraserdar@gmail.com. Destek düğmesi e-posta uygulamasını açar; otomatik mesaj göndermez.

## Doğrulama durumu

**v0.7 son tam çalışma/telefon koşusu: PENDING.** Root'un son görüntülü tam çalışma testleri sürüyor; gerçek JSON sonuçları gelince bu satır güncellenecek.

v0.7 gerçek EXE ile iki ayrı süreçli kalıcı kayıt testinde **29 yazma + 24 okuma kontrolü**, arayüz kontrolünde **6 kontrol geçti**. Drone gerçek teslimat kazancıyla alındı; yeni dekor ve işletme sayaçları da kayıttan geri geldi. Bu sonuçlar tam çalışma testi sayısıyla toplanmaz.

Aşağıdaki sayılar v0.6'nın tarihsel sonuçlarıdır; v0.7 için geçmiş sayılmaz.

v0.6 Windows uygulamasındaki tam çalışma testinde **276 kontrol geçti, 0 hata**: yaklaşık **148,31 saniye**. Kapsam; gerçek sürüş/çarpışma/teslimat, düşen paket, ödül, araç sahipliği ve kilidi, minivan gövde engeli, drone görevi ve tek ödülü, dokuz dekor, rastgele sokaklar, kayıt geçişleri ve ekranların duraklatma davranışıydı. Tarihsel sonuç: v06-Test-Sonucu.json. Yalnız C# derlemesi çalışma testi sayılmaz.

v0.6 kompakt telefon düzeninin ilk tam Windows çalışmasında 266 kontrol geçti. Son 1136×640 arayüz kontrolünde oda önizlemesi dahil **6 kontrol geçti**; Türkçe/İngilizce sayfa görüntüleri incelendi. Tarihsel sonuç: v06-Telefon-Arayuz-Testi.json. Bunlar Windows üzerinde telefon düzeni testleridir. **Gerçek iPhone'da çalıştırma, performans, ısınma veya pil testi yapılmadı.**

v0.6'da iki ayrı süreçte soğuk kayıt ve yeniden açılışta **20 yazma + 21 okuma kontrolü geçti, 0 hata**. Drone gerçek teslimat kazancıyla alınıp göreve gönderildi; devam eden görev, tur, sokak seçimi/saati ve tercihler v4 kaydından geri geldi. Tarihsel sonuçlar: v06-Kayit-Yazma.json ve v06-Kayit-Yeniden-Acilis.json.

İsteğe bağlı test: EXE'ye `-smokeTest -testOutput <tam-json-yolu>` ver. `-testScreenshot <tam-png-yolu>` ve `-previewFolder <tam-klasör>` eklenebilir. Testler kullanıcı kaydından ayrıdır. Ayrı süreç testleri `-persistenceCheckWrite` / `-persistenceCheckRead` ile yalnız kendi test tercih alanında çalışır.

## Unity projesi

UnityProject, **Unity 6000.6.7f1**, yerleşik render pipeline ile hazırlanmıştır. Görseller, modeller ve kısa sesler kodla üretilir. Ücretli varlık, dış API veya oyun sunucusu kullanılmaz.

1. Unity Hub → Add project from disk → UnityProject.
2. Assets/Scenes/Neighborhood.unity sahnesini açıp Play'e bas. Mahalle Play sırasında üretilir.
3. Gerekirse Kurmalı Kargo → Prepare prototype scene kullan.
4. Kurmalı Kargo → Build Windows prototype çıktısı Windows-v07 klasörüne yazılır.

PrototypeBuilder.Prepare() prototip yayıncı/Windows ayarlarını yazar; gerçek mağaza kimlikleri ayarlandıktan sonra yardımcıyı buna göre uyarlamak gerekir.

Temel kaynaklar: CourierGame.cs, CourierGame.Progress.cs, CourierGame.DeliveryExtras.cs, CourierGame.Companions.cs, CourierGame.Business.cs, StreetEvents.cs, DogStreets.cs, WorldBuilder.cs, CompactScreens.cs, GuiViewport.cs, GarageCatalog.cs, GarageVisual.cs ve araç/drone görsel bileşenleri. Yeni kurallar v07-Tasarim-Notlari.md içindedir.

## Yayınlama

Bu teslim, oynanabilir Windows oyunudur. **Gerçek reklam ve gerçek para ile uygulama içi satın alma entegre edilmedi.** Mevcut dükkân yalnız oynayarak kazanılan jeton kullanır.

iOS derlemesi/imzalama, gerçek cihaz testi, son ikon ve mağaza görselleri, yayıncının yasal adı ve herkese açık destek/gizlilik URL'leri son yayın işidir. Kullanıcının isteğiyle Claude'a devredilmiştir: Claude-Yayin-Devir.md. Güncel uygulanan fikirler ve sonraki kapsam Fikirler.md içindedir.

UFO, ileride değerlendirilebilecek bir araç fikridir; bu sürümde uygulanmadı.
