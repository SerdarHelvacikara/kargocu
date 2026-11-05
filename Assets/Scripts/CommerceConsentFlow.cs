using System;
using UnityEngine;
#if KARGO_GOOGLE_UMP
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Common;
#endif

namespace KurmaliKargo
{
    /// <summary>Official Google UMP forms; cached game booleans cannot resolve native consent.</summary>
    public sealed class CommerceConsentFlow
    {
        bool updatedThisSession, canRequestAds, isBusy;
        int generation;
        string status;
        public bool CanRequestAds => updatedThisSession && canRequestAds && !isBusy;
        public bool IsBusy => isBusy;
        public string Status => status ?? GameText.Pick("Gizlilik mesajı yapılandırılmadı.", "The privacy message is not configured.");
        public bool PrivacyOptionsRequired
        {
            get {
#if KARGO_GOOGLE_UMP
                return updatedThisSession && !isBusy && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
#else
                return false;
#endif
            }
        }
        bool Eligible => CommerceConfiguration.UmpConfigured && !CommerceConfiguration.IsTestSession &&
            (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) &&
            (CommerceConfiguration.Audience == CommerceAudience.General13Plus || CommerceConfiguration.Audience == CommerceAudience.Adults18Plus);

        public void Initialize(Action<bool> callback)
        {
            if (isBusy) { callback?.Invoke(false); return; }
            if (!Eligible) { callback?.Invoke(false); return; }
            if (updatedThisSession) { callback?.Invoke(CanRequestAds); return; }
#if KARGO_GOOGLE_UMP
            isBusy = true; canRequestAds = false;
            MobileAdsEventExecutor.Initialize();
            int requestGeneration = ++generation;
            status = GameText.Pick("Gizlilik tercihleri güncelleniyor…", "Updating privacy choices…");
            // Actual age-gating must happen before this entry point. Children/unknown audience are blocked above.
            var parameters = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            ConsentInformation.Update(parameters, updateError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (requestGeneration != generation) return;
                if (updateError != null) { Finish(false, callback); return; }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (requestGeneration != generation) return;
                    bool permitted = formError == null && ConsentInformation.CanRequestAds();
                    updatedThisSession = formError == null;
                    Finish(permitted, callback);
                }));
            }));
#else
            status = GameText.Pick("Bu derlemede Google UMP SDK'sı yok.", "The Google UMP SDK is absent from this build.");
            callback?.Invoke(false);
#endif
        }

        public void OpenOptions(Action<bool> callback)
        {
            if (!Eligible || isBusy) { callback?.Invoke(false); return; }
            if (!updatedThisSession)
            {
                Initialize(success =>
                {
                    if (updatedThisSession && PrivacyOptionsRequired) OpenOptions(callback);
                    else callback?.Invoke(success);
                });
                return;
            }
#if KARGO_GOOGLE_UMP
            if (ConsentInformation.PrivacyOptionsRequirementStatus != PrivacyOptionsRequirementStatus.Required)
            {
                status = GameText.Pick("SDK bu bölgede ek gizlilik formu gerektirmiyor.", "The SDK does not require an additional privacy form in this region.");
                callback?.Invoke(CanRequestAds); return;
            }
            isBusy = true; canRequestAds = false;
            int requestGeneration = ++generation;
            ConsentForm.ShowPrivacyOptionsForm(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (requestGeneration != generation) return;
                Finish(formError == null && ConsentInformation.CanRequestAds(), callback);
            }));
#else
            callback?.Invoke(false);
#endif
        }
        void Finish(bool permitted, Action<bool> callback)
        {
            isBusy = false; canRequestAds = permitted;
            status = permitted ? GameText.Pick("Gizlilik akışı tamamlandı.", "The privacy flow is complete.") :
                GameText.Pick("Gizlilik akışı reklam isteğine izin vermiyor; oyun devam eder.", "The privacy flow does not permit ad requests; you can keep playing.");
            callback?.Invoke(CanRequestAds);
        }
        // Discard only this facade's session; never use UMP Reset() in production.
        public void CancelPending()
        { generation++; isBusy = false; canRequestAds = false; updatedThisSession = false; }
    }
}
