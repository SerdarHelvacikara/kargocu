using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>One safe-area transform for drawing, hit testing and projected world labels.</summary>
    public static class GuiViewport
    {
        public const float Width = 1280;
        public const float Height = 800;
        public const float CompactHeight = 360;
        static readonly bool ForceCompact = Array.Exists(Environment.GetCommandLineArgs(), value => value == "-compactUI");

        public static bool Compact => ForceCompact || (Application.isMobilePlatform && Screen.width > Screen.height &&
            SafeScreenRect.width / SafeScreenRect.height >= 1.55f);
        public static float LogicalWidth => Current.LogicalWidth;
        public static float LogicalHeight => Current.LogicalHeight;

        public readonly struct Layout
        {
            public readonly Vector2 ScreenSize;
            public readonly Rect SafeScreenRect;
            public readonly float LogicalWidth, LogicalHeight, Scale;
            public readonly Vector2 Offset;

            public Layout(Vector2 screenSize, Rect safeScreenRect, float logicalWidth, float logicalHeight)
            {
                ScreenSize = screenSize;
                SafeScreenRect = safeScreenRect;
                LogicalWidth = logicalWidth;
                LogicalHeight = logicalHeight;
                Scale = Mathf.Min(safeScreenRect.width / logicalWidth, safeScreenRect.height / logicalHeight);
                Offset = new Vector2(safeScreenRect.xMin + (safeScreenRect.width - logicalWidth * Scale) * .5f,
                    screenSize.y - safeScreenRect.yMax + (safeScreenRect.height - logicalHeight * Scale) * .5f);
            }

            public Matrix4x4 Matrix => Matrix4x4.TRS(new Vector3(Offset.x, Offset.y, 0), Quaternion.identity, new Vector3(Scale, Scale, 1));
            public Vector2 ScreenToDesign(Vector2 point) => (new Vector2(point.x, ScreenSize.y - point.y) - Offset) / Scale;
            public Vector2 DesignToScreen(Vector2 point)
            {
                Vector2 topLeft = Offset + point * Scale;
                return new Vector2(topLeft.x, ScreenSize.y - topLeft.y);
            }
            public Rect FullScreenRect
            {
                get
                {
                    Vector2 origin = ScreenToDesign(new Vector2(0, ScreenSize.y));
                    return new Rect(origin.x, origin.y, ScreenSize.x / Scale, ScreenSize.y / Scale);
                }
            }
        }

        /// <summary>Pure viewport math: callers can test notch/size combinations without changing the display.</summary>
        public static Layout Compute(Vector2 screenSize, Rect safeRect, bool compact)
        {
            float width = float.IsNaN(screenSize.x) || float.IsInfinity(screenSize.x) ? 1 : Mathf.Max(1, screenSize.x);
            float height = float.IsNaN(screenSize.y) || float.IsInfinity(screenSize.y) ? 1 : Mathf.Max(1, screenSize.y);
            if (safeRect.width <= 0 || safeRect.height <= 0 || !Finite(safeRect.x) || !Finite(safeRect.y) ||
                !Finite(safeRect.width) || !Finite(safeRect.height)) safeRect = new Rect(0, 0, width, height);
            float left = Mathf.Clamp(safeRect.xMin, 0, width - 1);
            float bottom = Mathf.Clamp(safeRect.yMin, 0, height - 1);
            float right = Mathf.Clamp(safeRect.xMax, left + 1, width);
            float top = Mathf.Clamp(safeRect.yMax, bottom + 1, height);
            Rect safe = Rect.MinMaxRect(left, bottom, right, top);
            float logicalHeight = compact ? CompactHeight : Height;
            float logicalWidth = compact ? CompactHeight * safe.width / safe.height : Width;
            return new Layout(new Vector2(width, height), safe, logicalWidth, logicalHeight);
        }

        public static Layout Compute(float screenWidth, float screenHeight, Rect safeRect, bool compact) =>
            Compute(new Vector2(screenWidth, screenHeight), safeRect, compact);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static Layout Current => Compute(new Vector2(Screen.width, Screen.height), Screen.safeArea, Compact);

        public static Rect SafeScreenRect
        {
            get
            {
                return Compute(new Vector2(Screen.width, Screen.height), Screen.safeArea, false).SafeScreenRect;
            }
        }

        public static float Scale
        {
            get
            {
                return Current.Scale;
            }
        }

        public static Vector2 Offset
        {
            get
            {
                return Current.Offset;
            }
        }

        public static Matrix4x4 Matrix
        {
            get
            {
                return Current.Matrix;
            }
        }

        /// <param name="screenPoint">Unity screen coordinates: origin at bottom-left.</param>
        public static Vector2 ScreenToDesign(Vector2 screenPoint)
        {
            return Current.ScreenToDesign(screenPoint);
        }

        public static Vector2 DesignToScreen(Vector2 designPoint)
        {
            return Current.DesignToScreen(designPoint);
        }

        /// <summary>For dimming the entire display, including areas beyond the safe content.</summary>
        public static Rect FullScreenRect
        {
            get
            {
                return Current.FullScreenRect;
            }
        }
    }
}
