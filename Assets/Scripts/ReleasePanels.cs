using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Local release overlays. Every action delegates to CourierGame.</summary>
    public sealed class ReleasePanels : MonoBehaviour
    {
        public CourierGame Game;
        readonly Color ink = Hex(0x193B4B);
        readonly Color muted = Hex(0x66818B);
        readonly Color cream = Hex(0xFFF9ED);
        readonly Color teal = Hex(0x119E99);
        readonly Color coral = Hex(0xE97760);
        readonly Color gold = Hex(0xF3BF48);
        readonly Color pale = Hex(0xEAF3EA);
        Font font;
        Texture2D circle;
        GUIStyle label, hitStyle, invisibleSlider, invisibleThumb;
        Vector2 privacyScroll;
        static readonly Rect PrivacyScrollRect = new Rect(331, 217, 620, 384);
        const float PrivacyContentWidth = 593;
        float privacyContentHeight = 384;
        int privacyFinger = -1;
        int resetStage;
        GamePhase lastPhase;

        static string L(string tr, string en) => GameText.Pick(tr, en);

        void Awake()
        {
            if (!Game) Game = GetComponent<CourierGame>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            circle = MakeCircle();
        }

        void OnDestroy() { if (circle) Destroy(circle); }

        void Update()
        {
            if (Game && Game.Phase == GamePhase.Garage) return;
            if (Game && (GuiViewport.Compact || Game.Phase == GamePhase.Garage))
            {
                CompactScreens.UpdateTouch(Game);
                privacyFinger = -1;
                return;
            }
            if (!Game || Game.Phase != GamePhase.Privacy || Input.touchCount != 1)
            {
                privacyFinger = -1;
                return;
            }
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                // The right-hand scrollbar strip keeps its normal IMGUI handling.
                Rect contentArea = new Rect(PrivacyScrollRect.x, PrivacyScrollRect.y, PrivacyContentWidth, PrivacyScrollRect.height);
                privacyFinger = contentArea.Contains(GuiViewport.ScreenToDesign(touch.position)) ? touch.fingerId : -1;
                return;
            }
            if (touch.fingerId != privacyFinger) return;
            if (touch.phase == TouchPhase.Moved)
            {
                float maximum = Mathf.Max(0, privacyContentHeight - PrivacyScrollRect.height);
                privacyScroll.y = Mathf.Clamp(privacyScroll.y + touch.deltaPosition.y / GuiViewport.Scale, 0, maximum);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) privacyFinger = -1;
        }

        static bool IsOverlay(GamePhase phase)
        {
            return phase == GamePhase.Settings || phase == GamePhase.Privacy || phase == GamePhase.Garage ||
                   phase == GamePhase.Paused || phase == GamePhase.Tutorial;
        }

        public bool BlocksPointer(Vector2 screenPosition) => Game && IsOverlay(Game.Phase);

        void OnGUI()
        {
            if (!Game) return;
            if (lastPhase != Game.Phase)
            {
                resetStage = 0;
                if (Game.Phase == GamePhase.Privacy) privacyScroll = Vector2.zero;
                lastPhase = Game.Phase;
            }
            if (!IsOverlay(Game.Phase)) return;
            if (Game.Phase == GamePhase.Garage) return;
            if (GuiViewport.Compact) { CompactScreens.DrawOverlay(Game); return; }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { font = font, wordWrap = true, richText = false, padding = new RectOffset(0, 0, 0, 0) };
                hitStyle = new GUIStyle(GUIStyle.none);
                invisibleSlider = new GUIStyle(GUIStyle.none) { fixedHeight = 36, padding = new RectOffset(12, 12, 0, 0) };
                invisibleThumb = new GUIStyle(GUIStyle.none) { fixedWidth = 24, fixedHeight = 36 };
            }
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = GuiViewport.Matrix;
            GUI.color = Color.white;

            if (Game.Phase == GamePhase.Settings) DrawSettings();
            else if (Game.Phase == GamePhase.Privacy) DrawPrivacy();
            else if (Game.Phase == GamePhase.Garage) DrawGarage();
            else if (Game.Phase == GamePhase.Paused) DrawPause();
            else DrawTutorial();

            bool previousEnabled = GUI.enabled;
            GUI.enabled = resetStage == 0 && !Game.AdPreferencesOpen;
            if (Button(new Rect(1138, 28, 114, 43), Game.IsTurkish ? "TR  →  EN" : "EN  →  TR", cream, ink, 13)) Game.ToggleLanguage();
            GUI.enabled = previousEnabled;
            Text(new Rect(28, 778, 500, 18), GameText.VersionLabel, 10, WithAlpha(ink, .62f), true);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            if (Game.Phase == GamePhase.Settings && Game.AdPreferencesOpen) CompactScreens.DrawAdPreferences(Game);
        }

        void Dim(float alpha = .28f) => Fill(GuiViewport.FullScreenRect, new Color(.06f, .16f, .19f, alpha));

        void DrawSettings()
        {
            Dim();
            Panel(new Rect(350, 54, 580, 690), cream, 28);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = resetStage == 0 && !Game.AdPreferencesOpen;
            Text(new Rect(386, 85, 508, 43), L("Biraz kendine göre ayarla.", "Make yourself comfortable."), 29, ink, true);
            Text(new Rect(386, 138, 508, 26), L("AYARLAR", "SETTINGS"), 11, teal, true);

            Rounded(new Rect(382, 185, 516, 118), pale, 18);
            Text(new Rect(403, 202, 326, 29), L("Ses düzeyi", "Volume"), 19, ink, true);
            Text(new Rect(780, 204, 94, 25), Percent(Game.Volume), 17, muted, true, TextAnchor.MiddleRight);
            Rect slider = new Rect(405, 247, 468, 36);
            Rounded(new Rect(slider.x + 12, slider.y + 15, slider.width - 24, 7), Hex(0xCCDCD3), 4);
            Rounded(new Rect(slider.x + 12, slider.y + 15, (slider.width - 24) * Mathf.Clamp01(Game.Volume), 7), teal, 4);
            float knob = slider.x + 12 + (slider.width - 24) * Mathf.Clamp01(Game.Volume);
            Disc(new Rect(knob - 11, slider.y + 7, 22, 22), teal);
            Disc(new Rect(knob - 5, slider.y + 13, 10, 10), cream);
            float volume = GUI.HorizontalSlider(slider, Game.Volume, 0, 1, invisibleSlider, invisibleThumb);
            if (Mathf.Abs(volume - Game.Volume) > .001f) Game.SettingVolume(volume);

            Rounded(new Rect(382, 321, 516, 87), pale, 18);
            Text(new Rect(403, 336, 330, 26), L("Hareketi azalt", "Reduce motion"), 19, ink, true);
            Text(new Rect(403, 366, 330, 22), L("Hareket efektlerini daha sakin yapar.", "Keep movement effects gentler."), 13, muted);
            if (Button(new Rect(772, 340, 105, 46), Game.ReducedMotion ? L("Açık", "On") : L("Kapalı", "Off"), Game.ReducedMotion ? teal : Hex(0xD9E4D8), Game.ReducedMotion ? Color.white : ink, 17)) Game.ToggleReducedMotion();

            Text(new Rect(386, 434, 120, 28), L("Dil", "Language"), 19, ink, true);
            if (Button(new Rect(634, 422, 264, 45), Game.IsTurkish ? "Türkçe  →  English" : "English  →  Türkçe", pale, ink, 16)) Game.ToggleLanguage();

            if (Button(new Rect(382, 488, 250, 51), L("Nasıl oynanır?", "How to play"), pale, ink, 17)) Game.OpenTutorial();
            if (Button(new Rect(648, 488, 250, 51), L("Gizlilik", "Privacy"), pale, ink, 17)) Game.OpenPrivacy();
            if (Button(new Rect(382, 550, 516, 45), L("Reklam tercihleri", "Ad preferences"), pale, ink, 17)) Game.OpenAdPreferences();
            if (Button(new Rect(382, 606, 516, 45), L("İlerlemeyi sıfırla", "Reset progress"), Hex(0xF8E6DE), Hex(0xAE5344), 15)) resetStage = 1;
            if (Button(new Rect(382, 662, 516, 50), L("←  Geri", "←  Back"), teal, Color.white, 18)) Game.CloseOverlay();

            GUI.enabled = previousEnabled;
            if (resetStage > 0) DrawResetConfirm();
        }

        void DrawResetConfirm()
        {
            Dim(.38f);
            Panel(new Rect(384, 216, 512, 365), cream, 25);
            Text(new Rect(414, 246, 452, 43), resetStage == 1 ? L("Yeni bir başlangıç mı?", "A fresh start?") : L("Son kez onayla", "One last confirmation"), 28, ink, true);
            string description = resetStage == 1
                ? L("Kazanılmış jetonlar, jetonla açtığın ürünler, en iyi turun ve ayarlar sıfırlanır. Satın alınmış kalıcı haklar ve harcanmamış satın alma jetonları korunur.", "Earned coins, coin-unlocked items, best round and settings reset. Purchased permanent rights and unspent purchased coins are kept.")
                : L("Bu işlem geri alınamaz. Kazanılmış ilerlemeni ve ayarlarını sıfırlamak istediğinden emin misin?", "This cannot be undone. Are you sure you want to reset your earned progress and settings?");
            Text(new Rect(414, 307, 452, 112), description, 18, muted);
            if (Button(new Rect(414, 443, 452, 49), resetStage == 1 ? L("Devam et", "Continue") : L("Evet, ilerlemeyi sıfırla", "Yes, reset my progress"), coral, Color.white, 17))
            {
                if (resetStage == 1) resetStage = 2;
                else { resetStage = 0; Game.ResetProgress(); }
            }
            if (Button(new Rect(414, 509, 452, 42), L("Vazgeç", "Cancel"), pale, ink, 16)) resetStage = 0;
        }

        void DrawPrivacy()
        {
            Dim();
            Panel(new Rect(280, 62, 720, 674), cream, 28);
            Text(new Rect(316, 93, 648, 43), L("Gizlilik bilgileri", "Privacy information"), 31, ink, true);
            Text(new Rect(316, 148, 648, 26), L("BU CİHAZDAKİ SÜRÜM", "THE CURRENT BUILD ON THIS DEVICE"), 11, teal, true);
            Rounded(new Rect(310, 196, 660, 427), pale, 17);
            string policy = Game.PrivacyText ?? "";
            label.fontSize = 17;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.UpperLeft;
            label.normal.textColor = ink;
            privacyContentHeight = Mathf.Max(PrivacyScrollRect.height, label.CalcHeight(new GUIContent(policy), PrivacyContentWidth) + 20);
            privacyScroll.y = Mathf.Clamp(privacyScroll.y, 0, Mathf.Max(0, privacyContentHeight - PrivacyScrollRect.height));
            privacyScroll = GUI.BeginScrollView(PrivacyScrollRect, privacyScroll, new Rect(0, 0, PrivacyContentWidth, privacyContentHeight));
            GUI.Label(new Rect(0, 0, PrivacyContentWidth, privacyContentHeight), policy, label);
            GUI.EndScrollView();
            if (Game.SupportAvailable)
            {
                if (Button(new Rect(310, 653, 320, 51), L("←  Geri", "←  Back"), teal, Color.white, 18)) Game.CloseOverlay();
                if (Button(new Rect(646, 653, 324, 51), L("Destek", "Support"), pale, ink, 18)) Game.OpenSupport();
            }
            else if (Button(new Rect(310, 653, 660, 51), L("←  Geri", "←  Back"), teal, Color.white, 18)) Game.CloseOverlay();
        }

        void DrawPause()
        {
            Dim(.30f);
            Panel(new Rect(433, 186, 414, 421), cream, 28);
            Rounded(new Rect(597, 217, 86, 70), pale, 19);
            Rounded(new Rect(622, 236, 10, 32), teal, 4);
            Rounded(new Rect(649, 236, 10, 32), teal, 4);
            Text(new Rect(463, 309, 354, 45), L("Kısa bir mola.", "A little breather."), 31, ink, true, TextAnchor.MiddleCenter);
            if (Button(new Rect(465, 380, 350, 56), L("Teslimata dön  →", "Back to the ride  →"), teal, Color.white, 20)) Game.ResumeGame();
            if (Button(new Rect(465, 454, 350, 49), L("Ayarlar", "Settings"), pale, ink, 18)) Game.OpenSettings();
            if (Button(new Rect(465, 521, 350, 49), L("Kaydet ve menüye dön", "Save and return to menu"), pale, ink, 16)) Game.ShowMenu();
            Text(new Rect(463, 582, 354, 20), L("Turuna menüden devam edebilirsin.", "You can resume this round from the menu."), 12, muted, false, TextAnchor.MiddleCenter);
        }

        void DrawTutorial()
        {
            Dim();
            Panel(new Rect(280, 68, 720, 666), cream, 28);
            int page = Mathf.Clamp(Game.TutorialPage, 0, 2);
            Text(new Rect(316, 98, 390, 25), L("KÜÇÜK KURYE REHBERİ", "YOUR LITTLE COURIER GUIDE"), 11, teal, true);
            if (Button(new Rect(858, 93, 106, 36), L("Atla", "Skip"), pale, muted, 14)) Game.FinishTutorial();
            string[] titles = { L("Paketini al, yola çık.", "Pick up. Roll out."), L("Hızlı ol, nazik ol.", "Quick ride. Gentle hands."), L("Kazandığına biraz renk kat.", "Give your coins some color.") };
            string[] descriptions = {
                L("Haritadaki alım noktasının ışıklı halkasından paketini al. WASD / ok tuşlarıyla veya sürükleyerek sür. Hedef evin halkasında teslimat otomatik yapılır.", "Collect your parcel at the marked pickup circle. Use WASD / arrows, or drag to steer. Enter the target home's circle to deliver automatically."),
                L("Çarpmak paketin sağlamlığını azaltır. Pasta sallanır, bitki ağırdır, çay takımı çok kırılgandır. Süre yalnızca bonus içindir; yetişemesen de teslimat ücretini alırsın.", "Crashes reduce parcel condition. Cake wobbles, the plant is heavy, and the tea set is extra fragile. Time is only a bonus: a late delivery still earns its base fee."),
                L("Teslimat jetonlarıyla araç ve dekor aç. Drone ek jeton kazanır. Mağaza fiyatları ve isteğe bağlı reklamlar yalnızca kullanıma hazır olduğunda gösterilir.", "Unlock rides and decor with delivery coins. A drone earns extra coins. Store prices and optional ads appear when available.")
            };
            Text(new Rect(316, 153, 648, 55), titles[page], 33, ink, true);
            Rounded(new Rect(316, 240, 648, 183), pale, 22);
            TutorialDiagram(page, new Rect(316, 240, 648, 183));
            Text(new Rect(326, 454, 628, 116), descriptions[page], 20, muted);
            for (int i = 0; i < 3; i++) Disc(new Rect(612 + i * 24, 589, 10, 10), i == page ? teal : Hex(0xCFDDD3));
            if (Button(new Rect(316, 637, 198, 56), L("←  Önceki", "←  Previous"), pale, ink, 17, page > 0)) Game.PreviousTutorial();
            if (Button(new Rect(530, 637, 434, 56), page == 2 ? L("Hazırım  →", "Ready to roll  →") : L("Sonraki  →", "Next  →"), teal, Color.white, 20))
            {
                if (page == 2) Game.FinishTutorial(); else Game.NextTutorial();
            }
        }

        void TutorialDiagram(int page, Rect area)
        {
            float y = area.y + 56;
            float a = area.x + 98, b = area.center.x, c = area.xMax - 98;
            if (page == 0)
            {
                ParcelIcon(new Rect(a - 23, y, 46, 46), gold);
                ScooterIcon(new Rect(b - 45, y - 1, 90, 54), teal);
                HouseIcon(new Rect(c - 28, y - 10, 56, 64), coral);
                Arrow(new Vector2(a + 51, y + 22), new Vector2(b - 63, y + 22));
                Arrow(new Vector2(b + 66, y + 22), new Vector2(c - 54, y + 22));
                DiagramCaption(a, y + 66, L("PAKETİNİ AL", "PICK UP"));
                DiagramCaption(b, y + 66, L("SÜR", "RIDE"));
                DiagramCaption(c, y + 66, L("TESLİM ET", "DELIVER"));
            }
            else if (page == 1)
            {
                DecorIcon(2, new Rect(a - 24, y - 3, 48, 48), coral);
                ParcelIcon(new Rect(a - 20, y + 5, 40, 37), gold);
                DecorIcon(0, new Rect(b - 25, y - 9, 50, 58), teal);
                Rounded(new Rect(c - 24, y + 1, 40, 33), cream, 8);
                Rounded(new Rect(c + 10, y + 6, 16, 21), gold, 7);
                Rounded(new Rect(c + 15, y + 10, 8, 13), pale, 4);
                Rounded(new Rect(c - 29, y + 39, 61, 7), gold, 3);
                DiagramCaption(a, y + 66, L("DENGELİ SÜR", "KEEP STEADY"));
                DiagramCaption(b, y + 66, L("AĞIR YÜK", "HEAVY LOAD"));
                DiagramCaption(c, y + 66, L("ÇOK KIRILGAN", "EXTRA FRAGILE"));
            }
            else
            {
                Disc(new Rect(a - 25, y - 1, 50, 50), gold);
                Disc(new Rect(a - 17, y + 7, 34, 34), Hex(0xFFDA7B));
                ScooterIcon(new Rect(b - 45, y - 1, 90, 54), coral);
                DecorIcon(4, new Rect(c - 31, y - 8, 62, 62), teal);
                Arrow(new Vector2(a + 51, y + 22), new Vector2(b - 63, y + 22));
                Arrow(new Vector2(b + 66, y + 22), new Vector2(c - 54, y + 22));
                DiagramCaption(a, y + 66, L("JETON KAZAN", "EARN COINS"));
                DiagramCaption(b, y + 66, L("RENK SEÇ", "FRESH PAINT"));
                DiagramCaption(c, y + 66, L("DEKOR EKLE", "ADD DECOR"));
            }
        }

        void DiagramCaption(float x, float y, string caption) => Text(new Rect(x - 86, y, 172, 26), caption, 11, ink, true, TextAnchor.MiddleCenter);

        void DrawGarage()
        {
            Dim(.05f);
            DrawVehicleSelection();
            Panel(new Rect(866, 124, 386, 620), cream, 26);
            Text(new Rect(886, 144, 346, 38), L("Depoyu canlandır.", "Make it yours."), 27, ink, true);
            Disc(new Rect(889, 189, 22, 22), gold);
            Text(new Rect(922, 187, 308, 27), Game.Coins + L(" jeton", " coins"), 18, ink, true);

            string[] names = { L("Saksı", "Plant"), L("Lamba", "Lamp"), L("Halı", "Rug"), L("Tezgâh", "Workbench"), L("Raf", "Shelf"), L("Kargo tabelası", "Parcel sign") };
            Color[] colors = { teal, gold, coral, Hex(0xAF835E), Hex(0x7B9EA0), gold };
            for (int i = 0; i < 6; i++)
            {
                Rect card = new Rect(884 + i % 2 * 184, 232 + i / 2 * 126, 166, 115);
                bool owned = Game.DecorOwned(i);
                bool equipped = Game.DecorEquipped(i);
                bool affordable = owned || Game.Coins >= Game.DecorPrice(i);
                Color cardColor = equipped ? Hex(0xDCEDE2) : Hex(0xF1EEE3);
                if (affordable && card.Contains(Event.current.mousePosition)) cardColor = Color.Lerp(cardColor, Color.white, .12f);
                Rounded(card, cardColor, 14);
                DecorIcon(i, new Rect(card.x + 12, card.y + 13, 34, 37), colors[i]);
                Text(new Rect(card.x + 54, card.y + 12, 103, 38), names[i], 15, ink, true, TextAnchor.MiddleLeft);
                string status = owned ? equipped ? L("Görünüyor", "On display") : L("Garajında", "Owned") : Game.DecorPrice(i) + L(" jeton", " coins");
                Text(new Rect(card.x + 12, card.y + 54, 142, 20), status, 12, muted);
                ButtonFace(new Rect(card.x + 10, card.y + 80, 146, 26), owned ? equipped ? L("Gizle", "Hide") : L("Göster", "Show") : L("Satın al", "Buy"), equipped ? Hex(0xBCD9CF) : affordable ? teal : Hex(0xD9DFD3), equipped || !affordable ? ink : Color.white, 13, affordable);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && affordable;
                bool cardClicked = GUI.Button(card, GUIContent.none, hitStyle);
                GUI.enabled = previousEnabled;
                if (cardClicked) Game.SelectDecor(i);
            }
            if (Button(new Rect(884, 628, 350, 42), L("Scooter renkleri  →", "Scooter colors  →"), pale, ink, 16)) Game.OpenShop();
            if (Button(new Rect(884, 687, 350, 39), L("←  Geri", "←  Back"), teal, Color.white, 17)) Game.CloseOverlay();

            Panel(new Rect(43, 651, 413, 84), WithAlpha(cream, .94f), 18);
            Text(new Rect(63, 663, 373, 21), L("KÜÇÜK DEPO, BÜYÜK TARZ", "LITTLE DEPOT. BIG PERSONALITY."), 11, teal, true);
            Text(new Rect(63, 692, 373, 28), L("Dekorlarını seç; odada hemen gör.", "Pick some decor and watch the room change."), 16, ink);
        }

        void DrawVehicleSelection()
        {
            Panel(new Rect(43, 124, 660, 230), WithAlpha(cream, .97f), 22);
            Text(new Rect(63, 141, 360, 22), L("ARACINI SEÇ", "CHOOSE YOUR RIDE"), 11, teal, true);
            Text(new Rect(439, 141, 242, 22), L("İKİ ÜCRETSİZ SEÇENEK", "TWO FREE RIDES"), 10, muted, true, TextAnchor.MiddleRight);
            for (int i = 0; i < 2; i++)
            {
                Rect card = new Rect(61 + i * 322, 176, 302, 100);
                bool selected = Game.VehicleIndex == i;
                Color color = i == 0 ? teal : coral;
                Color background = selected ? Hex(0xDCEDE2) : Hex(0xF1EEE3);
                if (!selected && Game.CanChangeVehicle && card.Contains(Event.current.mousePosition)) background = Color.Lerp(background, Color.white, .12f);
                Rounded(card, background, 15);
                ScooterIcon(new Rect(card.x + 15, card.y + 23, 76, 47), color);
                string name = selected ? Game.VehicleName : i == 0 ? L("Tatlı scooter", "Easy rider") : L("Fırlama scooter", "Zip rider");
                Text(new Rect(card.x + 106, card.y + 14, 184, 27), name, 18, ink, true);
                Text(new Rect(card.x + 106, card.y + 44, 184, 21), i == 0 ? L("Dengeli sürüş", "Steady handling") : L("Hızlı, daha riskli", "Fast, with more risk"), 13, muted);
                Rounded(new Rect(card.x + 106, card.y + 71, 116, 21), selected ? color : pale, 8);
                Text(new Rect(card.x + 106, card.y + 71, 116, 21), selected ? L("SEÇİLİ", "SELECTED") : L("SEÇ", "SELECT"), 10, selected ? Color.white : ink, true, TextAnchor.MiddleCenter);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && !selected && Game.CanChangeVehicle;
                bool clicked = GUI.Button(card, GUIContent.none, hitStyle);
                GUI.enabled = previousEnabled;
                if (clicked) Game.SelectVehicle(i);
            }
            Text(new Rect(63, 294, 620, 44), Game.CanChangeVehicle ? Game.VehicleHint : L("Aracı değiştirmek için önce paketini teslim et.", "Deliver your parcel before changing rides."), 13, muted);
        }

        void DecorIcon(int index, Rect r, Color color)
        {
            float s = r.width / 50;
            Vector2 p = new Vector2(r.x, r.y);
            if (index == 0)
            {
                Rounded(new Rect(p.x + 12 * s, p.y + 31 * s, 28 * s, 23 * s), Hex(0xC88865), 5 * s);
                Rounded(new Rect(p.x + 24 * s, p.y + 10 * s, 4 * s, 28 * s), color, 2 * s);
                Disc(new Rect(p.x + 4 * s, p.y + 3 * s, 27 * s, 18 * s), color);
                Disc(new Rect(p.x + 23 * s, p.y + 13 * s, 25 * s, 17 * s), color);
            }
            else if (index == 1)
            {
                Rounded(new Rect(p.x + 23 * s, p.y + 20 * s, 5 * s, 29 * s), ink, 2 * s);
                Rounded(new Rect(p.x + 10 * s, p.y + 47 * s, 31 * s, 6 * s), ink, 3 * s);
                Rounded(new Rect(p.x + 7 * s, p.y + 3 * s, 38 * s, 24 * s), color, 9 * s);
            }
            else if (index == 2)
            {
                Rounded(new Rect(p.x + 2 * s, p.y + 15 * s, 47 * s, 27 * s), color, 9 * s);
                Rounded(new Rect(p.x + 9 * s, p.y + 21 * s, 33 * s, 15 * s), Hex(0xF7D9A2), 5 * s);
                Rounded(new Rect(p.x + 15 * s, p.y + 26 * s, 21 * s, 5 * s), color, 2 * s);
            }
            else if (index == 3)
            {
                Rounded(new Rect(p.x + 1 * s, p.y + 12 * s, 48 * s, 12 * s), color, 4 * s);
                Rounded(new Rect(p.x + 7 * s, p.y + 23 * s, 7 * s, 29 * s), color, 2 * s);
                Rounded(new Rect(p.x + 37 * s, p.y + 23 * s, 7 * s, 29 * s), color, 2 * s);
                ParcelIcon(new Rect(p.x + 8 * s, p.y - 2 * s, 18 * s, 17 * s), gold);
            }
            else if (index == 4)
            {
                Rounded(new Rect(p.x + 3 * s, p.y + 1 * s, 6 * s, 52 * s), color, 2 * s);
                Rounded(new Rect(p.x + 42 * s, p.y + 1 * s, 6 * s, 52 * s), color, 2 * s);
                foreach (float y in new[] { 7f, 28f, 47f }) Rounded(new Rect(p.x + 7 * s, p.y + y * s, 38 * s, 5 * s), color, 2 * s);
                ParcelIcon(new Rect(p.x + 12 * s, p.y + 13 * s, 18 * s, 16 * s), gold);
            }
            else
            {
                Rounded(new Rect(p.x + 22 * s, p.y + 28 * s, 6 * s, 25 * s), Hex(0xAF835E), 2 * s);
                Rounded(new Rect(p.x + 1 * s, p.y + 4 * s, 49 * s, 32 * s), color, 7 * s);
                ParcelIcon(new Rect(p.x + 15 * s, p.y + 10 * s, 21 * s, 20 * s), cream);
            }
        }

        void ParcelIcon(Rect r, Color color)
        {
            Rounded(r, color, r.width * .16f);
            Fill(new Rect(r.x + r.width * .43f, r.y, r.width * .14f, r.height), WithAlpha(cream, .8f));
        }

        void ScooterIcon(Rect r, Color color)
        {
            float s = r.width / 90;
            Disc(new Rect(r.x + 5 * s, r.y + 33 * s, 22 * s, 22 * s), ink);
            Disc(new Rect(r.x + 64 * s, r.y + 33 * s, 22 * s, 22 * s), ink);
            Rounded(new Rect(r.x + 16 * s, r.y + 28 * s, 58 * s, 13 * s), color, 6 * s);
            Rounded(new Rect(r.x + 64 * s, r.y + 5 * s, 8 * s, 34 * s), color, 4 * s);
            Rounded(new Rect(r.x + 59 * s, r.y + 3 * s, 20 * s, 5 * s), ink, 2 * s);
            Rounded(new Rect(r.x + 30 * s, r.y + 19 * s, 24 * s, 7 * s), ink, 3 * s);
            ParcelIcon(new Rect(r.x + 4 * s, r.y + 8 * s, 23 * s, 22 * s), gold);
        }

        void HouseIcon(Rect r, Color color)
        {
            Rounded(new Rect(r.x + 6, r.y + 17, r.width - 12, r.height - 17), color, 6);
            Rounded(new Rect(r.x + 17, r.y + 36, 18, 28), cream, 4);
            Line(new Vector2(r.x, r.y + 21), new Vector2(r.center.x, r.y), 8, ink);
            Line(new Vector2(r.center.x, r.y), new Vector2(r.xMax, r.y + 21), 8, ink);
        }

        void Arrow(Vector2 start, Vector2 end)
        {
            Line(start, end, 3, WithAlpha(teal, .5f));
            Line(end - new Vector2(8, 7), end, 3, WithAlpha(teal, .5f));
            Line(end - new Vector2(8, -7), end, 3, WithAlpha(teal, .5f));
        }

        void Line(Vector2 start, Vector2 end, float thickness, Color color)
        {
            Matrix4x4 previous = GUI.matrix;
            Vector2 delta = end - start;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            Fill(new Rect(start.x, start.y - thickness * .5f, delta.magnitude, thickness), color);
            GUI.matrix = previous;
        }

        string Percent(float value) => Game.IsTurkish ? "%" + Mathf.RoundToInt(value * 100) : Mathf.RoundToInt(value * 100) + "%";

        bool Button(Rect rect, string caption, Color background, Color foreground, int size, bool enabled = true)
        {
            bool previous = GUI.enabled;
            enabled = enabled && previous;
            ButtonFace(rect, caption, background, foreground, size, enabled);
            GUI.enabled = enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, hitStyle);
            GUI.enabled = previous;
            return clicked;
        }

        void ButtonFace(Rect rect, string caption, Color background, Color foreground, int size, bool enabled = true)
        {
            bool hover = enabled && GUI.enabled && rect.Contains(Event.current.mousePosition);
            Rounded(new Rect(rect.x, rect.y + 3, rect.width, rect.height), WithAlpha(ink, .11f), 12);
            Rounded(rect, hover ? Color.Lerp(background, Color.white, .1f) : background, 12);
            Text(rect, caption, size, foreground, true, TextAnchor.MiddleCenter);
        }

        void Panel(Rect r, Color color, float radius)
        {
            Rounded(new Rect(r.x, r.y + 6, r.width, r.height), WithAlpha(ink, .13f), radius);
            Rounded(r, color, radius);
        }

        void Text(Rect r, string value, int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.normal.textColor = color;
            GUI.Label(r, value, label);
        }

        void Rounded(Rect r, Color color, float radius)
        {
            if (r.width <= 0 || r.height <= 0) return;
            radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
            if (radius < .5f) { Fill(r, color); return; }
            Fill(new Rect(r.x + radius, r.y, r.width - radius * 2, r.height), color);
            Fill(new Rect(r.x, r.y + radius, r.width, r.height - radius * 2), color);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, radius, radius), circle, new Rect(0, .5f, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - radius, r.y, radius, radius), circle, new Rect(.5f, .5f, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - radius, radius, radius), circle, new Rect(0, 0, .5f, .5f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - radius, r.yMax - radius, radius, radius), circle, new Rect(.5f, 0, .5f, .5f));
            GUI.color = previous;
        }

        void Fill(Rect r, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void Disc(Rect r, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, circle);
            GUI.color = previous;
        }

        static Texture2D MakeCircle()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Release overlay corners", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - distance));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        static Color Hex(uint value) => new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);
    }
}
