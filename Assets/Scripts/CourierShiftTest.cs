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
            Check(game.InShift && game.Phase == GamePhase.Playing && game.ShiftStars == 3 && game.ShiftLevel == 1 && !game.HasPackage,
                "Start begins a shift with three stars, level one and an order to pick up");

            yield return PickUp();
            Check(game.HasPackage, "Reaching the pickup loads the parcel");
            yield return Shot("1-playing");
            int wallet = game.Coins;
            game.PlaceForTest(game.TargetPosition);
            yield return WaitUntil(() => game.ShiftDeliveries == 1 || game.Phase == GamePhase.ShiftCard, 3);
            Check(game.ShiftDeliveries == 1 && game.Coins > wallet && game.ShiftEarnings == game.Coins - wallet,
                "Delivery banks coins immediately and counts toward the shift");
            Check(game.Phase == GamePhase.ShiftCard && game.ShiftOfferCount == 3 && game.ShiftLevel == 2,
                "A fast, careful first delivery levels up and offers three cards");
            yield return Shot("2-cards");
            ShiftCard picked = game.ShiftOffer(0);
            game.ChooseShiftCard(0);
            Check(game.Phase == GamePhase.Playing && game.CardLevel(picked) == 1 && !game.HasPackage,
                "Choosing a card applies it and returns to the next order");

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
            Check(game.InShift && game.Phase == GamePhase.Playing && game.ShiftStars == 3 && game.ShiftDeliveries == 0, "A new shift starts fresh");
            wallet = game.Coins;
            game.SetShiftElapsedForTest(CourierGame.ShiftLength - .05f);
            yield return WaitUntil(() => game.Phase == GamePhase.ShiftEnd, 2);
            Check(game.Phase == GamePhase.ShiftEnd && game.ShiftSucceeded && game.ShiftBonus > 0 && game.Coins == wallet + game.ShiftBonus,
                "Surviving the clock completes the shift and pays the bonus");
            yield return Shot("4-shift-complete");
            game.FinishShift();
            Check(game.Phase == GamePhase.Menu && !game.InShift && !game.HasSavedRun, "Leaving returns to the menu with nothing to resume");
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
