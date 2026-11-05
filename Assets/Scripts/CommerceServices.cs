using System;
using System.Collections.Generic;

namespace KurmaliKargo
{
    public enum CommerceOutcome { Success, Cancelled, Failed, Unavailable, Pending }
    public enum MockCommerceOutcome { Success, Cancelled, Failed, Pending }

    public sealed class AdResult
    {
        public string RequestToken { get; set; }
        public CommerceOutcome Outcome { get; set; }
        public string Message { get; set; }
        public bool Completed => Outcome == CommerceOutcome.Success;
        public bool RewardEarned { get; set; }
        public bool Closed { get; set; }
        public bool IsMock { get; set; }
    }
    public sealed class StoreProduct
    {
        string title, localizedPrice;
        public string Key { get; set; }
        public string Title { get => CommerceConfiguration.Find(Key)?.Title ?? title; set => title = value; }
        public string LocalizedPrice { get => IsMock ? GameText.Pick("TEST — ücret yok", "TEST — no charge") : localizedPrice; set => localizedPrice = value; }
        public bool Available { get; set; }
        public bool Consumable { get; set; }
        public int CoinAmount { get; set; }
        public bool IsMock { get; set; }
    }
    public sealed class StorePurchaseResult
    {
        public string ProductKey { get; set; }
        public string TransactionId { get; set; }
        public CommerceOutcome Outcome { get; set; }
        public string Message { get; set; }
        public bool IsRestore { get; set; }
        public bool RequiresConfirmation { get; set; }
        public bool IsMock { get; set; }
        public bool Completed => Outcome == CommerceOutcome.Success;
    }
    public sealed class StoreRestoreResult
    {
        public CommerceOutcome Outcome { get; set; }
        public StorePurchaseResult[] Entitlements { get; set; } = Array.Empty<StorePurchaseResult>();
        public string Message { get; set; }
        public bool IsMock { get; set; }
        public bool Completed => Outcome == CommerceOutcome.Success;
    }
    public interface IAdService : IDisposable
    {
        bool IsMock { get; }
        bool IsAvailable { get; }
        bool RewardedReady { get; }
        bool InterstitialReady { get; }
        string Status { get; }
        bool PrivacyOptionsAvailable { get; }
        void Initialize();
        void OpenPrivacyOptions(Action<bool> callback);
        void UpdatePrivacy(CommercePrivacyDecision decision);
        void ResetPrivacy();
        void ShowRewarded(string requestToken, Action<AdResult> callback);
        void ShowInterstitial(string requestToken, Action<AdResult> callback);
    }
    public interface IStoreService : IDisposable
    {
        bool IsMock { get; }
        bool IsAvailable { get; }
        string Status { get; }
        IReadOnlyList<StoreProduct> Products { get; }
        event Action<StorePurchaseResult> PurchaseReceived;
        void Initialize();
        void Purchase(string productKey, Action<StorePurchaseResult> callback);
        void Restore(Action<StoreRestoreResult> callback);
        // Invoke only after the game durably records both fulfillment and the transaction ID.
        void Confirm(string transactionId);
    }

    public static class CommerceServices
    {
        public static IAdService CreateAds() => CommerceConfiguration.IsTestSession ? (IAdService)new MockAdService() : new UnityLevelPlayAdService();
        public static IStoreService CreateStore() => CommerceConfiguration.IsTestSession ? (IStoreService)new MockStoreService() : new UnityIapStoreService();
        internal static CommerceOutcome Convert(MockCommerceOutcome value) => value == MockCommerceOutcome.Success ? CommerceOutcome.Success :
            value == MockCommerceOutcome.Cancelled ? CommerceOutcome.Cancelled : value == MockCommerceOutcome.Pending ? CommerceOutcome.Pending : CommerceOutcome.Failed;
        internal static string MockLabel => GameText.Pick("TEST • gerçek ödeme/reklam yok", "TEST • no real payment/ad");
    }

    /// <summary>Deterministic development-only service. The factory never uses it in normal player sessions.</summary>
    public sealed class MockAdService : IAdService
    {
        public MockCommerceOutcome NextOutcome { get; set; } = MockCommerceOutcome.Success;
        public bool DuplicateNextCallback { get; set; }
        public bool CloseBeforeRewardNext { get; set; }
        public int RewardedRequests { get; private set; }
        public int InterstitialRequests { get; private set; }
        public bool IsMock => true;
        public bool IsAvailable { get; private set; }
        public bool RewardedReady => IsAvailable;
        public bool InterstitialReady => IsAvailable;
        public string Status => CommerceServices.MockLabel;
        public bool PrivacyOptionsAvailable => IsAvailable;
        public void Initialize() { IsAvailable = true; }
        public void OpenPrivacyOptions(Action<bool> callback) { callback?.Invoke(IsAvailable); }
        public void UpdatePrivacy(CommercePrivacyDecision decision) { }
        public void ResetPrivacy() { }
        public void ShowRewarded(string token, Action<AdResult> callback) { RewardedRequests++; Complete(token, true, callback); }
        public void ShowInterstitial(string token, Action<AdResult> callback) { InterstitialRequests++; Complete(token, false, callback); }
        void Complete(string token, bool rewarded, Action<AdResult> callback)
        {
            var outcome = IsAvailable && !string.IsNullOrEmpty(token) ? CommerceServices.Convert(NextOutcome) : CommerceOutcome.Unavailable;
            var result = new AdResult { RequestToken = token, Outcome = outcome, RewardEarned = rewarded && outcome == CommerceOutcome.Success, Closed = true, IsMock = true, Message = Status };
            bool duplicate = DuplicateNextCallback;
            bool lateReward = CloseBeforeRewardNext && rewarded && outcome == CommerceOutcome.Success;
            NextOutcome = MockCommerceOutcome.Success; DuplicateNextCallback = false;
            CloseBeforeRewardNext = false;
            if (lateReward) callback?.Invoke(new AdResult { RequestToken = token, Outcome = CommerceOutcome.Cancelled, Closed = true, IsMock = true });
            callback?.Invoke(result);
            if (duplicate) callback?.Invoke(result);
        }
        public void Dispose() { IsAvailable = false; }
    }

