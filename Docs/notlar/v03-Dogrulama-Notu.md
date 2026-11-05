# v0.3.0 doğrulama kaydı

3 Kasım 2026. Unity 6000.6.7f1 ile Windows x64 derlemesi başarılı. Son derleme: `work/logs/unity-v03-final-build.log`, kaynak tür/API derleme kontrolü de başarılı.

- Gerçek uygulama: **115 kontrol / 0 hata**, 65,47 saniye. Sonuç `v03-Test-Sonucu.json`.
- Normal yerel kayda yazıp süreçten çıkış: **13 kontrol / 0 hata**.
- Yeni süreçte normal kayıt yükleme ve devam: **13 kontrol / 0 hata**. Paket, konum, sağlamlık, süre, dil, ses ve hareket tercihi korundu. Bu test `__kk_saveprobe__` alanını kullanır; gerçek oyuncu kaydına dokunmaz. Okuma sonunda yalnız test kayıtları temizlenir.
- Görsel kontrol: TR/EN menü, paket oynanışı, sonuç, garaj, ayarlar, öğretici ve gizlilik ekranları incelendi. Güncel görüntüler `v03-Ekranlar` klasöründe.
- Normal uygulamada doğrudan pencere tıklamalarıyla Menü → Garaj → Scooter renkleri → Garaj → Menü çalıştı. Sahip olunan iki renk, küçük düğmeye basmadan kartın üstünden seçilip geri değiştirildi; jeton sayısı değişmedi. Mevcut oyuncunun varsayılan renk seçimi geri getirildi. Normal önizleme menüde açık bırakıldı.
- Paket arşivi doğrulandı: 157 Windows dosyası, EXE ve oyun assembly/veri dosyaları birlikte. Kaynak ZIP 63 dosya içerir; Library klasörü dahil değildir. Sürümlü paketler eski sürümleri korur.
- Claude bağımsız incelemesi `Claude-v03-Inceleme.md` dosyasında. İnceleme çalıştırılmış test yerine sayılmadı.

Testler iPhone performansı, fiziksel dokunma ergonomisi, imzalı iOS arşivi, gerçek reklam/ödeme veya mağaza kabulünü kapsamaz. Gerçek reklam/IAP entegrasyonu bulunmuyor. Yayınlama Claude'a devredilecek; eksikler `Claude-Yayin-Devir.md` içinde.

Bir sonraki kalite adımları: gerçek telefon boyutuna uygun arayüz ölçüleri; garaj/mahalle kamera geçişinin daha kısa olması; gerekirse yalnız mevcut turdan vazgeçme onayı. Çöp kamyonu, drone veya yeni mülk sistemi bu küçük sürümün testleri yerine eklenmemeli.
