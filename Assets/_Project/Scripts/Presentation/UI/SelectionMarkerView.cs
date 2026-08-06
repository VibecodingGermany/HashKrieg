using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// World-space selection feedback: every selected entity — units AND
    /// buildings — gets a flat marker quad on the ground under it, tinted the
    /// own-selection GREEN of the RTS norm. Green rather than the faction
    /// colour is deliberate: the selection can only ever contain own
    /// entities, so one colour answers "this is mine and selected" without
    /// spending the faction channel the entity bodies already carry
    /// (D-072 tints live on the bodies via UnitViewManager).
    /// <para>
    /// The marker set is re-derived from <see cref="RtsDeviceInput.Selection"/>
    /// every frame out of a small pool (no per-frame allocation), so a marker
    /// exists exactly while its entity is selected and alive: death,
    /// deselection and id recycling need no event handling.
    /// </para>
    /// <para>
    /// Markers anchor to the live VIEW (<see cref="UnitViewManager.TryGetView"/>)
    /// rather than the 10-Hz sim position, so they track the interpolated
    /// body the player actually sees instead of trailing it by a tick; the
    /// sim position is only the fallback for the first frames before a view
    /// exists. Building entities anchor at the lower-left corner of their
    /// footprint's centre cell (origin + 1), so the marker is shifted half a
    /// cell to frame the actual 3x3 footprint.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectionMarkerView : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;
        [SerializeField] private UnitViewManager _views;

        [Header("Marker look")]
        [Tooltip("Height above the ground plane; avoids z-fighting with the ground quad and the map blockout markers.")]
        [SerializeField] private float _markerHeight = 0.06f;
        [Tooltip("Edge length of the unit marker in world units — slightly larger than the biggest graybox vehicle body.")]
        [SerializeField] private float _unitMarkerSize = 2.4f;
        [Tooltip("Extra edge the building marker gains over the 3x3 footprint, so the outline is not fully hidden under the body.")]
        [SerializeField] private float _buildingMarkerMargin = 0.4f;
        [Tooltip("Own-selection green (RTS norm). Alpha comes from the marker texture; this tints border and fill together.")]
        [SerializeField] private Color _selectionColor = new Color(0.35f, 1f, 0.4f, 1f);

        private readonly Stack<GameObject> _pool = new Stack<GameObject>(SelectionManager.MaxSelectedEntities);
        private readonly List<GameObject> _active = new List<GameObject>(SelectionManager.MaxSelectedEntities);
        private MaterialPropertyBlock _tintBlock;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
            if (_views == null) _views = FindAnyObjectByType<UnitViewManager>();
        }

        private void LateUpdate()
        {
            ReturnAllToPool();
            if (_runner == null || _input == null || !_runner.IsRunning) return;
            EntityManager entities = _runner.Entities;
            if (entities == null) return;

            ReadOnlySpan<EntityId> selected = _input.Selection.SelectedEntities;
            for (int i = 0; i < selected.Length; i++)
            {
                if (!entities.TryGetUnit(selected[i], out UnitState unit)) continue;

                Vector3 anchor;
                if (_views != null && _views.TryGetView(selected[i], out GameObject view))
                {
                    // The interpolated body the player actually sees.
                    anchor = view.transform.position;
                }
                else
                {
                    anchor = new Vector3(
                        unit.Transform.PositionX.ToFloat(), 0f,
                        unit.Transform.PositionY.ToFloat());
                }

                bool building = SimDefinitions.IsBuildingRole(unit.Role);
                float size = building
                    ? SimDefinitions.BuildingFootprintCells + _buildingMarkerMargin
                    : _unitMarkerSize;
                float shift = building ? 0.5f : 0f; // footprint centre, see class remarks

                GameObject marker = Rent();
                Transform markerTransform = marker.transform;
                markerTransform.position = new Vector3(anchor.x + shift, _markerHeight, anchor.z + shift);
                markerTransform.localScale = new Vector3(size, size, 1f);
            }
        }

        private GameObject Rent()
        {
            GameObject marker = _pool.Count > 0 ? _pool.Pop() : CreateMarker();
            marker.SetActive(true);
            _active.Add(marker);
            return marker;
        }

        private GameObject CreateMarker()
        {
            GameObject marker = GroundMarkerVisuals.CreateFlatQuad("SelectionMarker", transform);
            // The tint never changes per marker, so the property block is
            // uploaded once at creation — the per-frame path stays free of
            // SetPropertyBlock calls.
            if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();
            _tintBlock.Clear();
            FactionTint.ApplyToPropertyBlock(_tintBlock, _selectionColor);
            marker.GetComponent<MeshRenderer>().SetPropertyBlock(_tintBlock);
            return marker;
        }

        private void ReturnAllToPool()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].SetActive(false);
                _pool.Push(_active[i]);
            }
            _active.Clear();
        }

        private void OnDestroy()
        {
            ReturnAllToPool();
            while (_pool.Count > 0)
            {
                Destroy(_pool.Pop());
            }
            GroundMarkerVisuals.DestroyShared();
        }
    }
}
