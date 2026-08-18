namespace Nova.Gameplay
{
    /// <summary>
    /// Read-only modal-surface channel between the HUD layer
    /// (Nova.Presentation.UI) and anyone who must know whether a modal panel
    /// currently owns the player's input. Same pattern as
    /// <see cref="HudPointerLink"/>: a static in Nova.Gameplay both
    /// Presentation assemblies can see, no new assembly edge.
    /// <para>
    /// The semantics are exactly "a modal surface owns the input this frame"
    /// — the pause menu, the match-result screen, a network status panel —
    /// and deliberately NOT "the match is not running": a paused match and a
    /// match without any modal are different states, and consumers that need
    /// the clock read <c>MatchRunner.IsRunning</c> instead.
    /// </para>
    /// <para>
    /// ONE writer: the component that draws the modal panels publishes once
    /// per frame (today PauseMenuHud, which aggregates MatchFrameHud's
    /// result/network panels). The value is a per-frame verdict, never a
    /// latch with memory — a stuck <c>true</c> would deadlock every world
    /// gesture for the rest of the session, so the writer's OnDisable calls
    /// <see cref="Reset"/> (the UI root switch-off on the way to the main
    /// menu is the case that must never leave a stale <c>true</c> behind).
    /// The reader is RtsDeviceInput, which suspends world gestures and
    /// re-publishes through <see cref="HudPointerLink"/> so the camera's
    /// edge-pan strip stops too without gaining a reader of its own.
    /// </para>
    /// </summary>
    public static class ModalSurfaceLink
    {
        /// <summary>The last published verdict: a modal surface owns the input this frame.</summary>
        public static bool Open { get; private set; }

        /// <summary>Publishes this frame's verdict. Called by the modal-drawing component every frame.</summary>
        public static void Publish(bool open)
        {
            Open = open;
        }

        /// <summary>Clears the verdict (writer disabled, play-mode transitions, domain-reload-off safety).</summary>
        public static void Reset()
        {
            Open = false;
        }
    }
}
