using System;
using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        const string BusinessKey = "kk_business_v1";
        BusinessSave business = new BusinessSave();
        int[] route = { 0, 1, 2 };
        float dogMusicAt;
        int dogMusicBeat;
        static readonly int[] DogRiff = { 330, 440, 392, 523, 330, 659, 587, 440 };
        DogStreets dogs;

        [Serializable]
        sealed class BusinessSave
        {
            public int version = 1;
            public int deliveries, careful, outskirts, droneFlights, orders;
        }

        public int LastMultiplier { get; private set; } = 1;
        public int ActiveStopIndex => route[Mathf.Clamp(Completed, 0, 2)];
        public int DestinationCount => world.Stops.Length;
        public DogStreets DogScene => dogs;
        public int BusinessCount => 4;
        public int BusinessOrders => business.orders;
        public int BusinessReadyCount
        {
            get { int count = 0; for (int i = 0; i < BusinessCount; i++) if (BusinessProgress(i) >= BusinessTarget(i)) count++; return count; }
        }
        public void AddPlaytestCoins()
        {
            if (!IsPlaytest) return;
            Coins = Mathf.Min(999999, Coins + 50000);
            Notify(GameText.Pick("Deneme cüzdanına 50.000 jeton eklendi.", "Added 50,000 coins to the playtest wallet."));
            Save();
        }
        public string BusinessName(int i) => i == 0 ? GameText.Pick("Mahalle anlaşması", "Neighborhood contract") : i == 1 ? GameText.Pick("Nazik eller", "Gentle hands") : i == 2 ? GameText.Pick("Yeni sokaklar", "Beyond the block") : GameText.Pick("Hava kargo", "Air mail");
        public string BusinessHint(int i) => i == 0 ? GameText.Pick("3 paket teslim et.", "Deliver 3 parcels.") : i == 1 ? GameText.Pick("2 paketi en az %90 sağlam götür.", "Deliver 2 parcels with at least 90% condition.") : i == 2 ? GameText.Pick("Yeni mahalledeki adreslere 2 teslimat yap.", "Make 2 deliveries to the outer neighborhood.") : GameText.Pick("Drone ile 2 görev tamamla.", "Finish 2 drone flights.");
        public int BusinessTarget(int i) => i == 0 ? 3 : 2;
        public int BusinessReward(int i) => i == 0 ? 90 : i == 1 ? 80 : i == 2 ? 120 : 70;
        public int BusinessProgress(int i) => i == 0 ? business.deliveries : i == 1 ? business.careful : i == 2 ? business.outskirts : i == 3 ? business.droneFlights : 0;
        public bool CanClaimBusiness(int i) => Phase == GamePhase.Garage && i >= 0 && i < BusinessCount && BusinessProgress(i) >= BusinessTarget(i);

        public void ClaimBusiness(int i)
        {
            if (!CanClaimBusiness(i)) return;
            int target = BusinessTarget(i);
            if (i == 0) business.deliveries -= target;
            else if (i == 1) business.careful -= target;
            else if (i == 2) business.outskirts -= target;
            else business.droneFlights -= target;
            business.orders = Mathf.Min(1000000, business.orders + 1);
            Coins += BusinessReward(i);
            Notify(BusinessName(i) + GameText.Pick(" tamam! +", " complete! +") + BusinessReward(i) + GameText.Pick(" jeton", " coins"));
            PlayNote(1100);
            Save();
        }

        void RecordBusinessDelivery()
        {
            business.deliveries = Mathf.Min(1000, business.deliveries + 1);
            if (Cargo >= 90) business.careful = Mathf.Min(1000, business.careful + 1);
            if (ActiveStopIndex >= 3) business.outskirts = Mathf.Min(1000, business.outskirts + 1);
        }

        void LoadBusiness()
        {
            business = new BusinessSave();
            if (smokeTest) return;
            string json = LocalProgress.GetString(BusinessKey, "");
            if (string.IsNullOrEmpty(json) || json.Length > 1024) return;
            try
            {
                var candidate = JsonUtility.FromJson<BusinessSave>(json);
                if (candidate != null && candidate.version == 1 && candidate.deliveries >= 0 && candidate.deliveries <= 1000 &&
                    candidate.careful >= 0 && candidate.careful <= 1000 && candidate.outskirts >= 0 && candidate.outskirts <= 1000 &&
                    candidate.droneFlights >= 0 && candidate.droneFlights <= 1000 && candidate.orders >= 0 && candidate.orders <= 1000000) business = candidate;
            }
            catch (ArgumentException) { }
        }

        void SaveBusiness()
        {
            if (!smokeTest) LocalProgress.SetString(BusinessKey, JsonUtility.ToJson(business));
        }

        void BeginRoute()
        {
            route = new[] { 0, 1, 2 };
            if (smokeTest || world.Stops.Length < 6) return;
            var random = new System.Random(streetEvents.Seed ^ 0x315A);
            route[0] = random.Next(3);
            route[1] = 3 + random.Next(3);
            do { route[2] = random.Next(world.Stops.Length); } while (route[2] == route[0] || route[2] == route[1]);
        }

        void TickDogs(float dt)
        {
            if (!dogs || Phase != GamePhase.Playing || !streetEnabled) return;
            dogs.ScaredOfCourier = DinoScaresDogs;
            dogs.StandOffDistance = VehicleIndex == 3 ? 2.3f : VehicleIndex == 2 ? 2f : .95f;
            dogs.Tick(player.position, dt, ReducedMotion);
            if (!dogs.AnyChasing) { dogMusicBeat = 0; return; }
            if (Time.unscaledTime < dogMusicAt) return;
            dogMusicAt = Time.unscaledTime + .18f;
            PlayNote(DogRiff[dogMusicBeat++ % DogRiff.Length]);
        }

        public bool SetRouteForTest(int a, int b, int c)
        {
            if ((!smokeTest && !persistenceTest) || !ValidRoute(new[] { a, b, c })) return false;
            route = new[] { a, b, c };
            return true;
        }

        static bool ValidRoute(int[] value) => value != null && value.Length == 3 && value[0] >= 0 && value[0] < 6 &&
            value[1] >= 0 && value[1] < 6 && value[2] >= 0 && value[2] < 6 && value[0] != value[1] && value[0] != value[2] && value[1] != value[2];
    }
}
