using System;
using Nova.Core;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Deterministic simulation system for Flow-Field Pathfinding.
    /// Manages cost fields, integration wave propagation, and flow field generation.
    /// <para>
    /// Multi-destination cache: the system holds up to
    /// <see cref="FlowFieldCache.MaxEntries"/> generated flow fields keyed by
    /// their destination cell (manifest caps
    /// <c>capacity.flowFieldCacheEntryCap</c> / <c>flowFieldCacheMiBCap</c>).
    /// A unit follows the field of its own <c>TargetGridPos</c>, so ordering
    /// one group somewhere no longer retargets every other moving group.
    /// The cache key is the already snapshot-serialized
    /// <c>UnitState.TargetGridPos</c> — no unit state and no entity-store
    /// block change is involved.
    /// </para>
    /// <para>
    /// Stateful (<see cref="IStatefulSimSystem"/>): the block stores the cost
    /// field epoch, the cache directory (destination + last-used tick per
    /// entry, canonically ordered) and the most recently requested
    /// destination. The integration/flow field CONTENTS stay a derived cache
    /// of (cost field, destination) and are rebuilt on restore by re-running
    /// the identical deterministic generation — this is the derived-cache
    /// rebuild the spec allows when the continuation provably matches
    /// (docs/tech/SimulationCore.md section 3). Since the sprint
    /// Truppenführung the cost field also carries building footprints, so
    /// the epoch counts mutation HISTORY a restore cannot replay: the
    /// serialized epoch is therefore ADOPTED (<see cref="CostField.RestoreEpoch"/>)
    /// instead of compared, and the content proof is structural — footprint
    /// content is fully determined by the construction block, which restores
    /// before this one (registration order).
    /// </para>
    /// <para>
    /// Deliberately not sealed and <see cref="RequestFlowField"/> is virtual:
    /// the flow-field generation runs inside kernel command application
    /// (UnitCommandStateView.ApplyMove), so the headless performance harness
    /// (tools/Nova.SimRunner) can only attribute the pathfinding phase by
    /// subclassing this system. The simulation itself carries no measurement
    /// logic; the interception point exists solely so harness code can wrap
    /// the call from outside. Consequently <see cref="GetField"/> is a pure
    /// lookup — a generating lookup would silently move pathfinding cost out
    /// of the measured window and under-report the V4/V5a and G5 budgets.
    /// Generation additionally happens inside <see cref="ExecuteTick"/> when
    /// a cost field mutation forces the bounded cache regeneration
    /// (SyncCostFieldEpoch) — per-system tick work of this system's phase.
    /// </para>
    /// </summary>
    public class PathfindingSystem : IStatefulSimSystem
    {
        /// <summary>
        /// Serialization version of the pathfinding snapshot block. v2 adds
        /// the cost field epoch and the flow-field cache directory; the
        /// pre-G1 format window is open, so v1 blocks are rejected outright
        /// rather than migrated.
        /// </summary>
        public const byte StateVersion = 2;

        private readonly FlowFieldCache _cache;
        private readonly GridPos2D[] _entryDestinations;
        private readonly uint[] _entryTicks;

        /// <summary>Tick stamped onto cache entries; mirrors the last executed tick.</summary>
        private uint _currentTick;

        /// <summary>Cost field epoch the cached fields were generated against.</summary>
        private uint _cacheEpoch;

        public string Name => "PathfindingSystem";

        public CostField CostField { get; }

        /// <summary>
        /// Shared integration scratch buffer of the most recent GENERATION.
        /// Read only inside <see cref="RequestFlowField"/> (and by debug
        /// views); no behaviour depends on its content across calls, so it is
        /// deliberately not part of the snapshot block.
        /// </summary>
        public IntegrationField IntegrationField { get; }

        /// <summary>
        /// Flow field of the most recently requested destination, or null
        /// before the first request. Kept for debug views and existing tests;
        /// movement code must use <see cref="GetField"/> with the unit's own
        /// target, otherwise the single-field retargeting bug returns.
        /// </summary>
        public FlowField FlowField => _cache.MruField;

        /// <summary>Number of destinations the cache can hold simultaneously.</summary>
        public int FlowFieldCacheCapacity => _cache.Capacity;

        /// <summary>Number of currently cached destinations.</summary>
        public int FlowFieldCacheCount => _cache.Count;

        public PathfindingSystem(ushort width, ushort height)
        {
            CostField = new CostField(width, height);
            IntegrationField = new IntegrationField(width, height);
            _cache = new FlowFieldCache(width, height);
            _entryDestinations = new GridPos2D[_cache.Capacity];
            _entryTicks = new uint[_cache.Capacity];
            _currentTick = 0;
            _cacheEpoch = CostField.Epoch;
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo(
                $"[{Name}] Initialized Flow-Field Grid ({CostField.Width}x{CostField.Height}), " +
                $"cache capacity {_cache.Capacity}.");
        }

        /// <summary>
        /// Generates (or refreshes) the flow field for <paramref name="destination"/>
        /// and makes it the most recent entry. A hit on an entry generated
        /// against the current <see cref="CostField.Epoch"/> is served without
        /// regenerating — the result is bit-identical by construction, so the
        /// skip is a pure cost saving and not a behaviour change. An epoch
        /// change drops the whole cache before anything is served.
        /// Out-of-bounds destinations are ignored; every cached destination is
        /// therefore inside the map, which the snapshot block relies on.
        /// </summary>
        public virtual void RequestFlowField(GridPos2D destination)
        {
            if (!destination.IsValid || !CostField.IsInBounds(destination.X, destination.Y))
            {
                return;
            }

            SyncCostFieldEpoch();

            int hit = _cache.FindSlot(destination);
            if (hit >= 0)
            {
                _cache.Touch(hit, _currentTick);
                return;
            }

            int slot = _cache.Acquire(destination, _currentTick);
            IntegrationField.Generate(CostField, destination);
            _cache.FieldAt(slot).Generate(IntegrationField, CostField);
        }

        /// <summary>
        /// Pure lookup of the field belonging to <paramref name="destination"/>;
        /// null when it is not cached. Never generates and never mutates (see
        /// the class remarks on performance attribution). Callers treat null
        /// as "no flow information" and fall back to direct steering.
        /// </summary>
        public FlowField GetField(GridPos2D destination)
        {
            return _cache.TryGetField(destination);
        }

        /// <summary>True when a field for <paramref name="destination"/> is cached.</summary>
        public bool HasField(GridPos2D destination)
        {
            return _cache.FindSlot(destination) >= 0;
        }

        public void ExecuteTick(Tick tick)
        {
            // No pathfinding work is scheduled per tick (fields are generated
            // on command application); the tick is recorded because it is the
            // deterministic recency key of the flow-field cache.
            _currentTick = tick.Value;

            // Terrain that moved during command application invalidates every
            // derived field. Syncing here as well as in RequestFlowField
            // guarantees the cache is consistent with the cost field at every
            // tick boundary — which is where snapshots and state hashes are
            // taken — and, under the canonical registration order
            // (pathfinding before movement), before any unit reads a field.
            SyncCostFieldEpoch();
        }

        public void Shutdown()
        {
        }

        /// <summary>Snapshot block id of the pathfinding state (registry: <see cref="Snapshots.SnapshotBlockIds"/>).</summary>
        public ushort StateBlockId => Snapshots.SnapshotBlockIds.Pathfinding;

        /// <summary>
        /// Writes the cost field epoch, the recency tick, the most recent
        /// destination and the cache directory in canonical order; the field
        /// contents are rebuilt on restore.
        /// </summary>
        public void WriteState(Snapshots.SnapshotBlockWriter writer)
        {
            // The block always describes a cache that is valid for the CURRENT
            // cost field. In the (currently unreachable) window between a
            // terrain write and the next sync the resident cache is stale, so
            // it is written as empty rather than as a directory a restoring
            // host would rebuild against different terrain. WriteState is a
            // read path — it never mutates, not even the derived cache.
            bool cacheValid = _cacheEpoch == CostField.Epoch;
            int count = cacheValid ? _cache.CopyCanonicalEntries(_entryDestinations, _entryTicks) : 0;
            bool hasMru = cacheValid && _cache.HasMru;
            GridPos2D mru = hasMru ? _cache.MruDestination : GridPos2D.Invalid;

            writer.WriteUInt8(StateVersion);
            writer.WriteUInt32(CostField.Epoch);
            writer.WriteUInt32(_currentTick);
            writer.WriteUInt8(hasMru ? (byte)1 : (byte)0);
            writer.WriteUInt16(mru.X);
            writer.WriteUInt16(mru.Y);
            writer.WriteUInt8((byte)count);
            for (int i = 0; i < count; i++)
            {
                writer.WriteUInt16(_entryDestinations[i].X);
                writer.WriteUInt16(_entryDestinations[i].Y);
                writer.WriteUInt32(_entryTicks[i]);
            }
        }

        /// <summary>
        /// Fully validates a pathfinding block produced by
        /// <see cref="WriteState"/> without touching the current state.
        /// </summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _, out _, out _, out _, out _);
        }

        /// <summary>
        /// Restores the cache directory and deterministically rebuilds every
        /// cached field from it. Malformed input — including a cost field
        /// epoch that does not match this host — returns false without
        /// touching the current state.
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out uint tick, out GridPos2D mru,
                    out int count, out bool hasMru, out uint epoch))
            {
                return false;
            }

            // Commit: rebuild the derived cache from the canonical inputs so
            // the restored host continues with identical directions. The
            // parsed entries live in the scratch buffers filled by
            // TryParseState; they are already in canonical order. The
            // construction block committed before this one (registration
            // order), so the live cost field already carries every restored
            // footprint; should a host ever restore in a different order,
            // the next SyncCostFieldEpoch regenerates the fields against the
            // completed cost field — convergence is guaranteed either way.
            _cache.Clear();
            for (int i = 0; i < count; i++)
            {
                int slot = _cache.Acquire(_entryDestinations[i], _entryTicks[i]);
                IntegrationField.Generate(CostField, _entryDestinations[i]);
                _cache.FieldAt(slot).Generate(IntegrationField, CostField);
            }

            if (hasMru)
            {
                // Validated above: the MRU destination is one of the entries.
                _cache.TrySetMru(mru);

                // Leave the shared scratch on the most recent destination so
                // debug views match the live host. Behaviour never depends on
                // it (it is consumed inside the generating call).
                IntegrationField.Generate(CostField, mru);
            }

            // Adopt the serialized epoch: footprint writes count mutation
            // HISTORY, which a block restore cannot replay, so the live
            // counter after a rebuild differs from the saving host's.
            // Adopting keeps both counters in lockstep for every mutation
            // that follows the restore — later snapshots stay byte-comparable.
            CostField.RestoreEpoch(epoch);
            _currentTick = tick;
            _cacheEpoch = epoch;
            return true;
        }

        /// <summary>
        /// Rebuilds every cached field against the current cost field when
        /// the terrain moved since the fields were generated (a building
        /// footprint written by the construction system). Regeneration in
        /// place — not a cache drop — keeps the fields of already-moving
        /// units valid: a dropped cache would fall those units back to
        /// direct steering, which knows no costs and would walk them
        /// through walls. The sync runs at most once per tick boundary (or
        /// once per request after a mutation), so a whole build queue of
        /// placements inside one tick coalesces into a single regeneration
        /// pass bounded by the cache capacity — never one recompute per
        /// placement.
        /// </summary>
        private void SyncCostFieldEpoch()
        {
            if (_cacheEpoch == CostField.Epoch) return;

            int count = _cache.CopyCanonicalEntries(_entryDestinations, _entryTicks);
            for (int i = 0; i < count; i++)
            {
                int slot = _cache.FindSlot(_entryDestinations[i]);
                IntegrationField.Generate(CostField, _entryDestinations[i]);
                _cache.FieldAt(slot).Generate(IntegrationField, CostField);
            }
            _cacheEpoch = CostField.Epoch;
        }

        /// <summary>
        /// Parses and fully validates block content. On success the canonical
        /// entries are left in <see cref="_entryDestinations"/> /
        /// <see cref="_entryTicks"/> for the commit phase; those buffers are
        /// scratch, never authoritative state, so filling them does not
        /// violate the "validate mutates nothing" contract.
        /// </summary>
        private bool TryParseState(
            ReadOnlySpan<byte> blockContent,
            out uint tick, out GridPos2D mruDestination, out int entryCount, out bool hasMru,
            out uint epoch)
        {
            tick = 0;
            mruDestination = GridPos2D.Invalid;
            entryCount = 0;
            hasMru = false;
            epoch = 0;

            var reader = new Snapshots.SnapshotBlockReader(blockContent);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            // The serialized epoch is NOT compared against the live field:
            // with building footprints written into the cost field it counts
            // mutation history, which a restore cannot replay — the restore
            // adopts it instead (see TryRestoreState). The field stays in the
            // v2 format and is validated for presence only.
            if (!reader.TryReadUInt32(out uint parsedEpoch)) return false;
            if (!reader.TryReadUInt32(out uint parsedTick)) return false;
            if (!reader.TryReadUInt8(out byte hasMruRaw) || hasMruRaw > 1) return false;
            if (!reader.TryReadUInt16(out ushort mruX)) return false;
            if (!reader.TryReadUInt16(out ushort mruY)) return false;
            if (!reader.TryReadUInt8(out byte countRaw)) return false;
            if (countRaw > _cache.Capacity) return false;

            var parsedMru = new GridPos2D(mruX, mruY);
            if (hasMruRaw == 0)
            {
                // Canonical empty state: no MRU implies no entries and the
                // exact invalid-destination sentinel.
                if (countRaw != 0) return false;
                if (mruX != ushort.MaxValue || mruY != ushort.MaxValue) return false;
            }
            else if (!CostField.IsInBounds(parsedMru.X, parsedMru.Y) || countRaw == 0)
            {
                return false;
            }

            int previousIndex = -1;
            bool mruFound = false;
            for (int i = 0; i < countRaw; i++)
            {
                if (!reader.TryReadUInt16(out ushort destX)) return false;
                if (!reader.TryReadUInt16(out ushort destY)) return false;
                if (!reader.TryReadUInt32(out uint entryTick)) return false;

                var destination = new GridPos2D(destX, destY);
                if (!CostField.IsInBounds(destX, destY)) return false;

                // Canonical order: strictly ascending grid index. Rejects both
                // duplicates and any non-canonical permutation.
                int index = _cache.GridIndex(destination);
                if (index <= previousIndex) return false;
                previousIndex = index;

                if (hasMruRaw == 1 && destination == parsedMru) mruFound = true;

                _entryDestinations[i] = destination;
                _entryTicks[i] = entryTick;
            }
            if (reader.Remaining != 0) return false;
            if (hasMruRaw == 1 && !mruFound) return false;

            tick = parsedTick;
            mruDestination = parsedMru;
            entryCount = countRaw;
            hasMru = hasMruRaw == 1;
            epoch = parsedEpoch;
            return true;
        }
    }
}
