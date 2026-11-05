# Kurmalı Kargo v0.7.0 — Claude için son yayın devri

Kullanıcının kararı: Codex küçük oyunu geliştirir, oynanabilir Windows sürümünü ve Unity kaynaklarını teslim eder. Son iOS/Android hazırlığı, gerçek cihaz doğrulaması, mağaza hesap/ürün bilgileri ve gönderim kullanıcıyla Claude tarafından tamamlanır. Bu belge geliştirme durumunu aktarır; mağazada yayınlandığı veya iPhone'da doğrulandığı anlamına gelmez.

## Açılacak sürüm

- Windows: Windows-v07/KurmaliKargo.exe. Tüm DLL ve Data dosyaları yanındaki klasörde kalmalı.
- Elle ürün denemesi: Bol-Jetonlu-Deneme.cmd → `-playtest`. İlk açılışta 50.000 jeton ve menüde görünür +50.000 jeton düğmesi; yalnız ayrı `__kk_playtest__` profiline yazar. Normal oyuncu profili ve kazanılmış jetonları ayrıdır.
- Kaynak: UnityProject, Unity 6000.6.7f1, yerleşik render pipeline.
- Sahne: Assets/Scenes/Neighborhood.unity. Mahalle, garaj ve modeller Play sırasında üretilir.
- Önceki sürümler ve test kayıtları arşiv olarak korunur. Onizleme.html eski v0.1 Canvas önizlemesidir.
- Kullanım ve fiyatlar: BENI_OKU.md. Uygulanmış/gelecek fikir ayrımı: Fikirler.md. Yeni kural ayrıntıları: v07-Tasarim-Notlari.md.

## v0.7 kaynak kapsamı

Teslimat: üç farklı paket, hasar animasyonları, üç teslimatlık tur, hız/sağlamlık bonusları, iki dilde dokuz müşteri tepkisi. Geç kalmak oyunu bitirmez. Yaklaşık iki kat büyüyen mahallede altı adres vardır; normal yeni tur bir merkez, bir dış mahalle ve farklı üçüncü adresten oluşur. Kayıt aynı rotayı devam ettirir.

Araçlar: iki ücretsiz scooter, 300 jetonluk minivan ve 700 jetonluk Koca teker. Minivanın **(45 temel ücret + hız + sağlamlık) ×2** toplam ödemesi vardır; yalnız temel ücret çarpılmaz. Hızı ×0,88, hızlanma/darbe hassasiyeti ×0,65'tir. Koca teker 40 temel ücret ve ×1 kazançla daha büyük gövde/geniş dönüş sunar; hafif darbeleri yumuşatır, sert çarpışmada hasar artar, kargo bağlı kalır. Fırlama scooter %32 hızlıdır, temel ücreti 35'tir ve sert darbede kargo düşebilir. Tatlı scooter 40 temel jetondur. Paket taşırken/yerdeyken veya yüklü kaydı devam ettirirken araç değişimi kilitlidir.

Drone: 250 oyun jetonuyla açılır, garajdan otomatik göreve gönderilir. Yalnız GamePhase.Playing sırasında geçen 30 saniyede bir görevi tamamlar; 25 jetonu bir kez verir. Menü/mola/kapalı uygulamada çevrimdışı gelir birikmez. Drone sahipliği, tamamlanma sayısı ve görev süresi tur anlık görüntüsünden ayrı kalıcı kayıttır. Eski turu yüklemek görev ödülünü yeniden üretmez.

Garaj: araç, drone, dekor ve işletme işi sayfaları; seçilen aracı gösteren oda, **14 sabit dekor** ve ücretsiz göster/gizle. İlk dokuz ürünün kimlikleri korunur. Filiz dost posteri 275, Bulut dost posteri 300, Boğaz manzarası 340, Sağlam paket kupası 380 ve Paket otomatı 440 jetondur. Minik dostlar özgün karakterlerdir; Pokémon veya başka oyunun markalı karakterleri kullanılmaz. Şarj standı drone ürününden bağımsız kozmetiktir. Serbest yerleştirme editörü yoktur.

İşletme işleri: 3 teslimata 90 jeton; en az %90 sağlamlıkla 2 teslimata 80; dış mahalleye 2 teslimata 120; 2 drone görevine 70 jeton. Garajda ödül alınırken gerekli sayaç düşülür, fazlası korunur ve iş tekrarlanabilir. Sayaçlar tur kaydından ayrı kalıcı ilerlemedir.

