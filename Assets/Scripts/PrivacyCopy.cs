namespace KurmaliKargo
{
    /// <summary>
    /// In-app notice for the actual v0.8 configuration. Publisher identity, public
    /// policy URL and final mobile vendor disclosures remain release configuration.
    /// Reviewed with work/v08-sdk-notes.md and primary sources:
    /// https://unity.com/legal/game-player-and-app-user-privacy-policy
    /// https://docs.unity.com/en-us/iap/privacy-and-consent
    /// https://developers.google.com/admob/unity/privacy
    /// https://support.apple.com/en-us/102420
    /// </summary>
    public static class PrivacyCopy
    {
        public static string Text => GameText.Pick(Turkish, English);

        const string Turkish =
            "GİZLİLİK • v0.8 • 4 Kasım 2026\n\n" +
            "Oyun kaydı\n" +
            "Kurmalı Kargo hesap açmadan, çevrimdışı oynanabilir. Jetonlar, araçlar, dekorlar, tur kaydı, ayarlar ve işlem tekrarlarını önleyen kayıtlar cihazında tutulur. Geliştiricinin işlettiği bir oyun sunucusu, bulut kayıt veya ayrıca eklenmiş oyun analitiği servisi yoktur.\n\n" +
            "Bu sürümde reklam ve ödeme\n" +
            "Projede Unity LevelPlay 9.6, Unity IAP 5.4, Google UMP ve iOS ATT desteği bulunur. Mevcut canlı servis anahtarları ve ürün kimlikleri yapılandırılmamıştır; canlı reklam ve satın alma kapalıdır. Normal Windows oyununda gerçek reklam veya mağaza ödemesi sunulmaz. Editör ve açıkça belirtilen deneme profillerindeki işlemler simülasyondur; TEST olarak gösterilir ve gerçek ücret alınmaz.\n\n" +
            "Mobil servisler etkinleştirildiğinde\n" +
            "Reklam sağlayıcıları ve aracılık ortakları IP adresi, cihaz/işletim sistemi bilgileri, izin verildiğinde reklam kimliği, reklam etkileşimleri ve tanılama verilerini işleyebilir. Unity IAP ve Apple/Google mağazaları satın alma geçmişi, ürün ve işlem kimlikleri ile makbuzları teslimat, doğrulama ve geri yükleme için işleyebilir. Oyunun çevrimdışı olması, etkin SDK'ların hiç veri işlemediği anlamına gelmez. Ödemeyi mağaza işler; oyun kart bilgisi istemez.\n\n" +
            "Tercihlerin\n" +
            "Canlı mobil reklamlar uygun bölgesel gizlilik akışı tamamlanmadan başlatılmaz. Google UMP'nin gerekli formu ve kullanılabiliyorsa Ayarlar > Reklam tercihleri üzerinden seçimlerini yönetebilirsin. Bu entegrasyon reklam isteklerinde kişiselleştirme kapalı yaklaşımını kullanır; bir oyun düğmesi sistem izni yerine geçmez. iOS takibi için ATT ayrı bir izindir; iPhone Ayarlar > Gizlilik ve Güvenlik > Takip bölümünden değiştirilebilir. İzin vermemek temel oyunu engellemez. Ödüllü reklamlar isteğe bağlıdır; “Reklamları kaldır” hakkı geçiş reklamlarını kapatır, isteğe bağlı ödüllü teklifleri kaldırmaz.\n\n" +
            "Kayıtlar ve talepler\n" +
            "İlerlemeyi sıfırlamak oyun ilerlemesini ve oynayarak kazanılmış jetonları sıfırlar; harcanmamış satın alınan jeton bakiyesi, kalıcı satın alma hakları ve çift teslimatı önleyen işlem kayıtları korunur. Mağaza ve SDK kayıtları bu işlemle silinmez; sağlayıcıların saklama süreleri ve gizlilik araçları geçerlidir. Destek için e-posta gönderirsen adresin ve iletinin içeriği talebini yanıtlamak için kullanılır. Bölgenin tanıdığı erişim, düzeltme, silme veya itiraz talepleri için: helvacikaraserdar@gmail.com";

        const string English =
            "PRIVACY • v0.8 • 4 November 2026\n\n" +
            "Game saves\n" +
            "Wobbly Delivery can be played offline without an account. Coins, vehicles, decorations, the current tour, settings and transaction deduplication records are stored on your device. There is no developer-operated game server, cloud-save service or separately added gameplay analytics service.\n\n" +
            "Ads and payments in this version\n" +
            "The project includes Unity LevelPlay 9.6, Unity IAP 5.4, Google UMP and iOS ATT support. Live service keys and product IDs are not configured; live ads and purchases are disabled. Normal Windows play offers no real ads or store payments. Editor sessions and explicitly marked test profiles simulate these services, display TEST labels and make no real charges.\n\n" +
            "When mobile services are enabled\n" +
            "Ad providers and mediation partners may process IP addresses, device/operating-system details, advertising identifiers when permitted, ad interactions and diagnostics. Unity IAP and Apple/Google stores may process purchase history, product and transaction IDs, and receipts for fulfillment, verification and restoration. Offline gameplay does not mean active SDKs process no data. Payments are handled by the store; the game does not request card details.\n\n" +
            "Your choices\n" +
            "Live mobile ads do not start before the applicable regional privacy flow is completed. Manage choices through the required Google UMP form and Settings > Ad preferences when that entry point is available. This integration uses a non-personalized approach to ad requests; an in-game switch cannot replace system permission. iOS tracking requires a separate ATT permission, managed in iPhone Settings > Privacy & Security > Tracking. Declining permission does not prevent basic gameplay. Rewarded ads are optional. The Remove Ads entitlement disables interstitial ads; optional rewarded offers remain available.\n\n" +
            "Records and requests\n" +
            "Resetting progress clears gameplay progress and earned coins, while retaining unspent purchased coins, permanent purchase entitlements and transaction records that prevent duplicate fulfillment. It does not delete store or SDK records; providers apply their own retention periods and privacy tools. If you email support, your address and message are used to respond to your request. For support or access, correction, deletion or objection requests available in your region, contact helvacikaraserdar@gmail.com";
    }
}