    public sealed class MockStoreService : IStoreService
    {
        readonly List<StoreProduct> products = new List<StoreProduct>();
        readonly Dictionary<string, StorePurchaseResult> pending = new Dictionary<string, StorePurchaseResult>();
        readonly Dictionary<string, StorePurchaseResult> owned = new Dictionary<string, StorePurchaseResult>();
        int sequence;
        readonly string sessionId;
        public MockStoreService(string deterministicSessionId = null) { sessionId = deterministicSessionId ?? Guid.NewGuid().ToString("N"); }
        public MockCommerceOutcome NextOutcome { get; set; } = MockCommerceOutcome.Success;
        public bool DuplicateNextCallback { get; set; }
        public int ConfirmedCount { get; private set; }
        public bool IsMock => true;
        public bool IsAvailable { get; private set; }
        public string Status => CommerceServices.MockLabel;
        public IReadOnlyList<StoreProduct> Products => products;
        public event Action<StorePurchaseResult> PurchaseReceived;
        public void Initialize()
        {
            products.Clear();
            foreach (var item in CommerceConfiguration.Catalog)
                products.Add(new StoreProduct { Key = item.Key, Title = item.Title, Consumable = item.Consumable, CoinAmount = item.CoinAmount,
                    LocalizedPrice = GameText.Pick("TEST — ücret yok", "TEST — no charge"), Available = true, IsMock = true });
            IsAvailable = true;
        }
        public void Purchase(string key, Action<StorePurchaseResult> callback)
        {
            var definition = CommerceConfiguration.Find(key);
            var outcome = IsAvailable && definition != null ? CommerceServices.Convert(NextOutcome) : CommerceOutcome.Unavailable;
            bool duplicate = DuplicateNextCallback;
            NextOutcome = MockCommerceOutcome.Success; DuplicateNextCallback = false;
            var result = new StorePurchaseResult { ProductKey = key, TransactionId = outcome == CommerceOutcome.Success ? "mock-" + sessionId + "-" + (++sequence) : "",
                Outcome = outcome, RequiresConfirmation = outcome == CommerceOutcome.Success, IsMock = true, Message = Status };
            if (result.Completed) pending[result.TransactionId] = result;
            callback?.Invoke(result);
            if (duplicate) callback?.Invoke(result);
        }
        public void Confirm(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId) || !pending.TryGetValue(transactionId, out var result)) return;
            pending.Remove(transactionId); ConfirmedCount++;
            var product = CommerceConfiguration.Find(result.ProductKey);
            if (product != null && !product.Consumable) owned[product.Key] = result;
        }
        public void Restore(Action<StoreRestoreResult> callback)
        {
            var outcome = IsAvailable ? CommerceServices.Convert(NextOutcome) : CommerceOutcome.Unavailable;
            NextOutcome = MockCommerceOutcome.Success;
            var restored = new List<StorePurchaseResult>();
            if (outcome == CommerceOutcome.Success)
                foreach (var item in owned.Values)
                    restored.Add(new StorePurchaseResult { ProductKey = item.ProductKey, TransactionId = item.TransactionId, Outcome = CommerceOutcome.Success, IsRestore = true, IsMock = true });
            var result = new StoreRestoreResult { Outcome = outcome, Entitlements = restored.ToArray(), IsMock = true, Message = Status };
            bool duplicate = DuplicateNextCallback; DuplicateNextCallback = false;
            callback?.Invoke(result); if (duplicate) callback?.Invoke(result);
        }
        // Exercises store re-delivery after a callback was lost; IDs are intentionally unchanged.
        public void ReplayPending()
        { foreach (var item in new List<StorePurchaseResult>(pending.Values)) PurchaseReceived?.Invoke(item); }
        public void SeedRestorable(string key, string transactionId)
        {
            var product = CommerceConfiguration.Find(key);
            if (product == null || product.Consumable || string.IsNullOrEmpty(transactionId)) return;
            owned[key] = new StorePurchaseResult { ProductKey = key, TransactionId = transactionId, Outcome = CommerceOutcome.Success, IsRestore = true, IsMock = true };
        }
        public void Dispose() { IsAvailable = false; PurchaseReceived = null; }
    }
}
