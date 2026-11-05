using System.Collections.Generic;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// Everything on screen during a shift: one slim top bar, labels over the
    /// waiting orders, the parcel-in-hand chip, payout pops with flying coins,
    /// the streak glow, the level-up card picker and the shift-over screen.
    /// </summary>
    public sealed class ShiftHUD : MonoBehaviour
    {
        public CourierGame Game;

        static readonly Color Ink = ToyGui.Ink, Muted = ToyGui.Muted, Cream = ToyGui.Cream, Teal = ToyGui.Teal,
            Coral = ToyGui.Coral, Gold = ToyGui.Gold, Pale = ToyGui.Pale, StarOff = ToyGui.StarOff;
        static Texture2D star => ToyGui.Star;
        static Texture2D disc => ToyGui.Disc;
        static GUIStyle hit => ToyGui.HitStyle;
        float shownCoins, coinBump;
        bool coinsPrimed;

        sealed class Pop { public Vector2 at; public string text; public float age; }
        sealed class FlyingCoin { public Vector2 from; public float delay, age; }
        readonly List<Pop> pops = new List<Pop>();
        readonly List<FlyingCoin> coins = new List<FlyingCoin>();

        static string L(string tr, string en) => GameText.Pick(tr, en);
        static float W => GuiViewport.LogicalWidth;
        static float H => GuiViewport.LogicalHeight;
        static bool Compact => GuiViewport.Compact;
        static Rect Bar => Compact ? new Rect(8, 6, W - 16, 46) : new Rect(16, 12, W - 32, 66);
        static Rect PauseRect { get { Rect b = Bar; return new Rect(b.x + 6, b.y + 6, b.height - 12, b.height - 12); } }
        static Rect CoinRect { get { Rect b = Bar; float w = Compact ? 120 : 170; return new Rect(b.xMax - w - 6, b.y + 6, w, b.height - 12); } }

        /// <summary>The joystick may start anywhere except on the top bar.</summary>
        public static bool BlocksPointer(Vector2 screenPoint)
        {
            if (!GuiViewport.SafeScreenRect.Contains(screenPoint)) return true;
            Vector2 p = GuiViewport.ScreenToDesign(screenPoint);
            Rect b = Bar;
            return p.y < b.yMax + 6;
        }

        void Awake() { if (!Game) Game = GetComponent<CourierGame>(); }
        void Start() { if (Game) Game.ShiftPaid += OnPaid; }
        void OnDestroy() { if (Game) Game.ShiftPaid -= OnPaid; }

        void OnPaid(Vector3 world, int amount)
        {
            Vector2 at = WorldToDesign(world, out bool visible);
            if (!visible) at = new Vector2(W * .5f, H * .45f);
            pops.Add(new Pop { at = at, text = "+" + amount });
            if (Game.ReducedMotion) return;
            int count = Mathf.Clamp(amount / 12, 5, 12);
            for (int i = 0; i < count; i++)
                coins.Add(new FlyingCoin { from = at + Random.insideUnitCircle * 26, delay = i * .045f });
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = pops.Count - 1; i >= 0; i--) { pops[i].age += dt; if (pops[i].age > 1.3f) pops.RemoveAt(i); }
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                coins[i].age += dt;
                if (coins[i].age - coins[i].delay >= .7f) { coins.RemoveAt(i); coinBump = .18f; }
            }
            coinBump = Mathf.Max(0, coinBump - dt);
            if (!Game) return;
            // Roll the wallet toward the real value so payouts are felt, not just shown.
            if (!coinsPrimed || Game.ReducedMotion || !Game.InShift) { shownCoins = Game.Coins; coinsPrimed = true; }
            else if (coins.Count == 0) shownCoins = Mathf.MoveTowards(shownCoins, Game.Coins, Mathf.Max(30, Mathf.Abs(Game.Coins - shownCoins) * 5) * dt);
        }

        void OnGUI()
        {
            if (!Game || !Game.InShift) return;
            bool playing = Game.Phase == GamePhase.Playing;
            if (!playing && Game.Phase != GamePhase.ShiftCard && Game.Phase != GamePhase.ShiftEnd) return;
            ToyGui.Ensure();
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.depth = -10; // above the comic bubbles drawn by CourierHUD
            GUI.matrix = GuiViewport.Matrix;
            try
            {
                if (playing)
                {
                    DrawStreakGlow();
                    DrawOrderLabels();
                    DrawCarryChip();
                    DrawBar();
                    DrawToast();
                    DrawPops();
                }
                else if (Game.Phase == GamePhase.ShiftCard) DrawCards();
                else DrawEnd();
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        void DrawBar()
        {
            Rect b = Bar;
            bool rush = Game.IsRushHour;
            bool pulse = rush && !Game.ReducedMotion && Mathf.Repeat(Time.unscaledTime, .8f) < .4f;
            Panel(b, pulse ? Hex(0xFBD3C4) : Alpha(Cream, .96f));
            Rect pause = PauseRect;
            Panel(pause, Pale);
            float barW = pause.width * .14f, barH = pause.height * .42f;
            Fill(new Rect(pause.center.x - barW * 1.6f, pause.center.y - barH * .5f, barW, barH), Ink);
            Fill(new Rect(pause.center.x + barW * .6f, pause.center.y - barH * .5f, barW, barH), Ink);
            if (GUI.Button(pause, GUIContent.none, hit)) Game.PauseGame();

            float s = b.height - (Compact ? 16 : 22);
            float x = pause.xMax + 12;
            for (int i = 0; i < CourierGame.ShiftStarCount; i++)
                Icon(new Rect(x + i * (s + 3), b.center.y - s * .5f, s, s), star, i < Game.ShiftStars ? Gold : StarOff);
            x += 3 * (s + 3) + 10;
            int seconds = Mathf.CeilToInt(Game.ShiftTimeLeft);
            Text(new Rect(x, b.y, Compact ? 62 : 88, b.height), seconds / 60 + ":" + (seconds % 60).ToString("00"), Compact ? 21 : 30,
                rush ? Coral : Ink, true, TextAnchor.MiddleLeft);
            x += Compact ? 62 : 88;

            Rect coinBox = CoinRect;
            float barStart = x + 8, barEnd = coinBox.x - 14;
            if (rush)
            {
                Rect tag = new Rect(barStart, b.center.y - (Compact ? 13 : 16), Compact ? 100 : 136, Compact ? 26 : 32);
                Panel(tag, Coral);
                Text(tag, L("YOĞUN SAAT ×2", "RUSH HOUR ×2"), Compact ? 12 : 15, Color.white, true, TextAnchor.MiddleCenter);
                barStart = tag.xMax + 10;
            }
            float badge = b.height - (Compact ? 14 : 20);
            Rect lv = new Rect(barStart, b.center.y - badge * .5f, badge, badge);
            Icon(lv, disc, Teal);
            Text(lv, Game.ShiftLevel.ToString(), Compact ? 16 : 22, Color.white, true, TextAnchor.MiddleCenter);
            Rect xp = new Rect(lv.xMax + 6, b.center.y - 6, Mathf.Max(30, barEnd - lv.xMax - 6), 12);
            Thin(xp, Hex(0xDBE5DE));
            if (Game.ShiftXp01 > .01f) Thin(new Rect(xp.x, xp.y, Mathf.Max(12, xp.width * Game.ShiftXp01), xp.height), Gold);

            Panel(coinBox, Ink);
            float bump = 1 + coinBump * 1.4f, c = coinBox.height - 10;
            Icon(new Rect(coinBox.x + 6 - c * (bump - 1) * .5f, coinBox.center.y - c * bump * .5f, c * bump, c * bump), disc, Gold);
            Icon(new Rect(coinBox.x + 6 + c * .25f, coinBox.center.y - c * .25f, c * .5f, c * .5f), disc, Hex(0xFFE08A));
            Text(new Rect(coinBox.x + c + 10, coinBox.y, coinBox.width - c - 18, coinBox.height), Mathf.RoundToInt(shownCoins).ToString("N0"),
                Compact ? 17 : 23, Color.white, true, TextAnchor.MiddleRight);
        }

        void DrawToast()
        {
            if (Game.NoticeTime <= 0 || string.IsNullOrEmpty(Game.Notice)) return;
            float w = Mathf.Min(W - 40, Compact ? 520 : 640), h = Compact ? 36 : 46;
            Rect r = new Rect((W - w) * .5f, Bar.yMax + 8, w, h);
            Panel(r, Alpha(Ink, Mathf.Clamp01(Game.NoticeTime * 2) * .9f));
            Text(new Rect(r.x + 12, r.y, r.width - 24, r.height), Game.Notice, Compact ? 13 : 16, Color.white, true, TextAnchor.MiddleCenter);
        }

        void DrawOrderLabels()
        {
            bool carrying = Game.HasPackage || Game.IsParcelDropped;
            for (int i = 0; i < CourierGame.BoardSize; i++)
            {
                if (!Game.BoardLive(i)) continue;
                float left = Game.BoardLeft(i);
                Color c = Game.BoardColor(i);
                if (carrying) c = Color.Lerp(c, Color.white, .35f);
                Tag(Game.BoardPosition(i) + Vector3.up * 2.7f, Game.BoardLabel(i), Mathf.CeilToInt(left) + L(" sn", "s"), c,
                    Game.BoardLeft01(i), left < 6, false);
            }
            if (Game.IsParcelDropped)
                Tag(Game.DroppedParcelPosition + Vector3.up * 2.2f, L("DÜŞEN PAKET", "DROPPED"), "", Coral, 1, true, true);
            else if (Game.HasPackage)
                Tag(Game.TargetPosition + Vector3.up * 2.9f, Game.TargetName, Mathf.CeilToInt(Game.TimeLeft) + L(" sn", "s"),
                    Game.TimeLeft <= 8 ? Coral : Teal, Mathf.Clamp01(Game.TimeLeft / Mathf.Max(1, Game.TimeBudget)), Game.TimeLeft <= 8, true);
        }

        // A label over a world position. Off-screen targets stick to the edge so
        // the player always knows where every order is.
        void Tag(Vector3 world, string title, string time, Color color, float fill01, bool flash, bool primary)
        {
            Vector2 p = WorldToDesign(world, out bool visible);
            float w = primary ? (Compact ? 150 : 200) : (Compact ? 104 : 132), h = primary ? (Compact ? 34 : 44) : (Compact ? 28 : 36);
            // Stay clear of the toast line under the bar.
            float top = Bar.yMax + (Compact ? 46 : 62), bottom = H - (Compact ? 50 : 74);
            bool clamped = !visible || p.x < w * .5f + 8 || p.x > W - w * .5f - 8 || p.y < top + h || p.y > bottom;
            p.x = Mathf.Clamp(p.x, w * .5f + 8, W - w * .5f - 8);
            p.y = Mathf.Clamp(p.y, top + h, bottom);
            if (flash && !Game.ReducedMotion && Mathf.Repeat(Time.unscaledTime, .5f) < .25f) color = Color.Lerp(color, Color.white, .5f);
            Rect r = new Rect(p.x - w * .5f, p.y - h, w, h);
            Panel(r, clamped ? Alpha(color, .88f) : color);
            if (!clamped) Icon(new Rect(p.x - 7, r.yMax - 3, 14, 14), disc, color);
            int titleSize = primary ? (Compact ? 13 : 16) : (Compact ? 11 : 13);
            if (string.IsNullOrEmpty(time)) { Text(r, title, titleSize, Color.white, true, TextAnchor.MiddleCenter); return; }
            Text(new Rect(r.x + 8, r.y, r.width * .62f, r.height - 4), title, titleSize, Color.white, true, TextAnchor.MiddleLeft);
            Text(new Rect(r.x + r.width * .55f, r.y, r.width * .45f - 8, r.height - 4), time, titleSize + 2, Color.white, true, TextAnchor.MiddleRight);
            Fill(new Rect(r.x + 8, r.yMax - 6, (r.width - 16) * Mathf.Clamp01(fill01), 3), Alpha(Color.white, .8f));
        }

        void DrawCarryChip()
        {
            if (!Game.HasPackage && !Game.IsParcelDropped)
            {
                Hint(L("Bir siparişe git ve paketi al", "Ride to an order and pick it up"));
                return;
            }
            float w = Mathf.Min(W - 32, Compact ? 330 : 440), h = Compact ? 44 : 60;
            Rect r = new Rect((W - w) * .5f, H - h - (Compact ? 6 : 14), w, h);
            Panel(r, Alpha(Cream, .96f));
            Rect tag = new Rect(r.x + 8, r.y + 8, Compact ? 70 : 92, r.height - 16);
            Panel(tag, Game.CarriedColor);
            Text(tag, Game.CarriedLabel, Compact ? 11 : 13, Color.white, true, TextAnchor.MiddleCenter);
            string what = Game.IsParcelDropped ? L("Paketi düşürdün! Geri dön.", "Parcel dropped! Go back.") : "→ " + Game.TargetName;
            Text(new Rect(tag.xMax + 10, r.y + 2, r.width * .55f, r.height * .6f), what, Compact ? 14 : 18, Ink, true, TextAnchor.MiddleLeft);
            Rect cargo = new Rect(tag.xMax + 10, r.y + r.height * .68f, r.width * .42f, Compact ? 7 : 9);
            Thin(cargo, Hex(0xDBE5DE));
            float c = Mathf.Clamp01(Game.Cargo / 100f);
            Thin(new Rect(cargo.x, cargo.y, Mathf.Max(8, cargo.width * c), cargo.height), c >= .8f ? Teal : c >= .4f ? Gold : Coral);
            bool late = Game.TimeLeft <= 8;
            Text(new Rect(r.xMax - 96, r.y, 86, r.height), Mathf.CeilToInt(Game.TimeLeft) + L(" sn", "s"), Compact ? 20 : 28,
                late ? Coral : Teal, true, TextAnchor.MiddleRight);
        }

        void Hint(string text)
        {
            float w = Mathf.Min(W - 32, Compact ? 300 : 380), h = Compact ? 30 : 38;
            Rect r = new Rect((W - w) * .5f, H - h - (Compact ? 8 : 18), w, h);
            Panel(r, Alpha(Ink, .72f));
            Text(r, text, Compact ? 13 : 16, Color.white, true, TextAnchor.MiddleCenter);
        }

        void DrawStreakGlow()
        {
            int combo = Game.ComboCount;
            if (combo < 2) return;
            float pulse = Game.ReducedMotion ? .5f : .5f + .5f * Mathf.Sin(Time.unscaledTime * 6);
            Color glow = Alpha(combo >= 4 ? Coral : Gold, .16f + .14f * pulse);
            Rect full = GuiViewport.FullScreenRect;
            float t = Compact ? 8 : 12;
            Fill(new Rect(full.x, full.y, full.width, t), glow);
            Fill(new Rect(full.x, full.yMax - t, full.width, t), glow);
            Fill(new Rect(full.x, full.y, t, full.height), glow);
            Fill(new Rect(full.xMax - t, full.y, t, full.height), glow);
            Rect tag = new Rect(Bar.x + 6, Bar.yMax + 8, Compact ? 96 : 124, Compact ? 28 : 36);
            Panel(tag, combo >= 4 ? Coral : Gold);
            Text(tag, L("SERİ ×", "STREAK ×") + combo, Compact ? 14 : 18, combo >= 4 ? Color.white : Ink, true, TextAnchor.MiddleCenter);
        }

        void DrawPops()
        {
            foreach (Pop pop in pops)
            {
                float t = pop.age / 1.3f;
                float scale = Game.ReducedMotion ? 1 : t < .15f ? Mathf.Lerp(.4f, 1.25f, t / .15f) : Mathf.Lerp(1.25f, 1, Mathf.Clamp01((t - .15f) / .2f));
                float rise = Game.ReducedMotion ? 0 : t * 70;
                int size = Mathf.RoundToInt((Compact ? 26 : 40) * scale);
                Color c = Gold; c.a = 1 - Mathf.Clamp01((t - .7f) / .3f);
                Color shade = Alpha(Ink, c.a * .6f);
                Rect r = new Rect(pop.at.x - 120, pop.at.y - rise - 30, 240, 60);
                Text(new Rect(r.x + 2, r.y + 3, r.width, r.height), pop.text, size, shade, true, TextAnchor.MiddleCenter);
                Text(r, pop.text, size, c, true, TextAnchor.MiddleCenter);
            }
            Rect target = CoinRect;
            Vector2 to = new Vector2(target.x + target.height * .5f, target.center.y);
            float coinSize = Compact ? 18 : 26;
            foreach (FlyingCoin coin in coins)
            {
                float t = Mathf.Clamp01((coin.age - coin.delay) / .7f);
                if (coin.age < coin.delay) continue;
                float ease = t * t * (3 - 2 * t);
                Vector2 mid = Vector2.Lerp(coin.from, to, .5f) + new Vector2(0, -80);
                Vector2 p = Vector2.Lerp(Vector2.Lerp(coin.from, mid, ease), Vector2.Lerp(mid, to, ease), ease);
                Icon(new Rect(p.x - coinSize * .5f, p.y - coinSize * .5f, coinSize, coinSize), disc, Gold);
                Icon(new Rect(p.x - coinSize * .22f, p.y - coinSize * .22f, coinSize * .44f, coinSize * .44f), disc, Hex(0xFFE08A));
            }
        }

        void DrawCards()
        {
            Fill(GuiViewport.FullScreenRect, new Color(.04f, .10f, .13f, .62f));
            bool opening = Game.ShiftOpeningPick;
            Text(new Rect(0, Compact ? 12 : 70, W, 44), opening ? L("VARDİYA BAŞLIYOR!", "SHIFT STARTING!") : L("SEVİYE ATLADIN!", "LEVEL UP!"),
                Compact ? 30 : 44, Gold, true, TextAnchor.MiddleCenter);
            Text(new Rect(0, Compact ? 52 : 124, W, 30), opening ? L("İlk gücünü seç", "Pick your first power") : L("Bir güç seç", "Pick a power"),
                Compact ? 17 : 22, Color.white, true, TextAnchor.MiddleCenter);
            int count = Game.ShiftOfferCount;
            float gap = Compact ? 12 : 24;
            float cardW = Mathf.Min(Compact ? 220 : 300, (W - 48 - gap * (count - 1)) / count);
            float cardH = Compact ? H - 108 : 420;
            float startX = (W - (cardW * count + gap * (count - 1))) * .5f, y = Compact ? 92 : 190;
            for (int i = 0; i < count; i++)
            {
                ShiftCard card = Game.ShiftOffer(i);
                Rect c = new Rect(startX + i * (cardW + gap), y, cardW, cardH);
                Color tint = CourierGame.CardColor(card);
                Panel(c, Cream);
                Panel(new Rect(c.x, c.y, c.width, cardH * .40f), tint);
                int level = Game.CardLevel(card);
                float dot = Compact ? 14 : 20, dots = CourierGame.CardMaxLevel * dot + (CourierGame.CardMaxLevel - 1) * 8;
                for (int d = 0; d < CourierGame.CardMaxLevel; d++)
                    Icon(new Rect(c.center.x - dots * .5f + d * (dot + 8), c.y + cardH * .40f - dot - 10, dot, dot), star,
                        d < level ? Color.white : d == level ? Gold : Alpha(Color.white, .35f));
                Text(new Rect(c.x + 10, c.y + (Compact ? 6 : 14), c.width - 20, 24), level == 0 ? L("YENİ", "NEW") : L("Seviye ", "Level ") + (level + 1),
                    Compact ? 13 : 17, Alpha(Color.white, .9f), true, TextAnchor.UpperCenter);
                Text(new Rect(c.x + 10, c.y + (Compact ? 22 : 40), c.width - 20, cardH * .40f - dot - (Compact ? 30 : 56)), CourierGame.CardBadge(card),
                    Compact ? 30 : 48, Color.white, true, TextAnchor.MiddleCenter);
                Text(new Rect(c.x + 12, c.y + cardH * .43f, c.width - 24, Compact ? 30 : 40), CourierGame.CardName(card), Compact ? 19 : 26, Ink, true, TextAnchor.MiddleCenter);
                Text(new Rect(c.x + 14, c.y + cardH * .43f + (Compact ? 32 : 46), c.width - 28, Compact ? 50 : 80), CourierGame.CardDescription(card),
                    Compact ? 14 : 18, Muted, false, TextAnchor.UpperCenter);
                Rect pick = new Rect(c.x + 14, c.yMax - (Compact ? 46 : 64), c.width - 28, Compact ? 36 : 50);
                Panel(pick, tint);
                Text(pick, L("SEÇ", "PICK"), Compact ? 16 : 20, Color.white, true, TextAnchor.MiddleCenter);
                if (GUI.Button(c, GUIContent.none, hit)) Game.ChooseShiftCard(i);
            }
        }

        void DrawEnd()
        {
            Fill(GuiViewport.FullScreenRect, new Color(.04f, .10f, .13f, .55f));
            float pw = Mathf.Min(Compact ? 620 : 560, W - 32), ph = Compact ? H - 24 : 680;
            Rect p = new Rect((W - pw) * .5f, (H - ph) * .5f, pw, ph);
            Panel(p, Cream);
            bool won = Game.ShiftSucceeded;
            Text(new Rect(p.x, p.y + (Compact ? 10 : 30), p.width, 44), won ? L("VARDİYA TAMAM!", "SHIFT COMPLETE!") : L("VARDİYA BİTTİ", "SHIFT OVER"),
                Compact ? 28 : 40, won ? Teal : Coral, true, TextAnchor.MiddleCenter);
            Text(new Rect(p.x + 20, p.y + (Compact ? 50 : 82), p.width - 40, 26),
                won ? L("Mahalle sana bayıldı!", "The neighborhood loves you!") : L("Yıldızların bitti. Bir dahakine!", "Out of stars. Next time!"),
                Compact ? 15 : 19, Muted, false, TextAnchor.MiddleCenter);

            float rowY = p.y + (Compact ? 78 : 128), rowH = Compact ? 22 : 34;
            Row(p, ref rowY, rowH, L("Teslimat", "Deliveries"), Game.ShiftDeliveries.ToString());
            Row(p, ref rowY, rowH, L("En uzun seri", "Best streak"), "×" + Mathf.Max(1, Game.ShiftBestCombo));
            Row(p, ref rowY, rowH, L("Seviye", "Level"), Game.ShiftLevel.ToString());
            Row(p, ref rowY, rowH, Game.RankedUpLastShift ? L("RÜTBE ATLADIN!", "RANK UP!") : L("Rütbe puanı", "Rank XP"),
                "+" + Game.LastRankGain + (Game.RankedUpLastShift ? "  →  " + Game.Rank : ""));
            if (Game.ShiftBonus > 0) Row(p, ref rowY, rowH, L("Vardiya bonusu", "Shift bonus"), "+" + Game.ShiftBonus);
            Row(p, ref rowY, rowH, L("Toplam kazanç", "Total earned"), Game.ShiftEarnings + L(" jeton", " coins"), true);

            string test = Game.CommerceIsMock ? "TEST • " : "";
            float bh = Compact ? 42 : 56, gap = 10;
            float by = p.yMax - (Compact ? 12 : 24);
            if (Compact)
            {
                float half = (p.width - 48 - gap) * .5f;
                by -= bh;
                if (Button(new Rect(p.x + 24, by, half, bh), L("Yeni vardiya", "New shift"), Teal, Color.white, 17)) Game.StartAnotherShift();
                if (Button(new Rect(p.x + 24 + half + gap, by, half, bh), L("Ana menü", "Main menu"), Pale, Ink, 17)) Game.FinishShift();
                by -= bh + gap;
                if (Game.CanReviveShift && Button(new Rect(p.x + 24, by, half, bh), test + L("Reklamla devam et", "Continue • ad"), Coral, Color.white, 15)) Game.RequestShiftRevive();
                if (Game.CanDoubleShift && Button(new Rect(p.x + 24 + half + gap, by, half, bh), test + L("Kazanç ×2 • reklam", "Earnings ×2 • ad"), Gold, Ink, 15)) Game.RequestShiftDouble();
                return;
            }
            float bw = p.width - 48;
            by -= bh;
            if (Button(new Rect(p.x + 24, by, bw, bh), L("Ana menü", "Main menu"), Pale, Ink, 19)) Game.FinishShift();
            by -= bh + gap;
            if (Button(new Rect(p.x + 24, by, bw, bh), L("Yeni vardiya  →", "New shift  →"), Teal, Color.white, 21)) Game.StartAnotherShift();
            if (Game.CanDoubleShift)
            {
                by -= bh + gap;
                if (Button(new Rect(p.x + 24, by, bw, bh), test + L("Kazancı ×2 yap • reklam izle", "Double your earnings • watch ad"), Gold, Ink, 19)) Game.RequestShiftDouble();
            }
            if (Game.CanReviveShift)
            {
                by -= bh + gap;
                if (Button(new Rect(p.x + 24, by, bw, bh), test + L("Reklam izle, 1 yıldızla devam et", "Watch an ad, continue with 1 star"), Coral, Color.white, 19)) Game.RequestShiftRevive();
            }
        }

        void Row(Rect p, ref float y, float h, string name, string value, bool strong = false)
        {
            int size = Compact ? (strong ? 18 : 15) : (strong ? 24 : 19);
            Text(new Rect(p.x + 40, y, p.width * .55f, h), name, size, strong ? Ink : Muted, strong, TextAnchor.MiddleLeft);
            Text(new Rect(p.x + p.width * .45f, y, p.width * .55f - 40, h), value, size, strong ? Teal : Ink, true, TextAnchor.MiddleRight);
            y += h;
        }

        Vector2 WorldToDesign(Vector3 world, out bool visible)
        {
            visible = false;
            if (!Game.GameCamera) return new Vector2(W * .5f, H * .5f);
            Vector3 s = Game.GameCamera.WorldToScreenPoint(world);
            visible = s.z > 0;
            return GuiViewport.ScreenToDesign(new Vector2(s.x, s.y));
        }

        bool Button(Rect r, string text, Color fill, Color ink, int size) => ToyGui.Button(r, text, fill, ink, size);
        static void Panel(Rect r, Color c) => ToyGui.Panel(r, c);
        static void Thin(Rect r, Color c) => ToyGui.Thin(r, c);
        static void Fill(Rect r, Color c) => ToyGui.Fill(r, c);
        static void Icon(Rect r, Texture2D t, Color c) => ToyGui.Icon(r, t, c);
        static void Text(Rect r, string text, int size, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft) =>
            ToyGui.Text(r, text, size, color, bold, anchor);

        static Color Alpha(Color c, float a) => ToyGui.Alpha(c, a);
        static Color Hex(int rgb) => ToyGui.Hex(rgb);
    }
}
