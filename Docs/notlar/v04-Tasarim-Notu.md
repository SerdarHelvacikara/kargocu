# v0.4 — sokakta karar ve hızlı sürüş

Bu adım dekor sayısını büyütmez. İki sürüş profili, geri alınabilir düşen paket ve okunabilir iki sokak engeli ekler.

## Araç dengesi

| | Tatlı scooter | Fırlama scooter |
|---|---|---|
| Hız | Paket türüne göre mevcut hız | Aynı paket için %32 daha hızlı |
| Hızlanma | Mevcut değer | %15 daha yüksek |
| Temel teslimat ücreti | 40 jeton | 35 jeton |
| Ek bonus | En fazla 25 hız + 35 sağlamlık | Aynı bonus sınırları |
| Darbe hasarı | Paket türüne göre mevcut değer | %25 daha fazla |
| Paketin düşmesi | Bağlı kalır | Sert çarpışmada düşebilir |

İkisi de bu taslakta ücretsiz seçilir. Paket alınmışsa seçim kilitlenir; sonuç ekranından garaja girip sonraki paketten önce değiştirilebilir. Böylece hızlı gidip teslimat öncesi daha yüksek ücretli profile geçiş yapılmaz.

## Düşen paket

Düşüş rastgele zar değildir: hızlı araçta çarpışma normaline yaklaşma hızı en az 5,8 dünya birimi/saniye olduğunda tetiklenir. Paket çarpışmanın gerisinde yaklaşık 1,95 metreye bırakılır; sürücüden en az 1,7 metre uzakta, sınırlar içinde ve engelle örtüşmeyen bir yer aranır. Uygun yer bulunamazsa o darbede hasar verilir, paket bağlı kalır.

İşaretçi ve mini harita artık paketi gösterir. Müşteriye paketsiz giderek teslimat veya ödül alınamaz. Geri alma yeni depodan alış değildir: hasar, kalan süre, tür ve müşteri korunur. Paket yerdeyken süre işlemeye devam eder. Yerdeki konum yerel tur kaydına dahil edilir; devam edince aynı paket yeniden oluşturulur. Güvensiz kayıt konumunda güvenli depo alanına dönülür.

## Mahalle olayları

- Tek küçük belediye çöp kamyonu, sabit yol güzergâhı ve kısa toplama durakları kullanır. İlk hareketten önce sekiz saniyelik uyarı vardır. Scooter önündeyse kamyon bekler; hareketi oyuncuyu itmez. Menü/mola/ayarlar açıkken olay saati ve kamyon durur.
- Orta-batı yolda tek dubalı rögar çalışması vardır. Engel gerçek collider ile geçişi kapatır; kavşak, depo ve teslimat halkaları açık kalır. Çevrede alternatif sokaklar vardır.
- İki olay mini haritada işaretlidir. Oyuncunun önünde rastgele belirmezler.

## Kayıt uyumu

Yeni tur şeması v2, yeni `kk_activeRun_v2` anahtarında tutulur. v0.3'ün eski kaydı okunup taşınır; eski sonuçların temel ücreti 40 olarak yorumlanır. Ödüller geri yüklemede tekrar verilmez. Araç tercihi ayrı yerel ayardır. Aynı oyuncu verisini kullanan farklı oyun sürümleri aynı anda oynatılmamalıdır.

Bu sürüm hâlâ yerel, tek oyunculu bir Windows denemesidir. Gerçek reklam/ödeme, yeni dış servis, drone veya genel trafik yapay zekâsı eklenmedi. Telefon sürüş hissi ve nihai denge gerçek oyuncu/cihaz geri bildirimiyle ayarlanmalıdır.
