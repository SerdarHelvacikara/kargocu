using System;
using UnityEngine;

namespace KurmaliKargo
{
    public enum ShiftCard { Turbo, Tape, Overtime, Magnet, Tips, DogTreat }

    /// <summary>
    /// Four-minute shift: orders keep arriving, three stars, a revive, and a
    /// choice of one of three power cards on every level-up. Shifts are never
    /// written to the run snapshot; coins are banked on each delivery.
    /// </summary>
    public sealed partial class CourierGame
    {
        public const float ShiftLength = 240, RushHourLength = 30;
        public const int ShiftStarCount = 3, CardMaxLevel = 3;
        static readonly int CardCount = Enum.GetValues(typeof(ShiftCard)).Length;

        bool shiftActive, shiftRevived, shiftSucceeded, rushAnnounced, shiftTest;
        float shiftElapsed;
        int shiftStars, shiftXp, shiftLevel, shiftDeliveries, shiftEarnings, shiftBestCombo, shiftBonus;
        int shiftPendingLevels, shiftRunId, shiftDoubledRun = -1, shiftLastStop = -1, shiftLastNode = -1;
        readonly int[] shiftCards = new int[CardCount];
        readonly ShiftCard[] shiftOffer = new ShiftCard[3];
        int shiftOfferCount;
        System.Random shiftRandom = new System.Random();

        bool ShiftModeEnabled => !persistenceTest && (!smokeTest || shiftTest);
        public bool InShift => shiftActive;
        public int ShiftStars => shiftStars;
        public float ShiftTimeLeft => Mathf.Max(0, ShiftLength - shiftElapsed);
        public bool IsRushHour => shiftActive && ShiftTimeLeft <= RushHourLength;
        public int ShiftLevel => shiftLevel;
        public int ShiftXpNeeded => 3 + 2 * (shiftLevel - 1);
        public float ShiftXp01 => Mathf.Clamp01(shiftXp / (float)ShiftXpNeeded);
        public int ShiftDeliveries => shiftDeliveries;
        public int ShiftEarnings => shiftEarnings;
        public int ShiftBonus => shiftBonus;
        public int ShiftBestCombo => shiftBestCombo;
        public bool ShiftSucceeded => shiftSucceeded;
        public int ShiftOfferCount => shiftOfferCount;
        public ShiftCard ShiftOffer(int i) => shiftOffer[Mathf.Clamp(i, 0, 2)];
        public int CardLevel(ShiftCard card) => shiftCards[(int)card];
        public bool CanReviveShift => Phase == GamePhase.ShiftEnd && shiftActive && !shiftSucceeded && !shiftRevived &&
            shiftDoubledRun != shiftRunId && AdsReady;
        public bool CanDoubleShift => Phase == GamePhase.ShiftEnd && shiftEarnings > 0 && shiftDoubledRun != shiftRunId && AdsReady;

        float ShiftSpeedScale => shiftActive ? 1 + .12f * shiftCards[(int)ShiftCard.Turbo] : 1;
        float ShiftDamageScale => shiftActive ? Mathf.Pow(.75f, shiftCards[(int)ShiftCard.Tape]) : 1;
        float ShiftCoinScale => shiftActive ? (1 + .2f * shiftCards[(int)ShiftCard.Tips]) * (IsRushHour ? 2 : 1) : 1;
        // Later orders get shorter; Overtime pushes back.
        float ShiftClockScale => shiftActive ? Mathf.Lerp(1, .72f, shiftElapsed / ShiftLength) * (1 + .15f * shiftCards[(int)ShiftCard.Overtime]) : 1;
        float ReachRadius => 1.28f * (shiftActive ? 1 + .4f * shiftCards[(int)ShiftCard.Magnet] : 1);
        bool ShiftDogTreat => shiftActive && shiftCards[(int)ShiftCard.DogTreat] > 0;

        public static string CardName(ShiftCard card)
        {
            switch (card)
            {
                case ShiftCard.Turbo: return GameText.Pick("Turbo", "Turbo");
                case ShiftCard.Tape: return GameText.Pick("Sağlam bant", "Strong tape");
                case ShiftCard.Overtime: return GameText.Pick("Fazla mesai", "Overtime");
                case ShiftCard.Magnet: return GameText.Pick("Mıknatıs", "Magnet");
                case ShiftCard.Tips: return GameText.Pick("Bahşiş kavanozu", "Tip jar");
                default: return GameText.Pick("Köpek maması", "Dog treats");
            }
        }

