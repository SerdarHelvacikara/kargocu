using System;
using System.Collections.Generic;
using UnityEngine;
#if KARGO_UNITY_IAP
using UnityEngine.Purchasing;
#endif
#if KARGO_LEVELPLAY
using Unity.Services.LevelPlay;
#endif
#if KARGO_ATT && (UNITY_IOS || UNITY_EDITOR)
using Unity.Advertisement.IosSupport;
#endif

namespace KurmaliKargo
{
    /// <summary>Native store adapter. No dashboard ID or opt-in means no connection attempt.</summary>
    public sealed class UnityIapStoreService : IStoreService
    {
        readonly List<StoreProduct> products = new List<StoreProduct>();
        bool disposed, initializing;
        string status;
        public bool IsMock => false;
        public bool IsAvailable { get; private set; }
        public string Status => status ?? GameText.Pick("Mağaza henüz yapılandırılmadı.", "The store is not configured yet.");
        public IReadOnlyList<StoreProduct> Products => products;
        public event Action<StorePurchaseResult> PurchaseReceived;
#if KARGO_UNITY_IAP
        StoreController controller;
        readonly Dictionary<string, Product> nativeProducts = new Dictionary<string, Product>();
        readonly Dictionary<string, PendingOrder> pendingOrders = new Dictionary<string, PendingOrder>();
        Action<StorePurchaseResult> purchaseCallback;
        Action<StoreRestoreResult> restoreCallback;
        string purchasingKey;
#endif
        public async void Initialize()
        {
            if (disposed || initializing || IsAvailable) return;
            if (!CommerceConfiguration.LiveStoreEnabled || !CommerceConfiguration.StoreIdsConfigured ||
                (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)) return;
#if KARGO_UNITY_IAP
            initializing = true;
            status = GameText.Pick("Mağazaya bağlanılıyor…", "Connecting to the store…");
            try
            {
                if (controller == null)
                {
                    controller = UnityIAPServices.StoreController();
                    controller.OnStoreDisconnected += OnDisconnected;
                    controller.OnProductsFetched += OnProductsFetched;
                    controller.OnProductsFetchFailed += OnProductsFailed;
                    controller.OnPurchasePending += OnPending;
                    controller.OnPurchaseFailed += OnFailed;
                    controller.OnPurchaseDeferred += OnDeferred;
                    controller.OnPurchasesFetched += OnPurchasesFetched;
                    controller.OnPurchasesFetchFailed += OnPurchasesFailed;
                    controller.OnPurchaseConfirmed += OnConfirmed;
                }
                await controller.Connect();
                if (disposed) return;
                var definitions = new List<ProductDefinition>();
                foreach (var item in CommerceConfiguration.Catalog)
                    definitions.Add(new ProductDefinition(item.PlatformId, item.Consumable ? ProductType.Consumable : ProductType.NonConsumable));
                controller.FetchProducts(definitions);
            }
            catch (Exception) { initializing = false; status = GameText.Pick("Mağazaya bağlanılamadı. Tekrar dene.", "Store connection failed. Try again."); }
#else
            status = GameText.Pick("Bu derlemede mağaza SDK'sı yok.", "The store SDK is absent from this build.");
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }
        public void Purchase(string productKey, Action<StorePurchaseResult> callback)
        {
#if KARGO_UNITY_IAP
            if (IsAvailable && purchaseCallback == null && restoreCallback == null && nativeProducts.TryGetValue(productKey, out var product) && product.availableToPurchase)
            {
                purchaseCallback = callback; purchasingKey = productKey;
                try { controller.PurchaseProduct(product); }
                catch (Exception) { FinishPurchase(new StorePurchaseResult { ProductKey = productKey, Outcome = CommerceOutcome.Failed, Message = GameText.Pick("Satın alma başlatılamadı.", "The purchase could not be started.") }); }
                return;
            }
#endif
            callback?.Invoke(new StorePurchaseResult { ProductKey = productKey, Outcome = CommerceOutcome.Unavailable, Message = Status });
        }
        public void Restore(Action<StoreRestoreResult> callback)
        {
#if KARGO_UNITY_IAP
            if (IsAvailable && purchaseCallback == null && restoreCallback == null)
            {
                restoreCallback = callback;
                controller.RestoreTransactions((success, error) =>
                {
                    if (disposed) return;
                    if (success) controller.FetchPurchases();
                    else FinishRestore(new StoreRestoreResult { Outcome = CommerceOutcome.Failed, Message = GameText.Pick("Satın almalar geri yüklenemedi.", "Purchases could not be restored.") });
                });
                return;
            }
#endif
            callback?.Invoke(new StoreRestoreResult { Outcome = CommerceOutcome.Unavailable, Message = Status });
        }
        public void Confirm(string transactionId)
        {
#if KARGO_UNITY_IAP
            if (!disposed && controller != null && !string.IsNullOrEmpty(transactionId) && pendingOrders.TryGetValue(transactionId, out var order))
                controller.ConfirmPurchase(order);
#endif
        }
#if KARGO_UNITY_IAP
        void OnProductsFetched(List<Product> fetched)
        {
            if (disposed) return;
            products.Clear(); nativeProducts.Clear();
            foreach (var item in CommerceConfiguration.Catalog)
            {
                var native = fetched.Find(value => value.definition.id == item.PlatformId);
                if (native == null) continue;
                nativeProducts[item.Key] = native;
                products.Add(new StoreProduct { Key = item.Key, Title = item.Title, LocalizedPrice = native.metadata.localizedPriceString,
                    Available = native.availableToPurchase, Consumable = item.Consumable, CoinAmount = item.CoinAmount });
            }
            // Resolve old pending transactions before enabling fresh purchase actions.
            controller.FetchPurchases();
        }
        void OnProductsFailed(ProductFetchFailed failure)
        { initializing = false; IsAvailable = false; status = GameText.Pick("Mağaza ürünleri alınamadı.", "Store products could not be fetched."); }
        void OnDisconnected(StoreConnectionFailureDescription failure)
        {
            initializing = false; IsAvailable = false;
            status = GameText.Pick("Mağaza bağlantısı kesildi.", "The store disconnected.");
            FinishPurchase(new StorePurchaseResult { ProductKey = purchasingKey, Outcome = CommerceOutcome.Unavailable, Message = Status });
            FinishRestore(new StoreRestoreResult { Outcome = CommerceOutcome.Unavailable, Message = Status });
        }
        string KeyFor(Order order)
        {
            if (order?.CartOrdered == null) return null;
            var items = order.CartOrdered.Items();
            if (items.Count != 1 || items[0].Quantity != 1) return null;
            string id = items[0].Product.definition.id;
            foreach (var item in CommerceConfiguration.Catalog) if (item.PlatformId == id) return item.Key;
            return null;
        }
        StorePurchaseResult ResultFor(Order order, bool restore, bool pending)
        {
            string key = KeyFor(order), transactionId = order?.Info?.TransactionID;
            bool valid = key != null && !string.IsNullOrEmpty(transactionId);
            return new StorePurchaseResult { ProductKey = key, TransactionId = transactionId, Outcome = valid ? CommerceOutcome.Success : CommerceOutcome.Failed,
                IsRestore = restore, RequiresConfirmation = pending && valid,
                Message = valid ? "" : GameText.Pick("İşlem bilgisi doğrulanamadı; ürün verilmedi.", "The transaction information could not be verified; no item was granted.") };
        }
        void OnPending(PendingOrder order)
        {
            if (disposed) return;
            var result = ResultFor(order, false, true);
            if (result.Completed) pendingOrders[result.TransactionId] = order;
            if (purchaseCallback != null && result.ProductKey == purchasingKey) FinishPurchase(result);
            else PurchaseReceived?.Invoke(result);
        }
        void OnFailed(FailedOrder order)
        {
            if (disposed) return;
            var result = ResultFor(order, false, false);
            result.Outcome = order.FailureReason == PurchaseFailureReason.UserCancelled ? CommerceOutcome.Cancelled : CommerceOutcome.Failed;
            result.Message = result.Outcome == CommerceOutcome.Cancelled ? GameText.Pick("Satın alma iptal edildi.", "Purchase cancelled.") : GameText.Pick("Satın alma tamamlanamadı.", "Purchase failed.");
            FinishPurchase(result);
        }
        void OnDeferred(DeferredOrder order)
        { FinishPurchase(new StorePurchaseResult { ProductKey = KeyFor(order), Outcome = CommerceOutcome.Pending, Message = GameText.Pick("Satın alma onay bekliyor.", "Purchase is awaiting approval.") }); }
        void OnConfirmed(Order order)
        {
            // Failure leaves the durable fulfillment record untouched and pending order available for retry.
            if (order is ConfirmedOrder && !string.IsNullOrEmpty(order.Info?.TransactionID)) pendingOrders.Remove(order.Info.TransactionID);
            else if (order is FailedOrder) status = GameText.Pick("İşlem teslim edildi; mağaza onayı tekrar denenecek.", "Content was delivered; store confirmation needs retrying.");
        }
        void OnPurchasesFetched(Orders orders)
        {
            if (disposed) return;
            var restored = new List<StorePurchaseResult>();
            foreach (var order in orders.ConfirmedOrders)
            {
                var result = ResultFor(order, true, false);
                var product = CommerceConfiguration.Find(result.ProductKey);
                if (!result.Completed || product == null || product.Consumable) continue;
                restored.Add(result);
                if (restoreCallback == null) PurchaseReceived?.Invoke(result);
            }
            foreach (var pending in orders.PendingOrders) OnPending(pending);
            initializing = false; IsAvailable = products.Count > 0;
            status = IsAvailable ? GameText.Pick("Mağaza hazır.", "Store ready.") : GameText.Pick("Satılabilir ürün bulunamadı.", "No store products are available.");
            FinishRestore(new StoreRestoreResult { Outcome = CommerceOutcome.Success, Entitlements = restored.ToArray(), Message = GameText.Pick("Geri yükleme tamamlandı.", "Restore complete.") });
        }
        void OnPurchasesFailed(PurchasesFetchFailureDescription failure)
        { initializing = false; IsAvailable = false; status = GameText.Pick("Önceki satın almalar alınamadı. Tekrar dene.", "Previous purchases could not be fetched. Try again."); FinishRestore(new StoreRestoreResult { Outcome = CommerceOutcome.Failed, Message = Status }); }
        void FinishPurchase(StorePurchaseResult result)
        { var callback = purchaseCallback; purchaseCallback = null; purchasingKey = null; callback?.Invoke(result); }
        void FinishRestore(StoreRestoreResult result)
        { var callback = restoreCallback; restoreCallback = null; callback?.Invoke(result); }
#endif
        public void Dispose()
        {
            disposed = true; IsAvailable = false;
#if KARGO_UNITY_IAP
            if (controller != null)
            {
                controller.OnStoreDisconnected -= OnDisconnected; controller.OnProductsFetched -= OnProductsFetched;
                controller.OnProductsFetchFailed -= OnProductsFailed; controller.OnPurchasePending -= OnPending;
                controller.OnPurchaseFailed -= OnFailed; controller.OnPurchaseDeferred -= OnDeferred;
                controller.OnPurchasesFetched -= OnPurchasesFetched; controller.OnPurchasesFetchFailed -= OnPurchasesFailed;
                controller.OnPurchaseConfirmed -= OnConfirmed;
            }
            purchaseCallback = null; restoreCallback = null;
#endif
            PurchaseReceived = null;
        }
    }