Sokaklar: tur başında özel System.Random tohumu ile dört mod (sakin/kamyon/çalışma/ikisi) ve üç güvenli çalışma konumu. Kamyonun 8–18 saniyelik ilk beklemesi, toplama durakları, güvenli yol verme ve çalışma konumuna göre rota seçimi bulunur. Menü ve diğer duraklatan ekranlarda saati ilerlemez. Tur kaydı aynı tohumu, konumu ve olay saatini korur.

Köpekli kestirmeler: yeni kuzey/doğu tuğlalı ara sokaklarında tur tohumuna bağlı köpek varlığı, pati işaretleri ve 1,1 saniyelik görünür uyarı. Yaklaşık 2,7 saniyelik kısa takip ve sentezlenmiş komik riff bulunur. Isırma/can/kargo hasarı yoktur; bir temas iki saniyelik 0,78× hız etkisi oluşturabilir. Sakin alternatif sokaklar açıktır. Köpek hareketi ve etki saati yalnız Playing sırasında ilerler.

Küçük ekranlar: mantıksal yüksekliği 360 birim olan kompakt düzen, büyük düğmeler, kaydırma, güvenli alan, sadeleştirilmiş sürüş başlığı ve garaj oda önizlemesi korunur. v0.7'nin genişleyen içerik ve ekranları için son root kontrolü beklenir; Windows küçük ekran kontrolü gerçek iPhone doğrulaması yerine geçmez.

Yerel kayıt: yeni **v5 tur şeması** üç adresli rotayı ve son teslimat çarpanını saklar. Eski v1/v2 turlar sakin sokaklarla taşınır; v3/v4 sokak durumu korunur. Eski v4 makbuzunun çarpanı ×1 kalır; eski minivan teslimatına geriye dönük çift ücret verilmez. Konum, paket, düşen kargo, hasar, süre ve ara sonuç devam eder. Ara sonucu yeniden yüklemek ödül vermez. İşletme sayaçları, araç/drone sahipliği ve görev ilerlemesi turdan ayrı saklanır.

Elle deneme modu otomatik test değildir. İlk 50.000 jeton ve menüden ekleme yalnız `-playtest` profilindedir; standart açılışta ürünler gerçek oyun kazancıyla alınır. Mağazaya gönderilecek uygulama profilinde bu geliştirme başlatıcısı/para düğmesi kullanılmamalıdır.

## Tamamlanan testler ve açık doğrulama sınırı

**v0.7 son görüntülü tam Windows/telefon koşusu: PENDING.** Root'un son tam çalışma ve paket kontrolleri sürüyor. Bu koşuların sayıları gerçek v0.7 JSON sonuçları gelince güncellenmeli.

v0.7 gerçek EXE'nin iki ayrı süreçli v5 kayıt testinde **29 yazma + 24 okuma kontrolü geçti**. Drone gerçek kazançla satın alınıp göreve gönderildi; yeni dekor sahipliği ve işletme kayıtları korundu. Arayüz koşusunda **6 kontrol geçti**. Bunları tam çalışma testiyle toplayıp tek sayı gibi sunma. Aşağıdaki sonuçlar yalnız v0.6'nın tarihsel kayıtlarıdır.

v0.6 teslim uygulamasının tam Windows çalışma testi: **276 kontrol geçti, 0 hata**, yaklaşık **148,31 saniye**. Gerçek controller hareketi/çarpışması, teslimat ödemeleri, düşüş/geri alma, araç satın alma ve kilidi, minivan gövde engeli, drone görevi/tek ödül, dokuz dekor, rastgele sokak seçimi/rota/yol verme, kayıt uyumu ve duraklatma kapsamdaydı. Tarihsel sonuç v06-Test-Sonucu.json.

v0.6 kompakt telefon düzeninin ilk tam Windows koşusunda 266 kontrol geçti. Son 1136×640 arayüz koşusunda oda önizlemesi ve dükkâna dönüş dahil 6 kontrol geçti. Bu sayılar farklı koşuların sonuçlarıdır; toplanıp tek bir test sayısı gibi sunulmamalı. Tarihsel görüntüler v06-Telefon-Son-Ekranlar, rapor v06-Telefon-Arayuz-Testi.json.

v0.6'nın iki ayrı süreçli v4 kayıt/yeniden açılışında **20 yazma + 21 okuma kontrolü geçti, 0 hata**. Gerçek teslimat kazancıyla alınan drone'un devam eden görevi, tur, sokak modu/saati, araç ve ayarlar korundu. Tarihsel sonuçlar v06-Kayit-Yazma.json ve v06-Kayit-Yeniden-Acilis.json.

