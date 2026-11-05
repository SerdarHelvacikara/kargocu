using System;
using UnityEngine;

namespace KurmaliKargo
{
    public enum ShiftCard { Turbo, Tape, Overtime, Magnet, Tips, DogTreat }

    /// <summary>
    /// Four-minute shift. Two or three orders wait on the street at once, each
    /// with its own countdown; the courier picks which to take. Late or broken
    /// parcels cost one of three stars, losing every star offers one revive,
    /// and every level-up pauses on a pick-one-of-three power card. Shifts are
    /// never written to the run snapshot; coins are banked on each delivery.
    /// </summary>
    public sealed partial class CourierGame
    {
        public const float ShiftLength = 240, RushHourLength = 30;
        public const int ShiftStarCount = 3, CardMaxLevel = 3, BoardSize = 3;
        static readonly int CardCount = Enum.GetValues(typeof(ShiftCard)).Length;

        sealed class BoardOrder
        {
            public bool live;
            public int node, stop;
            public OrderStyle style;
            public float left, life;
        }

        bool shiftActive, shiftRevived, shiftSucceeded, rushAnnounced, shiftTest;
        float shiftElapsed, boardSpawnIn;
        int shiftStars, shiftXp, shiftLevel, shiftDeliveries, shiftEarnings, shiftBestCombo, shiftBonus;
        int shiftPendingLevels, shiftRunId, shiftDoubledRun = -1;
        readonly int[] shiftCards = new int[CardCount];
        readonly ShiftCard[] shiftOffer = new ShiftCard[3];
        readonly BoardOrder[] board = { new BoardOrder(), new BoardOrder(), new BoardOrder() };
        Transform[] boardRings;
        Material[] boardPaint;
        int shiftOfferCount;
        System.Random shiftRandom = new System.Random();

        /// <summary>World position and amount of every shift payout, for HUD coin bursts.</summary>
        public event Action<Vector3, int> ShiftPaid;

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
        public bool ShiftOpeningPick => shiftDeliveries == 0 && shiftLevel == 1;
        public int ShiftOfferCount => shiftOfferCount;
        public ShiftCard ShiftOffer(int i) => shiftOffer[Mathf.Clamp(i, 0, 2)];
        public int CardLevel(ShiftCard card) => shiftCards[(int)card];
        public bool CanReviveShift => Phase == GamePhase.ShiftEnd && shiftActive && !shiftSucceeded && !shiftRevived &&
            shiftDoubledRun != shiftRunId && AdsReady;
        public bool CanDoubleShift => Phase == GamePhase.ShiftEnd && shiftEarnings > 0 && shiftDoubledRun != shiftRunId && AdsReady;

        public bool BoardLive(int i) => shiftActive && board[i].live;
        public Vector3 BoardPosition(int i) => PickupNodes[board[i].node];
        public float BoardLeft(int i) => board[i].left;
        public float BoardLeft01(int i) => board[i].life > 0 ? Mathf.Clamp01(board[i].left / board[i].life) : 0;
        public string BoardLabel(int i) => ShortOrderName(board[i].style);
        public Color BoardColor(int i) => OrderColor(board[i].style);
        public string CarriedLabel => ShortOrderName(CurrentOrder);
        public Color CarriedColor => OrderColor(CurrentOrder);

        float ShiftSpeedScale => shiftActive ? 1 + .12f * shiftCards[(int)ShiftCard.Turbo] : 1;
        float ShiftDamageScale => shiftActive ? Mathf.Pow(.75f, shiftCards[(int)ShiftCard.Tape]) : 1;
        float ShiftCoinScale => shiftActive ? (1 + .2f * shiftCards[(int)ShiftCard.Tips]) * (IsRushHour ? 2 : 1) : 1;
        float ShiftOvertime => 1 + .15f * shiftCards[(int)ShiftCard.Overtime];
        // Later orders get shorter; Overtime pushes back.
        float ShiftClockScale => shiftActive ? Mathf.Lerp(1, .72f, shiftElapsed / ShiftLength) * ShiftOvertime : 1;
        float ReachRadius => 1.28f * (shiftActive ? 1 + .4f * shiftCards[(int)ShiftCard.Magnet] : 1);
        bool ShiftDogTreat => shiftActive && shiftCards[(int)ShiftCard.DogTreat] > 0;
        int BoardTarget => shiftElapsed < 50 ? 2 : 3;

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
                case ShiftCard.Overtime: return GameText.Pick("Sipariş süreleri +%15", "Order timers +15%");
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