    /// <summary>LevelPlay unit API with a hard pre-initialization consent/age gate.</summary>
    public sealed class UnityLevelPlayAdService : IAdService
    {
        readonly CommerceConsentFlow consent = new CommerceConsentFlow();
        CommercePrivacyDecision privacy;
        bool disposed, initialized, initializing;
        string status;
        public bool IsMock => false;
        public bool IsAvailable => initialized && !disposed && privacy.AllowsLiveAds && consent.CanRequestAds;
        public bool PrivacyOptionsAvailable => !disposed && consent.PrivacyOptionsRequired;
        public string Status => status ?? GameText.Pick("Canlı reklam yapılandırılmadı; gizlilik akışı gerekli.", "Live ads are not configured; a privacy flow is required.");
        public bool RewardedReady
        {
            get {
#if KARGO_LEVELPLAY
                return IsAvailable && rewarded != null && rewarded.IsAdReady() && activeReward == null && activeInterstitial == null;
#else
                return false;
#endif
            }
        }
        public bool InterstitialReady
        {
            get {
#if KARGO_LEVELPLAY
                return IsAvailable && interstitial != null && interstitial.IsAdReady() && activeInterstitial == null && activeReward == null;
#else
                return false;
#endif
            }
        }
#if KARGO_LEVELPLAY
        sealed class AdRequest { public string Token; public Action<AdResult> Callback; public bool Earned; }
        LevelPlayRewardedAd rewarded;
        LevelPlayInterstitialAd interstitial;
        bool subscribed;
        AdRequest activeReward, activeInterstitial;
        // Keep a closed impression's token for late OnAdRewarded callbacks. A close never grants a reward.
        readonly Dictionary<string, AdRequest> impressions = new Dictionary<string, AdRequest>();
#endif
        public void Initialize()
        {
            if (disposed || initializing || !CommerceConfiguration.LiveAdsEnabled || !CommerceConfiguration.AdIdsConfigured || privacy.ChildDirected ||
                (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)) return;
            if (!consent.CanRequestAds)
            {
                if (consent.IsBusy) return;
                consent.Initialize(permitted =>
                {
                    if (disposed) return;
                    privacy.FlowResolved = permitted;
                    status = consent.Status;
                    if (permitted) Initialize();
                });
                return;
            }
            privacy.FlowResolved = true;
            if (!privacy.AllowsLiveAds) return;
#if KARGO_LEVELPLAY
            if (initialized) { CreateAds(); return; }
            ApplyPrivacy();
            if (!subscribed)
            { LevelPlay.OnInitSuccess += OnInitialized; LevelPlay.OnInitFailed += OnInitFailed; subscribed = true; }
            initializing = true; status = GameText.Pick("Reklamlar hazırlanıyor…", "Preparing ads…");
            LevelPlay.Init(CommerceConfiguration.LevelPlayAppKey);
#else
            status = GameText.Pick("Bu derlemede reklam SDK'sı yok.", "The ad SDK is absent from this build.");
#endif
        }
        public void UpdatePrivacy(CommercePrivacyDecision decision)
        {
            privacy = decision;
            // A game preference or manual checkbox cannot impersonate native CMP completion.
            privacy.FlowResolved = consent.CanRequestAds;
#if KARGO_LEVELPLAY
            // Calling this facade with unresolved choices does not construct or initialize the native SDK.
            if (initialized) ApplyPrivacy();
            if (!privacy.AllowsLiveAds) DestroyAds();
            else if (initialized && !disposed) CreateAds();
#endif
            if (!privacy.ChildDirected) Initialize();
        }
        public void ResetPrivacy()
        {
            OpenPrivacyOptions(null);
        }
        public void OpenPrivacyOptions(Action<bool> callback)
        {
            if (disposed) { callback?.Invoke(false); return; }
            privacy.FlowResolved = false;
#if KARGO_LEVELPLAY
            if (initialized)
            {
                LevelPlayPrivacySettings.SetGDPRConsent(false);
                LevelPlayPrivacySettings.SetCCPA(true);
                DestroyAds();
            }
#endif
            consent.OpenOptions(permitted =>
            {
                if (disposed) return;
                privacy.FlowResolved = permitted;
                status = consent.Status;
                if (permitted) Initialize();
                callback?.Invoke(permitted);
            });
            // Production uses the native privacy-options form, never ConsentInformation.Reset().
            // System ATT permission belongs to iOS and cannot be reset by this game.
        }
        public void ShowRewarded(string token, Action<AdResult> callback)
        {
#if KARGO_LEVELPLAY
            if (RewardedReady && !string.IsNullOrEmpty(token))
            {
                activeReward = new AdRequest { Token = token, Callback = callback };
                rewarded.ShowAd(); return;
            }
            if (IsAvailable && rewarded != null) rewarded.LoadAd();
#endif
            callback?.Invoke(new AdResult { RequestToken = token, Outcome = CommerceOutcome.Unavailable, Message = Status });
        }
        public void ShowInterstitial(string token, Action<AdResult> callback)
        {
#if KARGO_LEVELPLAY
            if (InterstitialReady && !string.IsNullOrEmpty(token))
            { activeInterstitial = new AdRequest { Token = token, Callback = callback }; interstitial.ShowAd(); return; }
            if (IsAvailable && interstitial != null) interstitial.LoadAd();
#endif
            callback?.Invoke(new AdResult { RequestToken = token, Outcome = CommerceOutcome.Unavailable, Message = Status });
        }
#if KARGO_LEVELPLAY
        void ApplyPrivacy()
        {
            // UMP CanRequestAds is permission to request ads, not blanket personalization consent.
            // Keep this first implementation non-personalized/opted-out across mediated networks.
            LevelPlayPrivacySettings.SetGDPRConsent(false);
            LevelPlayPrivacySettings.SetCCPA(true);
            LevelPlayPrivacySettings.SetCOPPA(privacy.ChildDirected);
        }
        void OnInitialized(LevelPlayConfiguration configuration)
        {
            initializing = false; initialized = true;
            if (disposed || !privacy.AllowsLiveAds) return;
            CreateAds();
        }
        void CreateAds()
        {
            if (rewarded != null || interstitial != null) return;
            rewarded = new LevelPlayRewardedAd(CommerceConfiguration.RewardedAdUnitId);
            rewarded.OnAdDisplayed += OnRewardDisplayed; rewarded.OnAdRewarded += OnRewardEarned;
            rewarded.OnAdClosed += OnRewardClosed; rewarded.OnAdDisplayFailed += OnRewardFailed;
            rewarded.OnAdLoadFailed += OnLoadFailed;
            interstitial = new LevelPlayInterstitialAd(CommerceConfiguration.InterstitialAdUnitId);
            interstitial.OnAdClosed += OnInterstitialClosed; interstitial.OnAdDisplayFailed += OnInterstitialFailed;
            interstitial.OnAdLoadFailed += OnLoadFailed;
            rewarded.LoadAd(); interstitial.LoadAd();
            status = GameText.Pick("Reklamlar yükleniyor.", "Ads are loading.");
        }
        void OnInitFailed(LevelPlayInitError error)
        { initializing = false; status = GameText.Pick("Reklam servisine ulaşılamadı.", "The ad service could not be reached."); }
        void OnLoadFailed(LevelPlayAdError error)
        { status = GameText.Pick("Şu an reklam yok. Daha sonra dene.", "No ad is available. Try later."); }
        void OnRewardDisplayed(LevelPlayAdInfo info)
        {
            if (activeReward == null || string.IsNullOrEmpty(info.AdId)) return;
            // No unbounded SDK history is retained between sessions.
            if (impressions.Count > 64) impressions.Clear();
            impressions[info.AdId] = activeReward;
        }
        void OnRewardEarned(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            AdRequest request = null;
            if (!string.IsNullOrEmpty(info.AdId)) impressions.TryGetValue(info.AdId, out request);
            if (request == null) request = activeReward;
            if (request == null || request.Earned || disposed || !privacy.AllowsLiveAds) return;
            request.Earned = true;
            request.Callback?.Invoke(new AdResult { RequestToken = request.Token, Outcome = CommerceOutcome.Success, RewardEarned = true });
        }
        void OnRewardClosed(LevelPlayAdInfo info)
        {
            var request = activeReward; activeReward = null;
            // A later reward may follow this close event with the SAME token. Game dedupe must preserve it.
            request?.Callback?.Invoke(new AdResult { RequestToken = request.Token, Outcome = request.Earned ? CommerceOutcome.Success : CommerceOutcome.Cancelled,
                RewardEarned = request.Earned, Closed = true });
            if (IsAvailable) rewarded?.LoadAd();
        }
        void OnRewardFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            var request = activeReward; activeReward = null;
            request?.Callback?.Invoke(new AdResult { RequestToken = request.Token, Outcome = CommerceOutcome.Failed, Closed = true });
            if (IsAvailable) rewarded?.LoadAd();
        }
        void OnInterstitialClosed(LevelPlayAdInfo info)
        {
            var request = activeInterstitial; activeInterstitial = null;
            request?.Callback?.Invoke(new AdResult { RequestToken = request.Token, Outcome = CommerceOutcome.Success, Closed = true });
            if (IsAvailable) interstitial?.LoadAd();
        }
        void OnInterstitialFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            var request = activeInterstitial; activeInterstitial = null;
            request?.Callback?.Invoke(new AdResult { RequestToken = request.Token, Outcome = CommerceOutcome.Failed, Closed = true });
            if (IsAvailable) interstitial?.LoadAd();
        }
        void DestroyAds()
        {
            if (rewarded != null)
            {
                rewarded.OnAdDisplayed -= OnRewardDisplayed; rewarded.OnAdRewarded -= OnRewardEarned;
                rewarded.OnAdClosed -= OnRewardClosed; rewarded.OnAdDisplayFailed -= OnRewardFailed;
                rewarded.OnAdLoadFailed -= OnLoadFailed; rewarded.DestroyAd(); rewarded = null;
            }
            if (interstitial != null)
            {
                interstitial.OnAdClosed -= OnInterstitialClosed; interstitial.OnAdDisplayFailed -= OnInterstitialFailed;
                interstitial.OnAdLoadFailed -= OnLoadFailed; interstitial.DestroyAd(); interstitial = null;
            }
            activeReward = null; activeInterstitial = null; impressions.Clear();
        }
