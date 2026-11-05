# Kurmalı Kargo — iOS yayın kontrolü

> **v0.4 ilerleme notu:** İki araç profili, geri alınabilir düşen kargo, küçük çöp kamyonu ve dubalı belediye çalışması eklendi. Son Windows testi 157/0 hata; iki süreçli kayıt testi 14+17/0 hata. Yeni harici SDK veya servis yok. Bu değişiklikler iOS arşivi/cihaz ve gerçek reklam/ödeme kapılarını tamamlamaz.

> **v0.3 ilerleme notu (3 Kasım 2026):** Altı dekorlu 3D garaj, ayarlar, öğretici, gizlilik/destek ekranı, Screen.safeArea dönüşümü ve yarım tur kaydı eklendi. Windows gerçek çalışma testinde 115 kontrol; iki ayrı süreçle normal disk kaydı/yeniden açılışta 13+13 kontrol geçti. submitAnalytics son derlemede 0. Destek e-postası helvacikaraserdar@gmail.com olarak doğrulandı; kullanıcıda Mac/iPhone ve mağaza hesapları var. Aşağıdaki tablo v0.2.1 denetiminin tarihsel kaydıdır; iOS kapıları tamamlanmış sayılmaz. Kullanıcı yayınlama işlerini Claude'a bıraktı; güncel devir Claude-Yayin-Devir.md dosyasındadır.

**Denetim tarihi:** 3 Kasım 2026. **Kaynak anlık görüntüsü:** Unity 6000.6.7f1 / oyun 0.2.1. Bu belge kaynakları ve mevcut PC test kaydını okuyarak hazırlanmıştır; iOS derlemesi veya App Store başvurusu yapılmamıştır. Daha sonraki kod değişikliklerinde ilgili kapılar yeniden doğrulanmalıdır.

**Sonuç:** PC’de doğrulanmış bir temel var. iOS yayını henüz hazır olarak işaretlenemez. Aşağıdaki kapılar tamamlanınca başvuru hazırlanabilir; Apple’ın ilk incelemede kabul edeceği garanti edilemez.

## Kaynakta doğrulanan mevcut durum

| Alan | Bulgu | Kanıt |
|---|---|---|
| Temel oynanış | Üç teslimat, farklı kargolar, hasar/tepki, ödül, garaj ve TR/EN akışı PC testinde geçiyor. | `v021-Test-Sonucu.json`: `passed: true`, 62 kontrol, `errors: []`. Bu sonuç iPhone testi değildir. |
| Harici SDK | Manifestte yalnızca Unity’nin altı temel modülü var. Reklam, Analytics, Unity IAP veya üçüncü taraf ağ SDK’sı görünmüyor. | `UnityProject/Packages/manifest.json`; `Assets` kaynak taraması. |
| Veri kaydı | Jeton, en iyi tur, renk kilitleri/seçimi, ses ve dil tercihi yerel `PlayerPrefs` ile tutuluyor. | `Assets/Scripts/CourierGame.cs`, `GameText.cs`. |
| Uygulamanın ağ kodu | İncelenen oyun kaynaklarında sunucuya veri gönderme, hesap açma, kimlik bilgisi toplama, IDFA veya cihaz kimliği okuma kodu bulunmadı. | `Assets` içinde ağ/SDK/kimlik API taraması. |
| Unity hizmetleri | `m_Enabled`, Engine Diagnostics, Cloud Diagnostics, Analytics, Ads, Purchasing ve Performance Reporting kapalı. Bulut proje kimliği boş. | `ProjectSettings/UnityConnectSettings.asset`; `ProjectSettings.asset`: `cloudEnabled: 0`, `cloudServicesEnabled: {}`. |
| Açık gizlilik sorusu | `ProjectSettings.asset` içinde `submitAnalytics: 1` ve `usePlayerLog: 1` bulunuyor. Bunların varlığı tek başına veri aktarımı kanıtı değildir; hedef platformun Diagnostics ayarı ve son oyuncu çıktısıyla birlikte doğrulanmalı. | `ProjectSettings.asset`, satır 98–99. |
| Yayın kimliği | Şirket adı `KurmaliKargoPrototype`, uygulama kimliği sözlüğü boş. Bunlar gerçek yayıncı/bundle kimliği olarak doğrulanmış değil. | `ProjectSettings.asset`; `Assets/Editor/PrototypeBuilder.cs`. |
| iOS ortamı | Bu PC’de Windows ve WebGL modülleri var; iOS Build Support yok. iOS arşivi, imzalama ve cihaz testi gösterilemedi. | Yerel Unity `PlaybackEngines` listesi. |

## Gizlilik beyanı için güvenilir sınır

