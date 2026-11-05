using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Package-free, resolution-independent interface for the playable courier sketch.</summary>
    public sealed class CourierHUD : MonoBehaviour
    {
        public CourierGame Game;
        public Func<int, bool> OwnsColor;
        // Optional floating stick, in ordinary screen coordinates (origin at bottom-left).
        public bool StickVisible;
        public Vector2 StickOrigin;
        public Vector2 StickPosition;

        const float Width = GuiViewport.Width;
        const float Height = GuiViewport.Height;
        readonly Color ink = Hex(0x193B4B);
        readonly Color muted = Hex(0x66818B);
        readonly Color cream = Hex(0xFFF9ED);
        readonly Color teal = Hex(0x119E99);
        readonly Color coral = Hex(0xE97760);
        readonly Color gold = Hex(0xF3BF48);
        readonly Color pale = Hex(0xEAF3EA);
        Font regular;
        Font bold;
        Texture2D circle;
        Texture2D triangle;
        GUIStyle label;
        GUIStyle hitStyle;
        static string L(string tr, string en) => GameText.Pick(tr, en);
        string RewardLabel(string tr, string en) => (Game.CommerceIsMock ? "TEST • " : "") + L(tr, en);

        void Awake()
        {
            if (!Game) Game = GetComponent<CourierGame>();
            // Unity's bundled font also works when a system font face is absent.
            regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bold = regular;
            circle = MakeCircle(64);
            triangle = MakeArrow(64);
        }

        void OnDestroy()
        {
            if (circle) Destroy(circle);
            if (triangle) Destroy(triangle);
        }

        Vector2 DesignPoint(Vector2 screenPosition) => GuiViewport.ScreenToDesign(screenPosition);

        public bool BlocksPointer(Vector2 screenPosition)
        {
            if (!Game || Game.Phase != GamePhase.Playing) return true;
            if (GuiViewport.Compact) return CompactScreens.BlocksGameplayPointer(screenPosition);
            if (!GuiViewport.SafeScreenRect.Contains(screenPosition)) return true;
            Vector2 point = DesignPoint(screenPosition);
            return point.y < 145 || new Rect(1090, 615, 170, 170).Contains(point) ||
                (Game.AdsReady && (Game.CanRecallParcel || Game.CanExtraTime) && new Rect(548, 709, 516, 60).Contains(point));
        }

        void OnGUI()
        {
            if (Game && Game.Phase == GamePhase.Shop) return;
            if (!Game || (Game.Phase != GamePhase.Menu && Game.Phase != GamePhase.Playing &&
                Game.Phase != GamePhase.Result && Game.Phase != GamePhase.Shop)) return;
            if (GuiViewport.Compact)
            {
                CompactScreens.DrawHud(Game);
                if (Game.Phase == GamePhase.Playing || Game.Phase == GamePhase.Result) Game.DrawFeelGUI();
                return;
            }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false, padding = new RectOffset(0, 0, 0, 0) };
                hitStyle = new GUIStyle(GUIStyle.none);
            }
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.matrix = GuiViewport.Matrix;
            GUI.color = Color.white;

            if (Game.Phase == GamePhase.Menu) DrawMenu();
            else if (Game.Phase == GamePhase.Playing) DrawPlaying();
            else if (Game.Phase == GamePhase.Result) DrawResult();
            else if (Game.Phase == GamePhase.Shop) DrawShop();

            if (Game.Phase == GamePhase.Playing && Game.DroneOwned && Game.DroneBusy)
            {
                Rounded(new Rect(1020, 107, 232, 30), AlphaDronePanel(), 12);
                Text(new Rect(1032, 109, 208, 26), "Drone " + Mathf.RoundToInt(Game.DroneProgress * 100) + "%", 16, teal, true, TextAnchor.MiddleCenter);
            }
            if (Game.Phase == GamePhase.Playing && Game.RentalVehicle >= 0)
            {
                Rounded(new Rect(1020, 143, 232, 48), AlphaDronePanel(), 12);
                string remaining = Game.RentalWaitingForDelivery ? L("Kiralık • teslimattan sonra biter", "Rental • ends after delivery") :
                    L("Kiralık ", "Rental ") + Mathf.CeilToInt(Game.RentalRemaining / 60f) + L(" dk", " min");
                Text(new Rect(1030, 148, 212, 38), remaining, 15, teal, true, TextAnchor.MiddleCenter);
            }

            if ((Game.Phase == GamePhase.Playing || Game.Phase == GamePhase.Shop) && Game.NoticeTime > 0 && !string.IsNullOrEmpty(Game.Notice))
            {
                float noticeY = Game.Phase == GamePhase.Shop ? 690 : Game.InShift ? 214 : 155;
                Panel(new Rect(350, noticeY, 580, 58), WithAlpha(ink, .96f), 18);
                Text(new Rect(368, noticeY + 10, 544, 38), Game.Notice, 17, Color.white, true, TextAnchor.MiddleCenter);
            }

            DrawLanguage(new Rect(1166, 28, 86, 62));
            Text(new Rect(28, 778, 500, 18), GameText.VersionLabel, 10, WithAlpha(ink, .62f), true);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            if (Game.Phase == GamePhase.Playing || Game.Phase == GamePhase.Result) Game.DrawFeelGUI();
        }

        void DrawMenu()
        {
            RectFill(GuiViewport.FullScreenRect, new Color(.05f, .12f, .15f, .08f));
            Panel(new Rect(48, 82, 454, 632), WithAlpha(cream, .98f), 30);
            Parcel(new Rect(80, 114, 45, 45), gold);
            Pill(new Rect(139, 123, 267, 27), L("MAHALLE SENİ BEKLİYOR", "YOUR NEIGHBORHOOD AWAITS"), pale, teal, 10);
            Text(new Rect(77, 179, 396, 88), L("Kurmalı", "Wobbly"), 69, ink, true);
            Text(new Rect(76, 254, 398, 94), L("Kargo", "Delivery"), GameText.Turkish ? 85 : 70, teal, true);
            Text(new Rect(82, 363, 366, 63), L("Paketler sende.\nMahalle biraz karışık.", "Precious parcels.\nA wonderfully wobbly ride."), 23, ink);

            Pill(new Rect(82, 446, 111, 30), L("4 DAKİKA", "4 MINUTES"), pale, ink, 10);
            Pill(new Rect(201, 446, 106, 30), L("3 YILDIZ", "3 STARS"), Hex(0xF9E5CE), ink, 10);
            Pill(new Rect(315, 446, 151, 30), L("GÜÇ KARTI SEÇ", "PICK POWER CARDS"), Hex(0xF8E3DC), ink, 10);

            string primary = Game.HasSavedRun ? L("Devam et  →", "Continue  →") : L("Vardiyaya başla  →", "Start your shift  →");
            if (Button(new Rect(81, 501, 387, 65), primary, teal, Color.white, 22))
            {
                if (Game.HasSavedRun) Game.ResumeRun(); else Game.StartRun();
            }
            if (Button(new Rect(81, 581, 186, 52), L("Garaj ve şehir", "Garage & city"), pale, ink, 19)) Game.OpenGarage();
            if (Button(new Rect(282, 581, 186, 52), L("Ayarlar", "Settings"), pale, ink, 19)) Game.OpenSettings();
            Text(new Rect(82, 653, 369, 34), L("WASD / ok tuşları veya sürükle.\nParmağını bırakınca scooter durur.", "WASD / arrow keys, or click and drag.\nRelease to stop your scooter."), 13, muted);

            DrawWallet(new Rect(976, 28, 174, 62));
            DrawSound(new Rect(896, 28, 64, 62));
            if (Game.IsPlaytest && Button(new Rect(897, 108, 353, 52), L("DENEME: +50.000 jeton", "PLAYTEST: +50,000 coins"), gold, ink, 17)) Game.AddPlaytestCoins();
            if (Game.BestScore > 0)
            {
                Panel(new Rect(927, 687, 325, 66), WithAlpha(cream, .96f), 18);
                Text(new Rect(948, 699, 283, 18), L("EN İYİ TUR", "BEST ROUND"), 11, muted, true);
                Text(new Rect(948, 719, 283, 24), Game.BestScore + L(" jeton", " coins"), 21, ink, true);
            }
        }

        void DrawPlaying()
        {
            Panel(new Rect(28, 28, 194, 68), WithAlpha(cream, .97f), 18);
            Parcel(new Rect(43, 45, 34, 34), gold);
            Text(new Rect(89, 37, 119, 52), L("KURMALI\nKARGO", "WOBBLY\nDELIVERY"), 17, ink, true, TextAnchor.MiddleLeft);

            Panel(new Rect(240, 28, 548, 110), WithAlpha(cream, .98f), 20);
            string heading = Game.IsParcelDropped ? L("PAKETİN DÜŞTÜ", "PARCEL DROPPED") : Game.HasPackage ?
                (Game.InShift ? L("SİPARİŞ #", "ORDER #") + (Game.ShiftDeliveries + 1) :
                Game.IsHeavyDelivery ? L("FABRİKA • AĞIR YÜK", "FACTORY • HEAVY LOAD") : L("TESLİMAT ", "DELIVERY ") + Mathf.Min(Game.Completed + 1, 3) + " / 3") :
                Game.InShift ? L("YENİ SİPARİŞ • PAKETİ AL", "NEW ORDER • PICK IT UP") : L("ÖNCE PAKETİNİ AL", "PICK UP YOUR PARCEL FIRST");
            Text(new Rect(260, 42, 400, 18), heading, 11, Game.IsParcelDropped ? coral : teal, true);
            string destination = Game.IsParcelDropped ? L("Düşen paketin", "Your dropped parcel") : Game.HasPackage ? Game.TargetName : Game.PickupName;
            Text(new Rect(260, 64, 398, 34), destination, 26, ink, true);
            Text(new Rect(260, 108, 68, 16), L("SAĞLAMLIK", "CONDITION"), 9, muted, true);
            Rounded(new Rect(337, 109, 218, 11), Hex(0xDBE5DE), 5);
            Color cargoColor = Game.Cargo >= 80 ? teal : Game.Cargo >= 40 ? gold : coral;
            Rounded(new Rect(337, 109, Mathf.Max(1, 218 * Mathf.Clamp01(Game.Cargo / 100f)), 11), cargoColor, 5);
            Text(new Rect(565, 104, 62, 21), GameText.Turkish ? "%" + Mathf.RoundToInt(Game.Cargo) : Mathf.RoundToInt(Game.Cargo) + "%", 12, ink, true);

            bool urgent = Game.HasPackage && Game.IsUrgentOrder;
            bool warning = urgent || Game.TimeLeft <= 10;
            bool pulseUrgent = urgent && !Game.ReducedMotion && Mathf.Repeat(Time.unscaledTime, 1) < .45f;
            Rounded(new Rect(676, 43, 94, 78), pulseUrgent ? Hex(0xF8C0A8) : warning ? Hex(0xF8E0D8) : pale, 14);
            Text(new Rect(681, 49, 84, 18), L("HIZ BONUSU", "SPEED BONUS"), 9, muted, true, TextAnchor.MiddleCenter);
            string time = Mathf.Max(0, Mathf.CeilToInt(Game.TimeLeft)).ToString("00") + L(" sn", " s");
            Text(new Rect(681, 70, 84, 34), time, 25, warning ? coral : teal, true, TextAnchor.MiddleCenter);
            if (Game.ComboCount > 0) Text(new Rect(28, 105, 194, 30), L("Nazik seri ×", "Care streak ×") + Game.ComboCount, 15, teal, true, TextAnchor.MiddleCenter);

            if (Button(new Rect(806, 28, 74, 62), L("Mola", "Pause"), WithAlpha(cream, .97f), ink, 15)) Game.PauseGame();
            DrawSound(new Rect(896, 28, 64, 62));
            DrawWallet(new Rect(976, 28, 174, 62));

            DrawTargetMarker();
            DrawMiniMap();
            Panel(new Rect(28, 610, 488, 82), WithAlpha(cream, .96f), 18);
            Parcel(new Rect(45, 631, 43, 43), Game.IsParcelDropped ? coral : cargoColor);
            Text(new Rect(103, 622, 391, 28), Game.ParcelName, 20, ink, true);
            Text(new Rect(103, 654, 391, 30), Game.ParcelHint, 13, muted);
            Panel(new Rect(28, 709, 488, 60), WithAlpha(cream, .94f), 18);
            Text(new Rect(45, 720, 453, 19), L("WASD / OK TUŞLARI  •  SÜRÜKLEYEREK YÖN VER", "WASD / ARROWS  •  CLICK AND DRAG TO STEER"), 11, ink, true);
            Text(new Rect(45, 744, 453, 17), Game.IsParcelDropped ? L("Işıklı halkaya dönüp düşen paketini al.", "Return to the glowing circle to collect your parcel.") : L("Işıklı alana yaklaş; paket otomatik teslim edilir.", "Reach the glowing zone to deliver automatically."), 12, muted);
            if (Game.AdsReady && Game.CanRecallParcel && Button(new Rect(548, 709, 516, 60), RewardLabel("Paketini geri çağır • reklam", "Recall your parcel • ad"), coral, Color.white, 18)) Game.RequestParcelRecall();
            else if (Game.AdsReady && Game.TimeLeft <= 10 && Game.CanExtraTime && Button(new Rect(548, 709, 516, 60), RewardLabel("+15 saniye • reklam", "+15 seconds • ad"), teal, Color.white, 18)) Game.RequestExtraTime();

            if (StickVisible)
            {
                Vector2 origin = DesignPoint(StickOrigin);
                Vector2 position = DesignPoint(StickPosition);
                Disc(new Rect(origin.x - 48, origin.y - 48, 96, 96), WithAlpha(cream, .45f));
                Disc(new Rect(origin.x - 41, origin.y - 41, 82, 82), WithAlpha(teal, .16f));
                Vector2 delta = Vector2.ClampMagnitude(position - origin, 36);
                Disc(new Rect(origin.x + delta.x - 21, origin.y + delta.y - 21, 42, 42), WithAlpha(cream, .94f));
                Disc(new Rect(origin.x + delta.x - 12, origin.y + delta.y - 12, 24, 24), teal);
            }
        }

        void DrawTargetMarker()
        {
            if (!Game.GameCamera || !Game.PlayerTransform) return;
            Vector3 world = Game.TargetPosition + Vector3.up * 2.15f;
            Vector3 screen = Game.GameCamera.WorldToScreenPoint(world);
            if (screen.z < 0) return;
            Vector2 p = DesignPoint(new Vector2(screen.x, screen.y));
            bool offscreen = p.x < 96 || p.x > 1184 || p.y < 193 || p.y > 626;
            Vector2 direction = p - new Vector2(640, 400);
            p.x = Mathf.Clamp(p.x, 96, 1184);
            p.y = Mathf.Clamp(p.y, 193, 626);
            string text = Game.IsParcelDropped ? L("Düşen paketin", "Your dropped parcel") : Game.TargetName;
            Color markerColor = Game.IsParcelDropped ? coral : teal;
            Rounded(new Rect(p.x - 90, p.y - 15, 180, 32), markerColor, 14);
            Text(new Rect(p.x - 87, p.y - 14, 174, 29), text, 12, Color.white, true, TextAnchor.MiddleCenter);
            Color previous = GUI.color;
            GUI.color = markerColor;
            float rotation = offscreen ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90 : 180;
            GUIUtility.RotateAroundPivot(rotation, new Vector2(p.x, p.y + 23));
            GUI.DrawTexture(new Rect(p.x - 8, p.y + 15, 16, 16), triangle);
            GUIUtility.RotateAroundPivot(-rotation, new Vector2(p.x, p.y + 23));
            GUI.color = previous;
        }

        void DrawMiniMap()
        {
            Rect map = new Rect(1094, 622, 158, 148);
            Panel(map, WithAlpha(cream, .95f), 18);
            Text(new Rect(1110, 633, 124, 18), L("MAHALLE", "NEIGHBORHOOD"), 9, muted, true);
            Rect land = new Rect(1108, 658, 130, 97);
            Rounded(land, Hex(0xD6E6CC), 9);
            float[] lanes = { -16, -8, 0, 8, 16 };
            foreach (float lane in lanes)
            {
                float x = Mathf.Lerp(land.x + 8, land.xMax - 8, Mathf.InverseLerp(-WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent, lane));
                float y = Mathf.Lerp(land.yMax - 8, land.y + 8, Mathf.InverseLerp(-WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent, lane));
                RectFill(new Rect(x - 2, land.y + 7, 4, land.height - 14), Hex(0xF5EDD9));
                RectFill(new Rect(land.x + 7, y - 2, land.width - 14, 4), Hex(0xF5EDD9));
            }
            var street = Game.StreetScene;
            bool hasTruck = street != null && street.HasTruck;
            bool hasWorksite = street != null && street.HasWorksite;
            bool truckWarning = hasTruck && street.WarningActive;
            if (hasWorksite)
            {
                Vector2 worksite = MapPoint(street.WorksitePosition, land);
                Disc(new Rect(worksite.x - 5, worksite.y - 5, 10, 10), gold);
                Disc(new Rect(worksite.x - 2, worksite.y - 2, 4, 4), ink);
            }
            if (hasTruck && street.TruckCollider != null && street.TruckCollider.enabled && street.TruckCollider.gameObject.activeInHierarchy)
            {
                Vector2 truck = MapPoint(street.TruckPosition, land);
                Rounded(new Rect(truck.x - 6, truck.y - 4, 12, 7), truckWarning ? gold : Hex(0x567E91), 2);
                Disc(new Rect(truck.x - 4, truck.y + 2, 3, 3), ink);
                Disc(new Rect(truck.x + 2, truck.y + 2, 3, 3), ink);
            }
            if (truckWarning)
            {
                Disc(new Rect(1227, 635, 12, 12), gold);
                Text(new Rect(1227, 634, 12, 14), "!", 10, ink, true, TextAnchor.MiddleCenter);
            }
            if (hasTruck)
            {
                RectFill(new Rect(1110, 758, 5, 5), Hex(0x567E91));
                Text(new Rect(1119, 756, 52, 12), L("Kamyon", "Truck"), 8, muted);
            }
            if (hasWorksite)
            {
                float legendX = hasTruck ? 1177 : 1110;
                Disc(new Rect(legendX, 758, 5, 5), gold);
                Text(new Rect(legendX + 9, 756, 49, 12), L("Rögar", "Works"), 8, muted);
            }
            if (!hasTruck && !hasWorksite) Text(new Rect(1110, 756, 128, 12), L("Sakin sokaklar", "Quiet streets"), 8, muted);
            Vector2 target = MapPoint(Game.TargetPosition, land);
            Disc(new Rect(target.x - 6, target.y - 6, 12, 12), coral);
            if (Game.PlayerTransform)
            {
                Vector2 player = MapPoint(Game.PlayerTransform.position, land);
                Disc(new Rect(player.x - 6, player.y - 6, 12, 12), Color.white);
                Disc(new Rect(player.x - 4, player.y - 4, 8, 8), teal);
            }
        }

        Vector2 MapPoint(Vector3 point, Rect map)
        {
            return new Vector2(Mathf.Lerp(map.x + 8, map.xMax - 8, Mathf.InverseLerp(-WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent, point.x)),
                Mathf.Lerp(map.yMax - 8, map.y + 8, Mathf.InverseLerp(-WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent, point.z)));
        }

        void DrawResult()
        {
            // Keep the courier and the customer's animated reaction visible to the left.
            RectFill(GuiViewport.FullScreenRect, new Color(.06f, .16f, .19f, .12f));
            Panel(new Rect(726, 113, 526, 625), cream, 28);
            GUI.BeginGroup(new Rect(726, 113, 526, 625));
            ReactionFace(new Rect(224, 25, 78, 78), Game.LastIntegrity);
            Color conditionColor = Game.LastIntegrity >= 80 ? teal : Game.LastIntegrity >= 40 ? Hex(0x997529) : coral;
            Pill(new Rect(43, 113, 440, 28), Game.LastParcelName + "  •  " + GameText.ConditionName(Game.LastIntegrity), pale, conditionColor, 12);
            Text(new Rect(32, 151, 462, 42), string.IsNullOrEmpty(Game.ResultTitle) ? L("Teslimat tamam!", "Delivery complete!") : Game.ResultTitle, 28, ink, true, TextAnchor.MiddleCenter);
            Text(new Rect(40, 198, 446, 81), Game.ResultLine ?? L("Paket yerini buldu. Sıradaki gelsin!", "Parcel delivered. Bring on the next one!"), 17, muted, false, TextAnchor.MiddleCenter);
            Rounded(new Rect(34, 292, 458, 170), pale, 18);
            Text(new Rect(55, 308, 290, 24), L("Teslimat ücreti", "Delivery fee") + (Game.LastMultiplier == 2 ? " ×2" : ""), 16, ink);
            Text(new Rect(346, 308, 120, 24), "+" + Game.LastBaseFee * Game.LastMultiplier, 17, ink, true, TextAnchor.UpperRight);
            Text(new Rect(55, 340, 290, 24), L("Hız + sağlamlık bonusu", "Speed + care bonus") + (Game.LastMultiplier == 2 ? " ×2" : ""), 15, muted);
            Text(new Rect(346, 340, 120, 24), "+" + (Game.LastCareBonus + Game.LastSpeedBonus) * Game.LastMultiplier, 17, teal, true, TextAnchor.UpperRight);
            Text(new Rect(55, 372, 290, 24), L("Nazik seri bonusu", "Care streak bonus"), 15, muted);
            Text(new Rect(346, 372, 120, 24), "+" + Game.LastComboBonus, 17, teal, true, TextAnchor.UpperRight);
            RectFill(new Rect(55, 407, 415, 1), Hex(0xCBDCD1));
            Text(new Rect(55, 428, 230, 26), L("KAZANDIN", "YOU EARNED"), 13, muted, true);
            Text(new Rect(280, 421, 190, 38), Game.LastPayout + L(" jeton", " coins"), 27, teal, true, TextAnchor.UpperRight);

            bool finished = Game.Completed >= 3;
            if (Button(new Rect(34, 478, 458, 59), finished ? L("Yeni tur  →", "Another round  →") : L("Sıradaki paket  →", "Next parcel  →"), teal, Color.white, 21))
            {
                if (finished) Game.RestartRun(); else Game.ContinueDelivery();
            }
            if (Button(new Rect(34, 551, 223, 52), L("Menü", "Menu"), pale, ink, 16)) Game.ShowMenu();
            if (Button(new Rect(269, 551, 223, 52), L("Garaj", "Garage"), pale, ink, 16)) Game.OpenGarage();
            GUI.EndGroup();
            if (finished && Game.AdsReady && Game.CanDoubleReward && Button(new Rect(48, 690, 630, 60), RewardLabel("Tur kazancını ikiye katla • reklam", "Double your round earnings • ad"), gold, ink, 20)) Game.RequestDoubleReward();
            DrawWallet(new Rect(976, 28, 174, 62));
        }

        void DrawShop()
        {
            RectFill(GuiViewport.FullScreenRect, new Color(.06f, .16f, .19f, .24f));
            Panel(new Rect(202, 135, 876, 534), cream, 28);
            Text(new Rect(237, 165, 800, 50), L("Küçük garaj, büyük tarz.", "Small garage. Big personality."), 35, ink, true);
            Text(new Rect(239, 227, 785, 36), L("Teslimatlardan jeton kazan, scooter’ına yeni bir renk aç.", "Earn coins on deliveries and unlock a fresh scooter color."), 18, muted);
            Color[] colors = CourierGame.ScooterColors;
            string[] names = { L("Nane", "Mint"), L("Mercan", "Coral"), L("Güneş", "Sunshine") };
            for (int i = 0; i < 3; ++i)
            {
                Rect card = new Rect(238 + 274 * i, 287, 256, 250);
                bool selected = Game.ScooterColorIndex == i;
                bool owned = i == 0 || selected || (OwnsColor != null && OwnsColor(i));
                bool canSelect = !selected && (owned || Game.Coins >= Game.ColorPrice(i));
                Color cardColor = selected ? Hex(0xDCEDE2) : Hex(0xF1EEE3);
                if (canSelect && card.Contains(Event.current.mousePosition)) cardColor = Color.Lerp(cardColor, Color.white, .12f);
                Rounded(card, cardColor, 19);
                if (selected) Pill(new Rect(card.x + 136, card.y + 14, 102, 24), L("SEÇİLİ", "SELECTED"), teal, Color.white, 10);
                Scooter(new Rect(card.x + 52, card.y + 44, 154, 87), colors[i]);
                Text(new Rect(card.x + 19, card.y + 140, 218, 30), names[i], 25, ink, true, TextAnchor.MiddleCenter);
                string caption = selected ? L("Hazırsın!", "Ready to roll!") : owned ? L("Garajında", "In your garage") : Game.ColorPrice(i) + L(" jeton", " coins");
                Text(new Rect(card.x + 19, card.y + 173, 218, 22), caption, 13, muted, false, TextAnchor.MiddleCenter);
                ButtonFace(new Rect(card.x + 18, card.y + 207, 220, 33), selected ? L("Seçili", "Selected") : owned ? L("Kullan", "Use color") : L("Rengi aç", "Unlock color"), selected ? Hex(0xBDDAD0) : canSelect ? colors[i] : Hex(0xD9DFD3), selected || !canSelect ? ink : Color.white, 15, canSelect);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && canSelect;
                bool cardClicked = GUI.Button(card, GUIContent.none, hitStyle);
                GUI.enabled = previousEnabled;
                if (cardClicked) Game.SelectColor(i);
            }
            string back = Game.ShopReturnsToGarage ? L("←  Garaja dön", "←  Back to garage") : Game.ShopReturnsToResult ? L("←  Teslimata dön", "←  Back to delivery") : L("←  Menüye dön", "←  Back to menu");
            if (Button(new Rect(238, 571, 209, 52), back, pale, ink, 17)) Game.CloseShop();
            Text(new Rect(470, 579, 563, 46), L("Bu garajda yalnızca oyunda kazandığın jetonlar kullanılır.", "Only coins earned in the game are used here."), 13, muted, false, TextAnchor.MiddleRight);
            DrawWallet(new Rect(976, 28, 174, 62));
        }

        void DrawWallet(Rect rect)
        {
            Panel(rect, WithAlpha(cream, .98f), 18);
            Disc(new Rect(rect.x + 17, rect.y + 17, 29, 29), gold);
            Disc(new Rect(rect.x + 22, rect.y + 22, 19, 19), Hex(0xFFD974));
            Text(new Rect(rect.x + 57, rect.y + 10, rect.width - 72, 31), Game.Coins.ToString(), 25, ink, true, TextAnchor.MiddleRight);
            Text(new Rect(rect.x + 57, rect.y + 39, rect.width - 72, 13), L("JETON", "COINS"), 9, muted, true, TextAnchor.MiddleRight);
        }

        void DrawSound(Rect rect)
        {
            if (Button(rect, Game.IsMuted ? L("SESSİZ", "MUTED") : L("SES", "SOUND"), WithAlpha(cream, .97f), muted, 10)) Game.ToggleMute();
        }

        void DrawLanguage(Rect rect)
        {
            Panel(rect, WithAlpha(cream, .98f), 16);
            Text(new Rect(rect.x + 5, rect.y + 6, rect.width - 10, 14), L("DİL", "LANGUAGE"), 8, muted, true, TextAnchor.MiddleCenter);
            Rect switches = new Rect(rect.x + 8, rect.y + 25, rect.width - 16, 27);
            Rounded(switches, pale, 9);
            Rounded(new Rect(switches.x + (GameText.Turkish ? 0 : switches.width * .5f), switches.y, switches.width * .5f, switches.height), teal, 9);
            Text(new Rect(switches.x, switches.y, switches.width * .5f, switches.height), "TR", 11, GameText.Turkish ? Color.white : muted, true, TextAnchor.MiddleCenter);
            Text(new Rect(switches.x + switches.width * .5f, switches.y, switches.width * .5f, switches.height), "EN", 11, GameText.Turkish ? muted : Color.white, true, TextAnchor.MiddleCenter);
            if (GUI.Button(rect, GUIContent.none, hitStyle)) Game.ToggleLanguage();
        }

        void ReactionFace(Rect rect, float integrity)
        {
            float x = rect.center.x;
            float y = rect.center.y;
            bool happy = integrity >= 80;
            bool dented = integrity >= 40;
            Disc(rect, happy ? Hex(0xDCEEDB) : dented ? Hex(0xF8EAC7) : Hex(0xF8DED7));
            Disc(new Rect(x - 19, y - 10, 8, happy || dented ? 10 : 12), ink);
            Disc(new Rect(x + 11, y - 10, 8, happy || dented ? 10 : 12), ink);
            Disc(new Rect(x - 29, y + 4, 11, 6), WithAlpha(coral, .35f));
            Disc(new Rect(x + 18, y + 4, 11, 6), WithAlpha(coral, .35f));
            if (happy)
            {
                Vector2 last = new Vector2(x - 17, y + 7);
                for (int i = 1; i <= 12; i++)
                {
                    float angle = Mathf.PI - (Mathf.PI * i / 12);
                    Vector2 next = new Vector2(x + Mathf.Cos(angle) * 17, y + 7 + Mathf.Sin(angle) * 10);
                    Line(last, next, 3, ink);
                    last = next;
                }
            }
            else if (dented)
            {
                Line(new Vector2(x - 14, y + 15), new Vector2(x + 14, y + 11), 3, ink);
                Line(new Vector2(x - 22, y - 17), new Vector2(x - 10, y - 15), 2, ink);
            }
            else
            {
                Disc(new Rect(x - 7, y + 9, 14, 19), ink);
                Line(new Vector2(x - 22, y - 18), new Vector2(x - 10, y - 21), 2, ink);
                Line(new Vector2(x + 10, y - 21), new Vector2(x + 22, y - 18), 2, ink);
            }
        }

        void Line(Vector2 start, Vector2 end, float thickness, Color color)
        {
            Matrix4x4 previous = GUI.matrix;
            Vector2 delta = end - start;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            RectFill(new Rect(start.x, start.y - thickness * .5f, delta.magnitude, thickness), color);
            GUI.matrix = previous;
        }

        void Scooter(Rect rect, Color color)
        {
            float factor = rect.width / 154f;
            Vector2 pivot = new Vector2(rect.x, rect.y);
            Disc(new Rect(pivot.x + 10 * factor, pivot.y + 57 * factor, 31 * factor, 31 * factor), ink);
            Disc(new Rect(pivot.x + 115 * factor, pivot.y + 57 * factor, 31 * factor, 31 * factor), ink);
            Disc(new Rect(pivot.x + 18 * factor, pivot.y + 65 * factor, 15 * factor, 15 * factor), Hex(0xB7C7C7));
            Disc(new Rect(pivot.x + 123 * factor, pivot.y + 65 * factor, 15 * factor, 15 * factor), Hex(0xB7C7C7));
            Rounded(new Rect(pivot.x + 20 * factor, pivot.y + 48 * factor, 99 * factor, 24 * factor), color, 10 * factor);
            Rounded(new Rect(pivot.x + 109 * factor, pivot.y + 15 * factor, 13 * factor, 49 * factor), color, 6 * factor);
            Rounded(new Rect(pivot.x + 103 * factor, pivot.y + 10 * factor, 28 * factor, 7 * factor), ink, 3 * factor);
            Rounded(new Rect(pivot.x + 39 * factor, pivot.y + 32 * factor, 43 * factor, 10 * factor), ink, 5 * factor);
            Parcel(new Rect(pivot.x + 6 * factor, pivot.y + 13 * factor, 32 * factor, 32 * factor), Hex(0xDCAF79));
        }

        void Parcel(Rect rect, Color color)
        {
            Rounded(rect, color, rect.width * .17f);
            RectFill(new Rect(rect.x + rect.width * .43f, rect.y, rect.width * .14f, rect.height), WithAlpha(cream, .77f));
            Rounded(new Rect(rect.x + rect.width * .12f, rect.y + rect.height * .66f, rect.width * .24f, rect.height * .12f), WithAlpha(cream, .75f), 1);
        }

        bool Button(Rect rect, string text, Color background, Color foreground, int size, bool enabled = true)
        {
            ButtonFace(rect, text, background, foreground, size, enabled);
            bool previous = GUI.enabled;
            GUI.enabled = enabled && previous;
            bool clicked = GUI.Button(rect, GUIContent.none, hitStyle);
            GUI.enabled = previous;
            return clicked;
        }

        void ButtonFace(Rect rect, string text, Color background, Color foreground, int size, bool enabled = true)
        {
            bool hover = enabled && GUI.enabled && rect.Contains(Event.current.mousePosition);
            Color fill = hover ? Color.Lerp(background, Color.white, .12f) : background;
            Rounded(new Rect(rect.x, rect.y + 4, rect.width, rect.height), WithAlpha(ink, .13f), 14);
            Rounded(rect, fill, 14);
            Text(rect, text, size, foreground, true, TextAnchor.MiddleCenter);
        }

        void Pill(Rect rect, string text, Color background, Color foreground, int size)
        {
            Rounded(rect, background, rect.height * .5f);
            Text(rect, text, size, foreground, true, TextAnchor.MiddleCenter);
        }

        void Panel(Rect rect, Color color, float radius)
        {
            Rounded(new Rect(rect.x, rect.y + 6, rect.width, rect.height), WithAlpha(ink, .12f), radius);
            Rounded(rect, color, radius);
        }

        void Text(Rect rect, string value, int size, Color color, bool weight = false, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            label.font = weight ? bold : regular;
            label.fontSize = size;
            label.fontStyle = weight ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.normal.textColor = color;
            GUI.Label(rect, value, label);
        }

        void Rounded(Rect rect, Color color, float radius)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);
            if (radius <= .5f) { RectFill(rect, color); return; }
            RectFill(new Rect(rect.x + radius, rect.y, rect.width - radius * 2, rect.height), color);
            RectFill(new Rect(rect.x, rect.y + radius, rect.width, rect.height - radius * 2), color);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, radius, radius), circle, new Rect(0, .5f, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - radius, rect.y, radius, radius), circle, new Rect(.5f, .5f, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.yMax - radius, radius, radius), circle, new Rect(0, 0, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius), circle, new Rect(.5f, 0, .5f, .5f));
            GUI.color = previous;
        }

        void RectFill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void Disc(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circle);
            GUI.color = previous;
        }

        static Texture2D MakeCircle(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "HUD soft circle";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f));
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(size * .5f - distance));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Texture2D MakeArrow(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "HUD destination arrow";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float halfWidth = (size - y) * .5f;
                    float alpha = Mathf.Clamp01(halfWidth - Mathf.Abs(x - size * .5f));
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        Color AlphaDronePanel() => WithAlpha(cream, .96f);
        static Color Hex(uint value) { return new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1); }
    }
}
