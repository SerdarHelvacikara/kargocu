using System;
using UnityEngine;

namespace KurmaliKargo
{
    internal enum OrderStyle { Standard, Urgent, Fragile, Chilled, Chain, Vip, Surprise }

    public sealed partial class CourierGame
    {
        [Serializable]
        sealed class OrderState
        {
            public int[] plan;
            public float chillSeconds = 38;
            public bool checkpoint;
        }

        OrderState order = new OrderState();
        OrderStyle CurrentOrder => OrderAt(order, Completed);
        OrderStyle LastOrder => OrderAt(order, Completed - 1);
        internal bool AwaitingOrderCheckpoint => CurrentOrder == OrderStyle.Chain && !order.checkpoint;
        internal int ChainCheckpoint => ActiveStopIndex == 5 ? 4 : 5;
        internal bool IsUrgentOrder => CurrentOrder == OrderStyle.Urgent;
        public string LastParcelName => OrderName(LastOrder, LastParcel);
        float OrderFragility => CurrentOrder == OrderStyle.Fragile ? 1.7f : 1;
        internal bool IsChilledOrder => CurrentOrder == OrderStyle.Chilled;
        internal int ChillPercent => Mathf.CeilToInt(100 * order.chillSeconds / 38);

        static OrderStyle OrderAt(OrderState state, int index)
            => state?.plan != null && index >= 0 && index < state.plan.Length ? (OrderStyle)state.plan[index] : OrderStyle.Standard;

        void BeginOrders()
        {
            order = new OrderState();
            if (smokeTest || persistenceTest) return;
            var rng = new System.Random(streetEvents.Seed ^ 0x5B117);
            int standard = heavyIndex >= 0 ? heavyIndex : rng.Next(3);
            int first = rng.Next(1, 7);
            int second = rng.Next(1, 6);
            if (second >= first) second++;
            order.plan = new int[3];
            order.plan[(standard + 1) % 3] = first;
            order.plan[(standard + 2) % 3] = second;
        }

        void BeginOrderLeg()
        {
            order.chillSeconds = 38;
            order.checkpoint = false;
        }

        void RefreshVipCustomer(bool receipt = false)
        {
            int stop = receipt ? route[Mathf.Max(0, Completed - 1)] : ActiveStopIndex;
            bool vip = (receipt ? LastOrder : CurrentOrder) == OrderStyle.Vip;
            for (int i = 0; i < customers.Length; i++) customers[i].SetVipRole(vip && i == stop ? 1 + i % 2 : 0);
        }

        OrderState CaptureOrders() => order.plan == null ? null : new OrderState
            { plan = (int[])order.plan.Clone(), chillSeconds = order.chillSeconds, checkpoint = order.checkpoint };

        static bool ValidOrders(OrderState state)
        {
            if (state?.plan == null || state.plan.Length == 0) return true;
            if (state.plan.Length != 3 || !InRange(state.chillSeconds, 0, 38)) return false;
            for (int i = 0; i < state.plan.Length; i++)
                if (state.plan[i] < 0 || state.plan[i] > (int)OrderStyle.Surprise) return false;
            return true;
        }

        void RestoreOrders(OrderState state)
            => order = state?.plan != null && state.plan.Length == 3
                ? new OrderState { plan = (int[])state.plan.Clone(), chillSeconds = state.chillSeconds, checkpoint = state.checkpoint }
                : new OrderState();

        void TickOrder(float dt)
        {
            if (CurrentOrder != OrderStyle.Chilled || (!HasPackage && !IsParcelDropped)) return;
            order.chillSeconds = Mathf.Max(0, order.chillSeconds - dt);
            Cargo = Mathf.Min(Cargo, Mathf.Lerp(32, 100, order.chillSeconds / 38));
        }

        bool TryOrderCheckpoint()
        {
            if (!AwaitingOrderCheckpoint) return false;
            order.checkpoint = true;
            pickupDelay = .8f;
            Notify(GameText.Pick("Ara durak tamam! Şimdi alıcıya devam et.", "Checkpoint stamped! Now head to the recipient."));
            PlayFeelPair(550, 730);
            Save();
            return true;
        }

        static int OrderFee(OrderStyle style)
            => style == OrderStyle.Urgent ? 34 : style == OrderStyle.Fragile ? 22 : style == OrderStyle.Chilled ? 28 :
                style == OrderStyle.Chain ? 42 : style == OrderStyle.Vip ? 58 : 0;

