using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Courier rank (lifetime shift XP) and the rewarded free gift shown in the lobby.</summary>
    public sealed partial class CourierGame
    {
        const string RankKey = "kr_rank_xp", GiftKey = "kr_gift_ready_utc", DistrictPickKey = "kr_district_pick";
        const double GiftCooldownHours = 4;
        int rankXp, lastRankGain, lastRankBefore = 1, shiftRankAwarded;
        long giftReadyTicks;
        public LobbyUI Lobby { get; private set; }

        public int Rank => RankFor(rankXp);
        public int RankNeeded => RankStep(Rank);
        public int RankProgress => rankXp - RankStart(Rank);
        public float Rank01 => Mathf.Clamp01(RankProgress / (float)RankNeeded);
        public int LastRankGain => lastRankGain;
        public bool RankedUpLastShift => Rank > lastRankBefore && lastRankGain > 0;
        public int GiftAmount => 100 + 25 * (Rank - 1);
        public TimeSpan GiftWait
        {
            get
            {
                long left = giftReadyTicks - DateTime.UtcNow.Ticks;
                return left > 0 ? new TimeSpan(Math.Min(left, TimeSpan.FromHours(GiftCooldownHours).Ticks)) : TimeSpan.Zero;
            }
        }
        public bool GiftReady => GiftWait == TimeSpan.Zero;
        public bool CanClaimGift => Phase == GamePhase.Menu && GiftReady && AdsReady;

        static int RankStep(int rank) => 60 + 30 * (rank - 1);
        static int RankStart(int rank) { int total = 0; for (int r = 1; r < rank; r++) total += RankStep(r); return total; }
        static int RankFor(int xp) { int rank = 1; while (rank < 50 && xp >= RankStart(rank + 1)) rank++; return rank; }

        void LoadLobby()
        {
            rankXp = 0; giftReadyTicks = 0; districtPick = 0;
            if (smokeTest) return;
            rankXp = Mathf.Clamp(LocalProgress.GetInt(RankKey, 0), 0, 10000000);
            districtPick = Mathf.Clamp(LocalProgress.GetInt(DistrictPickKey, 0), 0, DistrictCount - 1);
            // A clock moved backwards cannot push the gift further than one cooldown away.
            if (long.TryParse(LocalProgress.GetString(GiftKey, "0"), out long ticks))
                giftReadyTicks = Math.Min(ticks, DateTime.UtcNow.Ticks + TimeSpan.FromHours(GiftCooldownHours).Ticks);
        }

        void SaveLobby()
        {
            if (smokeTest) return;
            LocalProgress.SetInt(RankKey, rankXp);
            LocalProgress.SetInt(DistrictPickKey, districtPick);
            LocalProgress.SetString(GiftKey, giftReadyTicks.ToString());
        }

        void ResetLobby()
        {
            rankXp = lastRankGain = 0; lastRankBefore = 1; giftReadyTicks = 0; districtPick = 0;
            if (smokeTest) return;
            LocalProgress.DeleteKey(RankKey);
            LocalProgress.DeleteKey(DistrictPickKey);
            LocalProgress.DeleteKey(GiftKey);
        }

        // Every shift feeds the lifetime rank, win or lose. A revived shift that
        // ends again is only credited for what it added since the first ending.
        void AwardShiftRank(bool success)
        {
            int total = shiftDeliveries * 10 + (shiftLevel - 1) * 5 + (success ? 30 : 0);
            if (shiftRankAwarded == 0) lastRankBefore = Rank;
            int gain = Mathf.Max(0, total - shiftRankAwarded);
            shiftRankAwarded = total;
            lastRankGain = total;
            rankXp = Mathf.Min(10000000, rankXp + gain);
            if (Rank > lastRankBefore)
                Notify(GameText.Pick("RÜTBE ATLADIN! Kurye rütbesi ", "RANK UP! Courier rank ") + Rank);
            UnlockDistrictsByRank();
        }

        public void ClaimGift() { if (CanClaimGift) RequestReward("gift", -1, GiftAmount); }

        bool GrantLobbyReward(string kind, int amount)
        {
            if (kind != "gift" || !GiftReady || amount <= 0) return false;
            Coins = Mathf.Min(999999, Coins + amount);
            giftReadyTicks = DateTime.UtcNow.Ticks + TimeSpan.FromHours(GiftCooldownHours).Ticks;
            Notify(GameText.Pick("Hediye kutusu açıldı! +", "Gift opened! +") + amount + GameText.Pick(" jeton", " coins"));
            PlayFeelPair(880, 1320);
            return true;
        }

        public void OpenGarageTab(int tab)
        {
            if (Phase != GamePhase.Menu) return;
            if (GarageScreen) GarageScreen.PresetTab(tab);
            OpenGarage();
        }
    }
}
