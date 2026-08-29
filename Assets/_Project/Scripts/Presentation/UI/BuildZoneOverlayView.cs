// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The build-zone overlay (issue #91, test report T-01): one flat quad
    /// over the whole map, textured per cell from the construction system's
    /// OWN placement reads, so the player can finally SEE the rule that
    /// otherwise only speaks through rejections. Two states, painted
    /// differently on purpose: a footprint origin inside the build influence
    /// and clear of every other footprint reads as a green wash, an origin
    /// inside the influence but rejected by the minimum building distance
    /// reads orange — "in the zone but blocked" is a different statement
    /// than "outside the zone" (no tint), and confusing the two was exactly
    /// the test-report complaint about
    /// <see cref="ConstructionSystem.MinimumBuildingDistanceCells"/>.
    /// <para>
    /// THE RULE IS ASKED, NEVER REBUILT: each texel is the answer of
    /// <see cref="ConstructionSystem.IsInsideBuildInfluence"/> and
    /// <see cref="ConstructionSystem.HasMinimumBuildingSpacing"/> for a 3x3
    /// footprint ORIGIN at that cell — the same two reads
    /// <see cref="ConstructionSystem.ValidatePlacement"/> consumes, made
    /// public for exactly this caller. The overlay holds no radius constant
    /// and no anchor list of its own, so when D-108 re-opens the anchor
    /// list the picture follows the simulation on its own instead of
    /// silently going stale. Terrain walkability and field spacing are
    /// deliberately NOT painted: field spacing is role-dependent (the
    /// Refinery inverts it) and the placement ghost already gives the full
    /// per-position verdict at the cursor. Texel (x, y) answers for a
    /// footprint origin at cell (x, y); the ghost's origin sits one cell
    /// south-west of the cursor cell, which is exactly the value the LMB
    /// click validates.
    /// </para>
    /// <para>
    /// VISIBILITY: shown while <see cref="RtsDeviceInput.PlacementModeActive"/>
    /// (the ghost is armed and the zone is the missing context) or while
    /// pinned by the O toggle (<see cref="RtsDeviceInput.BuildZoneOverlayPinned"/>),
    /// so the base reach is readable without a build intent. Hidden when no
    /// match is live. Pure presentation in the sense of the
    /// FogOfWarOverlayView boundary: no collider, no input handling, no
    /// simulation write — the mask is read, pixels are written, Apply().
    /// </para>
    /// <para>
    /// CADENCE AND MAPPING: same one-quad-one-texture approach as the fog
    /// overlay (CreateFlatQuad, u -&gt; world x, v -&gt; world z, texel =
    /// cell one-to-one, no flip). A full 128x128 repaint runs both cell
    /// reads, each a scan over the placement tables — cheap but not free —
    /// so the texture refreshes at a fixed cadence while visible
    /// (construction state only changes on tick events) and immediately on
    /// show, instead of repainting 16 KiB of pixels per frame. The quad
    /// sits above the fog sheet (0.04) and below the lowest HUD marker
    /// (0.06). FilterMode.Point, not bilinear: the two states must stay
    /// crisp per cell — blending green into orange at the boundary would
    /// invent a third colour that means nothing.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildZoneOverlayView : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [Tooltip("Tint of a footprint origin inside the build influence and clear of the minimum building distance.")]
        [SerializeField] private Color32 _buildableColor = new Color32(70, 230, 90, 26);
        [Tooltip("Tint of a footprint origin inside the build influence but blocked by the minimum building distance.")]
        [SerializeField] private Color32 _spacingBlockedColor = new Color32(255, 150, 40, 44);
        [Tooltip("World height of the overlay quad: above the fog sheet (0.04) and below the lowest HUD marker (0.06).")]
        [SerializeField] private float _overlayHeight = 0.05f;
        [Tooltip("Seconds between full repaints while the overlay is visible.")]
        [SerializeField, Range(0.05f, 2f)] private float _repaintIntervalSeconds = 0.25f;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private Texture2D _texture;
        private Color32[] _pixels;
        private Material _material;
        private GameObject _quad;
        private float _nextRepaintTime;
        // Repaint scratch (21.4 follow-up): the two zone masks are filled by
        // the construction system in ONE register pass per repaint — the
        // per-texel query loop used to rescan that register per texel, which
        // hitched the main thread four times a second while placing.
        private bool[] _influenceMask;
        private bool[] _spacingBlockedMask;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void LateUpdate()
        {
            // The systems are re-read every frame: a new match replaces them,
            // and a stale reference would paint a dead match's zone.
            ConstructionSystem construction = _runner != null ? _runner.Construction : null;
            MatchSession session = _runner != null ? _runner.Session : null;
            bool wanted = construction != null && session != null && _input != null
                && (_input.PlacementModeActive || _input.BuildZoneOverlayPinned);
            if (!wanted)
            {
                SetQuadActive(false);
                // The next show repaints immediately, not one interval late.
                _nextRepaintTime = 0f;
                return;
            }

            EnsureResources();
            SetQuadActive(true);
            if (Time.time < _nextRepaintTime) return;
            _nextRepaintTime = Time.time + _repaintIntervalSeconds;
            Repaint(construction, session.LocalSlot);
        }

        /// <summary>
        /// One texel per cell: outside the build influence stays clear,
        /// inside + minimum distance kept gets the buildable tint, inside +
        /// too close to an existing footprint gets the blocked tint. Origins
        /// whose 3x3 footprint would leave the map stay clear as well —
        /// painting them buildable would promise a placement the validator
        /// rejects on bounds alone (map geometry, not the zone rule).
        /// The two masks come from the construction system pre-filled for the
        /// whole grid (<see cref="ConstructionSystem.FillBuildZoneMasks"/>) —
        /// texels only read them.
        /// </summary>
        private void Repaint(ConstructionSystem construction, byte viewerSlot)
        {
            int size = ConstructionSystem.GridSize;
            int lastOrigin = size - SimDefinitions.BuildingFootprintCells;

            EnsureMasks(size);
            construction.FillBuildZoneMasks(viewerSlot, _influenceMask, _spacingBlockedMask);

            for (int y = 0; y < size; y++)
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    Color32 color = Clear;
                    if (x <= lastOrigin && y <= lastOrigin && _influenceMask[row + x])
                    {
                        color = _spacingBlockedMask[row + x]
                            ? _spacingBlockedColor
                            : _buildableColor;
                    }
                    _pixels[row + x] = color;
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply();
        }

        private void EnsureMasks(int size)
        {
            if (_influenceMask == null || _influenceMask.Length != size * size)
            {
                _influenceMask = new bool[size * size];
                _spacingBlockedMask = new bool[size * size];
            }
        }

        /// <summary>
        /// Builds texture, material and quad lazily on the first show.
        /// Everything is runtime-generated and HideAndDontSave — the slice
        /// forbids hand-made material assets (GroundMarkerVisuals /
        /// FogOfWarOverlayView convention).
        /// </summary>
        private void EnsureResources()
        {
            if (_texture == null)
            {
                int size = ConstructionSystem.GridSize;
                _texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                _pixels = new Color32[size * size];
            }

            if (_material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                _material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    mainTexture = _texture
                };
            }

            if (_quad == null)
            {
                _quad = GroundMarkerVisuals.CreateFlatQuad("BuildZoneOverlay", transform);
                _quad.GetComponent<MeshRenderer>().sharedMaterial = _material;
                float size = ConstructionSystem.GridSize;
                _quad.transform.position = new Vector3(size * 0.5f, _overlayHeight, size * 0.5f);
                _quad.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        private void SetQuadActive(bool active)
        {
            if (_quad != null && _quad.activeSelf != active)
            {
                _quad.SetActive(active);
            }
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
            if (_material != null) Destroy(_material);
            // The quad is a child GameObject and dies with the UI object; the
            // shared GroundMarkerVisuals assets are untouched (the overlay
            // runs on its own material).
        }
    }
}