        public static string CardDescription(ShiftCard card)
        {
            switch (card)
            {
                case ShiftCard.Turbo: return GameText.Pick("Hız +%12", "Speed +12%");
                case ShiftCard.Tape: return GameText.Pick("Darbe hasarı −%25", "Impact damage −25%");
                case ShiftCard.Overtime: return GameText.Pick("Sipariş süresi +%15", "Order time +15%");
                case ShiftCard.Magnet: return GameText.Pick("Alma ve teslim alanı +%40", "Pickup and drop-off reach +40%");
                case ShiftCard.Tips: return GameText.Pick("Kazanç +%20", "Earnings +20%");
                default: return GameText.Pick("Köpekler kovalamaz, kaçar", "Dogs run away instead of chasing");
            }
        }

        public static string CardBadge(ShiftCard card)
        {
            switch (card)
            {
                case ShiftCard.Turbo: return GameText.Pick("%12", "12%") + "↑";
                case ShiftCard.Tape: return GameText.Pick("%25", "25%") + "↓";
                case ShiftCard.Overtime: return "+" + GameText.Pick("%15", "15%");
                case ShiftCard.Magnet: return "+" + GameText.Pick("%40", "40%");
                case ShiftCard.Tips: return "+" + GameText.Pick("%20", "20%");
                default: return GameText.Pick("KAÇ!", "SHOO!");
            }
        }

        public static Color CardColor(ShiftCard card)
        {
            switch (card)
            {
                case ShiftCard.Turbo: return new Color(.93f, .45f, .33f);
                case ShiftCard.Tape: return new Color(.36f, .55f, .82f);
                case ShiftCard.Overtime: return new Color(.55f, .42f, .78f);
                case ShiftCard.Magnet: return new Color(.86f, .33f, .47f);
                case ShiftCard.Tips: return new Color(.92f, .70f, .20f);
                default: return new Color(.30f, .66f, .52f);
            }
        }

        void StartShift()
        {
            shiftActive = false;
            StartTour();
            shiftActive = true;
            shiftRunId++;
            shiftRevived = shiftSucceeded = rushAnnounced = false;
            shiftElapsed = 0;
            shiftStars = ShiftStarCount;
            shiftXp = shiftDeliveries = shiftEarnings = shiftBestCombo = shiftBonus = shiftPendingLevels = shiftOfferCount = 0;
            shiftLevel = 1;
            shiftLastStop = shiftLastNode = -1;
            Array.Clear(shiftCards, 0, shiftCards.Length);
            shiftRandom = new System.Random(streetEvents.Seed ^ 0x51F7);
            savedRun = null;
            heavyIndex = -1;
            NextShiftOrder();
            Notify(GameText.Pick("Vardiya başladı! 4 dakika, 3 yıldız. Siparişleri yetiştir!",
                "Shift started! 4 minutes, 3 stars. Keep the orders coming!"));
        }

        void NextShiftOrder()
        {
            int stops = world.Stops.Length;
            int stop = shiftRandom.Next(stops);
            if (stop == shiftLastStop) stop = (stop + 1 + shiftRandom.Next(stops - 1)) % stops;
            route[0] = stop;
            int nodes = PickupNodes.Length, first = shiftRandom.Next(nodes), chosen = -1;
            for (int offset = 0; offset < nodes && chosen < 0; offset++)
            {
                int node = (first + offset) % nodes;
                if (node == shiftLastNode || Vector3.Distance(PickupNodes[node], world.Stops[stop]) < 6 || !PickupNodeSafe(node)) continue;
                chosen = node;
            }
            if (chosen < 0) chosen = 0;
            pickupPlan[0] = chosen;
            shiftLastStop = stop; shiftLastNode = chosen;
            order = new OrderState { plan = new int[3] };
            order.plan[0] = (int)RollShiftOrder();
            BeginOrderLeg();
            Completed = 0;
            HasPackage = false;
            package.gameObject.SetActive(false);
            ClearDroppedParcel();
            Cargo = 100;
            PrepareDeliveryClock();
            RefreshVipCustomer();
            pickupDelay = Mathf.Max(pickupDelay, .4f);
        }

        OrderStyle RollShiftOrder()
        {
            // The first minute stays simple; special orders ramp in afterwards.
            if (shiftElapsed < 45 && shiftDeliveries < 2) return OrderStyle.Standard;
            int roll = shiftRandom.Next(100);
            if (roll < 45) return OrderStyle.Standard;
            if (roll < 62) return OrderStyle.Urgent;
            if (roll < 75) return OrderStyle.Fragile;
            if (roll < 86) return OrderStyle.Chilled;
            if (roll < 95) return OrderStyle.Vip;
            return OrderStyle.Surprise;
        }