Soğuk kayıt testleri LocalProgress içinde ayrı __kk_saveprobe__ tercih alanı kullanır. Tam çalışma ve arayüz testleri bellekteki test durumunu kullanır; normal kayda yazma kapalıdır. Bu kontroller normal oyuncu kaydını silmez veya yapay jetonla doldurmaz. Sadece kaynak derlemesini, gerçek EXE çalışmasını ve henüz yapılmamış telefon testini ayrı değerlendir.

Yapılmadı: gerçek iPhone / iPad / Android cihaz çalıştırması, iOS imzalı yayın adayı, mağaza incelemesi, canlı reklam/ödeme testi, gerçek cihaz ısınma/pil ve uzun oturum performans ölçümü.

## Gerçek para ve reklam durumu

Mevcut dükkân **oynayarak kazanılan jeton** kullanır. Minivan, drone, renk ve dekor açılması StoreKit veya Google Play Billing satın alımı değildir. **Reklam ve gerçek para ile satın alma SDK'sı henüz entegre edilmedi.**

Kullanıcının uzun vadeli isteği basit satın almalar ve reklam geliridir. İlk yayın stratejisini buna göre sonlandır. Olası küçük kapsam: açık fiyatlı kozmetik/dekor paketleri ve isteğe bağlı ödüllü reklam. Canlı ürün/ad-unit kimlikleri, hak geri yükleme, iptal/bekleyen işlem, ağ kaybı ve tek seferlik ödül gerçek cihazda denenmeden bunları tamamlanmış özellik olarak sunma.

SDK eklenirse gizlilik metni, veri etiketi, yaş anketi ve izinler son davranışa göre yeniden değerlendirilir. Platform fiyatını veya mağaza ürün kimliğini uydurma. Yeni ücretli servis/API/varlık alınmasına izin verilmedi; bu devir bunları satın alma yetkisi vermez.

## Hesap ve kimlik bilgileri

- Destek e-postası kullanıcı tarafından doğrulandı: helvacikaraserdar@gmail.com.
- Yayıncı şahıs; yasal adı henüz verilmedi. E-postadan isim türetme. KurmaliKargoPrototype gerçek yayıncı adı değildir.
- Önceki konuşmada geliştirici hesapları ile Mac/iPhone erişimi bulunduğu belirtildi; bu Windows çalışmasında hesaplara bağlanılıp doğrulanmadı. Son gönderimde doğru hesap, takım, sözleşme ve imzalama bilgilerini kontrol et.
- Türkçe Kurmalı Kargo, İngilizce Wobbly Delivery çalışma adlarıdır; mağaza/marka kullanılabilirliği doğrulanmadı.
- Giriş gerektirmeyen herkese açık HTTPS Privacy Policy ve Support URL henüz tamamlanmalı. Uygulama içi gizlilik ekranı ve e-posta adresi bu URL'lerin yerine geçmez.

## Mac ve gerçek cihaz adımları

1. Son kaynakları Mac'e taşı: Assets, Packages, ProjectSettings. Windows Library, Temp, Logs ve UserSettings klasörlerini taşıma.
2. Unity 6000.6.7f1 ve gerekli hedef platform modüllerini kur. Önce sahneyi Play ile doğrula.
3. Assets/Editor/PrototypeBuilder.cs, v0.7 Windows prototip kimliği, render/input ayarları, shader koruma materyali ve yatay yön ayarlarını yazar. Gerçek bundle/yayıncı ayarlarını girdikten sonra bu yardımcıyı kontrolsüz yeniden çalıştırma; yayın profilini ayır.
4. Apple/Google'ın o günkü resmi hedef SDK ve gönderim koşullarını doğrula. Bundle ID, sürüm/build numarası, imzalama ve desteklenen cihaz/yönleri gerçek hedefe göre ayarla.
5. iPhone'da soğuk açılış, dokunma/sürükleme, iki yatay yön, çentik/home indicator, öğretici, mola, ayarlar/gizlilik kaydırması, TR/EN metin taşmaları ve garaj sekmelerini dene. Oda önizlemesinin küçük ekranda görünür olduğuna bak.
6. Dört araçla teslimat; minivanın bütün bonuslarla ×2 kazancı; Koca tekerin geniş dönüşleri ve hafif/sert darbe farkı; geniş gövdelerin yeni sokaklardan geçişi; hızlı araçta düşen pakete geri dönüş; köpekli kestirmede uyarı, kısa takip ve duraklatma; kamyonun oyuncuya yol vermesini doğrula.
7. Drone'u gerçek kazançla açıp göreve yolla. Menü/arka planda sürenin donmasını, kapatıp açınca devamı ve aynı görevin yalnız bir kez ödeme yapmasını doğrula. 14 dekorun sahipliği/görünümü, işletme işlerinin ödül tüketimi, altı adresten seçilen üçlü rota ve araç rengi kayıtta korunmalı. Eski v4 makbuzunu taşımanın para üretmediğini kontrol et.
8. Isınma, bellek ve kare süresini gerçek cihazda ölç. PC veya telefon boyutlu pencere bu ölçümün yerine geçmez.

