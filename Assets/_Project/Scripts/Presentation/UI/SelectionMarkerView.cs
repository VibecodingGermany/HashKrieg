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
    public enum ClickFeedbackKind : byte
    {
        Move,
        Attack,
        Harvest,
        Repair,
        Rally,
    }

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
        [Tooltip("Hostile attack-target red (sprint 18.2, #51). Drawn under visible enemies targeted by the current selection.")]
        [SerializeField] private Color _targetColor = new Color(1f, 0.28f, 0.22f, 1f);

        private struct ClickFeedbackInstance
        {
            public GameObject Object;
            public MeshRenderer Renderer;
            public Color BaseColor;
            public float Elapsed;
            public const float Duration = 0.35f;
        }

        private readonly Stack<GameObject> _pool = new Stack<GameObject>(SelectionManager.MaxSelectedEntities);
        private readonly List<GameObject> _active = new List<GameObject>(SelectionManager.MaxSelectedEntities);
        private readonly Stack<GameObject> _targetPool = new Stack<GameObject>(8);
        private readonly List<GameObject> _activeTargets = new List<GameObject>(8);
        private readonly List<ClickFeedbackInstance> _activeClickPings = new List<ClickFeedbackInstance>(8);
        private readonly Stack<GameObject> _clickPingPool = new Stack<GameObject>(8);
        private readonly HashSet<EntityId> _drawnTargets = new HashSet<EntityId>();
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

            _drawnTargets.Clear();
            ReadOnlySpan<EntityId> selected = _input.Selection.SelectedEntities;
            for (int i = 0; i < selected.Length; i++)
            {
                if (!entities.TryGetUnit(selected[i], out UnitState unit)) continue;

                PlaceMarker(Rent(), in unit, selected[i], _markerHeight);

                if (unit.AttackTarget.IsValid && _drawnTargets.Add(unit.AttackTarget))
                {
                    TryPlaceTargetMarker(entities, unit.AttackTarget);
                }
            }

            if (selected.Length > 0
                && _input.FocusedAttackTarget.IsValid
                && _drawnTargets.Add(_input.FocusedAttackTarget))
            {
                TryPlaceTargetMarker(entities, _input.FocusedAttackTarget);
            }

            UpdateClickFeedbackPings(Time.deltaTime);
        }

        /// <summary>
        /// Spawns a snappy animated ground marker ping at the commanded world location,
        /// colored by order kind (Green = Move, Red = Attack, Cyan = Harvest, Yellow = Repair, Orange = Rally).
        /// </summary>
        public void SpawnClickFeedback(Vector3 position, ClickFeedbackKind kind)
        {
            Color color;
            switch (kind)
            {
                case ClickFeedbackKind.Attack:
                    color = new Color(1f, 0.22f, 0.18f, 0.95f);
                    break;
                case ClickFeedbackKind.Harvest:
                    color = new Color(0.18f, 0.90f, 1f, 0.95f);
                    break;
                case ClickFeedbackKind.Repair:
                    color = new Color(1f, 0.85f, 0.20f, 0.95f);
                    break;
                case ClickFeedbackKind.Rally:
                    color = new Color(1f, 0.55f, 0.15f, 0.95f);
                    break;
                default:
                    color = new Color(0.30f, 1f, 0.42f, 0.95f);
                    break;
            }

            GameObject obj = _clickPingPool.Count > 0
                ? _clickPingPool.Pop()
                : CreateMarker("ClickFeedbackPing", color);

            obj.transform.position = new Vector3(position.x, _markerHeight + 0.02f, position.z);
            obj.transform.localScale = new Vector3(2.0f, 2.0f, 1f);
            obj.SetActive(true);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();
            _tintBlock.Clear();
            FactionTint.ApplyToPropertyBlock(_tintBlock, color);
            mr.SetPropertyBlock(_tintBlock);

            _activeClickPings.Add(new ClickFeedbackInstance
            {
                Object = obj,
                Renderer = mr,
                BaseColor = color,
                Elapsed = 0f,
            });
        }

        private void UpdateClickFeedbackPings(float deltaTime)
        {
            for (int i = _activeClickPings.Count - 1; i >= 0; i--)
            {
                ClickFeedbackInstance ping = _activeClickPings[i];
                ping.Elapsed += deltaTime;
                if (ping.Elapsed >= ClickFeedbackInstance.Duration)
                {
                    ping.Object.SetActive(false);
                    _clickPingPool.Push(ping.Object);
                    _activeClickPings.RemoveAt(i);
                    continue;
                }

                float t = ping.Elapsed / ClickFeedbackInstance.Duration;
                float currentScale = Mathf.Lerp(2.2f, 0.9f, t);
                ping.Object.transform.localScale = new Vector3(currentScale, currentScale, 1f);

                float alpha = (1f - t) * (1f - t);
                Color c = new Color(ping.BaseColor.r, ping.BaseColor.g, ping.BaseColor.b, ping.BaseColor.a * alpha);
                if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();
                _tintBlock.Clear();
                FactionTint.ApplyToPropertyBlock(_tintBlock, c);
                ping.Renderer.SetPropertyBlock(_tintBlock);

                _activeClickPings[i] = ping;
            }
        }

        private void TryPlaceTargetMarker(EntityManager entities, EntityId targetId)
        {
            if (!entities.TryGetUnit(targetId, out UnitState target)) return;
            if (!_input.IsForeignEntityVisibleToLocalSlot(in target)) return;

            PlaceMarker(RentTarget(), in target, targetId, _markerHeight + 0.01f);
        }

        private void PlaceMarker(GameObject marker, in UnitState unit, EntityId id, float height)
        {
            Vector3 anchor;
            if (_views != null && _views.TryGetView(id, out GameObject view))
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

            Transform markerTransform = marker.transform;
            markerTransform.position = new Vector3(anchor.x + shift, height, anchor.z + shift);
            markerTransform.localScale = new Vector3(size, size, 1f);
        }

        private GameObject Rent()
        {
            GameObject marker = _pool.Count > 0 ? _pool.Pop() : CreateMarker("SelectionMarker", _selectionColor);
            marker.SetActive(true);
            _active.Add(marker);
            return marker;
        }

        private GameObject RentTarget()
        {
            GameObject marker = _targetPool.Count > 0 ? _targetPool.Pop() : CreateMarker("AttackTargetMarker", _targetColor);
            marker.SetActive(true);
            _activeTargets.Add(marker);
            return marker;
        }

        private GameObject CreateMarker(string markerName, Color color)
        {
            GameObject marker = GroundMarkerVisuals.CreateFlatQuad(markerName, transform);
            // The tint never changes per marker, so the property block is
            // uploaded once at creation — the per-frame path stays free of
            // SetPropertyBlock calls.
            if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();
            _tintBlock.Clear();
            FactionTint.ApplyToPropertyBlock(_tintBlock, color);
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

            for (int i = 0; i < _activeTargets.Count; i++)
            {
                _activeTargets[i].SetActive(false);
                _targetPool.Push(_activeTargets[i]);
            }
            _activeTargets.Clear();
        }

        private void OnDestroy()
        {
            ReturnAllToPool();
            while (_pool.Count > 0)
            {
                Destroy(_pool.Pop());
            }
            while (_targetPool.Count > 0)
            {
                Destroy(_targetPool.Pop());
            }
            while (_clickPingPool.Count > 0)
            {
                Destroy(_clickPingPool.Pop());
            }
            for (int i = 0; i < _activeClickPings.Count; i++)
            {
                if (_activeClickPings[i].Object != null) Destroy(_activeClickPings[i].Object);
            }
            _activeClickPings.Clear();
            GroundMarkerVisuals.DestroyShared();
        }
    }
}
