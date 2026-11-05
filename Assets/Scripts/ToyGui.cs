using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Shared look for the shift HUD and the lobby: rounded panels, discs, stars and bold labels.</summary>
    public static class ToyGui
    {
        public static readonly Color Ink = Hex(0x193B4B), Muted = Hex(0x66818B), Cream = Hex(0xFFF9ED), Teal = Hex(0x119E99),
            Coral = Hex(0xE97760), Gold = Hex(0xF3BF48), Pale = Hex(0xEAF3EA), StarOff = Hex(0xCFD8D2), Violet = Hex(0x7E66C4),
            Sky = Hex(0x56B4D6), Leaf = Hex(0x56B38A), CoinLight = Hex(0xFFE08A);

        static Texture2D rounded, roundedSmall, roundedMid, disc, star, white;
        static GUIStyle box, thin, mid, label;
        public static GUIStyle HitStyle { get; private set; }
        public static Texture2D Disc { get { Ensure(); return disc; } }
        public static Texture2D Star { get { Ensure(); return star; } }

        public static void Ensure()
        {
            if (box != null && rounded) return;
            rounded = MakeRounded(64, 22);
            roundedSmall = MakeRounded(12, 4);
            roundedMid = MakeRounded(24, 8);
            disc = MakeRounded(64, 32);
            star = MakeStar(64);
            white = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            white.SetPixel(0, 0, Color.white); white.Apply();
            box = new GUIStyle(GUIStyle.none) { normal = { background = rounded }, border = new RectOffset(22, 22, 22, 22) };
            thin = new GUIStyle(GUIStyle.none) { normal = { background = roundedSmall }, border = new RectOffset(4, 4, 4, 4) };
            mid = new GUIStyle(GUIStyle.none) { normal = { background = roundedMid }, border = new RectOffset(8, 8, 8, 8) };
            label = new GUIStyle(GUI.skin.label) { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), wordWrap = true,
                richText = false, padding = new RectOffset(0, 0, 0, 0) };
            HitStyle = new GUIStyle(GUIStyle.none);
        }

        // Small rects get a smaller corner so short buttons on phones keep their shape.
        public static void Panel(Rect r, Color c)
        {
            float edge = Mathf.Min(r.width, r.height);
            GUIStyle style = edge >= 46 ? box : edge >= 18 ? mid : thin;
            GUI.color = c; GUI.Box(r, GUIContent.none, style); GUI.color = Color.white;
        }
        public static void Thin(Rect r, Color c) { GUI.color = c; GUI.Box(r, GUIContent.none, thin); GUI.color = Color.white; }
        public static void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, white); GUI.color = Color.white; }
        public static void Icon(Rect r, Texture2D t, Color c) { GUI.color = c; GUI.DrawTexture(r, t); GUI.color = Color.white; }
        public static bool Hit(Rect r) => GUI.Button(r, GUIContent.none, HitStyle);

        /// <summary>A panel with a darker lip underneath, like a pressable toy button.</summary>
        public static void Raised(Rect r, Color c)
        {
            Panel(new Rect(r.x, r.y + 5, r.width, r.height), Color.Lerp(c, Ink, .35f));
            Panel(r, c);
        }

        public static bool Button(Rect r, string text, Color fill, Color ink, int size, bool enabled = true)
        {
            if (!enabled) { fill = Color.Lerp(fill, StarOff, .7f); ink = Muted; }
            Raised(r, fill);
            Text(r, text, size, ink, true, TextAnchor.MiddleCenter);
            return enabled && Hit(r);
        }

        public static void Text(Rect r, string text, int size, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            label.fontSize = Mathf.Max(1, size); label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.alignment = anchor;
            label.normal.textColor = color;
            GUI.Label(r, text, label);
        }

        /// <summary>Text with a soft drop shadow, for titles over the 3D city.</summary>
        public static void Title(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            Text(new Rect(r.x + 2, r.y + 4, r.width, r.height), text, size, Alpha(Ink, .45f), true, anchor);
            Text(r, text, size, color, true, anchor);
        }

        public static void Coin(Vector2 center, float size)
        {
            float rim = size * 1.14f;
            Icon(new Rect(center.x - rim * .5f, center.y - rim * .5f, rim, rim), Disc, Hex(0xC07F1C));
            Icon(new Rect(center.x - size * .5f, center.y - size * .5f, size, size), Disc, Gold);
            Icon(new Rect(center.x - size * .25f, center.y - size * .25f, size * .5f, size * .5f), Disc, CoinLight);
        }

        public static Color Alpha(Color c, float a) { c.a = a; return c; }
        public static Color Hex(int rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);

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
    }
}
