using System;
using UnityEngine;

namespace KurmaliKargo
{
    public enum CommerceAudience { Unconfigured, General13Plus, Kids, Adults18Plus }

    /// <summary>Release-only switches and platform IDs. Empty IDs never initialize a live SDK.</summary>
    public static class CommerceConfiguration
    {
        public static bool LiveStoreEnabled = false;
        public static bool LiveAdsEnabled = false;
        public static CommerceAudience Audience = CommerceAudience.Adults18Plus;
        // Set only after a real regional CMP/age flow has been integrated and device-tested.
        public static bool PrivacyFlowVerified = false;
        public static bool UmpEnabled = false;
        // Must also match Google Mobile Ads Settings / native manifests. Publish the privacy message first.
        public static string IosAdMobAppId = "";
        public static string AndroidAdMobAppId = "";
        public static bool TrackingUsageDescriptionConfigured = false;
        public static string IosLevelPlayAppKey = "";
        public static string AndroidLevelPlayAppKey = "";
        public static string IosRewardedAdUnitId = "";
        public static string AndroidRewardedAdUnitId = "";
        public static string IosInterstitialAdUnitId = "";
        public static string AndroidInterstitialAdUnitId = "";

        public const string RemoveAds = "remove_ads";
        public const string Dragon = "dragon";
        public const string Dino = "dino";
        public const string Fleet = "fleet";
        public const string CoinsSmall = "coins_small";
        public const string CoinsMedium = "coins_medium";
        public const string CoinsLarge = "coins_large";

        public static readonly CommerceProductDefinition[] Catalog =
        {
            new CommerceProductDefinition(RemoveAds, "Reklamları kaldır", "Remove ads", false, 0),
            new CommerceProductDefinition(Dragon, "Kalıcı ejderha aracı", "Permanent dragon vehicle", false, 0),
            new CommerceProductDefinition(Dino, "Kalıcı dinozor aracı", "Permanent dinosaur vehicle", false, 0),
            new CommerceProductDefinition(Fleet, "Fantastik Filo • kalıcı araçlar", "Fantastic Fleet • permanent vehicles", false, 0),
            new CommerceProductDefinition(CoinsSmall, "Küçük jeton paketi", "Small coin pack", true, 500),
            new CommerceProductDefinition(CoinsMedium, "Orta jeton paketi", "Medium coin pack", true, 1500),
            new CommerceProductDefinition(CoinsLarge, "Büyük jeton paketi", "Large coin pack", true, 4000)
        };

        public static CommerceProductDefinition Find(string key) => Array.Find(Catalog, item => item.Key == key);
        public static bool HasId(string value) => !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf("CONFIGURE", StringComparison.OrdinalIgnoreCase) < 0 &&
            value.IndexOf("YOUR_", StringComparison.OrdinalIgnoreCase) < 0;
        public static bool StoreIdsConfigured
        {
            get { foreach (var item in Catalog) if (!HasId(item.PlatformId)) return false; return true; }
        }
        public static string LevelPlayAppKey => Application.platform == RuntimePlatform.IPhonePlayer ? IosLevelPlayAppKey : AndroidLevelPlayAppKey;
        public static string RewardedAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? IosRewardedAdUnitId : AndroidRewardedAdUnitId;
        public static string InterstitialAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? IosInterstitialAdUnitId : AndroidInterstitialAdUnitId;
        public static bool AdIdsConfigured => HasId(LevelPlayAppKey) && HasId(RewardedAdUnitId) && HasId(InterstitialAdUnitId);
        public static string AdMobAppId => Application.platform == RuntimePlatform.IPhonePlayer ? IosAdMobAppId : AndroidAdMobAppId;
        public static bool UmpConfigured => UmpEnabled && PrivacyFlowVerified && HasId(AdMobAppId);
        public static bool IsTestSession
        {
            get
            {
                if (Application.isEditor) return true;
                foreach (string value in Environment.GetCommandLineArgs())
                    if (value == "-smokeTest" || value == "-persistenceCheckWrite" || value == "-persistenceCheckRead" || value == "-playtest") return true;
                return false;
            }
        }
    }

    [Serializable]
    public sealed class CommerceProductDefinition
    {
        public readonly string Key, TitleTr, TitleEn;
        public readonly bool Consumable;
        public readonly int CoinAmount;
        // Fill these with actual dashboard IDs before enabling live store initialization.
        public string IosProductId = "", AndroidProductId = "";
        public string PlatformId => Application.platform == RuntimePlatform.IPhonePlayer ? IosProductId : AndroidProductId;
        public string Title => GameText.Pick(TitleTr, TitleEn);
        public CommerceProductDefinition(string key, string tr, string en, bool consumable, int coins)
        { Key = key; TitleTr = tr; TitleEn = en; Consumable = consumable; CoinAmount = coins; }
    }

    [Serializable]
    public struct CommercePrivacyDecision
    {
        public bool FlowResolved;
        public bool GdprConsent;
        public bool DoNotSell;
        public bool ChildDirected;
        public bool TrackingRequested;
        public bool AllowsLiveAds => FlowResolved && CommerceConfiguration.PrivacyFlowVerified &&
            (CommerceConfiguration.Audience == CommerceAudience.General13Plus || CommerceConfiguration.Audience == CommerceAudience.Adults18Plus) && !ChildDirected;
    }
}
