using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Opt-in shift loop checks. Requires -smokeTest -shiftTest; uses mock commerce and never saves.</summary>
    public sealed class CourierShiftTest : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public bool passed;
            public List<string> checks = new List<string>(), errors = new List<string>();
            public float wallSeconds;
        }
        readonly Report report = new Report();
        CourierGame game;
        float started;
        bool finished;

        void Start()
        {
            game = GetComponent<CourierGame>();
            Application.runInBackground = true;
            started = Time.realtimeSinceStartup;
            Application.logMessageReceived += Log;
            StartCoroutine(Run());
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
        void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(type + ": " + message); }
        void Update()
        { if (!finished && Time.realtimeSinceStartup - started > 120) { report.errors.Add("Shift test timed out."); Finish(); } }

        IEnumerator Run()
        {
            yield return null;
            game.StartRun();
            Check(game.InShift && game.Phase == GamePhase.ShiftCard && game.ShiftOpeningPick && game.ShiftOfferCount == 3,
                "A shift opens with a free power pick");
            yield return Shot("0-opening-pick");
            ShiftCard opening = game.ShiftOffer(1);
            game.ChooseShiftCard(1);
            Check(game.Phase == GamePhase.Playing && game.CardLevel(opening) == 1 && game.ShiftStars == 3 && game.ShiftLevel == 1 && !game.HasPackage,
                "After the pick the shift runs with three stars, level one and nothing in hand");
            Check(game.LiveBoardCountForTest == 2, "Two orders wait on the street at the start");
            yield return Shot("1a-board");

            game.ExpireBoardForTest();
            yield return WaitUntil(() => game.LiveBoardCountForTest == 0, 1);
            Check(game.LiveBoardCountForTest == 0 && game.ShiftStars == 3, "A missed waiting order is cancelled without costing a star");
            yield return WaitUntil(() => game.LiveBoardCountForTest > 0, 3);
            Check(game.LiveBoardCountForTest > 0, "The board refills after cancellations");

            yield return PickUp();
            Check(game.HasPackage, "Reaching a waiting order loads its parcel");
            yield return Shot("1b-carrying");
            int wallet = game.Coins;
            game.PlaceForTest(game.TargetPosition);
            yield return WaitUntil(() => game.ShiftDeliveries == 1 || game.Phase == GamePhase.ShiftCard, 3);
            Check(game.ShiftDeliveries == 1 && game.Coins > wallet && game.ShiftEarnings == game.Coins - wallet,
                "Delivery banks coins immediately and counts toward the shift");
            Check(game.Phase == GamePhase.ShiftCard && game.ShiftOfferCount == 3 && game.ShiftLevel == 2,
                "A fast, careful first delivery levels up and offers three cards");
            yield return Shot("2-cards");
            ShiftCard picked = game.ShiftOffer(0);
            int before = game.CardLevel(picked);
            game.ChooseShiftCard(0);
            Check(game.Phase == GamePhase.Playing && game.CardLevel(picked) == before + 1 && !game.HasPackage,
                "Choosing a card applies it and returns to the next order");

            // A bumpy delivery earns less XP, so play continues and the payout pop is visible.
            yield return PickUp();
            game.SetCargoForTest(60);
            wallet = game.Coins;
            game.PlaceForTest(game.TargetPosition);
            yield return WaitUntil(() => game.ShiftDeliveries == 2, 3);
            Check(game.ShiftDeliveries == 2 && game.Phase == GamePhase.Playing && game.Coins > wallet && game.ComboCount == 0,
                "A damaged delivery still pays but breaks the streak");
            yield return ShotNow("1c-payout");

            for (int star = 2; star >= 0; star--)
            {
                yield return PickUp();
                game.ExpireOrderForTest();
                yield return WaitUntil(() => game.ShiftStars == star, 2);
                Check(game.ShiftStars == star, "A late order costs one star (" + star + " left)");
            }
            Check(game.Phase == GamePhase.ShiftEnd && !game.ShiftSucceeded && !game.HasSavedRun, "Losing every star ends the shift without a resumable save");
            Check(game.CanReviveShift, "A first failure offers the rewarded revive");
            yield return Shot("3-shift-over");
            game.RequestShiftRevive();
            yield return WaitUntil(() => game.Phase == GamePhase.Playing, 2);
            Check(game.Phase == GamePhase.Playing && game.ShiftStars == 1 && game.InShift, "The revive ad resumes the shift with one star");

            yield return PickUp();
            game.ExpireOrderForTest();
            yield return WaitUntil(() => game.Phase == GamePhase.ShiftEnd, 2);
            Check(game.Phase == GamePhase.ShiftEnd && !game.CanReviveShift, "A shift can be revived only once");
            int earned = game.ShiftEarnings;
            wallet = game.Coins;
            Check(game.CanDoubleShift, "Shift earnings can be doubled with an ad");
            game.RequestShiftDouble();
            yield return null;
            Check(game.Coins == wallet + earned && !game.CanDoubleShift, "Doubling pays the shift earnings once");

            game.StartAnotherShift();
            yield return null;
            Check(game.InShift && game.Phase == GamePhase.ShiftCard && game.ShiftStars == 3 && game.ShiftDeliveries == 0, "A new shift starts fresh");
            game.ChooseShiftCard(0);
            wallet = game.Coins;
            game.SetShiftElapsedForTest(CourierGame.ShiftLength - .05f);
            yield return WaitUntil(() => game.Phase == GamePhase.ShiftEnd, 2);
            Check(game.Phase == GamePhase.ShiftEnd && game.ShiftSucceeded && game.ShiftBonus > 0 && game.Coins == wallet + game.ShiftBonus,
                "Surviving the clock completes the shift and pays the bonus");
            yield return Shot("4-shift-complete");
            Check(game.LastRankGain > 0 && (game.Rank > 1 || game.RankProgress > 0), "Every shift adds courier rank XP");
            game.FinishShift();
            Check(game.Phase == GamePhase.Menu && !game.InShift && !game.HasSavedRun, "Leaving returns to the menu with nothing to resume");

            // Lobby: free gift through a rewarded ad, then a store purchase made from the shop page.
            wallet = game.Coins;
            int gift = game.GiftAmount;
            Check(game.CanClaimGift, "The lobby gift is ready on a fresh profile");
            yield return Shot("5-lobby");
            game.ClaimGift();
            yield return null;
            Check(game.Coins == wallet + gift && !game.GiftReady && !game.CanClaimGift, "The gift ad pays once and starts its cooldown");
            game.Lobby.ShowPage(LobbyUI.Page.Shop);
            yield return Shot("6-shop");
            wallet = game.Coins;
            game.PurchaseProduct(CommerceConfiguration.CoinsSmall);
            yield return null;
            Check(game.Coins == wallet + 500, "A coin pack can be bought straight from the lobby shop");

            game.Lobby.ShowPage(LobbyUI.Page.Garage);
            yield return Shot("7-garage");
            wallet = game.Coins;
            game.SelectVehicle(2);
            Check(game.VehicleIndex == 2 && game.VehicleOwned(2) && game.Coins == wallet - game.VehiclePrice(2), "The lobby garage buys and equips the van");
            game.SelectVehicle(0);
            int depot = game.DepotLevel;
            game.UpgradeDepot();
            Check(game.DepotLevel == depot + 1, "The depot can be upgraded from the lobby");

            game.Lobby.ShowPage(LobbyUI.Page.City);
            yield return Shot("8-city");
            Check(!game.DistrictOwned(1) && game.ShiftDistrict == 0, "Only the starting district is open on a fresh profile");
            game.UnlockDistrict(1);
            Check(game.DistrictOwned(1) && game.ShiftDistrict == 1, "Opening a district with coins makes it the shift district");
            game.Lobby.ShowPage(LobbyUI.Page.Home);
            game.StartRun();
            Check(game.InShift && game.DistrictIndex == 1, "The next shift starts in the picked district");
            if (game.Phase == GamePhase.ShiftCard) game.ChooseShiftCard(0);
            yield return Shot("9-district-shift");
            Finish();
        }

        IEnumerator PickUp()
        {
            if (game.Phase != GamePhase.Playing) yield break;
            game.PlaceForTest(game.TargetPosition);
            yield return WaitUntil(() => game.HasPackage, 3);
        }

        // Optional: -shotDir <folder> saves screenshots of each shift screen.
        IEnumerator Shot(string name)
        {
            string dir = Arg("-shotDir", "");
            if (string.IsNullOrEmpty(dir)) yield break;
            for (int i = 0; i < 20; i++) yield return null;
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null;
        }

        IEnumerator ShotNow(string name)
        {
            string dir = Arg("-shotDir", "");
            if (string.IsNullOrEmpty(dir)) yield break;
            for (int i = 0; i < 12; i++) yield return null;
            Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null;
        }

        IEnumerator WaitUntil(Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        void Check(bool ok, string name)
        {
            if (ok) report.checks.Add(name); else report.errors.Add("FAILED: " + name);
        }

        void Finish()
        {
            if (finished) return; finished = true;
            report.wallSeconds = Time.realtimeSinceStartup - started;
            report.passed = report.errors.Count == 0;
            string path = Arg("-testOutput", Path.Combine(Application.persistentDataPath, "shift-test.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(Path.GetFullPath(path), JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }

        static string Arg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
    }
}
