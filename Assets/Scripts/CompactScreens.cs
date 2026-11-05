using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Readable phone pages and the shared category-based garage. No networking or purchase SDK.</summary>
    public static class CompactScreens
    {
        public readonly struct PhoneLayout
        {
            public readonly Rect GameHeader, GamePause, MiniMap, OverlayHeader, OverlayBody, OverlayFooter;
            public readonly Rect GarageBack, GarageTabs, GarageContent;
            public PhoneLayout(float width, float height)
            {
                GameHeader = new Rect(8, 8, width - 16, 96);
                GamePause = new Rect(16, 18, 56, 60);
                MiniMap = new Rect(width - 108, height - 138, 96, 90);
                OverlayHeader = new Rect(12, 8, width - 24, 52);
                OverlayBody = new Rect(12, 72, width - 24, height - 148);
                OverlayFooter = new Rect(12, height - 64, width - 24, 52);
                GarageBack = new Rect(12, 8, 104, 52);
                GarageTabs = new Rect(12, 72, width - 24, 52);
                GarageContent = new Rect(12, 136, width - 24, height - 148);
            }
        }

        public static PhoneLayout GetLayout(GuiViewport.Layout viewport) => new PhoneLayout(viewport.LogicalWidth, viewport.LogicalHeight);
        public static Rect[] CardGrid(float width, int count, int columns, float cardHeight, float gap = 8)
        {
            columns = Mathf.Max(1, columns);
            count = Mathf.Max(0, count);
            cardHeight = Mathf.Max(52, cardHeight);
            float cardWidth = Mathf.Max(1, (width - gap * (columns - 1)) / columns);
            Rect[] cards = new Rect[count];
            for (int i = 0; i < count; i++) cards[i] = new Rect(i % columns * (cardWidth + gap), i / columns * (cardHeight + gap), cardWidth, cardHeight);
            return cards;
        }

        static readonly Color Ink = Hex(0x193B4B), Muted = Hex(0x66818B), Cream = Hex(0xFFF9ED), Teal = Hex(0x119E99), Coral = Hex(0xE97760), Gold = Hex(0xF3BF48), Pale = Hex(0xEAF3EA);
        static CourierGame game;
        static GUIStyle label, hit, sliderStyle, thumbStyle;
        static Texture2D circle;
        static Font font;
        static GamePhase lastPhase;
        static int garageTab, resetStage, finger = -1, scrollKey, activeScrollKey = -1;
        static Vector2 scroll;
        static Rect scrollRect;
        static float scrollContentHeight;
        static float touchTravel, suppressClickUntil;
        static bool roomPreview;
        static float W => GuiViewport.LogicalWidth;
        static float H => GuiViewport.LogicalHeight;
        static string L(string tr, string en) => GameText.Pick(tr, en);
        static string RewardLabel(string tr, string en) => (game.CommerceIsMock ? "TEST • " : "") + L(tr, en);
        static PhoneLayout Phone => new PhoneLayout(W, H);

        /// <summary>Screenshot seam only. Does not initialize GUI resources or alter game state.</summary>
        public static void SelectGarageTabForTest(int index)
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), value => value == "-smokeTest")) return;
            garageTab = Mathf.Clamp(index, 0, 4);
            roomPreview = false;
            scroll = Vector2.zero; finger = -1; activeScrollKey = -1;
        }

        public static void PreviewGarageRoomForTest(bool preview)
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), value => value == "-smokeTest")) return;
            roomPreview = preview;
            finger = -1;
        }

        static void Ensure(CourierGame target)
        {
            if (game != target)
            {
                game = target; garageTab = resetStage = 0; scroll = Vector2.zero;
                roomPreview = false;
                finger = -1; activeScrollKey = -1; lastPhase = target.Phase;
            }
            if (lastPhase != target.Phase)
            {
                resetStage = 0; scroll = Vector2.zero; finger = -1; activeScrollKey = -1;
                roomPreview = false;
                lastPhase = target.Phase;
            }
            if (label != null && circle) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label = new GUIStyle(GUI.skin.label) { font = font, wordWrap = true, richText = false, padding = new RectOffset(0, 0, 0, 0) };
            hit = new GUIStyle(GUIStyle.none);
            sliderStyle = new GUIStyle(GUIStyle.none) { fixedHeight = 52, padding = new RectOffset(14, 14, 0, 0) };
            thumbStyle = new GUIStyle(GUIStyle.none) { fixedWidth = 28, fixedHeight = 52 };
            circle = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Compact UI corners", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color[] pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32))));
            circle.SetPixels(pixels); circle.Apply(false, true);
        }

        static void Frame(CourierGame target, Action draw)
        {
            Ensure(target);
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color; bool oldEnabled = GUI.enabled;
            GUI.matrix = GuiViewport.Matrix; GUI.color = Color.white;
            try { draw(); }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled; }
        }

        public static bool BlocksGameplayPointer(Vector2 screenPoint)
        {
            if (!GuiViewport.SafeScreenRect.Contains(screenPoint)) return true;
            Vector2 p = GuiViewport.ScreenToDesign(screenPoint);
            return p.y < 112 || Phone.MiniMap.Contains(p) ||
                (game && game.AdsReady && (game.CanRecallParcel || game.CanExtraTime) && GameplayRewardRect().Contains(p));
        }

        public static void UpdateTouch(CourierGame target)
        {
            if (!target || target != game || resetStage > 0 || Input.touchCount != 1 || activeScrollKey < 0 ||
                (GuiViewport.Compact && target.Phase == GamePhase.Garage && roomPreview) ||
                target.AdPreferencesOpen ||
                (target.Phase != GamePhase.Settings && target.Phase != GamePhase.Privacy && target.Phase != GamePhase.Garage && target.Phase != GamePhase.Result) ||
                scrollKey != ((int)target.Phase * 10 + (target.Phase == GamePhase.Garage ? garageTab : 0)))
            { finger = -1; return; }
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Rect content = scrollRect; content.width = Mathf.Max(1, content.width - 24);
                finger = content.Contains(GuiViewport.ScreenToDesign(touch.position)) ? touch.fingerId : -1;
                touchTravel = 0;
            }
            else if (touch.fingerId == finger && touch.phase == TouchPhase.Moved)
            {
                touchTravel += touch.deltaPosition.magnitude / GuiViewport.Scale;
                if (touchTravel > 8) suppressClickUntil = Time.unscaledTime + .2f;
                scroll.y = Mathf.Clamp(scroll.y + touch.deltaPosition.y / GuiViewport.Scale, 0, Mathf.Max(0, scrollContentHeight - scrollRect.height));
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (touch.fingerId == finger && touchTravel > 8) suppressClickUntil = Time.unscaledTime + .15f;
                finger = -1;
            }
        }

        static void BeginScroll(Rect area, float contentHeight)
        {
            scrollKey = (int)game.Phase * 10 + (game.Phase == GamePhase.Garage ? garageTab : 0);
            activeScrollKey = scrollKey; scrollRect = area; scrollContentHeight = Mathf.Max(area.height, contentHeight);
            scroll.y = Mathf.Clamp(scroll.y, 0, Mathf.Max(0, scrollContentHeight - area.height));
            scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, Mathf.Max(1, area.width - 24), scrollContentHeight));
        }

        public static void DrawHud(CourierGame target) => Frame(target, () =>
        {
            if (game.Phase == GamePhase.Menu) Menu();
            else if (game.Phase == GamePhase.Playing) Playing();
            else if (game.Phase == GamePhase.Result) Result();
            else if (game.Phase == GamePhase.Shop) ColorStore();
        });

        public static void DrawOverlay(CourierGame target) => Frame(target, () =>
        {
            if (game.Phase == GamePhase.Settings) Settings();
            else if (game.Phase == GamePhase.Privacy) Privacy();
            else if (game.Phase == GamePhase.Paused) Paused();
            else if (game.Phase == GamePhase.Tutorial) Tutorial();
        });

        static void Menu()
        {
            Dim(.10f);
            float split = W * .48f;
            Panel(new Rect(8, 20, split - 18, H - 40), Alpha(Cream, .96f));
            Text(new Rect(25, 38, split - 48, 96), L("Kurmalı\nKargo", "Wobbly\nDelivery"), 37, Ink, true);
            Text(new Rect(25, 150, split - 48, 58), L("Paketler sende.\nMahalle biraz karışık.", "Precious parcels.\nA wobbly little ride."), 18, Ink);
            Text(new Rect(25, 225, split - 48, 30), game.Coins + L(" jeton", " coins"), 22, Teal, true);
            if (Button(new Rect(24, H - 82, split - 48, 52), game.IsTurkish ? "TR  →  EN" : "EN  →  TR", Pale, Ink)) game.ToggleLanguage();
            Panel(new Rect(split, 20, W - split - 8, H - 40), Alpha(Cream, .96f));
            float x = split + 16, width = W - split - 40;
            if (Button(new Rect(x, 52, width, 60), game.HasSavedRun ? L("Devam et  →", "Resume delivery  →") : L("Yola çık  →", "Start delivering  →"), Teal, Color.white, 20))
            { if (game.HasSavedRun) game.ResumeRun(); else game.StartRun(); }
            if (Button(new Rect(x, 134, width, 56), L("Garaj ve şehir", "Garage & city") + (game.BusinessReadyCount > 0 ? " • " + game.BusinessReadyCount + L(" ödül", " rewards") : ""), Pale, Ink, 20)) game.OpenGarage();
            if (Button(new Rect(x, 212, width, 56), L("Ayarlar", "Settings"), Pale, Ink, 20)) game.OpenSettings();
            if (game.IsPlaytest)
            { if (Button(new Rect(x, 284, width, 52), L("Deneme: +50.000 jeton", "Playtest: +50,000 coins"), Gold, Ink, 17)) game.AddPlaytestCoins(); }
            else Text(new Rect(x, 293, width, 27), GameText.VersionLabel, 16, Muted, false, TextAnchor.MiddleCenter);
        }

        static void Playing()
        {
            Panel(Phone.GameHeader, Alpha(Cream, .97f));
            if (Button(Phone.GamePause, L("Mola", "Pause"), Pale, Ink, 16)) game.PauseGame();
            string destination = game.IsParcelDropped ? L("Düşen paketin", "Your dropped parcel") : game.TargetName;
            Text(new Rect(84, 16, W - 252, 28), destination, 20, game.IsParcelDropped ? Coral : Ink, true);
            Text(new Rect(84, 46, W - 252, 24), game.ParcelName, 16, Muted);
            Text(new Rect(W - 156, 18, 132, 28), game.Coins + L(" jeton", " coins"), 20, Ink, true, TextAnchor.MiddleRight);
            bool urgent = game.HasPackage && game.IsUrgentOrder;
            Text(new Rect(W - 156, 54, 132, 25), (urgent ? L("Acil ", "Rush ") : L("Bonus ", "Bonus ")) + Mathf.CeilToInt(game.TimeLeft) + L(" sn", " s"),
                16, urgent || game.TimeLeft <= 10 ? Coral : Teal, urgent || game.TimeLeft <= 10, TextAnchor.MiddleRight);
            string cargoStatus = game.HasPackage && game.IsChilledOrder
                ? L("Soğukluk ", "Chill ") + game.ChillPercent + "%" : L("Sağlamlık ", "Condition ") + Mathf.RoundToInt(game.Cargo) + "%";
            Text(new Rect(84, 76, 159, 24), cargoStatus, 16, Ink);
            Rounded(new Rect(250, 83, Mathf.Max(80, W - 420), 9), Hex(0xD5E2D7), 4);
            Rounded(new Rect(250, 83, Mathf.Max(80, W - 420) * Mathf.Clamp01(game.Cargo / 100), 9), game.Cargo >= 80 ? Teal : game.Cargo >= 40 ? Gold : Coral, 4);
            MiniMap(); TargetMarker();
            if (game.DroneOwned && game.DroneBusy)
            {
                Rect cue = new Rect(W - 174, H - 37, 162, 29);
                Rounded(cue, Alpha(Cream, .96f), 10);
                Text(cue, "Drone " + Mathf.RoundToInt(game.DroneProgress * 100) + "%", 16, Teal, true, TextAnchor.MiddleCenter);
            }
            if (game.RentalVehicle >= 0)
            {
                Rect rental = new Rect(12, H - 36, W - 198, 26);
                Rounded(rental, Alpha(Cream, .95f), 9);
                Text(rental, game.RentalWaitingForDelivery ? L("Kiralık • teslimattan sonra biter", "Rental • ends after delivery") :
                    L("Kiralık ", "Rental ") + Mathf.CeilToInt(game.RentalRemaining) + L(" sn", " s"), 16, Teal, true, TextAnchor.MiddleCenter);
            }
            if (game.IsParcelDropped)
            {
                if (!game.AdsReady || !game.CanRecallParcel)
                {
                    Panel(new Rect(12, H - 104, W - 136, 60), Alpha(Cream, .98f));
                    Text(new Rect(24, H - 97, W - 160, 48), L("PAKETİN DÜŞTÜ. Işıklı halkaya dönüp al.", "PARCEL DROPPED. Return to its glowing circle."), 17, Coral, true, TextAnchor.MiddleLeft);
                }
            }
            if (game.AdsReady && game.CanRecallParcel && Button(GameplayRewardRect(), RewardLabel("Paketini geri çağır • reklam", "Recall parcel • ad"), Coral, Color.white, 17)) game.RequestParcelRecall();
            else if (game.AdsReady && game.TimeLeft <= 10 && game.CanExtraTime && Button(GameplayRewardRect(), RewardLabel("+15 saniye • reklam", "+15 seconds • ad"), Teal, Color.white, 18)) game.RequestExtraTime();
            if (game.NoticeTime > 0 && !string.IsNullOrEmpty(game.Notice))
            {
                Panel(new Rect(12, 116, W - 24, 58), Alpha(Ink, .94f));
                Text(new Rect(24, 123, W - 48, 45), game.Notice, 16, Color.white, false, TextAnchor.MiddleCenter);
            }
        }

        static Rect GameplayRewardRect() => new Rect(12, H - 102, W - 136, 56);

        static void TargetMarker()
        {
            if (!game.GameCamera) return;
            Vector3 screen = game.GameCamera.WorldToScreenPoint(game.TargetPosition + Vector3.up * 2.15f);
            if (screen.z <= 0) return;
            Vector2 p = GuiViewport.ScreenToDesign(new Vector2(screen.x, screen.y));
            p.x = Mathf.Clamp(p.x, 102, W - 102); p.y = Mathf.Clamp(p.y, 133, H - 24);
            Color color = game.IsParcelDropped ? Coral : Teal;
            Rounded(new Rect(p.x - 94, p.y - 17, 188, 34), color, 12);
            Text(new Rect(p.x - 89, p.y - 16, 178, 32), game.TargetName, 16, Color.white, true, TextAnchor.MiddleCenter);
        }

        static void MiniMap()
        {
            Rect map = Phone.MiniMap;
            Panel(map, Alpha(Cream, .95f));
            Rect land = new Rect(map.x + 8, map.y + 8, map.width - 16, map.height - 16);
            Rounded(land, Hex(0xD6E6CC), 8);
            foreach (float v in new[] { -16f, -8f, 0f, 8f, 16f })
            {
                Vector2 p = Map(new Vector3(v, 0, v), land);
                Fill(new Rect(p.x - 1.5f, land.y + 2, 3, land.height - 4), Cream);
                Fill(new Rect(land.x + 2, p.y - 1.5f, land.width - 4, 3), Cream);
            }
            var street = game.StreetScene;
            if (street != null && street.HasWorksite) { Vector2 p = Map(street.WorksitePosition, land); Disc(new Rect(p.x - 5, p.y - 5, 10, 10), Gold); }
            if (street != null && street.HasTruck && street.TruckCollider && street.TruckCollider.enabled && street.TruckCollider.gameObject.activeInHierarchy)
            { Vector2 p = Map(street.TruckPosition, land); Rounded(new Rect(p.x - 6, p.y - 4, 12, 8), street.WarningActive ? Gold : Hex(0x567E91), 2); }
            Vector2 target = Map(game.TargetPosition, land); Disc(new Rect(target.x - 5, target.y - 5, 10, 10), Coral);
            if (game.PlayerTransform) { Vector2 p = Map(game.PlayerTransform.position, land); Disc(new Rect(p.x - 5, p.y - 5, 10, 10), Color.white); Disc(new Rect(p.x - 3, p.y - 3, 6, 6), Teal); }
        }

        static Vector2 Map(Vector3 p, Rect rect) => new Vector2(Mathf.Lerp(rect.x + 4, rect.xMax - 4, Mathf.InverseLerp(-WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent, p.x)), Mathf.Lerp(rect.yMax - 4, rect.y + 4, Mathf.InverseLerp(-WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent, p.z)));

        static void Result()
        {
            Dim(.12f); Panel(new Rect(12, 12, W - 24, H - 24), Cream);
            Face(new Rect(28, 25, 48, 48), game.LastIntegrity);
            Text(new Rect(91, 24, W - 120, 36), game.ResultTitle, 24, Ink, true);
            Text(new Rect(91, 66, W - 120, 25), game.LastParcelName + " • " + GameText.ConditionName(game.LastIntegrity), 16, Muted);
            bool canDouble = game.Completed >= 3 && game.AdsReady && game.CanDoubleReward;
            Rect body = new Rect(20, 100, W - 40, H - 180);
            float width = body.width - 24;
            BeginScroll(body, canDouble ? 270 : 195);
            Text(new Rect(8, 0, width - 16, 48), game.ResultLine, 18, Ink, false, TextAnchor.MiddleCenter);
            ReceiptRow(59, L("Teslimat ücreti", "Delivery fee") + (game.LastMultiplier == 2 ? " ×2" : ""), "+" + game.LastBaseFee * game.LastMultiplier, false, width);
            ReceiptRow(91, L("Hız + sağlamlık", "Speed + care") + (game.LastMultiplier == 2 ? " ×2" : ""), "+" + (game.LastSpeedBonus + game.LastCareBonus) * game.LastMultiplier, false, width);
            ReceiptRow(123, L("Nazik seri bonusu", "Care streak bonus"), "+" + game.LastComboBonus, false, width);
            ReceiptRow(155, L("Kazandın", "You earned"), game.LastPayout + L(" jeton", " coins"), true, width);
            if (canDouble && Button(new Rect(0, 207, width, 52), RewardLabel("Tur kazancını ×2 yap • reklam", "Double round earnings • ad"), Gold, Ink, 17)) game.RequestDoubleReward();
            GUI.EndScrollView();
            float side = (W - 48) * .25f, primary = (W - 48) * .5f;
            if (Button(new Rect(20, H - 68, primary, 52), game.Completed >= 3 ? L("Yeni tur  →", "New round  →") : L("Sıradaki paket  →", "Next parcel  →"), Teal, Color.white, 18))
            { if (game.Completed >= 3) game.RestartRun(); else game.ContinueDelivery(); }
            if (Button(new Rect(28 + primary, H - 68, side, 52), L("Menü", "Menu"), Pale, Ink, 18)) game.ShowMenu();
            if (Button(new Rect(36 + primary + side, H - 68, side, 52), L("Garaj", "Garage"), Pale, Ink, 18)) game.OpenGarage();
        }

        static void ReceiptRow(float y, string name, string amount, bool total = false, float width = 0)
        {
            if (width <= 0) width = W;
            Text(new Rect(8, y, width - 202, 28), name, total ? 20 : 18, total ? Teal : Muted, total);
            Text(new Rect(width - 190, y, 182, 28), amount, total ? 22 : 20, total ? Teal : Ink, true, TextAnchor.MiddleRight);
        }

        static void ColorStore()
        {
            Dim(); Panel(new Rect(4, 4, W - 8, H - 8), Cream);
            Text(new Rect(16, 17, W - 290, 36), L("Scooter renkleri", "Scooter colors"), 24, Ink, true);
            Text(new Rect(W - 275, 22, 129, 28), game.Coins + L(" jeton", " coins"), 18, Ink, true, TextAnchor.MiddleRight);
            if (Button(new Rect(W - 132, 8, 120, 52), L("← Geri", "← Back"), Cream, Ink, 18)) game.CloseShop();
            float width = (W - 48) / 3;
            string[] names = { L("Nane", "Mint"), L("Mercan", "Coral"), L("Güneş", "Sunshine") };
            for (int i = 0; i < 3; i++)
            {
                Rect card = new Rect(12 + i * (width + 12), 83, width, 206);
                bool selected = game.ScooterColorIndex == i, owned = game.IsColorUnlocked(i), allowed = !selected && (owned || game.Coins >= game.ColorPrice(i));
                Rounded(card, selected ? Hex(0xDCEDE2) : Cream, 15);
                Scooter(new Rect(card.center.x - 46, card.y + 20, 92, 59), CourierGame.ScooterColors[i]);
                Text(new Rect(card.x + 10, card.y + 96, width - 20, 30), names[i], 21, Ink, true, TextAnchor.MiddleCenter);
                Text(new Rect(card.x + 10, card.y + 137, width - 20, 49), selected ? L("Seçili", "Selected") : owned ? L("Kullan", "Use color") : game.ColorPrice(i) + L(" jeton", " coins"), 18, selected ? Teal : Muted, false, TextAnchor.MiddleCenter);
                if (CardHit(card, allowed)) game.SelectColor(i);
            }
            Text(new Rect(16, 306, W - 32, 39), L("Renkler, teslimattan kazandığın jetonlarla açılır.", "Unlock colors with coins earned on deliveries."), 16, Muted, false, TextAnchor.MiddleCenter);
        }

        static void Settings()
        {
            OverlayHeader(L("Ayarlar", "Settings"));
            Rect area = Phone.OverlayBody; float width = area.width - 24;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && resetStage == 0 && !game.AdPreferencesOpen;
            BeginScroll(area, 545);
            Text(new Rect(8, 3, width - 16, 26), L("Ses düzeyi", "Volume") + "  " + Mathf.RoundToInt(game.Volume * 100) + "%", 19, Ink, true);
            Rect slider = new Rect(8, 35, width - 16, 52);
            Rounded(new Rect(slider.x + 14, slider.y + 22, slider.width - 28, 8), Hex(0xD5E2D7), 4);
            Rounded(new Rect(slider.x + 14, slider.y + 22, (slider.width - 28) * game.Volume, 8), Teal, 4);
            Disc(new Rect(slider.x + 14 + (slider.width - 28) * game.Volume - 14, slider.y + 12, 28, 28), Teal);
            float volume = GUI.HorizontalSlider(slider, game.Volume, 0, 1, sliderStyle, thumbStyle);
            if (Mathf.Abs(volume - game.Volume) > .001f) game.SettingVolume(volume);
            if (Button(new Rect(0, 104, width, 62), L("Hareketi azalt: ", "Reduce motion: ") + (game.ReducedMotion ? L("Açık", "On") : L("Kapalı", "Off")), Pale, Ink, 18, resetStage == 0)) game.ToggleReducedMotion();
            if (Button(new Rect(0, 181, width, 52), game.IsTurkish ? "Türkçe  →  English" : "English  →  Türkçe", Pale, Ink, 18, resetStage == 0)) game.ToggleLanguage();
            if (Button(new Rect(0, 248, width, 52), L("Nasıl oynanır?", "How to play"), Pale, Ink, 18, resetStage == 0)) game.OpenTutorial();
            if (Button(new Rect(0, 315, width, 52), L("Gizlilik", "Privacy"), Pale, Ink, 18, resetStage == 0)) game.OpenPrivacy();
            if (Button(new Rect(0, 382, width, 52), L("Reklam tercihleri", "Ad preferences"), Pale, Ink, 18, resetStage == 0)) game.OpenAdPreferences();
            if (Button(new Rect(0, 463, width, 52), L("İlerlemeyi sıfırla", "Reset progress"), Hex(0xF8E6DE), Coral, 18, resetStage == 0)) resetStage = 1;
            GUI.EndScrollView();
            if (Button(Phone.OverlayFooter, L("← Geri", "← Back"), Teal, Color.white, 19, resetStage == 0)) game.CloseOverlay();
            GUI.enabled = previousEnabled;
            if (resetStage > 0) ResetConfirmation();
            else if (game.AdPreferencesOpen) AdPreferences();
        }

        public static void DrawAdPreferences(CourierGame target) => Frame(target, AdPreferences);

        static void AdPreferences()
        {
            Dim(.45f);
            float width = Mathf.Min(640, W - 24), height = Mathf.Min(340, H - 24);
            float x = (W - width) * .5f, y = (H - height) * .5f;
            Panel(new Rect(x, y, width, height), Cream);
            Text(new Rect(x + 20, y + 18, width - 40, 38), L("Reklam tercihleri", "Ad preferences"), 25, Ink, true);
            if (!game.CommerceIsMock)
            {
                Text(new Rect(x + 20, y + 78, width - 40, 128), L("Reklam gizlilik ekranı şu anda kullanılamıyor. Hazır olduğunda tercihlerini izin yöneticisinden değiştirebilirsin.", "The ad privacy screen is currently unavailable. When ready, you can update your choices through the privacy manager."), 19, Muted);
                if (Button(new Rect(x + 20, y + height - 68, width - 40, 52), L("← Geri", "← Back"), Pale, Ink, 18)) game.CloseAdPreferences();
                return;
            }
            Text(new Rect(x + 20, y + 70, width - 40, 99), L("Kişiselleştirilmiş reklam tercihini bu cihazda saklarız. Sistem izinleri ve bölgesel gizlilik seçimleri ayrıca geçerlidir.", "Your personalized-ad preference is stored on this device. System permissions and regional privacy choices also apply."), 18, Muted);
            float half = (width - 52) * .5f;
            if (Button(new Rect(x + 20, y + height - 136, half, 52), L("Kişiselleştirme", "No personalization"), game.PersonalizedAds ? Pale : Teal, game.PersonalizedAds ? Ink : Color.white, 17)) game.SetAdPrivacy(false);
            if (Button(new Rect(x + 32 + half, y + height - 136, half, 52), L("Kişiselleştirmeyi tercih et", "Prefer personalization"), game.PersonalizedAds ? Teal : Pale, game.PersonalizedAds ? Color.white : Ink, 17)) game.SetAdPrivacy(true);
            if (Button(new Rect(x + 20, y + height - 68, width - 40, 52), L("← Geri", "← Back"), Pale, Ink, 18)) game.CloseAdPreferences();
        }

        static void ResetConfirmation()
        {
            Dim(.45f); Panel(new Rect(12, 20, W - 24, H - 40), Cream);
            Text(new Rect(30, 40, W - 60, 40), resetStage == 1 ? L("Yeni bir başlangıç mı?", "A fresh start?") : L("Son kez onayla", "One last confirmation"), 25, Ink, true);
            Text(new Rect(30, 104, W - 60, 121), resetStage == 1 ? L("Kazanılmış jetonlar, jetonla açtığın ürünler ve ayarlar sıfırlanır. Satın alınmış kalıcı haklar ve harcanmamış satın alma jetonları korunur.", "Earned coins, coin-unlocked items and settings reset. Purchased permanent rights and unspent purchased coins are kept.") : L("Bu işlem geri alınamaz. Kazanılmış ilerlemeni ve ayarlarını sıfırlamak istediğinden emin misin?", "This cannot be undone. Reset your earned progress and settings?"), 19, Muted);
            float width = (W - 72) * .5f;
            if (Button(new Rect(28, H - 90, width, 52), L("Vazgeç", "Cancel"), Pale, Ink, 19)) resetStage = 0;
            if (Button(new Rect(44 + width, H - 90, width, 52), resetStage == 1 ? L("Devam et", "Continue") : L("Evet, sıfırla", "Yes, reset"), Coral, Color.white, 19))
            { if (resetStage == 1) resetStage = 2; else { resetStage = 0; game.ResetProgress(); } }
        }

        static void Privacy()
        {
            OverlayHeader(L("Gizlilik bilgileri", "Privacy information"));
            Rect area = Phone.OverlayBody; float width = area.width - 24;
            SetLabel(18, Ink, false, TextAnchor.UpperLeft);
            string policy = game.PrivacyText ?? "";
            float height = label.CalcHeight(new GUIContent(policy), width - 16) + 24;
            BeginScroll(area, height);
            Text(new Rect(8, 4, width - 16, height), policy, 18, Ink);
            GUI.EndScrollView();
            Rect footer = Phone.OverlayFooter;
            if (game.SupportAvailable)
            {
                if (Button(new Rect(footer.x, footer.y, (footer.width - 12) * .5f, 52), L("← Geri", "← Back"), Teal, Color.white, 18)) game.CloseOverlay();
                if (Button(new Rect(footer.center.x + 6, footer.y, (footer.width - 12) * .5f, 52), L("Destek", "Support"), Pale, Ink, 18)) game.OpenSupport();
            }
            else if (Button(footer, L("← Geri", "← Back"), Teal, Color.white, 19)) game.CloseOverlay();
        }

        static void Paused()
        {
            Dim(.3f); float width = Mathf.Min(440, W - 24), x = (W - width) * .5f;
            Panel(new Rect(x, 20, width, H - 40), Cream);
            Text(new Rect(x + 18, 38, width - 36, 44), L("Kısa bir mola.", "A little breather."), 27, Ink, true, TextAnchor.MiddleCenter);
            if (Button(new Rect(x + 20, 103, width - 40, 56), L("Teslimata dön  →", "Resume delivery  →"), Teal, Color.white, 20)) game.ResumeGame();
            if (Button(new Rect(x + 20, 178, width - 40, 52), L("Ayarlar", "Settings"), Pale, Ink, 19)) game.OpenSettings();
            if (Button(new Rect(x + 20, 252, width - 40, 52), L("Kaydet ve menüye dön", "Save and return to menu"), Pale, Ink, 18)) game.ShowMenu();
        }

        static void Tutorial()
        {
            Dim(); Panel(new Rect(4, 4, W - 8, H - 8), Cream);
            Text(new Rect(16, 15, W - 160, 35), L("Küçük kurye rehberi", "Your courier guide"), 23, Ink, true);
            if (Button(new Rect(W - 132, 8, 120, 52), L("Atla", "Skip"), Cream, Ink, 18)) game.FinishTutorial();
            int page = Mathf.Clamp(game.TutorialPage, 0, 2);
            string[] titles = { L("Paketini al, yola çık.", "Pick up. Roll out."), L("Hızlı ol, nazik ol.", "Quick ride. Gentle hands."), L("Kazandığına renk kat.", "Give your coins some color.") };
            string[] body = {
                L("İşaretli alım noktasının halkasından paketini al. Sürükleyerek veya WASD / ok tuşlarıyla sür. Hedef evin halkasında teslimat otomatik yapılır.", "Collect your parcel at the marked pickup circle. Drag to steer, or use WASD / arrows. Enter the target home's circle to deliver automatically."),
                L("Pasta sallanır, bitki ağırdır, çay takımı kırılgandır. Darbeler bonusunu azaltır. Düşen paketi halkasından geri al. Süre bitse de teslimat ücretini kazanırsın.", "Cake wobbles, plants are heavy, tea is fragile. Crashes reduce your bonus. Pick up dropped parcels from their circle. Late deliveries still earn a fee."),
                L("Garajda aracını ve dekorlarını seç. Drone ek jeton kazanır. Mağaza ve isteğe bağlı reklam seçenekleri hazır olduklarında açılır.", "Choose your ride and decor in the garage. A drone earns extra coins. Store and optional-ad choices open when available.") };
            Text(new Rect(16, 70, W - 32, 36), titles[page], 25, Ink, true);
            Rounded(new Rect(16, 116, W - 32, 77), Pale, 16);
            float a = W * .2f, b = W * .5f, c = W * .8f;
            if (page == 0)
            {
                Parcel(new Rect(a - 19, 131, 38, 36), Gold); Scooter(new Rect(b - 33, 127, 66, 43), Teal); House(new Rect(c - 22, 129, 44, 42), Coral);
            }
            else if (page == 1)
            {
                Parcel(new Rect(a - 19, 131, 38, 36), Gold); DecorIcon(0, new Rect(b - 22, 124, 44, 48), Teal); Rounded(new Rect(c - 18, 132, 36, 32), Cream, 7);
            }
            else
            {
                Disc(new Rect(a - 18, 130, 36, 36), Gold); Drone(new Rect(b - 32, 125, 64, 50), Teal); DecorIcon(4, new Rect(c - 22, 125, 44, 47), Coral);
            }
            Text(new Rect(18, 206, W - 36, 74), body[page], 18, Ink);
            if (Button(new Rect(12, H - 64, (W - 36) * .35f, 52), L("← Önceki", "← Previous"), Pale, Ink, 17, page > 0)) game.PreviousTutorial();
            if (Button(new Rect(24 + (W - 36) * .35f, H - 64, (W - 36) * .65f, 52), page == 2 ? L("Hazırım →", "Ready to roll →") : L("Sonraki →", "Next →"), Teal, Color.white, 19))
            { if (page == 2) game.FinishTutorial(); else game.NextTutorial(); }
        }

        static void OverlayHeader(string title)
        {
            Dim(); Panel(new Rect(4, 4, W - 8, H - 8), Cream);
            Text(new Rect(16, 17, W - 32, 39), title, 26, Ink, true);
        }

        public static void DrawGarage(CourierGame target) => Frame(target, Garage);

        static void Garage()
        {
            bool compact = GuiViewport.Compact;
            if (compact && roomPreview)
            {
                if (Button(new Rect(12, 8, 190, 52), L("← Dükkâna dön", "← Back to shop"), Cream, Ink, 18)) { roomPreview = false; finger = -1; }
                Panel(new Rect(W - 174, 8, 162, 52), Alpha(Cream, .96f));
                Text(new Rect(W - 164, 17, 142, 34), game.Coins + L(" jeton", " coins"), 20, Teal, true, TextAnchor.MiddleRight);
                return;
            }
            Dim(.05f);
            Rect panel = compact ? new Rect(4, 4, W - 8, H - 8) : new Rect(866, 124, 386, 620);
            Panel(panel, Alpha(Cream, .97f));
            Rect tabs, content;
            if (compact)
            {
                if (Button(Phone.GarageBack, L("← Geri", "← Back"), Pale, Ink, 18)) game.CloseOverlay();
                if (Button(new Rect(130, 8, 160, 52), L("Odayı gör", "View room"), Pale, Ink, 18)) { roomPreview = true; finger = -1; }
                Text(new Rect(W - 166, 22, 146, 30), game.Coins + L(" jeton", " coins"), 18, Teal, true, TextAnchor.MiddleRight);
                tabs = Phone.GarageTabs; content = Phone.GarageContent;
            }
            else
            {
                Text(new Rect(884, 143, 350, 36), L("Küçük garaj, büyük tarz.", "Small garage. Big style."), 24, Ink, true);
                Text(new Rect(884, 184, 350, 26), game.Coins + L(" jeton", " coins"), 19, Teal, true);
                tabs = new Rect(884, 222, 350, 52); content = new Rect(884, 288, 350, 370);
                if (Button(new Rect(884, 678, 350, 48), L("← Geri", "← Back"), Teal, Color.white, 18)) game.CloseOverlay();
                Text(new Rect(43, 713, 600, 24), L("Depo senin. Tarzı da öyle.", "Your depot. Your own little style."), 18, Ink, true);
                Text(new Rect(28, 778, 500, 18), GameText.VersionLabel, 12, Muted);
            }
            string[] names = { L("Araçlar", "Vehicles"), "Drone", L("Dekor", "Decor"), L("İşler", "Jobs"), L("Mağaza", "Store") };
            if (game.BusinessReadyCount > 0) names[3] += " (" + game.BusinessReadyCount + ")";
            float tabWidth = (tabs.width - 24) / 5;
            for (int i = 0; i < 5; i++)
                if (Button(new Rect(tabs.x + i * (tabWidth + 6), tabs.y, tabWidth, tabs.height), names[i], garageTab == i ? Teal : Pale, garageTab == i ? Color.white : Ink, compact ? 17 : 14))
                { garageTab = i; scroll = Vector2.zero; finger = -1; }
            if (garageTab == 0) Vehicles(content, compact);
            else if (garageTab == 1) DronePage(content, compact);
            else if (garageTab == 2) Decorations(content, compact);
            else if (garageTab == 3) BusinessPage(content, compact);
            else StorePage(content, compact);
        }

        static void Vehicles(Rect area, bool compact)
        {
            float width = area.width - 24, height = compact ? 144 : 165;
            float end = 0;
            for (int i = 0; i < game.VehicleCount; i++) end += height + 12 + (i >= 4 && !game.VehicleOwned(i) ? 64 : 0);
            BeginScroll(area, end + 139);
            float y = 0;
            for (int i = 0; i < game.VehicleCount; i++)
            {
                Rect card = new Rect(0, y, width, height);
                bool selected = game.VehicleIndex == i, owned = game.VehicleOwned(i), accessible = game.VehicleAccessible(i);
                bool allowed = !selected && game.CanChangeVehicle && (accessible || game.Coins >= game.VehiclePrice(i));
                Rounded(card, selected ? Hex(0xDCEDE2) : Pale, 14);
                Color color = i == 0 ? Teal : i == 1 ? Coral : Gold;
                if (i >= 4) FantasyIcon(new Rect(card.x + 8, card.y + 22, 70, 64), i == 4);
                else if (i == 3) Monster(new Rect(card.x + 10, card.y + 22, 64, 44), color);
                else if (i == 2) Van(new Rect(card.x + 10, card.y + 22, 64, 44), color);
                else Scooter(new Rect(card.x + 10, card.y + 22, 64, 44), color);
                float textX = card.x + 88, textWidth = card.width - 100;
                Text(new Rect(textX, card.y + 8, textWidth, 30), game.VehicleNameFor(i), compact ? 20 : 18, Ink, true);
                Text(new Rect(textX, card.y + 43, textWidth, compact ? 67 : 78), game.VehicleHintFor(i), compact ? 16 : 14, Muted);
                string action = selected ? L("Seçili", "Selected") : !game.CanChangeVehicle ? L("Önce paketi teslim et", "Deliver your parcel first") : owned ? L("Seç", "Select") : accessible ? L("Kiralık aracı seç", "Use rental") : game.VehiclePrice(i) + L(" jetonla aç", " coins to unlock");
                Text(new Rect(textX, card.yMax - 30, textWidth, 25), action, compact ? 16 : 14, selected ? Teal : allowed ? Ink : Muted, true);
                if (CardHit(card, allowed)) game.SelectVehicle(i);
                y += height + 12;
                if (i >= 4 && !owned)
                {
                    if (Button(new Rect(0, y, width, 52), RewardLabel("3 dakika dene • reklam", "Try for 3 minutes • ad"), Pale, Ink, compact ? 17 : 16, game.CanRequestRental(i))) game.RentWithAd(i);
                    y += 64;
                }
            }
            if (Button(new Rect(0, end + 21, width, 52), L("Scooter renkleri →", "Scooter colors →"), Pale, Ink, compact ? 18 : 17)) game.OpenShop();
            Text(new Rect(0, end + 87, width, 43), game.CanChangeVehicle ? game.VehicleHint : L("Aracı değiştirmek için önce paketini teslim et.", "Deliver your parcel before changing rides."), compact ? 16 : 14, Muted);
            GUI.EndScrollView();
        }

        static void StorePage(Rect area, bool compact)
        {
            float width = area.width - 24, height = compact ? 143 : 154;
            var products = game.StoreProducts;
            int count = products == null ? 0 : products.Count;
            BeginScroll(area, 124 + count * (height + 12) + 58);
            string status = game.CommerceIsMock ? L("TEST • gerçek ödeme/reklam yok", "TEST • no real payment/ad") :
                game.StoreReady ? L("Fiyatlar cihazının mağazasından gelir.", "Prices are provided by your device's store.") : L("Mağaza şu anda kullanılamıyor.", "The store is currently unavailable.");
            if (!game.CommerceIsMock && game.NoticeTime > 0 && !string.IsNullOrEmpty(game.Notice)) status = game.Notice;
            Text(new Rect(2, 0, width - 4, 52), status, compact ? 16 : 15, game.CommerceIsMock ? Coral : Muted);
            if (Button(new Rect(0, 61, width, 52), L("Satın alımları geri yükle", "Restore purchases"), Pale, Ink, compact ? 18 : 16, game.StoreReady)) game.RestorePurchases();
            for (int i = 0; i < count; i++)
            {
                StoreProduct product = products[i];
                if (product == null) continue;
                float y = 128 + i * (height + 12);
                Rect card = new Rect(0, y, width, height);
                bool owned = StoreProductOwned(product.Key);
                bool available = game.StoreReady && product.Available && !owned && !string.IsNullOrEmpty(product.LocalizedPrice);
                Rounded(card, owned ? Hex(0xDCEDE2) : Pale, 14);
                var definition = CommerceConfiguration.Find(product.Key);
                string title = definition == null ? product.Title : definition.Title;
                Text(new Rect(12, y + 12, width - 24, 52), title, compact ? 20 : 18, Ink, true);
                string detail = product.Consumable ? "+" + product.CoinAmount + L(" jeton", " coins") :
                    product.Key == CommerceConfiguration.RemoveAds ? L("Geçiş reklamları kapanır; ödüllüler isteğe bağlı.", "No interstitials; optional rewarded ads remain.") : L("Kalıcı araç erişimi", "Permanent vehicle access");
                Text(new Rect(12, y + 63, width - 24, 45), detail, compact ? 16 : 14, Muted);
                string action = owned ? L("Sende var", "Already yours") : available ? product.LocalizedPrice : L("Kullanılamıyor", "Unavailable");
                Text(new Rect(12, y + height - 31, width - 24, 26), action, compact ? 18 : 16, available ? Teal : Muted, true, TextAnchor.MiddleRight);
                if (CardHit(card, available)) game.PurchaseProduct(product.Key);
            }
            Text(new Rect(0, 136 + count * (height + 12), width, 46), L("Jeton paketleri geri yüklenmez. Kalıcı ürünler mağaza hesabınla geri yüklenir.", "Coin packs are not restored. Permanent products restore through your store account."), compact ? 16 : 14, Muted);
            GUI.EndScrollView();
        }

        static bool StoreProductOwned(string key) => key == CommerceConfiguration.RemoveAds ? game.NoInterstitialAds :
            key == CommerceConfiguration.Dragon ? game.VehicleOwned(4) : key == CommerceConfiguration.Dino ? game.VehicleOwned(5) :
            key == CommerceConfiguration.Fleet && game.VehicleOwned(4) && game.VehicleOwned(5);

        static void FantasyIcon(Rect r, bool dragon)
        {
            Color color = dragon ? Hex(0xA99BCE) : Hex(0x95BD92);
            if (dragon)
            {
                Disc(new Rect(r.x, r.y + r.height * .31f, r.width * .46f, r.height * .29f), Coral);
                Disc(new Rect(r.x + r.width * .54f, r.y + r.height * .31f, r.width * .46f, r.height * .29f), Coral);
            }
            Disc(new Rect(r.x + r.width * .23f, r.y + r.height * .38f, r.width * .54f, r.height * .56f), color);
            Disc(new Rect(r.x + r.width * .25f, r.y + r.height * .05f, r.width * .50f, r.height * .57f), color);
            Disc(new Rect(r.x + r.width * .35f, r.y + r.height * .25f, r.width * .30f, r.height * .27f), Cream);
            Disc(new Rect(r.x + r.width * .34f, r.y + r.height * .22f, r.width * .06f, r.height * .10f), Ink);
            Disc(new Rect(r.x + r.width * .60f, r.y + r.height * .22f, r.width * .06f, r.height * .10f), Ink);
            Parcel(new Rect(r.x + r.width * .36f, r.y + r.height * .57f, r.width * .28f, r.height * .27f), Gold);
        }

        static void DronePage(Rect area, bool compact)
        {
            float width = area.width - 24;
            if (compact)
            {
                BeginScroll(area, 282);
                Rounded(new Rect(0, 0, 110, 98), Pale, 17);
                Drone(new Rect(7, 9, 96, 76), game.DroneOwned ? Teal : Muted);
                Text(new Rect(126, 1, width - 126, 98), game.DroneStatus, 18, Ink);
                Text(new Rect(0, 112, width, 28), L("Görev ödülü: +", "Mission reward: +") + game.DroneReward + L(" jeton", " coins"), 18, Teal, true);
                if (game.DroneBusy)
                {
                    Rounded(new Rect(0, 157, width, 12), Hex(0xD5E2D7), 5);
                    Rounded(new Rect(0, 157, width * Mathf.Clamp01(game.DroneProgress), 12), Teal, 5);
                    Text(new Rect(0, 180, width, 29), Mathf.RoundToInt(game.DroneProgress * 100) + "%", 18, Teal, true, TextAnchor.MiddleCenter);
                }
                else if (!game.DroneOwned)
                {
                    if (Button(new Rect(0, 150, width, 56), game.DronePrice + L(" jetonla aç", " coins to unlock"), Teal, Color.white, 18, game.Coins >= game.DronePrice)) game.BuyDrone();
                }
                else if (Button(new Rect(0, 150, width, 56), L("Göreve gönder →", "Send on delivery →"), Teal, Color.white, 18, game.CanLaunchDrone)) game.LaunchDrone();
                Text(new Rect(0, 230, width, 47), L("Görev süresi sen teslimat yaparken ilerler.", "Mission time advances while you make deliveries."), 16, Muted);
                GUI.EndScrollView();
                return;
            }
            BeginScroll(area, 403);
            Rounded(new Rect(0, 0, width, 137), Pale, 17);
            Drone(new Rect(width * .5f - 54, 17, 108, 83), game.DroneOwned ? Teal : Muted);
            Text(new Rect(8, 103, width - 16, 30), L("Küçük uçan yardımcın", "Your little flying helper"), compact ? 20 : 18, Ink, true, TextAnchor.MiddleCenter);
            Text(new Rect(0, 157, width, 66), game.DroneStatus, compact ? 18 : 16, Ink);
            Text(new Rect(0, 229, width, 30), L("Görev ödülü: +", "Mission reward: +") + game.DroneReward + L(" jeton", " coins"), compact ? 18 : 16, Teal, true);
            if (game.DroneBusy)
            {
                Rounded(new Rect(0, 278, width, 12), Hex(0xD5E2D7), 5);
                Rounded(new Rect(0, 278, width * Mathf.Clamp01(game.DroneProgress), 12), Teal, 5);
                Text(new Rect(0, 299, width, 31), Mathf.RoundToInt(game.DroneProgress * 100) + "%", 18, Teal, true, TextAnchor.MiddleCenter);
            }
            else if (!game.DroneOwned)
            {
                if (Button(new Rect(0, 277, width, 56), game.DronePrice + L(" jetonla aç", " coins to unlock"), Teal, Color.white, compact ? 18 : 17, game.Coins >= game.DronePrice)) game.BuyDrone();
            }
            else if (Button(new Rect(0, 277, width, 56), L("Göreve gönder →", "Send on delivery →"), Teal, Color.white, compact ? 18 : 17, game.CanLaunchDrone)) game.LaunchDrone();
            Text(new Rect(0, 351, width, 47), L("Görev süresi sen teslimat yaparken ilerler.", "Mission time advances while you make deliveries."), compact ? 16 : 14, Muted);
            GUI.EndScrollView();
        }

        static void Decorations(Rect area, bool compact)
        {
            float width = area.width - 24;
            Rect[] cards = CardGrid(width, game.DecorCount, compact ? 3 : 2, compact ? 78 : 98, 9);
            BeginScroll(area, cards.Length == 0 ? 0 : cards[cards.Length - 1].yMax + 9);
            for (int i = 0; i < cards.Length; i++)
            {
                Rect card = cards[i]; bool owned = game.DecorOwned(i), equipped = game.DecorEquipped(i), allowed = owned || game.Coins >= game.DecorPrice(i);
                Rounded(card, equipped ? Hex(0xDCEDE2) : Pale, 12);
                DecorIcon(i, new Rect(card.x + 8, card.y + 8, 28, 34), i % 3 == 0 ? Teal : i % 3 == 1 ? Gold : Coral);
                Text(new Rect(card.x + 44, card.y + 7, card.width - 51, 42), game.DecorName(i), compact ? 16 : 15, Ink, true);
                string action = owned ? equipped ? L("Gizle", "Hide") : L("Göster", "Show") : game.DecorPrice(i) + L(" jeton", " coins");
                Text(new Rect(card.x + 9, card.yMax - 29, card.width - 18, 25), action, compact ? 16 : 14, equipped ? Teal : Muted, owned);
                if (CardHit(card, allowed)) game.SelectDecor(i);
            }
            GUI.EndScrollView();
        }

        static void BusinessPage(Rect area, bool compact)
        {
            float width = area.width - 24;
            Rect[] cards = CardGrid(width, game.BusinessCount, compact ? 2 : 1, 206, 12);
            BeginScroll(area, cards[cards.Length - 1].yMax + 55);
            for (int i = 0; i < cards.Length; i++)
            {
                Rect card = cards[i]; bool ready = game.CanClaimBusiness(i);
                Rounded(card, ready ? Hex(0xDCEDE2) : Pale, 13);
                Text(new Rect(card.x + 12, card.y + 10, card.width - 24, 29), game.BusinessName(i), 19, Ink, true);
                Text(new Rect(card.x + 12, card.y + 46, card.width - 24, 53), game.BusinessHint(i), 17, Muted);
                Text(new Rect(card.x + 12, card.y + 105, card.width - 24, 29), Mathf.Min(game.BusinessProgress(i), game.BusinessTarget(i)) + " / " + game.BusinessTarget(i) + "   •   +" + game.BusinessReward(i) + L(" jeton", " coins"), 17, ready ? Teal : Ink, true);
                if (Button(new Rect(card.x + 10, card.y + 143, card.width - 20, 52), ready ? L("Ödülü al", "Claim reward") : L("İş devam ediyor", "In progress"), ready ? Teal : Cream, ready ? Color.white : Muted, 17, ready)) game.ClaimBusiness(i);
            }
            Text(new Rect(0, cards[cards.Length - 1].yMax + 10, width, 42), L("Tamamlanan anlaşma: ", "Contracts completed: ") + game.BusinessOrders + L(" • İşler tekrar alınabilir.", " • Contracts are repeatable."), 16, Muted);
            GUI.EndScrollView();
        }

        static void Face(Rect r, float integrity)
        {
            Disc(r, integrity >= 80 ? Pale : integrity >= 40 ? Hex(0xF8EAC7) : Hex(0xF8DED7));
            Disc(new Rect(r.x + r.width * .28f, r.y + r.height * .31f, 5, 7), Ink); Disc(new Rect(r.x + r.width * .64f, r.y + r.height * .31f, 5, 7), Ink);
            if (integrity < 40) Disc(new Rect(r.center.x - 4, r.y + r.height * .61f, 8, 12), Ink);
            else Rounded(new Rect(r.x + r.width * .32f, r.y + r.height * .65f, r.width * .38f, integrity >= 80 ? 6 : 3), Ink, 3);
        }

        static void Parcel(Rect r, Color color) { Rounded(r, color, r.width * .15f); Fill(new Rect(r.x + r.width * .43f, r.y, r.width * .14f, r.height), Alpha(Cream, .8f)); }
        static void Scooter(Rect r, Color color)
        {
            float s = r.width / 90;
            Disc(new Rect(r.x + 5 * s, r.y + 33 * s, 22 * s, 22 * s), Ink); Disc(new Rect(r.x + 64 * s, r.y + 33 * s, 22 * s, 22 * s), Ink);
            Rounded(new Rect(r.x + 16 * s, r.y + 28 * s, 58 * s, 13 * s), color, 6 * s); Rounded(new Rect(r.x + 64 * s, r.y + 5 * s, 8 * s, 34 * s), color, 4 * s);
            Rounded(new Rect(r.x + 59 * s, r.y + 3 * s, 20 * s, 5 * s), Ink, 2 * s); Parcel(new Rect(r.x + 4 * s, r.y + 8 * s, 23 * s, 22 * s), Gold);
        }
        static void Van(Rect r, Color color)
        {
            float s = r.width / 90;
            Rounded(new Rect(r.x + 3 * s, r.y + 3 * s, 57 * s, 39 * s), color, 7 * s);
            Rounded(new Rect(r.x + 53 * s, r.y + 16 * s, 34 * s, 26 * s), color, 7 * s);
            Rounded(new Rect(r.x + 60 * s, r.y + 17 * s, 17 * s, 12 * s), Cream, 3 * s);
            Disc(new Rect(r.x + 12 * s, r.y + 33 * s, 20 * s, 20 * s), Ink);
            Disc(new Rect(r.x + 61 * s, r.y + 33 * s, 20 * s, 20 * s), Ink);
            Parcel(new Rect(r.x + 20 * s, r.y + 12 * s, 21 * s, 19 * s), Cream);
        }
        static void Monster(Rect r, Color color)
        {
            float s = r.width / 90;
            Rounded(new Rect(r.x + 13 * s, r.y + 5 * s, 65 * s, 29 * s), color, 6 * s);
            Rounded(new Rect(r.x + 40 * s, r.y - 3 * s, 27 * s, 22 * s), color, 5 * s);
            Fill(new Rect(r.x + 46 * s, r.y + 1 * s, 13 * s, 11 * s), Cream);
            foreach (float x in new[] { 7f, 58f })
            { Disc(new Rect(r.x + x * s, r.y + 22 * s, 29 * s, 29 * s), Ink); Disc(new Rect(r.x + (x + 8) * s, r.y + 30 * s, 13 * s, 13 * s), Gold); }
        }
        static void House(Rect r, Color color) { Rounded(r, color, 8); Rounded(new Rect(r.center.x - 7, r.yMax - 24, 14, 24), Cream, 3); Rounded(new Rect(r.x - 4, r.y - 6, r.width + 8, 12), Ink, 5); }
        static void Drone(Rect r, Color color)
        {
            float s = r.width / 100;
            Rounded(new Rect(r.x + 18 * s, r.y + 12.5f * s, 64 * s, 5 * s), color, 2.5f * s);
            Rounded(new Rect(r.x + 18 * s, r.y + 57.5f * s, 64 * s, 5 * s), color, 2.5f * s);
            Rounded(new Rect(r.x + 47.5f * s, r.y + 15 * s, 5 * s, 45 * s), color, 2.5f * s);
            foreach (Vector2 p in new[] { new Vector2(18, 15), new Vector2(82, 15), new Vector2(18, 60), new Vector2(82, 60) })
            { Disc(new Rect(r.x + (p.x - 15) * s, r.y + (p.y - 8) * s, 30 * s, 16 * s), Alpha(Ink, .3f)); Rounded(new Rect(r.x + (p.x - 11) * s, r.y + (p.y - 2) * s, 22 * s, 4 * s), Ink, 2 * s); }
            Rounded(new Rect(r.x + 33 * s, r.y + 26 * s, 34 * s, 26 * s), color, 8 * s); Parcel(new Rect(r.x + 40 * s, r.y + 46 * s, 20 * s, 19 * s), Gold);
        }
        static void DecorIcon(int index, Rect r, Color color)
        {
            float s = r.width / 40;
            if (index == 0) { Rounded(new Rect(r.x + 8 * s, r.y + 24 * s, 25 * s, 22 * s), Coral, 4); Disc(new Rect(r.x, r.y + 2 * s, 25 * s, 18 * s), color); Disc(new Rect(r.x + 17 * s, r.y + 10 * s, 23 * s, 18 * s), color); }
            else if (index == 1) { Rounded(new Rect(r.x + 18 * s, r.y + 10 * s, 5 * s, 33 * s), Ink, 2); Rounded(new Rect(r.x + 2 * s, r.y + 2 * s, 37 * s, 21 * s), color, 6); }
            else if (index == 2) { Rounded(new Rect(r.x, r.y + 9 * s, 40 * s, 28 * s), color, 8); Rounded(new Rect(r.x + 7 * s, r.y + 16 * s, 26 * s, 13 * s), Cream, 4); }
            else if (index == 3 || index == 4) { Rounded(new Rect(r.x + 2 * s, r.y + 3 * s, 5 * s, 39 * s), color, 2); Rounded(new Rect(r.x + 33 * s, r.y + 3 * s, 5 * s, 39 * s), color, 2); Rounded(new Rect(r.x, r.y + 12 * s, 40 * s, 7 * s), color, 2); if (index == 4) Rounded(new Rect(r.x, r.y + 32 * s, 40 * s, 5 * s), color, 2); }
            else if (index == 6) { Rounded(new Rect(r.x + 5 * s, r.y + 3 * s, 30 * s, 30 * s), color, 7); Rounded(new Rect(r.x, r.y + 21 * s, 40 * s, 15 * s), color, 5); }
            else if (index == 8) { Rounded(new Rect(r.x, r.y + 8 * s, 40 * s, 31 * s), color, 8); Text(new Rect(r.x, r.y + 10 * s, 40 * s, 26 * s), "H", 18, Cream, true, TextAnchor.MiddleCenter); }
            else if (index == 9 || index == 10) { Rounded(r, color, 3); Disc(new Rect(r.x + 6 * s, r.y + 9 * s, 28 * s, 25 * s), Cream); Disc(new Rect(r.x + 12 * s, r.y + 18 * s, 4 * s, 5 * s), Ink); Disc(new Rect(r.x + 24 * s, r.y + 18 * s, 4 * s, 5 * s), Ink); }
            else if (index == 11) { Rounded(r, color, 3); for (int i = 0; i < 4; i++) Fill(new Rect(r.x + (4 + i * 8) * s, r.y + (14 - i % 2 * 7) * s, 6 * s, 22 * s), Cream); }
            else { Rounded(new Rect(r.x + 3 * s, r.y + 3 * s, 34 * s, 37 * s), color, 5); Parcel(new Rect(r.x + 11 * s, r.y + 11 * s, 18 * s, 17 * s), Cream); }
        }

        static bool CardHit(Rect rect, bool enabled)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, hit); GUI.enabled = previous;
            return clicked && Time.unscaledTime >= suppressClickUntil;
        }
        static bool Button(Rect r, string value, Color background, Color foreground, int size = 18, bool enabled = true)
        {
            Color fill = enabled && GUI.enabled && r.Contains(Event.current.mousePosition) ? Color.Lerp(background, Color.white, .1f) : background;
            Rounded(new Rect(r.x, r.y + 3, r.width, r.height), Alpha(Ink, .1f), 12); Rounded(r, fill, 12);
            Text(r, value, size, enabled ? foreground : Muted, true, TextAnchor.MiddleCenter);
            return CardHit(r, enabled);
        }
        static void Panel(Rect r, Color color) { Rounded(new Rect(r.x, r.y + 4, r.width, r.height), Alpha(Ink, .12f), 18); Rounded(r, color, 18); }
        static void Dim(float alpha = .24f) => Fill(GuiViewport.FullScreenRect, new Color(.06f, .16f, .19f, alpha));
        static void SetLabel(int size, Color color, bool bold, TextAnchor anchor)
        { label.fontSize = GuiViewport.Compact ? Mathf.Max(16, size) : size; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.alignment = anchor; label.normal.textColor = color; }
        static void Text(Rect r, string value, int size, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
        { SetLabel(size, color, bold, anchor); GUI.Label(r, value, label); }
        static void Line(Vector2 a, Vector2 b, float thickness, Color color)
        { Matrix4x4 matrix = GUI.matrix; Vector2 delta = b - a; GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a); Fill(new Rect(a.x, a.y - thickness * .5f, delta.magnitude, thickness), color); GUI.matrix = matrix; }
        static void Rounded(Rect r, Color color, float radius)
        {
            if (r.width <= 0 || r.height <= 0) return;
            radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
            if (radius < .5f) { Fill(r, color); return; }
            Fill(new Rect(r.x + radius, r.y, r.width - 2 * radius, r.height), color); Fill(new Rect(r.x, r.y + radius, r.width, r.height - 2 * radius), color);
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, radius, radius), circle, new Rect(0, .5f, .5f, .5f)); GUI.DrawTextureWithTexCoords(new Rect(r.xMax - radius, r.y, radius, radius), circle, new Rect(.5f, .5f, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - radius, radius, radius), circle, new Rect(0, 0, .5f, .5f)); GUI.DrawTextureWithTexCoords(new Rect(r.xMax - radius, r.yMax - radius, radius, radius), circle, new Rect(.5f, 0, .5f, .5f)); GUI.color = previous;
        }
        static void Fill(Rect r, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        static void Disc(Rect r, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, circle); GUI.color = old; }
        static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        static Color Hex(uint value) => new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);
    }
}
