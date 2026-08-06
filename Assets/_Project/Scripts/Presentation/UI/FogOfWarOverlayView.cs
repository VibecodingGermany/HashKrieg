// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Vision;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The Fog-of-War world overlay: one flat quad spanning the whole map,
    /// textured from the viewer team's COMMITTED team view so the three
    /// vision states read over the terrain — Unexplored near-opaque black,
    /// Explored darkened, Visible clear. Without this the fog existed only
    /// implicitly (entities popping in and out); the terrain itself now
    /// carries the exploration state, which is what makes "where have I
    /// been?" answerable at a glance.
    /// <para>
    /// DATA SOURCE: <see cref="FogOfWarSystem.GetTeamView"/> of
    /// <c>MatchRunner.Session.LocalSlot</c> — the SAME team the
    /// <see cref="UnitViewManager"/> renders, so the darkened ground and the
    /// hidden entities can never disagree. The mask is read, pixels are
    /// written, <c>Apply()</c> — nothing here mutates simulation state, and
    /// the overlay carries no collider, so world picking (which projects onto
    /// the mathematical ground plane) is unaffected.
    /// </para>
    /// <para>
    /// CADENCE: the committed view recomputes at 5 Hz
    /// (<see cref="FogOfWarSystem.RecomputeIntervalTicks"/> on the 10 Hz
    /// clock), so the texture refreshes only when
    /// <see cref="FogOfWarSystem.LastRecomputeTick"/> advances — matching the
    /// data's cadence instead of repainting 16 KiB of pixels per frame.
    /// </para>
    /// <para>
    /// TEXTURE MAPPING: the quad comes from
    /// <see cref="GroundMarkerVisuals.CreateFlatQuad"/> (Unity Quad pitched
    /// 90°: u -> world x, v -> world z), centred on the map and scaled to the
    /// fog grid, so texel (x, y) covers cell (x, y) of the mask one-to-one
    /// with no flip. The shader is Sprites/Default — the one built-in shader
    /// that renders transparent and unlit in every pipeline (same finding as
    /// GroundMarkerVisuals). The quad sits just above the terrain and below
    /// every HUD marker (selection 0.06, rally 0.07, ghost 0.09), so markers
    /// keep rendering on top of the fog.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FogOfWarOverlayView : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;

        [Header("Presentation")]
        [Tooltip("Opacity of the black over Unexplored cells.")]
        [SerializeField, Range(0f, 1f)] private float _unexploredAlpha = 0.85f;
        [Tooltip("Opacity of the black over Explored (seen before, not currently visible) cells.")]
        [SerializeField, Range(0f, 1f)] private float _exploredAlpha = 0.45f;
        [Tooltip("World height of the overlay quad: above the terrain and below the lowest HUD marker (0.06).")]
        [SerializeField] private float _overlayHeight = 0.04f;

        private Texture2D _texture;
        private Color32[] _pixels;
        private Material _material;
        private GameObject _quad;
        private uint _renderedTick;
        private bool _hasRendered;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
        }

        private void LateUpdate()
        {
            FogOfWarSystem fog = _runner != null ? _runner.FogOfWar : null;
            if (fog == null)
            {
                // No fog system, no overlay: never render an all-clear sheet
                // that would LIE about sight (UnitViewManager renders nothing
                // in the same situation).
                SetQuadActive(false);
                return;
            }

            SetQuadActive(true);
            EnsureResources(fog);

            // Repaint only when the committed view actually advanced (5 Hz)
            // — or on the very first frame, before any commit, when the mask
            // is all-Unexplored by construction.
            if (_hasRendered && (!fog.HasCommittedView || fog.LastRecomputeTick == _renderedTick)) return;

            TeamView view = fog.GetTeamView(ResolveViewerTeam(fog));
            PaintTexture(in view);
            _renderedTick = fog.LastRecomputeTick;
            _hasRendered = true;
        }

        /// <summary>
        /// The local viewer team, clamped into the fog system's team range —
        /// the same convention <see cref="UnitViewManager.ResolveViewerTeam"/>
        /// uses (without its debug override, which this readout does not need).
        /// </summary>
        private byte ResolveViewerTeam(FogOfWarSystem fog)
        {
            MatchSession session = _runner.Session;
            int team = session != null ? session.LocalSlot : 0;
            if (team >= fog.TeamCount) team = fog.TeamCount - 1;
            if (team < 0) team = 0;
            return (byte)team;
        }

        /// <summary>
        /// One byte per mask cell becomes one black texel whose alpha is the
        /// vision state: Unexplored / Explored / Visible = dark / shaded /
        /// clear. Texel (x, y) maps to mask cell (x, y) directly (row-major
        /// y * Width + x), see the class remarks on the quad's UV layout.
        /// </summary>
        private void PaintTexture(in TeamView view)
        {
            int width = view.Width;
            int height = view.Height;
            byte unexplored = (byte)Mathf.RoundToInt(_unexploredAlpha * 255f);
            byte explored = (byte)Mathf.RoundToInt(_exploredAlpha * 255f);

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    byte alpha;
                    switch (view.GetCellState(x, y))
                    {
                        case VisionState.Visible: alpha = 0; break;
                        case VisionState.Explored: alpha = explored; break;
                        default: alpha = unexplored; break;
                    }
                    _pixels[row + x] = new Color32(0, 0, 0, alpha);
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply();
        }

        /// <summary>
        /// Builds texture, material and quad lazily once the fog grid's
        /// dimensions are known. Everything is runtime-generated and
        /// HideAndDontSave — the slice forbids hand-made material assets
        /// (GroundMarkerVisuals convention).
        /// </summary>
        private void EnsureResources(FogOfWarSystem fog)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(fog.Width, fog.Height, TextureFormat.RGBA32, mipChain: false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _pixels = new Color32[fog.Width * fog.Height];
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
                _quad = GroundMarkerVisuals.CreateFlatQuad("FogOfWarOverlay", transform);
                _quad.GetComponent<MeshRenderer>().sharedMaterial = _material;
                _quad.transform.position = new Vector3(fog.Width * 0.5f, _overlayHeight, fog.Height * 0.5f);
                _quad.transform.localScale = new Vector3(fog.Width, fog.Height, 1f);
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
