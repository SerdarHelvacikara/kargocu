# Kurmalı Kargo — Claude yayın devri

Codex Windows oyununu ve Unity kaynağını teslim eder. Hesap, mağaza ürünleri, imzalama ve gönderim kullanıcı ile Claude'a aittir. Unity sürümü 6000.6.7f1. Kaynakta Assets, Packages ve ProjectSettings yeterlidir. Neighborhood sahnesi mahalleyi, garajı ve araçları kodla kurar.

## v0.9.x kapsamı

Çarşı, Çin Mahallesi ve Plaza ayrı teslimat haritaları olarak garajdan açılır. v7 tur kaydı bölgeyi taşır. v6 ve daha eski turlar başlangıç mahallesine yüklenir. Bölge sahipliği ve 10 kademeli depo ayrı yerel anahtarlardadır. Oyuncu kayıtlarıyla eski kurulum üstüne güncelleme testi yayın adımlarına dahildir.

uGUI garajı, RenderTexture araç önizlemesi ve mağaza onay ekranı gerçek iPhone/Android cihazda kontrol edilmeli. Windows dikey ve yatay pencere raporları cihaz testi yerine geçmez. Ses ayarı yeni prosedürel mahalle müziğini de yönetir. Hesap ve canlı reklam bayrakları önceki yapılandırmayı kullanır.

## Oynanış ve kayıt

v0.8 gövde eksenli monster truck ve dino sürüşü, köpek panik kaçışı, tohuma bağlı sekiz alış noktası, mesafeye göre süre, belediye, bacalı kargo fabrikası ve yetişkinlerin oynadığı halı saha içerir. Fabrika ağır işi +80 temel ücret ve taşıma sırasında düşük hız getirir. Balonlar ve parçacıklar havuzlanır. ReducedMotion hareketi azaltır. Ejderha alçak kasaları ve yol bariyerini aşar. Dinozor paketi bağlı taşır ve köpekleri kaçırır. Garaj, drone ve işletme işleri korunur.

kk_activeRun_v6 yeni alış planını, süreyi, fabrika işini ve teslimat serisini saklar. Eski v5 turu depo alışlarıyla devam eder. Kazanılmış ilerleme ve drone ayrı anahtarlardadır. Satın alma defteri kalıcı hakları ve harcanmamış satın alınmış jetonları oyun sıfırlamasından korur. Windows deneme modu ayrı profil kullanır.

## Canlı hizmetlere geçiş

- CommerceConfiguration.cs içindeki iOS/Android IAP ürün kimliklerini, LevelPlay app key ve ad-unit kimliklerini ve Google UMP için native AdMob app kimliklerini gerçek hesaplardan gir. SDK-Yayin-Notlari.md kurulum ayrıntılarını içerir. Unity IAP 5.4.4, LevelPlay 9.6.0, iOS Support 1.3.0 ve Google Mobile Ads 11.6.0 çözülüp Windows derlemesine dahil edildi.
- Canlı bayraklar ve kimlikler teslimde kapalı/boş. Önce UMP mesajlarını ve ağları yapılandır. ATT, izin reddi, tercihleri tekrar açma, ağ kaybı ve izin akışını cihazda doğrula. İzinli bağlamda reklam istekleri kişiselleştirme kapalı olarak tasarlanmıştır. Canlı yapılandırmayı cihaz kanıtıyla aç.
- Sandbox/TestFlight ve Android dahili testinde gerçek fiyat, bekleyen/iptal edilen işlem, tekrar gelen callback, kalıcı araç ve reklam kaldırma geri yüklemesi, jeton paketleri, ödüllü reklam ve geçiş reklamını kontrol et. Yerel mağaza işlemi doğrulama ve tekrar engelleme var. Sunucu tarafı makbuz doğrulaması kurulmadı. Smoke, persistence ve playtest yalnız mock servisleri kullanır.
- Kira yalnız oynanırken işler ve teslimat ortasında dolarsa teslimat sonunda biter. Geçiş reklamı yalnız üç teslimatlık turun sonucundan çıkarken adaydır, ilk üç tur muaftır, aralık en az 240 aktif saniyedir. Reklamı kaldırma isteğe bağlı ödülleri bırakır.
- Son iOS/Android çıktısında SDK manifestlerini, mağaza gizlilik ve veri güvenliği formlarını, takip izin metnini, reklam satıcılarını ve yaş anketini gerçek davranışa göre doldur. Pazarlama hedefi yetişkin oyunculardır. Yaş derecesi mağazanın içerik anketine bağlıdır. Okul yerine fabrika kullanılır.
- Kullanıcının doğruladığı destek adresi helvacikaraserdar@gmail.com. Yasal yayıncı adını kullanıcıdan al. Herkese açık HTTPS destek ve gizlilik sayfalarını, son ikonu, gerçek cihaz ekran görüntülerini, mağaza metinlerini ve inceleme notlarını tamamla. Uygulama içindeki gizlilik metnini son yayın yapılandırmasıyla karşılaştır.
- Gerçek iPhone ve Android cihazda dokunmatik, çentik/güvenli alan, iki yatay yön, soğuk kayıt, arka plan, ses, pil/ısınma ve kare sürelerini ölç. Windows kompakt pencere testi ayrı bir kontroldür. İmzalı iOS/Android derlemesi, cihaz testleri ve mağaza incelemesi bu teslimin ardından yapılacak.

Güncel Windows kanıtı ve sürüm dosyaları Gelistirme-Devam.md ile sürümlü JSON raporlarında tutulur. Eski v07 raporlarını v08 kanıtı olarak kullanma. Önceki Animal Run inceleme uyarısını mağaza metni ve görselleri hazırlarken gerçek ret mesajı üzerinden değerlendir.

