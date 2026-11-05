using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        static readonly Vector3[] StartingPickups = {
            new Vector3(-8,0,-8), new Vector3(0,0,-8), new Vector3(8,0,-3), new Vector3(-16,0,0),
            new Vector3(0,0,10), new Vector3(8,0,-16), new Vector3(16,0,-8), new Vector3(-16,0,8)
        };
        Vector3[] PickupNodes => world.Pickups ?? StartingPickups;
        int[] pickupPlan = { 0, 0, 0 };
        float timeBudget = 55, urgentBeat;
        int heavyIndex = -1, comboCount;
        readonly Collider[] pickupChecks = new Collider[32];
        public int LastComboBonus { get; private set; }
        public int ComboCount => comboCount;
        public float TimeBudget => timeBudget;
        public Vector3 PickupPosition => PickupNodes[pickupPlan[Mathf.Clamp(Completed, 0, 2)]];
        public int PickupCount => PickupNodes.Length;
        public bool IsHeavyDelivery => Completed == heavyIndex;
        public string PickupName
        {
            get
            {
                int node = pickupPlan[Mathf.Clamp(Completed, 0, 2)];
                if (DistrictIndex == 0) return PickupNameFor(node);
                return node == 0 ? DistrictName(DistrictIndex) + GameText.Pick(" deposu", " depot") :
                    GameText.Pick("Alış noktası ", "Pickup point ") + node;
            }
        }
        public string PickupNameFor(int i) => i == 0 ? GameText.Pick("Paket deposu", "Parcel depot") : i == 1 ? GameText.Pick("Kafe önü", "Cafe pickup") :
            i == 2 ? GameText.Pick("Manav durağı", "Greengrocer pickup") : i == 3 ? GameText.Pick("Park durağı", "Park pickup") :
            i == 4 ? GameText.Pick("Fırın önü", "Bakery pickup") : i == 5 ? GameText.Pick("Fabrika kapısı", "Factory gate") :
            i == 6 ? GameText.Pick("Doğu marketi", "East market") : GameText.Pick("Batı kargo noktası", "West parcel stand");
        public Vector3 PickupNodePosition(int i) => PickupNodes[Mathf.Clamp(i, 0, PickupNodes.Length - 1)];

        void BeginPickupPlan()
        {
            pickupPlan = new[] { 0, 0, 0 }; heavyIndex = -1; comboCount = LastComboBonus = 0;
            if (!smokeTest && !persistenceTest)
            {
                var random = new System.Random(streetEvents.Seed ^ 0x29D8);
                if (DistrictIndex == 0 && streetEvents.Seed % 3 == 0)
                    for (int i = 0; i < route.Length; i++) if (route[i] == 5) heavyIndex = i;
                int previous = -1;
                for (int delivery = 0; delivery < 3; delivery++)
                {
                    int start = random.Next(PickupNodes.Length), chosen = 0;
                    for (int offset = 0; offset < PickupNodes.Length; offset++)
                    {
                        int node = (start + offset) % PickupNodes.Length;
                        if (node == previous || Vector3.Distance(PickupNodes[node], world.Stops[route[delivery]]) < 4 || !PickupNodeSafe(node)) continue;
                        chosen = node; break;
                    }
                    pickupPlan[delivery] = chosen; previous = chosen;
                }
            }
            PrepareDeliveryClock();
        }

        public bool PickupNodeSafe(int index)
        {
            if (index < 0 || index >= PickupNodes.Length) return false;
            Vector3 p = PickupNodes[index];
            Physics.SyncTransforms();
            // Largest ground courier envelope, both street directions. This also
            // protects a pickup selected before the player changes vehicles.
            for (int turn = 0; turn < 2; turn++)
            {
                int count = Physics.OverlapBoxNonAlloc(p + Vector3.up * 1.42f, new Vector3(1.08f, 1.4f, 1.8f),
                    pickupChecks, Quaternion.Euler(0, turn * 90, 0), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (count == pickupChecks.Length) return false;
                for (int i = 0; i < count; i++)
                    if (pickupChecks[i] && pickupChecks[i] != controller && !pickupChecks[i].transform.IsChildOf(player)) return false;
            }
            return SafeRunPosition(p, 0);
        }

        void PrepareDeliveryClock()
        {
            Vector3 finish = world.Stops[ActiveStopIndex];
            float distance = CurrentOrder == OrderStyle.Chain
                ? Vector3.Distance(PickupPosition, world.Stops[ChainCheckpoint]) + Vector3.Distance(world.Stops[ChainCheckpoint], finish)
                : Vector3.Distance(PickupPosition, finish);
            float ordinary = smokeTest || persistenceTest ? 55 : Mathf.Clamp(22 + distance * 1.4f, 28, 75);
            timeBudget = Mathf.Max(28, ordinary * DistrictClockScale * (IsUrgentOrder ? .68f : 1));
            TimeLeft = timeBudget;
        }

        static bool ValidPickupPlan(int[] plan) => plan != null && plan.Length == 3 && plan[0] >= 0 && plan[0] < 8 && plan[1] >= 0 && plan[1] < 8 && plan[2] >= 0 && plan[2] < 8;
        public bool ConfigurePickupsForTest(int seed)
        {
            if (!smokeTest) return false;
            var random = new System.Random(seed);
            int previous = -1;
            for (int d = 0; d < 3; d++)
            {
                bool found = false;
                int first = random.Next(8);
                for (int n = 0; n < 8; n++)
                {
                    int candidate = (first + n) % 8;
                    if (candidate == previous || !PickupNodeSafe(candidate) || Vector3.Distance(PickupNodes[candidate], world.Stops[route[d]]) < 4) continue;
                    pickupPlan[d] = candidate; previous = candidate; found = true; break;
                }
                if (!found) return false;
            }
            PrepareDeliveryClock(); return true;
        }
        void TickUrgency(float dt)
        {
            if (Phase != GamePhase.Playing || !HasPackage || TimeLeft <= 0 || TimeLeft > 10) return;
            urgentBeat -= dt;
            if (urgentBeat > 0) return;
            urgentBeat = TimeLeft < 4 ? .28f : .55f;
            PlayNote(TimeLeft < 4 ? 230 : 300);
        }
    }
}
