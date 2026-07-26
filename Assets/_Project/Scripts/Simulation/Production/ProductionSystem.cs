using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Production
{
    /// <summary>
    /// Canonical production system of the MS-1 production/construction slice
    /// (docs/tech/SimulationCore.md section 2, phase 4): per-building unit
    /// queues with up-front costs, T2 gating, rally points and deterministic
    /// spawning. Deterministic, pure integer/fixed-point, zero engine
    /// dependencies. Replaces the unregistered prototype scaffolding
    /// (pre-G1 reset): the retired ProductionQueueSystem held one global
    /// queue with float definition relics, and the retired ResearchTreeSystem
    /// modeled a tech tree that MS-1 explicitly does not have —
    /// quality/content/mvp-v1.json technology: a completed ResearchLab
    /// unlocks tier 2, researchUpgradesEnabled = false,
    /// researchQueueEnabled = false, tier3Enabled = false. There is
    /// deliberately NO research system in this slice; the T2 flag lives in
    /// the construction domain (it is set by a construction completion
    /// event) and is only READ here.
    /// <para>
    /// Tick order: registered after the construction system and before
    /// pathfinding/movement (phases 4/5 before 6). A unit spawned in phase 4
    /// of tick T carries no movement order yet, so movement leaves it
    /// untouched in tick T; the earliest it can move is tick T+1 via a
    /// command (documented interaction).
    /// </para>
    /// <para>
    /// QueueUnit: each command appends ONE queue entry of Count units to the
    /// named building; the full cost (CostAE x Count) is charged at enqueue
    /// via <see cref="PlayerEconomyState.TrySpendCredits"/>. Producer
    /// assignment (documented provisional, Q-040): HQ produces Builder and
    /// Harvester, Barracks produces the infantry roles, VehicleFactory
    /// produces the vehicle roles (<see cref="SimDefinitions"/>). The
    /// state-dependent validation order is fixed: unknown unit definition,
    /// building not an own COMPLETED producer of the unit's class
    /// (RejectedInvalidTarget), then tier-2 gating (RejectedPrerequisitesNotMet
    /// while the owner's T2 unlock is missing), then queue capacity
    /// (provisional maximum <see cref="MaxQueueEntries"/> entries per
    /// building, Q-040 candidate; also RejectedPrerequisitesNotMet), then
    /// affordability of CostAE x Count (RejectedInsufficientResources).
    /// </para>
    /// <para>
    /// Processing: only the first entry runs. Progress accumulates in exact
    /// Q16.16 — <see cref="PlayerEconomyState.ProductionSpeedMultiplierQ16"/>
    /// raw per tick (1.0 at full power, exactly 0.5 under low power; no
    /// rounding, 0.5 is exact in Q16.16). On completion the unit spawns at
    /// the building's rally point: the rally CELL is tried first, then
    /// expanding Chebyshev rings 1..<see cref="SpawnSearchMaxRing"/>, within
    /// a ring in ascending (y, x) order; a cell is free when no placement
    /// footprint covers it (units may share cells — provisional, Q-040).
    /// When no free cell exists inside the search range or the entity store
    /// is full (MS-1 cap 1.024, mvp-v1.json capacity.entityStoreCap), the
    /// finished unit waits: progress stays clamped at the threshold and the
    /// queue retries every tick (documented pause, no unit is lost and none
    /// is spawned twice).
    /// </para>
    /// <para>
    /// Rally/cancel (documented provisional rules, Q-040 candidates):
    /// SetRallyPoint stores a fixed-point world target per producing
    /// building (default: two cells east of the building's center cell).
    /// CancelProduction(index) removes one entry and refunds CostAE x its
    /// REMAINING unit count in full — the cancel action itself is free; a
    /// running entry therefore refunds exactly the units not yet spawned,
    /// and removing a queued (unstarted) entry costs and refunds the same.
    /// A queue whose building is sold or destroyed is lost WITHOUT refund.
    /// </para>
    /// <para>
    /// State (snapshot block <see cref="SnapshotBlockIds.Production"/>, v1):
    /// per producer row the building entity, the rally point (Q16.16 raw)
    /// and the queue entries (unit definition, remaining count, fixed-point
    /// progress). Hash-sensitive by construction; restore is two-phase and
    /// validates capacities, known definitions, progress bounds and unique
    /// producer ids. Unit hit points, roles and positions live with the
    /// entities (entity store block) — exactly one home per value.
    /// </para>
    /// </summary>
    public sealed class ProductionSystem : IStatefulSimSystem
    {
        /// <summary>Serialization version of the production snapshot block.</summary>
        public const byte StateVersion = 1;

        /// <summary>Format capacity for producer rows (buildings with a queue or rally point).</summary>
        public const int MaxProducers = 64;

        /// <summary>Provisional maximum queue entries per building (Q-040 candidate).</summary>
        public const int MaxQueueEntries = 5;

        /// <summary>Provisional spawn search range in Chebyshev rings around the rally cell (Q-040 candidate).</summary>
        public const int SpawnSearchMaxRing = 8;

        private struct QueueEntry
        {
            public ushort UnitDefId;
            public ushort RemainingCount;
            public int ProgressRaw; // Q16.16 ticks progressed on the current unit
        }

        private sealed class ProducerRow
        {
            public bool IsActive;
            public uint RawEntityId;
            public int RallyXRaw; // SimFixed raw
            public int RallyYRaw; // SimFixed raw
            public int EntryCount;
            public readonly QueueEntry[] Entries = new QueueEntry[MaxQueueEntries];
        }

        private readonly EntityManager _entityManager;
        private readonly EconomySystem _economy;
        private readonly Construction.ConstructionSystem _construction;
        private readonly ProducerRow[] _producers;

        public string Name => "ProductionSystem";

        public ushort StateBlockId => SnapshotBlockIds.Production;

        public ProductionSystem(EntityManager entityManager, EconomySystem economy, Construction.ConstructionSystem construction)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
            _producers = new ProducerRow[MaxProducers];
            for (int i = 0; i < MaxProducers; i++)
            {
                _producers[i] = new ProducerRow();
            }
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo(
                $"[{Name}] Initialized canonical production ({MaxProducers} producers, {MaxQueueEntries} queue entries each, no research tree — MS-1 technology model).");
        }

        public void Shutdown()
        {
        }

        // ------------------------------------------------------------------
        // Read-only queries
        // ------------------------------------------------------------------

        /// <summary>Total number of queued (not yet spawned) units across all producer rows.</summary>
        public int TotalQueuedUnits
        {
            get
            {
                int total = 0;
                for (int i = 0; i < MaxProducers; i++)
                {
                    ProducerRow row = _producers[i];
                    if (!row.IsActive) continue;
                    for (int e = 0; e < row.EntryCount; e++)
                    {
                        total += row.Entries[e].RemainingCount;
                    }
                }
                return total;
            }
        }

        /// <summary>Read-only snapshot of one producer row for tests and diagnostics.</summary>
        public bool TryGetProducer(uint rawEntityId, out int entryCount, out int rallyXRaw, out int rallyYRaw)
        {
            ProducerRow row = FindRow(rawEntityId);
            if (row != null)
            {
                entryCount = row.EntryCount;
                rallyXRaw = row.RallyXRaw;
                rallyYRaw = row.RallyYRaw;
                return true;
            }
            entryCount = 0;
            rallyXRaw = 0;
            rallyYRaw = 0;
            return false;
        }

        /// <summary>Read-only snapshot of one queue entry for tests and diagnostics.</summary>
        public bool TryGetQueueEntry(uint rawEntityId, int index, out ushort unitDefId, out ushort remainingCount, out int progressRaw)
        {
            ProducerRow row = FindRow(rawEntityId);
            if (row != null && index >= 0 && index < row.EntryCount)
            {
                unitDefId = row.Entries[index].UnitDefId;
                remainingCount = row.Entries[index].RemainingCount;
                progressRaw = row.Entries[index].ProgressRaw;
                return true;
            }
            unitDefId = 0;
            remainingCount = 0;
            progressRaw = 0;
            return false;
        }

        // ------------------------------------------------------------------
        // State-dependent validation (no mutation; fixed documented order)
        // ------------------------------------------------------------------

        /// <summary>
        /// QueueUnit legality in fixed order: unknown definition and
        /// building-not-an-own-completed-producer (RejectedInvalidTarget),
        /// then T2 gating and queue/producer capacity
        /// (RejectedPrerequisitesNotMet), then affordability of CostAE x
        /// Count (RejectedInsufficientResources).
        /// </summary>
        public CommandResultCode ValidateQueueUnit(byte playerSlot, uint buildingRaw, ushort unitDefId, ushort count)
        {
            if (!SimDefinitions.TryGetUnit(unitDefId, out SimUnitDefinition def))
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            if (!IsOwnedCompletedProducer(playerSlot, buildingRaw, def.ProducerRole))
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            if (def.Tier >= 2 && !_construction.IsT2Unlocked(playerSlot))
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            ProducerRow row = FindRow(buildingRaw);
            if (row != null && row.EntryCount >= MaxQueueEntries)
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            if (row == null && FreeRowIndex() < 0)
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            long cost = (long)def.CostAE * count;
            if (_economy.GetPlayerEconomy(playerSlot).AetheriumCredits < cost)
            {
                return CommandResultCode.RejectedInsufficientResources;
            }
            return CommandResultCode.Applied;
        }

        /// <summary>CancelProduction legality: an own producer row with a queue entry at the given index.</summary>
        public CommandResultCode ValidateCancelProduction(byte playerSlot, uint buildingRaw, int queueIndex)
        {
            ProducerRow row = FindRow(buildingRaw);
            if (row == null || queueIndex < 0 || queueIndex >= row.EntryCount)
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            return OwnsBuilding(playerSlot, buildingRaw)
                ? CommandResultCode.Applied
                : CommandResultCode.RejectedInvalidTarget;
        }

        /// <summary>
        /// SetRallyPoint legality: the building must be an own completed
        /// placement with a producer role, and the rally target (SimFixed
        /// world coordinates, grid-mapped by floor) must lie inside the map
        /// (0 &lt;= x,y &lt; <see cref="Construction.ConstructionSystem.GridSize"/>
        /// in grid cells) — an off-map target is RejectedInvalidTarget and
        /// leaves the existing rally point unchanged.
        /// </summary>
        public CommandResultCode ValidateSetRallyPoint(byte playerSlot, uint buildingRaw, SimFixed targetX, SimFixed targetY)
        {
            EntityId id = UnitCommandStateView.ToEntityId(buildingRaw);
            if (!_entityManager.TryGetUnit(id, out UnitState unit) || unit.PlayerId != playerSlot)
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            if (!IsProducerRole(unit.Role) || !IsCompletedPlacement(buildingRaw))
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            int gridX = SimFixed.WorldToGrid(targetX);
            int gridY = SimFixed.WorldToGrid(targetY);
            if (gridX < 0 || gridY < 0
                || gridX >= Construction.ConstructionSystem.GridSize
                || gridY >= Construction.ConstructionSystem.GridSize)
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            return CommandResultCode.Applied;
        }

        // ------------------------------------------------------------------
        // Application (command view Apply path and programmatic callers)
        // ------------------------------------------------------------------

        /// <summary>
        /// Programmatic enqueue (command Apply path, AI): validates exactly as
        /// <see cref="ValidateQueueUnit"/>, charges CostAE x Count and appends
        /// one entry. Returns false without mutating when any check fails.
        /// </summary>
        public bool TryQueueUnit(byte playerSlot, uint buildingRaw, ushort unitDefId, ushort count)
        {
            if (count == 0) return false;
            if (ValidateQueueUnit(playerSlot, buildingRaw, unitDefId, count) != CommandResultCode.Applied)
            {
                return false;
            }
            SimDefinitions.TryGetUnit(unitDefId, out SimUnitDefinition def);
            if (!_economy.GetPlayerEconomy(playerSlot).TrySpendCredits((long)def.CostAE * count))
            {
                return false;
            }
            ProducerRow row = GetOrCreateRow(buildingRaw);
            row.Entries[row.EntryCount++] = new QueueEntry
            {
                UnitDefId = unitDefId,
                RemainingCount = count,
                ProgressRaw = 0,
            };
            return true;
        }

        /// <summary>
        /// Cancels one queue entry: refunds CostAE x its remaining unit count
        /// in full and removes the entry (the cancel action itself is free).
        /// Returns false when no entry exists at the index.
        /// </summary>
        public bool CancelProduction(uint buildingRaw, int queueIndex)
        {
            ProducerRow row = FindRow(buildingRaw);
            if (row == null || queueIndex < 0 || queueIndex >= row.EntryCount) return false;

            SimDefinitions.TryGetUnit(row.Entries[queueIndex].UnitDefId, out SimUnitDefinition def);
            EntityId id = UnitCommandStateView.ToEntityId(buildingRaw);
            if (_entityManager.TryGetUnit(id, out UnitState building))
            {
                _economy.GetPlayerEconomy(building.PlayerId)
                    .AddCredits((long)def.CostAE * row.Entries[queueIndex].RemainingCount);
            }
            RemoveEntry(row, queueIndex);
            return true;
        }

        /// <summary>Sets a producer building's rally point (Q16.16 world target). Assumes validated input.</summary>
        public void SetRallyPoint(uint buildingRaw, SimFixed targetX, SimFixed targetY)
        {
            ProducerRow row = GetOrCreateRow(buildingRaw);
            row.RallyXRaw = targetX.RawValue;
            row.RallyYRaw = targetY.RawValue;
        }

        // ------------------------------------------------------------------
        // Tick (phase 4, after construction, before movement)
        // ------------------------------------------------------------------

        /// <summary>
        /// Phase 4: drops rows whose building died (queue lost without
        /// refund), then progresses every producer's first entry by the
        /// owner's exact Q16.16 speed multiplier and spawns finished units at
        /// the rally point — in strict ascending row order.
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            for (int i = 0; i < MaxProducers; i++)
            {
                ProducerRow row = _producers[i];
                if (!row.IsActive) continue;

                EntityId buildingId = UnitCommandStateView.ToEntityId(row.RawEntityId);
                if (!_entityManager.TryGetUnit(buildingId, out UnitState building))
                {
                    row.IsActive = false; // building sold/destroyed: queue lost without refund
                    row.EntryCount = 0;
                    continue;
                }
                if (row.EntryCount == 0) continue;

                ref QueueEntry entry = ref row.Entries[0];
                SimDefinitions.TryGetUnit(entry.UnitDefId, out SimUnitDefinition def);
                int thresholdRaw = def.BuildTicks << 16;

                ref readonly PlayerEconomyState eco = ref _economy.GetPlayerEconomy(building.PlayerId);
                entry.ProgressRaw += eco.ProductionSpeedMultiplierQ16.RawValue;

                while (entry.RemainingCount > 0 && entry.ProgressRaw >= thresholdRaw)
                {
                    if (_entityManager.ActiveCount >= _entityManager.Capacity)
                    {
                        // Entity store full (MS-1 cap): the queue pauses with
                        // the finished unit held at the threshold.
                        entry.ProgressRaw = thresholdRaw;
                        break;
                    }
                    if (!TryFindSpawnCell(row, out int cellX, out int cellY))
                    {
                        // No free cell inside the search range: same documented pause.
                        entry.ProgressRaw = thresholdRaw;
                        break;
                    }

                    _entityManager.SpawnUnit(
                        building.PlayerId,
                        new Transform2D(SimFixed.FromInt(cellX), SimFixed.FromInt(cellY)),
                        def.MoveSpeed,
                        maxHealth: def.MaxHealth,
                        role: def.Role);
                    entry.RemainingCount--;
                    entry.ProgressRaw -= thresholdRaw;
                }

                if (entry.RemainingCount == 0)
                {
                    RemoveEntry(row, 0);
                }
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        /// <summary>
        /// Spawn cell search (documented deterministic algorithm): the rally
        /// cell first, then expanding Chebyshev rings 1..SpawnSearchMaxRing,
        /// within a ring ascending (y, x); a cell is free when no placement
        /// footprint covers it.
        /// </summary>
        private bool TryFindSpawnCell(ProducerRow row, out int cellX, out int cellY)
        {
            int rallyCellX = Math.Max(0, SimFixed.WorldToGrid(SimFixed.FromRaw(row.RallyXRaw)));
            int rallyCellY = Math.Max(0, SimFixed.WorldToGrid(SimFixed.FromRaw(row.RallyYRaw)));

            for (int ring = 0; ring <= SpawnSearchMaxRing; ring++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                        int x = rallyCellX + dx;
                        int y = rallyCellY + dy;
                        if (_construction.IsCellFree(x, y))
                        {
                            cellX = x;
                            cellY = y;
                            return true;
                        }
                    }
                }
            }
            cellX = 0;
            cellY = 0;
            return false;
        }

        private bool IsOwnedCompletedProducer(byte playerSlot, uint buildingRaw, UnitRole producerRole)
        {
            EntityId id = UnitCommandStateView.ToEntityId(buildingRaw);
            if (!_entityManager.TryGetUnit(id, out UnitState unit)) return false;
            if (unit.PlayerId != playerSlot || unit.Role != producerRole) return false;
            return IsCompletedPlacement(buildingRaw);
        }

        private bool IsCompletedPlacement(uint buildingRaw)
        {
            // A producer must be a COMPLETED placement of the construction
            // domain (never a site): the construction system tracks exactly
            // those entities in its placements table.
            return _construction.IsCompletedPlacement(buildingRaw);
        }

        private static bool IsProducerRole(UnitRole role)
        {
            return role == UnitRole.HQ || role == UnitRole.Barracks || role == UnitRole.VehicleFactory;
        }

        private bool OwnsBuilding(byte playerSlot, uint buildingRaw)
        {
            EntityId id = UnitCommandStateView.ToEntityId(buildingRaw);
            return _entityManager.TryGetUnit(id, out UnitState unit) && unit.PlayerId == playerSlot;
        }

        private ProducerRow FindRow(uint rawEntityId)
        {
            for (int i = 0; i < MaxProducers; i++)
            {
                if (_producers[i].IsActive && _producers[i].RawEntityId == rawEntityId) return _producers[i];
            }
            return null;
        }

        private int FreeRowIndex()
        {
            for (int i = 0; i < MaxProducers; i++) if (!_producers[i].IsActive) return i;
            return -1;
        }

        /// <summary>
        /// Returns the building's row, creating it (with the documented
        /// default rally point two cells east of the building's center cell)
        /// when missing.
        /// </summary>
        private ProducerRow GetOrCreateRow(uint rawEntityId)
        {
            ProducerRow row = FindRow(rawEntityId);
            if (row != null) return row;

            int index = FreeRowIndex();
            row = _producers[index];
            row.IsActive = true;
            row.RawEntityId = rawEntityId;
            row.EntryCount = 0;

            int rallyX = 0;
            int rallyY = 0;
            EntityId id = UnitCommandStateView.ToEntityId(rawEntityId);
            if (_entityManager.TryGetUnit(id, out UnitState building))
            {
                rallyX = Math.Max(0, SimFixed.WorldToGrid(building.Transform.PositionX)) + 2;
                rallyY = Math.Max(0, SimFixed.WorldToGrid(building.Transform.PositionY));
            }
            row.RallyXRaw = SimFixed.FromInt(rallyX).RawValue;
            row.RallyYRaw = SimFixed.FromInt(rallyY).RawValue;
            return row;
        }

        private static void RemoveEntry(ProducerRow row, int index)
        {
            for (int i = index; i < row.EntryCount - 1; i++)
            {
                row.Entries[i] = row.Entries[i + 1];
            }
            row.Entries[row.EntryCount - 1] = default;
            row.EntryCount--;
        }

        // ------------------------------------------------------------------
        // Snapshot block 106 (v1)
        // ------------------------------------------------------------------

        /// <summary>
        /// Block content v1: version, then the producer rows in ascending
        /// table order — building entity, rally point (Q16.16 raw) and per
        /// queue entry the unit definition, remaining count and fixed-point
        /// progress. Hash-sensitive by construction.
        /// </summary>
        public void WriteState(SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);

            int rowCount = 0;
            for (int i = 0; i < MaxProducers; i++) if (_producers[i].IsActive) rowCount++;
            writer.WriteUInt16(unchecked((ushort)rowCount));
            for (int i = 0; i < MaxProducers; i++)
            {
                ProducerRow row = _producers[i];
                if (!row.IsActive) continue;
                writer.WriteUInt32(row.RawEntityId);
                writer.WriteInt32(row.RallyXRaw);
                writer.WriteInt32(row.RallyYRaw);
                writer.WriteUInt8(unchecked((byte)row.EntryCount));
                for (int e = 0; e < row.EntryCount; e++)
                {
                    writer.WriteUInt16(row.Entries[e].UnitDefId);
                    writer.WriteUInt16(row.Entries[e].RemainingCount);
                    writer.WriteInt32(row.Entries[e].ProgressRaw);
                }
            }
        }

        /// <summary>Fully validates a production block without mutating the system.</summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _);
        }

        /// <summary>
        /// Restores producer rows; malformed input returns false and leaves
        /// the system untouched (two-phase contract of
        /// <see cref="IStatefulSimSystem"/>).
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out ProducerRow[] parsed)) return false;

            for (int i = 0; i < MaxProducers; i++)
            {
                ProducerRow source = parsed[i];
                ProducerRow target = _producers[i];
                target.IsActive = source.IsActive;
                target.RawEntityId = source.RawEntityId;
                target.RallyXRaw = source.RallyXRaw;
                target.RallyYRaw = source.RallyYRaw;
                target.EntryCount = source.EntryCount;
                Array.Copy(source.Entries, target.Entries, MaxQueueEntries);
            }
            return true;
        }

        /// <summary>
        /// Parses and fully validates block content — exact lengths,
        /// capacities, known definitions, progress bounds, canonical and
        /// unique producer entity ids — into commit-ready rows. Never
        /// mutates this system.
        /// </summary>
        private bool TryParseState(ReadOnlySpan<byte> blockContent, out ProducerRow[] parsed)
        {
            parsed = null;
            var reader = new SnapshotBlockReader(blockContent);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt16(out ushort rowCount) || rowCount > MaxProducers) return false;

            var rows = new ProducerRow[MaxProducers];
            for (int i = 0; i < MaxProducers; i++) rows[i] = new ProducerRow();

            for (int i = 0; i < rowCount; i++)
            {
                if (!reader.TryReadUInt32(out uint rawEntityId)) return false;
                if (!CommandIds.IsCanonicalEntityId(rawEntityId)) return false;
                if (!reader.TryReadInt32(out int rallyXRaw)) return false;
                if (!reader.TryReadInt32(out int rallyYRaw)) return false;
                if (!reader.TryReadUInt8(out byte entryCount) || entryCount > MaxQueueEntries) return false;

                for (int j = 0; j < i; j++)
                {
                    if (rows[j].RawEntityId == rawEntityId) return false; // duplicate producer
                }

                ProducerRow row = rows[i];
                row.IsActive = true;
                row.RawEntityId = rawEntityId;
                row.RallyXRaw = rallyXRaw;
                row.RallyYRaw = rallyYRaw;
                row.EntryCount = entryCount;

                for (int e = 0; e < entryCount; e++)
                {
                    if (!reader.TryReadUInt16(out ushort unitDefId)) return false;
                    if (!SimDefinitions.TryGetUnit(unitDefId, out SimUnitDefinition def)) return false;
                    if (!reader.TryReadUInt16(out ushort remainingCount) || remainingCount == 0) return false;
                    if (!reader.TryReadInt32(out int progressRaw)) return false;
                    if (progressRaw < 0 || progressRaw > (def.BuildTicks << 16)) return false;
                    row.Entries[e] = new QueueEntry
                    {
                        UnitDefId = unitDefId,
                        RemainingCount = remainingCount,
                        ProgressRaw = progressRaw,
                    };
                }
            }
            if (reader.Remaining != 0) return false;

            parsed = rows;
            return true;
        }
    }
}
