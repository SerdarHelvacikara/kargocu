using System;
using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        const string DroneKey = "kk_drone_v1";
        const string VanKey = "kk_van";
        const float DroneDuration = 30;
        DroneVisual droneVisual;
        DroneSave drone = new DroneSave();

        [Serializable]
        sealed class DroneSave
        {
            public int version = 1;
            public bool owned, busy;
            public float elapsed;
            public int flights;
        }

        public int DronePrice => 250;
        public int DroneReward => 25;
        public bool DroneOwned => drone.owned;
        public bool DroneBusy => drone.busy;
        public float DroneProgress => drone.busy ? Mathf.Clamp01(drone.elapsed / DroneDuration) : 0;
        public int DroneCompletedFlights => drone.flights;
        public bool CanLaunchDrone => Phase == GamePhase.Garage && drone.owned && !drone.busy;
        public string DroneStatus => !drone.owned ? GameText.Pick("Küçük teslimat yardımcını aç.", "Unlock your little delivery helper.") : drone.busy ?
            GameText.Pick("Sen sürerken çalışıyor • ", "Working while you ride • ") + Mathf.CeilToInt((DroneDuration - drone.elapsed) / DepotDroneRate) + GameText.Pick(" sn kaldı", " s left") :
            GameText.Pick("Yeni uçuşa hazır • ", "Ready for a flight • ") + Mathf.CeilToInt(DroneDuration / DepotDroneRate) + GameText.Pick(" sn sürüşte +", " s riding earns ") + DroneReward + GameText.Pick(" jeton", " coins");

        void LoadCompanions()
        {
            vanOwned = !smokeTest && LocalProgress.GetInt(VanKey, 0) == 1;
            LoadMonsterTruckOwnership();
            LoadFantasyVehicleOwnership();
            drone = new DroneSave();
            if (smokeTest) return;
            string json = LocalProgress.GetString(DroneKey, "");
            if (string.IsNullOrEmpty(json) || json.Length > 1024) return;
            try
            {
                var saved = JsonUtility.FromJson<DroneSave>(json);
                if (saved != null && saved.version == 1 && saved.flights >= 0 && saved.flights <= 1000000 &&
                    InRange(saved.elapsed, 0, DroneDuration) && (!saved.busy || (saved.owned && saved.elapsed < DroneDuration)) &&
                    (saved.owned || (!saved.busy && saved.flights == 0)) && (saved.busy || saved.elapsed == 0)) drone = saved;
            }
            catch (ArgumentException) { }
        }

        void SaveCompanions()
        {
            if (smokeTest) return;
            LocalProgress.SetInt(VanKey, vanOwned ? 1 : 0);
            LocalProgress.SetString(DroneKey, JsonUtility.ToJson(drone));
            SaveMonsterTruckOwnership();
            SaveFantasyVehicleOwnership();
        }

        public void BuyDrone()
        {
            if (Phase != GamePhase.Garage || drone.owned) return;
            if (Coins < DronePrice) { Notify(GameText.Pick("Drone için birkaç teslimat daha yapmalısın.", "A few more deliveries will unlock the drone.")); return; }
            SpendCoins(DronePrice);
            drone.owned = true;
            Notify(GameText.Pick("Yeni yardımcın hazır! Garajdan uçuşa gönder.", "Your helper is ready! Launch a flight from the garage."));
            PlayNote(880);
            Save();
        }

        public void LaunchDrone()
        {
            if (!CanLaunchDrone) return;
            drone.busy = true;
            drone.elapsed = 0;
            Notify(GameText.Pick("Drone yola çıktı. Sen sürerken teslimatını tamamlayacak.", "Drone away! It will finish its delivery while you ride."));
            Save();
        }

        void TickDrone(float dt)
        {
            if (Phase == GamePhase.Playing && drone.busy && Finite(dt) && dt > 0)
            {
                drone.elapsed = Mathf.Min(DroneDuration, drone.elapsed + dt * DepotDroneRate);
                if (drone.elapsed >= DroneDuration)
                {
                    // Commit the completed flight before saving the wallet. Run
                    // snapshots never rewind this separate companion progress.
                    drone.busy = false;
                    drone.elapsed = 0;
                    drone.flights++;
                    business.droneFlights = Mathf.Min(1000, business.droneFlights + 1);
                    Coins += DroneReward;
                    Notify(GameText.Pick("Drone teslimatı tamamladı! +", "Drone delivery complete! +") + DroneReward + GameText.Pick(" jeton", " coins"));
                    PlayNote(1100);
                    Save();
                }
            }
            if (!droneVisual) return;
            droneVisual.Show(drone.owned);
            if (!drone.owned) return;
            Vector3 depot = world.Depot;
            Vector3 destination = world.Stops[drone.flights % world.Stops.Length];
            float p = DroneProgress;
            Vector3 position, heading;
            if (!drone.busy) { position = Phase == GamePhase.Garage ? garageVisual.transform.position + new Vector3(4.65f, 1.35f, -3.55f) : depot + new Vector3(.8f, .95f, -.3f); heading = Vector3.forward; }
            else if (p < .12f) { position = depot + Vector3.up * Mathf.Lerp(.95f, 4.3f, p / .12f); heading = destination - depot; }
            else if (p < .58f) { float t = (p - .12f) / .46f; position = Vector3.Lerp(depot, destination, t) + Vector3.up * (4.3f + Mathf.Sin(t * Mathf.PI) * .45f); heading = destination - depot; }
            else if (p < .7f) { position = destination + Vector3.up * Mathf.Lerp(4.3f, 3.7f, Mathf.Sin((p - .58f) / .12f * Mathf.PI)); heading = destination - depot; }
            else { float t = (p - .7f) / .3f; position = Vector3.Lerp(destination, depot, t) + Vector3.up * Mathf.Lerp(4.3f, .95f, t * t); heading = depot - destination; }
            droneVisual.Animate(position, heading, drone.busy ? p : 0, drone.busy && p < .64f, ReducedMotion || Phase != GamePhase.Playing);
        }

        public void AdvanceDroneForTest(float dt) { if (smokeTest) TickDrone(dt); }
    }
}
