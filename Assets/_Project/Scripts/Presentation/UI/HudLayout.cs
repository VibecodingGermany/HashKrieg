using UnityEngine;
using Nova.Gameplay;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The HUD zone model (D-085): the ONE place that reads
    /// <c>Screen.width/height</c> for HUD panel layout. Every panel asks for
    /// its zone here instead of computing its own rect — the zones interlock
    /// through their shared parameters, so overlap is excluded by
    /// construction, not fixed panel by panel. The rect math itself lives in
    /// <see cref="HudLayoutMath"/> (Nova.Gameplay, Unity-free,
    /// EditMode-tested); this class only adapts it to the screen and to
    /// <see cref="Rect"/>.
    /// <para>
    /// ZONES in scaled GUI space (top-left origin): the status strip along
    /// the top edge (<see cref="DebugHud"/>), the minimap bottom-left and
    /// the command card bottom-right, both docked above the build bar's
    /// reserve (<see cref="BuildMenuHud.OccupiedHeight"/>), the build bar
    /// bottom-center with its status line, and the F3 debug panel in the
    /// remaining free field between the strip, the minimap and the card
    /// column.
    /// </para>
    /// <para>
    /// SCOPE: panel LAYOUT only. Raw-pixel conversions that are not layout
    /// (the selection drag rectangle in RtsDeviceInput.OnGUI) keep their own
    /// math; the raw-mouse to scaled-GUI conversion the hit tests share is
    /// <see cref="RawMouseToGui"/> here, so no panel opens the Screen API
    /// itself.
    /// </para>
    /// </summary>
    internal static class HudLayout
    {
        /// <summary>Screen size in scaled GUI space, scale clamped like every panel does.</summary>
        public static float GuiWidth(float scale) => Screen.width / Mathf.Max(1f, scale);
        public static float GuiHeight(float scale) => Screen.height / Mathf.Max(1f, scale);

        /// <summary>Top-edge strip of the always-on status line.</summary>
        public static Rect StatusStrip(float scale, float stripHeight, float margin)
        {
            return ToRect(HudLayoutMath.StatusStrip(GuiWidth(scale), margin, stripHeight));
        }

        /// <summary>Bottom-center zone of the build bar: status line plus button row, bottom at the screen margin.</summary>
        public static Rect BuildBarZone(float scale, float contentWidth, float contentHeight, float margin)
        {
            return ToRect(HudLayoutMath.BottomCenterZone(
                GuiWidth(scale), GuiHeight(scale), contentWidth, contentHeight, margin));
        }

        /// <summary>Bottom-left square docked one margin above <paramref name="reserveBelow"/> (the build bar's occupied height).</summary>
        public static Rect MinimapZone(float scale, float size, float margin, float reserveBelow)
        {
            return ToRect(HudLayoutMath.BottomLeftZoneAbove(GuiHeight(scale), size, margin, reserveBelow));
        }

        /// <summary>
        /// The minimap zone with the canonical defaults
        /// (<see cref="MinimapHud.DefaultMapSize"/>/<see cref="MinimapHud.DefaultMargin"/>
        /// over <see cref="BuildMenuHud.DefaultOccupiedHeight"/>), for panels
        /// that must clear the minimap without holding a reference to it —
        /// the F3 debug panel bounds its free field with this.
        /// </summary>
        public static Rect CanonicalMinimapZone(float scale)
        {
            return MinimapZone(scale,
                MinimapHud.DefaultMapSize, MinimapHud.DefaultMargin, BuildMenuHud.DefaultOccupiedHeight);
        }

        /// <summary>Bottom-right, content-sized (the card's height follows its model), docked like the minimap.</summary>
        public static Rect CommandCardZone(float scale, float width, float height, float margin, float reserveBelow)
        {
            return ToRect(HudLayoutMath.BottomRightZoneAbove(
                GuiWidth(scale), GuiHeight(scale), width, height, margin, reserveBelow));
        }

        /// <summary>
        /// The free field of the F3 debug panel: between the strip and the
        /// top of the (canonical) minimap zone, never entering the command
        /// card's column (<see cref="CommandCardHud.DefaultPanelWidth"/>) —
        /// degenerate (zero-size) when the window cannot fit one, and the
        /// panel draws nothing then instead of overlapping.
        /// </summary>
        public static Rect DebugPanelZone(float scale, float maxWidth, float margin, float top, float bottomBound)
        {
            float cardLeft = GuiWidth(scale) - margin - CommandCardHud.DefaultPanelWidth;
            return ToRect(HudLayoutMath.FreeFieldZone(GuiWidth(scale), maxWidth, margin, top, bottomBound, cardLeft));
        }

        /// <summary>
        /// Raw mouse position (bottom-left origin, unscaled pixels) to scaled
        /// top-left GUI space — the conversion every panel hit test shares,
        /// centralized so the Screen read stays here.
        /// </summary>
        public static Vector2 RawMouseToGui(Vector2 rawMousePosition, float scale)
        {
            float safeScale = Mathf.Max(1f, scale);
            return new Vector2(rawMousePosition.x / safeScale, (Screen.height - rawMousePosition.y) / safeScale);
        }

        private static Rect ToRect(HudRect zone) => new Rect(zone.X, zone.Y, zone.Width, zone.Height);
    }
}
