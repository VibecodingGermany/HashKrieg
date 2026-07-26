using UnityEngine;

namespace Nova.Presentation
{
    /// <summary>
    /// Top-down RTS camera rig: arrow-key panning, screen-edge panning, mouse-wheel zoom along the
    /// view axis and Z/X yaw around the focus point, with the focus clamped to the map rectangle.
    /// Uses the legacy input backend (ProjectSettings activeInputHandler: 0).
    /// PRESENTATION ONLY: never reads or mutates simulation state and never submits commands.
    /// All tuning defaults live here, so the Bootstrap scene generator must not hardcode any of them.
    /// <para>
    /// KEY BUDGET: the camera deliberately does NOT bind W/A/S/D or Q/E. Those belong to the command
    /// layer (RtsDeviceInput: S stop, A attack, Q queue) where RTS convention is fixed and there is no
    /// alternative binding; the camera has arrow keys and screen-edge panning, so it is the side that
    /// can yield. Do not re-add WASD here without moving the command hotkeys first — the collision is
    /// silent and makes every pan issue orders.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class RtsCameraController : MonoBehaviour
    {
        [Header("Map Bounds (world units; 1 tile = 1 unit)")]
        [SerializeField] private float _mapWidth = 128f;
        [SerializeField] private float _mapHeight = 128f;

        [Header("Pan")]
        [SerializeField] private float _panSpeed = 18f;
        [Tooltip("Pan speed multiplier reached at _maxHeight; scales linearly from 1x at _minHeight.")]
        [SerializeField] private float _panSpeedHeightScale = 3f;
        [SerializeField] private bool _edgePanEnabled = true;
        [Tooltip("Screen-edge band in pixels that triggers edge panning.")]
        [SerializeField] private float _edgePanMargin = 12f;

        [Header("Zoom")]
        [Tooltip("World units of camera height per unit of scroll-wheel delta.")]
        [SerializeField] private float _zoomSpeed = 60f;
        [SerializeField] private float _minHeight = 12f;
        [SerializeField] private float _maxHeight = 90f;

        [Header("Orbit")]
        [SerializeField] private float _pitchDegrees = 55f;
        [SerializeField] private float _rotationSpeed = 90f;

        [Header("Start Focus (human player HQ sits at tile 4,4)")]
        [SerializeField] private float _startFocusX = 4f;
        [SerializeField] private float _startFocusZ = 4f;
        [SerializeField] private float _startHeight = 34f;
        [SerializeField] private float _startYawDegrees = 45f;

        private Camera _camera;
        private Vector3 _focus;
        private float _yaw;
        private float _height;

        /// <summary>Camera this rig drives. Cached in Awake.</summary>
        public Camera ViewCamera => _camera;

        /// <summary>Point on the y=0 ground plane the rig looks at and rotates around.</summary>
        public Vector3 FocusPoint => _focus;

        /// <summary>Current camera height above the ground plane, always within [MinHeight, MaxHeight].</summary>
        public float CurrentHeight => _height;

        /// <summary>Current yaw in degrees around the world Y axis.</summary>
        public float YawDegrees => _yaw;

        public float MinHeight => _minHeight;

        public float MaxHeight => _maxHeight;

        public float MapWidth
        {
            get => _mapWidth;
            set { _mapWidth = Mathf.Max(1f, value); ClampFocus(); ApplyTransform(); }
        }

        public float MapHeight
        {
            get => _mapHeight;
            set { _mapHeight = Mathf.Max(1f, value); ClampFocus(); ApplyTransform(); }
        }

        public bool EdgePanEnabled
        {
            get => _edgePanEnabled;
            set => _edgePanEnabled = value;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _focus = new Vector3(_startFocusX, 0f, _startFocusZ);
            _yaw = _startYawDegrees;
            _height = Mathf.Clamp(_startHeight, _minHeight, _maxHeight);
            ClampFocus();
            ApplyTransform();
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateZoom();
            UpdateRotation(dt);
            UpdatePan(dt);
            ClampFocus();
            ApplyTransform();
        }

        /// <summary>
        /// Snaps the rig onto a ground position (y is ignored), clamped to the map bounds.
        /// </summary>
        public void FocusOn(Vector3 worldPosition)
        {
            _focus = new Vector3(worldPosition.x, 0f, worldPosition.z);
            ClampFocus();
            ApplyTransform();
        }

        /// <summary>
        /// Projects a screen point onto the y=0 ground plane analytically (no Physics.Raycast —
        /// the graybox scene has no colliders). Returns false when the ray never crosses the plane.
        /// </summary>
        public bool TryScreenPointToGround(Vector2 screenPoint, out Vector3 world)
        {
            world = Vector3.zero;

            Camera cam = _camera != null ? _camera : GetComponent<Camera>();
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
            if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;

            float distance = -ray.origin.y / ray.direction.y;
            if (distance < 0f) return false;

            world = ray.origin + ray.direction * distance;
            world.y = 0f;
            return true;
        }

        private void UpdateZoom()
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            // Focus and pitch stay fixed, so changing the height translates the camera
            // strictly along its own view axis.
            _height = Mathf.Clamp(_height - scroll * _zoomSpeed, _minHeight, _maxHeight);
        }