        int OpenSurprise()
        {
            if (CurrentOrder != OrderStyle.Surprise) return 0;
            int index = 9 + (int)((uint)(streetEvents.Seed ^ Completed * 7919) % (uint)(DecorCount - 9));
            if (((streetEvents.Seed ^ Completed) & 1) != 0 && !DecorOwned(index))
            {
                ownedDecor |= 1 << index;
                equippedDecor |= 1 << index;
                RefreshGarage();
                Notify(GameText.Pick("Sürpriz! Garaja yeni dekor: ", "Surprise! New garage decoration: ") + DecorName(index));
                return 0;
            }
            Notify(GameText.Pick("Sürpriz kutu ücrete 32 temel jeton ekledi!", "The surprise box added 32 base coins to the fee!"));
            return 32;
        }

        static bool ValidOrderReceipt(int amount, int multiplier, OrderStyle style)
        {
            int standard = amount - OrderFee(style);
            if (style == OrderStyle.Surprise && !ValidReceiptBase(standard)) standard -= 32;
            return ValidReceiptBase(standard) && (multiplier == 1 || multiplier == 2 && (standard == 45 || standard == 125));
        }

        static string OrderName(OrderStyle style, ParcelKind standard)
        {
            switch (style)
            {
                case OrderStyle.Urgent: return GameText.Pick("ACİL SİPARİŞ", "RUSH ORDER");
                case OrderStyle.Fragile: return GameText.Pick("Cam vazo", "Glass vase");
                case OrderStyle.Chilled: return GameText.Pick("Soğuk zincir: dondurma", "Cold chain: ice cream");
                case OrderStyle.Chain: return GameText.Pick("Zincir teslimat", "Multi-stop order");
                case OrderStyle.Vip: return GameText.Pick("VIP siparişi", "VIP order");
                case OrderStyle.Surprise: return GameText.Pick("Sürpriz kutu", "Surprise box");
                default: return GameText.ParcelName(standard);
            }
        }

        string OrderHint(ParcelKind parcel)
        {
            switch (CurrentOrder)
            {
                case OrderStyle.Urgent: return GameText.Pick("Kısa süre, yüksek teslimat ücreti.", "Short deadline, higher delivery fee.");
                case OrderStyle.Fragile: return GameText.Pick("Camı koru. Darbeler daha çok hasar verir.", "Protect the glass. Impacts do extra damage.");
                case OrderStyle.Chilled: return GameText.Pick("Soğukluk %", "Chill ") + ChillPercent + (GameText.Turkish ? "" : "%");
                case OrderStyle.Chain: return AwaitingOrderCheckpoint ? GameText.Pick("Önce işaretli ara durağa uğra.", "Visit the marked checkpoint first.")
                    : GameText.Pick("Fiş onaylı. Sırada alıcı var.", "Stamped. Next stop is the recipient.");
                case OrderStyle.Vip: return ActiveStopIndex % 2 == 0 ? GameText.Pick("Şefin siparişi. Bahşiş ücrete dahil.", "Chef's order. The fee includes a tip.")
                    : GameText.Pick("Belediye başkanının siparişi.", "The mayor's order.");
                case OrderStyle.Surprise: return GameText.Pick("Alıcı kutuyu açınca hediyen belli olur.", "The recipient reveals your gift.");
                default: return GameText.ParcelHint(parcel);
            }
        }

        static (string tr, string en) SpecialOrderReaction(OrderStyle style, float integrity)
        {
            if (style == OrderStyle.Chilled)
                return integrity >= 85 ? ("Tam kıvamında, bir top da sana!", "Just right! A scoop for you!") :
                    ("Dondurmadan milkshake çıktı!", "The ice cream turned into a milkshake!");
            if (style == OrderStyle.Fragile)
                return integrity >= 85 ? ("Vazo tek parça! Ellerine sağlık.", "The vase is in one piece. Nice work!") :
                    ("Biraz sallanmış. Dikkatlice açayım.", "It had a bumpy ride. I'll open it carefully.");
            if (style == OrderStyle.Vip) return ("Zahmetin için bahşiş de var!", "There's a tip for your trouble!");
            if (style == OrderStyle.Chain) return ("Fiş damgalı, paket tamam!", "Stamped slip and parcel received!");
            if (style == OrderStyle.Surprise) return ("Birlikte açalım mı?", "Shall we open it together?");
            return ("Acil siparişim geldi!", "My rush order is here!");
        }

        string LastOrderReaction()
        {
            if (LastOrder == OrderStyle.Standard) return GameText.Reaction(LastParcel, LastIntegrity, DeliveryReactionVariant);
            var reaction = SpecialOrderReaction(LastOrder, LastIntegrity);
            return GameText.Pick(reaction.tr, reaction.en);
        }

        internal void SetOrderForTest(OrderStyle style)
        {
            if (!smokeTest && !persistenceTest) return;
            order.plan ??= new int[3];
            order.plan[Mathf.Clamp(Completed, 0, 2)] = (int)style;
            BeginOrderLeg();
            PrepareDeliveryClock();
            RefreshVipCustomer();
        }
    }
}
