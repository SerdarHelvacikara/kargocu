using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// Normal play keeps the existing preference names. Opt-in cold-start probes use
    /// a private namespace, so exercising real disk persistence cannot touch a player's progress.
    /// </summary>
    public static class LocalProgress
    {
        const string ProbePrefix = "__kk_saveprobe__";
        const string PlaytestPrefix = "__kk_playtest__";
        static string prefix = "";
        static bool testWritesDisabled;
        static readonly string[] KnownKeys =
        {
            "coins", "best", "colors", "paint", "mute", "language",
            "kk_volume", "kk_reducedMotion", "kk_tutorialSeen",
            "kk_decorOwned", "kk_decorEquipped", "kk_activeRun_v1", "kk_activeRun_v2", "kk_activeRun_v3", "kk_activeRun_v4", "kk_activeRun_v5", "kk_activeRun_v6", "kk_vehicle", "kk_van", "kk_drone_v1", "kk_monster_v1", "kk_business_v1", "kk_fantasy_vehicles_v1", "kk_commerce_v1", "kk_store_entitlements_v1"
            , "kk_activeRun_v7", "kk_districts_v1", "kk_depot_level_v1"
        };

        public static bool TestProfile { get; private set; }

        public static void Configure(bool persistenceTest, bool resetProfile)
        {
            string[] args = Environment.GetCommandLineArgs();
            bool writeProbe = Array.Exists(args, value => value == "-persistenceCheckWrite");
            bool readProbe = Array.Exists(args, value => value == "-persistenceCheckRead");
            TestProfile = persistenceTest && (writeProbe || readProbe);
            bool playtest = !writeProbe && !readProbe && Array.Exists(args, value => value == "-playtest") &&
                !Array.Exists(args, value => value == "-smokeTest");
            prefix = TestProfile ? ProbePrefix : playtest ? PlaytestPrefix : "";
            testWritesDisabled = false;
            // The second process must read exactly what the first process saved.
            if (TestProfile && writeProbe && resetProfile) ResetTestProfile();
        }

        public static int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(prefix + key, defaultValue);
        public static float GetFloat(string key, float defaultValue = 0) => PlayerPrefs.GetFloat(prefix + key, defaultValue);
        public static string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(prefix + key, defaultValue);
        public static bool HasKey(string key) => PlayerPrefs.HasKey(prefix + key);
        public static void SetInt(string key, int value) { if (CanWrite) PlayerPrefs.SetInt(prefix + key, value); }
        public static void SetFloat(string key, float value) { if (CanWrite) PlayerPrefs.SetFloat(prefix + key, value); }
        public static void SetString(string key, string value) { if (CanWrite) PlayerPrefs.SetString(prefix + key, value); }
        public static void DeleteKey(string key) { if (CanWrite) PlayerPrefs.DeleteKey(prefix + key); }
        public static void Save() { if (CanWrite) PlayerPrefs.Save(); }
        static bool CanWrite => !TestProfile || !testWritesDisabled;

        public static void ResetTestProfile()
        {
            if (!TestProfile) return;
            foreach (string key in KnownKeys) PlayerPrefs.DeleteKey(ProbePrefix + key);
            PlayerPrefs.Save();
        }

        public static void ClearTestProfile()
        {
            if (!TestProfile) return;
            ResetTestProfile();
            // Unity still invokes the game's normal shutdown callbacks after the
            // reader finishes. Keep their saves inside this now-closed sandbox.
            testWritesDisabled = true;
        }
    }
}
