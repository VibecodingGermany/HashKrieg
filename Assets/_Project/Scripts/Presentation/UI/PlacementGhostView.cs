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
        private Renderer _groundRenderer;
        private Renderer[] _hologramRenderers;
        private Renderer _coreRenderer;
        private Material _hologramMaterial;
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

            // The footprint spans exactly its 3x3 cells; the ghost centres on it.
            const float half = SimDefinitions.BuildingFootprintCells * 0.5f;
            _ghost.transform.position = new Vector3(originX + half, _ghostHeight, originY + half);

            bool valid = _input.PlacementValid;
            if (!_hasTint || valid != _tintedValid)
            {
                // The tint changes only when the verdict flips; steady-state
                // hovering costs no SetPropertyBlock at all.
                _hasTint = true;
                _tintedValid = valid;
                Color baseTint = valid ? _validColor : _invalidColor;

                _tintBlock.Clear();
                FactionTint.ApplyToPropertyBlock(_tintBlock, baseTint);
                _groundRenderer.SetPropertyBlock(_tintBlock);

                Color beamColor = new Color(baseTint.r, baseTint.g, baseTint.b, 0.78f);
                _tintBlock.Clear();
                FactionTint.ApplyToPropertyBlock(_tintBlock, beamColor);
                for (int i = 0; i < _hologramRenderers.Length; i++)
                {
                    _hologramRenderers[i].SetPropertyBlock(_tintBlock);
                }

                Color coreColor = new Color(baseTint.r, baseTint.g, baseTint.b, 0.24f);
                _tintBlock.Clear();
                FactionTint.ApplyToPropertyBlock(_tintBlock, coreColor);
                _coreRenderer.SetPropertyBlock(_tintBlock);
            }
        }

        private void EnsureGhost()
        {
            if (_ghost != null) return;
            _ghost = new GameObject("PlacementGhost");
            _ghost.transform.SetParent(transform, false);

            GameObject groundQuad = GroundMarkerVisuals.CreateFlatQuad("GhostFootprint", _ghost.transform);
            groundQuad.transform.localPosition = Vector3.zero;
            const int cells = SimDefinitions.BuildingFootprintCells;
            groundQuad.transform.localScale = new Vector3(cells, cells, 1f);
            _groundRenderer = groundQuad.GetComponent<MeshRenderer>();

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            _hologramMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            const float e = 1.38f;
            const float h = 1.55f;
            _hologramRenderers = new Renderer[8];
            _hologramRenderers[0] = CreateHoloStrut(_ghost.transform, "PostSW", new Vector3(-e, h * 0.5f, -e), new Vector3(0.09f, h, 0.09f), _hologramMaterial);
            _hologramRenderers[1] = CreateHoloStrut(_ghost.transform, "PostSE", new Vector3( e, h * 0.5f, -e), new Vector3(0.09f, h, 0.09f), _hologramMaterial);
            _hologramRenderers[2] = CreateHoloStrut(_ghost.transform, "PostNE", new Vector3( e, h * 0.5f,  e), new Vector3(0.09f, h, 0.09f), _hologramMaterial);
            _hologramRenderers[3] = CreateHoloStrut(_ghost.transform, "PostNW", new Vector3(-e, h * 0.5f,  e), new Vector3(0.09f, h, 0.09f), _hologramMaterial);
            _hologramRenderers[4] = CreateHoloStrut(_ghost.transform, "TopN",   new Vector3(0f, h,  e), new Vector3(e * 2f + 0.09f, 0.07f, 0.07f), _hologramMaterial);
            _hologramRenderers[5] = CreateHoloStrut(_ghost.transform, "TopS",   new Vector3(0f, h, -e), new Vector3(e * 2f + 0.09f, 0.07f, 0.07f), _hologramMaterial);
            _hologramRenderers[6] = CreateHoloStrut(_ghost.transform, "TopE",   new Vector3( e, h, 0f), new Vector3(0.07f, 0.07f, e * 2f + 0.09f), _hologramMaterial);
            _hologramRenderers[7] = CreateHoloStrut(_ghost.transform, "TopW",   new Vector3(-e, h, 0f), new Vector3(0.07f, 0.07f, e * 2f + 0.09f), _hologramMaterial);

            _coreRenderer = CreateHoloStrut(_ghost.transform, "HoloVolume", new Vector3(0f, 0.65f, 0f), new Vector3(2.25f, 1.20f, 2.25f), _hologramMaterial);
            _tintBlock = new MaterialPropertyBlock();
        }

        private static Renderer CreateHoloStrut(
            Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
        {
            GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strut.name = name;
            Destroy(strut.GetComponent<Collider>());
            strut.transform.SetParent(parent, false);
            strut.transform.localPosition = localPos;
            strut.transform.localScale = localScale;
            var renderer = strut.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private void OnDestroy()
        {
            if (_ghost != null) Destroy(_ghost);
            if (_hologramMaterial != null)
            {
                Destroy(_hologramMaterial);
                _hologramMaterial = null;
            }
            GroundMarkerVisuals.DestroyShared();
        }
    }
}
