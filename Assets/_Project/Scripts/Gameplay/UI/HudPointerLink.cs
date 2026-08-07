namespace Nova.Gameplay
{
    /// <summary>
    /// Read-only pointer channel between the HUD (Nova.Presentation.UI) and
    /// assemblies that may NOT reference it (same rank, gate-enforced — e.g.
    /// the camera rig in Nova.Presentation). The input component is the ONLY
    /// writer: it publishes once per frame whether the pointer sits over any
    /// HUD panel. Consumers read it to suppress world gestures that would
    /// otherwise fire under the HUD — the concrete case being the camera's
    /// edge-pan strip, which used to scroll the map while the player aimed
    /// for the build bar or the minimap at the screen edge.
    /// <para>
    /// Same pattern as <see cref="MinimapCameraLink"/>: a static in
    /// Nova.Gameplay both sides can see, no assembly edge between the two
    /// Presentation assemblies. State is session-transient (a per-frame
    /// verdict, not accumulated data); <see cref="Reset"/> clears it on
    /// play-mode transitions so a stale "over HUD" from the previous session
    /// cannot freeze the camera.
    /// </para>
    /// </summary>
    public static class HudPointerLink
    {
        /// <summary>The last published verdict: the pointer is over a HUD panel this frame.</summary>
        public static bool PointerOverHud { get; private set; }

        /// <summary>Publishes this frame's verdict. Called by the input component every frame.</summary>
        public static void Publish(bool pointerOverHud)
        {
            PointerOverHud = pointerOverHud;
        }

        /// <summary>Clears the verdict (play-mode transitions, domain-reload-off safety).</summary>
        public static void Reset()
        {
            PointerOverHud = false;
        }
    }
}
