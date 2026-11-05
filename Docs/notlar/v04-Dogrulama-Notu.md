# v0.4.0 doğrulama

3 Kasım 2026. Unity 6000.6.7f1, Windows x64. Kaynak tür/API kontrolü ve Unity gerçek derlemesi başarılı.

- **157 çalışma kontrolü / 0 hata**, 84,37 saniye. Önceki 115 kontrole ek gerçek yüksek hızlı çarpışma/düşüş/geri alma, teslim edememe, düşen paketin JSON kaydı, 35 temel ücret ve tekrar ödül koruması; paket varken menü/garaj araç kilidi; eski v1 kayıt uyumu; belediye engeli ve kamyonun güvenli yol vermesi kapsanır.
- Düşen paket en az 1,7 metre uzağa bırakılır. Hareket etmeden beklemek otomatik geri alma oluşturmadı. Erişilebilir yer bulunamazsa düşüş gerçekleşmez, darbe hasarı uygulanır.
- **14 yazma + 17 yeni süreçte okuma kontrolü / 0 hata.** Normal kalıcı kayıt yoluyla paket, konum, hasar, süre, ayarlar, araç tercihi ve soğuk açılışta araç kilidi korundu. Kullanıcı verisi yerine ayrı test alanı kullanıldı; alan okuma sonrasında temizlendi.
- Görsel kontrol: yakın düşen paket, kurtarılan paket, 35 jetonluk sonuç, araç seçenekli garaj, rögar çalışması ve çöp kamyonu incelendi. Görüntüler v04-Ekranlar içinde. Düşen kutunun zemine gömülmesi düzeltilerek yeniden görüntülendi.
- Ayrı kaynak incelemesinde bulunan erişilemez/çok yakın düşüş ve araç değişimiyle ücret/risk aşma sorunları düzeltildi; ilgili tekrar kontrolleri son 157 teste dahildir.

Sonuçlar v04-Test-Sonucu.json, v04-Kayit-Yazma.json, v04-Kayit-Yeniden-Acilis.json dosyalarında. Eski sürümler korunur. Test başarısı iPhone ergonomisi, fiziksel cihaz performansı, gerçek reklam/ödeme veya mağaza kabulünü ifade etmez.

Sonraki öncelik: telefon arayüzü/joystick rahatlığı ve oyun dengesi. Daha fazla dekor, drone, serbest mülk sistemi veya genel trafik yapay zekâsı bu adıma eklenmedi.
