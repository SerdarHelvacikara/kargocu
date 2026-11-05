using System;
using System.Collections.Generic;
using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        const string CommerceKey = "kk_commerce_v1", PurchaseKey = "kk_store_entitlements_v1";
        [Serializable] sealed class PurchaseLedger
        {
            public int version = 1, mask, paidCoins;
            public List<string> transactions = new List<string>();
        }
        [Serializable] sealed class CommerceProgress
        {
            public int version = 1, rounds, activeTour, doubledRound = -1;
            public float sinceInterstitial, rentalCooldown;
            public int rentedVehicle = -1, previousVehicle;
            public float rentalSeconds;
            public bool rentalDeferred;
            public string extraTimeTag = "", recallTag = "";
            public bool privacyChosen, personalized;
        }
        sealed class RewardContext
        {
            public string token, kind, tag;
            public int vehicle, round, amount;
            public bool rewarded, closed;
            public GamePhase returnPhase;
        }
        PurchaseLedger purchases = new PurchaseLedger();
        CommerceProgress commerce = new CommerceProgress();
        readonly Dictionary<string, RewardContext> rewardRequests = new Dictionary<string, RewardContext>();
        readonly Queue<string> rewardOrder = new Queue<string>();
        IAdService adService;
        IStoreService storeService;
        bool adBusy;
        public IAdService AdService => adService;
        public IStoreService StoreService => storeService;
        public IReadOnlyList<StoreProduct> StoreProducts => storeService != null ? storeService.Products : Array.Empty<StoreProduct>();
        public bool CommerceIsMock => adService != null && adService.IsMock && storeService != null && storeService.IsMock;
        public bool AdsReady => adService != null && adService.RewardedReady && !adBusy;
        public bool StoreReady => storeService != null && storeService.IsAvailable;
        public bool NoInterstitialAds => (purchases.mask & 1) != 0;
        public int PurchasedCoinsRemaining => purchases.paidCoins;
        public int RentalVehicle => commerce.rentedVehicle;
        public float RentalRemaining => commerce.rentalSeconds;
        public bool RentalWaitingForDelivery => commerce.rentalDeferred;
        public bool AdPreferencesOpen { get; private set; }
        public bool PersonalizedAds => commerce.privacyChosen && commerce.personalized;
        string DeliveryTag => commerce.activeTour + ":" + Completed;
        public bool CanDoubleReward => Phase == GamePhase.Result && Completed == 3 && runScore > 0 && commerce.doubledRound < commerce.rounds && AdsReady;
        public bool CanRecallParcel => Phase == GamePhase.Playing && IsParcelDropped && commerce.recallTag != DeliveryTag && AdsReady;
        public bool CanExtraTime => Phase == GamePhase.Playing && HasPackage && TimeLeft <= 10 && commerce.extraTimeTag != DeliveryTag && AdsReady;
        public bool CanRequestRental(int index) => Phase == GamePhase.Garage && CanChangeVehicle && index >= 4 && index <= 5 && !VehicleOwned(index) && commerce.rentalCooldown <= 0 && AdsReady;

        void LoadCommerceProgress()
        {
            purchases = new PurchaseLedger(); commerce = new CommerceProgress();
            if (smokeTest) return;
            try
            {
                string paid = LocalProgress.GetString(PurchaseKey, "");
                if (!string.IsNullOrEmpty(paid) && paid.Length < 1024 * 1024)
                {
                    var p = JsonUtility.FromJson<PurchaseLedger>(paid);
                    if (p != null && p.version == 1 && p.mask >= 0 && p.mask <= 7 && p.paidCoins >= 0 && p.transactions != null && p.transactions.Count <= 20000)
                    { purchases = p; purchases.paidCoins = Mathf.Min(purchases.paidCoins, Coins); }
                }
                string json = LocalProgress.GetString(CommerceKey, "");
                if (!string.IsNullOrEmpty(json) && json.Length < 4096)
                {
                    var c = JsonUtility.FromJson<CommerceProgress>(json);
                    if (c != null && c.version == 1 && c.rounds >= 0 && c.activeTour >= 0 && c.doubledRound >= -1 && c.doubledRound <= c.rounds &&
                        InRange(c.sinceInterstitial, 0, 1000000) && InRange(c.rentalCooldown, 0, 45) && InRange(c.rentalSeconds, 0, 180) &&
                        c.rentedVehicle >= -1 && c.rentedVehicle <= 5 && (c.rentedVehicle == -1 || c.rentedVehicle >= 4) && c.previousVehicle >= 0 && c.previousVehicle < 6 &&
                        (c.rentedVehicle >= 4 || (c.rentalSeconds == 0 && !c.rentalDeferred))) commerce = c;
                }
            }
            catch (ArgumentException) { }
        }
        void InitializeCommerce()
        {
            adService = CommerceServices.CreateAds(); storeService = CommerceServices.CreateStore();
            storeService.PurchaseReceived += FulfillPurchase;
            adService.Initialize(); storeService.Initialize();
        }
        void SaveCommerceProgress()
        {
            if (smokeTest) return;
            LocalProgress.SetString(CommerceKey, JsonUtility.ToJson(commerce));
            LocalProgress.SetString(PurchaseKey, JsonUtility.ToJson(purchases));
        }
        public bool RootPermanentVehicleOwned(int index) => index == 4 ? (purchases.mask & 2) != 0 : index == 5 && (purchases.mask & 4) != 0;
        public bool RootRentalVehicleAccessible(int index) => commerce.rentedVehicle == index && (commerce.rentalSeconds > 0 || commerce.rentalDeferred);
        public bool RootTryEquipRentalVehicle(int index) => RootRentalVehicleAccessible(index);

        public void PurchaseProduct(string key)
        {
            if (Phase != GamePhase.Garage || !StoreReady) return;
            storeService.Purchase(key, FulfillPurchase);
        }
        void FulfillPurchase(StorePurchaseResult result)
        {
            if (result == null || !result.Completed) { if (result != null) Notify(result.Message ?? GameText.Pick("İşlem tamamlanmadı.", "Purchase not completed.")); return; }
            if (result.IsMock != CommerceIsMock || string.IsNullOrEmpty(result.TransactionId) || result.TransactionId.Length > 512) return;
            var product = CommerceConfiguration.Find(result.ProductKey);
            if (product == null) return;
            bool recorded = purchases.transactions.Contains(result.TransactionId);
            // Restoring permanent entitlements is idempotent, even after progress
            // reset. Consumables are never fulfilled from restore or twice.
            if (!product.Consumable)
            {
                if (result.ProductKey == CommerceConfiguration.RemoveAds) purchases.mask |= 1;
                if (result.ProductKey == CommerceConfiguration.Dragon || result.ProductKey == CommerceConfiguration.Fleet) purchases.mask |= 2;
                if (result.ProductKey == CommerceConfiguration.Dino || result.ProductKey == CommerceConfiguration.Fleet) purchases.mask |= 4;
            }
            else if (!recorded && !result.IsRestore) { Coins += product.CoinAmount; purchases.paidCoins += product.CoinAmount; }
            if (!recorded) purchases.transactions.Add(result.TransactionId);
            Save(); // Durable entitlement + dedupe precede the store acknowledgment.
            storeService.Confirm(result.TransactionId);
            if (!recorded) Notify(GameText.Pick("Satın alma tamamlandı: ", "Purchase complete: ") + product.Title);
            RefreshGarage();
        }
        public void RestorePurchases()
        {
            if (Phase != GamePhase.Garage || !StoreReady) return;
            storeService.Restore(result =>
            {
                if (result == null || !result.Completed) { Notify(GameText.Pick("Geri yükleme şu an kullanılamıyor.", "Restore is unavailable right now.")); return; }
                foreach (var item in result.Entitlements) FulfillPurchase(item);
                Notify(GameText.Pick("Mağaza hakları kontrol edildi.", "Store entitlements checked."));
            });
        }

        public void RentWithAd(int index)
        {
            if (!CanRequestRental(index)) return;
            RequestReward("rent", index, 0);
        }
        public void RequestDoubleReward() { if (CanDoubleReward) RequestReward("double", -1, runScore); }
        public void RequestParcelRecall() { if (CanRecallParcel) RequestReward("recall", -1, 0); }
        public void RequestExtraTime() { if (CanExtraTime) RequestReward("time", -1, 0); }
        void RequestReward(string kind, int vehicle, int amount)
        {
            if (!AdsReady) return;
            var context = new RewardContext { token = Guid.NewGuid().ToString("N"), kind = kind, vehicle = vehicle, amount = amount,
                round = commerce.rounds, tag = DeliveryTag, returnPhase = Phase };
            rewardRequests.Add(context.token, context); rewardOrder.Enqueue(context.token);
            while (rewardOrder.Count > 64) rewardRequests.Remove(rewardOrder.Dequeue());
            adBusy = true;
            if (Phase == GamePhase.Playing) { Phase = GamePhase.Paused; pointerHeld = false; velocity = Vector3.zero; }
            adService.ShowRewarded(context.token, OnRewardResult);
        }
        void OnRewardResult(AdResult result)
        {
            if (result == null || !rewardRequests.TryGetValue(result.RequestToken ?? "", out var context) || result.IsMock != CommerceIsMock) return;
            // LevelPlay may report reward AFTER close. Retain closed contexts;
            // the reward bit, never a close callback, consumes the request.
            if (result.Closed || result.Outcome == CommerceOutcome.Cancelled || result.Outcome == CommerceOutcome.Failed || result.Outcome == CommerceOutcome.Unavailable)
            {
                if (!context.closed)
                {
                    context.closed = true; adBusy = false;
                    if (context.returnPhase == GamePhase.Playing && Phase == GamePhase.Paused) Phase = GamePhase.Playing;
                }
            }
            if (!result.Completed || !result.RewardEarned || context.rewarded) return;
            context.rewarded = true;
            if (GrantShiftReward(context.kind, context.amount)) { Save(); return; }
            if (context.kind == "rent")
            {
                int old = VehicleIndex;
                if (RootRentalVehicleAccessible(old)) old = commerce.previousVehicle;
                commerce.previousVehicle = VehicleOwned(old) ? old : 0;
                commerce.rentedVehicle = context.vehicle; commerce.rentalSeconds = 180; commerce.rentalDeferred = false; commerce.rentalCooldown = 45;
                if (CanChangeVehicle) SelectVehicle(context.vehicle);
                Notify(GameText.Pick("Kiralama hazır: 3 dakika aktif sürüş!", "Rental ready: 3 minutes of active riding!"));
            }
            else if (context.kind == "double" && commerce.doubledRound < context.round)
            { commerce.doubledRound = context.round; Coins += context.amount; Notify(GameText.Pick("Tur kazancına +", "Tour bonus +") + context.amount); }
            else if (context.kind == "time" && context.tag == DeliveryTag && HasPackage && commerce.extraTimeTag != context.tag)
            { commerce.extraTimeTag = context.tag; TimeLeft = Mathf.Min(timeBudget + 15, TimeLeft + 15); }
            else if (context.kind == "recall" && context.tag == DeliveryTag && IsParcelDropped && commerce.recallTag != context.tag)
            { commerce.recallTag = context.tag; RecoverDroppedParcel(); }
            Save();
        }
        void TickCommerce(float dt)
        {
            if (Phase != GamePhase.Playing || !Finite(dt) || dt <= 0 || adBusy) return;
            commerce.sinceInterstitial = Mathf.Min(1000000, commerce.sinceInterstitial + dt);
            commerce.rentalCooldown = Mathf.Max(0, commerce.rentalCooldown - dt);
            if (commerce.rentedVehicle < 4) return;
            commerce.rentalSeconds = Mathf.Max(0, commerce.rentalSeconds - dt);
            if (commerce.rentalSeconds > 0) return;
            if (VehicleIndex == commerce.rentedVehicle && (HasPackage || IsParcelDropped)) { commerce.rentalDeferred = true; return; }
            EndRental();
        }
        void EndRental()
        {
            if (commerce.rentedVehicle < 4) return;
            int expired = commerce.rentedVehicle, previous = commerce.previousVehicle;
            commerce.rentedVehicle = -1; commerce.rentalSeconds = 0; commerce.rentalDeferred = false;
            if (VehicleIndex == expired && !VehicleOwned(expired))
            {
                VehicleIndex = VehicleOwned(previous) ? previous : 0; ApplyVehicleStyle(); RefreshGarage();
            }
            Notify(GameText.Pick("Yardımcın dinlenmeye gitti. Garajdan tekrar çağırabilirsin.", "Your helper is resting. Call it again from the garage."));
            Save();
        }
        void CommerceDeliveryFinished()
        {
            if (Completed == 3) commerce.rounds++;
            if (commerce.rentalDeferred && commerce.rentalSeconds <= 0) EndRental();
        }
        public bool CanShowInterstitial => Phase == GamePhase.Result && Completed == 3 && !HasPackage && !IsParcelDropped && !NoInterstitialAds &&
            !adBusy && commerce.rounds > 3 && commerce.sinceInterstitial >= 240 && adService != null && adService.InterstitialReady;
        bool TryInterstitial(Action next)
        {
            if (adBusy) return true;
            if (!CanShowInterstitial) return false;
            adBusy = true;
            commerce.sinceInterstitial = 0;
            bool continued = false;
            adService.ShowInterstitial(Guid.NewGuid().ToString("N"), result =>
            {
                if (continued || result == null || result.Outcome == CommerceOutcome.Pending) return;
                if (!result.Closed && !result.Completed && result.Outcome != CommerceOutcome.Cancelled && result.Outcome != CommerceOutcome.Failed && result.Outcome != CommerceOutcome.Unavailable) return;
                continued = true; adBusy = false; next();
            });
            Save();
            return true;
        }
        void ResetCommerceProgress()
        {
            commerce = new CommerceProgress(); rewardRequests.Clear(); rewardOrder.Clear(); adBusy = false;
            // purchases and transaction IDs deliberately survive progress reset.
            SaveCommerceProgress();
        }
        void SpendCoins(int amount)
        {
            if (amount <= 0 || amount > Coins) return;
            int earned = Mathf.Max(0, Coins - purchases.paidCoins);
            purchases.paidCoins = Mathf.Max(0, purchases.paidCoins - Mathf.Max(0, amount - earned));
            Coins -= amount;
        }
        public void OpenAdPreferences()
        {
            if (!CommerceIsMock && adService != null && adService.PrivacyOptionsAvailable)
            {
                adService.OpenPrivacyOptions(success => Notify(success ? GameText.Pick("Reklam tercihleri güncellendi.", "Ad preferences updated.") : GameText.Pick("İzin ekranı şu an kullanılamıyor.", "Privacy options are unavailable right now.")));
                return;
            }
            AdPreferencesOpen = true;
        }
        public void CloseAdPreferences() { AdPreferencesOpen = false; }
        public void SetAdPrivacy(bool personalized)
        {
            if (!CommerceIsMock) { AdPreferencesOpen = false; OpenAdPreferences(); return; }
            commerce.privacyChosen = true; commerce.personalized = personalized;
            adService?.ResetPrivacy();
            adService?.UpdatePrivacy(new CommercePrivacyDecision { FlowResolved = true, GdprConsent = personalized, DoNotSell = !personalized,
                ChildDirected = CommerceConfiguration.Audience == CommerceAudience.Kids, TrackingRequested = personalized });
            Save(); AdPreferencesOpen = false;
        }
        public void TickCommerceForTest(float dt) { if (smokeTest) TickCommerce(dt); }
        public void SetRentalSecondsForTest(float value) { if (smokeTest && InRange(value, 0, 180)) commerce.rentalSeconds = value; }
        public void ForceInterstitialEligibilityForTest() { if (smokeTest) { commerce.rounds = 4; commerce.sinceInterstitial = 240; } }
        static bool ValidReceiptBase(int amount) => amount == 35 || amount == 40 || amount == 45 || amount == 50 || amount == 55 ||
            amount == 115 || amount == 120 || amount == 125 || amount == 130 || amount == 135;
    }
}
