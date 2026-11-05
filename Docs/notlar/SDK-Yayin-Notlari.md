# v0.8 commerce SDK integration notes — 4 November 2026

## Installed and compiled

Official Unity registry packages were downloaded and resolved with Unity 6000.6.7f1: `com.unity.purchasing` **5.4.4**, `com.unity.services.levelplay` **9.6.0**, `com.unity.ads.ios-support` **1.3.0**. Manifest and packages-lock contain those pins and resolved dependencies. The follow-up adds the official Google-owned `com.google.ads.mobile` **11.6.0**, pinned to its GitHub release path in manifest; the final Windows build resolved that dependency and updated the lock. This package includes real Google UMP (native Android 4.0.0 / iOS 3.1.0) and depends on free official External Dependency Manager. No account, paid service, live product, advertising campaign or payment was created.

`KurmaliKargo.Runtime.asmdef` references the actual SDK assemblies and defines KARGO_UNITY_IAP / KARGO_LEVELPLAY / KARGO_ATT by package version; editor has a separate assembly reference. The official SDK APIs in UnityCommerceAdapters.cs compiled successfully against Unity-imported reference assemblies, including a separate iOS conditional branch syntax compile. This verifies managed signatures, **not** iOS linking, native dependencies, device operation, consent compliance, or live purchase correctness.

The first full Unity import completed package resolution but exited with unrelated game partial-method definitions still being written by root. Log `work/logs/unity-v08-sdk-import.log`. Root's final Unity build/runtime tests are the authoritative whole-game result. Independent SDK syntax artifacts and response files are in `work/v08-sdk` and are not a shipping test result.

## APIs and deliberately disabled live behavior

Four source files: CommerceConfiguration.cs (all IDs and catalog), CommerceServices.cs (interfaces/DTOs/development mocks), UnityCommerceAdapters.cs (real native adapters and ATT facade), CommerceConsentFlow.cs (actual Google UMP update/form/privacy-options flow).

Factory selects deterministic mocks only in Unity Editor or explicit -smokeTest / -persistenceCheckWrite / -persistenceCheckRead / -playtest. Normal Windows sessions return unavailable real-service facades. Test products clearly say TEST / no charge; they never display invented currency prices. Mock outcomes support success, cancellation, failure, pending approval, duplicate callbacks, late reward after close, and pending transaction replay. IDs stay the same for duplicate/replayed callbacks; a different mock instance gets a unique transaction namespace. Tests can pass an explicit session ID to MockStoreService's constructor.

Live store/ad enable flags are **false**, platform product IDs / LevelPlay app keys / rewarded and interstitial unit IDs / AdMob app IDs are **empty**. Audience defaults to **Adults18Plus**, reflecting intended marketing, not a claim about official rating or verified age. UmpEnabled and PrivacyFlowVerified remain **false** until native AdMob app IDs and published regional privacy messages/vendor lists are configured and verified. These flags alone cannot authorize ads: the actual UMP result must permit ad requests during this app session. Child-directed users are blocked from this live ad implementation.

CommerceConsentFlow calls actual `ConsentInformation.Update` once each app session, then `ConsentForm.LoadAndShowConsentFormIfRequired`, then checks `ConsentInformation.CanRequestAds`. It fails closed on errors. SDK callbacks are dispatched through Google's public main-thread executor before game/LevelPlay operations. A cached game boolean or manual UpdatePrivacy call cannot stand in for native CMP completion. Successful UMP completion is not interpreted as blanket personalization consent.

Current LevelPlay privacy APIs are SetGDPRConsent, SetCCPA and SetCOPPA. Before LevelPlay.Init the first implementation conservatively sets GDPR consent false and CCPA opt-out true, with the child flag; only UMP CanRequestAds-approved contextual/non-personalized requests can proceed. Actual native TCF/Additional Consent signals remain owned by UMP; the AdMob message must include every mediated vendor used in the final build. No code calls MobileAds.Initialize or requests an AdMob ad directly.

IAdService exposes PrivacyOptionsAvailable and OpenPrivacyOptions(Action<bool>). Required privacy entry points invoke actual `ConsentForm.ShowPrivacyOptionsForm`; ResetPrivacy routes to that native options flow and destroys cached ads first. **Production never calls UMP Reset()** (Google reserves that for tests). It does not pretend to reset iOS ATT or erase SDK records. If UMP reports options not required, the UI should hide/disable that specific options button. There is no LevelPlay auto-init settings resource, scene ad component or codeless IAP catalog in this project. Do not enable SDK auto-init from the Editor; it would bypass the game's pre-init gate.

ATT uses Unity's official ATTrackingStatusBinding only from an explicit RequestIfConfigured call, with verified privacy setup, non-child audience and a configured NSUserTrackingUsageDescription. It does not prompt on launch. ATT denial must not block gameplay or be rewarded/coerced. ATT is separate from the UMP/GDPR flow. Actual age gating, native message/app-ID setup, device verification and final SDK data disclosures remain release work.

## Delivery and restore contract

