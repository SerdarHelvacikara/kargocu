using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        const string VehicleKey = "kk_vehicle";
        const string MonsterKey = "kk_monster_v1";
        const string FantasyKey = "kk_fantasy_vehicles_v1";
        StreetEvents streetEvents;
        bool streetEnabled, cameraInGarage;
        int testTourSeed;
        Transform fastTrim, droppedRoot;
        ParcelVisual droppedVisual;
        MinivanVisual minivanVisual;
        MonsterTruckVisual monsterTruckVisual;
        FantasyVehicleVisual dragonVisual, dinosaurVisual;
        readonly System.Collections.Generic.List<Transform> scooterParts = new System.Collections.Generic.List<Transform>();
        bool vanOwned, monsterOwned;
        int fantasyOwnedMask;
        readonly RaycastHit[] vanSweeps = new RaycastHit[16];
        readonly Collider[] vanOverlaps = new Collider[24];
        static readonly Vector3 VanHalfSize = new Vector3(.62f, .59f, 1.04f);
        static readonly Vector3 MonsterHalfSize = new Vector3(.99f, .94f, 1.29f);
        // Flight has a high physical capsule, but its clearance sweep extends
        // down to the street so short fences do not become an accidental bypass.
        static readonly Vector3 DragonHalfSize = new Vector3(.72f, 1.60f, 1.68f);
        static readonly Vector3 DinosaurHalfSize = new Vector3(.75f, 1.32f, 1.74f);

        public StreetEvents StreetScene => streetEvents;
        public int VehicleIndex { get; private set; }
        public bool CanChangeVehicle => (Phase == GamePhase.Menu || Phase == GamePhase.Garage) &&
            !HasPackage && !IsParcelDropped && (savedRun == null || (!savedRun.hasPackage && !savedRun.parcelDropped));
        public int VehicleCount => 6;
        public bool VehicleOwned(int index) => index >= 0 && index < VehicleCount &&
            (index < 2 || (index == 2 ? vanOwned : index == 3 ? monsterOwned : (fantasyOwnedMask & (1 << (index - 4))) != 0) || RootPermanentVehicleOwned(index));
        public bool VehicleAccessible(int index) => index >= 0 && index < VehicleCount && (VehicleOwned(index) || RootRentalVehicleAccessible(index));
        public int VehiclePrice(int index) => index == 5 ? 5500 : index == 4 ? 4500 : index == 3 ? 700 : index == 2 ? 300 : 0;
        public bool HasVisibleMinivan => minivanVisual && minivanVisual.gameObject.activeSelf;
        public bool HasVisibleMonsterTruck => monsterTruckVisual && monsterTruckVisual.gameObject.activeSelf;
        public bool HasVisibleDragon => dragonVisual && dragonVisual.gameObject.activeSelf;
        public bool HasVisibleDinosaur => dinosaurVisual && dinosaurVisual.gameObject.activeSelf;
        public bool DragonParachuteVisible => dragonVisual && dragonVisual.ParachuteVisible;
        public bool MonsterTruckOwned => monsterOwned;
        public bool UsesWideVehicleBody => VehicleIndex >= 2;
        public bool UsesPhysicalSteering => VehicleIndex == 3 || VehicleIndex == 5;
        public bool DinosaurScaresDogs => VehicleIndex == 5;
        public bool DinoScaresDogs => DinosaurScaresDogs || ShiftDogTreat;
        public int DeliveryMultiplier => DeliveryMultiplierFor(VehicleIndex);
        public static int DeliveryMultiplierFor(int index) => index == 2 ? 2 : 1;
        public float VehicleSpeedScale => VehicleSpeedFor(VehicleIndex);
        public static float VehicleSpeedFor(int index) => index == 5 ? .90f : index == 4 ? 1.12f : index == 1 ? 1.32f : index == 2 ? .88f : index == 3 ? 1.10f : 1;
        public float VehicleAccelerationScale => VehicleIndex == 5 ? .70f : VehicleIndex == 4 ? .90f : VehicleIndex == 1 ? 1.15f : VehicleIndex == 2 ? .65f : VehicleIndex == 3 ? .78f : 1;
        public float VehicleFragilityScale => VehicleFragilityFor(VehicleIndex);
        public static float VehicleFragilityFor(int index) => index == 5 ? .45f : index == 4 ? .85f : index == 1 ? 1.25f : index == 2 ? .65f : index == 3 ? .85f : 1;
        public float VehicleTurnRate => VehicleIndex == 5 ? 3.8f : VehicleIndex == 3 ? 4.5f : 13;
        public float VehicleSteeringDegreesPerSecond => VehicleIndex == 5 ? 85 : 110;
        public float VehicleContactSeparation => VehicleIndex >= 4 ? 1.9f : VehicleIndex == 3 ? 1.65f : VehicleIndex == 2 ? 1.3f : controller ? controller.radius : .35f;
        Vector3 WideBodyHalfSize => VehicleIndex == 5 ? DinosaurHalfSize : VehicleIndex == 4 ? DragonHalfSize : VehicleIndex == 3 ? MonsterHalfSize : VanHalfSize;
        float WideBodyCenterHeight => VehicleIndex == 5 ? 1.34f : VehicleIndex == 4 ? 1.63f : VehicleIndex == 3 ? .96f : .72f;
        public int DeliveryBaseFee => DeliveryBaseFeeFor(VehicleIndex);
        public static int DeliveryBaseFeeFor(int index) => index == 5 ? 55 : index == 4 ? 50 : index == 2 ? 45 : index == 1 ? 35 : 40;
        public int LastBaseFee { get; private set; } = 40;
        public bool IsParcelDropped { get; private set; }
        public Vector3 DroppedParcelPosition { get; private set; }
        public string VehicleName => VehicleNameFor(VehicleIndex);
        public string VehicleHint => VehicleHintFor(VehicleIndex);
        public string VehicleNameFor(int index) => index == 5 ? GameText.Pick("Nazik dev", "Gentle giant") : index == 4 ? GameText.Pick("Bulut kargocusu", "Cloud courier") : index == 3 ? GameText.Pick("Koca teker", "Big-wheel courier") : index == 2 ? GameText.Pick("Mini kargo van", "Little cargo van") : index == 1 ? GameText.Pick("Fırlama scooter", "Zip rider") : GameText.Pick("Tatlı scooter", "Easy rider");
        public string VehicleHintFor(int index) => index == 5 ? GameText.Pick("Korunaklı, ağır adımlar • geniş dönüş • köpekler yol verir • 55 temel jeton", "Guarded cargo, heavy steps • wide turns • dogs give way • 55 base coins") : index == 4 ? GameText.Pick("Alçaktan uçar: sokak kolisi ve yol çalışmasını aşar • duvarları aşamaz • 50 temel jeton", "Low flight clears street crates and roadworks • walls still block • 50 base coins") : index == 3 ? GameText.Pick("Geniş dönüş • hafif darbeye dayanıklı, sert çarpışma pahalı • paket bağlı kalır", "Wide turns • gentle bumps cushioned, hard crashes costly • cargo stays strapped") : index == 2 ? GameText.Pick("Korunaklı, daha ağır • teslimat ücreti ve bonuslar ×2", "Protected cargo, heavier ride • delivery fee and bonuses ×2") : index == 1 ?
            GameText.Pick("%32 hızlı • 35 temel jeton • sert darbede paket düşer", "32% faster • 35 base coins • hard crashes drop cargo") :
            GameText.Pick("Dengeli sürüş • 40 temel jeton • paket bağlı kalır", "Steady ride • 40 base coins • cargo stays strapped");

        public float VehicleImpactScale(float approachSpeed) => VehicleIndex == 3 ?
            Mathf.Lerp(.45f, 2.1f, Mathf.InverseLerp(2.2f, 5, Mathf.Max(0, approachSpeed))) : 1;

        void LoadMonsterTruckOwnership() { monsterOwned = !smokeTest && LocalProgress.GetInt(MonsterKey, 0) == 1; }
        void SaveMonsterTruckOwnership() { if (!smokeTest) LocalProgress.SetInt(MonsterKey, monsterOwned ? 1 : 0); }
        void ResetMonsterTruckOwnership() { monsterOwned = false; if (!smokeTest) LocalProgress.DeleteKey(MonsterKey); }
        void LoadFantasyVehicleOwnership() { fantasyOwnedMask = smokeTest ? 0 : LocalProgress.GetInt(FantasyKey, 0) & 3; }
        void SaveFantasyVehicleOwnership() { if (!smokeTest) LocalProgress.SetInt(FantasyKey, fantasyOwnedMask & 3); }
        void ResetFantasyVehicleOwnership() { fantasyOwnedMask = 0; if (!smokeTest) LocalProgress.DeleteKey(FantasyKey); }

        public void SelectVehicle(int index)
        {
            if (!CanChangeVehicle || index < 0 || index >= VehicleCount) return;
            if (!VehicleOwned(index))
            {
                if (RootRentalVehicleAccessible(index))
                {
                    if (!RootTryEquipRentalVehicle(index)) return;
                }
                else
                {
                    if (Coins < VehiclePrice(index)) { Notify(GameText.Pick("Bu araç için biraz daha teslimat yapmalısın.", "A few more deliveries will unlock this ride.")); return; }
                    SpendCoins(VehiclePrice(index));
                    if (index == 2) vanOwned = true;
                    else if (index == 3) monsterOwned = true;
                    else fantasyOwnedMask |= 1 << (index - 4);
                }
            }
            VehicleIndex = index;
            ApplyVehicleStyle();
            if (UsesWideVehicleBody && runActive && !SafeRunPosition(player.position, player.eulerAngles.y))
            {
                controller.enabled = false;
                player.position = world.Depot;
                player.rotation = Quaternion.identity;
                controller.enabled = true;
                streetEvents.KeepClearOf(player.position);
            }
            RefreshGarage();
            Notify(VehicleName + ": " + VehicleHint);
            Save();
        }

        void BuildVehicleDetails()
        {
            fastTrim = ToyGeometry.Group(model, "Zip rider racing trim", Vector3.zero);
            Color cream = ToyGeometry.Hex("FFF0CB");
            Color red = ToyGeometry.Hex("DF7161");
            ToyGeometry.Box(fastTrim, "Racing stripe", new Vector3(0, .82f, .642f), new Vector3(.11f, .56f, .03f), cream);
            var pennant = ToyGeometry.Group(fastTrim, "Delivery pennant", new Vector3(.31f, 1.1f, -.52f));
            ToyGeometry.Cylinder(pennant, "Flag pole", new Vector3(0, .52f, 0), new Vector3(.025f, 1.04f, .025f), cream);
            ToyGeometry.Box(pennant, "Racing flag", new Vector3(.13f, .9f, 0), new Vector3(.27f, .17f, .025f), red);
            fastTrim.gameObject.SetActive(VehicleIndex == 1);
            foreach (Transform child in model) if (child != package) scooterParts.Add(child);
            minivanVisual = MinivanVisual.Create(model);
            monsterTruckVisual = MonsterTruckVisual.Create(model);
            dragonVisual = FantasyVehicleVisual.Create(model, true);
            dinosaurVisual = FantasyVehicleVisual.Create(model, false);
            ApplyVehicleStyle();
        }

        void ApplyVehicleStyle()
        {
            if (streetEvents) streetEvents.ExtraClearance = VehicleIndex >= 4 ? 1.3f : VehicleIndex == 3 ? 1.05f : VehicleIndex == 2 ? .6f : 0;
            foreach (Transform part in scooterParts) if (part) part.gameObject.SetActive(!UsesWideVehicleBody);
            if (fastTrim) fastTrim.gameObject.SetActive(VehicleIndex == 1);
            if (minivanVisual)
            {
                minivanVisual.gameObject.SetActive(VehicleIndex == 2);
            }
            if (monsterTruckVisual) monsterTruckVisual.gameObject.SetActive(VehicleIndex == 3);
            if (dragonVisual) dragonVisual.gameObject.SetActive(VehicleIndex == 4);
            if (dragonVisual && VehicleIndex != 4) dragonVisual.StopParachute();
            if (dinosaurVisual) dinosaurVisual.gameObject.SetActive(VehicleIndex == 5);
            SetVehiclePaint(ScooterColors[ScooterColorIndex]);
            Transform cargoParent = VehicleIndex == 5 && dinosaurVisual ? dinosaurVisual.CargoAnchor :
                VehicleIndex == 4 && dragonVisual ? dragonVisual.CargoAnchor :
                VehicleIndex == 3 && monsterTruckVisual ? monsterTruckVisual.CargoAnchor :
                VehicleIndex == 2 && minivanVisual ? minivanVisual.CargoAnchor : model;
            package.SetParent(cargoParent, false);
            package.localPosition = VehicleIndex >= 3 ? Vector3.up * .36f : VehicleIndex == 2 ? Vector3.up * .40f : new Vector3(0, 1.18f, -.76f);
            package.localRotation = Quaternion.identity;
            if (controller)
            {
                controller.radius = VehicleIndex >= 4 ? .65f : VehicleIndex == 3 ? .93f : VehicleIndex == 2 ? .68f : .35f;
                controller.height = VehicleIndex == 5 ? 2.64f : VehicleIndex == 4 ? 1.40f : VehicleIndex == 3 ? 1.9f : VehicleIndex == 2 ? 1.4f : .9f;
                controller.center = new Vector3(0, VehicleIndex == 5 ? 1.34f : VehicleIndex == 4 ? 2.15f : VehicleIndex == 3 ? .96f : VehicleIndex == 2 ? .7f : .48f, 0);
            }
        }

        void SetVehiclePaint(Color color)
        {
            if (minivanVisual) minivanVisual.SetColor(color);
            if (monsterTruckVisual) monsterTruckVisual.SetColor(color);
            if (dragonVisual) dragonVisual.SetColor(color);
            if (dinosaurVisual) dinosaurVisual.SetColor(color);
        }

        void AnimateVehicleVisual(float speed01, float turn01, bool reducedMotion)
        {
            if (VehicleIndex == 2 && minivanVisual) minivanVisual.Animate(speed01, turn01, reducedMotion);
            if (VehicleIndex == 3 && monsterTruckVisual) monsterTruckVisual.Animate(speed01, turn01, reducedMotion);
            bool animate = Phase == GamePhase.Playing || Phase == GamePhase.Result;
            if (dragonVisual) dragonVisual.Animate(VehicleIndex == 4 ? speed01 : 0, turn01, reducedMotion, animate && VehicleIndex == 4);
            if (dinosaurVisual) dinosaurVisual.Animate(VehicleIndex == 5 ? speed01 : 0, turn01, reducedMotion, animate && VehicleIndex == 5);
        }

        public void TriggerDragonDelivery(Vector3 destination)
        {
            if (VehicleIndex == 4 && dragonVisual && !ReducedMotion) dragonVisual.TriggerParachute(destination);
        }

        public bool DragonCanFlyOver(Collider obstacle)
        {
            if (VehicleIndex != 4 || !obstacle || obstacle.bounds.max.y > 1.05f) return false;
            string hazard = obstacle.name;
            return hazard.StartsWith("Hazard_Crate", System.StringComparison.Ordinal) || hazard == "Hazard_Roadworks";
        }

        Vector3 ConstrainVanMovement(Vector3 step)
        {
            float distance = step.magnitude;
            if (distance < .00001f) return step;
            Vector3 direction = step / distance;
            Vector3 footprint = WideFootprint(player.rotation);
            float limitX = WorldBuilder.PlayableHalfExtent - footprint.x - .04f;
            float limitZ = WorldBuilder.PlayableHalfExtent - footprint.z - .04f;
            float allowed = distance;
            if (direction.x > .0001f) allowed = Mathf.Min(allowed, Mathf.Max(0, (limitX - player.position.x) / direction.x));
            else if (direction.x < -.0001f) allowed = Mathf.Min(allowed, Mathf.Max(0, (-limitX - player.position.x) / direction.x));
            if (direction.z > .0001f) allowed = Mathf.Min(allowed, Mathf.Max(0, (limitZ - player.position.z) / direction.z));
            else if (direction.z < -.0001f) allowed = Mathf.Min(allowed, Mathf.Max(0, (-limitZ - player.position.z) / direction.z));
            int count = Physics.BoxCastNonAlloc(player.position + Vector3.up * WideBodyCenterHeight, WideBodyHalfSize, direction,
                vanSweeps, player.rotation, allowed + .035f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count >= vanSweeps.Length) return Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = vanSweeps[i];
                if (!hit.collider || hit.collider == controller || hit.collider.transform.IsChildOf(player) || DragonCanFlyOver(hit.collider) || hit.normal.y >= .65f) continue;
                if (hit.distance < .001f)
                {
                    Vector3 away = player.position - hit.collider.ClosestPoint(player.position);
                    away.y = 0;
                    if (Vector3.Dot(step, away) >= 0) continue; // Always permit backing out of an existing overlap.
                }
                allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .025f));
                HitObstacle(hit.collider, hit.normal);
            }
            return direction * allowed;
        }

        bool VanCanTurn(Quaternion rotation)
        {
            float angle = Quaternion.Angle(player.rotation, rotation);
            int samples = Mathf.Max(1, Mathf.CeilToInt(angle / 5));
            for (int i = 1; i <= samples; i++)
                if (!VanBodyFits(player.position, Quaternion.Slerp(player.rotation, rotation, i / (float)samples))) return false;
            return true;
        }

        bool VanBodyFits(Vector3 position, Quaternion rotation)
        {
            Vector3 footprint = WideFootprint(rotation);
            if (Mathf.Abs(position.x) + footprint.x + .04f > WorldBuilder.PlayableHalfExtent ||
                Mathf.Abs(position.z) + footprint.z + .04f > WorldBuilder.PlayableHalfExtent) return false;
            int count = Physics.OverlapBoxNonAlloc(position + Vector3.up * WideBodyCenterHeight, WideBodyHalfSize,
                vanOverlaps, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count >= vanOverlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (vanOverlaps[i] && vanOverlaps[i] != controller && !vanOverlaps[i].transform.IsChildOf(player) && !DragonCanFlyOver(vanOverlaps[i])) return false;
            return true;
        }

        Vector3 WideFootprint(Quaternion rotation)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 half = WideBodyHalfSize;
            return new Vector3(Mathf.Abs(right.x) * half.x + Mathf.Abs(forward.x) * half.z, 0,
                Mathf.Abs(right.z) * half.x + Mathf.Abs(forward.z) * half.z);
        }

        bool TryDropParcel(Vector3 normal, float approachSpeed)
        {
            if (VehicleIndex != 1 || !HasPackage || approachSpeed < 5.8f) return false;
            Vector3 p = player.position + normal * 1.95f;
            float edge = WorldBuilder.PlayableHalfExtent - 1.3f;
            p.x = Mathf.Clamp(p.x, -edge, edge);
            p.z = Mathf.Clamp(p.z, -edge, edge);
            p.y = 0;
            // A dropping parcel stays in the accessible lane behind the collision.
            if (!SafeDropPosition(p))
            {
                bool found = false;
                foreach (float angle in new[] { 35f, -35f, 70f, -70f })
                {
                    Vector3 candidate = player.position + Quaternion.Euler(0, angle, 0) * normal * 1.95f;
                    if (SafeDropPosition(candidate)) { p = candidate; found = true; break; }
                }
                // A tight corner may have no fair recovery position. Damage still
                // applies, but cargo stays strapped instead of creating a trap.
                if (!found) return false;
            }
            SetDroppedParcel(p);
            pickupDelay = 1.1f;
            Notify(GameText.Pick("Eyvah, paket düştü! Geri dönüp ışıklı halkadan al.", "Oops, parcel overboard! Go back to its glowing ring."));
            Save();
            return true;
        }

        bool SafeDropPosition(Vector3 p) => Mathf.Abs(p.x) <= WorldBuilder.PlayableHalfExtent - 1.3f && Mathf.Abs(p.z) <= WorldBuilder.PlayableHalfExtent - 1.3f &&
            Vector3.Distance(player.position, p) >= 1.7f && SafeRunPosition(p);

        void SetDroppedParcel(Vector3 position)
        {
            IsParcelDropped = true;
            HasPackage = false;
            DroppedParcelPosition = position;
            package.gameObject.SetActive(false);
            if (!droppedRoot)
            {
                droppedRoot = new GameObject("Dropped parcel — collect to continue").transform;
                droppedVisual = droppedRoot.gameObject.AddComponent<ParcelVisual>();
            }
            droppedRoot.position = position + Vector3.up * .43f;
            droppedRoot.rotation = Quaternion.Euler(0, 18, 0);
            droppedVisual.Configure(ActiveParcel);
            droppedVisual.SetOrder(CurrentOrder);
            droppedVisual.Animate(Cargo, 0, 0, 0);
            droppedRoot.gameObject.SetActive(true);
        }

        void ClearDroppedParcel()
        {
            IsParcelDropped = false;
            DroppedParcelPosition = Vector3.zero;
            if (droppedRoot) droppedRoot.gameObject.SetActive(false);
        }

        void RecoverDroppedParcel()
        {
            ClearDroppedParcel();
            HasPackage = true;
            package.gameObject.SetActive(true);
            parcelVisual.Animate(Cargo, 0, 0, 0);
            pickupDelay = .75f;
            // Damage and elapsed time remain. Retrieval is not a new depot pickup.
            Notify(GameText.Pick("Paket kurtarıldı. Şimdi biraz daha nazik sür!", "Parcel rescued. A gentler ride from here!"));
            PlayNote(880);
            Save();
        }

        public void EnableStreetEventsForTest(bool enabled)
        {
            if (!smokeTest) return;
            streetEnabled = enabled;
            streetEvents.gameObject.SetActive(enabled);
            if (enabled) streetEvents.ConfigureFixed(true, true);
        }

        void BeginStreetTour()
        {
            int seed = smokeTest ? ++testTourSeed : System.Guid.NewGuid().GetHashCode() & int.MaxValue;
            if (seed == streetEvents.Seed) seed = seed == int.MaxValue ? 0 : seed + 1;
            streetEvents.Configure(seed);
        }
    }
}
