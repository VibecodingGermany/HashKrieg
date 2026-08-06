using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The placement ghost: one flat 3x3 footprint quad that follows the
    /// cursor while <see cref="RtsDeviceInput"/>'s placement mode is armed.
    /// GREEN when placing at the hovered cell would be accepted — the verdict
    /// comes from RtsDeviceInput, which evaluates the executor's own
    /// <c>ConstructionSystem.ValidatePlacement</c> plus affordability — RED
    /// otherwise. This is a pure view: the cell and the verdict are computed
    /// by the input component (the same values the LMB click will dispatch),
    /// this component only draws them, so ghost and outcome cannot disagree.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlacementGhostView : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private RtsDeviceInput _input;

        [Header("Ghost look")]
        [SerializeField] private float _ghostHeight = 0.09f;
        [SerializeField] private Color _validColor = new Color(0.3f, 1f, 0.4f, 1f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.25f, 1f);

        private GameObject _ghost;
        private Renderer _renderer;
        private MaterialPropertyBlock _tintBlock;
        private bool _tintedValid;
        private bool _hasTint;

        private void Awake()
        {
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void LateUpdate()
        {
            int originX = 0;
            int originY = 0;
            bool show = _input != null && _input.PlacementModeActive
                && _input.TryGetPlacementCell(out originX, out originY);
            if (!show)
            {
                if (_ghost != null && _ghost.activeSelf) _ghost.SetActive(false);
                return;
            }

            EnsureGhost();
            if (!_ghost.activeSelf) _ghost.SetActive(true);

            // The footprint spans exactly its 3x3 cells; the quad centres on it.
            const float half = SimDefinitions.BuildingFootprintCells * 0.5f;
            _ghost.transform.position = new Vector3(originX + half, _ghostHeight, originY + half);

            bool valid = _input.PlacementValid;
            if (!_hasTint || valid != _tintedValid)
            {
                // The tint changes only when the verdict flips; steady-state
                // hovering costs no SetPropertyBlock at all.
                _hasTint = true;
                _tintedValid = valid;
                _tintBlock.Clear();
                FactionTint.ApplyToPropertyBlock(_tintBlock, valid ? _validColor : _invalidColor);
                _renderer.SetPropertyBlock(_tintBlock);
            }
        }

        private void EnsureGhost()
        {
            if (_ghost != null) return;
            _ghost = GroundMarkerVisuals.CreateFlatQuad("PlacementGhost", transform);
            _renderer = _ghost.GetComponent<MeshRenderer>();
            const int cells = SimDefinitions.BuildingFootprintCells;
            _ghost.transform.localScale = new Vector3(cells, cells, 1f);
            _tintBlock = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (_ghost != null) Destroy(_ghost);
            GroundMarkerVisuals.DestroyShared();
        }
    }
}
