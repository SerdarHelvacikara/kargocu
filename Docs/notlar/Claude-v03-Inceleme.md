# Claude Code — v0.3 bağımsız kaynak incelemesi

3 Kasım 2026, mevcut yetkilendirilmiş masaüstü oturumunda doğrudan görev gönderildi ve yanıt okundu. İnceleme yalnız okumaydı; Claude dosya değiştirmedi ve derleme/test çalıştırmadı.

İncelenen dosyalar: CourierGame.cs, CourierGame.Progress.cs, CourierHUD.cs, ReleasePanels.cs, GuiViewport.cs.

**Claude'un sonucu:** İstenen alanlarda somut hata bildirmedi. Ödülü tekrar vermeden sonuç geri yükleme, yarım tur kaydı, geçersiz kayıt sınırları, iç içe ekran dönüşleri ve odak kaybı akışlarını inceledi.

İki sınırlama not etti:

1. Temel smoke modunda kalıcı kayıt yazma/yükleme kapalı olduğundan JSON testleri, gerçekten uygulamayı kapatıp yeniden açma yolunu doğrulamıyordu. Codex aynı eksik için kullanıcı kayıtlarından ayrı bir test alanı ve iki süreçli normal kayıt/yükleme testi ekledi. Çalıştırılmış sonuçları ayrı JSON dosyalarında değerlendirin.
2. Menüde devam edilebilir tur varken yeni tur başlatma seçeneği bulunmuyor. Oyuncu üç teslimatı bitirir veya tüm yerel ilerlemeyi iki onayla sıfırlar. Bu sürüm, yanlışlıkla yarım turun üstüne yazmamak için yalnız devam düğmesini gösteriyor; ayrı turdan vazgeçme akışı sonraki bir cila olabilir.

Ek not: ContinueDelivery anında kayıt yazmaz; bir kapanma en son ara sonuç ekranına döndürebilir. Ödül tekrar verilmez. Uygulama odağı/arka plan/çıkış ve sekiz saniyelik otomatik kayıt normal akışta ek kayıt noktalarıdır.

Bu kaynak incelemesi, gerçek uygulama testinin veya iPhone testinin yerine geçmez. Sonradan eklenen LocalProgress izolasyonu, süreç testi ve kart dokunma alanı değişiklikleri bu ilk v0.3 incelemesinin ardından tamamlandı.
