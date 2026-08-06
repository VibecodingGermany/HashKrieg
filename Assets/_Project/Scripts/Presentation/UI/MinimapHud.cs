// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using System.Collections.Generic;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using Nova.Simulation.Vision;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The minimap: an IMGUI panel docked to the bottom-left corner, directly
    /// above the build bar (mirroring the command card on the right). Shows
    /// the Glutrinne desert ground, the viewer team's fog state as the same
    /// terrain silhouette in three brightness bands (visible full, explored
    /// dimmed, unexplored near-dark but readable — never black), every entity
    /// the team may legally see as a faction-coloured dot, and the camera's
    /// ground footprint as a white rectangle. A left click inside the map
    /// jumps the camera there.
    /// <para>
    /// NO LEAKS BY CONSTRUCTION: the background is the SAME committed team
    /// view the <see cref="FogOfWarOverlayView"/> renders and the dots come
    /// from <see cref="FogOfWarSystem.GetVisibleEntities"/> — the exact feed
    /// of <see cref="UnitViewManager"/> — so the minimap cannot show an
    /// entity the world view hides, and both read
    /// <c>MatchRunner.Session.LocalSlot</c>, so they cannot disagree.
    /// Strictly read-only: nothing here mutates simulation state or submits
    /// an intent.
    /// </para>
    /// <para>
    /// CAMERA CHANNEL (assembly rule): Nova.Presentation.UI may not reference
    /// Nova.Presentation, so the rig's pose and the click-to-jump request
    /// pass through <see cref="MinimapCameraLink"/> (Nova.Gameplay, visible
    /// to both). The rig publishes its pose every LateUpdate; this component
    /// reads it. Click detection lives HERE (this component owns the rect
    /// math — <see cref="IsPointerOverMinimap"/> and the draw share it, like
    /// the build bar and command card); the jump itself lives in
    /// RtsCameraController, which consumes the request and calls its own
    /// FocusOn.
    /// </para>
    /// <para>
    /// ORIENTATION: world z = 0 (the local player's corner) sits at the
    /// BOTTOM edge — <see cref="MinimapRenderer.WorldToMinimapGuiCoordinates"/>
    /// owns the flip, and the background texture is written to match (texel
    /// row 0 = world z 0 renders at the rect's bottom). All math lives in
    /// <see cref="MinimapRenderer"/> (Unity-free, EditMode-tested); this
    /// component only draws.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapHud : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [Tooltip("Build bar; the minimap docks directly above its bottom reserve, mirroring the command card.")]
        [SerializeField] private BuildMenuHud _buildMenu;

        /// <summary>Serialized defaults as constants — HudLayout's CanonicalMinimapZone reads these for panels that must clear the minimap without holding a reference to it.</summary>
        public const float DefaultMapSize = 168f;
        public const float DefaultMargin = 8f;

        [Header("Presentation")]
        [Tooltip("Whole map is scaled by this factor, matching the BuildMenuHud/DebugHud convention for Retina displays.")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private float _mapSize = DefaultMapSize;
        [SerializeField] private float _margin = DefaultMargin;

        [Header("Terrain (Glutrinne desert — matches GlutrinneBlockoutView._sandColor)")]
        [SerializeField] private Color _terrainColor = new Color(0.72f, 0.60f, 0.42f, 1f);
        [Tooltip("Brightness multiplier of the terrain over Explored (seen before, not currently visible) cells.")]
        [SerializeField, Range(0f, 1f)] private float _exploredBrightness = 0.45f;
        [Tooltip("Brightness multiplier of the terrain over UNEXPLORED cells — the silhouette stays readable instead of reading as a black hole (classic RTS convention).")]
        [SerializeField, Range(0f, 1f)] private float _unexploredBrightness = 0.15f;

        [Header("Dots")]
        [SerializeField] private float _unitDotSize = 3f;
        [SerializeField] private float _buildingDotSize = 4.5f;

        private readonly List<EntityId> _visibleScratch = new List<EntityId>(256);
        private Texture2D _background;
        private Color32[] _pixels;
        private uint _renderedTick;
        private bool _hasRendered;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_buildMenu == null) _buildMenu = FindAnyObjectByType<BuildMenuHud>();
        }

        /// <summary>
        /// Screen-space hit test used by RtsDeviceInput: a pointer over the
        /// map belongs to the HUD (camera jumps), so world selection drags,
        /// placement clicks and order picks are suppressed there. Same
        /// raw-mouse-to-scaled-GUI conversion as
        /// <see cref="BuildMenuHud.IsPointerOverBar"/>, and the layout math is
        /// the same <see cref="ComputeMapRect"/> OnGUI draws with — hit test
        /// and drawing cannot drift apart. Slightly inflated so a click on
        /// the chrome frame counts as HUD too.
        /// </summary>
        public bool IsPointerOverMinimap(Vector2 mousePosition)
        {
            float scale = Mathf.Max(1f, _uiScale);
            Vector2 gui = HudLayout.RawMouseToGui(mousePosition, scale);
            Rect map = ComputeMapRect();
            map.xMin -= 4f;
            map.yMin -= 4f;
            map.xMax += 4f;
            map.yMax += 4f;
            return map.Contains(gui);
        }

        /// <summary>Bottom-left docking: the minimap's zone sits directly above the build bar's reserve, sharing the left margin (HudLayout owns the screen read).</summary>
        private Rect ComputeMapRect()
        {
            float scale = Mathf.Max(1f, _uiScale);
            float barReserve = _buildMenu != null ? _buildMenu.OccupiedHeight : 0f;
            return HudLayout.MinimapZone(scale, _mapSize, _margin, barReserve);
        }

        private void OnGUI()
        {
            FogOfWarSystem fog = _runner != null ? _runner.FogOfWar : null;
            if (fog == null) return; // no committed sight, no minimap (the world view renders nothing either)

            byte team = ResolveViewerTeam(fog);
            RefreshBackground(fog, team);

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Rect map = ComputeMapRect();

            // Chrome frame behind the map (2px out, so the border ring reads
            // as the panel's edge, not as an overlay on the terrain).
            var frame = new Rect(map.x - 3f, map.y - 3f, map.width + 6f, map.height + 6f);
            GUI.Box(frame, GUIContent.none, HudChrome.PanelStyle);

            GUI.DrawTexture(map, _background);
            DrawEntityDots(map, fog, team);
            DrawCameraViewport(map, fog);
            HandleClick(map, fog);

            GUI.matrix = previousMatrix;
        }

        /// <summary>The local viewer team — the same convention as FogOfWarOverlayView/UnitViewManager.</summary>
        private byte ResolveViewerTeam(FogOfWarSystem fog)
        {
            MatchSession session = _runner.Session;
            int team = session != null ? session.LocalSlot : 0;
            if (team >= fog.TeamCount) team = fog.TeamCount - 1;
            if (team < 0) team = 0;
            return (byte)team;
        }

        /// <summary>
        /// The terrain background, shaded by the committed fog and repainted
        /// only when the view advances (5 Hz, <see cref="FogOfWarSystem.LastRecomputeTick"/>)
        /// — matching the data's cadence instead of repainting per OnGUI pass.
        /// Texel (x, y) is mask cell (x, y): row 0 = world z 0 = the rect's
        /// bottom edge, the same orientation the dots use.
        /// </summary>
        private void RefreshBackground(FogOfWarSystem fog, byte team)
        {
            if (_background == null || _background.width != fog.Width || _background.height != fog.Height)
            {
                if (_background != null) Destroy(_background);
                _background = new Texture2D(fog.Width, fog.Height, TextureFormat.RGBA32, mipChain: false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _pixels = new Color32[fog.Width * fog.Height];
                _hasRendered = false;
            }

            if (_hasRendered && (!fog.HasCommittedView || fog.LastRecomputeTick == _renderedTick)) return;

            TeamView view = fog.GetTeamView(team);
            Color32 visible = _terrainColor;
            var explored = new Color32(
                (byte)(_terrainColor.r * _exploredBrightness * 255f),
                (byte)(_terrainColor.g * _exploredBrightness * 255f),
                (byte)(_terrainColor.b * _exploredBrightness * 255f),
                255);
            // Unexplored is the same silhouette, much darker — not black. A
            // black hole reads as "broken"; a dimmed desert reads as
            // "unknown" (D-085, classic RTS minimap convention).
            var unexplored = new Color32(
                (byte)(_terrainColor.r * _unexploredBrightness * 255f),
                (byte)(_terrainColor.g * _unexploredBrightness * 255f),
                (byte)(_terrainColor.b * _unexploredBrightness * 255f),
                255);

            for (int y = 0; y < view.Height; y++)
            {
                int row = y * view.Width;
                for (int x = 0; x < view.Width; x++)
                {
                    switch (view.GetCellState(x, y))
                    {
                        case VisionState.Visible: _pixels[row + x] = visible; break;
                        case VisionState.Explored: _pixels[row + x] = explored; break;
                        default: _pixels[row + x] = unexplored; break;
                    }
                }
            }

            _background.SetPixels32(_pixels);
            _background.Apply();
            _renderedTick = fog.LastRecomputeTick;
            _hasRendered = true;
        }

        /// <summary>
        /// One dot per entity the committed view reports — own entities
        /// always, foreign ones only in Visible cells, so a hidden enemy
        /// cannot leak onto the map. Colour is the owning slot's FACTION
        /// (FactionTint, D-072), the same channel the world views use;
        /// buildings get the larger dot so a base reads differently from an
        /// army at a glance.
        /// </summary>
        private void DrawEntityDots(Rect map, FogOfWarSystem fog, byte team)
        {
            EntityManager entities = _runner.Entities;
            if (entities == null) return;

            _visibleScratch.Clear();
            fog.GetVisibleEntities(team, _visibleScratch);

            Color previousColor = GUI.color;
            for (int i = 0; i < _visibleScratch.Count; i++)
            {
                if (!entities.TryGetUnit(_visibleScratch[i], out UnitState unit)) continue;

                // Presentation boundary: SimFixed -> float happens here.
                (float uiX, float uiY) = MinimapRenderer.WorldToMinimapGuiCoordinates(
                    unit.Transform.PositionX.ToFloat(), unit.Transform.PositionY.ToFloat(),
                    fog.Width, fog.Height, map.width, map.height);

                float size = SimDefinitions.IsBuildingRole(unit.Role) ? _buildingDotSize : _unitDotSize;
                GUI.color = ColorForOwner(unit.PlayerId);
                GUI.DrawTexture(
                    new Rect(map.x + uiX - size * 0.5f, map.y + uiY - size * 0.5f, size, size),
                    HudChrome.Pixel);
            }
            GUI.color = previousColor;
        }

        /// <summary>Owner faction colour via the economy state — the same lookup the world views use; grey fallback, never a throw.</summary>
        private Color ColorForOwner(byte playerId)
        {
            if (_runner.Economy != null && playerId < Nova.Simulation.Economy.EconomySystem.MaxPlayers)
            {
                return FactionTint.BaseColor(_runner.Economy.GetSlotFaction(playerId));
            }
            return Color.gray;
        }

        /// <summary>
        /// The camera's ground footprint as a white outline: frustum corners
        /// from the rig's published pose (<see cref="MinimapCameraLink"/>),
        /// computed by <see cref="MinimapRenderer.TryComputeGroundViewCorners"/>
        /// and clipped to the map so the outline stays inside the panel.
        /// Skipped silently while no pose exists or the frustum does not hit
        /// the ground (the math reports it).
        /// </summary>
        private void DrawCameraViewport(Rect map, FogOfWarSystem fog)
        {
            if (!MinimapCameraLink.HasPose) return;
            if (!MinimapRenderer.TryComputeGroundViewCorners(
                    MinimapCameraLink.FocusX, MinimapCameraLink.FocusZ,
                    MinimapCameraLink.Height, MinimapCameraLink.PitchDegrees, MinimapCameraLink.YawDegrees,
                    MinimapCameraLink.VerticalFovDegrees, MinimapCameraLink.Aspect,
                    out (float x, float z) bl, out (float x, float z) br,
                    out (float x, float z) tr, out (float x, float z) tl))
            {
                return;
            }

            Vector2 p0 = ViewportPoint(map, fog, bl);
            Vector2 p1 = ViewportPoint(map, fog, br);
            Vector2 p2 = ViewportPoint(map, fog, tr);
            Vector2 p3 = ViewportPoint(map, fog, tl);

            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            DrawViewportLine(p0, p1);
            DrawViewportLine(p1, p2);
            DrawViewportLine(p2, p3);
            DrawViewportLine(p3, p0);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        /// <summary>World corner -> GUI point inside the map rect, clamped to the map edge (the footprint may overhang the map).</summary>
        private static Vector2 ViewportPoint(Rect map, FogOfWarSystem fog, (float x, float z) corner)
        {
            (float uiX, float uiY) = MinimapRenderer.WorldToMinimapGuiCoordinates(
                corner.x, corner.z, fog.Width, fog.Height, map.width, map.height);
            return new Vector2(map.x + uiX, map.y + uiY);
        }

        /// <summary>
        /// IMGUI line: a 1.5px strip of the shared white pixel, stretched
        /// between the points and rotated into place. RotateAroundPivot
        /// composes onto the current (scaled) GUI matrix, so the caller
        /// restores the matrix after the four edges.
        /// </summary>
        private static void DrawViewportLine(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.5f) return;

            Vector2 midpoint = (from + to) * 0.5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, midpoint);
            GUI.DrawTexture(new Rect(midpoint.x - length * 0.5f, midpoint.y - 0.75f, length, 1.5f), HudChrome.Pixel);
            GUIUtility.RotateAroundPivot(-angle, midpoint);
        }

        /// <summary>
        /// LMB inside the map: convert the click to a world ground position
        /// (the exact inverse of the dot mapping) and hand it to the camera
        /// rig through <see cref="MinimapCameraLink"/> — the jump itself is
        /// the rig's own FocusOn. The click is consumed AND the rect is wired
        /// into RtsDeviceInput's HUD suppression, so the same click cannot
        /// also start a world selection drag behind the map.
        /// </summary>
        private void HandleClick(Rect map, FogOfWarSystem fog)
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.MouseDown || current.button != 0) return;
            if (!map.Contains(current.mousePosition)) return;

            (float worldX, float worldZ) = MinimapRenderer.MinimapGuiToWorldCoordinates(
                current.mousePosition.x - map.x, current.mousePosition.y - map.y,
                fog.Width, fog.Height, map.width, map.height);
            MinimapCameraLink.RequestFocus(worldX, worldZ);
            current.Use();
        }

        private void OnDestroy()
        {
            if (_background != null) Destroy(_background);
            HudChrome.DestroyShared();
        }
    }
}
