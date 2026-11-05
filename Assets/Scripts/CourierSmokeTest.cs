using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// Opt-in runtime check of the real movement, colliders, delivery loop and wallet.
    /// Activated only by -smokeTest; normal play and saved progress are unaffected.
    /// </summary>
    public sealed class CourierSmokeTest : MonoBehaviour
    {
        [Serializable]
        public sealed class Report
        {
            public bool passed;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public float wallSeconds;
            public int completedDeliveries;
            public int coins;
            public int bestScore;
            public float cargo;
            public string screenshot;
        }

        CourierGame game;
        readonly Report report = new Report();
        float started;
        bool finished;
        int completedDeliveries;
        string output;
        string firstResultSnapshot;
        float standardCakeImpactDamage;
        float standardTeaImpactDamage;

        [Serializable]
        sealed class RouteReceipt
        {
            public int version;
            public int[] route;
            public int lastMultiplier;
        }

        void Start()
        {
            Application.runInBackground = true;
            game = GetComponent<CourierGame>();
            started = Time.realtimeSinceStartup;
            output = Argument("-testOutput", Path.Combine(Application.persistentDataPath, "smoke-test.json"));
            Application.logMessageReceived += OnLog;
            StartCoroutine(Execute(Run()));
        }

        void OnDestroy() => Application.logMessageReceived -= OnLog;

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                report.errors.Add(type + ": " + condition);
        }

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - started > 600)
            {
                report.errors.Add("Runtime test timed out after 600 seconds.");
                Finish();
            }
        }

        // Flatten nested routines so exceptions in navigation also produce the report.
        IEnumerator Execute(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0 && !finished)
            {
                object current = null;
                bool moved = false;
                Exception failure = null;
                try
                {
                    moved = stack.Peek().MoveNext();
                    if (moved) current = stack.Peek().Current;
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null)
                {
                    report.errors.Add(failure.Message);
                    break;
                }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator child) stack.Push(child);
                else yield return current;
            }
            Finish();
        }

        IEnumerator Run()
        {
            yield return null;
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "-uiPreviewOnly"))
            {
                yield return PreviewInterface();
                yield break;
            }
            game.SelectVehicle(0);
            Check(game != null && game.PlayerTransform != null && game.GameCamera != null, "World, camera and courier initialize");
            Check(game.Phase == GamePhase.Menu, "Player starts at menu");
            Check(game.IsTurkish, "Isolated tests begin in Turkish");
            yield return CapturePreview("v02-Menu-TR.png");
            game.ToggleLanguage();
            Check(!game.IsTurkish && game.TargetName == "Parcel depot", "Language switch updates the depot name in English");
            yield return CapturePreview("v02-Menu-EN.png");
            game.ToggleLanguage();
            foreach (bool tr in new[] { true, false })
            {
                GameText.SetLanguage(tr);
                var lines = new HashSet<string>();
                foreach (ParcelKind kind in Enum.GetValues(typeof(ParcelKind)))
                    foreach (float integrity in new[] { 100f, 60f, 20f }) lines.Add(GameText.Reaction(kind, integrity));
                Check(lines.Count == 9 && !lines.Contains(""), "All nine package-condition reactions are distinct in " + (tr ? "Turkish" : "English"));
            }
            GameText.SetLanguage(true);
            Check(game.Coins == 0 && game.IsColorUnlocked(0), "Isolated test wallet starts empty with default paint");
            game.AddPlaytestCoins();
            Check(!game.IsPlaytest && game.Coins == 0,
                "Playtest-only coin grants cannot fund the normal smoke-test economy");
            VerifyV06UnaffordableVehiclesAndDrone();
            VerifyV07EmptyBusiness();
            game.SelectColor(1);
            Check(game.Coins == 0 && game.ScooterColorIndex == 0 && !game.IsColorUnlocked(1), "Unaffordable paint does not spend or unlock");
            game.OpenShop();
            Check(!game.ShopReturnsToResult, "Garage entered from menu keeps its menu return route");
            yield return CapturePreview("v021-Garaj-TR.png");
            game.CloseShop();
            Check(game.Phase == GamePhase.Menu && game.Coins == 0, "Closing menu garage returns to menu without changing the wallet");
            game.SelectColor(-1);
            game.SelectColor(99);
            Check(game.Coins == 0 && game.ScooterColorIndex == 0, "Invalid paint selections are ignored");
            bool muted = game.IsMuted;
            game.ToggleMute();
            Check(game.IsMuted != muted, "Sound toggle changes state");
            game.ToggleMute();

            game.StartRun();
            yield return WaitFor(() => game.HasPackage, 3, "Automatic depot pickup");
            Check(game.Cargo == 100 && game.TimeLeft > 50 && game.TargetName == "Limon Evi", "First package starts intact with first address and bonus time");
            Check(game.ActiveParcel == ParcelKind.Cake && game.CargoVisual.Kind == ParcelKind.Cake, "First delivery carries a visible cake");
            float cakeAcceleration = game.HandlingAcceleration;
            yield return CapturePreview("v02-Pasta-TR.png");
            game.ToggleLanguage();
            Check(game.HasPackage && game.Completed == 0 && game.Coins == 0 && game.Cargo == 100 && game.TargetName == "Lemon House", "Changing language during play preserves delivery state");
            yield return CapturePreview("v02-Cake-EN.png");
            game.ToggleLanguage();

            // Hit the actual west-road crate, then steer around it.
            yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
            float beforeHit = game.Cargo;
            float until = Time.realtimeSinceStartup + .65f;
            while (Time.realtimeSinceStartup < until)
            {
                game.MoveWorld(Vector3.forward);
                yield return null;
            }
            game.MoveWorld(Vector3.zero);
            Check(game.Cargo < beforeHit && game.Cargo >= 0, "Real obstacle collision reduces package integrity");
            float cakeDamage = beforeHit - game.Cargo;
            standardCakeImpactDamage = cakeDamage;
            Check(game.CargoVisual.ImpactCount > 0, "Cake impact triggers visible damage feedback");
            float afterFirstImpact = game.Cargo;
            int firstImpactCount = game.CargoVisual.ImpactCount;
            until = Time.realtimeSinceStartup + 2.2f;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
            Check(game.Cargo == afterFirstImpact && game.CargoVisual.ImpactCount == firstImpactCount,
                "Holding against the same obstacle never repeats crash damage or effects");
            yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
            yield return DriveTo(new Vector3(-7.2f, 0, -3.25f));
            yield return DriveTo(new Vector3(-7.2f, 0, 7), true);
            yield return VerifyDelivery(1, 0);
            firstResultSnapshot = game.ExportRunForTest();
            Check(game.LastParcel == ParcelKind.Cake && game.LastIntegrity < 80 && game.LastIntegrity >= 40 && game.ResultLine.Contains("Krema"), "Dented cake gets its specific customer reaction");
            yield return CapturePreview("v02-Pasta-Teslimat-TR.png");

            int coinsAfterFirst = game.Coins;
            game.OpenShop();
            Check(game.ShopReturnsToResult && game.Phase == GamePhase.Shop, "Garage entered after delivery remembers the result screen");
            game.SelectColor(0);
            game.CloseShop();
            Check(game.Phase == GamePhase.Result && game.Completed == 1 && game.Coins == coinsAfterFirst && game.LastParcel == ParcelKind.Cake,
                "Garage round trip preserves unfinished tour, wallet and delivered package");
            game.ContinueDelivery();
            game.ContinueDelivery();
            Check(game.Phase == GamePhase.Playing && !game.HasPackage && game.Coins == coinsAfterFirst,
                "Continue is idempotent and asks for the next depot pickup");
            game.PlaceForTest(new Vector3(-8, 0, -8));
            yield return WaitFor(() => game.HasPackage, 3, "Second automatic depot pickup");
            Check(game.Cargo == 100 && game.TargetName == "Lavanta Evi", "Second pickup restores integrity and advances address");
            Check(game.ActiveParcel == ParcelKind.Plant && game.CargoVisual.Kind == ParcelKind.Plant && game.CargoVisual.VisibleLeafCount > 0, "Second delivery carries a leafy plant");
            Check(game.HandlingAcceleration < cakeAcceleration, "Heavy plant accelerates more slowly than cake");
            int initialLeaves = game.CargoVisual.VisibleLeafCount;
            yield return CapturePreview("v02-Saksi-TR.png");
            yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
            beforeHit = game.Cargo;
            until = Time.realtimeSinceStartup + .65f;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
            Check(beforeHit - game.Cargo < cakeDamage && game.Cargo < beforeHit, "Plant resists the same real obstacle better than cake");
            for (int attempt = 0; attempt < 6 && game.Cargo >= 40; attempt++)
            {
                yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
                yield return new WaitForSecondsRealtime(1.2f);
                float previousCargo = game.Cargo;
                until = Time.realtimeSinceStartup + .65f;
                while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
                Check(game.Cargo < previousCargo, "Separating then making a fresh collision damages cargo, impact " + (attempt + 2));
            }
            Check(game.Cargo < 40 && game.CargoVisual.VisibleLeafCount < initialLeaves, "Repeated real collisions visibly strip the damaged plant");
            game.MoveWorld(Vector3.zero);
            yield return CapturePreview("v02-Saksi-Hasarli.png");
            yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
            yield return DriveTo(new Vector3(-7.2f, 0, -3.25f));
            yield return DriveTo(new Vector3(-7.2f, 0, 8));
            yield return DriveTo(new Vector3(8, 0, 8), true);
            yield return VerifyDelivery(2, coinsAfterFirst);
            Check(game.LastParcel == ParcelKind.Plant && game.LastIntegrity < 40 && game.ResultLine.Contains("süpürge"), "Badly damaged plant gets its specific customer reaction");
            game.ToggleLanguage();
            Check(game.ResultLine.Contains("broom"), "Customer reaction also updates live in English");
            yield return CapturePreview("v02-Plant-Result-EN.png");
            game.ToggleLanguage();

            int coinsAfterSecond = game.Coins;
            game.ContinueDelivery();
            game.PlaceForTest(new Vector3(-8, 0, -8));
            yield return WaitFor(() => game.HasPackage, 3, "Third automatic depot pickup");
            Check(game.TargetName == "Nane Evi", "Third address selected");
            Check(game.ActiveParcel == ParcelKind.TeaSet && game.CargoVisual.Kind == ParcelKind.TeaSet, "Third delivery carries a visible tea set");
            yield return CapturePreview("v02-Cay-TR.png");
            yield return DriveTo(new Vector3(-7.2f, 0, -8));
            yield return DriveTo(new Vector3(-7.2f, 0, 0));
            yield return DriveTo(new Vector3(8.45f, 0, 0));
            beforeHit = game.Cargo;
            until = Time.realtimeSinceStartup + .9f;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
            Check(beforeHit - game.Cargo > cakeDamage, "Tea set suffers more damage from the same obstacle type than cake");
            standardTeaImpactDamage = beforeHit - game.Cargo;
            Check(game.CargoVisual.ImpactCount > 0, "Tea impact triggers package feedback");
            yield return DriveTo(new Vector3(8.45f, 0, 0));
            yield return DriveTo(new Vector3(7.2f, 0, 0));
            yield return DriveTo(new Vector3(7.2f, 0, -7.2f), true);
            yield return VerifyDelivery(3, coinsAfterSecond);
            Check(game.ResultTitle == "Mahallenin yıldızı!" && game.BestScore == game.Coins,
                "Third delivery completes the session and records its score");

            string screenshot = Argument("-testScreenshot", "");
            if (!string.IsNullOrEmpty(screenshot))
            {
                screenshot = Path.GetFullPath(screenshot);
                Directory.CreateDirectory(Path.GetDirectoryName(screenshot));
                yield return new WaitForSecondsRealtime(.7f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(screenshot);
                report.screenshot = screenshot;
                yield return new WaitForSecondsRealtime(.6f);
            }

            int earned = game.Coins;
            game.OpenShop();
            game.SelectColor(1);
            Check(game.IsColorUnlocked(1) && game.ScooterColorIndex == 1 && game.Coins == earned - 120,
                "Earned coins buy and equip a paint color for its displayed price");
            int afterPurchase = game.Coins;
            game.SelectColor(1);
            Check(game.Coins == afterPurchase, "Equipping owned paint never charges again");
            game.SelectColor(0);
            Check(game.Coins == afterPurchase && game.ScooterColorIndex == 0, "Default paint remains freely selectable");
            game.CloseShop();
            Check(game.Phase == GamePhase.Result && game.Completed == 3 && game.Coins == afterPurchase,
                "Garage purchase returns to the completed result without duplicating rewards");
            int best = game.BestScore;
            game.ShowMenu();
            Check(game.Phase == GamePhase.Menu && game.Coins == afterPurchase, "Menu keeps earned progress");
            game.RestartRun();
            Check(game.Completed == 0 && !game.HasPackage && game.Coins == afterPurchase && game.BestScore == best,
                "New run resets deliveries while preserving wallet and best score");
            yield return VerifyReleaseFeatures();
            yield return VerifyLegacyResultMigration();
            yield return VerifyV04Features();
            yield return VerifyV05StreetTours();
            yield return VerifyV06GarageProgression();
            yield return VerifyV07ExpandedNeighborhood();
            yield return VerifyV07Dogs();
            VerifyCompactLayoutGeometry();
        }

        IEnumerator PreviewInterface()
        {
            Check(game.Phase == GamePhase.Menu && game.Coins == 0, "UI preview starts in an isolated empty menu");
            yield return CapturePreview("Menu-TR.png", .25f);
            game.ToggleLanguage();
            yield return CapturePreview("Menu-EN.png", .2f);
            game.OpenSettings();
            Check(game.Phase == GamePhase.Settings, "Phone settings page opens");
            yield return CapturePreview("Ayarlar-EN.png", .2f);
            game.ToggleLanguage();
            game.OpenPrivacy();
            Check(game.Phase == GamePhase.Privacy, "Phone privacy page opens over settings");
            yield return CapturePreview("Gizlilik-TR.png", .2f);
            game.CloseOverlay(); game.CloseOverlay();
            game.OpenTutorial();
            yield return CapturePreview("Ogretici-1.png", .2f);
            game.NextTutorial(); game.NextTutorial();
            yield return CapturePreview("Ogretici-3.png", .2f);
            game.FinishTutorial();
            game.OpenGarage();
            yield return null;
            game.GarageScreen.SelectTabForTest(0);
            yield return CapturePreview("Garaj-Araclar.png", .3f);
            Check(game.GarageScreen.CatalogFitsViewport, "Canvas cards match the compact viewport width without clipping");
            game.GarageScreen.SelectTabForTest(1);
            yield return CapturePreview("Garaj-Drone.png", .2f);
            game.GarageScreen.SelectTabForTest(2);
            yield return CapturePreview("Garaj-Dekor.png", .2f);
            game.ToggleLanguage();
            yield return CapturePreview("Garaj-Decor-EN.png", .2f);
            int previewWallet = game.Coins;
            game.GarageScreen.PreviewRoomForTest(true);
            yield return CapturePreview("Garaj-Oda-EN.png", .2f);
            Check(game.Phase == GamePhase.Garage && game.Coins == previewWallet, "Room preview preserves the garage and wallet");
            game.GarageScreen.PreviewRoomForTest(false);
            yield return CapturePreview("Garaj-Dukkana-Donus.png", .2f);
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Menu && game.Coins == 0 && !game.DroneOwned && !game.VehicleOwned(2),
                "Page previews do not spend, unlock or fabricate progress");
            Check(GuiViewport.Compact, "The compact phone interface is selected for the preview");
        }

        void VerifyV06UnaffordableVehiclesAndDrone()
        {
            int wallet = game.Coins;
            game.OpenGarage();
            Check(game.VehicleCount == 6 && game.VehicleOwned(0) && game.VehicleOwned(1) && !game.VehicleOwned(2) && !game.VehicleOwned(3)
                && !game.VehicleOwned(4) && !game.VehicleOwned(5),
                "Garage starts with two free scooters, a locked minivan and a locked monster truck");
            Check(game.VehiclePrice(2) == 300 && game.VehiclePrice(3) == 700 && game.DronePrice == 250 && game.DroneReward == 25,
                "Premium vehicles and drone expose their actual progression prices and mission reward");
            game.BuyDrone();
            game.LaunchDrone();
            Check(!game.DroneOwned && !game.DroneBusy && !game.CanLaunchDrone && game.Coins == wallet,
                "An empty wallet cannot buy or launch a drone");
            game.SelectVehicle(2);
            Check(!game.VehicleOwned(2) && game.VehicleIndex == 0 && game.Coins == wallet,
                "An empty wallet cannot unlock or equip the minivan");
            game.SelectVehicle(3);
            Check(!game.VehicleOwned(3) && game.VehicleIndex == 0 && game.Coins == wallet,
                "An empty wallet cannot unlock or equip the monster truck");
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Menu && game.Coins == wallet,
                "Unaffordable garage actions return to the original menu without spending coins");
        }

        IEnumerator VerifyV06GarageProgression()
        {
            // Earn the economy through the same physical depot/customer routes.
            // The only accelerated clock belongs to the real drone mission tick.
            VerifyRestoredTruckFallbackClearance();
            game.StartRun();
            for (int delivery = 0; delivery < 3; delivery++)
            {
                if (delivery > 0) yield return ReturnForNextParcel();
                else yield return WaitFor(() => game.HasPackage, 3, "Progression tour picks up its first package");
                yield return CompleteCleanCurrentDelivery();
            }
            Check(game.Completed == 3 && game.Coins >= game.DronePrice,
                "A complete clean tour earns enough real delivery coins to purchase the drone");
            game.OpenGarage();
            int wallet = game.Coins;
            game.BuyDrone();
            Check(game.DroneOwned && !game.DroneBusy && game.Coins == wallet - game.DronePrice && game.CanLaunchDrone,
                "Drone purchase deducts its price and enables missions in the garage");
            wallet = game.Coins;
            game.BuyDrone();
            Check(game.Coins == wallet && game.DroneOwned, "Buying an already owned drone never charges a second time");
            int flightsBefore = game.DroneCompletedFlights;
            game.LaunchDrone();
            Check(game.DroneBusy && !game.CanLaunchDrone && !string.IsNullOrEmpty(game.DroneStatus),
                "Launching an owned drone starts one mission with a visible status");
            float parkedProgress = game.DroneProgress;
            game.AdvanceDroneForTest(40);
            yield return new WaitForSecondsRealtime(.2f);
            Check(game.DroneBusy && Mathf.Abs(game.DroneProgress - parkedProgress) < .001f && game.Coins == wallet,
                "A drone mission cannot progress or pay while the player stays in the garage");
            game.OpenSettings();
            game.AdvanceDroneForTest(40);
            yield return new WaitForSecondsRealtime(.2f);
            Check(game.DroneBusy && Mathf.Abs(game.DroneProgress - parkedProgress) < .001f && game.Coins == wallet,
                "Settings also freezes the global drone mission");
            game.CloseOverlay();
            game.GarageScreen.SelectTabForTest(1);
            yield return CapturePreview("v06-Drone-Garaj.png", .3f);
            game.CloseOverlay();

            game.StartRun();
            yield return WaitFor(() => game.HasPackage, 3, "Second progression tour loads cargo while its drone is working");
            string inFlightRun = game.ExportRunForTest();
            Check(!string.IsNullOrEmpty(inFlightRun) && game.DroneBusy,
                "An ordinary delivery snapshot can be captured while the global drone is in flight");
            float flyingProgress = game.DroneProgress;
            game.AdvanceDroneForTest(1);
            Check(game.DroneBusy && game.DroneProgress > flyingProgress,
                "Active play advances the actual drone mission clock");
            game.AdvanceDroneForTest(10);
            Check(game.DroneBusy && game.DroneProgress > .25f && game.DroneProgress < .7f,
                "The working drone reaches its visible outbound flight before completion");
            yield return CapturePreview("v06-Drone-Ucusta.png", .3f);
            wallet = game.Coins;
            game.AdvanceDroneForTest(40);
            Check(!game.DroneBusy && game.Coins == wallet + game.DroneReward && game.DroneCompletedFlights == flightsBefore + 1,
                "Completing the first drone mission credits exactly its stated reward");
            wallet = game.Coins;
            game.AdvanceDroneForTest(60);
            Check(game.Coins == wallet && !game.DroneBusy, "A completed drone mission cannot pay again on later ticks");
            Check(game.RestoreRunForTest(inFlightRun) && game.DroneOwned && !game.DroneBusy && game.Coins == wallet
                && game.DroneCompletedFlights == flightsBefore + 1,
                "Restoring an older delivery snapshot cannot rewind or re-award the global drone mission");
            game.AdvanceDroneForTest(60);
            Check(game.Coins == wallet && !game.DroneBusy,
                "Ticks after old-run restoration cannot duplicate the first drone reward");

            yield return CompleteCleanCurrentDelivery();
            yield return ReturnForNextParcel();
            yield return CompleteCleanCurrentDelivery();
            Check(game.Completed == 2 && game.Coins >= game.VehiclePrice(2),
                "Five clean deliveries and one drone mission earn the minivan price without artificial funds");
            game.OpenGarage();
            float ordinarySpeed = game.HandlingSpeed;
            float ordinaryFragility = game.Fragility;
            wallet = game.Coins;
            int vanPrice = game.VehiclePrice(2);
            game.SelectVehicle(2);
            Check(game.VehicleOwned(2) && game.VehicleIndex == 2 && game.HasVisibleMinivan && game.Coins == wallet - vanPrice,
                "Minivan purchase unlocks and equips its actual vehicle for the listed price");
            Check(game.HandlingSpeed < ordinarySpeed && game.Fragility < ordinaryFragility
                && !string.IsNullOrEmpty(game.VehicleNameFor(2)) && !string.IsNullOrEmpty(game.VehicleHintFor(2)),
                "Minivan has slower handling, protected cargo and a meaningful name and hint");
            wallet = game.Coins;
            game.SelectVehicle(2);
            Check(game.Coins == wallet && game.VehicleIndex == 2,
                "Equipping the owned minivan again is free");
            Check(game.CanLaunchDrone, "An owned idle drone is available for a second mission");
            game.LaunchDrone();
            Check(game.DroneBusy, "The drone can start a second distinct mission");
            game.GarageScreen.SelectTabForTest(0);
            yield return CapturePreview("v06-Minivan-Garaj.png", .3f);
            game.CloseOverlay();
            yield return ReturnForNextParcel();
            Check(game.HasPackage && game.ActiveParcel == ParcelKind.TeaSet && game.VehicleIndex == 2,
                "The next real depot pickup loads tea into the newly unlocked minivan");
            game.SelectVehicle(0);
            Check(game.VehicleIndex == 2 && !game.CanChangeVehicle,
                "Carried minivan cargo keeps the same vehicle lock as scooter cargo");
            wallet = game.Coins;
            game.AdvanceDroneForTest(40);
            Check(!game.DroneBusy && game.Coins == wallet + game.DroneReward && game.DroneCompletedFlights == flightsBefore + 2,
                "The second completed drone mission awards one additional reward");
            wallet = game.Coins;
            game.AdvanceDroneForTest(60);
            Check(game.Coins == wallet, "The second drone completion is also payment-idempotent");

            yield return VerifyLongVanRestoreAndTruckClearance();

            yield return DriveTo(new Vector3(-7.2f, 0, -8));
            yield return DriveTo(new Vector3(-7.2f, 0, 0));
            yield return DriveTo(new Vector3(8.45f, 0, 0));
            float cargo = game.Cargo;
            float until = Time.realtimeSinceStartup + .9f;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
            Check(game.Cargo < cargo && cargo - game.Cargo < standardTeaImpactDamage && !game.IsParcelDropped,
                "The minivan protects the same tea cargo better than the standard scooter during a real crate impact");
            float heldCargo = game.Cargo;
            int heldImpacts = game.CargoVisual.ImpactCount;
            until = Time.realtimeSinceStartup + 2.2f;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
            Check(game.Cargo == heldCargo && game.CargoVisual.ImpactCount == heldImpacts,
                "Holding the minivan bumper against one crate never repeats damage or impact effects");
            MinivanVisual activeVan = null;
            foreach (MinivanVisual candidate in game.PlayerTransform.GetComponentsInChildren<MinivanVisual>(true))
                if (candidate.gameObject.activeInHierarchy && candidate.transform.IsChildOf(game.PlayerTransform)) { activeVan = candidate; break; }
            GameObject eastCrate = GameObject.Find("Hazard_Crate_East");
            Collider eastCollider = eastCrate ? eastCrate.GetComponent<Collider>() : null;
            Check(activeVan && eastCollider, "Bumper clearance is measured on the driven minivan and real east-road crate");
            float visualFront = float.MinValue;
            foreach (Renderer renderer in activeVan.GetComponentsInChildren<Renderer>())
                if (renderer.enabled && renderer.gameObject.activeInHierarchy) visualFront = Mathf.Max(visualFront, renderer.bounds.max.z);
            Check(visualFront != float.MinValue && visualFront <= eastCollider.bounds.min.z + .10f,
                "The held minivan's visible front stays outside the crate rather than clipping through it");
            Vector3 heldPosition = game.PlayerTransform.position;
            yield return DriveTo(new Vector3(8.45f, 0, 0));
            Check(game.PlayerTransform.position.z < heldPosition.z - .25f,
                "The minivan can reverse away from its blocked bumper after sustained contact");
            yield return DriveTo(new Vector3(7.2f, 0, 0));
            yield return DriveTo(new Vector3(7.2f, 0, -7.2f), true);
            yield return WaitFor(() => game.Phase == GamePhase.Result, 2, "The minivan completes its protected tea delivery");
            Check(game.Completed == 3 && game.LastBaseFee == 45 && game.LastMultiplier == 2
                && game.LastPayout == (45 + game.LastSpeedBonus + game.LastCareBonus) * 2 + game.LastComboBonus && game.Coins == wallet + game.LastPayout,
                "Minivan receipt doubles its full 45-coin fee and both bonuses exactly once");
            yield return CapturePreview("v06-Minivan-Teslimat.png", .3f);

            game.OpenGarage();
            var names = new HashSet<string>();
            for (int i = 0; i < game.DecorCount; i++) names.Add(game.DecorName(i));
            Check(game.DecorCount == 14 && names.Count == 14 && !names.Contains(""),
                "Garage exposes fourteen distinct named decoration options");
            Check(game.DecorPrice(6) == 180 && game.DecorPrice(7) == 210 && game.DecorPrice(8) == 240,
                "The three new decorations expose their actual prices");
            int visible = game.GarageDecorationCount;
            wallet = game.Coins;
            game.SelectDecor(game.DecorCount);
            Check(game.Coins == wallet && game.GarageDecorationCount == visible,
                "The first index beyond fourteen decoration slots cannot mutate garage purchases");
            Check(!game.DecorOwned(6) && wallet >= game.DecorPrice(6),
                "Real deliveries and drone rewards can fund one newly added decoration");
            game.SelectDecor(6);
            Check(game.DecorOwned(6) && game.DecorEquipped(6) && game.Coins == wallet - game.DecorPrice(6)
                && game.GarageDecorationCount == visible + 1,
                "A new decoration is paid for and appears as an actual extra 3D garage object");
            wallet = game.Coins;
            game.SelectDecor(6);
            game.SelectDecor(6);
            Check(game.Coins == wallet && game.DecorEquipped(6) && game.GarageDecorationCount == visible + 1,
                "Hiding and showing the new decoration keeps ownership without charging again");
            game.GarageScreen.SelectTabForTest(2);
            yield return CapturePreview("v06-Yeni-Dekor.png", .3f);
            game.CloseOverlay();
            game.ShowMenu();
        }

        void VerifyV07EmptyBusiness()
        {
            game.OpenGarage();
            int wallet = game.Coins;
            var names = new HashSet<string>();
            for (int i = 0; i < game.BusinessCount; i++)
            {
                names.Add(game.BusinessName(i));
                Check(game.BusinessProgress(i) == 0 && game.BusinessTarget(i) > 0 && game.BusinessReward(i) > 0
                    && !game.CanClaimBusiness(i) && !string.IsNullOrEmpty(game.BusinessHint(i)),
                    "Fresh business contract " + i + " has an explicit target, reward and no invented progress");
                game.ClaimBusiness(i);
            }
            game.ClaimBusiness(-1); game.ClaimBusiness(game.BusinessCount);
            Check(game.BusinessCount == 4 && names.Count == 4 && game.BusinessOrders == 0 && game.Coins == wallet,
                "Early and invalid business claims cannot create money or completed orders");
            game.CloseOverlay();
        }

        int[] BusinessProgressSnapshot()
        {
            var progress = new int[game.BusinessCount + 1];
            for (int i = 0; i < game.BusinessCount; i++) progress[i] = game.BusinessProgress(i);
            progress[game.BusinessCount] = game.BusinessOrders;
            return progress;
        }

        static bool SameInts(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        IEnumerator VerifyV07ExpandedNeighborhood()
        {
            Check(WorldBuilder.PlayableHalfExtent == 20 && WorldBuilder.PlayBounds.size.x == 40
                && WorldBuilder.DestinationCount == 6 && game.DestinationCount == 6,
                "The playable neighborhood expands to forty meters and six real delivery addresses");
            int[] originalBusiness = BusinessProgressSnapshot();
            game.StartRun();
            game.EnableStreetEventsForTest(false);
            Check(game.SetRouteForTest(3, 4, 5) && !game.SetRouteForTest(3, 3, 5)
                && !game.SetRouteForTest(3, 4, 6),
                "The six-address route accepts three distinct destinations and rejects invalid routes atomically");
            for (int delivery = 0; delivery < 3; delivery++)
            {
                if (delivery > 0) { game.ContinueDelivery(); game.PlaceForTest(new Vector3(-8, 0, -8)); }
                yield return WaitFor(() => game.HasPackage, 3, "Outer-neighborhood package pickup " + (delivery + 1));
                Check(game.ActiveStopIndex == delivery + 3,
                    "The current delivery resolves its selected outer address " + (delivery + 3));
                yield return CompleteOuterDelivery(delivery + 3);
                if (delivery == 0)
                {
                    string receipt = game.ExportRunForTest();
                    RouteReceipt route = JsonUtility.FromJson<RouteReceipt>(receipt);
                    Check(route != null && route.version == 7 && SameInts(route.route, new[] { 3, 4, 5 })
                        && route.lastMultiplier == 2,
                        "Version-five result serializes the outer route and full minivan reward multiplier");
                    int wallet = game.Coins;
                    int[] business = BusinessProgressSnapshot();
                    game.ShowMenu();
                    Check(game.RestoreRunForTest(receipt) && game.RestoreRunForTest(receipt)
                        && game.Phase == GamePhase.Result && game.Completed == 1 && game.LastMultiplier == 2
                        && game.ActiveStopIndex == 4 && game.Coins == wallet
                        && SameInts(business, BusinessProgressSnapshot()),
                        "Restoring an outer-address doubled receipt preserves its route without paying or counting business twice");
                    game.OpenGarage(); game.GarageScreen.SelectTabForTest(3);
                    yield return CapturePreview("v07-Isletme-Sozlesmeleri.png", .3f);
                    game.CloseOverlay();
                }
            }
            Check(game.BusinessProgress(0) == originalBusiness[0] + 3
                && game.BusinessProgress(2) == originalBusiness[2] + 3,
                "Three physical outer-address deliveries increment delivery and outskirts contracts exactly three times");
            yield return CapturePreview("v07-Yeni-Mahalle-Teslimat.png", .3f);

            int beforeClaim = game.Coins;
            int[] earned = BusinessProgressSnapshot();
            game.ShowMenu(); game.ClaimBusiness(0);
            Check(game.Coins == beforeClaim && SameInts(earned, BusinessProgressSnapshot()),
                "An earned business reward is still protected by the garage claim gate");
            game.OpenGarage();
            for (int job = 0; job < game.BusinessCount; job++)
            {
                int claims = 0;
                while (game.CanClaimBusiness(job))
                {
                    int wallet = game.Coins, progress = game.BusinessProgress(job), orders = game.BusinessOrders;
                    game.ClaimBusiness(job);
                    Check(game.Coins == wallet + game.BusinessReward(job)
                        && game.BusinessProgress(job) == progress - game.BusinessTarget(job)
                        && game.BusinessOrders == orders + 1,
                        "Earned repeatable business contract " + job + " consumes one target and pays one order");
                    if (++claims > 100) throw new InvalidOperationException("Business claim did not consume its earned target.");
                }
                int remainingWallet = game.Coins;
                int[] remaining = BusinessProgressSnapshot();
                game.ClaimBusiness(job);
                Check(game.Coins == remainingWallet && SameInts(remaining, BusinessProgressSnapshot()),
                    "Exhausted business contract " + job + " cannot repeat an unpaid claim");
            }
            game.CloseOverlay();

            // Remaining funds, if needed, come from real doubled minivan deliveries.
            for (int attempt = 0; game.Coins < game.VehiclePrice(3) && attempt < 5; attempt++)
                yield return EarnOneOuterVanDelivery();
            Check(game.Coins >= game.VehiclePrice(3) && !game.VehicleOwned(3),
                "Physical deliveries and earned contracts fund the 700-coin monster truck without artificial funds");
            game.ShowMenu(); game.OpenGarage();
            int monsterWallet = game.Coins;
            game.SelectVehicle(3);
            Check(game.VehicleOwned(3) && game.MonsterTruckOwned && game.VehicleIndex == 3
                && game.HasVisibleMonsterTruck && !game.HasVisibleMinivan
                && game.Coins == monsterWallet - game.VehiclePrice(3),
                "The monster truck is purchased once and equips its actual separate 3D vehicle");
            monsterWallet = game.Coins;
            game.SelectVehicle(3);
            Check(game.Coins == monsterWallet && game.VehicleIndex == 3
                && game.VehicleTurnRate < 13 && game.VehicleSpeedScale > 1
                && game.VehicleImpactScale(2) < 1 && game.VehicleImpactScale(5.1f) > 2,
                "Owned monster selection is free and exposes wide turning plus distinct gentle and severe impact handling");
            game.GarageScreen.SelectTabForTest(0);
            yield return CapturePreview("v07-Koca-Teker-Garaj.png", .3f);
            game.CloseOverlay();
            game.StartRun(); game.EnableStreetEventsForTest(false); game.SetRouteForTest(5, 3, 4);
            yield return WaitFor(() => game.HasPackage, 3, "The real depot loads the purchased monster truck");
            game.SelectVehicle(0);
            Check(game.VehicleIndex == 3 && !game.CanChangeVehicle,
                "Loaded monster-truck cargo also locks vehicle changes");
            yield return VerifyMonsterSteering();
            Vector3 start = game.PlayerTransform.position;
            yield return DriveTo(new Vector3(-8, 0, -12));
            Check(Vector3.Distance(start, game.PlayerTransform.position) > 3 && !game.IsParcelDropped,
                "The purchased wide vehicle actually drives through the live controller with its cargo strapped");
            yield return DriveTo(new Vector3(-8, 0, -16));
            yield return DriveTo(new Vector3(0, 0, -16));
            int receiptWallet = game.Coins;
            yield return DriveTo(new Vector3(8, 0, -16), true);
            yield return WaitFor(() => game.Phase == GamePhase.Result, 2, "Monster truck reaches the real southern outer customer");
            Check(game.LastBaseFee == 40 && game.LastMultiplier == 1
                && game.LastPayout == 40 + game.LastSpeedBonus + game.LastCareBonus + game.LastComboBonus
                && game.Coins == receiptWallet + game.LastPayout,
                "The monster truck delivers for its normal fee without inheriting the minivan multiplier");
            yield return CapturePreview("v07-Koca-Teker-Teslimat.png", .3f);

            for (int attempt = 0; game.Coins < game.DecorPrice(13) && attempt < 5; attempt++)
                yield return EarnOneOuterVanDelivery();
            game.ShowMenu(); game.OpenGarage();
            int decorWallet = game.Coins, visible = game.GarageDecorationCount;
            Check(game.DecorCount == 14 && game.DecorPrice(13) == 440 && decorWallet >= game.DecorPrice(13)
                && !game.DecorOwned(13), "Earned funds reach the new highest decoration bit and its displayed price");
            game.SelectDecor(13);
            Check(game.DecorOwned(13) && game.DecorEquipped(13) && game.Coins == decorWallet - 440
                && game.GarageDecorationCount == visible + 1,
                "Decoration bit thirteen buys and displays its actual extra garage object");
            decorWallet = game.Coins;
            game.SelectDecor(13); game.SelectDecor(13);
            Check(game.DecorOwned(13) && game.DecorEquipped(13) && game.Coins == decorWallet,
                "High-bit decoration ownership survives hiding and re-equipping without another payment");
            game.GarageScreen.SelectTabForTest(2);
            yield return CapturePreview("v07-Genis-Garaj-Dekor.png", .3f);
            game.CloseOverlay(); game.ShowMenu();
        }

        IEnumerator VerifyMonsterSteering()
        {
            string original = game.ExportRunForTest();
            int wallet = game.Coins;
            int[] business = BusinessProgressSnapshot();
            float cargo = game.Cargo;
            Vector3 openRoad = new Vector3(0, 0, -12);
            game.PlaceForTest(openRoad); game.PlayerTransform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return MonsterInputFor(Vector3.back, .55f);
            Vector3 reversed = game.PlayerTransform.position;
            Check(reversed.z < openRoad.z - 1 && Mathf.Abs(reversed.x - openRoad.x) < .02f
                && Quaternion.Angle(game.PlayerTransform.rotation, Quaternion.identity) < .5f,
                "A real backward monster command reverses along its body without turning toward backward velocity");
            yield return MonsterInputFor(Vector3.zero, .35f);
            Vector3 stopped = game.PlayerTransform.position;
            Check(Quaternion.Angle(game.PlayerTransform.rotation, Quaternion.identity) < .5f
                && stopped.z <= reversed.z + .01f,
                "Releasing reverse brakes the actual truck without spinning its body");
            yield return MonsterInputFor(Vector3.zero, .1f);
            Check(Vector3.Distance(game.PlayerTransform.position, stopped) < .005f,
                "The released monster settles at rest without continuing to drift");

            game.PlaceForTest(openRoad); game.PlayerTransform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return MonsterInputFor(Vector3.back, .55f);
            bool sawStop = false, sawForward = false, firstStep = true, firstStepStillBrakes = false;
            float maxYaw = 0;
            float until = Time.realtimeSinceStartup + .85f;
            while (Time.realtimeSinceStartup < until)
            {
                Vector3 previous = game.PlayerTransform.position;
                game.MoveWorld(Vector3.forward);
                float dz = game.PlayerTransform.position.z - previous.z;
                if (firstStep) { firstStepStillBrakes = dz <= .002f; firstStep = false; }
                if (Mathf.Abs(dz) < .0002f) sawStop = true;
                if (dz > .002f && sawStop) sawForward = true;
                maxYaw = Mathf.Max(maxYaw, Quaternion.Angle(game.PlayerTransform.rotation, Quaternion.identity));
                yield return null;
            }
            Check(firstStepStillBrakes && sawStop && sawForward && maxYaw < .5f,
                "Reverse-to-forward input passes through a stopped frame then moves forward without a U-turn");

            // The north wall leaves the truck's side clear by only two centimeters.
            // A turn toward the open south side still needs rear-corner clearance
            // against that wall; sideways translation would incorrectly escape it.
            Vector3 narrow = new Vector3(-4, 0, 13.75f + WorldBuilder.AlleyWallOffset - 1.12f);
            Quaternion alongAlley = Quaternion.Euler(0, 90, 0);
            game.PlaceForTest(narrow); game.PlayerTransform.rotation = alongAlley;
            Physics.SyncTransforms();
            bool actualWall = false, initiallyOverlapping = false;
            foreach (Collider obstacle in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (obstacle.name == "Brick wall collision")
                {
                    float distance = Vector3.Distance(obstacle.ClosestPoint(narrow + Vector3.up * .96f), narrow + Vector3.up * .96f);
                    if (distance > .99f && distance < 1.04f) actualWall = true;
                }
            foreach (Collider obstacle in Physics.OverlapBox(narrow + Vector3.up * .96f,
                new Vector3(.99f, .94f, 1.29f), alongAlley, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (obstacle.name == "Brick wall collision") initiallyOverlapping = true;
            Check(actualWall && !initiallyOverlapping,
                "Blocked-turn fixture uses a real brick wall beside an initially clear full monster footprint");
            yield return MonsterInputFor(Vector3.back, .18f);
            Vector3 blocked = game.PlayerTransform.position;
            Check(Mathf.Abs(blocked.z - narrow.z) < .035f
                && Quaternion.Angle(game.PlayerTransform.rotation, alongAlley) < 3,
                "A rejected brick-alley turn cannot strafe the truck perpendicular to its accepted body heading");
            yield return MonsterInputFor(Vector3.left, .65f);
            Check(game.PlayerTransform.position.x < blocked.x - 1
                && Mathf.Abs(game.PlayerTransform.position.z - blocked.z) < .05f
                && Quaternion.Angle(game.PlayerTransform.rotation, alongAlley) < 3,
                "The wide truck can reverse straight out of a blocked tight turn");
            Check(game.Cargo == cargo && game.Coins == wallet && SameInts(business, BusinessProgressSnapshot()),
                "Steering and wall-clearance checks do not fabricate damage, payment or business progress");
            Check(game.RestoreRunForTest(original) && game.VehicleIndex == 3 && game.HasPackage
                && game.Cargo == cargo && game.Coins == wallet && SameInts(business, BusinessProgressSnapshot()),
                "Steering fixtures return to the genuine loaded monster delivery and its unchanged economy");
        }

        IEnumerator MonsterInputFor(Vector3 direction, float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(direction); yield return null; }
        }

        IEnumerator EarnOneOuterVanDelivery()
        {
            game.ShowMenu(); game.SelectVehicle(2); game.StartRun();
            game.EnableStreetEventsForTest(false); game.SetRouteForTest(3, 4, 5);
            yield return WaitFor(() => game.HasPackage, 3, "Earned-funds minivan picks up a real parcel");
            yield return CompleteOuterDelivery(3);
        }

        IEnumerator CompleteOuterDelivery(int stop)
        {
            int wallet = game.Coins, completed = game.Completed, fee = game.DeliveryBaseFee, multiplier = game.DeliveryMultiplier;
            if (stop == 3)
            {
                yield return DriveTo(new Vector3(-7.2f, 0, -8));
                yield return DriveTo(new Vector3(-7.2f, 0, 8));
                yield return DriveTo(new Vector3(-7.2f, 0, 16), true);
            }
            else if (stop == 4)
            {
                yield return DriveTo(new Vector3(-7.2f, 0, -8));
                yield return DriveTo(new Vector3(-7.2f, 0, 0));
                yield return DriveTo(new Vector3(8, 0, 0));
                yield return DriveTo(new Vector3(16, 0, 0));
                yield return DriveTo(new Vector3(16, 0, 8), true);
            }
            else
            {
                yield return DriveTo(new Vector3(-8, 0, -16));
                yield return DriveTo(new Vector3(0, 0, -16));
                yield return DriveTo(new Vector3(8, 0, -16), true);
            }
            yield return WaitFor(() => game.Phase == GamePhase.Result, 2, "Physical navigation reaches outer address " + stop);
            Check(game.Completed == completed + 1 && game.LastBaseFee == fee && game.LastMultiplier == multiplier
                && game.LastPayout == (fee + game.LastSpeedBonus + game.LastCareBonus) * multiplier + game.LastComboBonus
                && game.Coins == wallet + game.LastPayout,
                "Outer address " + stop + " pays one correctly multiplied real delivery");
        }

        IEnumerator VerifyV07Dogs()
        {
            game.ShowMenu(); game.SelectVehicle(0); game.StartRun();
            game.EnableStreetEventsForTest(true); game.StreetScene.ConfigureFixed(false, false);
            DogStreets dogs = game.DogScene;
            Check(dogs != null, "The expanded shortcut dog scene initializes");
            int modes = 0;
            bool deterministic = true, counts = true;
            for (int seed = 0; seed < 64; seed++)
            {
                dogs.Configure(seed); int mask = dogs.PresenceMask;
                modes |= 1 << mask;
                counts &= dogs.ActiveCount == (mask & 1) + ((mask >> 1) & 1);
                dogs.Configure(seed); deterministic &= dogs.PresenceMask == mask;
            }
            Check(modes == 15 && deterministic && counts,
                "Seeded dog encounters deterministically cover neither, either and both shortcuts");
            dogs.ConfigureFixed(0); dogs.Tick(game.PlayerTransform.position, 1, false);
            Check(dogs.ActiveCount == 0 && !dogs.AnyChasing && !dogs.AnyWarning && dogs.SlowMultiplier == 1,
                "A quiet dog fixture has no hidden chase or handling penalty");
            dogs.ConfigureFixed(1); dogs.Tick(game.PlayerTransform.position, .01f, false);
            yield return WaitFor(() => game.HasPackage, 3, "Dog encounter uses real loaded cargo");
            float cargo = game.Cargo, normalSpeed = game.HandlingSpeed;
            int wallet = game.Coins;
            game.PlaceForTest(new Vector3(-4, 0, 14.25f));
            Vector3 rider = game.PlayerTransform.position;
            dogs.Tick(rider, .01f, false);
            Check(dogs.AnyWarning && !dogs.AnyChasing && dogs.ChaseCount == 0 && dogs.SlowMultiplier == 1,
                "Entering the signed shortcut telegraphs a dog before any chase or slowdown");
            game.PauseGame();
            float clock = dogs.Clock; Vector3 dogPosition = dogs.DogPositions[0];
            yield return new WaitForSecondsRealtime(.25f);
            Check(Mathf.Abs(dogs.Clock - clock) < .001f && Vector3.Distance(dogs.DogPositions[0], dogPosition) < .001f
                && dogs.AnyWarning && Vector3.Distance(game.PlayerTransform.position, rider) < .001f,
                "Pausing freezes the dog warning clock and both dog and courier positions");
            game.OpenSettings(); yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Abs(dogs.Clock - clock) < .001f && dogs.AnyWarning,
                "Nested settings cannot consume a dog's warning or chase time");
            game.CloseOverlay(); game.ResumeGame();
            dogs.Tick(rider, 1.12f, false);
            Check(dogs.AnyChasing && dogs.ChaseCount == 1,
                "The telegraph expires into one bounded cartoon chase during active play");
            dogs.Tick(rider, .3f, false);
            Check(dogs.NudgeCount == 1 && dogs.SlowMultiplier < 1 && game.HandlingSpeed < normalSpeed
                && game.Cargo == cargo && game.Coins == wallet && Vector3.Distance(game.PlayerTransform.position, rider) < .001f,
                "A close dog adds a temporary handling slowdown without pushing, damaging or charging the courier");
            yield return CapturePreview("v07-Kopek-Kovalamaca.png", .2f);
            yield return WaitFor(() => game.PanicTimeLeft <= 0, 2, "The short panic blend expires before testing straight-line escape");
            yield return DriveTo(new Vector3(2, 0, 14.25f));
            yield return new WaitForSecondsRealtime(2.3f);
            Check(!dogs.AnyChasing && dogs.ChaseCount == 1 && dogs.NudgeCount == 1 && dogs.SlowMultiplier == 1
                && Mathf.Abs(game.HandlingSpeed - normalSpeed) < .01f && game.Cargo == cargo && game.Coins == wallet,
                "Driving out of the shortcut ends the chase and expires its single slowdown without cargo loss");
            game.PauseGame();
            rider = game.PlayerTransform.position;
            dogs.Tick(rider, 8, true);
            Check(Vector3.Distance(game.PlayerTransform.position, rider) < .001f && game.Cargo == cargo,
                "Even a reduced-motion return simulation cannot move the real courier");
            dogs.ConfigureFixed(0); game.EnableStreetEventsForTest(false); game.ShowMenu();
        }

        void VerifyRestoredTruckFallbackClearance()
        {
            int wallet = game.Coins;
            game.EnableStreetEventsForTest(true);
            StreetEvents street = game.StreetScene;
            street.ConfigureFixed(true, true, 1);
            StreetState fixture = street.CaptureState();
            fixture.clock = 60;
            fixture.warningLeft = 0;
            fixture.collectionLeft = 0;
            fixture.routeIndex = 3;
            fixture.headingForMidpoint = true;
            fixture.spawned = true;
            fixture.truckX = -7.35f;
            fixture.truckZ = -7.35f;
            fixture.yaw = 90;
            game.PlaceForTest(new Vector3(-12, 0, 12));
            Check(StreetEvents.ValidState(fixture) && street.RestoreState(fixture, game.PlayerTransform.position)
                && street.TruckCollider.enabled,
                "Fallback-clearance regression restores a valid truck beside the depot");
            game.PlaceForTest(new Vector3(-8, 0, -8));
            street.KeepClearOf(game.PlayerTransform.position);
            Check(!street.TruckCollider.enabled && !street.TruckCollider.gameObject.activeSelf
                && street.HasTruck && street.HasWorksite && street.WorksiteIndex == 1 && street.Seed == -1
                && Mathf.Abs(street.Clock - 60) < .01f && game.Coins == wallet,
                "A final courier fallback position clears an overlapping restored truck without rerolling or spending");
            Check(StreetEvents.ValidState(street.CaptureState()),
                "Truck clearance leaves a valid resumable street state");
            game.EnableStreetEventsForTest(false);
        }

        IEnumerator VerifyLongVanRestoreAndTruckClearance()
        {
            string original = game.ExportRunForTest();
            int wallet = game.Coins;
            float cargo = game.Cargo;
            bool droneBusy = game.DroneBusy;
            float droneProgress = game.DroneProgress;
            int flights = game.DroneCompletedFlights;
            string unsafePose = new Regex("\"x\"\\s*:\\s*-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?")
                .Replace(original, "\"x\":-6.45", 1);
            unsafePose = new Regex("\"z\"\\s*:\\s*-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?")
                .Replace(unsafePose, "\"z\":3.6", 1);
            // Anchor to the root z property so the nested street state's yaw is untouched.
            unsafePose = new Regex("(\"z\"\\s*:\\s*3\\.6\\s*,\\s*)\"yaw\"\\s*:\\s*-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?")
                .Replace(unsafePose, "$1\"yaw\":90", 1);
            Check(unsafePose != original && game.RestoreRunForTest(unsafePose),
                "A structurally valid loaded-minivan run accepts pose validation during restore");
            Check(Vector3.Distance(game.PlayerTransform.position, new Vector3(-8, 0, -8)) < .03f
                && game.VehicleIndex == 2 && game.HasPackage && game.Cargo == cargo && game.Coins == wallet,
                "Restoring a capsule-clear but long-body-overlapping van falls back to the safe depot");
            Check(game.RestoreRunForTest(original) && game.HasPackage && game.ActiveParcel == ParcelKind.TeaSet,
                "The original loaded-minivan run remains restorable after the unsafe-pose fixture");

            game.PauseGame();
            game.EnableStreetEventsForTest(true);
            StreetEvents street = game.StreetScene;
            street.ConfigureFixed(true, false);
            game.PlaceForTest(new Vector3(-12, 0, 12));
            yield return null;
            Check(street.ExtraClearance >= .6f,
                "The selected long minivan contributes its extra truck-yield clearance");
            street.Tick(8.1f, true, game.PlayerTransform.position, false);
            Vector3 truck = street.TruckPosition;
            game.PlaceForTest(truck + new Vector3(1, 0, 2.15f));
            game.PlayerTransform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            Vector3 rider = game.PlayerTransform.position;
            street.Tick(1, true, rider, false);
            Check(street.TruckWaiting && street.IsBlockedByPlayer && Vector3.Distance(truck, street.TruckPosition) < .03f,
                "A truck yields before entering the long van's laterally offset rear footprint");
            MinivanVisual drivenVan = null;
            foreach (MinivanVisual candidate in game.PlayerTransform.GetComponentsInChildren<MinivanVisual>(true))
                if (candidate.gameObject.activeInHierarchy && candidate.transform.IsChildOf(game.PlayerTransform)) { drivenVan = candidate; break; }
            bool hasBounds = false;
            Bounds vanBounds = new Bounds();
            if (drivenVan)
                foreach (Renderer renderer in drivenVan.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                    {
                        if (!hasBounds) { vanBounds = renderer.bounds; hasBounds = true; }
                        else vanBounds.Encapsulate(renderer.bounds);
                    }
            Check(hasBounds && !vanBounds.Intersects(street.TruckCollider.bounds)
                && Vector3.Distance(game.PlayerTransform.position, rider) < .015f,
                "Offset truck yielding keeps actual minivan render bounds separate and never pushes the courier");
            game.EnableStreetEventsForTest(false);
            Check(game.RestoreRunForTest(original) && game.HasPackage && game.VehicleIndex == 2 && game.Cargo == cargo
                && game.Coins == wallet && game.DroneBusy == droneBusy
                && Mathf.Abs(game.DroneProgress - droneProgress) < .001f && game.DroneCompletedFlights == flights,
                "Long-van safety fixtures restore the real delivery without changing wallet or companion mission");
        }

        void VerifyCompactLayoutGeometry()
        {
            Vector2[] sizes = { new Vector2(1280, 800), new Vector2(844, 390), new Vector2(667, 375), new Vector2(568, 320) };
            Rect[] safeRects = { new Rect(0, 0, 1280, 800), new Rect(47, 21, 750, 369), new Rect(0, 0, 667, 375), new Rect(0, 0, 568, 320) };
            for (int i = 0; i < sizes.Length; i++)
            {
                bool compact = i > 0;
                GuiViewport.Layout view = GuiViewport.Compute(sizes[i], safeRects[i], compact);
                Vector2 topLeft = view.DesignToScreen(Vector2.zero);
                Vector2 bottomRight = view.DesignToScreen(new Vector2(view.LogicalWidth, view.LogicalHeight));
                Rect safe = view.SafeScreenRect;
                Check(view.Scale > 0 && !float.IsNaN(view.Scale) && !float.IsInfinity(view.Scale)
                    && topLeft.x >= safe.xMin - .02f && topLeft.y <= safe.yMax + .02f
                    && bottomRight.x <= safe.xMax + .02f && bottomRight.y >= safe.yMin - .02f,
                    "Viewport content fits the safe area at " + sizes[i]);
                Vector2 design = new Vector2(view.LogicalWidth * .73f, view.LogicalHeight * .41f);
                Check(Vector2.Distance(view.ScreenToDesign(view.DesignToScreen(design)), design) < .02f,
                    "Draw and pointer transforms round-trip at " + sizes[i]);
                if (!compact) continue;
                CompactScreens.PhoneLayout layout = CompactScreens.GetLayout(view);
                Rect canvas = new Rect(0, 0, view.LogicalWidth, view.LogicalHeight);
                Rect[] panels = { layout.GameHeader, layout.GamePause, layout.MiniMap, layout.OverlayHeader,
                    layout.OverlayBody, layout.OverlayFooter, layout.GarageBack, layout.GarageTabs, layout.GarageContent };
                bool contained = true;
                foreach (Rect panel in panels) contained &= panel.width > 0 && panel.height > 0
                    && panel.xMin >= canvas.xMin && panel.yMin >= canvas.yMin
                    && panel.xMax <= canvas.xMax + .02f && panel.yMax <= canvas.yMax + .02f;
                Check(contained && !layout.OverlayHeader.Overlaps(layout.OverlayBody)
                    && !layout.OverlayBody.Overlaps(layout.OverlayFooter) && !layout.OverlayHeader.Overlaps(layout.OverlayFooter)
                    && !layout.GarageBack.Overlaps(layout.GarageTabs) && !layout.GarageTabs.Overlaps(layout.GarageContent),
                    "Compact settings and garage regions fit without covering each other's actions at " + sizes[i]);
                Check(layout.GamePause.width >= 44 && layout.GamePause.height >= 44
                    && layout.GarageBack.height >= 44 && layout.GarageTabs.height >= 44 && layout.OverlayFooter.height >= 44,
                    "Primary compact navigation has generous logical hit areas at " + sizes[i]);
                float contentWidth = layout.GarageContent.width - 24;
                Rect[] cards = CompactScreens.CardGrid(contentWidth, game.DecorCount, 3, 78, 9);
                bool validCards = cards.Length == 14;
                for (int card = 0; card < cards.Length; card++)
                {
                    validCards &= cards[card].xMin >= 0 && cards[card].xMax <= contentWidth + .02f
                        && cards[card].width > 0 && cards[card].height >= 52;
                    for (int other = card + 1; other < cards.Length; other++) validCards &= !cards[card].Overlaps(cards[other]);
                }
                Check(validCards, "All fourteen compact decoration cards stay separate within their scroll content at " + sizes[i]);
            }
            GuiViewport.Layout fallback = GuiViewport.Compute(new Vector2(float.NaN, -1), new Rect(0, 0, 0, 0), false);
            Check(fallback.Scale > 0 && !float.IsNaN(fallback.Scale) && !float.IsInfinity(fallback.Scale),
                "Invalid display dimensions fall back to a finite usable viewport transform");
        }

        IEnumerator ReturnForNextParcel()
        {
            int completed = game.Completed;
            Check(game.Phase == GamePhase.Result && completed > 0 && completed < 3,
                "A completed delivery can return for its next real package");
            game.ContinueDelivery();
            if (completed == 2) yield return DriveTo(new Vector3(-7.2f, 0, 8));
            yield return DriveTo(new Vector3(-7.2f, 0, -7.2f));
            yield return DriveTo(new Vector3(-8, 0, -8));
            yield return WaitFor(() => game.HasPackage, 3, "Physical depot return collects the next progression package");
        }

        IEnumerator CompleteCleanCurrentDelivery()
        {
            Check(game.Phase == GamePhase.Playing && game.HasPackage,
                "Progression delivery begins with real carried cargo");
            int wallet = game.Coins;
            int completed = game.Completed;
            int fee = game.DeliveryBaseFee;
            int multiplier = game.DeliveryMultiplier;
            if (completed == 0)
            {
                yield return DriveTo(new Vector3(-7.2f, 0, -8));
                yield return DriveTo(new Vector3(-7.2f, 0, 7), true);
            }
            else if (completed == 1)
            {
                yield return DriveTo(new Vector3(-7.2f, 0, -8));
                yield return DriveTo(new Vector3(-7.2f, 0, 8));
                yield return DriveTo(new Vector3(8, 0, 8), true);
            }
            else
            {
                yield return DriveTo(new Vector3(-8, 0, -7.2f));
                yield return DriveTo(new Vector3(8, 0, -7.2f), true);
            }
            yield return WaitFor(() => game.Phase == GamePhase.Result, 2, "Clean progression delivery reaches its actual customer");
            Check(game.Completed == completed + 1 && game.LastBaseFee == fee
                && game.LastMultiplier == multiplier && game.LastPayout == (fee + game.LastSpeedBonus + game.LastCareBonus) * multiplier + game.LastComboBonus && game.Coins == wallet + game.LastPayout,
                "Clean progression delivery credits one correctly calculated customer payment");
        }

        IEnumerator VerifyLegacyResultMigration()
        {
            int wallet = game.Coins;
            Check(game.RestoreRunForTest(firstResultSnapshot), "Current result fixture restores before compatibility checks");
            StreetState currentStreet = CloneStreet(game.StreetScene.CaptureState());
            string versionThree = firstResultSnapshot.Replace("\"version\":7", "\"version\":3")
                .Replace("\"version\":5", "\"version\":3")
                .Replace("\"version\":4", "\"version\":3");
            Check(versionThree != firstResultSnapshot && game.RestoreRunForTest(versionThree),
                "Version-three result imports through the current snapshot compatibility path");
            Check(game.Phase == GamePhase.Result && game.Completed == 1 && game.LastBaseFee == 40
                && SameStreet(currentStreet, game.StreetScene.CaptureState()) && game.Coins == wallet,
                "Version-three import preserves its street mode, seed, clock and already credited wallet");
            string legacy = firstResultSnapshot.Replace("\"version\":7", "\"version\":1")
                .Replace("\"version\":5", "\"version\":1")
                .Replace("\"version\":4", "\"version\":1")
                .Replace("\"version\":3", "\"version\":1")
                .Replace("\"version\":2", "\"version\":1");
            Check(legacy != firstResultSnapshot && game.RestoreRunForTest(legacy),
                "Version-one standard result imports through the legacy snapshot migration path");
            Check(game.Phase == GamePhase.Result && game.Completed == 1 && !game.HasPackage
                && !game.IsParcelDropped && game.LastBaseFee == 40 && game.Coins == wallet,
                "Legacy result migration keeps its standard fee, completed delivery and already credited wallet");
            game.ShowMenu();
            Check(game.Phase == GamePhase.Menu && game.VehicleIndex == 0 && game.CanChangeVehicle,
                "Legacy-result check returns to the menu between packages with vehicle choice available");
            yield return null;
        }

        IEnumerator VerifyV04Features()
        {
            // All previous deliveries and release-flow checks use the standard
            // scooter and disabled street events. New features are opt-in here.
            Check(game.Phase == GamePhase.Menu && game.VehicleIndex == 0,
                "Original regression suite finishes with the standard scooter selected");
            float standardSpeed = game.HandlingSpeed;
            string standardName = game.VehicleName;
            int wallet = game.Coins;
            game.SelectVehicle(-1);
            game.SelectVehicle(99);
            Check(game.VehicleIndex == 0 && game.Coins == wallet,
                "Invalid vehicle choices cannot change the scooter or wallet");
            game.SelectVehicle(1);
            Check(game.VehicleIndex == 1 && game.HandlingSpeed > standardSpeed * 1.2f
                && !string.IsNullOrEmpty(game.VehicleName) && game.VehicleName != standardName,
                "Fast scooter has a distinct name and meaningfully higher handling speed");
            yield return CapturePreview("v04-Hizli-Scooter-Menu.png");
            game.StartRun();
            yield return WaitFor(() => game.HasPackage, 3, "Fast-scooter run picks up its first cake");
            game.SelectVehicle(0);
            Check(game.VehicleIndex == 1,
                "Vehicle cannot be swapped directly during an active delivery");
            Check(!game.IsParcelDropped && game.Cargo == 100,
                "Fresh fast-scooter delivery starts with carried, intact cargo");

            // Give the real controller enough run-up to reach the severe-impact
            // threshold before the west-road crate. No damage or drop setters.
            game.PlaceForTest(new Vector3(-8.45f, 0, -6));
            float until = Time.realtimeSinceStartup + 2;
            while (!game.IsParcelDropped && Time.realtimeSinceStartup < until)
            {
                game.MoveWorld(Vector3.forward);
                if (game.IsParcelDropped) break;
                yield return null;
            }
            Check(game.IsParcelDropped && !game.HasPackage,
                "A real high-speed crate impact drops the fast scooter's parcel");
            Check(game.Cargo < 100 - standardCakeImpactDamage && game.Cargo >= 0,
                "Fast-scooter impact causes more damage than the standard cake collision");
            Vector3 dropped = game.DroppedParcelPosition;
            float damaged = game.Cargo;
            float remaining = game.TimeLeft;
            ParcelKind kind = game.ActiveParcel;
            Vector3 impactPosition = game.PlayerTransform.position;
            Check(FlatDistance(impactPosition, dropped) >= 1.7f,
                "A severe crash drops the parcel beyond the automatic recovery radius");
            Check(Vector3.Distance(game.TargetPosition, dropped) < .03f,
                "The destination marker changes to the dropped parcel");
            yield return CapturePreview("v04-Dusen-Paket-Yakindan.png", .3f);
            yield return new WaitForSecondsRealtime(1.2f);
            Check(game.IsParcelDropped && !game.HasPackage && FlatDistance(game.PlayerTransform.position, impactPosition) < .02f,
                "A motionless courier does not automatically recover the parcel after its pickup cooldown");
            remaining = game.TimeLeft;
            game.PlaceForTest(new Vector3(-8, 0, 7));
            int completedBefore = game.Completed;
            yield return new WaitForSecondsRealtime(1.35f);
            Check(game.IsParcelDropped && !game.HasPackage && game.Completed == completedBefore && game.Coins == wallet,
                "Reaching the customer without retrieving the dropped parcel cannot deliver or pay");
            Check(game.Cargo == damaged && game.TimeLeft < remaining,
                "Dropped cargo keeps its damage while the delivery clock continues");
            yield return CapturePreview("v04-Dusen-Paket.png");

            string droppedSnapshot = game.ExportRunForTest();
            Check(!string.IsNullOrEmpty(droppedSnapshot), "A run with a dropped parcel serializes successfully");
            remaining = game.TimeLeft;
            game.ShowMenu();
            game.SelectVehicle(0);
            Check(game.Phase == GamePhase.Menu && !game.CanChangeVehicle && game.VehicleIndex == 1 && game.Coins == wallet,
                "Menu cannot swap vehicles while the current delivery has a dropped parcel");
            game.OpenGarage();
            game.SelectVehicle(0);
            Check(game.Phase == GamePhase.Garage && !game.CanChangeVehicle && game.VehicleIndex == 1,
                "Garage cannot bypass the vehicle lock for a dropped parcel");
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Menu && game.VehicleIndex == 1 && game.IsParcelDropped,
                "Returning from garage preserves the locked vehicle and dropped parcel");
            Check(game.RestoreRunForTest(droppedSnapshot), "Dropped-parcel snapshot restores through the real run serializer");
            Check(game.IsParcelDropped && !game.HasPackage && game.ActiveParcel == kind
                && Vector3.Distance(game.DroppedParcelPosition, dropped) < .03f
                && Vector3.Distance(game.TargetPosition, dropped) < .03f,
                "Restored run recreates the same dropped parcel and recovery target");
            Check(game.Cargo == damaged && Mathf.Abs(game.TimeLeft - remaining) < .05f && game.Coins == wallet,
                "Restoring a dropped parcel preserves damage, time and wallet");
            game.PlaceForTest(dropped + new Vector3(1.1f, 0, -2.1f));
            yield return DriveTo(dropped);
            yield return WaitFor(() => game.HasPackage && !game.IsParcelDropped, 3, "Actual approach retrieves the dropped parcel");
            Check(game.Cargo == damaged && game.TimeLeft <= remaining && game.TimeLeft < 54
                && game.ActiveParcel == kind && game.CargoVisual.Kind == kind && game.Coins == wallet,
                "Recovery keeps the damaged parcel and elapsed time instead of granting a fresh package");
            Check(Vector3.Distance(game.TargetPosition, new Vector3(-8, 0, 7)) < .03f,
                "Recovered parcel restores its original customer destination");
            yield return CapturePreview("v04-Paket-Geri-Alindi.png");
            yield return DriveTo(new Vector3(-7.2f, 0, dropped.z));
            yield return DriveTo(new Vector3(-7.2f, 0, 7), true);
            yield return WaitFor(() => game.Phase == GamePhase.Result, 2, "Recovered fast-scooter cargo reaches its customer");
            Check(game.Completed == 1 && !game.IsParcelDropped && !game.HasPackage && game.LastBaseFee == 35,
                "Fast-scooter delivery uses its stated lower base fee and consumes the recovered parcel");
            Check(game.LastPayout == 35 + game.LastSpeedBonus + game.LastCareBonus + game.LastComboBonus
                && game.Coins == wallet + game.LastPayout,
                "Recovered fast-scooter delivery credits its correct payout exactly once");
            yield return CapturePreview("v04-Hizli-Teslimat-Sonuc.png");
            wallet = game.Coins;
            yield return new WaitForSecondsRealtime(.2f);
            Check(game.Coins == wallet && game.Completed == 1,
                "A recovered parcel's result cannot duplicate its payment");

            game.ShowMenu();
            game.SelectVehicle(0);
            game.StartRun();
            yield return WaitFor(() => game.HasPackage, 3, "Street-event run loads a standard-scooter package");
            game.EnableStreetEventsForTest(true);
            StreetEvents street = game.StreetScene;
            Check(street != null && street.TruckCollider && street.WorksiteCollider,
                "Street events provide real truck and worksite colliders");
            street.Reset();
            yield return new WaitForSecondsRealtime(.35f);
            Check(street.Clock > 0 && street.WarningActive,
                "Playing starts the street-event clock and initial road warning");
            float clock = street.Clock;
            Vector3 truck = street.TruckPosition;
            game.OpenSettings();
            yield return new WaitForSecondsRealtime(.35f);
            Check(Mathf.Abs(street.Clock - clock) < .025f && Vector3.Distance(street.TruckPosition, truck) < .02f,
                "Settings freezes the truck and street-event clock");
            game.CloseOverlay();
            yield return new WaitForSecondsRealtime(.35f);
            Check(street.Clock > clock + .15f, "Returning to play resumes the street-event clock");

            // The worksite is a real obstacle in the road, not just a visual sign.
            game.PlaceForTest(new Vector3(-6.8f, 0, 0));
            Vector3 beforeWorksite = game.PlayerTransform.position;
            float cargoBeforeWorksite = game.Cargo;
            until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.right); yield return null; }
            game.MoveWorld(Vector3.zero);
            Check(street.WorksiteCollider.enabled && game.PlayerTransform.position.x > beforeWorksite.x + .25f
                && game.PlayerTransform.position.x < street.WorksiteCollider.bounds.min.x - .15f,
                "Controller approaches but cannot drive through the active worksite barrier");
            Check(game.Cargo < cargoBeforeWorksite && !game.IsParcelDropped,
                "Standard-scooter worksite collision damages cargo without creating a high-speed drop");
            yield return CapturePreview("v04-Yol-Calismasi.png");

            // Use deterministic ticks of the same visible event instance while
            // the game is paused, so the ordinary game loop cannot double-tick it.
            game.PauseGame();
            street.Reset();
            game.PlaceForTest(new Vector3(-12, 0, 12));
            street.Tick(8.9f, true, game.PlayerTransform.position, false);
            Vector3 movedTruck = street.TruckPosition;
            street.Tick(.4f, true, game.PlayerTransform.position, false);
            Check(Vector3.Distance(street.TruckPosition, movedTruck) > .15f,
                "Truck progresses along its route when the road is clear");
            truck = street.TruckPosition;
            clock = street.Clock;
            street.Tick(.5f, false, game.PlayerTransform.position, false);
            Check(Vector3.Distance(street.TruckPosition, truck) < .015f && Mathf.Abs(street.Clock - clock) < .015f,
                "A non-playing street tick cannot advance the truck or event time");
            game.PlaceForTest(truck + Vector3.forward * 1.9f);
            Vector3 waitingPlayer = game.PlayerTransform.position;
            street.Tick(.5f, true, waitingPlayer, false);
            Check(street.IsBlockedByPlayer && street.TruckWaiting
                && Vector3.Distance(street.TruckPosition, truck) < .03f,
                "Truck yields to a courier standing ahead of it");
            Check(Vector3.Distance(game.PlayerTransform.position, waitingPlayer) < .015f
                && !street.TruckCollider.bounds.Intersects(game.PlayerTransform.GetComponent<CharacterController>().bounds),
                "Yielding truck neither overlaps nor pushes the courier");
            game.PlaceForTest(new Vector3(-12, 0, 12));
            street.Tick(.5f, true, game.PlayerTransform.position, false);
            Check(!street.IsBlockedByPlayer && Vector3.Distance(street.TruckPosition, truck) > .15f,
                "Truck continues once the courier clears its path");
            game.ResumeGame();
            game.PlaceForTest(new Vector3(-7.2f, 0, 0));
            yield return CapturePreview("v04-Mahalle-Trafik.png");
            game.EnableStreetEventsForTest(false);
            game.ShowMenu();
            game.SelectVehicle(0);
        }

        IEnumerator VerifyV05StreetTours()
        {
            game.StartRun();
            yield return WaitFor(() => game.HasPackage, 3, "Random-street tour starts with a real loaded delivery");
            game.EnableStreetEventsForTest(true);
            game.PauseGame();
            game.PlaceForTest(new Vector3(-12, 0, 12));
            StreetEvents street = game.StreetScene;
            var modes = new HashSet<int>();
            var sites = new HashSet<int>();
            bool deterministic = true, valid = true, disabledWhenAbsent = true, publicFlagsMatch = true, placementMatches = true;
            bool timingValid = true;
            for (int seed = 0; seed < 128; seed++)
            {
                street.Configure(seed);
                StreetState first = street.CaptureState();
                string firstJson = JsonUtility.ToJson(first);
                street.Configure(seed);
                StreetState again = street.CaptureState();
                deterministic &= firstJson == JsonUtility.ToJson(again);
                valid &= StreetEvents.ValidState(first) && StreetEvents.ValidState(again) && first.seed == seed;
                timingValid &= first.clock == 0 && first.arrivalDelay >= 8 && first.arrivalDelay <= 18;
                modes.Add(first.mask);
                if (street.HasWorksite) sites.Add(street.WorksiteIndex);
                publicFlagsMatch &= street.HasTruck == ((first.mask & 1) != 0)
                    && street.HasWorksite == ((first.mask & 2) != 0) && street.WorksiteIndex == first.worksiteIndex;
                street.Tick(.01f, true, game.PlayerTransform.position, false);
                if (!street.HasTruck) disabledWhenAbsent &= !street.TruckCollider.enabled && !street.TruckCollider.gameObject.activeSelf;
                if (!street.HasWorksite) disabledWhenAbsent &= !street.WorksiteCollider.enabled && !street.WorksiteCollider.gameObject.activeSelf;
                else placementMatches &= FlatDistance(street.WorksiteCollider.bounds.center, street.WorksitePosition) < .03f;
            }
            Check(deterministic, "All 128 seeds reproduce the same complete initial street configuration");
            Check(valid && timingValid, "All 128 generated street states validate with bounded arrival delays and reset clocks");
            Check(modes.Count == 4 && modes.Contains(0) && modes.Contains(1) && modes.Contains(2) && modes.Contains(3),
                "Seed coverage includes quiet, truck-only, worksite-only and combined tours");
            Check(sites.Count == 3, "Seed coverage places roadworks at all three supported sites");
            Check(publicFlagsMatch, "Public street and map flags match every generated event mode");
            Check(disabledWhenAbsent, "Absent truck and worksite objects never retain active colliders");
            Check(placementMatches, "Active worksite collision bounds match the public map position");
            Check(StreetEvents.ValidState(StreetEvents.QuietState()), "The quiet migration fixture is a valid no-event street state");

            // Capture a nontrivial truck position, not just a freshly configured map.
            street.Configure(2);
            street.Tick(street.ArrivalDelay + 1.5f, true, game.PlayerTransform.position, false);
            StreetState original = CloneStreet(street.CaptureState());
            Check(original.clock > original.arrivalDelay && original.spawned && StreetEvents.ValidState(original),
                "A moving randomized street state remains valid after its arrival delay");
            int wallet = game.Coins;
            game.ResumeGame();
            game.OpenSettings();
            yield return new WaitForSecondsRealtime(.25f);
            Check(SameStreet(original, street.CaptureState()), "Settings does not reroll or advance the current random street tour");
            game.CloseOverlay();
            game.ShowMenu();
            yield return new WaitForSecondsRealtime(.25f);
            Check(SameStreet(original, street.CaptureState()), "Menu preserves the current street seed, site, mode and simulation progress");
            game.ResumeRun();
            Check(SameStreet(original, street.CaptureState()) && game.HasPackage && game.Coins == wallet,
                "Resume restores the same street configuration and clock without changing the wallet");
            game.PauseGame();
            string wholeRun = game.ExportRunForTest();
            street.Configure(1);
            Check(game.RestoreRunForTest(wholeRun), "Version-three run snapshot with street state can be restored");
            Check(SameStreet(original, street.CaptureState()) && game.Coins == wallet,
                "Whole-run serialization preserves street mode, site, seed, truck position and clock");
            game.PauseGame();

            StreetState beforeInvalid = CloneStreet(street.CaptureState());
            StreetState invalid = CloneStreet(beforeInvalid);
            invalid.clock = float.NaN;
            Check(!StreetEvents.ValidState(invalid) && !street.RestoreState(invalid, game.PlayerTransform.position),
                "Non-finite street clocks are rejected by validation and restoration");
            Check(SameStreet(beforeInvalid, street.CaptureState()) && game.Coins == wallet,
                "Rejected non-finite street state leaves the live tour and wallet unchanged");
            invalid = CloneStreet(beforeInvalid);
            invalid.worksiteIndex = 99;
            Check(!StreetEvents.ValidState(invalid) && !street.RestoreState(invalid, game.PlayerTransform.position),
                "Out-of-range worksite indices are rejected by validation and restoration");
            Check(SameStreet(beforeInvalid, street.CaptureState()) && game.Coins == wallet,
                "Rejected worksite state cannot partly replace the current tour or spend coins");

            // Controlled examples use the actual street instance. Resume briefly
            // only to render the unobscured game, then freeze before reconfiguring.
            int[] exampleSeeds = { 1, 3, 0, 2 };
            string[] exampleNames = { "v05-Sakin-Mahalle.png", "v05-Yalniz-Kamyon.png", "v05-Yalniz-Yol-Calismasi.png", "v05-Kamyon-Ve-Yol-Calismasi.png" };
            for (int i = 0; i < exampleSeeds.Length; i++)
            {
                street.Configure(exampleSeeds[i]);
                Vector3 approach = street.WorksiteIndex == 2 ? Vector3.back : street.WorksiteIndex == 1 ? Vector3.right : Vector3.left;
                Vector3 position = street.HasWorksite ? street.WorksitePosition + approach * 3.2f : new Vector3(-2, 0, -8.4f);
                if (i == 0) position = new Vector3(-7.2f, 0, 0);
                game.PlaceForTest(position);
                game.ResumeGame();
                yield return CapturePreview(exampleNames[i], .3f);
                game.PauseGame();
            }
            game.PlaceForTest(new Vector3(-12, 0, 12));
            Check(street.RestoreState(original, game.PlayerTransform.position) && SameStreet(original, street.CaptureState()),
                "Mode previews restore the original street configuration and progress afterward");

            var newTourSeeds = new HashSet<int>();
            bool clocksReset = true;
            for (int i = 0; i < 8; i++)
            {
                int previousSeed = street.Seed;
                game.StartRun();
                newTourSeeds.Add(street.Seed);
                clocksReset &= street.Seed != previousSeed && street.Clock == 0 && StreetEvents.ValidState(street.CaptureState());
            }
            Check(newTourSeeds.Count == 8 && clocksReset, "Eight new tours choose new seeds and reset their event clocks");
            Check(game.RestoreRunForTest(wholeRun) && SameStreet(original, street.CaptureState()) && game.Coins == wallet,
                "Restoring the earlier tour after new tours reinstates its saved events without spending or paying");
            game.EnableStreetEventsForTest(false);
            game.ShowMenu();
        }

        static StreetState CloneStreet(StreetState state) => JsonUtility.FromJson<StreetState>(JsonUtility.ToJson(state));

        static bool SameStreet(StreetState a, StreetState b)
        {
            return a != null && b != null && a.version == b.version && a.seed == b.seed && a.mask == b.mask
                && a.worksiteIndex == b.worksiteIndex && a.routeIndex == b.routeIndex
                && a.headingForMidpoint == b.headingForMidpoint && a.spawned == b.spawned
                && Mathf.Abs(a.arrivalDelay - b.arrivalDelay) < .025f && Mathf.Abs(a.clock - b.clock) < .025f
                && Mathf.Abs(a.warningLeft - b.warningLeft) < .025f && Mathf.Abs(a.collectionLeft - b.collectionLeft) < .025f
                && Mathf.Abs(a.truckX - b.truckX) < .025f && Mathf.Abs(a.truckZ - b.truckZ) < .025f
                && Mathf.Abs(Mathf.DeltaAngle(a.yaw, b.yaw)) < .05f;
        }

        IEnumerator VerifyReleaseFeatures()
        {
            // The original 62 checks above stay intact. Exercise new release flows
            // on a real loaded and damaged delivery, never through user preferences.
            yield return WaitFor(() => game.HasPackage, 3, "Release-flow run picks up its cake");
            yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
            float beforeHit = game.Cargo;
            float until = Time.realtimeSinceStartup + .65f;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); yield return null; }
            Check(game.Cargo < beforeHit, "Release-flow snapshot includes real damaged cargo");
            yield return DriveTo(new Vector3(-8.45f, 0, -3.25f));
            yield return DriveTo(new Vector3(-7.2f, 0, -3.25f));
            game.MoveWorld(Vector3.zero);

            Vector3 parked = game.PlayerTransform.position;
            float integrity = game.Cargo;
            float remaining = game.TimeLeft;
            game.OpenSettings();
            Check(game.Phase == GamePhase.Settings, "Settings opens over an unfinished delivery");
            yield return new WaitForSecondsRealtime(.35f);
            CheckFrozen(parked, integrity, remaining, "Settings freezes position, cargo and delivery timer");
            yield return CapturePreview("v03-Ayarlar-TR.png");

            float oldVolume = game.Volume;
            game.SettingVolume(-.5f);
            Check(Mathf.Approximately(game.Volume, 0), "Volume clamps negative values to silent");
            game.SettingVolume(1.5f);
            Check(Mathf.Approximately(game.Volume, 1), "Volume clamps values above one");
            game.SettingVolume(.37f);
            Check(Mathf.Abs(game.Volume - .37f) < .001f, "Settings accepts an intermediate volume");
            bool reduced = game.ReducedMotion;
            game.ToggleReducedMotion();
            Check(game.ReducedMotion != reduced, "Reduced-motion setting changes state");
            game.ToggleReducedMotion();
            Check(game.ReducedMotion == reduced, "Reduced-motion preference can be restored");
            game.SettingVolume(oldVolume);

            game.ToggleLanguage();
            Check(!game.IsTurkish && game.Phase == GamePhase.Settings, "Settings language switch preserves its overlay");
            yield return CapturePreview("v03-Settings-EN.png");
            game.ToggleLanguage();
            game.OpenPrivacy();
            Check(game.Phase == GamePhase.Privacy, "Privacy opens from settings");
            yield return CapturePreview("v03-Gizlilik-TR.png");
            CheckFrozen(parked, integrity, remaining, "Nested privacy panel keeps the delivery frozen");
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Settings, "Closing privacy returns to settings");
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Playing && game.HasPackage, "Closing settings returns to its loaded delivery");

            remaining = game.TimeLeft;
            game.PauseGame();
            Check(game.Phase == GamePhase.Paused, "Pause opens a dedicated pause screen");
            game.MoveWorld(Vector3.right);
            yield return new WaitForSecondsRealtime(.35f);
            CheckFrozen(parked, integrity, remaining, "Paused input cannot move the scooter or drain its timer");
            game.OpenSettings();
            game.OpenPrivacy();
            game.CloseOverlay();
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Paused, "Nested settings and privacy return to the pause screen");
            game.ResumeGame();
            Check(game.Phase == GamePhase.Playing && game.HasPackage && game.Cargo == integrity,
                "Resume keeps the loaded parcel and its integrity");

            game.OpenSettings();
            game.OpenTutorial();
            Check(game.Phase == GamePhase.Tutorial && game.TutorialPage == 0, "Tutorial starts at page one");
            game.PreviousTutorial();
            Check(game.TutorialPage == 0, "Tutorial cannot navigate before its first page");
            yield return CapturePreview("v03-Nasil-Oynanir-1.png");
            game.NextTutorial();
            Check(game.TutorialPage == 1, "Tutorial advances to its second page");
            game.PreviousTutorial();
            Check(game.TutorialPage == 0, "Tutorial previous navigation works");
            game.NextTutorial();
            game.NextTutorial();
            Check(game.TutorialPage == 2, "Tutorial reaches its third page");
            game.NextTutorial();
            Check(game.TutorialPage == 2, "Tutorial cannot advance beyond its final page");
            yield return CapturePreview("v03-Nasil-Oynanir-3.png");
            game.FinishTutorial();
            Check(game.Phase == GamePhase.Settings, "Finishing tutorial returns to its settings origin");
            game.CloseOverlay();

            // Menu/resume must use the saved run rather than silently starting over.
            parked = game.PlayerTransform.position;
            integrity = game.Cargo;
            remaining = game.TimeLeft;
            int wallet = game.Coins;
            game.ShowMenu();
            Check(game.Phase == GamePhase.Menu && game.HasSavedRun, "Menu advertises the unfinished saved run");
            yield return new WaitForSecondsRealtime(.2f);
            game.ResumeRun();
            Check(game.Phase == GamePhase.Playing && game.HasPackage && game.Completed == 0 && game.ActiveParcel == ParcelKind.Cake,
                "Continue saved run restores the unfinished cake delivery");
            Check(Vector3.Distance(game.PlayerTransform.position, parked) < .03f && game.Cargo == integrity
                && Mathf.Abs(game.TimeLeft - remaining) < .05f && game.Coins == wallet,
                "Saved-run resume preserves location, damage, time and wallet");

            string live = game.ExportRunForTest();
            Check(!string.IsNullOrEmpty(live), "Unfinished delivery serializes to a nonempty snapshot");
            parked = game.PlayerTransform.position;
            integrity = game.Cargo;
            remaining = game.TimeLeft;
            game.PlaceForTest(new Vector3(8, 0, -8));
            game.ShowMenu();
            Check(game.RestoreRunForTest(live), "Serialized unfinished run can be restored");
            Check(game.Phase == GamePhase.Playing && game.HasPackage && game.Completed == 0
                && game.ActiveParcel == ParcelKind.Cake && game.CargoVisual.Kind == ParcelKind.Cake,
                "Restored run has its actual loaded parcel and delivery phase");
            Check(Vector3.Distance(game.PlayerTransform.position, parked) < .03f && game.Cargo == integrity
                && Mathf.Abs(game.TimeLeft - remaining) < .05f && game.Coins == wallet,
                "Serialization round trip preserves position, damage and time without rewinding wallet");

            // Reload an already credited, unfinished-tour result. Its payment must
            // never be credited by deserialization or simply waiting on the result.
            Check(game.RestoreRunForTest(firstResultSnapshot), "Interim delivery result can be restored");
            Check(game.Phase == GamePhase.Result && game.Completed == 1 && !game.HasPackage && game.LastParcel == ParcelKind.Cake,
                "Restored result remembers the delivered parcel and completed count");
            yield return new WaitForSecondsRealtime(.25f);
            Check(game.Coins == wallet, "Restoring an already credited result never pays a second time");
            Check(game.RestoreRunForTest(firstResultSnapshot), "The same saved result can be loaded again safely");
            Check(game.Coins == wallet && game.Completed == 1, "Repeated result restoration is wallet-idempotent");
            Check(!game.RestoreRunForTest("") && !game.RestoreRunForTest("{invalid json"), "Empty and malformed run snapshots are rejected");
            Check(game.Phase == GamePhase.Result && game.Completed == 1 && game.Coins == wallet,
                "Rejected snapshots leave the live result and wallet untouched");
            game.ContinueDelivery();
            game.ContinueDelivery();
            Check(game.Phase == GamePhase.Playing && game.Completed == 1 && !game.HasPackage && game.Coins == wallet,
                "Continuing a restored result starts exactly its next package without recrediting");

            game.ShowMenu();
            game.OpenGarage();
            Check(game.Phase == GamePhase.Garage, "Garage opens its separate 3D room");
            Check(game.GarageDecorationCount == EquippedDecorCount(), "3D garage initially reflects its equipped decoration mask");
            int initialVisible = game.GarageDecorationCount;
            game.SelectDecor(-1);
            game.SelectDecor(99);
            Check(game.Coins == wallet && game.GarageDecorationCount == initialVisible,
                "Invalid decoration selections leave wallet and visible room unchanged");
            int expensive = -1;
            int buy = -1;
            int buyPrice = int.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                if (!game.DecorOwned(i) && game.DecorPrice(i) > wallet) expensive = i;
                if (!game.DecorOwned(i) && game.DecorPrice(i) <= wallet && game.DecorPrice(i) < buyPrice)
                { buy = i; buyPrice = game.DecorPrice(i); }
            }
            if (expensive >= 0)
            {
                game.SelectDecor(expensive);
                Check(!game.DecorOwned(expensive) && game.Coins == wallet && game.GarageDecorationCount == initialVisible,
                    "Unaffordable decoration cannot unlock or consume coins");
            }
            Check(buy >= 0 && buyPrice > 0, "Delivery earnings can afford at least one real garage decoration");
            game.SelectDecor(buy);
            yield return null;
            Check(game.DecorOwned(buy) && game.DecorEquipped(buy) && game.Coins == wallet - buyPrice,
                "Decoration purchase deducts its listed price and equips the owned object");
            Check(game.GarageDecorationCount == initialVisible + 1 && game.GarageDecorationCount == EquippedDecorCount(),
                "Purchasing decoration adds its actual 3D object to the garage");
            wallet = game.Coins;
            game.SelectDecor(buy);
            yield return null;
            Check(game.DecorOwned(buy) && !game.DecorEquipped(buy) && game.Coins == wallet
                && game.GarageDecorationCount == initialVisible,
                "Hiding an owned decoration changes the 3D room without charging again");
            game.SelectDecor(buy);
            yield return null;
            Check(game.DecorEquipped(buy) && game.Coins == wallet && game.GarageDecorationCount == initialVisible + 1,
                "Showing an owned decoration restores its 3D object without charging");
            if (expensive < 0)
            {
                for (int i = 0; i < 6; i++) if (!game.DecorOwned(i) && game.DecorPrice(i) > wallet) { expensive = i; break; }
                Check(expensive >= 0, "Garage includes an unowned decoration above the remaining budget");
                game.SelectDecor(expensive);
                Check(!game.DecorOwned(expensive) && game.Coins == wallet, "Insufficient decoration budget is enforced after a purchase");
            }
            yield return CapturePreview("v03-Garaj-Odasi-TR.png");
            game.ToggleLanguage();
            Check(!game.IsTurkish && game.Phase == GamePhase.Garage && game.GarageDecorationCount == EquippedDecorCount(),
                "English garage preserves its equipped 3D decorations");
            yield return CapturePreview("v03-Garage-Room-EN.png");
            game.ToggleLanguage();
            game.OpenShop();
            Check(game.ShopReturnsToGarage && game.Phase == GamePhase.Shop, "Scooter colors opened from garage remember their room origin");
            game.CloseShop();
            Check(game.Phase == GamePhase.Garage && game.Coins == wallet,
                "Closing scooter colors returns to garage without changing purchases");
            game.CloseOverlay();
            Check(game.Phase == GamePhase.Menu && game.Coins == wallet && game.HasSavedRun,
                "Closing garage returns to menu and preserves purchases and unfinished run");
        }

        int EquippedDecorCount()
        {
            int count = 0;
            for (int i = 0; i < 6; i++) if (game.DecorEquipped(i)) count++;
            return count;
        }

        void CheckFrozen(Vector3 position, float cargo, float timer, string label)
        {
            Check(Vector3.Distance(game.PlayerTransform.position, position) < .015f
                && Mathf.Abs(game.Cargo - cargo) < .001f && Mathf.Abs(game.TimeLeft - timer) < .025f, label);
        }

        IEnumerator VerifyDelivery(int expected, int priorCoins)
        {
            yield return WaitFor(() => game.Phase == GamePhase.Result, 2, "Delivery " + expected + " result appears");
            Check(game.Completed == expected && !game.HasPackage, "Delivery " + expected + " consumes one parcel and advances once");
            Check(game.LastPayout == 40 + game.LastSpeedBonus + game.LastCareBonus + game.LastComboBonus
                && game.LastPayout >= 40 && game.LastPayout <= 110,
                "Delivery " + expected + " has bounded, consistent reward breakdown");
            Check(game.Coins == priorCoins + game.LastPayout && game.Cargo >= 0 && game.Cargo <= 100,
                "Delivery " + expected + " credits its reward once and preserves valid cargo bounds");
            int reward = game.Coins;
            game.MoveWorld(Vector3.right);
            yield return new WaitForSecondsRealtime(.15f);
            Check(game.Coins == reward && game.Completed == expected, "Delivery " + expected + " result cannot duplicate payment");
            completedDeliveries = expected;
        }

        IEnumerator CapturePreview(string name, float settleSeconds = 1.1f)
        {
            string folder = Argument("-previewFolder", "");
            if (string.IsNullOrEmpty(folder)) yield break;
            folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(settleSeconds);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name));
            yield return new WaitForSecondsRealtime(.25f);
        }

        IEnumerator DriveTo(Vector3 target, bool allowDelivery = false)
        {
            bool monster = game.VehicleIndex == 3;
            float deadline = Time.realtimeSinceStartup + (monster ? 12 : 8);
            while (FlatDistance(game.PlayerTransform.position, target) > (monster ? .28f : .17f))
            {
                if (allowDelivery && game.Phase == GamePhase.Result) yield break;
                if (game.Phase != GamePhase.Playing)
                    throw new InvalidOperationException("Unexpected phase while navigating to " + target + ": " + game.Phase);
                if (Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Road navigation blocked. Target=" + target + " actual=" + game.PlayerTransform.position);
                Vector3 delta = target - game.PlayerTransform.position;
                delta.y = 0;
                // Slow near waypoints so steering acceleration does not overshoot.
                float throttle = Mathf.Clamp(delta.magnitude * 2, .12f, 1);
                if (monster)
                {
                    float alignment = Vector3.Dot(game.PlayerTransform.forward, delta.normalized);
                    // A car turns at low throttle before accelerating along its
                    // accepted heading. Straight reverse remains a real reverse.
                    if (alignment > -.7f && alignment < .94f) throttle = .14f;
                }
                game.MoveWorld(delta.normalized * throttle);
                yield return null;
            }
            game.MoveWorld(Vector3.zero);
            yield return null;
        }

        IEnumerator WaitFor(Func<bool> predicate, float seconds, string label)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline) throw new InvalidOperationException(label + " timed out.");
                yield return null;
            }
            report.checks.Add(label);
        }

        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Failed: " + label);
            report.checks.Add(label);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            return Vector3.Distance(a, b);
        }

        static string Argument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return fallback;
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            report.wallSeconds = Time.realtimeSinceStartup - started;
            report.completedDeliveries = completedDeliveries;
            if (game)
            {
                report.coins = game.Coins;
                report.bestScore = game.BestScore;
                report.cargo = game.Cargo;
            }
            report.passed = report.errors.Count == 0;
            try
            {
                string full = Path.GetFullPath(output);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full, JsonUtility.ToJson(report, true));
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not save smoke-test report: " + exception.Message);
                report.passed = false;
            }
            Debug.Log("KARGO_SMOKE_TEST " + (report.passed ? "PASS" : "FAIL") + " checks=" + report.checks.Count + " output=" + output);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}