        static string ShortOrderName(OrderStyle style)
        {
            switch (style)
            {
                case OrderStyle.Urgent: return GameText.Pick("ACİL", "RUSH");
                case OrderStyle.Fragile: return GameText.Pick("CAM", "GLASS");
                case OrderStyle.Chilled: return GameText.Pick("DONDURMA", "ICE CREAM");
                case OrderStyle.Vip: return "VIP";
                case OrderStyle.Surprise: return GameText.Pick("SÜRPRİZ", "SURPRISE");
                default: return GameText.Pick("PAKET", "PARCEL");
            }
        }

        static Color OrderColor(OrderStyle style)
        {
            switch (style)
            {
                case OrderStyle.Urgent: return new Color(.91f, .42f, .33f);
                case OrderStyle.Fragile: return new Color(.38f, .57f, .84f);
                case OrderStyle.Chilled: return new Color(.33f, .73f, .84f);
                case OrderStyle.Vip: return new Color(.93f, .70f, .18f);
                case OrderStyle.Surprise: return new Color(.62f, .45f, .80f);
                default: return new Color(.07f, .62f, .60f);
            }
        }

        void StartShift()
        {
            shiftActive = false;
            StartTour();
            // The old three-page tour tutorial does not describe shifts; the
            // opening card pick and the order labels teach the loop instead.
            if (Phase == GamePhase.Tutorial) { ClearOverlays(); Phase = GamePhase.Playing; }
            tutorialSeen = true;
            shiftActive = true;
            shiftRunId++;
            shiftRevived = shiftSucceeded = rushAnnounced = false;
            shiftElapsed = 0;
            shiftStars = ShiftStarCount;
            shiftXp = shiftDeliveries = shiftEarnings = shiftBestCombo = shiftBonus = shiftPendingLevels = shiftOfferCount = 0;
            shiftLevel = 1;
            shiftRankAwarded = 0;
            Array.Clear(shiftCards, 0, shiftCards.Length);
            shiftRandom = new System.Random(streetEvents.Seed ^ 0x51F7);
            savedRun = null;
            heavyIndex = -1;
            EnsureBoardVisuals();
            foreach (BoardOrder slot in board) slot.live = false;
            ResetCarry(); // spawns the first waiting order
            SpawnBoardOrder();
            Notify(GameText.Pick("Vardiya başladı! Siparişler sokakta seni bekliyor.", "Shift started! Orders are waiting on the street."));
            // The shift opens with a free power pick.
            shiftPendingLevels = 1;
            OfferShiftCards();
        }

        void ResetCarry()
        {
            Completed = 0;
            HasPackage = false;
            package.gameObject.SetActive(false);
            ClearDroppedParcel();
            Cargo = 100;
            pickupDelay = Mathf.Max(pickupDelay, .4f);
            if (LiveBoardCount() == 0) SpawnBoardOrder();
        }

        int LiveBoardCount()
        {
            int count = 0;
            foreach (BoardOrder slot in board) if (slot.live) count++;
            return count;
        }

        bool SpawnBoardOrder()
        {
            BoardOrder slot = Array.Find(board, s => !s.live);
            if (slot == null) return false;
            int nodes = PickupNodes.Length, first = shiftRandom.Next(nodes), node = -1;
            for (int offset = 0; offset < nodes && node < 0; offset++)
            {
                int candidate = (first + offset) % nodes;
                bool taken = (HasPackage || IsParcelDropped) && candidate == pickupPlan[0];
                foreach (BoardOrder other in board) if (other.live && other.node == candidate) taken = true;
                if (!taken && Vector3.Distance(PickupNodes[candidate], player.position) > 3 && PickupNodeSafe(candidate)) node = candidate;
            }
            if (node < 0) return false;
            int stops = world.Stops.Length, stop = shiftRandom.Next(stops);
            for (int i = 0; i < stops && Vector3.Distance(PickupNodes[node], world.Stops[stop]) < 6; i++) stop = (stop + 1) % stops;
            slot.node = node; slot.stop = stop;
            slot.style = RollShiftOrder();
            slot.life = slot.left = Mathf.Lerp(32, 22, shiftElapsed / ShiftLength) * ShiftOvertime;
            slot.live = true;
            return true;
        }