Şu an kullanılabilecek dar ifade: **“İncelenen oyun kaynakları, ilerlemeyi cihazda saklıyor; oyun tarafından hazırlanmış hesap, reklam, analitik veya sunucuya aktarım entegrasyonu bulunmuyor.”**

App Store’daki **“Data Not Collected / Veri Toplanmaz”** yanıtı, son iOS çıktısı için ayrıca doğrulanmalıdır. Apple, cihazda kalan işlemeyi toplama olarak saymaz; üçüncü tarafların uygulamadan topladığı veriler de beyana dahildir. Bu nedenle yalnızca kaynak taraması nihai etiketin yerini tutmaz. [Apple — App Privacy Details](https://developer.apple.com/app-store/app-privacy-details/)

Yayın öncesi gizlilik kapısı:

- [ ] iOS Build Profile içindeki Diagnostic Data Collection seçimini doğrula; `UnityConnectSettings` değerleriyle birlikte kaydet. Unity 6.6 belgelerinde diagnostics hedef profil üzerinden yönetilir. [Unity — Build Profiles](https://docs.unity.com/en-us/engine/6000.6/manual/building-and-publishing/build-settings/build-profiles-reference)
- [ ] `submitAnalytics` alanının bu sürümdeki etkisini doğrula; son iPhone derlemesinde başlangıç, teslimat, garaj ve bekleme sırasında beklenmeyen ağ aktarımı olmadığını gözle.
- [ ] Unity’nin veri toplama ayarını kontrol et. Diagnostics açık olduğunda Essential Data davranışı önemlidir; mevcut kapalı ayarların yayın profiline taşındığını teyit et. [Unity — Data Collection Settings](https://docs.unity.com/en-us/cloud/developer-data-framework/data-collection-settings)
- [ ] Xcode arşivindeki `PrivacyInfo.xcprivacy` dosyalarını ve birleşik privacy raporunu incele. `PlayerPrefs` için User Defaults API gerekçesinin dahil olduğunu kontrol et; Unity belgesi `CA92.1` gerekçesini listeler. **“Veri toplanmaz” etiketi, required-reason manifestini gereksiz yapmaz.** [Unity — Apple privacy manifest](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/iphone/ios-building-and-delivering/apple-privacy-manifest-policy)
- [ ] Yayıncı adı, destek iletişimi, yerel kayıtların ne olduğu ve nasıl sıfırlandığı son gizlilik metninde doğru anlatılsın. Platformun kendi işlemleriyle oyunun topladığı veriler ayrıştırılsın.

## Başvuru öncesi kapılar

| Durum | Kapı | Tamamlanma kanıtı |
|---|---|---|
| **Eksik** | Gerçek yayın kimliği | Yayıncı adı, benzersiz bundle ID, Apple takım kimliği, sürüm/build numarası ve imzalı uygulama aynı yayına ait. Build yardımcı betiği bunları yeniden prototip değerlerine döndürmüyor. |
| **Eksik** | Mac / Xcode / iOS derlemesi | Uygun Mac’te iOS Build Support kurulu; gerçek cihaz derlemesi ve Xcode archive validation başarılı. |
| **Doğrulanacak** | Güncel Apple araç sınırı | 28 Nisan 2026’dan beri Xcode 26+ ve iOS 26 SDK+ gerekiyor. 9 Eylül 2026’dan beri minimum deployment target iOS 13+; projedeki `15.0` bu alt sınırı karşılıyor. SDK sürümü ve minimum çalıştırma sürümü ayrı ayarlardır. [Apple — Upcoming Requirements](https://developer.apple.com/news/upcoming-requirements/) |
| **Eksik** | Herkese açık gizlilik ve destek sayfaları | Çalışan, giriş istemeyen HTTPS Privacy Policy URL ve Support URL; uygulama içinden erişim; geçerli destek iletişimi. Şu an kaynakta `Application.OpenURL` veya yayın URL yapılandırması yok. |
| **Eksik** | Gerçek iPhone kalite testi | Çentik/Dynamic Island ve home indicator güvenli alanı; iki yatay yön; en küçük desteklenen ekran; dokunmatik kontrol; okunabilir Türkçe/İngilizce arayüz. Mevcut HUD `Screen.width/height` ölçekli, `Screen.safeArea` kullanımı görülmedi. |
| **Eksik** | Yaşam döngüsü ve kayıt | Kilitle/aç, arka plana al/dön, uygulamayı kapat/aç, ses ayarı, dil ayarı, satın alınmış oyun içi renkler ve jetonlar; açık teslimatın nasıl devam edeceği tutarlı. Kaydı sıfırlama akışı yalnızca yerel veriyi etkilediğini açıklıyor ve yanlışlıkla silinmeyi önlüyor. |
| **Eksik** | Performans ve dayanıklılık | Gerçek iPhone’da uzun oturum, tekrarlanan teslimatlar, ısınma, bellek artışı, kare hızı ve pil davranışı; soğuk açılışta çökme/hata yok. PC’de 62 kontrol bu kanıtı sağlamaz. |
| **Eksik** | Mağaza içerikleri | Son uygulama ikonu/launch ekranı, gerçek oyun görüntüleri, doğru TR/EN açıklamalar ve inceleme notları. Prototip etiketi/yer tutucular yayın çıktısından temizlenmiş. |
| **Eksik** | Yaş ve diğer App Store Connect soruları | Gerçek içerik ve son reklam/iletişim özelliklerine göre güncel yaş anketi tamamlanmış. Şimdiden “4+” veya başka bir yaş değeri atanmadı. [Apple — Set an app age rating](https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating/) |
| **Doğrulanacak** | Özgünlük ve içerik değeri | Hay Day/Crossy Road adları, ikonları veya ekran düzenleri mağaza kimliği yapılmıyor. Ad/marka kullanılabilirliği, varlık kökenleri ve üç teslimatın tekrar oynama değeri ayrıca değerlendiriliyor. Hazır kodla üretilmiş olmak onay kanıtı değildir. [Apple — 4.1 Copycats](https://developer.apple.com/help/app-review/guideline-reference/4-1-prevent-copycat-rejection) |

Gizlilik bağlantısı, erişilebilir destek bilgisi, tamamlanmış içerik ve çalışan satın alma akışları inceleme açısından önemlidir. Mevcut prototip için bu kapılar açık kaldığından “yayına hazır” denmemelidir. [Apple — App Review Guidelines, 1.5 / 2.1 / 5.1.1](https://developer.apple.com/app-store/review/guidelines/)

## Reklam ve gerçek para mağazası kararı

**Mevcut garaj yalnızca oynayarak kazanılan jetonları kullanır. Gerçek para ürünü, StoreKit/IAP entegrasyonu, reklam SDK’sı veya gerçek “satın alımları geri yükle” akışı yoktur.**

- **IAP eklenecekse:** Ürün kimlikleri, Apple ürün türleri ve yerelleştirilmiş fiyatlar gerçek mağazadan alınmalı. İptal, bekleyen işlem, yeniden yükleme, bağlantı kesilmesi ve iki kez ödül vermeme test edilmeli. Geri yüklenebilir ürünlere çalışan geri yükleme mekanizması gerekir; sahte aktif düğme yeterli değildir. [Apple — In-App Purchase](https://developer.apple.com/in-app-purchase/)
- **Reklam eklenecekse:** Sağlayıcı ve SDK sürümü kesinleşince gizlilik beyanı yeniden yapılmalı. Reklam kapatma/atlama ve uygunsuz reklam bildirme akışı, yaş uygunluğu ve ödülün bir kez verilmesi doğrulanmalı. [Apple — App Review Guidelines, 2.5.18](https://developer.apple.com/app-store/review/guidelines/#software-requirements)
- **ATT:** Her reklam otomatik olarak ATT gerektirmez. Seçilen entegrasyon şirketler arası takip veya IDFA erişimi yapacaksa ATT koşulları uygulanır; izin reddinde takip başlamamalıdır. Şu an incelenen kaynakta böyle bir entegrasyon bulunmadı. [Apple — User Privacy and Data Use](https://developer.apple.com/app-store/user-privacy-and-data-use/)

Reklam veya IAP eklenmesi; veri, yaş anketi, mağaza açıklaması ve cihaz testini yeniden açar. İlk yayın için hangilerinin gerçekten etkin olacağı açıkça seçilmelidir.

## Yayıncıdan gereken bilgiler

1. Gerçek yayıncı/şahıs/şirket adı ve kullanılacak destek e-posta adresi.
2. Apple Developer / App Store Connect hesabının durumu; varsa Apple’ın önceki Animal Run ret mesajındaki madde ve açıklama.
3. Mac + Xcode ve test iPhone’u erişimi; desteklenecek iPhone/iPad kapsamı.
4. Herkese açık Privacy Policy / Support sayfalarının yayınlanacağı adresler.
5. İlk sürümde reklam olacak mı; olacaksa sağlayıcı hangisi? Gerçek para ürünü olacak mı; olacaksa hangi ürünler ve ürün türleri?
6. Son oyun adı, yayın ülkeleri, hedef kitle ve yayıncı haklarına ilişkin doğrulama.

**Başvuru koşulu:** Kimlik/URL/iOS arşivi/gerçek cihaz/gizlilik/mağaza kapıları tamamlanmış, son derleme test edilmiş ve App Store Connect beyanları aynı derlemeyi anlatıyor olmalı. Bu belge bir kabul belgesi veya ilk inceleme onayı taahhüdü değildir.
