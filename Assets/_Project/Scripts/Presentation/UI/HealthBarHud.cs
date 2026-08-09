// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using System.Collections.Generic;
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using Nova.Simulation.Vision;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Health bars over entities (sprint 09 §5): a thin IMGUI bar above every
    /// VISIBLE entity that is damaged or currently selected — never a
    /// permanent bar over everything, or the screen drowns in chrome. With
    /// auto-acquire (D-087) combat happens without clicks, and these bars are
    /// what make a firefight readable at a glance.
    /// <para>
    /// STRICTLY READ-ONLY: the entity set comes from the committed team view
    /// (<see cref="FogOfWarSystem.GetVisibleEntities"/>, the same feed
    /// <see cref="UnitViewManager"/> renders), health from the entity store,
    /// selection from <see cref="RtsDeviceInput"/>. Nothing here touches
    /// simulation state; SimFixed-to-float conversion happens at this
    /// boundary only.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBarHud : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [Tooltip("Whole HUD is scaled by this factor, matching the other cockpit panels.")]
        [SerializeField] private float _uiScale = 1.5f;
        [Tooltip("Bar width in scaled GUI pixels over a unit (buildings get 1.5x).")]
        [SerializeField] private float _barWidth = 26f;
        [SerializeField] private float _barHeight = 4f;
        [Tooltip("World-space height of the bar above the entity's ground position.")]
        [SerializeField] private float _unitHeightOffset = 1.6f;
        [SerializeField] private float _buildingHeightOffset = 3.4f;

        private readonly List<EntityId> _visibleScratch = new List<EntityId>(256);
        private Texture2D _pixel;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void OnGUI()
        {
            if (_runner == null || _input == null || !_runner.IsRunning) return;
            EntityManager entities = _runner.Entities;
            FogOfWarSystem fog = _runner.FogOfWar;
            if (entities == null || fog == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            byte viewerTeam = (byte)(_runner.Session != null ? _runner.Session.LocalSlot : 0);
            _visibleScratch.Clear();
            if (FogRevealDebug.RevealAll)
            {
                // Lab reveal: bars over the revealed enemy too — a retreat is
                // only readable next to the health that triggered it.
                FogRevealDebug.CollectAllActive(entities, _visibleScratch);
            }
            else
            {
                fog.GetVisibleEntities(viewerTeam, _visibleScratch);
            }
            if (_visibleScratch.Count == 0) return;

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            for (int i = 0; i < _visibleScratch.Count; i++)
            {
                EntityId id = _visibleScratch[i];
                if (!entities.TryGetUnit(id, out UnitState unit)) continue;

                bool selected = Contains(_input.Selection.SelectedEntities, id);
                bool damaged = unit.MaxHealth > 0 && unit.CurrentHealth < unit.MaxHealth;
                if (!selected && !damaged) continue;

                bool isBuilding = SimDefinitions.IsBuildingRole(unit.Role);
                var world = new Vector3(
                    unit.Transform.PositionX.ToFloat(),
                    isBuilding ? _buildingHeightOffset : _unitHeightOffset,
                    unit.Transform.PositionY.ToFloat());
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue; // behind the camera

                // WorldToScreenPoint is bottom-left origin, unscaled; IMGUI is
                // top-left origin and currently scale-transformed.
                float width = isBuilding ? _barWidth * 1.5f : _barWidth;
                float x = screen.x / scale - width * 0.5f;
                float y = (Screen.height - screen.y) / scale;

                float fraction = unit.MaxHealth > 0
                    ? Mathf.Clamp01((float)unit.CurrentHealth / unit.MaxHealth)
                    : 0f;

                // Selected-and-undamaged entities get a full green bar (the
                // marker confirms the selection), damaged ones a green-to-red
                // ramp. Same ramp the health tint on the bodies uses.
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(x, y, width, _barHeight), Pixel);
                GUI.color = fraction > 0.5f
                    ? new Color(0.25f, 0.85f, 0.3f, 0.95f)
                    : fraction > 0.25f
                        ? new Color(0.9f, 0.75f, 0.2f, 0.95f)
                        : new Color(0.85f, 0.25f, 0.2f, 0.95f);
                GUI.DrawTexture(new Rect(x, y, width * fraction, _barHeight), Pixel);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private static bool Contains(System.ReadOnlySpan<EntityId> haystack, EntityId needle)
        {
            for (int i = 0; i < haystack.Length; i++)
            {
                if (haystack[i] == needle) return true;
            }
            return false;
        }

        private Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                {
                    _pixel = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                    _pixel.SetPixel(0, 0, Color.white);
                    _pixel.Apply();
                }
                return _pixel;
            }
        }

        private void OnDestroy()
        {
            if (_pixel != null) Destroy(_pixel);
        }
    }
}