#endif
        public void Dispose()
        {
            disposed = true;
            consent.CancelPending();
#if KARGO_LEVELPLAY
            LevelPlay.OnInitSuccess -= OnInitialized; LevelPlay.OnInitFailed -= OnInitFailed;
            DestroyAds();
#endif
        }
    }

    public static class CommerceTrackingConsent
    {
        public static string Status
        {
            get {
#if KARGO_ATT && (UNITY_IOS || UNITY_EDITOR)
                if (Application.platform == RuntimePlatform.IPhonePlayer) return ATTrackingStatusBinding.GetAuthorizationTrackingStatus().ToString();
#endif
                return "NotApplicable";
            }
        }
        public static bool RequestIfConfigured(Action<string> callback)
        {
#if KARGO_ATT && UNITY_IOS && !UNITY_EDITOR
            if (CommerceConfiguration.TrackingUsageDescriptionConfigured && CommerceConfiguration.PrivacyFlowVerified &&
                (CommerceConfiguration.Audience == CommerceAudience.General13Plus || CommerceConfiguration.Audience == CommerceAudience.Adults18Plus) && Application.platform == RuntimePlatform.IPhonePlayer)
            {
                var current = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                if (current == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                    ATTrackingStatusBinding.RequestAuthorizationTracking(value => callback?.Invoke(((ATTrackingStatusBinding.AuthorizationTrackingStatus)value).ToString()));
                else callback?.Invoke(current.ToString());
                return true;
            }
#endif
            callback?.Invoke(Status); return false;
        }
    }
}
