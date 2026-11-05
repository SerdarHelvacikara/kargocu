using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Opt-in v0.8 integration checks. Requires -smokeTest -v08Test; never funds a normal player.</summary>
    public sealed class CourierV08Test : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public bool passed;
            public List<string> checks = new List<string>(), errors = new List<string>();
            public float wallSeconds;
        }
        [Serializable] sealed class Snapshot
        {
            public int version, heavyIndex, comboCount, lastComboBonus;
            public int[] pickups;
            public float timeBudget;
        }
        readonly Report report = new Report();
        CourierGame game;
        MockAdService ads;
        MockStoreService store;
        float started;
        bool finished;

        void Start()
        {
            if (!HasArg("-smokeTest") || !HasArg("-v08Test")) { enabled = false; return; }
            Application.runInBackground = true;
            started = Time.realtimeSinceStartup;
            game = GetComponent<CourierGame>();
            Application.logMessageReceived += Log;
            StartCoroutine(Execute(Run()));
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
        void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(type + ": " + message); }
        void Update()
        { if (!finished && started > 0 && Time.realtimeSinceStartup - started > 240) { report.errors.Add("v0.8 test timed out."); Finish(); } }
        IEnumerator Execute(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            while (stack.Count > 0 && !finished)
            {
                bool moved = false; object next = null;
                try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
                catch (Exception error) { report.errors.Add(error.ToString()); break; }
                if (!moved) { stack.Pop(); continue; }
                if (next is IEnumerator child) stack.Push(child); else yield return next;
            }
            Finish();
        }

        IEnumerator Run()
        {
            yield return null;
            ads = game.AdService as MockAdService; store = game.StoreService as MockStoreService;
            Require(ads != null && store != null && game.CommerceIsMock, "Explicit test flags select only mock commerce services");
            Require(game.Coins == 0 && game.VehicleCount == 6 && !game.VehicleOwned(4) && !game.VehicleOwned(5), "v0.8 begins with an empty ordinary smoke wallet and locked fantasy vehicles");
            game.SelectVehicle(4); game.SelectVehicle(5);
            Check(game.Coins == 0 && game.VehicleIndex == 0 && !game.VehicleOwned(4) && !game.VehicleOwned(5), "Unaffordable fantasy vehicles cannot equip, spend or unlock");
            game.OpenGarage();
            int confirmed = store.ConfirmedCount;
            store.NextOutcome = MockCommerceOutcome.Cancelled;
            game.PurchaseProduct(CommerceConfiguration.CoinsMedium);
            Check(game.Coins == 0 && store.ConfirmedCount == confirmed, "Cancelled store purchase gives no coins and is not confirmed");
            store.DuplicateNextCallback = true;
            game.PurchaseProduct(CommerceConfiguration.CoinsMedium);
            Check(game.Coins == 1500 && store.ConfirmedCount == confirmed + 1, "A confirmed mock coin purchase fulfills and acknowledges a duplicate callback exactly once");
            for (int i = 0; i < 3; i++) game.PurchaseProduct(CommerceConfiguration.CoinsLarge);
            Check(game.Coins == 13500, "Additional test funds come from real mock-store purchase fulfillment after empty-wallet guards");
            yield return Rental();
            game.OpenGarage();
            int wallet = game.Coins;
            game.SelectVehicle(4);
            Check(game.VehicleOwned(4) && game.VehicleIndex == 4 && game.Coins == wallet - 4500 && game.HasVisibleDragon, "Coin purchase unlocks and displays the dragon for its single advertised price");
            wallet = game.Coins; game.SelectVehicle(5);
            Check(game.VehicleOwned(5) && game.VehicleIndex == 5 && game.Coins == wallet - 5500 && game.HasVisibleDinosaur && game.DinoScaresDogs, "Coin purchase unlocks the guarded dinosaur and its real dog-scare profile");
            yield return PickupAndReceipt();
            yield return FantasyPhysics();
            yield return RecallAndTime();
            yield return PanicWall();
            yield return RestoreAndReset();
            yield return StreetLife();
            yield return StreetIncidents();
            yield return SpecialOrders();
            yield return DistrictGarage();
            yield return NeighborhoodPolish();
        }

        IEnumerator NeighborhoodPolish()
        {
            game.OpenGarage();
            game.SelectVehicle(2);
            game.StartDistrict(0);
            game.EnableStreetEventsForTest(false);
            if (game.IsMuted) game.ToggleMute();
            yield return Wait(() => game.MusicRunning, "Procedural street music loops in actual gameplay");
            game.PauseGame();
            yield return null;
            Check(!game.MusicRunning, "Street music pauses in the overlay");
            game.ResumeGame();
            yield return Wait(() => game.MusicRunning, "Street music resumes with the delivery");
            game.ToggleMute();
            yield return null;
            Check(!game.MusicRunning, "The existing sound control mutes street music too");
            game.ToggleMute();
            if (!game.ReducedMotion) game.ToggleReducedMotion();
            yield return Wait(() => game.HasPackage, "Minivan loads real cargo before yielding to the cyclist");
            int blocked = game.CyclistYieldCount;
            float condition = game.Cargo;
            Vector3 bike = game.CityScene.BicyclePosition;
            game.PlaceForTest(bike + Vector3.back * 3.2f);
            game.PlayerTransform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 2;
            while (game.CyclistYieldCount == blocked && Time.realtimeSinceStartup < deadline)
            {
                game.MoveWorld(Vector3.forward);
                yield return null;
            }
            bike = game.CityScene.BicyclePosition;
            float separation = bike.z - game.PlayerTransform.position.z;
            Check(game.VehicleIndex == 2 && game.CyclistYieldCount > blocked && separation >= game.TrafficMargin + .49f && game.Cargo == condition,
                "Actual minivan stops behind the cyclist without passing through or damaging cargo");
            yield return InputFor(Vector3.back, .6f);
            Check(game.CityScene.BicyclePosition.z - game.PlayerTransform.position.z > separation, "Cyclist stop still permits retreat");
            game.CityScene.PlaceBicycleAtNorthEndForTest();
            game.PlaceForTest(new Vector3(16.5f, 0, .9f));
            game.PlayerTransform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 3.6f;
            while (game.PlayerTransform.position.z <= 5.6f && Time.realtimeSinceStartup < deadline)
            {
                game.MoveWorld(Vector3.forward);
                yield return null;
            }
            Check(game.PlayerTransform.position.z > 5.6f && game.Cargo == condition,
                "At the outer delivery approach the cyclist finds a shoulder and the loaded minivan continues without damage");
            game.ShowMenu();
        }

        IEnumerator DistrictGarage()
        {
            game.OpenGarage();
            yield return null;
            var screen = game.GarageScreen;
            Require(screen.Visible, "Normal garage entry displays the actual Canvas catalog");
            Check(screen.CatalogFitsViewport, "Canvas cards match the desktop viewport width without clipping");
            Require(screen.ClickForTest(GameText.Pick("Bölgeler", "Districts")) && screen.SelectedTab == 6 &&
                screen.CardCount == CourierGame.DistrictCount, "Live district tab shows all four selectable maps");
            int wallet = game.Coins;
            Check(screen.ClickForTest(game.DistrictPrice(1) + GameText.Pick(" jeton", " coins")) && game.Coins == wallet,
                "Tapping a locked map opens confirmation without spending");
            Check(screen.ClickForTest(GameText.Pick("Vazgeç", "Cancel")) && !game.DistrictOwned(1), "Cancelling the district modal keeps the map locked");
            screen.ClickForTest(GameText.Pick("Mağaza", "Store"));
            Check(screen.SelectedTab == 5 && screen.CardCount == game.StoreProducts.Count + 1, "Store tab lists the actual mock catalog and restore card");
            for (int i = 0; i < 5; i++) game.PurchaseProduct(CommerceConfiguration.CoinsLarge);
            yield return null;
            Require(screen.ClickForTest(GameText.Pick("Depo", "Depot")) && screen.SelectedTab == 7, "Live depot tab opens the actual upgrade card");
            int beforeUpgrade = game.Coins, upgradeCost = game.DepotPrice;
            Check(screen.ClickForTest(upgradeCost + GameText.Pick(" jeton", " coins")) && game.DepotLevel == 1, "Upgrade button previews the price before spending");
            Check(screen.ClickForTest(GameText.Pick("Onayla", "Confirm")) && game.DepotLevel == 2 && game.Coins == beforeUpgrade - upgradeCost &&
                game.LoadingDelay < .9f && game.DepotImpactScale < 1 && game.DepotDroneRate > 1,
                "Confirmed depot level purchase applies all three handling perks");
            string legacyHome = "";
            for (int district = 0; district < CourierGame.DistrictCount; district++)
            {
                game.OpenGarage();
                if (district > 0)
                {
                    wallet = game.Coins;
                    game.UnlockDistrict(district);
                    game.UnlockDistrict(district);
                    Require(game.DistrictOwned(district) && game.Coins == wallet - game.DistrictPrice(district), "District unlock charges its coin price once: " + district);
                }
                for (int vehicle = 2; vehicle < game.VehicleCount; vehicle++)
                {
                    game.SelectVehicle(vehicle);
                    Require(game.VehicleIndex == vehicle, "Testing equipped wide vehicle " + vehicle + " in district " + district);
                    game.StartDistrict(district);
                    game.EnableStreetEventsForTest(false);
                    bool accessible = true;
                    for (int i = 0; i < game.PickupCount; i++) accessible &= game.PickupNodeSafe(i);
                    if (district > 0)
                        for (int i = 0; i < game.DestinationCount; i++) accessible &= game.DistrictStopSafeForTest(i);
                    Check(accessible, "Every new map pickup and delivery pad clears the real wide hull: " + district + "/" + vehicle);
                    if (district == 0 && vehicle == 2)
                        legacyHome = new Regex("\"version\"\\s*:\\s*7").Replace(game.ExportRunForTest(), "\"version\":6", 1);
                    if (district > 0 && vehicle == 4)
                    {
                        yield return Wait(() => game.HasPackage, "Dragon loads a real parcel at district depot " + district);
                        if (HasArg("-districtPreviews"))
                        {
                            game.PlaceForTest(Vector3.zero);
                            yield return new WaitForSecondsRealtime(.95f);
                            string directory = Arg("-districtPreviews", "");
                            Directory.CreateDirectory(directory);
                            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "v09-Bolge-" + district + ".png"));
                            yield return new WaitForEndOfFrame();
                        }
                        yield return Deliver();
                        Check(game.LastBaseFee == game.DeliveryBaseFee + game.DistrictFee(district), "District bonus is included in its actual receipt: " + district);
                        string snapshot = game.ExportRunForTest();
                        int paid = game.Coins;
                        Check(game.RestoreRunForTest(snapshot) && game.DistrictIndex == district && game.Coins == paid,
                            "Resuming the district result preserves its address and pays nothing twice: " + district);
                    }
                    game.ShowMenu();
                    game.OpenGarage();
                }
                game.CloseOverlay();
            }
            Check(game.RestoreRunForTest(legacyHome) && game.DistrictIndex == 0, "Version-six route migrates into the original neighborhood");
            game.ShowMenu();
        }

        IEnumerator SpecialOrders()
        {
            foreach (OrderStyle style in new[] { OrderStyle.Urgent, OrderStyle.Fragile, OrderStyle.Chilled,
                OrderStyle.Chain, OrderStyle.Vip, OrderStyle.Surprise })
            {
                game.StartRun();
                game.EnableStreetEventsForTest(false);
                float ordinaryBudget = game.TimeBudget;
                game.SetOrderForTest(style);
                yield return Wait(() => game.HasPackage, "Actual pickup loads special order: " + style);
                if (style == OrderStyle.Urgent)
                    Check(game.IsUrgentOrder && game.TimeBudget < ordinaryBudget && game.TimeBudget >= 28, "Urgent order shortens the timer within safe bounds");
                if (style == OrderStyle.Fragile)
                    Check(game.Fragility > game.VehicleFragilityScale * 1.9f, "Glass vase applies increased damage susceptibility");
                if (style == OrderStyle.Chilled)
                {
                    yield return new WaitForSecondsRealtime(.6f);
                    int chill = game.ChillPercent;
                    Check(game.Cargo < 100 && chill < 100, "Ice cream melts through actual playing time");
                    game.PauseGame();
                    float cargo = game.Cargo;
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(game.Cargo == cargo && game.ChillPercent == chill, "Melt stops in pause");
                    game.ResumeGame();
                    Check(game.RestoreRunForTest(game.ExportRunForTest()) && game.ChillPercent == chill, "Cold state survives the real run serializer");
                }
                if (style == OrderStyle.Chain)
                {
                    Vector3 checkpoint = game.TargetPosition;
                    int wallet = game.Coins;
                    game.PlaceForTest(checkpoint);
                    yield return Wait(() => !game.AwaitingOrderCheckpoint, "Proximity stamps the chain checkpoint");
                    Check(game.Completed == 0 && game.HasPackage && game.Coins == wallet && game.TargetPosition != checkpoint,
                        "Checkpoint redirects the loaded delivery without a premature payment");
                    Check(game.RestoreRunForTest(game.ExportRunForTest()) && !game.AwaitingOrderCheckpoint && game.TargetPosition != checkpoint,
                        "Chain checkpoint survives restoring a loaded order");
                }
                int oldDecor = 0;
                for (int i = 0; i < game.DecorCount; i++) if (game.DecorOwned(i)) oldDecor++;
                yield return Deliver();
                int adjustment = game.LastBaseFee - game.DeliveryBaseFee;
                if (style == OrderStyle.Surprise)
                {
                    int decor = 0;
                    for (int i = 0; i < game.DecorCount; i++) if (game.DecorOwned(i)) decor++;
                    Check(adjustment == 32 || decor == oldDecor + 1, "Surprise order grants coins or one genuine owned decoration");
                }
                else Check(adjustment > 0, "Special order fee is included in its real receipt: " + style);
                int after = game.Coins;
                Check(game.RestoreRunForTest(game.ExportRunForTest()) && game.Phase == GamePhase.Result && game.Coins == after,
                    "Restoring a special receipt preserves its phase without paying again: " + style);
                game.ShowMenu();
            }
        }

        IEnumerator StreetIncidents()
        {
            var scene = game.IncidentScene;
            int coverage = 0;
            bool deterministic = true;
            for (int seed = 0; seed < 96; seed++)
            {
                scene.Configure(seed, new IncidentSave { enabled = true });
                int signature = 0, count = 0;
                for (int kind = 0; kind < 5; kind++)
                {
                    int slot = scene.Slot((Incident)kind);
                    signature = signature * 4 + slot + 1;
                    if (slot < 0) continue;
                    count++;
                    coverage |= 1 << kind;
                }
                scene.Configure(seed, new IncidentSave { enabled = true });
                int replay = 0;
                for (int kind = 0; kind < 5; kind++) replay = replay * 4 + scene.Slot((Incident)kind) + 1;
                deterministic &= count >= 1 && count <= 3 && signature == replay;
            }
            Check(deterministic && coverage == 31, "96 seeded tours deterministically select one to three of all five incidents");

            game.StartRun();
            game.EnableStreetEventsForTest(false);
            int fixtureSeed = 0;
            foreach (Incident kind in new[] { Incident.BurstPipe, Incident.Market, Incident.Wedding, Incident.Wind, Incident.Ducks })
            {
                for (fixtureSeed = 0; fixtureSeed < 96; fixtureSeed++)
                {
                    scene.Configure(fixtureSeed, new IncidentSave { enabled = true });
                    if (scene.Slot(kind) >= 0) break;
                }
                Require(fixtureSeed < 96, "Found seeded fixture for " + kind);
                scene.Configure(fixtureSeed, new IncidentSave { enabled = true, seconds = 5 + 17 * scene.Slot(kind) });
                Vector3 p = NeighborhoodIncidents.Position(kind);
                if (kind == Incident.BurstPipe) Check(scene.GripScale(p) < 1 && scene.GripScale(p + Vector3.right * 5) == 1, "Burst pipe affects braking grip only on its street");
                if (kind == Incident.Market) Check(scene.SpeedScale(p) < 1 && scene.SpeedScale(p + Vector3.right * 5) == 1, "Market shortcut is passable at a lower speed");
                if (kind == Incident.Wedding) Check(scene.ConstrainStep(p + Vector3.left, Vector3.right, .35f).x < .25f &&
                    scene.ConstrainStep(p + Vector3.left, Vector3.left, .35f) == Vector3.left, "Wedding blocks forward crossing and allows retreat");
                if (kind == Incident.Wind) Check(Mathf.Abs(scene.Gust) > .1f, "Wind has a deterministic active sway");
            }
            game.StreetScene.Configure(fixtureSeed);
            Vector3 crossing = NeighborhoodIncidents.Position(Incident.Ducks);
            game.PlaceForTest(crossing + Vector3.back * 2);
            int wallet = game.Coins;
            yield return InputFor(Vector3.forward, .8f);
            Check(game.PlayerTransform.position.z < crossing.z - .65f && game.Coins == wallet,
                "Actual scooter movement stops for the ducks before granting the waiting bonus");
            game.PauseGame();
            float clock = scene.Capture().seconds;
            yield return new WaitForSecondsRealtime(.15f);
            Check(scene.Capture().seconds == clock, "Pausing freezes the active incident clock");
            game.ResumeGame();
            yield return Wait(() => game.Coins == wallet + 8, "Waiting for the ducks pays eight coins once");
            string save = game.ExportRunForTest();
            Require(game.RestoreRunForTest(save), "Incident state round-trips through the real run serializer");
            yield return new WaitForSecondsRealtime(1.6f);
            Check(scene.Capture().duckPaid && game.Coins == wallet + 8, "Restoring an active crossing cannot pay its waiting bonus twice");
            game.ShowMenu();
        }

        IEnumerator StreetLife()
        {
            var city = game.CityScene;
            Check(city.StreetLifeRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Walkers, cyclist, pigeons, cat and street decorations add no collision or pickup obstruction");
            Vector3 start = city.FirstWalkerPosition;
            float clock = city.Clock;
            yield return new WaitForSecondsRealtime(.15f);
            Check(city.Clock == clock && city.FirstWalkerPosition == start, "Street activity freezes in the main menu");

            game.StartRun();
            yield return new WaitForSecondsRealtime(.2f);
            Check(city.Clock > clock && city.FirstWalkerPosition != start, "Actual gameplay advances the pedestrian route");
            game.PlaceForTest(CityLandmarks.PigeonPlaza);
            yield return Wait(() => city.PigeonsFlying, "Approaching the plaza starts the flock's takeoff");
            int takeoffs = city.PigeonTakeoffs;
            if (HasArg("-streetLifeScreenshot"))
            {
                yield return new WaitForSecondsRealtime(.40f);
                ScreenCapture.CaptureScreenshot(Arg("-streetLifeScreenshot", ""));
                yield return new WaitForEndOfFrame();
            }
            game.PauseGame();
            start = city.FirstWalkerPosition; clock = city.Clock;
            yield return new WaitForSecondsRealtime(.15f);
            Check(city.Clock == clock && city.FirstWalkerPosition == start && city.PigeonsFlying,
                "Pause freezes pedestrians and the flock midway through its takeoff");
            game.ToggleReducedMotion();
            game.ResumeGame();
            yield return Wait(() => !city.PigeonsFlying, "Reduced motion brings the flock back to ground");
            game.PlaceForTest(new Vector3(-8, 0, -8));
            yield return null;
            game.PlaceForTest(CityLandmarks.PigeonPlaza);
            yield return new WaitForSecondsRealtime(.15f);
            Check(!city.PigeonsFlying && city.PigeonTakeoffs == takeoffs, "Reduced motion keeps the flock grounded on approach");
            game.ShowMenu();
        }

        IEnumerator Rental()
        {
            int wallet = game.Coins, requests = ads.RewardedRequests;
            ads.NextOutcome = MockCommerceOutcome.Cancelled; game.RentWithAd(4);
            Check(game.RentalVehicle == -1 && game.VehicleIndex == 0 && game.Coins == wallet && ads.RewardedRequests == requests + 1, "Cancelled rental ad grants no vehicle or reward");
            ads.DuplicateNextCallback = true; game.RentWithAd(4);
            Check(game.RentalVehicle == 4 && game.VehicleIndex == 4 && !game.VehicleOwned(4) && Mathf.Abs(game.RentalRemaining - 180) < .01f && game.Coins == wallet, "Successful duplicate rental callback gives one temporary ride without coin ownership or spending");
            game.TickCommerceForTest(30);
            Check(Mathf.Abs(game.RentalRemaining - 180) < .01f, "Rental timer freezes in the garage");
            game.StartRun(); game.EnableStreetEventsForTest(false);
            yield return Wait(() => game.HasPackage, "Rented dragon picks up a genuine parcel");
            float seconds = game.RentalRemaining; game.TickCommerceForTest(10);
            Check(Mathf.Abs(game.RentalRemaining - (seconds - 10)) < .05f, "Rental uses active Playing seconds");
            game.PauseGame(); seconds = game.RentalRemaining; game.TickCommerceForTest(30);
            Check(Mathf.Abs(game.RentalRemaining - seconds) < .01f, "Paused rental does not expire");
            game.ResumeGame(); game.SetRentalSecondsForTest(1); game.TickCommerceForTest(2);
            Check(game.RentalWaitingForDelivery && game.VehicleIndex == 4 && game.HasPackage && game.VehicleAccessible(4), "Expired rental stays available until its carried parcel is delivered");
            int cash = game.Coins; yield return Deliver();
            Check(game.VehicleIndex == 0 && game.RentalVehicle == -1 && !game.VehicleOwned(4) && game.Coins == cash + game.LastPayout, "Delivery ends the deferred rental once and restores the preceding owned ride");
            game.ShowMenu();
        }

        IEnumerator PickupAndReceipt()
        {
            for (int vehicle = 4; vehicle <= 5; vehicle++)
            {
                game.SelectVehicle(vehicle);
                for (int i = 0; i < game.PickupCount; i++) Check(game.PickupNodeSafe(i), "Pickup " + i + " has a clear widest-vehicle approach with vehicle " + vehicle);
            }
            game.StartRun(); game.EnableStreetEventsForTest(false); game.PlaceForTest(new Vector3(0, 0, -12));
            int seen = 0;
            for (int seed = 1; seed <= 80; seed++)
            {
                Require(game.ConfigurePickupsForTest(seed), "Deterministic pickup plan is constructible for seed " + seed);
                var s = JsonUtility.FromJson<Snapshot>(game.ExportRunForTest());
                Require(s != null && s.version == 7 && s.pickups != null && s.pickups.Length == 3, "Configured pickup plan exports a valid v7 snapshot");
                Require(s.pickups[0] != s.pickups[1] && s.pickups[1] != s.pickups[2], "Consecutive pickups do not repeat");
                for (int i = 0; i < 3; i++) seen |= 1 << s.pickups[i];
            }
            Check(seen == 255, "Deterministic tours can use all eight actual pickup nodes");
            string v7 = game.ExportRunForTest(); int coins = game.Coins;
            string v5 = new Regex("\"version\"\\s*:\\s*7").Replace(v7, "\"version\":5", 1);
            Check(game.RestoreRunForTest(v5) && game.TimeBudget == 55 && game.PickupPosition == game.PickupNodePosition(0) && game.ComboCount == 0 && game.Coins == coins, "v5 snapshot migrates to depot/55-second v7 defaults without crediting funds");
            Require(game.RestoreRunForTest(v7), "Current v7 pickup plan restores after migration probe");
            game.SetRouteForTest(5, 3, 4);
            Require(game.ConfigurePickupsForTest(71), "Factory route selects a pickup away from its actual delivery address");
            Check(game.TargetPosition == game.PickupPosition && !string.IsNullOrEmpty(game.PickupName), "Empty courier target and localized label point to the selected pickup");
            game.PlaceForTest(game.PickupPosition);
            yield return Wait(() => game.HasPackage, "An alternate pickup's actual proximity loads the parcel");
            string heavy = Number(game.ExportRunForTest(), "heavyIndex", 0);
            Require(game.RestoreRunForTest(heavy), "Factory heavy-delivery fixture passes normal v7 validation");
            Check(game.IsHeavyDelivery && game.HandlingSpeed < 5.1f * game.VehicleSpeedScale * .8f, "Factory cargo applies the heavier actual handling profile");
            yield return Deliver();
            Check(game.LastBaseFee == 135 && game.LastMultiplier == 1 && game.LastPayout == 135 + game.LastSpeedBonus + game.LastCareBonus + game.LastComboBonus, "Dinosaur factory delivery includes the +80 fee and a consistent full receipt");
            yield return NextParcel(); yield return Deliver();
            Check(game.ComboCount == 2 && game.LastComboBonus == 5, "Two careful timely deliveries earn the separate five-coin streak bonus");
            yield return NextParcel(); yield return Deliver();
            Check(game.ComboCount == 3 && game.LastComboBonus == 10, "A third careful delivery earns the capped ten-coin streak bonus");
            game.ForceInterstitialEligibilityForTest();
            int wallet = game.Coins, requests = ads.RewardedRequests;
            ads.NextOutcome = MockCommerceOutcome.Cancelled; game.RequestDoubleReward();
            Check(game.Coins == wallet && game.CanDoubleReward, "Cancelled end-of-tour ad leaves the reward unclaimed");
            ads.DuplicateNextCallback = true; game.RequestDoubleReward();
            int paid = game.Coins - wallet;
            Check(paid > 0 && !game.CanDoubleReward, "Completed round can be doubled through its actual rewarded placement");
            game.RequestDoubleReward();
            Check(game.Coins == wallet + paid && ads.RewardedRequests == requests + 2, "Duplicate callback and repeated tap cannot double a completed round twice");
            int interstitials = ads.InterstitialRequests;
            game.ShowMenu();
            Check(game.Phase == GamePhase.Menu && ads.InterstitialRequests == interstitials + 1, "Eligible interstitial runs only after the final result when leaving the tour");
        }

        IEnumerator FantasyPhysics()
        {
            game.SelectVehicle(4); game.StartRun();
            yield return Wait(() => game.HasPackage, "Owned dragon begins its real physics probe with cargo");
            game.EnableStreetEventsForTest(true); game.StreetScene.ConfigureFixed(false, true, 0);
            Vector3 work = game.StreetScene.WorksitePosition;
            game.PlaceForTest(work + Vector3.left * 4); game.PlayerTransform.rotation = Quaternion.Euler(0, 90, 0); Physics.SyncTransforms();
            float cargo = game.Cargo;
            yield return InputFor(Vector3.right, 1.8f);
            Check(game.DragonCanFlyOver(game.StreetScene.WorksiteCollider) && game.PlayerTransform.position.x > work.x + 2 && game.Cargo == cargo, "Dragon physically clears the named roadwork barrier without package damage");
            game.EnableStreetEventsForTest(false);
            game.PlaceForTest(new Vector3(-8.45f, 0, -5.4f)); game.PlayerTransform.rotation = Quaternion.identity; Physics.SyncTransforms();
            cargo = game.Cargo; yield return InputFor(Vector3.forward, 1.3f);
            Check(game.PlayerTransform.position.z > -.8f && game.Cargo == cargo, "Dragon's raised capsule and filtered sweep both clear the real west crate");
            yield return FlyShortcut(new Vector3(0, 0, 11.3f), new Vector3(0, 0, 13.75f), new Vector3(-6.5f, 0, 13.75f));
            yield return FlyShortcut(new Vector3(11.3f, 0, 0), new Vector3(13.75f, 0, 0), new Vector3(13.75f, 0, 6.5f));
            game.PlaceForTest(new Vector3(-4, 0, 12.5f)); game.PlayerTransform.rotation = Quaternion.identity; Physics.SyncTransforms();
            yield return InputFor(Vector3.forward, .8f);
            Check(game.PlayerTransform.position.z < 13.5f, "Flying dragon cannot cross the real brick alley wall");
            var fence = new GameObject("v08 test unrelated low fence");
            fence.transform.position = new Vector3(0, .35f, -12); fence.AddComponent<BoxCollider>().size = new Vector3(3, .7f, .2f);
            game.PlaceForTest(new Vector3(0, 0, -15.2f)); game.PlayerTransform.rotation = Quaternion.identity; Physics.SyncTransforms();
            yield return InputFor(Vector3.forward, 1);
            Check(game.PlayerTransform.position.z < -13.7f && !game.DragonCanFlyOver(fence.GetComponent<Collider>()), "Unrelated short barriers remain solid despite the high flying capsule");
            Destroy(fence); yield return null;
            yield return Deliver(); game.ShowMenu(); game.SelectVehicle(5); game.StartRun();
            yield return Wait(() => game.HasPackage, "Dinosaur begins its strapped-cargo dog fixture");
            game.EnableStreetEventsForTest(true); game.StreetScene.ConfigureFixed(false, false);
            var dogs = game.DogScene; dogs.ConfigureFixed(1); dogs.Tick(new Vector3(-4, 0, 9), .05f, false);
            game.PlaceForTest(new Vector3(-3.8f, 0, 13.75f));
            int wallet = game.Coins; cargo = game.Cargo;
            yield return new WaitForSecondsRealtime(.6f);
            Check(dogs.ScaredCount > 0 && !dogs.AnyChasing && game.PanicTimeLeft == 0 && game.Cargo == cargo && game.Coins == wallet, "Actual dinosaur presence scares the shortcut dog without panic, damage or escape money");
            dogs.ConfigureFixed(0); game.EnableStreetEventsForTest(false); yield return Deliver(); game.ShowMenu();
        }

        IEnumerator FlyShortcut(Vector3 approach, Vector3 junction, Vector3 exit)
        {
            game.PlaceForTest(approach);
            game.PlayerTransform.rotation = Quaternion.LookRotation(junction - approach);
            Physics.SyncTransforms();
            float integrity = game.Cargo;
            yield return FlyTo(junction);
            Vector3 turnStart = game.PlayerTransform.position;
            yield return FlyTo(exit);
            Vector3 actual = game.PlayerTransform.position;
            Check(Vector3.Distance(turnStart, junction) < .2f && Vector3.Distance(actual, exit) < .2f && game.Cargo == integrity,
                $"Loaded dragon clears the widened shortcut without scraping. Turn {turnStart}, exit {actual}/{exit}, cargo {game.Cargo}/{integrity}");
        }

        IEnumerator FlyTo(Vector3 destination)
        {
            float until = Time.realtimeSinceStartup + 4;
            while (Time.realtimeSinceStartup < until)
            {
                Vector3 remaining = destination - game.PlayerTransform.position;
                if (remaining.sqrMagnitude < .09f * .09f) break;
                game.MoveWorld(Vector3.ClampMagnitude(remaining * 1.8f, 1));
                yield return null;
            }
        }

        IEnumerator RecallAndTime()
        {
            game.SelectVehicle(1); game.StartRun(); game.EnableStreetEventsForTest(false);
            yield return Wait(() => game.HasPackage, "Fast scooter loads the actual parcel used for recall");
            game.PlaceForTest(new Vector3(-8.45f, 0, -4)); game.PlayerTransform.rotation = Quaternion.identity; Physics.SyncTransforms();
            yield return InputFor(Vector3.forward, 1);
            Require(game.IsParcelDropped && !game.HasPackage, "Real fast crash drops the parcel before rewarded recall");
            float integrity = game.Cargo; int wallet = game.Coins;
            ads.NextOutcome = MockCommerceOutcome.Cancelled; game.RequestParcelRecall();
            Check(game.IsParcelDropped && game.Cargo == integrity && game.Coins == wallet, "Cancelled recall keeps the real dropped parcel and condition");
            int requests = ads.RewardedRequests; ads.DuplicateNextCallback = true; game.RequestParcelRecall();
            Check(!game.IsParcelDropped && game.HasPackage && game.Cargo == integrity && game.Coins == wallet, "Rewarded recall retrieves once without repairing or paying the parcel");
            game.RequestParcelRecall();
            Check(ads.RewardedRequests == requests + 1, "A duplicate recall tap cannot open another ad for recovered cargo");
            Require(game.RestoreRunForTest(Number(game.ExportRunForTest(), "timeLeft", 5)), "Low-time fixture passes the normal save validator");
            ads.NextOutcome = MockCommerceOutcome.Cancelled; game.RequestExtraTime();
            Check(Mathf.Abs(game.TimeLeft - 5) < .1f && game.CanExtraTime, "Cancelled time ad grants no seconds and leaves the choice available");
            requests = ads.RewardedRequests; ads.CloseBeforeRewardNext = true; ads.DuplicateNextCallback = true; game.RequestExtraTime();
            Check(Mathf.Abs(game.TimeLeft - 20) < .1f && game.Phase == GamePhase.Playing && !game.CanExtraTime, "A late-after-close duplicate reward gives exactly fifteen seconds and resumes play");
            game.RequestExtraTime(); Check(ads.RewardedRequests == requests + 1, "Extra time is single-use for this delivery");
            game.ForceInterstitialEligibilityForTest(); int interstitials = ads.InterstitialRequests;
            game.ShowMenu(); Check(ads.InterstitialRequests == interstitials, "Even eligible counters cannot show an interstitial while abandoning active play");
            game.ResumeRun(); yield return Deliver(); game.ShowMenu();
        }

        IEnumerator PanicWall()
        {
            game.SelectVehicle(0); game.StartRun();
            Require(game.VehicleIndex == 0, "Panic-wall fixture uses the ordinary scooter");
            yield return Wait(() => game.HasPackage, "Ordinary courier loads a parcel before the panic-wall fixture");
            game.EnableStreetEventsForTest(true); game.StreetScene.ConfigureFixed(false, false);
            var dogs = game.DogScene; dogs.ConfigureFixed(1);
            yield return null;
            game.PlaceForTest(new Vector3(-4, 0, 14.25f));
            dogs.Tick(game.PlayerTransform.position, .01f, false);
            Require(dogs.AnyWarning, "The rider entering the alley starts the dog warning");
            dogs.Tick(game.PlayerTransform.position, 1.2f, false);
            Require(game.PanicTimeLeft > 0 && dogs.AnyChasing, "Real dog chase emits panic through the subscribed gameplay event");
            Check(game.BarkPulseActive, "Actual chase bark shows the reusable amber shock ring");
            float panic = game.PanicTimeLeft, clock = game.FeelClock;
            game.PauseGame(); yield return new WaitForSecondsRealtime(.15f);
            Check(Mathf.Abs(game.PanicTimeLeft - panic) < .01f && Mathf.Abs(game.FeelClock - clock) < .01f, "Panic and comedy clocks freeze in the pause overlay");
            game.ResumeGame(); float maxZ = game.PlayerTransform.position.z;
            float until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until) { game.MoveWorld(Vector3.forward); maxZ = Mathf.Max(maxZ, game.PlayerTransform.position.z); yield return null; }
            Check(maxZ > 14.35f && maxZ < 15.1f && Mathf.Abs(game.PlayerTransform.position.x) <= WorldBuilder.PlayableHalfExtent, "Panic input reaches but never crosses the solid north brick wall");
            dogs.ConfigureFixed(0); game.EnableStreetEventsForTest(false); yield return Deliver(); game.ShowMenu();
        }

        IEnumerator RestoreAndReset()
        {
            game.OpenGarage(); int wallet = game.Coins;
            store.SeedRestorable(CommerceConfiguration.Dragon, "v08-restored-dragon"); store.DuplicateNextCallback = true; game.RestorePurchases();
            Check(game.RootPermanentVehicleOwned(4) && game.Coins == wallet, "Restoring a permanent vehicle does not restore consumable coins or duplicate wallet value");
            game.PurchaseProduct(CommerceConfiguration.RemoveAds);
            Check(game.NoInterstitialAds, "Confirmed remove-ads purchase grants the actual entitlement");
            int purchasedCoins = game.PurchasedCoinsRemaining;
            game.ResetProgress();
            Check(game.Coins == purchasedCoins && game.PurchasedCoinsRemaining == purchasedCoins && game.VehicleOwned(4) && !game.VehicleOwned(5) && game.NoInterstitialAds && game.VehicleIndex == 0, "Progress reset clears earned progress and coin-bought dinosaur while keeping paid entitlements and unspent purchased coins");
            game.OpenGarage(); store.SeedRestorable(CommerceConfiguration.Fleet, "v08-restored-fleet"); game.RestorePurchases(); game.RestorePurchases();
            Check(game.VehicleOwned(4) && game.VehicleOwned(5) && game.Coins == purchasedCoins, "Fleet restoration after reset is repeatable and grants both permanent vehicles without adding coins");
            int acknowledged = store.ConfirmedCount;
            store.Purchase(CommerceConfiguration.CoinsSmall, ignored => { }); store.ReplayPending(); store.ReplayPending();
            Check(game.Coins == purchasedCoins + 500 && store.ConfirmedCount == acknowledged + 1, "Pending consumable redelivery fulfills and confirms exactly once after a lost store callback");
            game.CloseOverlay(); game.StartRun();
            yield return Wait(() => game.HasPackage, "No-ads entitlement probe starts a real final tour");
            for (int i = 0; i < 3; i++) { yield return Deliver(); if (i < 2) yield return NextParcel(); }
            game.ForceInterstitialEligibilityForTest(); int adsBefore = ads.InterstitialRequests;
            game.ShowMenu(); Check(game.Phase == GamePhase.Menu && ads.InterstitialRequests == adsBefore, "Permanent remove-ads entitlement suppresses an otherwise eligible final-result interstitial");
        }

        IEnumerator NextParcel()
        { game.ContinueDelivery(); game.PlaceForTest(game.PickupPosition); yield return Wait(() => game.HasPackage, "Actual next pickup loads parcel " + (game.Completed + 1)); }
        IEnumerator Deliver()
        {
            Require(game.Phase == GamePhase.Playing && game.HasPackage, "Delivery fixture starts with actual carried cargo");
            int wallet = game.Coins, completed = game.Completed;
            game.PlaceForTest(game.TargetPosition);
            yield return Wait(() => game.Phase == GamePhase.Result, "Actual destination proximity completes delivery");
            Check(game.Completed == completed + 1 && game.Coins == wallet + game.LastPayout &&
                game.LastPayout == (game.LastBaseFee + game.LastSpeedBonus + game.LastCareBonus) * game.LastMultiplier + game.LastComboBonus,
                "Actual delivery consumes one parcel and pays one complete v0.8 receipt");
        }
        IEnumerator InputFor(Vector3 direction, float seconds)
        { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) { game.MoveWorld(direction); yield return null; } game.MoveWorld(Vector3.zero); }
        IEnumerator Wait(Func<bool> predicate, string description)
        { float until = Time.realtimeSinceStartup + 4; while (!predicate() && Time.realtimeSinceStartup < until) yield return null; Require(predicate(), description); }
        void Check(bool result, string description) { report.checks.Add((result ? "PASS " : "FAIL ") + description); if (!result) report.errors.Add(description); }
        void Require(bool result, string description) { Check(result, description); if (!result) throw new InvalidOperationException(description); }
        static string Number(string json, string key, float value) => Regex.Replace(json, "(\"" + key + "\"\\s*:\\s*)-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?", match => match.Groups[1].Value + value.ToString(CultureInfo.InvariantCulture));
        static bool HasArg(string flag) => Array.Exists(Environment.GetCommandLineArgs(), value => value == flag);
        static string Arg(string key, string fallback)
        { string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        void Finish()
        {
            if (finished) return; finished = true; report.wallSeconds = Time.realtimeSinceStartup - started; report.passed = report.errors.Count == 0;
            string path = Path.GetFullPath(Arg("-testOutput", Path.Combine(Application.persistentDataPath, "v08-test.json")));
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}

