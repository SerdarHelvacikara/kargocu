# Codex ve Claude — ilk doğrudan çalışma

3 Kasım 2026 tarihinde açık Claude Code masaüstü uygulamasında yalnızca Kurmalı Kargo Unity projesi seçildi. Kullanıcı çalışma alanını onayladı. Codex, inceleme görevini doğrudan gönderdi ve yanıtı doğrudan okudu; kullanıcının mesajları taşıması gerekmedi.

## Görev paylaşımı

- Codex: Unity geliştirmesi, değişiklikleri birleştirme, Windows derlemesi ve gerçek uygulama testleri.
- Claude: Kaynak dosyalarının bağımsız incelemesi. Bu ilk görevde dosya değiştirmesi veya komut çalıştırması istenmedi.

## İlk incelemeden alınan düzeltmeler

- Sonuçtan garaja girince yarım tura dönüş yolu eksikti. Garaj artık hangi ekrandan açıldığını hatırlıyor; geri dönünce aynı sonuç ve tur korunuyor.
- Hedef işaretçisinin değişen rengi, ortak malzeme önbelleğinden ayrıldı.
- Teslimat bittiği karede Escape'in sonucu atlaması engellendi.
- Garaj fiyatları ve renkleri oyunun ortak değerlerinden alınıyor.
- Garaj bildirimi başlığı örtmeyecek şekilde aşağı taşındı.

İlk rapordaki, yarım turun skorunun hiç kaydedilmediği iddiası doğrulanmadı: en iyi skor zaten her teslimatta güncelleniyordu. Doğrulanan sorun, garajdan sonra mevcut turun devam ettirilememesiydi.

## Ek kontrol ve düzeltme

Codex'in ek kontrolünde, duvara karşı sürmeye devam etmenin aynı teması tekrar tekrar çarpışma saydığı görüldü. Sürekli temas artık tek darbe sayılıyor. Uzaklaşıp yeniden çarpmak yeni bir darbe oluşturuyor. Paralel sürtünme için yaklaşma yönü de denetleniyor.

## v0.2.1 doğrulaması

- Windows derlemesi başarılı.
- Gerçek uygulamada 62 kontrol geçti; hata listesi boş.
- Garajdan yarım tura dönüş, sürekli duvar temasında ek hasar oluşmaması, ayrılıp yeniden çarpma, üç teslimat, paket farkları, iki dil ve kozmetik alışverişi kontrol edildi.
- Yeni garaj görünümü görüntü üzerinden kontrol edildi.

Claude'a düzeltmelerin ardından ikinci inceleme görevi de gönderildi ve yanıtı doğrudan alındı. Takip incelemesinde somut bir gerileme bildirmedi; ilk raporundaki BestScore iddiasını da düzeltti. İlk ve ikinci yanıtlar `Claude-Ilk-Inceleme.md` ve `Claude-Takip-Incelemesi.md` dosyalarında saklandı.

Takip notları: mevcut mahalle yalnızca temel collider türleri kullanıyor; ileride karmaşık mesh engeller eklenirse temas ayrılma hesabı yeniden ele alınmalı. Çarpışma sonrası 1,15 saniyelik kısa koruma bilinçli olarak korunuyor. Bitmiş turdan garaja gidildiğinde geri düğmesinin "Sonuca dön" olarak adlandırılması küçük bir sonraki arayüz cilası olarak not edildi.
