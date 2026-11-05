using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Shift strip (stars, clock, level), level-up card picker and the shift-over screen.</summary>
    public sealed class ShiftHUD : MonoBehaviour
    {
        public CourierGame Game;

        static readonly Color Ink = Hex(0x193B4B), Muted = Hex(0x66818B), Cream = Hex(0xFFF9ED), Teal = Hex(0x119E99),
            Coral = Hex(0xE97760), Gold = Hex(0xF3BF48), Pale = Hex(0xEAF3EA), StarOff = Hex(0xCFD8D2);
        Texture2D rounded, star, white;
        GUIStyle box, label, hit;
        static string L(string tr, string en) => GameText.Pick(tr, en);
        static float W => GuiViewport.LogicalWidth;
        static float H => GuiViewport.LogicalHeight;

        void Awake()
        {
            if (!Game) Game = GetComponent<CourierGame>();
            rounded = MakeRounded(64, 22);
            star = MakeStar(64);
            white = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            white.SetPixel(0, 0, Color.white); white.Apply();
        }

        void OnDestroy()
        {
            if (rounded) Destroy(rounded);
            if (star) Destroy(star);
            if (white) Destroy(white);
        }

        void OnGUI()
        {
            if (!Game || !Game.InShift) return;
            bool playing = Game.Phase == GamePhase.Playing;
            if (!playing && Game.Phase != GamePhase.ShiftCard && Game.Phase != GamePhase.ShiftEnd) return;
            if (box == null)
            {
                box = new GUIStyle(GUIStyle.none) { normal = { background = rounded }, border = new RectOffset(22, 22, 22, 22) };
                label = new GUIStyle(GUI.skin.label) { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), wordWrap = true,
                    richText = false, padding = new RectOffset(0, 0, 0, 0) };
                hit = new GUIStyle(GUIStyle.none);
            }
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.matrix = GuiViewport.Matrix;
            try
            {
                if (playing) DrawStrip();
                else if (Game.Phase == GamePhase.ShiftCard) DrawCards();
                else DrawEnd();
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        void DrawStrip()
        {
            Rect r = GuiViewport.Compact ? new Rect(12, H - 48, W - 136, 40) : new Rect(240, 146, 548, 58);
            bool rush = Game.IsRushHour;
            bool pulse = rush && !Game.ReducedMotion && Mathf.Repeat(Time.unscaledTime, .8f) < .4f;
            Panel(r, pulse ? Hex(0xFBD3C4) : Alpha(Cream, .97f));
            float s = r.height - 16;
            for (int i = 0; i < CourierGame.ShiftStarCount; i++)
                Icon(new Rect(r.x + 12 + i * (s + 4), r.y + 8, s, s), star, i < Game.ShiftStars ? Gold : StarOff);
            float x = r.x + 24 + 3 * (s + 4);
            int seconds = Mathf.CeilToInt(Game.ShiftTimeLeft);
            string clock = seconds / 60 + ":" + (seconds % 60).ToString("00");
            float clockWidth = GuiViewport.Compact ? 70 : 84;
            Text(new Rect(x, r.y, clockWidth, r.height), clock, GuiViewport.Compact ? 20 : 26, rush ? Coral : Ink, true, TextAnchor.MiddleLeft);
            x += clockWidth;
            if (rush) { Text(new Rect(x, r.y, 120, r.height), L("YOĞUN SAAT ×2", "RUSH HOUR ×2"), 13, Coral, true, TextAnchor.MiddleLeft); x += 120; }
            float right = r.xMax - 12;
            float barWidth = Mathf.Max(60, right - x - 64);
            Rect badge = new Rect(right - barWidth - 58, r.y + 6, 50, r.height - 12);
            Panel(badge, Teal);
            Text(badge, L("Sv ", "Lv ") + Game.ShiftLevel, GuiViewport.Compact ? 14 : 17, Color.white, true, TextAnchor.MiddleCenter);
            Rect bar = new Rect(right - barWidth, r.y + r.height * .5f - 6, barWidth, 12);
            Panel(bar, Hex(0xDBE5DE));
            if (Game.ShiftXp01 > .01f) Panel(new Rect(bar.x, bar.y, Mathf.Max(12, bar.width * Game.ShiftXp01), bar.height), Gold);
        }

        void DrawCards()
        {
            Fill(GuiViewport.FullScreenRect, new Color(.04f, .10f, .13f, .62f));
            bool compact = GuiViewport.Compact;
            Text(new Rect(0, compact ? 12 : 70, W, 44), L("SEVİYE ATLADIN!", "LEVEL UP!"), compact ? 30 : 44, Gold, true, TextAnchor.MiddleCenter);
            Text(new Rect(0, compact ? 52 : 124, W, 30), L("Bir güç seç", "Pick a power"), compact ? 17 : 22, Color.white, true, TextAnchor.MiddleCenter);
            int count = Game.ShiftOfferCount;
            float gap = compact ? 12 : 24;
            float cardW = Mathf.Min(compact ? 220 : 300, (W - 48 - gap * (count - 1)) / count);
            float cardH = compact ? H - 108 : 420;
            float startX = (W - (cardW * count + gap * (count - 1))) * .5f, y = compact ? 92 : 190;
            for (int i = 0; i < count; i++)
            {
                ShiftCard card = Game.ShiftOffer(i);
                Rect c = new Rect(startX + i * (cardW + gap), y, cardW, cardH);
                Color tint = CourierGame.CardColor(card);
                Panel(c, Cream);
                Panel(new Rect(c.x, c.y, c.width, cardH * .40f), tint);
                int level = Game.CardLevel(card);
                float dot = compact ? 14 : 20, dots = CourierGame.CardMaxLevel * dot + (CourierGame.CardMaxLevel - 1) * 8;
                for (int d = 0; d < CourierGame.CardMaxLevel; d++)
                    Icon(new Rect(c.center.x - dots * .5f + d * (dot + 8), c.y + cardH * .40f - dot - 10, dot, dot), star,
                        d < level ? Color.white : d == level ? Gold : Alpha(Color.white, .35f));
                Text(new Rect(c.x + 10, c.y + (compact ? 6 : 14), c.width - 20, 24), level == 0 ? L("YENİ", "NEW") : L("Seviye ", "Level ") + (level + 1),
                    compact ? 13 : 17, Alpha(Color.white, .9f), true, TextAnchor.UpperCenter);
                Text(new Rect(c.x + 10, c.y + (compact ? 22 : 40), c.width - 20, cardH * .40f - dot - (compact ? 30 : 56)), CourierGame.CardBadge(card),
                    compact ? 30 : 48, Color.white, true, TextAnchor.MiddleCenter);
                Text(new Rect(c.x + 12, c.y + cardH * .43f, c.width - 24, compact ? 30 : 40), CourierGame.CardName(card), compact ? 19 : 26, Ink, true, TextAnchor.MiddleCenter);
                Text(new Rect(c.x + 14, c.y + cardH * .43f + (compact ? 32 : 46), c.width - 28, compact ? 50 : 80), CourierGame.CardDescription(card),
                    compact ? 14 : 18, Muted, false, TextAnchor.UpperCenter);
                Rect pick = new Rect(c.x + 14, c.yMax - (compact ? 46 : 64), c.width - 28, compact ? 36 : 50);
                Panel(pick, tint);
                Text(pick, L("SEÇ", "PICK"), compact ? 16 : 20, Color.white, true, TextAnchor.MiddleCenter);
                if (GUI.Button(c, GUIContent.none, hit)) Game.ChooseShiftCard(i);
            }
        }

        void DrawEnd()
        {
            Fill(GuiViewport.FullScreenRect, new Color(.04f, .10f, .13f, .55f));
            bool compact = GuiViewport.Compact;
            float pw = Mathf.Min(compact ? 620 : 560, W - 32), ph = compact ? H - 24 : 600;
            Rect p = new Rect((W - pw) * .5f, (H - ph) * .5f, pw, ph);
            Panel(p, Cream);
            bool won = Game.ShiftSucceeded;
            Text(new Rect(p.x, p.y + (compact ? 10 : 30), p.width, 44), won ? L("VARDİYA TAMAM!", "SHIFT COMPLETE!") : L("VARDİYA BİTTİ", "SHIFT OVER"),
                compact ? 28 : 40, won ? Teal : Coral, true, TextAnchor.MiddleCenter);
            Text(new Rect(p.x + 20, p.y + (compact ? 50 : 82), p.width - 40, 26),
                won ? L("Mahalle sana bayıldı!", "The neighborhood loves you!") : L("Yıldızların bitti. Bir dahakine!", "Out of stars. Next time!"),
                compact ? 15 : 19, Muted, false, TextAnchor.MiddleCenter);

            float rowY = p.y + (compact ? 82 : 132), rowH = compact ? 24 : 36;
            Row(p, ref rowY, rowH, L("Teslimat", "Deliveries"), Game.ShiftDeliveries.ToString());
            Row(p, ref rowY, rowH, L("En uzun seri", "Best streak"), "×" + Mathf.Max(1, Game.ShiftBestCombo));
            Row(p, ref rowY, rowH, L("Seviye", "Level"), Game.ShiftLevel.ToString());
            if (Game.ShiftBonus > 0) Row(p, ref rowY, rowH, L("Vardiya bonusu", "Shift bonus"), "+" + Game.ShiftBonus);
            Row(p, ref rowY, rowH, L("Toplam kazanç", "Total earned"), Game.ShiftEarnings + L(" jeton", " coins"), true);

            string test = Game.CommerceIsMock ? "TEST • " : "";
            float bh = compact ? 42 : 56, gap = 10;
            float by = p.yMax - (compact ? 12 : 24);
            if (compact)
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
            int size = GuiViewport.Compact ? (strong ? 18 : 15) : (strong ? 24 : 19);
            Text(new Rect(p.x + 40, y, p.width * .55f, h), name, size, strong ? Ink : Muted, strong, TextAnchor.MiddleLeft);
            Text(new Rect(p.x + p.width * .45f, y, p.width * .55f - 40, h), value, size, strong ? Teal : Ink, true, TextAnchor.MiddleRight);
            y += h;
        }

        bool Button(Rect r, string text, Color fill, Color ink, int size)
        {
            Panel(r, fill);
            Text(r, text, size, ink, true, TextAnchor.MiddleCenter);
            return GUI.Button(r, GUIContent.none, hit);
        }

        void Panel(Rect r, Color c) { GUI.color = c; GUI.Box(r, GUIContent.none, box); GUI.color = Color.white; }
        void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, white); GUI.color = Color.white; }
        void Icon(Rect r, Texture2D t, Color c) { GUI.color = c; GUI.DrawTexture(r, t); GUI.color = Color.white; }

        void Text(Rect r, string text, int size, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            label.fontSize = size; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.alignment = anchor;
            label.normal.textColor = color;
            GUI.Label(r, text, label);
        }

        static Texture2D MakeRounded(int size, float radius)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - (x + .5f), x + .5f - (size - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - (y + .5f), y + .5f - (size - radius)));
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f));
                }
            t.SetPixels(pixels); t.Apply();
            return t;
        }

        static Texture2D MakeStar(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * .5f + i * Mathf.PI / 5;
                float radius = (i % 2 == 0 ? .48f : .21f) * size;
                points[i] = new Vector2(size * .5f + Mathf.Cos(angle) * radius, size * .5f + Mathf.Sin(angle) * radius);
            }
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // 4x supersampling for soft edges.
                    int inside = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            if (InPolygon(points, new Vector2(x + .25f + sx * .5f, y + .25f + sy * .5f))) inside++;
                    pixels[y * size + x] = new Color(1, 1, 1, inside / 4f);
                }
            t.SetPixels(pixels); t.Apply();
            return t;
        }

        static bool InPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside;
            return inside;
        }

        static Color Alpha(Color c, float a) { c.a = a; return c; }
        static Color Hex(int rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);
    }
}
