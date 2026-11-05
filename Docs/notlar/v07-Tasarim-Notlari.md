# Kurmalı Kargo — v0.7 tasarım notları

Bu belge 4 Kasım 2026 tarihindeki yeni v0.7 kaynak kapsamını anlatır. **v0.7 derleme, gerçek EXE çalışması, soğuk kayıt/yeniden açılış ve son paket testleri root doğrulamasını bekliyor.** Önceki sürümün başarılı testleri bu sürüm için geçmiş sayılmaz. Son teslimde sürümün kendi JSON sonuçları esas alınmalıdır.

## Sürüş ve araç ekonomisi

Dört araç aynı teslimat döngüsünü kullanır. Pasta, saksı ve çay takımı sırası korunur; araç seçimleri paket alınmadan önce yapılır. Paket taşırken, yerdeyken veya yüklü bir kaydı devam ettirirken araç değiştirerek ücret/risk kuralı atlanamaz.

| Araç | Açma bedeli | Temel ücret | Kazanç ve sürüş farkı |
|---|---:|---:|---|
| Tatlı scooter | Ücretsiz | 40 jeton | Dengeli başlangıç. |
| Fırlama scooter | Ücretsiz | 35 jeton | %32 hızlı; daha hassas kargo ve sert darbede düşüş riski. |
| Mini kargo van | 300 jeton | 45 jeton | Teslimatın temel ücreti ve bütün bonusları ×2; daha ağır ve korunaklı. |
| Koca teker | 700 jeton | 40 jeton | Büyük gövde, geniş dönüşler; hafif darbeleri yumuşatır, sert çarpışmada hasar büyür. |

Minivanın yeni kuralı yalnız temel ücreti ikiye katlamaz:

**Minivan kazancı = (45 + hız bonusu + sağlamlık bonusu) × 2.**

Örneğin 20 hız ve 30 sağlamlık bonusuyla 190 jeton verir. Hız çarpanı 0,88; hızlanma ve darbe hassasiyeti çarpanları 0,65'tir. Geniş gövde kontrolü tamponların engellerden geçmesini önlemeyi amaçlar.

Koca tekerin hız çarpanı 1,10; hızlanması 0,78; temel darbe hassasiyeti 0,85'tir. Dönüş hızı diğer araçlardan düşüktür. Darbe yaklaşma hızına göre ilave hasar çarpanı hafif temasta 0,45'ten sert çarpışmada 2,10'a yükselir. Bu nedenle büyük tekerler her hızda dokunulmazlık sağlamaz. Kargosu bağlı kalır; risk, paketin hasarı ve geniş aracı iyi döndürebilmektir. Bu araç teslimatı ikiye katlamaz.

## Daha büyük mahalle ve altı adres

Mahalle yaklaşık iki kat alan hissi verecek şekilde genişletildi: oynanabilir sınır merkezden her yönde 20 birim, zemin 42×42'dir. Yeni dış sokak halkası, bağlantılar ve iki tuğlalı ara sokak bulunur. Ara sokaklar özgün, sıcak renkli bir şehir mahallesi görünümündedir.

İlk üç adres ve depo yerleri korunur. Yeni üç adres dış mahallededir:

| Adres | Konum (x, z) |
|---|---|
| Limon Evi | −8, 7 |
| Lavanta Evi | 8, 7 |
| Nane Evi | 8, −8 |
| Kiremit Apartmanı | −8, 16 |
| Saatçi Atölyesi | 16, 8 |
| Çınar Köşkü | 8, −16 |

Normal yeni tur hâlâ üç teslimattır. Rota bir merkez adresi, bir dış mahalle adresi ve bunlardan farklı üçüncü bir adres seçer. Altı adresin hepsi tek turda zorunlu değildir. Seçilen üçlü kaydedilir; devam etmek adresleri yeniden seçmez. Eski merkezi güzergâhlar regresyon testleri için korunur.

Çöp kamyonu ve belediye çalışmasının tur başındaki dört durumu sürer: sakin / kamyon / çalışma / ikisi. Aynı turdaki olay seçimi ve saati kayıtla korunur. Kamyon mevcut engelleri dolaşan güvenli güzergâhını kullanır; bu adım genel trafik simülasyonu eklemez.

## Köpekli kısa yollar

Kuzeyde ve doğuda iki isteğe bağlı tuğlalı kestirme vardır. Pati işaretleri ve görünür çizgi film köpekleri yolu önceden anlatır. Köpek varlığı tur tohumuna göre değişir; bazı turlarda hiçbiri, bazılarında biri veya ikisi bulunur.