IStoreService: Initialize, Products/IsAvailable/Status, Purchase(key, callback), Restore(callback), PurchaseReceived event, Confirm(transactionId). Real localized prices come only from Product.metadata.localizedPriceString. OnPurchasePending reports a paid but unfulfilled order. The game must durably record entitlement/currency **and** deduplication transaction ID before Confirm. Store redelivery can occur at any time. Deferred or cancelled payments grant nothing. Confirmed fetched orders restore only non-consumables; previously consumed coin packs are not granted again. Explicit native RestoreTransactions is exposed; initial FetchPurchases reconciles old pending and non-consumable orders. Confirmation failure keeps the pending order for retry; the game must not grant twice when it reappears.

Catalog logical keys: remove_ads, dragon, dino, fleet (non-consumable permanent entitlements); coins_small / coins_medium / coins_large (consumable 500 / 1500 / 4000 coins). IDs are deliberately empty and must match real store products before live enablement. Dragon/dino are permanent vehicles; fleet is permanent vehicle bundle. Do not sell temporary rentals as those non-consumable products.

Receipt/transaction authenticity needs final platform validation and device verification before live release. This adapter rejects missing IDs, unknown products and multi-item/quantity orders; that structural check is not an anti-fraud server. Apple StoreKit 2 verification and Google receipt-validation strategy must be assessed for the actual target. No external validation service was purchased or configured.

IAdService: Initialize, RewardedReady/InterstitialReady, UpdatePrivacy/ResetPrivacy, ShowRewarded(token, callback), ShowInterstitial(token, callback). RewardEarned, not a close or a generic Completed flag, authorizes game reward. LevelPlay documents that reward may arrive **after** close; the adapter retains impression→token mapping. A close can first yield Cancelled/Closed, followed later by Success/RewardEarned for that same token. Root retains bounded eligibility/dedupe records. A duplicated reward event must never extend a rental or pay twice. Interstitials never grant reward. Remove-ads entitlement should suppress unsolicited interstitials; disclose clearly if optional rewarded rentals remain available.

## Required before enabling live services

Use actual store/LevelPlay product and unit IDs, configure the AdMob **application** IDs in both CommerceConfiguration and Google Mobile Ads Settings/native manifests, and publish the relevant regional privacy messages including final mediation vendors. Verify the message is available for this app before UmpEnabled/PrivacyFlowVerified/live switches are enabled. Test real UMP initial form, decline/manage-options, no consent-required region, required privacy entry point, privacy change and failure/offline states. Google app IDs use the tilde application-ID format; they are not ad unit IDs. Keep all ad SDK initialization after the UMP completion gate; this code never calls MobileAds.Initialize. Audit actual native startup behavior on devices rather than relying on old delay-app-measurement flags. The Google plugin can block a mobile build if native app IDs are absent; do not insert a live or Google sample ID merely to get a release build through validation.

Validate platform receipts, configure native ad networks/dependency resolver and privacy manifests, update privacy/support HTTPS pages and store data labels for **installed SDK behavior**, then test on iPhone/Android sandbox. Check cancellation, Ask to Buy/deferred approval, offline/reconnect, pending delivery after relaunch, restore, duplicate callbacks, no-fill, close-before-reward, consent withdrawal, ATT denial, interrupted app lifecycle and non-consumable remove-ads semantics. The installed SDK packages change the previous v0.6 statement that no ad/payment SDK was present; copy must distinguish installed adapters from inactive live services.

Intended audience selection must match actual content/marketing and the store questionnaire. Calling the app “18+” does not automatically resolve child-directed advertising obligations or secure a particular official age rating. If distribution instead targets children or mixed audiences, this implementation keeps live ads blocked until those specific requirements are addressed.

## Primary sources checked

- [Unity IAP setup and v5 StoreController](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing)
- [Unity purchases, pending fulfillment, redelivery and confirmation](https://docs.unity.com/en-us/iap/purchases)
- [Unity receipt validation](https://docs.unity.com/en-us/iap/receipt-validation)
- [LevelPlay package integration](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/package-integration)
- [LevelPlay rewarded unit callbacks and late reward ordering](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/rewarded-ad-integration-package)
- [LevelPlay interstitial unit integration](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/interstitial-integration)
- [LevelPlay pre-initialization privacy APIs](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/get-started-components)
- [LevelPlay privacy and regulatory settings](https://docs.unity.com/en-us/grow/levelplay/sdk/ios/regulation-advanced-settings)
- [Unity official ATT binding source](https://github.com/Unity-Technologies/com.unity.ads.ios-support/blob/master/com.unity.ads.ios-support/Runtime/iOSTransparency/ATTrackingStatusBinding.cs)
- [Apple App Review Guidelines 1.3, 3.1.1, 5.1](https://developer.apple.com/app-store/review/guidelines/)
- [Apple ATT documentation](https://developer.apple.com/documentation/apptrackingtransparency)
- [Google Play Families policy: child/unknown-age ads](https://support.google.com/googleplay/android-developer/answer/9893335)
- [Google UMP Unity update/forms/privacy-options/CanRequestAds documentation](https://developers.google.com/admob/unity/privacy)
- [Official Google Mobile Ads Unity plugin release v11.6.0](https://github.com/googleads/googleads-mobile-unity/releases/tag/v11.6.0)
- [Official Google consent controller sample](https://github.com/googleads/googleads-mobile-unity/blob/v11.6.0/samples/HelloWorld/Assets/Scripts/GoogleMobileAdsConsentController.cs)

SDK package code was inspected directly from the official downloaded versions for exact API signatures and auto-initialization behavior; source tarballs and registry metadata are retained under work/v08-sdk.

