using System.Collections.Generic;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// World-space state marker for the local player's construction sites
    /// (D-085): a paused site must be distinguishable from a growing one at
    /// a glance, without selecting it first. Every own site carries a flat
    /// 3x3 ground frame (<see cref="GroundMarkerVisuals"/>, the placement
    /// ghost's shared material); a GROWING site shows a quiet steady frame,
    /// a PAUSED one — no living Builder assigned, or the Builder still
    /// outside the footprint's Chebyshev reach — a pulsing amber frame.
    /// <para>
    /// Pure view: the pause verdict is computed from the same mirrored rules
    /// the site card prints (<see cref="ConstructionSiteStatus"/>), never
    /// from a second interpretation of the sim. The pulse rides
    /// <see cref="Time.time"/> — presentation frame time, never simulation
    /// state. The quads come from a small pool (few sites exist at once), so
    /// steady state allocates nothing.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConstructionSiteMarkerView : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;

        [Header("Marker look")]
        [Tooltip("Ground height of the marker quads (below the placement ghost's 0.09, above the weathered edge veil).")]
        [SerializeField] private float _markerHeight = 0.07f;
        [Tooltip("Steady frame of a growing site.")]
        [SerializeField] private Color _buildingColor = new Color(0.35f, 0.90f, 0.55f, 0.55f);
        [Tooltip("Pulsing frame of a paused site (amber = needs attention).")]
        [SerializeField] private Color _pausedColor = new Color(1.00f, 0.62f, 0.15f, 1.00f);
        [Tooltip("Pulse frequency of the paused frame, in radians per second.")]
        [SerializeField] private float _pulseSpeed = 4f;

        private struct SiteVisual
        {
            public Vector3 Center;
            public bool Paused;
        }

        private readonly List<SiteVisual> _sites = new List<SiteVisual>(8);
        private readonly List<GameObject> _pool = new List<GameObject>(8);
        private readonly List<Renderer> _renderers = new List<Renderer>(8);
        private MaterialPropertyBlock _tintBlock;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
        }

        private void LateUpdate()
        {
            CollectSites();
            EnsurePool(_sites.Count);
            if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();

            // Paused sites pulse; the pulse is presentation frame time, so
            // it can never leak into the deterministic simulation.
            float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * _pulseSpeed);

            for (int i = 0; i < _pool.Count; i++)
            {
                bool used = i < _sites.Count;
                if (_pool[i].activeSelf != used) _pool[i].SetActive(used);
                if (!used) continue;

                _pool[i].transform.position = _sites[i].Center;
                Color color = _sites[i].Paused
                    ? new Color(_pausedColor.r, _pausedColor.g, _pausedColor.b, pulse)
                    : _buildingColor;
                _tintBlock.Clear();
                FactionTint.ApplyToPropertyBlock(_tintBlock, color);
                _renderers[i].SetPropertyBlock(_tintBlock);
            }
        }

        /// <summary>
        /// Every own live construction site, with its pause verdict in the
        /// sim's own evaluation order: no assigned Builder (none alive), then
        /// the reach check against the mirrored Chebyshev rule — the site
        /// entity sits at the footprint's center cell, so the origin is
        /// center minus half the footprint (the AI's derivation too).
        /// </summary>
        private void CollectSites()
        {
            _sites.Clear();
            EntityManager entities = _runner != null ? _runner.Entities : null;
            ConstructionSystem construction = _runner != null ? _runner.Construction : null;
            if (entities == null || construction == null || _runner.Session == null) return;

            byte slot = _runner.Session.LocalSlot;
            const int half = SimDefinitions.BuildingFootprintCells / 2;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId != slot) continue;
                uint raw = UnitCommandStateView.ToRawEntityId(unit.Id);
                if (raw == 0) continue;
                if (!construction.TryGetSite(raw, out _, out _, out uint assignedBuilderRaw)) continue;

                bool paused = true;
                if (assignedBuilderRaw != 0
                    && entities.TryGetUnit(UnitCommandStateView.ToEntityId(assignedBuilderRaw), out UnitState builder))
                {
                    int originX = GridCellOf(unit.Transform.PositionX) - half;
                    int originY = GridCellOf(unit.Transform.PositionY) - half;
                    paused = !ConstructionSiteStatus.IsInReachOfFootprint(
                        GridCellOf(builder.Transform.PositionX), GridCellOf(builder.Transform.PositionY),
                        originX, originY, SimDefinitions.BuildingFootprintCells);
                }

                // Presentation boundary: SimFixed -> float happens here.
                _sites.Add(new SiteVisual
                {
                    Center = new Vector3(
                        unit.Transform.PositionX.ToFloat(), _markerHeight, unit.Transform.PositionY.ToFloat()),
                    Paused = paused
                });
            }
        }

        /// <summary>Sim-space coordinate to grid cell at the presentation boundary: floor, clamped at 0 — the sim's mapping.</summary>
        private static int GridCellOf(Nova.Core.SimFixed simCoordinate)
        {
            return Mathf.Max(0, Mathf.FloorToInt(simCoordinate.ToFloat()));
        }

        /// <summary>Grows the quad pool to <paramref name="count"/>; surplus quads stay pooled (deactivated by LateUpdate).</summary>
        private void EnsurePool(int count)
        {
            while (_pool.Count < count)
            {
                GameObject quad = GroundMarkerVisuals.CreateFlatQuad($"SiteMarker_{_pool.Count}", transform);
                quad.transform.localScale = new Vector3(
                    SimDefinitions.BuildingFootprintCells, SimDefinitions.BuildingFootprintCells, 1f);
                _pool.Add(quad);
                _renderers.Add(quad.GetComponent<MeshRenderer>());
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null) Destroy(_pool[i]);
            }
            _pool.Clear();
            _renderers.Clear();
            GroundMarkerVisuals.DestroyShared();
        }
    }
}
