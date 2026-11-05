using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// Two-process check of the normal local-save path. LocalProgress must select
    /// its isolated test prefix before CourierGame loads any player preferences.
    /// </summary>
    public sealed class CourierPersistenceTest : MonoBehaviour
    {
        [Serializable]
        public sealed class ExpectedState
        {
            public Vector3 position;
            public float cargo;
            public float timeLeft;
            public bool hasPackage;
            public int coins;
            public int bestScore;
            public bool turkish;
            public float volume;
            public bool reducedMotion;
            public int completed;
            public int vehicleIndex;
            public int rentalVehicle;
            public float rentalRemaining;
            public bool parcelDropped;
            public Vector3 droppedParcelPosition;
            public bool hasTruck;
            public bool hasWorksite;
            public int worksiteIndex;
            public int streetSeed;
            public float streetClock;
            public bool incidentsEnabled;
            public float incidentSeconds;
            public int chillPercent;
            public int district, depotLevel;
            public bool droneOwned;
            public bool droneBusy;
            public float droneProgress;
            public int droneCompletedFlights;
            public int[] businessProgress;
            public int businessOrders;
            public int decorOwnedMask;
            public int decorEquippedMask;
            public int runVersion;
            public int[] route;
            public int lastMultiplier;
            public string savedRunJson;
        }

        [Serializable]
        sealed class SavedRoute
        {
            public int version;
            public int[] route;
            public int lastMultiplier;
        }

        [Serializable]
        public sealed class Report
        {
            public string stage;
            public bool passed;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public float wallSeconds;
            public string expectedStateFile;
            public string screenshot;
            public ExpectedState actual;
        }

        CourierGame game;
        readonly Report report = new Report();
        float started;
        bool finished;
        bool readStage;
        bool withDrone;
        bool withRental;
        bool withIncidents;
        bool withOrders;
        bool withDistrict;
        string outputPath;
        string statePath;

        void Start()
        {
            Application.runInBackground = true;
            game = GetComponent<CourierGame>();
            started = Time.realtimeSinceStartup;
            string[] args = Environment.GetCommandLineArgs();
            bool write = Array.Exists(args, argument => argument == "-persistenceCheckWrite");
            readStage = Array.Exists(args, argument => argument == "-persistenceCheckRead");
            withDrone = Array.Exists(args, argument => argument == "-withDrone");
            withRental = Array.Exists(args, argument => argument == "-withRental");
            withIncidents = Array.Exists(args, argument => argument == "-withIncidents");
            withOrders = Array.Exists(args, argument => argument == "-withOrders");
            withDistrict = Array.Exists(args, argument => argument == "-withDistrict");
            report.stage = readStage ? "read-after-relaunch" : "write-before-exit";
            statePath = Path.GetFullPath(Argument("-persistenceState", Path.Combine(Application.persistentDataPath, "persistence-state.json")));
            outputPath = Path.GetFullPath(Argument("-testOutput", Path.Combine(Application.persistentDataPath, "persistence-check.json")));
            report.expectedStateFile = statePath;
            Application.logMessageReceived += OnLog;
            StartCoroutine(Execute(Run(write)));
        }

        void OnDestroy() => Application.logMessageReceived -= OnLog;

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - started > 180)
            {
                report.errors.Add("Persistence probe timed out after 180 seconds.");
                Finish();
            }
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                report.errors.Add(type + ": " + condition);
        }

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
                if (failure != null) { report.errors.Add(failure.Message); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator child) stack.Push(child);
                else yield return current;
            }
            Finish();
        }

        IEnumerator Run(bool writeStage)
        {
            yield return null;
            Check(writeStage != readStage, "Exactly one persistence probe stage is selected");
            Check(LocalProgress.TestProfile, "LocalProgress isolated test profile is active before any probe writes");
            Check(game != null && game.PlayerTransform != null, "The ordinary game and world initialized");
            Check(game.Phase == GamePhase.Menu, "The process starts at the normal menu");
            if (writeStage) yield return WriteStage();
            else yield return ReadStage();
        }

        IEnumerator WriteStage()
        {
            Check(!game.HasSavedRun, "A fresh isolated write profile has no previous run");
            Check(game.Coins == 0 && game.BestScore == 0, "A fresh isolated profile has no artificial wallet funds or score");
            game.AddPlaytestCoins();
            Check(!game.IsPlaytest && game.Coins == 0, "Playtest-only grants cannot enter the ordinary persistence economy");
            game.SelectVehicle(1);
            Check(game.VehicleIndex == 1, "The fast scooter can be selected before a new saved run");
            if (withDrone) yield return EarnAndLaunchDrone();
            if (withRental)
            {
                game.OpenGarage();
                game.RentWithAd(4);
                Check(game.CommerceIsMock && game.VehicleIndex == 4 && game.RentalRemaining == 180 && !game.VehicleOwned(4),
                    "A completed mock ad equips a temporary dragon without granting ownership");
                game.CloseOverlay();
            }
            if (withDistrict)
            {
                game.OpenGarage();
                game.PurchaseProduct(CommerceConfiguration.CoinsMedium);
                game.UnlockDistrict(1);
                game.UpgradeDepot();
                game.StartDistrict(1);
            }
            else game.StartRun();
            if (withOrders) game.SetOrderForTest(OrderStyle.Chilled);
            if (game.Phase == GamePhase.Tutorial) game.FinishTutorial();
            Check(game.Phase == GamePhase.Playing, "Normal start and first-run tutorial lead into gameplay");
            float deadline = Time.realtimeSinceStartup + 4;
            while (!game.HasPackage)
            {
                if (Time.realtimeSinceStartup > deadline) throw new InvalidOperationException("Normal depot pickup timed out.");
                yield return null;
            }
            Check(game.HasPackage && game.ActiveParcel == ParcelKind.Cake, "The real depot pickup loads the first cake");

            Vector3 start = game.PlayerTransform.position;
            float until = Time.realtimeSinceStartup + .5f;
            while (Time.realtimeSinceStartup < until)
            {
                game.MoveWorld(Vector3.right);
                yield return null;
            }
            game.MoveWorld(Vector3.zero);
            Check(Vector3.Distance(start, game.PlayerTransform.position) > 1,
                "Actual controller movement leaves the depot before saving");
            game.SettingVolume(.37f);
            if (!game.ReducedMotion) game.ToggleReducedMotion();
            GameText.SetLanguage(false);
            if (withIncidents)
            {
                game.IncidentScene.Configure(game.StreetScene.Seed, new IncidentSave { enabled = true });
                yield return new WaitForSecondsRealtime(.25f);
            }
            game.PauseGame();
            Check(game.Phase == GamePhase.Paused, "The loaded delivery pauses before saving");
            game.ShowMenu();
            Check(game.Phase == GamePhase.Menu && game.HasSavedRun, "Returning to menu saves a resumable unfinished delivery");
            report.actual = Capture();
            if (withIncidents)
                Check(report.actual.incidentsEnabled && report.actual.incidentSeconds > 0, "Active play time is captured in the incident save");
            if (withOrders)
                Check(report.actual.chillPercent > 0 && report.actual.chillPercent < 100, "Cold-chain order has partially melted before the writer exits");
            if (withDistrict)
                Check(report.actual.district == 1 && report.actual.depotLevel == 2, "A paid district unlock and depot upgrade are written through normal gameplay");
            Check(report.actual.hasPackage && report.actual.completed == 0 && !report.actual.turkish
                && report.actual.reducedMotion && Mathf.Abs(report.actual.volume - .37f) < .001f
                && report.actual.vehicleIndex == (withRental ? 4 : 1) && !report.actual.parcelDropped,
                "Saved state includes loaded cargo, English, reduced motion, volume and the selected vehicle");
            if (withRental)
                Check(report.actual.rentalVehicle == 4 && report.actual.rentalRemaining > 0 && report.actual.rentalRemaining < 180,
                    "The writer saves an actual partially used rental");
            if (withDrone)
                Check(report.actual.droneOwned && report.actual.droneBusy && report.actual.droneProgress > 0
                    && report.actual.droneProgress < 1 && report.actual.droneCompletedFlights == 0,
                    "Optional drone probe saves a genuinely purchased, launched and partially progressed mission");
            Check(report.actual.runVersion == 7 && report.actual.route != null && report.actual.route.Length == 3
                && report.actual.route[0] >= 0 && report.actual.route[0] < 3
                && report.actual.route[1] >= 3 && report.actual.route[1] < 6,
                "The final ordinary version-seven save records its naturally selected central and outer delivery route");
            if (withDrone)
                Check((report.actual.decorOwnedMask & (1 << 9)) != 0 && (report.actual.decorEquippedMask & (1 << 9)) != 0
                    && report.actual.businessOrders == 2 && report.actual.businessProgress[0] >= 1,
                    "Earned business claims and a purchased decoration above the old nine-bit mask are saved through normal progress");
            Directory.CreateDirectory(Path.GetDirectoryName(statePath));
            File.WriteAllText(statePath, JsonUtility.ToJson(report.actual, true));
            Check(File.Exists(statePath), "Expected state is recorded for the separate reader process");
            yield return CaptureScreenshot();
        }

        IEnumerator ReadStage()
        {
            Check(File.Exists(statePath), "The earlier process wrote an expected-state record");
            ExpectedState expected = JsonUtility.FromJson<ExpectedState>(File.ReadAllText(statePath));
            Check(expected != null && expected.hasPackage && expected.completed == 0,
                "The expected state describes a genuine unfinished loaded delivery");
            Check(game.HasSavedRun, "The new process finds a saved run through the normal load path");
            Check(game.IsTurkish == expected.turkish && game.ReducedMotion == expected.reducedMotion
                && Mathf.Abs(game.Volume - expected.volume) < .001f,
                "Language, reduced motion and volume survived full process exit and relaunch");
            Check(game.Coins == expected.coins && game.BestScore == expected.bestScore,
                "Wallet and best score loaded without fabricated funds or rewards");
            if (withDistrict)
                Check(game.DistrictOwned(1) && game.DepotLevel == expected.depotLevel && game.DepotLevel == 2,
                    "District ownership and paid depot level are loaded from their separate keys");
            Check(game.VehicleIndex == expected.vehicleIndex && expected.vehicleIndex == (withRental ? 4 : 1),
                "The selected vehicle survived full process exit and relaunch");
            if (withRental)
            {
                Check(game.CommerceIsMock && game.RentalVehicle == 4 && game.RentalVehicle == expected.rentalVehicle
                    && Mathf.Abs(game.RentalRemaining - expected.rentalRemaining) < .001f && !game.VehicleOwned(4),
                    "Cold load restores the exact remaining rental without turning it into ownership");
                yield return new WaitForSecondsRealtime(.2f);
                Check(Mathf.Abs(game.RentalRemaining - expected.rentalRemaining) < .001f, "The cold-loaded menu does not spend rented seconds");
            }
            Check(!game.CanChangeVehicle, "A cold-loaded menu locks vehicle choice while its saved run carries cargo");
            game.SelectVehicle(0);
            Check(game.VehicleIndex == expected.vehicleIndex && !game.CanChangeVehicle && game.Coins == expected.coins,
                "Cold-loaded carried cargo cannot bypass its vehicle lock through the menu");
            game.ResumeRun();
            ExpectedState actual = Capture();
            if (withDistrict)
                Check(actual.district == expected.district && actual.district == 1, "The v7 run reconstructs the selected district after process relaunch");
            if (withIncidents)
                Check(actual.incidentsEnabled && Mathf.Abs(actual.incidentSeconds - expected.incidentSeconds) < .001f,
                    "A new process restores the seeded incident clock exactly");
            if (withOrders)
                Check(game.IsChilledOrder && actual.chillPercent == expected.chillPercent && actual.chillPercent < 100,
                    "A new process restores the cold-chain order and its partially melted state");
            Check(game.Phase == GamePhase.Playing && actual.hasPackage == expected.hasPackage
                && actual.completed == expected.completed && game.ActiveParcel == ParcelKind.Cake
                && game.CargoVisual.Kind == ParcelKind.Cake,
                "Normal ResumeRun restores the delivery phase and its actual visible cargo");
            Check(Vector3.Distance(actual.position, expected.position) < .03f,
                "Scooter position survives full relaunch within three centimeters");
            Check(Mathf.Abs(actual.cargo - expected.cargo) < .001f
                && Mathf.Abs(actual.timeLeft - expected.timeLeft) < .03f,
                "Cargo integrity and remaining bonus time survive full relaunch");
            Check(actual.coins == expected.coins && actual.bestScore == expected.bestScore,
                "Resuming the saved delivery does not credit any new payment");
            Check(actual.vehicleIndex == expected.vehicleIndex && actual.parcelDropped == expected.parcelDropped
                && !actual.parcelDropped,
                "Restored loaded cargo keeps its selected vehicle and is not mistaken for a dropped parcel");
            Check(actual.hasTruck == expected.hasTruck && actual.hasWorksite == expected.hasWorksite
                && actual.worksiteIndex == expected.worksiteIndex && actual.streetSeed == expected.streetSeed,
                "Random street mode, worksite and seed survive full process exit and relaunch");
            Check(Mathf.Abs(actual.streetClock - expected.streetClock) < .03f,
                "Random street simulation resumes at its saved event clock after relaunch");
            Check(actual.droneOwned == expected.droneOwned && actual.droneBusy == expected.droneBusy
                && actual.droneCompletedFlights == expected.droneCompletedFlights
                && Mathf.Abs(actual.droneProgress - expected.droneProgress) < .003f,
                "Drone ownership, busy mission, flight count and progress survive full process exit and relaunch");
            Check(SameInts(actual.businessProgress, expected.businessProgress) && actual.businessOrders == expected.businessOrders,
                "All four repeatable business counters and consumed claim orders survive relaunch without recounting deliveries");
            Check(actual.decorOwnedMask == expected.decorOwnedMask && actual.decorEquippedMask == expected.decorEquippedMask,
                "Decoration ownership and equipped masks, including expanded high bits, survive full relaunch");
            Check(actual.runVersion == 7 && SameInts(actual.route, expected.route)
                && actual.lastMultiplier == expected.lastMultiplier && actual.savedRunJson == expected.savedRunJson
                && game.ActiveStopIndex == expected.route[0],
                "The raw version-seven saved route and receipt multiplier survive cold load and target the same destination");
            if (withDrone)
                Check(actual.droneOwned && actual.droneBusy && actual.droneProgress > 0 && actual.droneProgress < 1,
                    "Optional drone reader resumes the paid helper's actual unfinished mission");
            report.actual = actual;
            yield return CaptureScreenshot();
        }

        ExpectedState Capture()
        {
            string runJson = LocalProgress.GetString("kk_activeRun_v7", "");
            SavedRoute saved = string.IsNullOrEmpty(runJson) ? null : JsonUtility.FromJson<SavedRoute>(runJson);
            var progress = new int[game.BusinessCount];
            for (int i = 0; i < progress.Length; i++) progress[i] = game.BusinessProgress(i);
            int owned = 0, equipped = 0;
            IncidentSave incident = game.IncidentScene.Capture();
            for (int i = 0; i < game.DecorCount; i++)
            {
                if (game.DecorOwned(i)) owned |= 1 << i;
                if (game.DecorEquipped(i)) equipped |= 1 << i;
            }
            return new ExpectedState
            {
                position = game.PlayerTransform.position,
                cargo = game.Cargo,
                timeLeft = game.TimeLeft,
                hasPackage = game.HasPackage,
                coins = game.Coins,
                bestScore = game.BestScore,
                turkish = game.IsTurkish,
                volume = game.Volume,
                reducedMotion = game.ReducedMotion,
                completed = game.Completed,
                vehicleIndex = game.VehicleIndex,
                rentalVehicle = game.RentalVehicle,
                rentalRemaining = game.RentalRemaining,
                parcelDropped = game.IsParcelDropped,
                droppedParcelPosition = game.DroppedParcelPosition,
                hasTruck = game.StreetScene.HasTruck,
                hasWorksite = game.StreetScene.HasWorksite,
                worksiteIndex = game.StreetScene.WorksiteIndex,
                streetSeed = game.StreetScene.Seed,
                streetClock = game.StreetScene.Clock,
                incidentsEnabled = incident != null,
                incidentSeconds = incident?.seconds ?? 0,
                chillPercent = game.IsChilledOrder ? game.ChillPercent : 0,
                district = game.DistrictIndex, depotLevel = game.DepotLevel,
                droneOwned = game.DroneOwned,
                droneBusy = game.DroneBusy,
                droneProgress = game.DroneProgress,
                droneCompletedFlights = game.DroneCompletedFlights,
                businessProgress = progress, businessOrders = game.BusinessOrders,
                decorOwnedMask = owned, decorEquippedMask = equipped,
                savedRunJson = runJson, runVersion = saved == null ? 0 : saved.version,
                route = saved == null ? null : saved.route, lastMultiplier = saved == null ? 0 : saved.lastMultiplier
            };
        }

        IEnumerator EarnAndLaunchDrone()
        {
            game.StartRun();
            // Funding is a quiet-road fixture with ordinary movement and rewards.
            // The subsequent saved StartRun generates its normal random street tour.
            game.StreetScene.ConfigureFixed(false, false);
            Check(game.SetRouteForTest(0, 1, 2), "Isolated persistence funding uses the original central route through its guarded fixture");
            if (game.Phase == GamePhase.Tutorial) game.FinishTutorial();
            for (int delivery = 0; delivery < 3; delivery++)
            {
                if (delivery > 0)
                {
                    game.ContinueDelivery();
                    if (delivery == 2) yield return DriveForProgress(new Vector3(-7.2f, 0, 8));
                    yield return DriveForProgress(new Vector3(-7.2f, 0, -7.2f));
                    yield return DriveForProgress(new Vector3(-8, 0, -8));
                }
                float deadline = Time.realtimeSinceStartup + 4;
                while (!game.HasPackage)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new InvalidOperationException("Drone funding depot pickup timed out.");
                    yield return null;
                }
                int before = game.Coins;
                if (delivery == 0)
                {
                    yield return DriveForProgress(new Vector3(-7.2f, 0, -8));
                    yield return DriveForProgress(new Vector3(-7.2f, 0, 7), true);
                }
                else if (delivery == 1)
                {
                    yield return DriveForProgress(new Vector3(-7.2f, 0, -8));
                    yield return DriveForProgress(new Vector3(-7.2f, 0, 8));
                    yield return DriveForProgress(new Vector3(8, 0, 8), true);
                }
                else
                {
                    yield return DriveForProgress(new Vector3(-8, 0, -7.2f));
                    yield return DriveForProgress(new Vector3(8, 0, -7.2f), true);
                }
                Check(game.Phase == GamePhase.Result && game.Completed == delivery + 1
                    && game.Coins == before + game.LastPayout && game.LastBaseFee == 35,
                    "Actual fast-scooter delivery " + (delivery + 1) + " earns real drone-purchase funds");
            }
            Check(game.Coins >= game.DronePrice && !game.DroneOwned, "Three real deliveries can fund the optional persistence drone");
            game.OpenGarage();
            int wallet = game.Coins;
            game.BuyDrone();
            game.LaunchDrone();
            Check(game.DroneOwned && game.DroneBusy && game.Coins == wallet - game.DronePrice,
                "Optional persistence mission is purchased and launched through the ordinary garage");
            int orders = game.BusinessOrders;
            wallet = game.Coins;
            game.ClaimBusiness(0); game.ClaimBusiness(1);
            Check(game.BusinessOrders == orders + 2 && game.Coins == wallet + game.BusinessReward(0) + game.BusinessReward(1),
                "The three real clean funding deliveries unlock and consume two ordinary business rewards");
            game.CloseOverlay();

            game.StartRun();
            game.StreetScene.ConfigureFixed(false, false);
            Check(game.SetRouteForTest(0, 1, 2), "One additional real delivery uses the guarded central funding route");
            float decorationDeadline = Time.realtimeSinceStartup + 4;
            while (!game.HasPackage)
            {
                if (Time.realtimeSinceStartup > decorationDeadline) throw new InvalidOperationException("High-bit decoration funding pickup timed out.");
                yield return null;
            }
            wallet = game.Coins;
            yield return DriveForProgress(new Vector3(-7.2f, 0, -8));
            yield return DriveForProgress(new Vector3(-7.2f, 0, 7), true);
            Check(game.Phase == GamePhase.Result && game.Completed == 1 && game.Coins == wallet + game.LastPayout,
                "A fourth physical delivery earns the remaining expanded-decoration funds");
            game.OpenGarage(); wallet = game.Coins;
            Check(wallet >= game.DecorPrice(9) && !game.DecorOwned(9), "Actual delivery and business earnings can fund decoration bit nine");
            game.SelectDecor(9);
            Check(game.DecorOwned(9) && game.DecorEquipped(9) && game.Coins == wallet - game.DecorPrice(9),
                "The persistence probe purchases and equips the new high-bit decoration through the normal garage");
            game.CloseOverlay();
        }

        static bool SameInts(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        IEnumerator DriveForProgress(Vector3 target, bool allowDelivery = false)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (true)
            {
                Vector3 delta = target - game.PlayerTransform.position;
                delta.y = 0;
                if (delta.magnitude <= .17f || (allowDelivery && game.Phase == GamePhase.Result)) yield break;
                if (game.Phase != GamePhase.Playing || Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Drone funding route blocked: " + target + " at " + game.PlayerTransform.position);
                game.MoveWorld(delta.normalized * Mathf.Clamp(delta.magnitude * 2, .12f, 1));
                yield return null;
            }
        }

        IEnumerator CaptureScreenshot()
        {
            string path = Argument("-testScreenshot", "");
            if (string.IsNullOrEmpty(path)) yield break;
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { Destroy(image); }
            report.screenshot = path;
        }

        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Failed: " + label);
            report.checks.Add(label);
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
            // Clearing only the probe namespace is safe. LocalProgress must suppress
            // subsequent test writes, including CourierGame.OnApplicationQuit saves.
            if (readStage && LocalProgress.TestProfile)
            {
                try { LocalProgress.ClearTestProfile(); }
                catch (Exception exception) { report.errors.Add("Test profile cleanup failed: " + exception.Message); }
            }
            report.passed = report.errors.Count == 0;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not save persistence report: " + exception.Message);
                report.passed = false;
            }
            Debug.Log("KARGO_PERSISTENCE_" + (readStage ? "READ" : "WRITE") + " "
                + (report.passed ? "PASS" : "FAIL") + " checks=" + report.checks.Count + " output=" + outputPath);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}