## Gizlilik ve mağaza paketi

Bu kaynaklarda oyun hesabı, oyun sunucusu, reklam/analitik/ödeme SDK entegrasyonu yoktur. Yerel tercihlere LocalProgress üzerinden erişilir; Unity'nin PlayerPrefs kaydı kullanılır. Unity diagnostics/hizmet ayarlarını ve gerçek iOS çıktısındaki privacy manifest/User Defaults gerekçelerini kontrol et. Son veri etiketini hedef uygulama incelenmeden kesinleştirme.

Son ikon, açılış ekranı, gerçek v0.7 oyun görüntüleri, TR/EN açıklamalar ve inceleme notları hazırlanmalı. Kullanıcının önceki Animal Run ret mesajını somut gerekçesiyle değerlendir; kabul garantisi verme. Hay Day, Crossy Road veya Brawl Stars adlarını/markalarını uygulama kimliğine veya pazarlama görsellerine taşıma. Yeni dost posterleri özgün çizimlerdir; başka markanın karakteri gibi tanıtma.

Modeller, mahalle, araçlar, drone, paketler, müşteriler ve garaj primitive geometriyle kodla üretilir. Kısa sesler sentezlenir. Arayüz kodla çizilir; Unity'nin LegacyRuntime.ttf fontu kullanılır. Ücretli varlık veya dış üretim servisi eklenmedi.

Eski Yayin-Kontrolu.md, v0.2.1 denetim anının tarihsel kaydıdır; son iOS davranışının belgesi olarak kullanılmamalı. Yayın için güncel resmi kaynaklar ve gerçek hedef derleme esas alınır.

## Kod haritası

| Dosya | Görev |
|---|---|
| CourierGame.cs | Sürüş, teslimat, ödül, kamera ve ana döngü. |
| CourierGame.Progress.cs | v5 kayıt ve eski makbuz geçişi, ayarlar, öğretici, dekor hakları. |
| CourierGame.DeliveryExtras.cs | Dört araç, geniş gövde kontrolü, paket düşürme/geri alma ve sokak tohumu. |
| CourierGame.Companions.cs | Kalıcı drone görevi ve minivan sahipliği. |
| CourierGame.Business.cs | Üç adresli rota, işletme sayaçları/ödülleri, köpek müziği ve deneme jetonu. |
| StreetEvents.cs | Tohumlu olay seçimi, DTO, kamyon güvenliği ve çalışma konumları. |
| DogStreets.cs, WorldBuilder.cs | Genişleyen mahalle, altı adres, isteğe bağlı köpekli kestirmeler. |
| CompactScreens.cs, GuiViewport.cs | Küçük ekranlar, garaj sekmeleri ve ortak güvenli alan. |
| GarageCatalog.cs, GarageVisual.cs ve araç görselleri | 14 dekor, özgün posterler, dört aracın oda/oyun sunumu. |
| LocalProgress.cs | Normal kayıt, ayrı elle deneme profili ve ayrı otomatik test alanı. |
| CourierSmokeTest.cs, CourierPersistenceTest.cs | Çalışma, ekran ve ayrı süreçte kalıcılık doğrulaması. |

Kullanıcının bu sohbet içindeki mevcut yayın yönlendirmesiyle ilerle; eksik hesap/kimlik/ürün bilgilerini uydurma. Nihai mağaza hazırlığını çalıştırılabilir Windows tesliminden ayrı tamamla.

UFO gelecekte değerlendirilebilecek bir araç adayıdır; v0.7'de uygulanmış veya satın alınabilir bir UFO yoktur.
