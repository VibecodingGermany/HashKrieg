namespace Nova.Gameplay
{
    /// <summary>
    /// The read-only presentation channel between the minimap HUD
    /// (Nova.Presentation.UI) and the camera rig (Nova.Presentation). The two
    /// Presentation assemblies may NOT reference each other (same rank,
    /// gate-enforced), so both directions of the minimap&lt;-&gt;camera
    /// traffic pass through this Nova.Gameplay static, which both can see:
    /// <para>
    /// POSE (minimap -> needs, camera -> has): the rig calls
    /// <see cref="PublishPose"/> every LateUpdate; the minimap reads it to
    /// draw the viewport rectangle via
    /// <see cref="MinimapRenderer.TryComputeGroundViewCorners"/>.
    /// </para>
    /// <para>
    /// JUMP (minimap -> has, camera -> needs): the minimap detects clicks
    /// inside its own rect (it owns the rect math), converts them to world
    /// coordinates and calls <see cref="RequestFocus"/>; the rig consumes the
    /// request and applies its own <c>FocusOn</c> — the camera jump stays in
    /// the camera.
    /// </para>
    /// Pure presentation state: nothing here reads or mutates the simulation,
    /// so determinism is unaffected by construction. Unity-free, like
    /// <see cref="MinimapRenderer"/>.
    /// </summary>
    public static class MinimapCameraLink
    {
        private static bool _focusRequested;
        private static float _requestedX;
        private static float _requestedZ;

        /// <summary>True once the rig published at least one pose this session.</summary>
        public static bool HasPose { get; private set; }

        /// <summary>Focus point on the y=0 ground plane the rig looks at (world X).</summary>
        public static float FocusX { get; private set; }

        /// <summary>Focus point on the y=0 ground plane the rig looks at (world Z).</summary>
        public static float FocusZ { get; private set; }

        /// <summary>Camera height above the ground plane (the rig's zoom state).</summary>
        public static float Height { get; private set; }

        /// <summary>Camera pitch in degrees below the horizontal.</summary>
        public static float PitchDegrees { get; private set; }

        /// <summary>Camera yaw in degrees around the world Y axis.</summary>
        public static float YawDegrees { get; private set; }

        /// <summary>Vertical field of view in degrees (Camera.fieldOfView).</summary>
        public static float VerticalFovDegrees { get; private set; }

        /// <summary>Viewport aspect ratio width/height (Camera.aspect).</summary>
        public static float Aspect { get; private set; }

        /// <summary>Written by the rig every LateUpdate; read by the minimap HUD.</summary>
        public static void PublishPose(
            float focusX, float focusZ, float height,
            float pitchDegrees, float yawDegrees,
            float verticalFovDegrees, float aspect)
        {
            FocusX = focusX;
            FocusZ = focusZ;
            Height = height;
            PitchDegrees = pitchDegrees;
            YawDegrees = yawDegrees;
            VerticalFovDegrees = verticalFovDegrees;
            Aspect = aspect;
            HasPose = true;
        }

        /// <summary>Written by the minimap HUD on a click inside its rect; world ground position.</summary>
        public static void RequestFocus(float worldX, float worldZ)
        {
            _requestedX = worldX;
            _requestedZ = worldZ;
            _focusRequested = true;
        }

        /// <summary>
        /// Consumed by the rig's LateUpdate: returns the pending jump target
        /// exactly once. A request the rig never consumes (disabled camera)
        /// simply lingers until the next session reset — it never reaches the
        /// simulation.
        /// </summary>
        public static bool TryConsumeFocusRequest(out float worldX, out float worldZ)
        {
            worldX = _requestedX;
            worldZ = _requestedZ;
            if (!_focusRequested) return false;
            _focusRequested = false;
            return true;
        }

        /// <summary>
        /// Clears pose and pending request. Called by the rig's OnEnable so a
        /// session never opens with a stale viewport rectangle or a queued
        /// jump from a previous play session (static state survives play-mode
        /// transitions when domain reload is off).
        /// </summary>
        public static void Reset()
        {
            HasPose = false;
            _focusRequested = false;
            _requestedX = 0f;
            _requestedZ = 0f;
        }
    }
}
