# Claude Code — v0.5 incelemesi

3 Kasım 2026. Kullanıcının yetkisiyle mevcut “Unity kargo oyunu incelemesi” Code oturumuna görev doğrudan gönderildi ve cevap doğrudan okundu. Claude yalnız kaynak okudu; dosya değiştirmedi veya derleme/test çalıştırmadı.

İncelenen alan: StreetEvents, CourierGame.Progress ve CourierGame.DeliveryExtras; gerektiğinde ana CourierGame. Konu tur başında rastgele seçilen olaylar, turda sabit kalma, sokak saatinin kaydı, eski kayıt göçü ve kamyon güvenliği.

Claude tek somut hata bildirdi: Kamyon, kayıttaki oyuncu konumuna göre güvenli yere alınıyordu. Sonraki güvenli konum kontrolü oyuncuyu veya düşen paketi depoya taşırsa kamyonla son mesafe yeniden denetlenmiyordu. Bunu doğruladık.

**Uygulanan düzeltme:** StreetEvents.KeepClearOf eklendi; ApplyRun son oyuncu konumundan ve düşen paket için son güvenli konumdan sonra çağırır. Çok yakın kamyon devre dışı bırakılıp uyarılı başlangıç konumuna döner. Sokak seçimi ve görev saati yeniden rastgele seçilmez. Minivan gövdesi için bekleme mesafesi ayrıca2,1 dünya birimine çıkarıldı. Bu son düzeltmeler v0.6 kaynaklarına ve tekrar kontrolüne dahildir; önce üretilen v0.5 binary'ye geriye dönük uygulanmış sayılmaz.

Doğrulanmamış bir not olarak, kamyon köşede beklerken depo veya üçüncü teslimat halkasının bir kısmını kapatabileceğini ve gerçek yaklaşım denenmesini istedi. Rotadaki engelleri atlama ve kamyonun oyuncuyu itmeden beklemesi test ediliyor; halka alternatif taraftan ulaşılabilir kalmalı.

Claude; seed doğrulaması, dört olay modu, üç yeri atlayan rotalar, köşede dönme ve eski v1/v2 kayıtların sakin sokakla taşınmasını tutarlı buldu. Kaynak incelemesi gerçek uygulama veya telefon testi yerine sayılmadı.