        private void UpdateRotation(float dt)
        {
            float direction = 0f;
            if (Input.GetKey(KeyCode.Z)) direction -= 1f;
            if (Input.GetKey(KeyCode.X)) direction += 1f;
            if (direction == 0f) return;

            _yaw = Mathf.Repeat(_yaw + direction * _rotationSpeed * dt, 360f);
        }

        private void UpdatePan(float dt)
        {
            Vector2 axis = ReadPanAxis();
            if (axis.sqrMagnitude <= 0f) return;
            if (axis.sqrMagnitude > 1f) axis.Normalize();

            float t = Mathf.InverseLerp(_minHeight, _maxHeight, _height);
            float speed = _panSpeed * Mathf.Lerp(1f, Mathf.Max(1f, _panSpeedHeightScale), t);

            Vector3 move = Quaternion.Euler(0f, _yaw, 0f) * new Vector3(axis.x, 0f, axis.y);
            _focus += move * (speed * dt);
        }

        private Vector2 ReadPanAxis()
        {
            // Arrow keys only — W/A/S/D are reserved for the command layer, see the class remarks.
            Vector2 axis = Vector2.zero;
            if (Input.GetKey(KeyCode.UpArrow)) axis.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) axis.y -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) axis.x += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) axis.x -= 1f;

            if (!_edgePanEnabled) return axis;

            // Ignore the cursor while it sits outside the game view (common in the editor).
            Vector3 mouse = Input.mousePosition;
            if (mouse.x < 0f || mouse.y < 0f || mouse.x > Screen.width || mouse.y > Screen.height) return axis;

            if (mouse.x <= _edgePanMargin) axis.x -= 1f;
            else if (mouse.x >= Screen.width - _edgePanMargin) axis.x += 1f;

            if (mouse.y <= _edgePanMargin) axis.y -= 1f;
            else if (mouse.y >= Screen.height - _edgePanMargin) axis.y += 1f;

            return axis;
        }

        private void ClampFocus()
        {
            _focus.x = Mathf.Clamp(_focus.x, 0f, Mathf.Max(1f, _mapWidth));
            _focus.y = 0f;
            _focus.z = Mathf.Clamp(_focus.z, 0f, Mathf.Max(1f, _mapHeight));
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitchDegrees, _yaw, 0f);
            float sinPitch = Mathf.Max(Mathf.Sin(_pitchDegrees * Mathf.Deg2Rad), 0.05f);
            float distance = _height / sinPitch;
            transform.SetPositionAndRotation(_focus - (rotation * Vector3.forward) * distance, rotation);
        }

        private void OnValidate()
        {
            _mapWidth = Mathf.Max(1f, _mapWidth);
            _mapHeight = Mathf.Max(1f, _mapHeight);
            _minHeight = Mathf.Max(1f, _minHeight);
            _maxHeight = Mathf.Max(_minHeight + 1f, _maxHeight);
            _pitchDegrees = Mathf.Clamp(_pitchDegrees, 15f, 89f);
            _edgePanMargin = Mathf.Max(0f, _edgePanMargin);
            _panSpeedHeightScale = Mathf.Max(1f, _panSpeedHeightScale);
        }
    }
}