        void ShiftPickedUp()
        {
            ActiveParcel = (ParcelKind)shiftRandom.Next(3);
            parcelVisual.Configure(ActiveParcel);
            parcelVisual.SetOrder(CurrentOrder);
            PrepareDeliveryClock();
            Notify(ParcelName + " → " + TargetName + ". " + ParcelHint);
        }

        void TickShift(float dt)
        {
            if (!shiftActive || Phase != GamePhase.Playing || !Finite(dt) || dt <= 0) return;
            shiftElapsed += dt;
            if (!rushAnnounced && IsRushHour)
            {
                rushAnnounced = true;
                Notify(GameText.Pick("YOĞUN SAAT! Son 30 saniye, kazanç ×2!", "RUSH HOUR! Last 30 seconds, earnings ×2!"));
                PlayFeelPair(660, 990);
            }
            if ((HasPackage || IsParcelDropped) && TimeLeft <= 0) { FailShiftOrder(false); return; }
            if (HasPackage && Cargo <= .5f) { FailShiftOrder(true); return; }
            if (shiftElapsed >= ShiftLength) EndShift(true);
        }

        void FailShiftOrder(bool broken)
        {
            shiftStars = Mathf.Max(0, shiftStars - 1);
            comboCount = 0;
            PushFeelBubble(player, player.position + Vector3.up * 2.2f,
                broken ? "PAKET PARAMPARÇA!" : "GEÇ KALDIM!", broken ? "PARCEL SMASHED!" : "TOO LATE!", 1.8f, true);
            Notify(broken ? GameText.Pick("Paket kırıldı, bir yıldız gitti.", "The parcel broke. You lost a star.")
                : GameText.Pick("Sipariş gecikti, bir yıldız gitti.", "The order was late. You lost a star."));
            PlayFeelPair(240, 170);
            if (shiftStars <= 0) { EndShift(false); return; }
            NextShiftOrder();
            Save();
        }

        void CompleteShiftDelivery()
        {
            if (Phase != GamePhase.Playing || !HasPackage) return;
            LastSpeedBonus = Mathf.RoundToInt(25 * Mathf.Clamp01(TimeLeft / timeBudget));
            LastCareBonus = Mathf.RoundToInt(35 * Cargo / 100);
            LastBaseFee = DeliveryBaseFee + DistrictSurcharge + OrderFee(CurrentOrder) + OpenSurprise();
            LastMultiplier = DeliveryMultiplier;
            comboCount = Cargo >= 85 && TimeLeft > 0 ? Mathf.Min(9, comboCount + 1) : 0;
            shiftBestCombo = Mathf.Max(shiftBestCombo, comboCount);
            LastComboBonus = Mathf.Max(0, comboCount - 1) * 5;
            LastPayout = Mathf.RoundToInt(((LastBaseFee + LastSpeedBonus + LastCareBonus) * LastMultiplier + LastComboBonus) * ShiftCoinScale);
            Coins = Mathf.Min(999999, Coins + LastPayout);
            shiftEarnings += LastPayout;
            runScore += LastPayout;
            LastParcel = ActiveParcel;
            LastIntegrity = Cargo;
            customers[ActiveStopIndex].React(Cargo);
            DeliveryFeel(customers[ActiveStopIndex].transform, world.Stops[ActiveStopIndex], Cargo);
            TriggerDragonDelivery(world.Stops[ActiveStopIndex]);
            RecordBusinessDelivery();
            shiftDeliveries++;
            BestScore = Mathf.Max(BestScore, shiftEarnings);
            HasPackage = false;
            package.gameObject.SetActive(false);
            StartCoroutine(DeliveryJingle());

            shiftXp += 2 + (Cargo >= 90 ? 1 : 0) + (TimeLeft >= timeBudget * .5f ? 1 : 0);
            while (shiftXp >= ShiftXpNeeded)
            {
                shiftXp -= ShiftXpNeeded;
                shiftLevel++;
                shiftPendingLevels++;
            }
            NextShiftOrder();
            if (shiftPendingLevels > 0) OfferShiftCards();
            Save();
        }

