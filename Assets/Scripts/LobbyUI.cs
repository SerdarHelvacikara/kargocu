using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// The main menu as a lobby: rank and wallet on top, a free gift and the
    /// job board on the sides, a big start button, a five-tab bottom bar and a
    /// card-grid shop. Drawn over the living city that the menu camera shows.
    /// </summary>
    public sealed class LobbyUI : MonoBehaviour
    {
        public enum Page { Home, Shop, Garage, City }
        public CourierGame Game;
        public Page Current { get; private set; }
        float toastLeft, toastRight;

        static readonly Color Ink = ToyGui.Ink, Muted = ToyGui.Muted, Cream = ToyGui.Cream, Teal = ToyGui.Teal, Coral = ToyGui.Coral,
            Gold = ToyGui.Gold, Pale = ToyGui.Pale, Violet = ToyGui.Violet, Sky = ToyGui.Sky, Leaf = ToyGui.Leaf;
        static string L(string tr, string en) => GameText.Pick(tr, en);
        static float W => GuiViewport.LogicalWidth;
        static float H => GuiViewport.LogicalHeight;
        // One design unit: 1 on desktop layouts, smaller on short phone screens.
        static float U => GuiViewport.Compact ? .5f : 1;
        static Rect TopBar => new Rect(12 * U + 4, 10 * U + 2, W - 24 * U - 8, 76 * U);
        static Rect NavBar => new Rect(0, H - 96 * U, W, 96 * U);

        sealed class Offer
        {
            public string key, badge;
            public Color tint;
            public int art; // 0 gift, 1 fleet, 2 dragon, 3 dino, 4..6 coins, 7 no ads
        }
        static readonly Offer[] Offers =
        {
            new Offer { key = "gift", tint = Coral, art = 0 },
            new Offer { key = CommerceConfiguration.Fleet, tint = Violet, art = 1, badge = "deal" },
            new Offer { key = CommerceConfiguration.Dragon, tint = Hex(0xA69BCB), art = 2 },
            new Offer { key = CommerceConfiguration.Dino, tint = Leaf, art = 3 },
            new Offer { key = CommerceConfiguration.CoinsSmall, tint = Gold, art = 4 },
            new Offer { key = CommerceConfiguration.CoinsMedium, tint = Gold, art = 5, badge = "popular" },
            new Offer { key = CommerceConfiguration.CoinsLarge, tint = Gold, art = 6, badge = "value" },
            new Offer { key = CommerceConfiguration.RemoveAds, tint = Sky, art = 7 }
        };

        void Awake() { if (!Game) Game = GetComponent<CourierGame>(); }

        void Update()
        {
            if (!Game) return;
            if (Game.Phase != GamePhase.Menu) return;
            if (Current != Page.Home && Input.GetKeyDown(KeyCode.Escape)) Current = Page.Home;
        }

        public void ShowPage(Page page) { if (Game && Game.Phase == GamePhase.Menu) Current = page; }

        void OnGUI()
        {
            if (!Game || Game.Phase != GamePhase.Menu) return;
            ToyGui.Ensure();
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.depth = -10;
            GUI.matrix = GuiViewport.Matrix;
            try
            {
                if (Current == Page.Home) DrawHome();
                else if (Current == Page.Shop) DrawShop();
                else if (Current == Page.Garage) DrawGarage();
                else DrawCity();
                DrawNav();
                DrawTopBar();
                DrawToast();
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        void DrawTopBar()
        {
            Rect b = TopBar;
            ToyGui.Panel(b, ToyGui.Alpha(Cream, .96f));
            float badge = b.height - 14 * U;
            Rect rank = new Rect(b.x + 10 * U, b.center.y - badge * .5f, badge, badge);
            ToyGui.Icon(new Rect(rank.x - 3, rank.y - 3, rank.width + 6, rank.height + 6), ToyGui.Disc, Gold);
            ToyGui.Icon(rank, ToyGui.Disc, Teal);
            ToyGui.Text(rank, Game.Rank.ToString(), Mathf.RoundToInt(28 * U + 4), Color.white, true, TextAnchor.MiddleCenter);
            float x = rank.xMax + 12 * U;
            ToyGui.Text(new Rect(x, b.y + 8 * U, 260 * U + 40, 24 * U + 6), L("KURYE RÜTBESİ", "COURIER RANK"), Mathf.RoundToInt(13 * U + 4), Muted, true, TextAnchor.MiddleLeft);
            Rect bar = new Rect(x, b.y + b.height * .58f, 220 * U + 30, 12 * U + 2);
            ToyGui.Thin(bar, Hex(0xDBE5DE));
            if (Game.Rank01 > .01f) ToyGui.Thin(new Rect(bar.x, bar.y, Mathf.Max(8, bar.width * Game.Rank01), bar.height), Gold);
            ToyGui.Text(new Rect(bar.xMax + 8, b.y, 120 * U + 20, b.height), Game.RankProgress + "/" + Game.RankNeeded, Mathf.RoundToInt(13 * U + 4), Ink, true, TextAnchor.MiddleLeft);
            toastLeft = bar.xMax + 90 * U + 20;

            Rect coins = new Rect(b.xMax - (200 * U + 20) - 8 * U, b.y + 8 * U, 200 * U + 20, b.height - 16 * U);
            ToyGui.Panel(coins, Ink);
            float c = coins.height - 10 * U;
            ToyGui.Coin(new Vector2(coins.x + 6 * U + c * .5f, coins.center.y), c);
            ToyGui.Text(new Rect(coins.x + c + 14 * U, coins.y, coins.width - c - 24 * U, coins.height), Game.Coins.ToString("N0"),
                Mathf.RoundToInt(22 * U + 6), Color.white, true, TextAnchor.MiddleRight);

            Rect settings = new Rect(coins.x - coins.height - 10 * U, coins.y, coins.height, coins.height);
            ToyGui.Raised(settings, Pale);
            for (int i = 0; i < 3; i++)
                ToyGui.Thin(new Rect(settings.x + settings.width * .25f, settings.y + settings.height * (.3f + i * .2f) - 2, settings.width * .5f, 4), Ink);
            if (ToyGui.Hit(settings)) Game.OpenSettings();
            toastRight = settings.x - 10 * U;
        }

        // Notices live in the empty middle of the top bar, never over cards or titles.
        void DrawToast()
        {
            if (Game.NoticeTime <= 0 || string.IsNullOrEmpty(Game.Notice) || toastRight - toastLeft < 120) return;
            Rect b = TopBar;
            Rect r = new Rect(toastLeft, b.y + 8 * U, toastRight - toastLeft, b.height - 16 * U);
            ToyGui.Panel(r, ToyGui.Alpha(Ink, Mathf.Clamp01(Game.NoticeTime * 2) * .92f));
            ToyGui.Text(new Rect(r.x + 12, r.y, r.width - 24, r.height), Game.Notice, Mathf.RoundToInt(15 * U + 4), Color.white, true, TextAnchor.MiddleCenter);
        }

        void DrawHome()
        {
            float top = TopBar.yMax + 12 * U, bottom = NavBar.y - 12 * U;
            ToyGui.Title(new Rect(0, top + 4 * U, W, 80 * U + 10), L("Kurmalı Kargo", "Wobbly Delivery"), Mathf.RoundToInt(64 * U + 8), Cream);
            ToyGui.Text(new Rect(0, top + 82 * U + 6, W, 30 * U + 6), L("4 dakika • 3 yıldız • her seviyede bir güç", "4 minutes • 3 stars • a new power every level"),
                Mathf.RoundToInt(18 * U + 4), Ink, true, TextAnchor.MiddleCenter);

            float cardW = Mathf.Min(320 * U + 20, W * .26f), cardH = Mathf.Min(300 * U + 20, bottom - top - 140 * U);
            Rect gift = new Rect(TopBar.x, bottom - cardH, cardW, cardH);
            Rect jobs = new Rect(TopBar.xMax - cardW, bottom - cardH, cardW, cardH);
            DrawGiftCard(gift);
            DrawJobsCard(jobs);

            float bw = Mathf.Min(440 * U + 40, W - cardW * 2 - 80 * U), bh = 100 * U + 8;
            float pulse = Game.ReducedMotion ? 1 : 1 + .03f * Mathf.Sin(Time.unscaledTime * 4);
            // Sits above the raised PLAY disc in the bottom bar.
            float lift = NavBar.height * .36f + 12 * U;
            Rect start = new Rect(W * .5f - bw * pulse * .5f, bottom - lift - bh * pulse, bw * pulse, bh * pulse);
            bool resume = Game.HasSavedRun;
            ToyGui.Raised(start, Teal);
            ToyGui.Text(new Rect(start.x, start.y + 6 * U, start.width, start.height * .62f), resume ? L("DEVAM ET", "CONTINUE") : L("VARDİYAYA BAŞLA", "START SHIFT"),
                Mathf.RoundToInt(34 * U + 6), Color.white, true, TextAnchor.MiddleCenter);
            ToyGui.Text(new Rect(start.x, start.y + start.height * .58f, start.width, start.height * .32f),
                Game.BestScore > 0 ? L("En iyi vardiya: ", "Best shift: ") + Game.BestScore + L(" jeton", " coins") : L("İlk vardiyan seni bekliyor!", "Your first shift awaits!"),
                Mathf.RoundToInt(15 * U + 4), ToyGui.Alpha(Color.white, .9f), true, TextAnchor.MiddleCenter);
            if (ToyGui.Hit(start)) StartOrResume();
            if (Game.IsPlaytest)
            {
                Rect test = new Rect(W * .5f - 110 * U - 30, start.y - 50 * U - 14, 220 * U + 60, 40 * U + 8);
                if (ToyGui.Button(test, L("DENEME: +50.000 jeton", "PLAYTEST: +50,000"), Gold, Ink, Mathf.RoundToInt(14 * U + 4))) Game.AddPlaytestCoins();
            }
        }

        void StartOrResume()
        {
            if (Game.HasSavedRun) Game.ResumeRun(); else Game.StartRun();
        }

        void DrawGiftCard(Rect r)
        {
            ToyGui.Raised(r, Cream);
            Rect art = new Rect(r.x, r.y, r.width, r.height * .52f);
            ToyGui.Panel(art, Coral);
            ToyGui.Text(new Rect(art.x, art.y + 8 * U, art.width, 24 * U + 4), L("ÜCRETSİZ HEDİYE", "FREE GIFT"), Mathf.RoundToInt(15 * U + 4), Color.white, true, TextAnchor.MiddleCenter);
            float s = Mathf.Min(art.width, art.height) * .55f;
            DrawGift(new Rect(art.center.x - s * .5f, art.yMax - s - 10 * U, s, s));
            ToyGui.Text(new Rect(r.x, art.yMax + 6 * U, r.width, 30 * U + 4), "+" + Game.GiftAmount + L(" jeton", " coins"), Mathf.RoundToInt(22 * U + 4), Ink, true, TextAnchor.MiddleCenter);
            Rect action = new Rect(r.x + 14 * U, r.yMax - 58 * U - 4, r.width - 28 * U, 48 * U + 4);
            string test = Game.CommerceIsMock ? "TEST • " : "";
            if (Game.GiftReady)
            {
                if (ToyGui.Button(action, test + L("Reklam izle, aç", "Watch ad, open"), Coral, Color.white, Mathf.RoundToInt(15 * U + 4), Game.AdsReady))
                    Game.ClaimGift();
            }
            else
            {
                TimeSpan wait = Game.GiftWait;
                ToyGui.Button(action, L("Yeni hediye: ", "Next gift: ") + (int)wait.TotalHours + ":" + wait.Minutes.ToString("00") + ":" + wait.Seconds.ToString("00"),
                    Pale, Muted, Mathf.RoundToInt(15 * U + 4), false);
            }
        }

        void DrawJobsCard(Rect r)
        {
            ToyGui.Raised(r, Cream);
            Rect head = new Rect(r.x, r.y, r.width, 44 * U + 8);
            ToyGui.Panel(head, Violet);
            ToyGui.Text(head, L("GÖREVLER", "JOBS"), Mathf.RoundToInt(16 * U + 4), Color.white, true, TextAnchor.MiddleCenter);
            int ready = Game.BusinessReadyCount;
            float y = head.yMax + 8 * U, row = (r.height - head.height - 70 * U) / Game.BusinessCount;
            for (int i = 0; i < Game.BusinessCount; i++)
            {
                int target = Game.BusinessTarget(i), progress = Mathf.Min(target, Game.BusinessProgress(i));
                bool done = progress >= target;
                ToyGui.Text(new Rect(r.x + 14 * U, y, r.width * .7f, row * .55f), Game.BusinessName(i), Mathf.RoundToInt(13 * U + 3), Ink, true, TextAnchor.MiddleLeft);
                ToyGui.Text(new Rect(r.x + r.width * .6f, y, r.width * .4f - 14 * U, row * .55f), progress + "/" + target, Mathf.RoundToInt(13 * U + 3), done ? Teal : Muted, true, TextAnchor.MiddleRight);
                Rect bar = new Rect(r.x + 14 * U, y + row * .58f, r.width - 28 * U, 8 * U + 2);
                ToyGui.Thin(bar, Hex(0xE4E9F1));
                if (progress > 0) ToyGui.Thin(new Rect(bar.x, bar.y, Mathf.Max(6, bar.width * progress / target), bar.height), done ? Teal : Violet);
                y += row;
            }
            Rect action = new Rect(r.x + 14 * U, r.yMax - 58 * U - 4, r.width - 28 * U, 48 * U + 4);
            if (ToyGui.Button(action, ready > 0 ? L("Ödülleri al (", "Claim rewards (") + ready + ")" : L("Tüm görevler", "All jobs"),
                ready > 0 ? Gold : Pale, Ink, Mathf.RoundToInt(15 * U + 4))) Game.OpenGarageTab(4);
        }

        void DrawShop()
        {
            float top = TopBar.yMax + 10 * U, bottom = NavBar.y - 8 * U;
            ToyGui.Fill(GuiViewport.FullScreenRect, ToyGui.Alpha(Hex(0x203F4E), .55f));
            ToyGui.Title(new Rect(TopBar.x, top, W * .5f, 50 * U + 6), L("MAĞAZA", "SHOP"), Mathf.RoundToInt(38 * U + 6), Gold, TextAnchor.MiddleLeft);
            Rect restore = new Rect(TopBar.xMax - 260 * U - 20, top + 6 * U, 260 * U + 20, 40 * U + 4);
            if (ToyGui.Button(restore, L("Satın alımları geri yükle", "Restore purchases"), Pale, Ink, Mathf.RoundToInt(13 * U + 4), Game.StoreReady))
                Game.RestorePurchases();
            float gridTop = top + 60 * U + 6, gap = 14 * U + 2;
            float cw = (TopBar.width - gap * 3) / 4, ch = (bottom - gridTop - gap) / 2;
            for (int i = 0; i < Offers.Length; i++)
                DrawOffer(new Rect(TopBar.x + i % 4 * (cw + gap), gridTop + i / 4 * (ch + gap), cw, ch), Offers[i]);
        }

        void DrawOffer(Rect r, Offer offer)
        {
            bool featured = offer.badge == "deal";
            if (featured && !Game.ReducedMotion)
            {
                float glow = .35f + .25f * Mathf.Sin(Time.unscaledTime * 3);
                ToyGui.Panel(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), ToyGui.Alpha(Gold, glow));
            }
            ToyGui.Raised(r, Cream);
            bool roomy = r.height >= 170; // short phone cards drop the detail line
            Rect art = new Rect(r.x, r.y, r.width, r.height * (roomy ? .54f : .42f));
            ToyGui.Panel(art, offer.tint);
            DrawArt(art, offer.art);

            StoreProduct product = null;
            foreach (StoreProduct p in Game.StoreProducts) if (p.Key == offer.key) product = p;
            bool gift = offer.key == "gift";
            bool owned = Owned(offer.key);
            string title = ShortTitle(offer.key) ?? (product != null ? product.Title : CommerceConfiguration.Find(offer.key)?.Title ?? offer.key);
            string detail = gift ? "+" + Game.GiftAmount + L(" jeton", " coins") : Detail(offer.key);
            ToyGui.Text(new Rect(r.x + 10 * U, art.yMax + 4 * U, r.width - 20 * U, 30 * U + 6), title, Mathf.RoundToInt(16 * U + 4), Ink, true, TextAnchor.MiddleCenter);
            if (roomy) ToyGui.Text(new Rect(r.x + 10 * U, art.yMax + 32 * U + 6, r.width - 20 * U, 26 * U + 4), detail, Mathf.RoundToInt(13 * U + 3), Muted, false, TextAnchor.MiddleCenter);

            Rect action = new Rect(r.x + 12 * U, r.yMax - 52 * U - 6, r.width - 24 * U, 44 * U + 4);
            int size = Mathf.RoundToInt(14 * U + 4);
            if (gift)
            {
                string test = Game.CommerceIsMock ? "TEST • " : "";
                if (Game.GiftReady)
                {
                    if (ToyGui.Button(action, test + L("Reklam izle", "Watch ad"), Coral, Color.white, size, Game.AdsReady)) Game.ClaimGift();
                }
                else
                {
                    TimeSpan wait = Game.GiftWait;
                    ToyGui.Button(action, (int)wait.TotalHours + ":" + wait.Minutes.ToString("00") + ":" + wait.Seconds.ToString("00"), Pale, Muted, size, false);
                }
            }
            else if (owned) ToyGui.Button(action, L("Sende var", "Owned"), Pale, Teal, size, false);
            else
            {
                bool available = Game.StoreReady && product != null && product.Available;
                string price = available ? product.LocalizedPrice : L("Şu an kapalı", "Unavailable");
                if (ToyGui.Button(action, price, Teal, Color.white, size, available)) Game.PurchaseProduct(offer.key);
            }

            if (!string.IsNullOrEmpty(offer.badge) && !owned)
            {
                string text = offer.badge == "deal" ? L("EN İYİ FIRSAT", "BEST DEAL") : offer.badge == "popular" ? L("EN POPÜLER", "POPULAR") : L("EN İYİ DEĞER", "BEST VALUE");
                Rect ribbon = new Rect(r.x - 6 * U, r.y + 10 * U, Mathf.Min(r.width * .7f, 150 * U + 20), 28 * U + 4);
                ToyGui.Panel(ribbon, offer.badge == "deal" ? Coral : Ink);
                ToyGui.Text(ribbon, text, Mathf.RoundToInt(11 * U + 3), Color.white, true, TextAnchor.MiddleCenter);
            }
        }

        bool Owned(string key)
        {
            if (key == CommerceConfiguration.RemoveAds) return Game.NoInterstitialAds;
            if (key == CommerceConfiguration.Dragon) return Game.RootPermanentVehicleOwned(4);
            if (key == CommerceConfiguration.Dino) return Game.RootPermanentVehicleOwned(5);
            if (key == CommerceConfiguration.Fleet) return Game.RootPermanentVehicleOwned(4) && Game.RootPermanentVehicleOwned(5);
            return false;
        }

        // Card titles must fit one line; the store's own product titles can be long.
        static string ShortTitle(string key)
        {
            if (key == "gift") return L("Günlük hediye", "Daily gift");
            if (key == CommerceConfiguration.Fleet) return L("Fantastik Filo", "Fantasy Fleet");
            if (key == CommerceConfiguration.Dragon) return L("Kargo Ejderhası", "Cargo Dragon");
            if (key == CommerceConfiguration.Dino) return L("Kurye Dinozor", "Courier Dino");
            return null;
        }

        static string Detail(string key)
        {
            var def = CommerceConfiguration.Find(key);
            if (def != null && def.Consumable) return def.CoinAmount.ToString("N0") + L(" jeton", " coins");
            if (key == CommerceConfiguration.Fleet) return L("Ejderha + dinozor, kalıcı", "Dragon + dinosaur, forever");
            if (key == CommerceConfiguration.RemoveAds) return L("Ara reklamlar kapanır", "No more interstitials");
            return L("Kalıcı fantastik araç", "Permanent fantasy ride");
        }

        void DrawArt(Rect art, int kind)
        {
            float s = Mathf.Min(art.width, art.height) * .62f;
            Vector2 c = new Vector2(art.center.x, art.center.y + 6 * U);
            switch (kind)
            {
                case 0: DrawGift(new Rect(c.x - s * .5f, c.y - s * .5f, s, s)); break;
                case 1:
                    DrawCreature(c + new Vector2(-s * .3f, 0), s * .72f, Hex(0xC4B8EC), true);
                    DrawCreature(c + new Vector2(s * .3f, s * .04f), s * .72f, Hex(0x9FD3A6), false);
                    break;
                case 2: DrawCreature(c, s, Hex(0xC4B8EC), true); break;
                case 3: DrawCreature(c, s, Hex(0x9FD3A6), false); break;
                case 7:
                    ToyGui.Icon(new Rect(c.x - s * .5f, c.y - s * .5f, s, s), ToyGui.Disc, Color.white);
                    ToyGui.Text(new Rect(c.x - s * .5f, c.y - s * .5f, s, s), "AD", Mathf.RoundToInt(s * .38f), Sky, true, TextAnchor.MiddleCenter);
                    Matrix4x4 before = GUI.matrix;
                    GUIUtility.RotateAroundPivot(-40, c);
                    ToyGui.Thin(new Rect(c.x - s * .52f, c.y - s * .06f, s * 1.04f, s * .12f), Coral);
                    GUI.matrix = before;
                    break;
                default:
                    int stacks = kind - 3; // 1, 2 or 3 stacks
                    float coin = s * .42f;
                    for (int st = 0; st < stacks; st++)
                    {
                        float x = c.x + (st - (stacks - 1) * .5f) * coin * .95f;
                        int height = 3 + st * 2 + kind - 4;
                        for (int h = 0; h < height; h++) ToyGui.Coin(new Vector2(x, c.y + s * .35f - h * coin * .22f), coin);
                    }
                    break;
            }
        }

        static void DrawGift(Rect r)
        {
            Color box = Hex(0xFFF1D8), ribbon = Gold;
            ToyGui.Panel(new Rect(r.x + r.width * .08f, r.y + r.height * .38f, r.width * .84f, r.height * .6f), box);
            ToyGui.Panel(new Rect(r.x, r.y + r.height * .24f, r.width, r.height * .22f), Color.Lerp(box, Color.white, .4f));
            ToyGui.Thin(new Rect(r.center.x - r.width * .08f, r.y + r.height * .24f, r.width * .16f, r.height * .74f), ribbon);
            ToyGui.Icon(new Rect(r.center.x - r.width * .32f, r.y, r.width * .3f, r.height * .28f), ToyGui.Disc, ribbon);
            ToyGui.Icon(new Rect(r.center.x + r.width * .02f, r.y, r.width * .3f, r.height * .28f), ToyGui.Disc, ribbon);
        }

        static void DrawCreature(Vector2 c, float s, Color body, bool dragon)
        {
            if (dragon)
            {
                ToyGui.Icon(new Rect(c.x - s * .42f, c.y - s * .58f, s * .22f, s * .3f), ToyGui.Disc, Hex(0xFFE8B0));
                ToyGui.Icon(new Rect(c.x + s * .2f, c.y - s * .58f, s * .22f, s * .3f), ToyGui.Disc, Hex(0xFFE8B0));
            }
            else
                for (int i = 0; i < 3; i++)
                    ToyGui.Icon(new Rect(c.x - s * .32f + i * s * .22f, c.y - s * .56f + (i == 1 ? -s * .06f : 0), s * .2f, s * .24f), ToyGui.Disc, Hex(0x6FB58A));
            ToyGui.Icon(new Rect(c.x - s * .5f, c.y - s * .45f, s, s * .9f), ToyGui.Disc, body);
            ToyGui.Icon(new Rect(c.x - s * .3f, c.y + s * .02f, s * .6f, s * .34f), ToyGui.Disc, Color.Lerp(body, Color.white, .55f));
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGui.Icon(new Rect(c.x + side * s * .2f - s * .1f, c.y - s * .2f, s * .2f, s * .22f), ToyGui.Disc, Color.white);
                ToyGui.Icon(new Rect(c.x + side * s * .2f - s * .045f, c.y - s * .14f, s * .09f, s * .11f), ToyGui.Disc, Ink);
            }
            ToyGui.Icon(new Rect(c.x - s * .2f - s * .06f, c.y + s * .06f, s * .12f, s * .07f), ToyGui.Disc, ToyGui.Alpha(Coral, .7f));
            ToyGui.Icon(new Rect(c.x + s * .2f - s * .06f, c.y + s * .06f, s * .12f, s * .07f), ToyGui.Disc, ToyGui.Alpha(Coral, .7f));
        }

        void DrawNav()
        {
            Rect nav = NavBar;
            ToyGui.Fill(new Rect(GuiViewport.FullScreenRect.x, nav.y + 10 * U, GuiViewport.FullScreenRect.width, nav.height + 400), ToyGui.Alpha(Ink, .92f));
            string[] names = { L("Mağaza", "Shop"), L("Garaj", "Garage"), "", L("Şehir", "City"), L("Görevler", "Jobs") };
            float slot = W / 5;
            for (int i = 0; i < 5; i++)
            {
                Rect r = new Rect(i * slot, nav.y + 10 * U, slot, nav.height - 10 * U);
                if (i == 2)
                {
                    float d = nav.height * (GuiViewport.Compact ? 1.2f : 1.02f);
                    Rect play = new Rect(r.center.x - d * .5f, nav.y - d * .32f, d, d);
                    ToyGui.Icon(new Rect(play.x - 5, play.y - 5, play.width + 10, play.height + 10), ToyGui.Disc, Cream);
                    ToyGui.Icon(play, ToyGui.Disc, Teal);
                    ToyGui.Text(new Rect(play.x - 20, play.y, play.width + 40, play.height), L("BAŞLA", "PLAY"),
                        Mathf.RoundToInt(d * .2f), Color.white, true, TextAnchor.MiddleCenter);
                    if (ToyGui.Hit(play)) StartOrResume();
                    continue;
                }
                bool active = (i == 0 && Current == Page.Shop) || (i == 1 && Current == Page.Garage) || (i == 3 && Current == Page.City);
                if (active) ToyGui.Panel(new Rect(r.x + 8 * U, r.y + 4 * U, r.width - 16 * U, r.height - 8 * U), ToyGui.Alpha(Color.white, .14f));
                float icon = r.height * .42f;
                Vector2 ic = new Vector2(r.center.x, r.y + r.height * .36f);
                DrawNavIcon(i, ic, icon);
                ToyGui.Text(new Rect(r.x, r.y + r.height * .62f, r.width, r.height * .34f), names[i], Mathf.RoundToInt(14 * U + 4),
                    active ? Gold : Color.white, true, TextAnchor.MiddleCenter);
                bool alert = (i == 0 && Game.GiftReady) || (i == 4 && Game.BusinessReadyCount > 0);
                if (alert)
                {
                    float a = 18 * U + 4;
                    ToyGui.Icon(new Rect(ic.x + icon * .45f, ic.y - icon * .65f, a, a), ToyGui.Disc, Coral);
                    ToyGui.Text(new Rect(ic.x + icon * .45f, ic.y - icon * .65f, a, a), "!", Mathf.RoundToInt(12 * U + 4), Color.white, true, TextAnchor.MiddleCenter);
                }
                if (ToyGui.Hit(r)) NavTap(i);
            }
        }

        void NavTap(int i)
        {
            Page target = i == 0 ? Page.Shop : i == 1 ? Page.Garage : i == 3 ? Page.City : Page.Home;
            if (i == 4) { Game.OpenGarageTab(4); return; } // job rewards are claimed in the garage
            Current = Current == target ? Page.Home : target;
        }

        // ---- Garage page -------------------------------------------------

        void PageHeader(string title, out float gridTop, out float bottom)
        {
            float top = TopBar.yMax + 10 * U;
            bottom = NavBar.y - 8 * U;
            ToyGui.Fill(GuiViewport.FullScreenRect, ToyGui.Alpha(Hex(0x203F4E), .55f));
            ToyGui.Title(new Rect(TopBar.x, top, W * .5f, 50 * U + 6), title, Mathf.RoundToInt(38 * U + 6), Gold, TextAnchor.MiddleLeft);
            gridTop = top + 60 * U + 6;
        }

        void DrawGarage()
        {
            PageHeader(L("GARAJ", "GARAGE"), out float gridTop, out float bottom);
            Rect more = new Rect(TopBar.xMax - 240 * U - 20, TopBar.yMax + 16 * U, 240 * U + 20, 40 * U + 4);
            if (ToyGui.Button(more, L("Dekor ve drone", "Decor & drone"), Pale, Ink, Mathf.RoundToInt(13 * U + 4))) Game.OpenGarageTab(2);
            if (GuiViewport.Compact)
            {
                // Legend for the label-free stat bars on short phone cards.
                string[] names = { L("Hız", "Speed"), L("Sağlamlık", "Care"), L("Kazanç", "Pay") };
                Color[] colors = { Coral, Sky, Gold };
                float lx = TopBar.x + 200 * U + 30;
                for (int k = 0; k < 3; k++)
                {
                    ToyGui.Icon(new Rect(lx, more.center.y - 6, 12, 12), ToyGui.Disc, colors[k]);
                    ToyGui.Text(new Rect(lx + 16, more.y, 110, more.height), names[k], 14, Color.white, true, TextAnchor.MiddleLeft);
                    lx += 120;
                }
            }

            float gap = 14 * U + 2;
            float leftW = TopBar.width * .70f;
            float cw = (leftW - gap * 2) / 3, ch = (bottom - gridTop - gap) / 2;
            for (int i = 0; i < Game.VehicleCount; i++)
                DrawVehicleCard(new Rect(TopBar.x + i % 3 * (cw + gap), gridTop + i / 3 * (ch + gap), cw, ch), i);
            float rx = TopBar.x + leftW + gap, rw = TopBar.xMax - rx;
            float paintH = (bottom - gridTop - gap) * .42f;
            DrawPaints(new Rect(rx, gridTop, rw, paintH));
            DrawDepot(new Rect(rx, gridTop + paintH + gap, rw, bottom - gridTop - paintH - gap));
        }

        void DrawVehicleCard(Rect r, int i)
        {
            bool equipped = Game.VehicleIndex == i, owned = Game.VehicleOwned(i);
            if (equipped) ToyGui.Panel(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Gold);
            ToyGui.Raised(r, Cream);
            Rect art = new Rect(r.x, r.y, r.width, r.height * .40f);
            ToyGui.Panel(art, VehicleTint(i));
            DrawVehicleArt(art, i);
            if (i >= 4)
            {
                Rect tag = new Rect(art.xMax - 96 * U - 10, art.y + 8 * U, 90 * U + 6, 24 * U + 4);
                ToyGui.Panel(tag, Violet);
                ToyGui.Text(tag, L("FANTASTİK", "FANTASY"), Mathf.RoundToInt(10 * U + 3), Color.white, true, TextAnchor.MiddleCenter);
            }
            float y = art.yMax + 4 * U;
            ToyGui.Text(new Rect(r.x + 8 * U, y, r.width - 16 * U, 26 * U + 4), Game.VehicleNameFor(i), Mathf.RoundToInt(16 * U + 4), Ink, true, TextAnchor.MiddleCenter);
            y += 28 * U + 4;
            float barsH = r.height - (y - r.y) - 56 * U - 8;
            float row = barsH / 3;
            float fee = CourierGame.DeliveryBaseFeeFor(i) * CourierGame.DeliveryMultiplierFor(i);
            if (row < 18)
            {
                // Short phone cards: three colored mini bars in one row; the page header has the legend.
                float bw = (r.width - 24 * U - 16) / 3;
                float[] values = { CourierGame.VehicleSpeedFor(i) / 1.32f, .45f / CourierGame.VehicleFragilityFor(i), fee / 90f };
                Color[] colors = { Coral, Sky, Gold };
                for (int k = 0; k < 3; k++)
                {
                    Rect bar = new Rect(r.x + 12 * U + k * (bw + 8), y + barsH * .5f - 4, bw, 8);
                    ToyGui.Thin(bar, Hex(0xE2E8E4));
                    ToyGui.Thin(new Rect(bar.x, bar.y, Mathf.Max(6, bar.width * Mathf.Clamp01(values[k])), bar.height), colors[k]);
                }
            }
            else
            {
            StatBar(new Rect(r.x + 12 * U, y, r.width - 24 * U, row), L("Hız", "Speed"), CourierGame.VehicleSpeedFor(i) / 1.32f, Coral);
            StatBar(new Rect(r.x + 12 * U, y + row, r.width - 24 * U, row), L("Sağlamlık", "Care"), .45f / CourierGame.VehicleFragilityFor(i), Sky);
            StatBar(new Rect(r.x + 12 * U, y + row * 2, r.width - 24 * U, row), L("Kazanç", "Pay"), fee / 90f, Gold);
            }

            Rect action = new Rect(r.x + 12 * U, r.yMax - 52 * U - 6, r.width - 24 * U, 44 * U + 4);
            int size = Mathf.RoundToInt(14 * U + 4);
            if (equipped) ToyGui.Button(action, L("Kullanılıyor", "In use"), Pale, Teal, size, false);
            else if (owned)
            {
                if (ToyGui.Button(action, L("Seç", "Use"), Teal, Color.white, size, Game.CanChangeVehicle)) Game.SelectVehicle(i);
            }
            else
            {
                int price = Game.VehiclePrice(i);
                bool afford = Game.Coins >= price && Game.CanChangeVehicle;
                if (ToyGui.Button(action, price.ToString("N0") + L(" jeton", " coins"), Gold, Ink, size, afford)) Game.SelectVehicle(i);
            }
        }

        static void StatBar(Rect r, string name, float value, Color color)
        {
            int size = Mathf.RoundToInt(12 * U + 3);
            ToyGui.Text(new Rect(r.x, r.y, r.width * .42f, r.height), name, size, Muted, true, TextAnchor.MiddleLeft);
            Rect bar = new Rect(r.x + r.width * .44f, r.center.y - 4 * U - 1, r.width * .56f, 8 * U + 2);
            ToyGui.Thin(bar, Hex(0xE2E8E4));
            ToyGui.Thin(new Rect(bar.x, bar.y, Mathf.Max(6, bar.width * Mathf.Clamp01(value)), bar.height), color);
        }

        static Color VehicleTint(int i) => i == 0 ? Hex(0x8FD3C8) : i == 1 ? Hex(0xF4B39C) : i == 2 ? Hex(0xB7D3EE) :
            i == 3 ? Hex(0xF2D18A) : i == 4 ? Hex(0xC9C0EA) : Hex(0xB5DDB0);

        void DrawVehicleArt(Rect art, int i)
        {
            float s = Mathf.Min(art.width * .8f, art.height * 1.1f);
            Vector2 c = new Vector2(art.center.x, art.center.y + 4 * U);
            Color paint = CourierGame.ScooterColors[Game.ScooterColorIndex];
            if (i >= 4) { DrawCreature(c, s * .62f, i == 4 ? Hex(0xC4B8EC) : Hex(0x9FD3A6), i == 4); return; }
            float wheel = i == 3 ? s * .34f : s * .2f;
            float bodyW = i == 2 ? s * .78f : i == 3 ? s * .7f : s * .6f, bodyH = i == 2 ? s * .4f : s * .22f;
            float baseY = c.y + s * .18f;
            ToyGui.Panel(new Rect(c.x - bodyW * .5f, baseY - bodyH - wheel * .3f, bodyW, bodyH), paint);
            if (i == 2) ToyGui.Panel(new Rect(c.x - bodyW * .3f, baseY - bodyH - wheel * .3f + bodyH * .15f, bodyW * .5f, bodyH * .35f), Hex(0xE8F4FA));
            if (i == 0 || i == 1)
            {
                ToyGui.Panel(new Rect(c.x + bodyW * .2f, baseY - bodyH * 2.4f, bodyW * .14f, bodyH * 1.6f), paint);
                ToyGui.Panel(new Rect(c.x - bodyW * .4f, baseY - bodyH * 1.6f, bodyW * .4f, bodyH * .5f), Ink);
                if (i == 1) ToyGui.Thin(new Rect(c.x - bodyW * .45f, baseY - bodyH * .9f, bodyW * .9f, bodyH * .18f), Cream);
            }
            if (i == 3) ToyGui.Panel(new Rect(c.x - bodyW * .25f, baseY - bodyH * 2 - wheel * .3f, bodyW * .5f, bodyH * .9f), paint);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 w = new Vector2(c.x + side * bodyW * .36f, baseY);
                ToyGui.Icon(new Rect(w.x - wheel * .5f, w.y - wheel * .5f, wheel, wheel), ToyGui.Disc, Ink);
                ToyGui.Icon(new Rect(w.x - wheel * .22f, w.y - wheel * .22f, wheel * .44f, wheel * .44f), ToyGui.Disc, Cream);
            }
        }

        void DrawPaints(Rect r)
        {
            ToyGui.Raised(r, Cream);
            ToyGui.Text(new Rect(r.x, r.y + 6 * U, r.width, 28 * U + 4), L("BOYA", "PAINT"), Mathf.RoundToInt(16 * U + 4), Ink, true, TextAnchor.MiddleCenter);
            int count = CourierGame.ScooterColors.Length;
            float d = Mathf.Min((r.width - 40 * U) / count - 12 * U, r.height - 90 * U);
            float total = count * d + (count - 1) * 14 * U, x = r.center.x - total * .5f, y = r.y + 40 * U + 4;
            for (int i = 0; i < count; i++)
            {
                Rect disc = new Rect(x + i * (d + 14 * U), y, d, d);
                bool selected = Game.ScooterColorIndex == i, unlocked = Game.IsColorUnlocked(i);
                if (selected) ToyGui.Icon(new Rect(disc.x - 5, disc.y - 5, disc.width + 10, disc.height + 10), ToyGui.Disc, Gold);
                ToyGui.Icon(disc, ToyGui.Disc, CourierGame.ScooterColors[i]);
                string label = selected ? L("Seçili", "On") : unlocked ? L("Seç", "Use") : Game.ColorPrice(i).ToString();
                ToyGui.Text(new Rect(disc.x - 10, disc.yMax + 2 * U, disc.width + 20, 22 * U + 4), label, Mathf.RoundToInt(12 * U + 3),
                    unlocked ? Ink : Muted, true, TextAnchor.MiddleCenter);
                if (!unlocked) ToyGui.Coin(new Vector2(disc.center.x, disc.center.y), d * .36f);
                if (!selected && (unlocked || Game.Coins >= Game.ColorPrice(i)) && ToyGui.Hit(disc)) Game.SelectColor(i);
            }
        }

        void DrawDepot(Rect r)
        {
            ToyGui.Raised(r, Cream);
            Rect head = new Rect(r.x, r.y, r.width, 40 * U + 6);
            ToyGui.Panel(head, Teal);
            ToyGui.Text(head, L("DEPO • Sv ", "DEPOT • Lv ") + Game.DepotLevel + "/" + CourierGame.MaxDepotLevel, Mathf.RoundToInt(15 * U + 4), Color.white, true, TextAnchor.MiddleCenter);
            float pip = (r.width - 40 * U) / CourierGame.MaxDepotLevel;
            for (int i = 0; i < CourierGame.MaxDepotLevel; i++)
                ToyGui.Thin(new Rect(r.x + 20 * U + i * pip + 2, head.yMax + 12 * U, pip - 4, 12 * U + 2), i < Game.DepotLevel ? Gold : Hex(0xE2E8E4));
            ToyGui.Text(new Rect(r.x + 14 * U, head.yMax + 30 * U + 4, r.width - 28 * U, r.height - head.height - 100 * U),
                L("Her seviye: paket daha hızlı yüklenir, darbeler daha az zarar verir, drone hızlanır.",
                  "Every level: faster loading, softer impacts and a quicker drone."), Mathf.RoundToInt(13 * U + 3), Muted, false, TextAnchor.UpperCenter);
            Rect action = new Rect(r.x + 14 * U, r.yMax - 52 * U - 6, r.width - 28 * U, 44 * U + 4);
            int size = Mathf.RoundToInt(14 * U + 4);
            if (Game.DepotLevel >= CourierGame.MaxDepotLevel) ToyGui.Button(action, L("En üst seviye", "Max level"), Pale, Teal, size, false);
            else if (ToyGui.Button(action, L("Yükselt • ", "Upgrade • ") + Game.DepotPrice.ToString("N0"), Gold, Ink, size, Game.Coins >= Game.DepotPrice))
                Game.UpgradeDepot();
        }

        // ---- City page ---------------------------------------------------

        void DrawCity()
        {
            PageHeader(L("ŞEHİR", "CITY"), out float gridTop, out float bottom);
            ToyGui.Text(new Rect(TopBar.x + 260 * U + 40, TopBar.yMax + 10 * U, TopBar.width - 260 * U - 40, 50 * U + 6),
                L("Rütbe atladıkça şehir büyür. Vardiyaların seçtiğin bölgede geçer.", "The city grows as you rank up. Shifts happen in the district you pick."),
                Mathf.RoundToInt(14 * U + 4), Color.white, true, TextAnchor.MiddleRight);
            int count = CourierGame.DistrictCount;
            float gap = 16 * U + 2, cw = (TopBar.width - gap * (count - 1)) / count, ch = bottom - gridTop;
            // A road joins the districts, left to right, in unlock order.
            Rect road = new Rect(TopBar.x, gridTop + ch * .26f, TopBar.width, 18 * U + 4);
            ToyGui.Thin(road, Hex(0x8C9AA0));
            for (float x = road.x + 10; x < road.xMax - 20; x += 46 * U + 10)
                ToyGui.Fill(new Rect(x, road.center.y - 1, 22 * U + 4, 3), Cream);
            for (int i = 0; i < count; i++) DrawDistrict(new Rect(TopBar.x + i * (cw + gap), gridTop, cw, ch), i);
        }

        void DrawDistrict(Rect r, int i)
        {
            bool owned = Game.DistrictOwned(i), picked = Game.ShiftDistrict == i;
            if (picked) ToyGui.Panel(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Gold);
            ToyGui.Raised(r, Cream);
            Rect art = new Rect(r.x, r.y, r.width, r.height * .46f);
            ToyGui.Panel(art, DistrictSky(i));
            DrawSkyline(art, i);
            if (!owned)
            {
                ToyGui.Panel(art, ToyGui.Alpha(Ink, .55f));
                float l = Mathf.Min(art.width, art.height) * .34f;
                Vector2 c = art.center;
                ToyGui.Icon(new Rect(c.x - l * .35f, c.y - l * .62f, l * .7f, l * .7f), ToyGui.Disc, Cream);
                ToyGui.Icon(new Rect(c.x - l * .2f, c.y - l * .47f, l * .4f, l * .4f), ToyGui.Disc, ToyGui.Alpha(Ink, .9f));
                ToyGui.Panel(new Rect(c.x - l * .5f, c.y - l * .2f, l, l * .72f), Gold);
            }
            if (picked)
            {
                Rect tag = new Rect(r.x - 6 * U, r.y + 10 * U, Mathf.Min(r.width * .8f, 170 * U + 20), 28 * U + 4);
                ToyGui.Panel(tag, Coral);
                ToyGui.Text(tag, L("VARDİYA BURADA", "SHIFTS HERE"), Mathf.RoundToInt(11 * U + 3), Color.white, true, TextAnchor.MiddleCenter);
            }
            float y = art.yMax + 6 * U;
            ToyGui.Text(new Rect(r.x + 8 * U, y, r.width - 16 * U, 28 * U + 4), CourierGame.DistrictName(i), Mathf.RoundToInt(17 * U + 4), Ink, true, TextAnchor.MiddleCenter);
            y += 30 * U + 4;
            string pay = i == 0 ? L("Temel ücret", "Base pay") : "+" + Game.DistrictFee(i) + L(" jeton / teslimat", " coins / delivery");
            ToyGui.Text(new Rect(r.x + 8 * U, y, r.width - 16 * U, 24 * U + 4), pay, Mathf.RoundToInt(14 * U + 3), i == 0 ? Muted : Teal, true, TextAnchor.MiddleCenter);
            y += 26 * U + 4;
            if (r.height > 260)
                ToyGui.Text(new Rect(r.x + 12 * U, y, r.width - 24 * U, r.yMax - y - 60 * U - 30), CourierGame.DistrictHint(i), Mathf.RoundToInt(13 * U + 3), Muted, false, TextAnchor.UpperCenter);

            Rect action = new Rect(r.x + 12 * U, r.yMax - 52 * U - 6, r.width - 24 * U, 44 * U + 4);
            int size = Mathf.RoundToInt(13 * U + 4);
            if (picked) ToyGui.Button(action, L("Seçili", "Selected"), Pale, Teal, size, false);
            else if (owned)
            {
                if (ToyGui.Button(action, L("Burada çalış", "Work here"), Teal, Color.white, size)) Game.PickShiftDistrict(i);
            }
            else
            {
                Rect rankLine = new Rect(r.x + 8 * U, action.y - 50 * U - 10, r.width - 16 * U, 22 * U + 4);
                ToyGui.Text(rankLine, L("Rütbe ", "Rank ") + Game.DistrictRank(i) + L("'te kendiliğinden açılır", " opens it for free"),
                    Mathf.RoundToInt(12 * U + 3), Violet, true, TextAnchor.MiddleCenter);
                Rect bar = new Rect(r.x + 18 * U, rankLine.yMax + 2 * U, r.width - 36 * U, 8 * U + 2);
                ToyGui.Thin(bar, Hex(0xE2E8E4));
                ToyGui.Thin(new Rect(bar.x, bar.y, Mathf.Max(6, bar.width * Mathf.Clamp01(Game.Rank / (float)Game.DistrictRank(i))), bar.height), Violet);
                int price = Game.DistrictPrice(i);
                if (ToyGui.Button(action, L("Şimdi aç • ", "Open now • ") + price.ToString("N0"), Gold, Ink, size, Game.Coins >= price))
                    Game.UnlockDistrict(i);
            }
        }

        static Color DistrictSky(int i) => i == 0 ? Hex(0xBFE3D4) : i == 1 ? Hex(0xF6D6B0) : i == 2 ? Hex(0xF2B7A8) : Hex(0xB9D4EE);

        static void DrawSkyline(Rect art, int i)
        {
            float ground = art.yMax - art.height * .14f;
            ToyGui.Fill(new Rect(art.x + 6, ground, art.width - 12, art.height * .1f), ToyGui.Alpha(Ink, .18f));
            int n = 5;
            float w = (art.width - 24) / n;
            for (int b = 0; b < n; b++)
            {
                float x = art.x + 12 + b * w;
                float h = art.height * (i == 3 ? .45f + .28f * ((b * 37) % 5) / 4f : .26f + .12f * ((b * 13) % 3) / 2f);
                Color body = i == 0 ? new[] { Hex(0xF4C9B8), Hex(0xFFF0C8), Hex(0xC9E4D8) }[b % 3] :
                    i == 1 ? new[] { Hex(0xFFE7B8), Hex(0xF1C79C) }[b % 2] :
                    i == 2 ? new[] { Hex(0xF7E3C8), Hex(0xE8C7A6) }[b % 2] : new[] { Hex(0xE3F1FB), Hex(0xC8DFF2) }[b % 2];
                Rect house = new Rect(x + w * .08f, ground - h, w * .84f, h);
                ToyGui.Thin(house, body);
                if (i == 0) ToyGui.Thin(new Rect(house.x - 2, house.y - h * .2f, house.width + 4, h * .3f), Hex(0xD97D6A));
                if (i == 1)
                    for (int s = 0; s < 4; s++)
                        ToyGui.Fill(new Rect(house.x + s * house.width / 4, house.y, house.width / 4, h * .22f), s % 2 == 0 ? Coral : Cream);
                if (i == 2)
                {
                    ToyGui.Thin(new Rect(house.x - w * .06f, house.y - h * .12f, house.width + w * .12f, h * .18f), Hex(0xC0453A));
                    ToyGui.Icon(new Rect(house.center.x - w * .12f, house.y + h * .25f, w * .24f, w * .28f), ToyGui.Disc, Hex(0xE8473A));
                }
                if (i == 3)
                    for (float wy = house.y + 6; wy < house.yMax - 8; wy += 10 * U + 4)
                        ToyGui.Fill(new Rect(house.x + house.width * .18f, wy, house.width * .64f, 3 * U + 1), Hex(0x7FB6DE));
            }
        }

        static void DrawNavIcon(int i, Vector2 c, float s)
        {
            if (i == 0) { ToyGui.Coin(c + new Vector2(-s * .18f, s * .08f), s * .7f); ToyGui.Coin(c + new Vector2(s * .2f, -s * .06f), s * .7f); return; }
            if (i == 1)
            {
                ToyGui.Panel(new Rect(c.x - s * .6f, c.y - s * .25f, s * 1.2f, s * .55f), Coral);
                ToyGui.Panel(new Rect(c.x - s * .3f, c.y - s * .52f, s * .6f, s * .36f), Coral);
                ToyGui.Icon(new Rect(c.x - s * .5f, c.y + s * .16f, s * .3f, s * .3f), ToyGui.Disc, Cream);
                ToyGui.Icon(new Rect(c.x + s * .2f, c.y + s * .16f, s * .3f, s * .3f), ToyGui.Disc, Cream);
                return;
            }
            if (i == 3)
            {
                ToyGui.Panel(new Rect(c.x - s * .5f, c.y - s * .1f, s * .45f, s * .6f), Sky);
                ToyGui.Panel(new Rect(c.x + s * .02f, c.y - s * .5f, s * .48f, s * 1.0f), Gold);
                return;
            }
            ToyGui.Icon(new Rect(c.x - s * .5f, c.y - s * .5f, s, s), ToyGui.Star, Gold);
        }

        static Color Hex(int rgb) => ToyGui.Hex(rgb);
    }
}
