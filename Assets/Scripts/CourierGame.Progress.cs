using System;
using System.Collections.Generic;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Local settings, fixed-slot garage, overlay navigation and resumable delivery state.</summary>
    public sealed partial class CourierGame
    {
        const string VolumeKey = "kk_volume";
        const string MotionKey = "kk_reducedMotion";
        const string TutorialKey = "kk_tutorialSeen";
        const string DecorOwnedKey = "kk_decorOwned";
        const string DecorEquippedKey = "kk_decorEquipped";
        const string RunKey = "kk_activeRun_v7";
        const string V06RunKey = "kk_activeRun_v6";
        const string V05RunKey = "kk_activeRun_v5";
        const string V04RunKey = "kk_activeRun_v4";
        const string PreviousRunKey = "kk_activeRun_v3";
        const string OlderRunKey = "kk_activeRun_v2";
        const string LegacyRunKey = "kk_activeRun_v1";
        static int DecorMask => (1 << GarageCatalog.Count) - 1;
        const string SupportEmail = "helvacikaraserdar@gmail.com";
        readonly List<GamePhase> overlayHistory = new List<GamePhase>();
        readonly Collider[] restoreOverlaps = new Collider[24];
        float volumeLevel = 1;
        float settingsSaveAt = -1;
        bool tutorialSeen, runActive;
        int ownedDecor, equippedDecor;
        GamePhase runPhase = GamePhase.Playing;
        RunSnapshot savedRun;

        public float Volume => IsMuted ? 0 : volumeLevel;
        public bool ReducedMotion { get; private set; }
        public int TutorialPage { get; private set; }
        public bool HasSavedRun => runActive || savedRun != null;
        public bool SupportAvailable => true;

        [Serializable]
        sealed class RunSnapshot
        {
            public int version = 7;
            public int district;
            public int[] pickups;
            public float timeBudget = 55;
            public int heavyIndex = -1, comboCount, lastComboBonus;
            public int reactionVariant;
            public int[] route;
            public int lastMultiplier = 1;
            public StreetState street;
            public IncidentSave incidents;
            public OrderState orders;
            public int phase;
            public int completed;
            public bool hasPackage;
            public bool parcelDropped;
            public float droppedX, droppedZ;
            public float x, z, yaw;
            public float cargo, timeLeft;
            public int activeParcel, lastParcel;
            public float lastIntegrity;
            public int lastPayout, lastSpeedBonus, lastCareBonus, score, lastBaseFee;
        }

        void LoadProgressExtras()
        {
            LoadCommerceProgress();
            LoadCompanions();
            LoadBusiness();
            districtMask = smokeTest ? 1 : (LocalProgress.GetInt(DistrictKey, 1) & 15) | 1;
            DepotLevel = smokeTest ? 1 : Mathf.Clamp(LocalProgress.GetInt(DepotLevelKey, 1), 1, MaxDepotLevel);
            VehicleIndex = smokeTest ? 0 : Mathf.Clamp(LocalProgress.GetInt(VehicleKey, 0), 0, VehicleCount - 1);
            if (!VehicleAccessible(VehicleIndex)) VehicleIndex = 0;
            volumeLevel = smokeTest ? 1 : Mathf.Clamp01(LocalProgress.GetFloat(VolumeKey, 1));
            if (!Finite(volumeLevel) || volumeLevel <= .001f) { volumeLevel = 1; if (!smokeTest) IsMuted = true; }
            ReducedMotion = !smokeTest && LocalProgress.GetInt(MotionKey, 0) == 1;
            tutorialSeen = !smokeTest && LocalProgress.GetInt(TutorialKey, 0) == 1;
            ownedDecor = smokeTest ? 0 : LocalProgress.GetInt(DecorOwnedKey, 0) & DecorMask;
            equippedDecor = smokeTest ? 0 : LocalProgress.GetInt(DecorEquippedKey, 0) & ownedDecor & DecorMask;
            TutorialPage = 0;
            runActive = false;
            savedRun = null;
            overlayHistory.Clear();
        }

        void SaveProgressExtras()
        {
            if (smokeTest) return;
            LocalProgress.SetFloat(VolumeKey, volumeLevel);
            LocalProgress.SetInt(MotionKey, ReducedMotion ? 1 : 0);
            LocalProgress.SetInt(TutorialKey, tutorialSeen ? 1 : 0);
            LocalProgress.SetInt(DecorOwnedKey, ownedDecor);
            LocalProgress.SetInt(DecorEquippedKey, equippedDecor);
            LocalProgress.SetInt(VehicleKey, VehicleIndex);
            SaveCompanions();
            SaveBusiness();
            LocalProgress.SetInt(DistrictKey, districtMask);
            LocalProgress.SetInt(DepotLevelKey, DepotLevel);
            SaveCommerceProgress();
            // Shifts are never resumable; their coins are banked per delivery.
            if (shiftActive) savedRun = null;
            else if (runActive)
            {
                RunSnapshot current = CaptureRun();
                if (ValidRun(current)) savedRun = current;
            }
            if (savedRun == null) LocalProgress.DeleteKey(RunKey);
            else LocalProgress.SetString(RunKey, JsonUtility.ToJson(savedRun));
            LocalProgress.DeleteKey(LegacyRunKey);
            LocalProgress.DeleteKey(PreviousRunKey);
            LocalProgress.DeleteKey(OlderRunKey);
            LocalProgress.DeleteKey(V04RunKey);
            LocalProgress.DeleteKey(V05RunKey);
            LocalProgress.DeleteKey(V06RunKey);
        }

        void LoadRun()
        {
            if (smokeTest) return;
            string sourceKey = RunKey;
            string json = LocalProgress.GetString(sourceKey, "");
            if (string.IsNullOrEmpty(json)) { sourceKey = V06RunKey; json = LocalProgress.GetString(sourceKey, ""); }
            if (string.IsNullOrEmpty(json)) { sourceKey = V05RunKey; json = LocalProgress.GetString(sourceKey, ""); }
            if (string.IsNullOrEmpty(json)) { sourceKey = V04RunKey; json = LocalProgress.GetString(sourceKey, ""); }
            if (string.IsNullOrEmpty(json)) { sourceKey = PreviousRunKey; json = LocalProgress.GetString(sourceKey, ""); }
            if (string.IsNullOrEmpty(json)) { sourceKey = OlderRunKey; json = LocalProgress.GetString(sourceKey, ""); }
            if (string.IsNullOrEmpty(json)) { sourceKey = LegacyRunKey; json = LocalProgress.GetString(sourceKey, ""); }
            RunSnapshot restored;
            if (TryReadRun(json, out restored))
            {
                districtMask |= 1 << restored.district;
                savedRun = restored;
            }
            else if (!string.IsNullOrEmpty(json)) LocalProgress.DeleteKey(sourceKey);
        }

        void MarkRunStarted()
        {
            ClearOverlays();
            runActive = true;
            runPhase = GamePhase.Playing;
            LastPayout = LastSpeedBonus = LastCareBonus = 0;
            LastBaseFee = DeliveryBaseFee;
            LastMultiplier = DeliveryMultiplier;
            savedRun = CaptureRun();
            if (!tutorialSeen && !smokeTest) OpenTutorial();
        }

        void MarkRunPlaying()
        {
            runActive = Completed >= 0 && Completed < 3;
            runPhase = GamePhase.Playing;
        }

        void MarkRunResult()
        {
            runPhase = GamePhase.Result;
            runActive = Completed > 0 && Completed < 3;
            savedRun = runActive ? CaptureRun() : null;
        }

        public void ResumeRun()
        {
            if (shiftActive && runActive)
            {
                ClearOverlays();
                Phase = GamePhase.Playing;
                return;
            }
            RunSnapshot restore = runActive ? CaptureRun() : savedRun;
            if (!ValidRun(restore)) return;
            ApplyRun(restore);
            Notify(GameText.Pick("Kaldığın yerden devam. Paketin seni bekliyor!", "Back where you left off. Your parcel is waiting!"));
        }

        RunSnapshot CaptureRun()
        {
            Vector3 p = player ? player.position : Vector3.zero;
            return new RunSnapshot
            {
                district = DistrictIndex, route = (int[])route.Clone(), lastMultiplier = LastMultiplier,
                pickups = (int[])pickupPlan.Clone(), timeBudget = timeBudget, heavyIndex = heavyIndex, comboCount = comboCount, lastComboBonus = LastComboBonus,
                reactionVariant = DeliveryReactionVariant,
                phase = (int)runPhase, completed = Completed, hasPackage = HasPackage, street = streetEvents.CaptureState(),
                incidents = incidents.Capture(), orders = CaptureOrders(),
                parcelDropped = IsParcelDropped, droppedX = DroppedParcelPosition.x, droppedZ = DroppedParcelPosition.z,
                x = p.x, z = p.z, yaw = player ? player.eulerAngles.y : 0,
                cargo = Cargo, timeLeft = TimeLeft, activeParcel = (int)ActiveParcel,
                lastParcel = (int)LastParcel, lastIntegrity = LastIntegrity,
                lastPayout = LastPayout, lastSpeedBonus = LastSpeedBonus,
                lastCareBonus = LastCareBonus, score = runScore, lastBaseFee = LastBaseFee
            };
        }

        static bool TryReadRun(string json, out RunSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrEmpty(json) || json.Length > 8192 || json[0] != '{') return false;
            try
            {
                RunSnapshot candidate = JsonUtility.FromJson<RunSnapshot>(json);
                if (candidate != null && candidate.version == 1)
                {
                    candidate.lastBaseFee = 40;
                    candidate.parcelDropped = false;
                    candidate.droppedX = candidate.droppedZ = 0;
                    candidate.version = 2;
                }
                if (candidate != null && candidate.version == 2)
                {
                    // Existing tours keep their roads quiet. New random closures
                    // start with the next tour, never under a returning rider.
                    candidate.street = StreetEvents.QuietState();
                    candidate.version = 3;
                }
                if (candidate != null && candidate.version == 3) candidate.version = 4;
                if (candidate != null && candidate.version == 4)
                {
                    candidate.route = new[] { 0, 1, 2 };
                    candidate.lastMultiplier = 1;
                    candidate.version = 5;
                }
                if (candidate != null && candidate.version == 5)
                {
                    candidate.pickups = new[] { 0, 0, 0 };
                    candidate.timeBudget = 55;
                    candidate.heavyIndex = -1;
                    candidate.comboCount = candidate.lastComboBonus = 0;
                    candidate.version = 6;
                }
                if (candidate != null && candidate.version == 6)
                {
                    candidate.district = 0;
                    candidate.version = 7;
                }
                if (!ValidRun(candidate)) return false;
                snapshot = candidate;
                return true;
            }
            catch (ArgumentException) { return false; }
        }

        static bool ValidRun(RunSnapshot s)
        {
            if (s == null || s.version != 7 || !ValidDistrict(s.district) || !ValidRoute(s.route) || !ValidPickupPlan(s.pickups) || !StreetEvents.ValidState(s.street) ||
                !NeighborhoodIncidents.Valid(s.incidents) || !ValidOrders(s.orders)) return false;
            if (!InRange(s.timeBudget, 28, 75) || s.heavyIndex < -1 || s.heavyIndex > 2 || (s.heavyIndex >= 0 && s.route[s.heavyIndex] != 5) || s.comboCount < 0 || s.comboCount > 3 || s.lastComboBonus < 0 || s.lastComboBonus > 10 || s.reactionVariant < 0 || s.reactionVariant > 4) return false;
            bool playing = s.phase == (int)GamePhase.Playing;
            bool result = s.phase == (int)GamePhase.Result;
            if ((!playing && !result) || s.completed < 0 || s.completed > 2 || (result && (s.completed == 0 || s.hasPackage || s.parcelDropped))) return false;
            float bound = WorldBuilder.PlayableHalfExtent - .8f;
            if (s.parcelDropped && (s.hasPackage || !InRange(s.droppedX, -bound, bound) || !InRange(s.droppedZ, -bound, bound))) return false;
            if (!Finite(s.x) || !Finite(s.z) || !Finite(s.yaw) || Mathf.Abs(s.x) > bound || Mathf.Abs(s.z) > bound) return false;
            if (!InRange(s.cargo, 0, 100) || !InRange(s.timeLeft, 0, s.timeBudget + 15) || !InRange(s.lastIntegrity, 0, 100)) return false;
            if (s.activeParcel < 0 || s.activeParcel > 2 || s.lastParcel < 0 || s.lastParcel > 2) return false;
            if ((s.hasPackage || s.parcelDropped) && s.activeParcel != s.completed) return false;
            int baseFee = s.lastBaseFee - (s.completed > 0 ? DistrictFees[s.district] : 0);
            if (!ValidOrderReceipt(baseFee, s.lastMultiplier, OrderAt(s.orders, s.completed - 1))) return false;
            if (s.score < s.completed * 35 || s.score > s.completed * 610) return false;
            if (s.completed == 0)
                return s.lastPayout == 0 && s.lastSpeedBonus == 0 && s.lastCareBonus == 0;
            return s.lastParcel == s.completed - 1 && s.lastSpeedBonus >= 0 && s.lastSpeedBonus <= 25 &&
                   s.lastCareBonus >= 0 && s.lastCareBonus <= 35 && s.lastPayout == (s.lastBaseFee + s.lastSpeedBonus + s.lastCareBonus) * s.lastMultiplier + s.lastComboBonus &&
                   s.lastPayout <= s.score;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool InRange(float value, float min, float max) => Finite(value) && value >= min && value <= max;

        void ApplyRun(RunSnapshot s)
        {
            deliveryCelebration = 0;
            ResetFeel();
            SwitchDistrict(s.district);
            streetEvents.RestoreState(s.street, new Vector3(s.x, 0, s.z));
            incidents.Configure(s.street.seed, s.incidents);
            RestoreOrders(s.orders);
            ClearOverlays();
            route = (int[])s.route.Clone();
            pickupPlan = (int[])s.pickups.Clone(); timeBudget = s.timeBudget; heavyIndex = s.heavyIndex; comboCount = s.comboCount; LastComboBonus = s.lastComboBonus;
            DeliveryReactionVariant = s.reactionVariant;
            LastMultiplier = s.lastMultiplier;
            Completed = s.completed;
            HasPackage = s.hasPackage;
            Cargo = s.cargo; TimeLeft = s.timeLeft;
            ActiveParcel = (ParcelKind)s.activeParcel;
            LastParcel = (ParcelKind)s.lastParcel; LastIntegrity = s.lastIntegrity;
            LastPayout = s.lastPayout; LastSpeedBonus = s.lastSpeedBonus; LastCareBonus = s.lastCareBonus;
            LastBaseFee = s.lastBaseFee;
            runScore = s.score;
            runPhase = (GamePhase)s.phase;
            runActive = true;
            savedRun = s;
            pointerHeld = false; velocity = Vector3.zero;
            hitCooldown = .3f; wobble = 0; pickupDelay = .7f;
            damageContacts.Clear(); contactsToRemove.Clear();
            Vector3 p = new Vector3(s.x, 0, s.z);
            if (!SafeRunPosition(p, s.yaw)) p = world.Depot;
            controller.enabled = false;
            player.position = p;
            ConfigureDistrictDogs(s.street.seed);
            if (dogs) dogs.KeepClearOf(p);
            player.rotation = Quaternion.Euler(0, Mathf.Repeat(s.yaw, 360), 0);
            previousYaw = player.eulerAngles.y;
            controller.enabled = true;
            streetEvents.KeepClearOf(player.position);
            parcelVisual.Configure(ActiveParcel);
            parcelVisual.SetOrder(CurrentOrder);
            parcelVisual.Animate(Cargo, 0, 0, 0);
            package.gameObject.SetActive(HasPackage);
            ClearDroppedParcel();
            if (s.parcelDropped)
            {
                Vector3 drop = new Vector3(s.droppedX, 0, s.droppedZ);
                if (!SafeRunPosition(drop)) drop = world.Depot;
                streetEvents.KeepClearOf(drop);
                SetDroppedParcel(drop);
                pickupDelay = 1.1f;
            }
            foreach (CustomerVisual customer in customers) customer.ResetPose();
            RefreshVipCustomer(runPhase == GamePhase.Result);
            if (runPhase == GamePhase.Result && Completed > 0) customers[route[Completed - 1]].React(LastIntegrity);
            Phase = runPhase;
            // Rewards live in the separate wallet. A restored result only redraws its receipt.
        }

        bool SafeRunPosition(Vector3 p, float? yaw = null)
        {
            Physics.SyncTransforms();
            if (UsesWideVehicleBody) return VanBodyFits(p, Quaternion.Euler(0, yaw ?? player.eulerAngles.y, 0));
            float radius = controller ? controller.radius * .86f : .3f;
            int count = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * (radius + .08f), p + Vector3.up * (radius + .28f), radius,
                restoreOverlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == restoreOverlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (restoreOverlaps[i] && restoreOverlaps[i] != controller && !restoreOverlaps[i].transform.IsChildOf(player)) return false;
            return true;
        }

        public string ExportRunForTest()
        {
            if (!smokeTest || !runActive) return "";
            RunSnapshot snapshot = CaptureRun();
            return ValidRun(snapshot) ? JsonUtility.ToJson(snapshot) : "";
        }

        public bool RestoreRunForTest(string json)
        {
            if (!smokeTest) return false;
            RunSnapshot snapshot;
            if (!TryReadRun(json, out snapshot)) return false;
            ApplyRun(snapshot);
            return true;
        }

        public void ClearOverlays()
        {
            overlayHistory.Clear();
            pointerHeld = false;
            velocity = Vector3.zero;
        }

        static bool OverlayPhase(GamePhase phase)
        {
            return phase == GamePhase.Shop || phase == GamePhase.Garage || phase == GamePhase.Settings ||
                   phase == GamePhase.Privacy || phase == GamePhase.Paused || phase == GamePhase.Tutorial;
        }

        public void OpenOverlay(GamePhase phase)
        {
            if (!OverlayPhase(phase) || Phase == phase) return;
            Save();
            overlayHistory.Add(Phase);
            Phase = phase;
            pointerHeld = false; velocity = Vector3.zero; NoticeTime = 0;
        }

        public void CloseOverlay()
        {
            CloseAdPreferences();
            if (!OverlayPhase(Phase)) return;
            GamePhase next = GamePhase.Menu;
            if (overlayHistory.Count > 0)
            {
                int last = overlayHistory.Count - 1;
                next = overlayHistory[last];
                overlayHistory.RemoveAt(last);
            }
            if (next == GamePhase.Playing && !runActive) next = GamePhase.Menu;
            Phase = next;
            pointerHeld = false; velocity = Vector3.zero; NoticeTime = 0;
            ApplyAudioSettings();
            Save();
        }

        public void PauseGame() { if (Phase == GamePhase.Playing) OpenOverlay(GamePhase.Paused); }
        public void ResumeGame() { if (Phase == GamePhase.Paused) CloseOverlay(); }
        public void OpenSettings() => OpenOverlay(GamePhase.Settings);
        public void OpenPrivacy() => OpenOverlay(GamePhase.Privacy);
        public void OpenTutorial() { TutorialPage = 0; OpenOverlay(GamePhase.Tutorial); }
        public void NextTutorial() { if (Phase == GamePhase.Tutorial) TutorialPage = Mathf.Min(2, TutorialPage + 1); }
        public void PreviousTutorial() { if (Phase == GamePhase.Tutorial) TutorialPage = Mathf.Max(0, TutorialPage - 1); }
        public void FinishTutorial()
        {
            if (Phase != GamePhase.Tutorial) return;
            tutorialSeen = true;
            CloseOverlay();
        }

        public void SettingVolume(float value)
        {
            if (!Finite(value)) return;
            value = Mathf.Clamp01(value);
            IsMuted = value <= .001f;
            if (!IsMuted) volumeLevel = value;
            ApplyAudioSettings();
            settingsSaveAt = Time.unscaledTime + .3f;
        }

        public void ApplyAudioSettings() { if (audioSource) audioSource.volume = .24f * Volume; }
        public void ToggleReducedMotion() { ReducedMotion = !ReducedMotion; Save(); }

        public int DecorCount => GarageCatalog.Count;
        public string DecorName(int index)
        {
            return GarageCatalog.Name(index);
        }
        public bool DecorOwned(int index) => index >= 0 && index < DecorCount && (ownedDecor & (1 << index)) != 0;
        public bool DecorEquipped(int index) => index >= 0 && index < DecorCount && (equippedDecor & (1 << index)) != 0;
        public int DecorPrice(int index) => index >= 0 && index < DecorCount ? GarageCatalog.Prices[index] : 0;

        public void SelectDecor(int index)
        {
            if (index < 0 || index >= DecorCount) return;
            int bit = 1 << index;
            if ((ownedDecor & bit) == 0)
            {
                if (Coins < DecorPrice(index))
                {
                    Notify(GameText.Pick("Bu dekor için birkaç teslimat daha yapmalısın.", "A few more deliveries will unlock this decoration."));
                    PlayNote(240);
                    return;
                }
                SpendCoins(DecorPrice(index));
                ownedDecor |= bit;
                equippedDecor |= bit;
            }
            else equippedDecor ^= bit;
            RefreshGarage();
            PlayNote(880);
            Save();
        }

        public void OpenGarage() { RefreshGarage(); OpenOverlay(GamePhase.Garage); }
        public void RefreshGarage()
        {
            if (!garageVisual) return;
            garageVisual.SetDecor(equippedDecor);
            garageVisual.SetScooterColor(ScooterColors[ScooterColorIndex]);
            garageVisual.SetVehicleStyle(VehicleIndex);
            garageVisual.ShowDepotLevel(DepotLevel);
        }

        public void ResetProgress()
        {
            if (!smokeTest)
            {
                string[] keys = { "coins", "best", "colors", "paint", "mute", GameText.PreferenceKey,
                    VolumeKey, MotionKey, TutorialKey, DecorOwnedKey, DecorEquippedKey, RunKey, V06RunKey, V05RunKey, V04RunKey, PreviousRunKey, OlderRunKey, LegacyRunKey, VehicleKey, DroneKey, VanKey, BusinessKey, CommerceKey, DistrictKey, DepotLevelKey };
                foreach (string key in keys) LocalProgress.DeleteKey(key);
            }
            Coins = Mathf.Min(Coins, purchases.paidCoins);
            BestScore = Completed = runScore = 0;
            districtMask = 1;
            DepotLevel = 1;
            SwitchDistrict(0);
            deliveryCelebration = 0;
            ResetFeel();
            unlockedColors = 1; ScooterColorIndex = 0;
            IsMuted = false; volumeLevel = 1; ReducedMotion = false;
            tutorialSeen = false; TutorialPage = 0;
            ownedDecor = equippedDecor = 0;
            runActive = false; savedRun = null; runPhase = GamePhase.Playing;
            VehicleIndex = 0;
            vanOwned = false;
            ResetMonsterTruckOwnership();
            ResetFantasyVehicleOwnership();
            ResetCommerceProgress();
            business = new BusinessSave();
            route = new[] { 0, 1, 2 };
            LastMultiplier = 1;
            LastComboBonus = comboCount = 0; timeBudget = 55; heavyIndex = -1; pickupPlan = new[] { 0, 0, 0 }; DeliveryReactionVariant = 0;
            if (dogs) dogs.Reset();
            drone = new DroneSave();
            if (droneVisual) droneVisual.Show(false);
            ApplyVehicleStyle();
            if (fastTrim) fastTrim.gameObject.SetActive(false);
            ClearDroppedParcel();
            if (streetEvents) streetEvents.ConfigureFixed(false, false);
            incidents.Configure(0, null);
            RestoreOrders(null);
            HasPackage = false; Cargo = 100; TimeLeft = 55;
            ActiveParcel = LastParcel = ParcelKind.Cake; LastIntegrity = 100;
            LastPayout = LastSpeedBonus = LastCareBonus = 0;
            LastBaseFee = 40;
            damageContacts.Clear(); contactsToRemove.Clear();
            ClearOverlays();
            if (controller) controller.enabled = false;
            if (player) { player.position = world.Depot; player.rotation = Quaternion.identity; }
            if (controller) controller.enabled = true;
            if (package) package.gameObject.SetActive(false);
            if (parcelVisual) parcelVisual.Configure(ParcelKind.Cake);
            if (customers != null) foreach (CustomerVisual customer in customers) if (customer) customer.ResetPose();
            if (customers != null) RefreshVipCustomer();
            if (scooterPaint) scooterPaint.color = ScooterColors[0];
            GameText.Initialize(smokeTest);
            RefreshGarage(); ApplyAudioSettings();
            Phase = GamePhase.Menu;
            Notify(GameText.Pick("Yeni bir başlangıç. Mahalle seni bekliyor!", "A fresh start. The neighborhood is waiting!"));
            Save();
        }

        public void OpenSupport()
        {
            Application.OpenURL("mailto:" + SupportEmail + "?subject=" + Uri.EscapeDataString("Kurmalı Kargo support"));
        }

        public string PrivacyText => PrivacyCopy.Text;
    }
}
