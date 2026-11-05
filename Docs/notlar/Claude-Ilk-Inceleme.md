# Claude ile ilk doğrudan inceleme

Kaynak: açık Claude Code masaüstü oturumundaki yanıt. İncelenen sürüm: v0.2.

Kısa sonuç: Kritik bir hata bulmadım. Kayıt, dil değişimi, paket ve müşteri sıfırlanması ile nesne yaşam süreleri sağlam görünüyor. Bir gerçek akış hatası ve bir gizli risk var. Gerisi isteğe bağlı cila.
Gerçek hatalar
1. Orta: Sonuç ekranından Garaj’a giden oyuncu turunu kaybediyor
CourierHUD.DrawResult → Game.OpenShop(), ardındanDrawShop → Game.ShowMenu(), ardından menüde “Teslimata başla” → StartRun().
• Senaryo: 1. veya 2. teslimattan sonra Garaj’a girilir, renk alınır, “Menüye dön” ve “Başla” seçilir. Completed ve runScoresıfırlanır, tur baştan başlar. Kazanılan jetonlar kaybolmaz, ama yarım turun skoru BestScore’a hiç yansımaz. Sonuç ekranı Garaj düğmesi sunduğu için oyuncu geri dönebileceğini düşünür.
• En küçük düzeltme: CourierGame’e GamePhase shopReturnalanı ekleyin. OpenShop() içinde shopReturn = Phase;yazın. Yeni bir CloseShop() metodu Phase = shopReturn == GamePhase.Result ? GamePhase.Result : GamePhase.Menu; Save(); yapsın. DrawShop’taki geri düğmesi bu metodu çağırsın, etiketi de duruma göre “Geri” olabilir. LastParcel,LastIntegrity ve LastPayout korunduğu için sonuç ekranı aynen geri gelir.
2. Düşük, gizli risk: İşaretçi malzemesi önbellekteki ortak malzemeyi değiştiriyor
CourierGame.CreateMarker / Update: markerMaterial = Paint(new Color(1f,.77f,.18f)) statik Paints önbelleğinden gelen ortak malzeme. Update her karede bunun .color değerini teal ile altın arasında değiştiriyor.
• Senaryo: Bugün bu rengi başka parça kullanmıyor, sorun görünmüyor. İleride biri Paint ile aynı altın rengini isterse (yeni bir dekor, kozmetik ya da çöp kamyonu), o parça işaretçiyle birlikte teal’e döner. Bunu bulmak zor olur.
• En küçük düzeltme: markerMaterial = new Material(Paint(new Color(1f,.77f,.18f))); kullanın.scooterPaint zaten bu şekilde kopyalanıyor.
İsteğe bağlı cila (hata değil)
• Menüye çıkış:Update içinde CompleteDelivery ile aynı karede Escape’e basılırsa oyun sonuç ekranını atlayıp menüye gider. Escape kontrolünü if (Phase == GamePhase.Playing && Input.GetKeyDown(...)) olarak koşullayın. Ayrıca oyun sırasındaki “Menü” düğmesi ve Escape, turu uyarmadan bırakıyor. Bu tasarım tercihiyse sorun yok.
• Fiyat ve renk tekrarı: DrawShop fiyatları ({0,120,180}) ve kart renklerini kendi listesinden alıyor. Game.ColorPrice(i)ve CourierGame.ScooterColors[i] kullanılırsa fiyat değiştiğinde garaj yanlış fiyat göstermez.
• Ölü kod: CourierHUD.StickVisible/StickOrigin/StickPosition hiç atanmıyor. Çubuğu CourierGame.OnGUI çiziyor. Ya kaldırın ya da tek bir yere taşıyın.
• Garaj bildirimi: Garajda bildirim paneli (y 155–213) başlığın üstüne biniyor.
Kontrol ettiğim, sorunsuz görünen alanlar
• Kayıt: Teslimattan, renk alımından, menüye dönüşten ve çıkıştan sonra yazılıyor. Smoke test modu PlayerPrefs’e dokunmuyor.
• Dil: Sonuç metni ve adres canlı güncelleniyor. Eski dildeki bildirim siliniyor.
• Paketler: ParcelVisual.Configure yaprakları, fincanları, çatlağı ve pasta katını sıfırlıyor.
• Müşteriler:StartRun içinde ResetPose çağrılıyor.
• Nesne yaşam süreleri: Konfeti, egzoz ve kırıntılar kendini yok ediyor.
• Ödeme: Sonuç ekranında tekrarlanamıyor.
Oynanış testi önerileri
1. a. teslimattan sonra Garaj’a girin, renk alın, geri dönün. Turun ve hedef evin korunduğunu, 2. paketin saksı olduğunu doğrulayın. Bu, 1. düzeltmenin testi.
2. Saksıyla 2–3 kez çarptıktan sonra Esc → Menü → Başla yapın. İlk paketin 6 yapraklı sağlam bir pasta olduğunu, müşteri balonlarının kapalı olduğunu ve sayacın 55 sn’den başladığını kontrol edin.
İnceleme tamamlandı.