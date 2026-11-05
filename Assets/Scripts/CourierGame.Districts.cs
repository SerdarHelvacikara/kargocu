using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        const string DistrictKey = "kk_districts_v1";
        public const int DistrictCount = 4;
        static readonly int[] DistrictPrices = { 0, 450, 900, 1600 };
        static readonly int[] DistrictFees = { 0, 12, 24, 42 };
        static readonly float[] DistrictClockScales = { 1, .97f, .90f, .81f };
        readonly WorldData[] districts = new WorldData[DistrictCount];
        int districtMask = 1;
        public int DistrictIndex { get; private set; }
        internal int DistrictSurcharge => DistrictFees[DistrictIndex];
        internal float DistrictClockScale => DistrictClockScales[DistrictIndex];

        public static string DistrictName(int index) => index == 1 ? GameText.Pick("Çarşı", "Covered Market") :
            index == 2 ? GameText.Pick("Çin Mahallesi", "Chinatown") : index == 3 ? GameText.Pick("Plaza", "Plaza District") :
            GameText.Pick("Başlangıç Mahallesi", "Starting Neighborhood");

        public static string DistrictHint(int index) => index == 1 ? GameText.Pick("Renkli tenteler ve baharat tezgâhları.", "Striped awnings and spice stalls.") :
            index == 2 ? GameText.Pick("Kırmızı fenerler, çay evi ve bambu avlusu.", "Red lanterns, tea house and bamboo courtyard.") :
            index == 3 ? GameText.Pick("Cam kuleler, kısa süre ve yüksek ücret.", "Glass towers, tighter deadlines and higher fees.") :
            GameText.Pick("Geniş caddeler, halı saha ve fabrika.", "Wide streets, a football pitch and the factory.");

        public int DistrictPrice(int index) => index >= 0 && index < DistrictCount ? DistrictPrices[index] : 0;
        public bool DistrictOwned(int index) => index >= 0 && index < DistrictCount && (districtMask & (1 << index)) != 0;
        public int DistrictFee(int index) => index >= 0 && index < DistrictCount ? DistrictFees[index] : 0;
        internal static bool ValidDistrict(int index) => index >= 0 && index < DistrictCount;
        internal Vector3 DistrictStop(int index) => world.Stops[index];

        public void UnlockDistrict(int index)
        {
            if ((Phase != GamePhase.Garage && Phase != GamePhase.Menu) || !ValidDistrict(index) || DistrictOwned(index) || Coins < DistrictPrice(index)) return;
            SpendCoins(DistrictPrice(index));
            districtMask |= 1 << index;
            districtPick = index;
            Notify(DistrictName(index) + GameText.Pick(" açıldı!", " unlocked!"));
            PlayFeelPair(660, 990);
            Save();
        }

        public void StartDistrict(int index)
        {
            if (Phase != GamePhase.Garage || !DistrictOwned(index)) return;
            SwitchDistrict(index);
            StartRun();
            if (Phase == GamePhase.Playing && !ReducedMotion)
                SpawnFeelParticles(player.position + Vector3.up, false, 18);
        }

        void SwitchDistrict(int index)
        {
            if (DistrictIndex == index) return;
            world.Root.gameObject.SetActive(false);
            if (world.City) world.City.gameObject.SetActive(false);
            DistrictIndex = index;
            world = districts[index] ?? (districts[index] = WorldBuilder.BuildDistrict(index));
            world.Root.gameObject.SetActive(true);
            if (world.City) world.City.gameObject.SetActive(true);
            for (int i = 0; i < customers.Length; i++)
                customers[i].transform.position = world.Stops[i] + new Vector3(-.7f, 0, 1.6f);
            Physics.SyncTransforms();
        }

        void ConfigureDistrictDogs(int seed)
        {
            if (!dogs) return;
            if (DistrictIndex == 0) dogs.Configure(seed);
            else dogs.ConfigureFixed(0);
        }

        // The city grows with play: each district opens by itself at a courier
        // rank. Coins can still open one early.
        static readonly int[] DistrictRanks = { 1, 3, 5, 8 };
        int districtPick;
        public int DistrictRank(int index) => ValidDistrict(index) ? DistrictRanks[index] : 1;
        public int ShiftDistrict => DistrictOwned(districtPick) ? districtPick : 0;

        public void PickShiftDistrict(int index)
        {
            if (Phase != GamePhase.Menu || !DistrictOwned(index) || districtPick == index) return;
            districtPick = index;
            Notify(GameText.Pick("Vardiyaların artık burada: ", "Your shifts are now in: ") + DistrictName(index));
            PlayNote(880);
            Save();
        }

        void UnlockDistrictsByRank()
        {
            for (int i = 1; i < DistrictCount; i++)
            {
                if (DistrictOwned(i) || Rank < DistrictRanks[i]) continue;
                districtMask |= 1 << i;
                districtPick = i;
                Notify(GameText.Pick("YENİ BÖLGE AÇILDI: ", "NEW DISTRICT OPEN: ") + DistrictName(i) +
                    GameText.Pick("! Vardiyaların artık orada.", "! Your shifts move there."));
            }
        }

        internal bool DistrictStopSafeForTest(int index) => smokeTest && SafeRunPosition(world.Stops[index], 0) && SafeRunPosition(world.Stops[index], 90);
    }
}