- Köpek, oyuncunun üstünde aniden belirmez; yakınında kayıt açıldıysa alan boşalana kadar görünürleşmesi bekletilir.
- Kestirmeye girişte yaklaşık 1,1 saniyelik görünür uyarı ve kısa komik ses duyulur.
- Takip yaklaşık 2,7 saniye sürer; köpek sonra yürüdüğü yolu geri izler.
- Isırma, sağlık kaybı veya kargo hasarı yoktur. Yakalanma en fazla bir kez, iki saniyelik 0,78× hız etkisi oluşturur.
- Takip sırasında kısa, sentezlenmiş komik bir müzik dizisi çalar; dış ses servisi veya müzik dosyası kullanılmaz.
- Menü, mola ve diğer duraklatan ekranlarda köpek hareketi ve etki saati ilerlemez.

Geniş ve sakin dış sokaklar alternatif olarak açık kalır. Amaç zorunlu bir ceza değil, isteyerek seçilen kısa yola küçük bir komedi riski eklemektir.

## Garajda 14 dekor

İlk dokuz dekorun kimlikleri ve fiyatları korunur. Beş yeni ürün, doğrudan satın alınan sabit oda dekorlarıdır. Görünür/gizli seçimi ücretsizdir ve kaydedilir.

| Dekor | Jeton |
|---|---:|
| Saksı | 40 |
| Lamba | 65 |
| Halı | 85 |
| Tezgâh | 110 |
| Raf | 135 |
| Kargo tabelası | 160 |
| Rahat koltuk | 180 |
| Paket posteri | 210 |
| Drone platformu | 240 |
| Filiz dost posteri | 275 |
| Bulut dost posteri | 300 |
| Boğaz manzarası | 340 |
| Sağlam paket kupası | 380 |
| Paket otomatı | 440 |

Filiz ve bulut dostları özgün küçük karakterlerdir. Manzara; köprü, vapur ve gün batımıyla koddan oluşturulur. Başka bir oyunun karakteri veya hazır görseli kullanılmaz. Drone platformu dekor ürünüdür; drone yardımcısı sahipliğiyle ayrı tutulur.

## Tekrarlanabilir işletme işleri

Garajda dört küçük iş bulunur. İlgili eylemler normal oynanışta sayılır; şart dolunca ödül garajdan alınır.

| İş | Gereken | Ödül |
|---|---|---:|
| Mahalle anlaşması | 3 paket teslimatı | 90 jeton |
| Nazik eller | En az %90 sağlamlıkla 2 teslimat | 80 jeton |
| Yeni sokaklar | Dış mahalle adreslerine 2 teslimat | 120 jeton |
| Hava kargo | Tamamlanmış 2 drone görevi | 70 jeton |

Ödül alınca o işin gereken sayısı ilerlemeden düşülür; fazlası korunur. İş tekrar yapılabilir. Aynı tamamlanmış ilerleme sınırsız ödül vermez. İş sayaçları tur kaydından ayrı yerel ilerlemedir.

Drone'un mevcut ana kuralı sürer: 250 jetonla açılır, garajdan gönderilir, yalnız aktif teslimat oyununda 30 saniyede tamamlanır ve görev başına bir kez 25 jeton verir. İş ödülü, iki gerçek uçuş tamamlandıktan sonra ayrıca alınır.

## Bol jetonlu elle deneme modu

Yeni **`-playtest`** seçeneği, oyunu elle incelemek ve ürünleri hızlıca denemek içindir. Bu mod otomatik regresyon testi değildir.

- Ayrı **`__kk_playtest__`** yerel kayıt alanı kullanır. Normal oyuncu profiline jeton veya ürün eklemez.
- Bu ayrı profil ilk kez açıldığında 50.000 jetonla başlar. Sonraki açılışlarda kendi ilerlemesini sürdürür.
- Ana menüde görünür **“Deneme: +50.000 jeton”** düğmesi bulunur; yalnız bu modun cüzdanını artırır.
- Normal oyun bu düğmeyi ve yapay başlangıç parasını kullanmaz.
- Son paketin kolay başlatma komutu root tarafından eklenecek. Temel çağrı `KurmaliKargo.exe -playtest` biçimindedir.

Normal ekonomiyi değerlendirirken standart açılış; bütün araç/dekorları hızlı görmek için deneme açılışı kullanılmalıdır. Otomatik smoke ve ayrı süreç kalıcılık testleri kendi bağımsız test alanlarını kullanmaya devam eder.

## Kapsam ve doğrulama

Bu dosya yeni kaynak davranışını belgeler. **Henüz v0.7 için geçti denilen bir çalışma testi veya hazır paket sonucu yoktur; root'un derleme ve çalışma doğrulaması beklenir.** Özellikle geniş araçların yeni sokaklardan geçişi, altı adresli kayıt devamı, işletme ödüllerinin tek kez alınması, köpek uyarı/etki/duraklatması ve deneme profilinin normal kayıttan ayrılması gerçek sürümde kontrol edilecek.

UFO yalnız ileride düşünülebilecek bir araç fikridir; bu sürümde uygulanmadı. Gerçek reklam veya gerçek para ile uygulama içi satın alma entegrasyonu bu adımda eklenmedi. Belgedeki fiyatlar oyun jetonudur; yeni ücretli servis kullanılmaz.
