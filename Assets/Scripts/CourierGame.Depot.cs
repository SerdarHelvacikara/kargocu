using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        const string DepotLevelKey = "kk_depot_level_v1";
        public const int MaxDepotLevel = 10;
        public int DepotLevel { get; private set; } = 1;
        public int DepotPrice => DepotLevel < MaxDepotLevel ? 100 + 90 * DepotLevel : 0;
        internal float LoadingDelay => .90f - (DepotLevel - 1) * .045f;
        internal float DepotImpactScale => 1 - (DepotLevel - 1) * .025f;
        internal float DepotDroneRate => 1 + (DepotLevel - 1) * .035f;

        public void UpgradeDepot()
        {
            if ((Phase != GamePhase.Garage && Phase != GamePhase.Menu) || DepotLevel >= MaxDepotLevel || Coins < DepotPrice) return;
            SpendCoins(DepotPrice);
            DepotLevel++;
            RefreshGarage();
            Notify(GameText.Pick("Depo yükseltildi! Seviye ", "Depot upgraded! Level ") + DepotLevel);
            PlayFeelPair(660, 990);
            Save();
        }
    }
}
