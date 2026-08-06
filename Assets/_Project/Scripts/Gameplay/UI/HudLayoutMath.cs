using System;

namespace Nova.Gameplay
{
    /// <summary>
    /// An axis-aligned rectangle in scaled GUI space (top-left origin), the
    /// currency of <see cref="HudLayoutMath"/>. Unity-free on purpose, like
    /// <see cref="MinimapRenderer"/>: the zone math is EditMode-tested here
    /// and only converted to <c>UnityEngine.Rect</c> at the IMGUI boundary
    /// (HudLayout, Nova.Presentation.UI).
    /// </summary>
    public struct HudRect : IEquatable<HudRect>
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public HudRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float Right => X + Width;
        public float Bottom => Y + Height;

        /// <summary>Strict area overlap; touching edges do not count, so docked zones never "overlap".</summary>
        public bool Overlaps(HudRect other)
        {
            return X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
        }

        public bool Equals(HudRect other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj) => obj is HudRect other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"HudRect({X},{Y} {Width}x{Height})";
    }

    /// <summary>
    /// The HUD zone model (D-085): the five screen zones of the graybox
    /// cockpit, computed from the scaled GUI-space screen size and nothing
    /// else. Every panel ASKS for its zone instead of computing its own
    /// rect, so overlap is excluded by construction — the zones interlock
    /// through their shared parameters (the bottom-left and bottom-right
    /// zones dock above the bottom-center zone's reserve, the free field is
    /// bounded by the strip above and the bottom-left zone below).
    /// <para>
    /// ZONES: status strip along the top edge; minimap bottom-left, docked
    /// above the bottom reserve; build bar bottom-center (its content
    /// includes the status line above the buttons); command card
    /// bottom-right, docked above the same reserve; the debug panel in the
    /// remaining free field between the strip and the bottom-left zone.
    /// </para>
    /// <para>
    /// Pure C# (no UnityEngine): EditMode tests (HudLayoutMathTests) pin the
    /// interlock relations and the no-overlap invariant at representative
    /// window sizes. HudLayout (Nova.Presentation.UI) is the thin Screen-reading
    /// adapter over this math.
    /// </para>
    /// </summary>
    public static class HudLayoutMath
    {
        /// <summary>Top edge: full width minus the side margins, <paramref name="stripHeight"/> tall.</summary>
        public static HudRect StatusStrip(float screenWidth, float margin, float stripHeight)
        {
            return new HudRect(margin, margin, Math.Max(0f, screenWidth - 2f * margin), stripHeight);
        }

        /// <summary>
        /// Bottom center: the build bar, horizontally centered, its bottom
        /// edge <paramref name="margin"/> above the screen bottom.
        /// <paramref name="contentHeight"/> covers the whole zone content
        /// (status line plus button row), so anything docking above the bar
        /// clears both.
        /// </summary>
        public static HudRect BottomCenterZone(
            float screenWidth, float screenHeight, float contentWidth, float contentHeight, float margin)
        {
            return new HudRect(
                (screenWidth - contentWidth) * 0.5f,
                screenHeight - margin - contentHeight,
                contentWidth, contentHeight);
        }

        /// <summary>
        /// Bottom left, docked above <paramref name="reserveBelow"/> (the
        /// bottom-center zone's occupied height) with one margin of air.
        /// Clamped onto the screen when the window is too short.
        /// </summary>
        public static HudRect BottomLeftZoneAbove(float screenHeight, float size, float margin, float reserveBelow)
        {
            float y = screenHeight - reserveBelow - margin - size;
            return new HudRect(margin, Math.Max(margin, y), size, size);
        }

        /// <summary>
        /// Bottom right, same docking as <see cref="BottomLeftZoneAbove"/> but
        /// content-sized (the command card's height follows its model), right
        /// edge at the screen margin. Clamped onto the screen when the card
        /// outgrows a short window.
        /// </summary>
        public static HudRect BottomRightZoneAbove(
            float screenWidth, float screenHeight, float width, float height, float margin, float reserveBelow)
        {
            float y = screenHeight - reserveBelow - margin - height;
            return new HudRect(
                Math.Max(margin, screenWidth - margin - width),
                Math.Max(margin, y),
                width, height);
        }

        /// <summary>
        /// The remaining free field for the debug panel: left at the margin,
        /// <paramref name="top"/> below the status strip, bottom one margin
        /// above <paramref name="bottomBound"/> (the top edge of the
        /// bottom-left zone), width capped at <paramref name="maxWidth"/> AND
        /// at <paramref name="rightNeighborLeft"/> (the command card's left
        /// edge) minus the margins — the free field never enters the card's
        /// column, no matter how tall the card currently is. A degenerate
        /// (zero-size) rect when the window cannot fit one — the panel skips
        /// drawing then instead of overlapping.
        /// </summary>
        public static HudRect FreeFieldZone(
            float screenWidth, float maxWidth, float margin, float top, float bottomBound, float rightNeighborLeft)
        {
            float width = Math.Min(maxWidth, screenWidth - 2f * margin);
            width = Math.Min(width, rightNeighborLeft - 2f * margin);
            if (width < 0f) width = 0f;
            float height = bottomBound - margin - top;
            if (height < 0f) height = 0f;
            return new HudRect(margin, top, width, height);
        }
    }
}