        void OfferShiftCards()
        {
            shiftOfferCount = 0;
            int start = shiftRandom.Next(CardCount);
            // Shuffle by stepping with a stride coprime to the card count.
            int stride = 1 + shiftRandom.Next(CardCount - 1);
            while (Gcd(stride, CardCount) != 1) stride = stride % (CardCount - 1) + 1;
            for (int i = 0; i < CardCount && shiftOfferCount < 3; i++)
            {
                var card = (ShiftCard)((start + i * stride) % CardCount);
                if (shiftCards[(int)card] < CardMaxLevel) shiftOffer[shiftOfferCount++] = card;
            }
            if (shiftOfferCount == 0)
            {
                // Every card is maxed: the level-up pays out instead.
                shiftPendingLevels = 0;
                Coins = Mathf.Min(999999, Coins + 50);
                shiftEarnings += 50;
                Notify(GameText.Pick("Tüm güçler tamam! Seviye ödülü +50", "All powers maxed! Level bonus +50"));
                return;
            }
            pointerHeld = false; velocity = Vector3.zero;
            Phase = GamePhase.ShiftCard;
            PlayFeelPair(880, 1320);
        }

        static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return a; }

        public void ChooseShiftCard(int index)
        {
            if (Phase != GamePhase.ShiftCard || index < 0 || index >= shiftOfferCount) return;
            ShiftCard card = shiftOffer[index];
            shiftCards[(int)card] = Mathf.Min(CardMaxLevel, shiftCards[(int)card] + 1);
            shiftPendingLevels = Mathf.Max(0, shiftPendingLevels - 1);
            PlayNote(1100);
            if (shiftPendingLevels > 0) { OfferShiftCards(); return; }
            Phase = GamePhase.Playing;
            pickupDelay = Mathf.Max(pickupDelay, .3f);
            Notify(CardName(card) + " " + CardLevel(card) + ": " + CardDescription(card));
        }

        void EndShift(bool success)
        {
            shiftSucceeded = success;
            shiftBonus = 0;
            if (success)
            {
                shiftBonus = 50 + 10 * shiftDeliveries;
                Coins = Mathf.Min(999999, Coins + shiftBonus);
                shiftEarnings += shiftBonus;
                BestScore = Mathf.Max(BestScore, shiftEarnings);
                commerce.rounds++;
            }
            HasPackage = false;
            package.gameObject.SetActive(false);
            ClearDroppedParcel();
            pointerHeld = false; velocity = Vector3.zero;
            runActive = false;
            savedRun = null;
            Phase = GamePhase.ShiftEnd;
            if (success) StartCoroutine(DeliveryJingle()); else PlayFeelPair(330, 220);
            Save();
        }

        public void RequestShiftRevive() { if (CanReviveShift) RequestReward("revive", -1, 0); }
        public void RequestShiftDouble() { if (CanDoubleShift) RequestReward("shiftdouble", -1, shiftEarnings); }

        // Called from the rewarded-ad callback once the network confirms the reward.
        bool GrantShiftReward(string kind, int amount)
        {
            if (kind == "revive" && Phase == GamePhase.ShiftEnd && shiftActive && !shiftSucceeded && !shiftRevived)
            {
                shiftRevived = true;
                shiftStars = 1;
                runActive = true;
                NextShiftOrder();
                Phase = GamePhase.Playing;
                Notify(GameText.Pick("Devam! Son yıldızın seninle, dikkatli sür.", "Back on shift! One star left, ride carefully."));
                return true;
            }
            if (kind == "shiftdouble" && Phase == GamePhase.ShiftEnd && shiftDoubledRun != shiftRunId && amount > 0)
            {
                shiftDoubledRun = shiftRunId;
                Coins = Mathf.Min(999999, Coins + amount);
                shiftEarnings += amount;
                Notify(GameText.Pick("Kazanç ikiye katlandı! +", "Earnings doubled! +") + amount);
                return true;
            }
            return false;
        }

        public void FinishShift()
        {
            if (Phase != GamePhase.ShiftEnd) return;
            shiftActive = false;
            runActive = false;
            savedRun = null;
            ClearOverlays();
            Phase = GamePhase.Menu;
            Save();
        }

        public void StartAnotherShift()
        {
            if (Phase != GamePhase.ShiftEnd) return;
            shiftActive = false;
            if (!TryInterstitial(StartRun)) StartRun();
        }

        // Test seams (require -smokeTest -shiftTest).
        internal void SetShiftElapsedForTest(float seconds) { if (smokeTest && shiftTest) shiftElapsed = Mathf.Clamp(seconds, 0, ShiftLength); }
        internal void ExpireOrderForTest() { if (smokeTest && shiftTest && (HasPackage || IsParcelDropped)) TimeLeft = 0; }
        internal void GrantShiftXpForTest(int xp)
        {
            if (!smokeTest || !shiftTest || Phase != GamePhase.Playing) return;
            shiftXp += xp;
            while (shiftXp >= ShiftXpNeeded) { shiftXp -= ShiftXpNeeded; shiftLevel++; shiftPendingLevels++; }
            if (shiftPendingLevels > 0) OfferShiftCards();
        }
    }
}
