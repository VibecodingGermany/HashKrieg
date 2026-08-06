using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Rally point readout of the SELECTED own producer buildings: a small
    /// flag quad at the rally point plus a flat line quad from the building's
    /// footprint centre to it. Drawing only for the current selection is the
    /// classic C&amp;C idiom — the lines are order feedback, not map
    /// decoration, and a permanent line thicket would drown the map.
    /// <para>
    /// The data comes from <see cref="ProductionSystem.TryGetProducer"/>: a
    /// producer row exists only once the building queued or received a rally
    /// point, and its rally fields are the very values the spawner walks
    /// units to. The marker therefore reflects actual sim state (including
    /// the documented default rally two cells east of the building) and can
    /// never drift from it — there is no UI-side copy of the rally point.
    /// </para>
    /// <para>
    /// Both quads are flat ground markers from the shared
    /// <see cref="GroundMarkerVisuals"/> pool, pooled per frame exactly like
    /// the selection markers: no per-frame allocation, no LineRenderer, no
    /// GL calls.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RallyFlagView : MonoBehaviour
    {
        /// <summary>Cap on simultaneously drawn flag/line pairs; a selection can hold at most 64 entities, producers among them are rare.</summary>
        private const int MaxPairs = 16;

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Marker look")]
        [SerializeField] private float _markerHeight = 0.07f;
        [Tooltip("Edge length of the rally flag quad in world units.")]
        [SerializeField] private float _flagSize = 1.0f;
        [Tooltip("Width of the line quad from the building to the rally flag.")]
        [SerializeField] private float _lineWidth = 0.18f;
        [Tooltip("Amber reads as 'order/egress' and stays distinct from both the green selection markers and the faction body tints.")]
        [SerializeField] private Color _rallyColor = new Color(1f, 0.82f, 0.25f, 1f);

        private readonly Stack<Pair> _pool = new Stack<Pair>(MaxPairs);
        private readonly List<Pair> _active = new List<Pair>(MaxPairs);
        private MaterialPropertyBlock _tintBlock;

        /// <summary>A flag/line pair. Holds two GameObjects so both pool and hide together.</summary>
        private sealed class Pair
        {
            public GameObject Flag;
            public GameObject Line;
        }

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void LateUpdate()
        {
            ReturnAllToPool();
            if (_runner == null || _input == null || !_runner.IsRunning) return;
            EntityManager entities = _runner.Entities;
            ProductionSystem production = _runner.Production;
            if (entities == null || production == null) return;

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            ReadOnlySpan<EntityId> selected = _input.Selection.SelectedEntities;
            for (int i = 0; i < selected.Length && _active.Count < MaxPairs; i++)
            {
                if (!entities.TryGetUnit(selected[i], out UnitState unit)) continue;
                if (unit.PlayerId != slot) continue;
                // Only producer buildings own a production row; for everything
                // else this lookup is false and the entity is skipped.
                if (!production.TryGetProducer(
                        UnitCommandStateView.ToRawEntityId(selected[i]),
                        out _, out int rallyXRaw, out int rallyYRaw))
                {
                    continue;
                }

                // Presentation boundary: Q16.16 rally -> float happens here.
                var rally = new Vector3(
                    SimFixed.FromRaw(rallyXRaw).ToFloat(), _markerHeight,
                    SimFixed.FromRaw(rallyYRaw).ToFloat());
                // Building entities anchor at the corner of their footprint's
                // centre cell (origin + 1); half a cell shifts to the true
                // 3x3 footprint centre.
                var origin = new Vector3(
                    unit.Transform.PositionX.ToFloat() + 0.5f, _markerHeight,
                    unit.Transform.PositionY.ToFloat() + 0.5f);

                Pair pair = Rent();
                PlaceFlag(pair.Flag.transform, rally);
                PlaceLine(pair.Line.transform, origin, rally);
            }
        }

        private void PlaceFlag(Transform flag, Vector3 rally)
        {
            flag.position = rally;
            flag.rotation = Quaternion.Euler(90f, 0f, 0f);
            flag.localScale = new Vector3(_flagSize, _flagSize, 1f);
        }

        private void PlaceLine(Transform line, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            // Degenerate (rally sits on the footprint centre): a zero-length
            // quad renders as nothing useful, so the line hides and the flag
            // alone carries the readout.
            line.gameObject.SetActive(length > 0.05f);
            if (length <= 0.05f) return;

            line.position = from + delta * 0.5f;
            // After the 90-degree pitch the quad's local Y lies along world
            // +Z; yawing by atan2 turns local Y onto the delta direction.
            line.rotation = Quaternion.Euler(90f, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0f);
            line.localScale = new Vector3(_lineWidth, length, 1f);
        }

        private Pair Rent()
        {
            Pair pair = _pool.Count > 0 ? _pool.Pop() : CreatePair();
            pair.Flag.SetActive(true);
            pair.Line.SetActive(true);
            _active.Add(pair);
            return pair;
        }

        private Pair CreatePair()
        {
            if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();
            _tintBlock.Clear();
            FactionTint.ApplyToPropertyBlock(_tintBlock, _rallyColor);

            var pair = new Pair
            {
                Flag = GroundMarkerVisuals.CreateFlatQuad("RallyFlag", transform),
                Line = GroundMarkerVisuals.CreateFlatQuad("RallyLine", transform)
            };
            // The tint is constant per pair: upload once at creation, never
            // per frame.
            pair.Flag.GetComponent<MeshRenderer>().SetPropertyBlock(_tintBlock);
            pair.Line.GetComponent<MeshRenderer>().SetPropertyBlock(_tintBlock);
            return pair;
        }

        private void ReturnAllToPool()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].Flag.SetActive(false);
                _active[i].Line.SetActive(false);
                _pool.Push(_active[i]);
            }
            _active.Clear();
        }

        private void OnDestroy()
        {
            ReturnAllToPool();
            while (_pool.Count > 0)
            {
                Pair pair = _pool.Pop();
                Destroy(pair.Flag);
                Destroy(pair.Line);
            }
            GroundMarkerVisuals.DestroyShared();
        }
    }
}