        OrderStyle RollShiftOrder()
        {
            // The first minute stays simple; special orders ramp in afterwards.
            if (shiftElapsed < 40 && shiftDeliveries < 2) return OrderStyle.Standard;
            int roll = shiftRandom.Next(100);
            if (roll < 42) return OrderStyle.Standard;
            if (roll < 60) return OrderStyle.Urgent;
            if (roll < 73) return OrderStyle.Fragile;
            if (roll < 84) return OrderStyle.Chilled;
            if (roll < 94) return OrderStyle.Vip;
            return OrderStyle.Surprise;
        }

        // While empty-handed the marker and the proximity check follow the
        // nearest waiting order.
        Vector3 ShiftPickupTarget
        {
            get
            {
                int best = NearestBoardOrder();
                return best >= 0 ? PickupNodes[board[best].node] : world.Depot;
            }
        }

        int NearestBoardOrder()
        {
            int best = -1; float bestDistance = float.MaxValue;
            for (int i = 0; i < board.Length; i++)
            {
                if (!board[i].live) continue;
                float d = Vector3.Distance(Flat(player.position), Flat(PickupNodes[board[i].node]));
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }

        bool ClaimNearestShiftOrder()
        {
            int i = NearestBoardOrder();
            if (i < 0 || Vector3.Distance(Flat(player.position), Flat(PickupNodes[board[i].node])) >= ReachRadius) return false;
            BoardOrder slot = board[i];
            route[0] = slot.stop;
            pickupPlan[0] = slot.node;
            order = new OrderState { plan = new int[3] };
            order.plan[0] = (int)slot.style;
            BeginOrderLeg();
            RefreshVipCustomer();
            Completed = 0;
            Cargo = 100;
            slot.live = false;
            boardSpawnIn = Mathf.Min(boardSpawnIn <= 0 ? 1.5f : boardSpawnIn, 1.5f);
            return true;
        }

        void ShiftPickedUp()
        {
            ActiveParcel = (ParcelKind)shiftRandom.Next(3);
            parcelVisual.Configure(ActiveParcel);
            parcelVisual.SetOrder(CurrentOrder);
            PrepareDeliveryClock();
            Notify(ParcelName + " → " + TargetName);
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
            TickBoard(dt);
            if ((HasPackage || IsParcelDropped) && TimeLeft <= 0) { FailShiftOrder(false); return; }
            if (HasPackage && Cargo <= .5f) { FailShiftOrder(true); return; }
            if (shiftElapsed >= ShiftLength) EndShift(true);
        }

        void TickBoard(float dt)
        {
            for (int i = 0; i < board.Length; i++)
            {
                BoardOrder slot = board[i];
                if (!slot.live) continue;
                slot.left -= dt;
                if (slot.left > 0) continue;
                // A missed order only costs the streak; stars are for parcels in hand.
                slot.live = false;
                comboCount = 0;
                PushFeelBubble(null, PickupNodes[slot.node] + Vector3.up * 1.8f, "İPTAL!", "CANCELLED!", 1.4f, true);
                boardSpawnIn = Mathf.Min(boardSpawnIn <= 0 ? 1 : boardSpawnIn, 1);
            }
            boardSpawnIn -= dt;
            if (boardSpawnIn <= 0 && LiveBoardCount() < BoardTarget)
            {
                SpawnBoardOrder();
                boardSpawnIn = Mathf.Lerp(3.5f, 1.8f, shiftElapsed / ShiftLength);
            }
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
            if (!ReducedMotion) feelShakeLeft = Mathf.Max(feelShakeLeft, .3f);
            if (shiftStars <= 0) { EndShift(false); return; }
            ResetCarry();
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
            Vector3 destination = world.Stops[ActiveStopIndex];
            customers[ActiveStopIndex].React(Cargo);
            DeliveryFeel(customers[ActiveStopIndex].transform, destination, Cargo);
            TriggerDragonDelivery(destination);
            RecordBusinessDelivery();
            shiftDeliveries++;
            BestScore = Mathf.Max(BestScore, shiftEarnings);
            ShiftPaid?.Invoke(destination + Vector3.up * 4.2f, LastPayout);
            StartCoroutine(DeliveryJingle());

            shiftXp += 2 + (Cargo >= 90 ? 1 : 0) + (TimeLeft >= timeBudget * .5f ? 1 : 0);
            while (shiftXp >= ShiftXpNeeded)
            {
                shiftXp -= ShiftXpNeeded;
                shiftLevel++;
                shiftPendingLevels++;
            }
            ResetCarry();
            if (shiftPendingLevels > 0) OfferShiftCards();
            Save();
        }

        void OfferShiftCards()
        {
            shiftOfferCount = 0;
            int start = shiftRandom.Next(CardCount);
            // Step through the cards with a stride coprime to the card count.
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
            AwardShiftRank(success);
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
                ResetCarry();
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
            NoticeTime = 0; // shift notices do not follow into the lobby
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

        void EnsureBoardVisuals()
        {
            if (boardRings != null) return;
            boardRings = new Transform[BoardSize];
            boardPaint = new Material[BoardSize];
            for (int s = 0; s < BoardSize; s++)
            {
                boardPaint[s] = new Material(Paint(Color.white));
                Transform ring = new GameObject("Waiting order " + (s + 1)).transform;
                for (int i = 0; i < 18; i++)
                {
                    float angle = i * Mathf.PI * 2 / 18;
                    Transform piece = Part(ring, "Order ring", PrimitiveType.Cube, new Vector3(Mathf.Cos(angle) * .95f, .12f, Mathf.Sin(angle) * .95f),
                        new Vector3(.26f, .05f, .12f), boardPaint[s]);
                    piece.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
                }
                Transform box = Part(ring, "Waiting parcel", PrimitiveType.Cube, new Vector3(0, 1.9f, 0), Vector3.one * .46f, boardPaint[s]);
                Part(box, "Parcel tape", PrimitiveType.Cube, new Vector3(0, .52f, 0), new Vector3(.24f, .04f, 1.02f), Paint(Color.white));
                ring.gameObject.SetActive(false);
                boardRings[s] = ring;
            }
        }

        void TickBoardVisuals()
        {
            if (boardRings == null) return;
            bool show = shiftActive && Phase == GamePhase.Playing;
            for (int s = 0; s < BoardSize; s++)
            {
                bool live = show && board[s].live;
                if (boardRings[s].gameObject.activeSelf != live) boardRings[s].gameObject.SetActive(live);
                if (!live) continue;
                boardRings[s].position = PickupNodes[board[s].node];
                // The ring shrinks as the order's patience runs out.
                float scale = Mathf.Lerp(.5f, 1, BoardLeft01(s));
                boardRings[s].localScale = new Vector3(scale, 1, scale);
                Transform box = boardRings[s].GetChild(boardRings[s].childCount - 1);
                float t = Time.time * 2 + s;
                box.localPosition = new Vector3(0, 1.9f + (ReducedMotion ? 0 : Mathf.Sin(t) * .14f), 0);
                box.localRotation = Quaternion.Euler(0, ReducedMotion ? 0 : Time.time * 50 + s * 40, 0);
                Color c = OrderColor(board[s].style);
                bool urgent = board[s].left < 6 && !ReducedMotion && Mathf.Repeat(Time.time, .5f) < .25f;
                boardPaint[s].color = urgent ? Color.white : (HasPackage || IsParcelDropped ? Color.Lerp(c, Color.white, .45f) : c);
            }
        }

        // Test seams (require -smokeTest -shiftTest).
        internal void SetShiftElapsedForTest(float seconds) { if (smokeTest && shiftTest) shiftElapsed = Mathf.Clamp(seconds, 0, ShiftLength); }
        internal void ExpireOrderForTest() { if (smokeTest && shiftTest && (HasPackage || IsParcelDropped)) TimeLeft = 0; }
        internal void ExpireBoardForTest() { if (smokeTest && shiftTest) foreach (BoardOrder slot in board) if (slot.live) slot.left = .01f; }
        internal int LiveBoardCountForTest => LiveBoardCount();
        internal void SetCargoForTest(float value) { if (smokeTest && shiftTest && HasPackage) Cargo = Mathf.Clamp(value, 1, 100); }
    }
}
