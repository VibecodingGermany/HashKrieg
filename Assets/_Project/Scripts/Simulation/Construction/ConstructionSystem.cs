using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Construction
{
    /// <summary>
    /// Canonical construction system of the MS-1 production/construction
    /// slice (docs/tech/SimulationCore.md section 2, phases 4 and 5):
    /// building placement with state-dependent validation, construction
    /// sites progressed by an assigned Builder, completion into building-role
    /// entities, cancel/sell refunds, repair orders and the per-slot T2
    /// unlock. Deterministic, pure integer/fixed-point, zero engine
    /// dependencies. Replaces the unregistered prototype scaffolding
    /// (pre-G1 reset; the retired ConstructionGrid is folded into this
    /// system as a derived occupancy cache).
    /// <para>
    /// Tick order: the system covers phase 4 (and owns the phase-5 T2 flag)
    /// and is registered AFTER the economy (phases 2/3) and BEFORE
    /// pathfinding/movement (phase 6). Consequences, stated explicitly:
    /// the low-power multiplier a site or repair reads inside one tick is
    /// the balance recomputed by the economy in that same tick, and a
    /// building completing in tick T feeds its power into the economy
    /// recompute of tick T+1 (economy runs before construction within a
    /// tick).
    /// </para>
    /// <para>
    /// Placement (PlaceBuilding): the full cost is charged up front via
    /// <see cref="PlayerEconomyState.TrySpendCredits"/> (a refusal rejects
    /// the command with RejectedInsufficientResources before anything
    /// mutates). The state-dependent validation order is fixed and
    /// deterministic: unknown/foreign definition; footprint outside the
    /// 128x128 grid or occupied cells; then D-104 terrain, influence,
    /// building-clearance and Aetherium-field geometry (all
    /// RejectedInvalidTarget); then missing prerequisite roles, the power rule
    /// and site capacity (RejectedPrerequisitesNotMet). The fine-grained
    /// FIRST failure of the same walk is readable through
    /// <see cref="GetPlacementDenial"/> (issue #135): the frozen schema-v1
    /// result codes stay shared between several causes, so the reason the
    /// HUD words travels outside the command stream.
    /// The power rule: a building with PowerRequired &gt; 0 may only be
    /// placed while the owner's last committed balance
    /// (previous tick's phase-2 recompute) covers the additional draw:
    /// PowerProvided - PowerRequired &gt;= PowerRequired of the new
    /// building. The Refinery carries NO prerequisite since D-077 (the
    /// classic loop start — quality/content/mvp-v1.json
    /// startStatePerPlayer): placed through the command path it is gated by
    /// funds, D-104 geometry and the power rule. D-103 represents every other
    /// prerequisite as a fail-closed all-of mask over completed own roles (for
    /// example Barracks needs HQ + Power, VehicleFactory needs Refinery +
    /// Barracks, and Radar needs Power + Barracks).
    /// <see cref="PlaceCompletedBuilding"/> bypasses gameplay placement
    /// validation entirely, including D-104 geometry — it is the direct write
    /// deterministic match setup uses.
    /// </para>
    /// <para>
    /// Same-tick power stacking (documented, same precedent as the combat
    /// duel asymmetry): the power rule reads the owner's last COMMITTED
    /// balance (previous tick's phase-2 recompute), so several power-drawing
    /// placements applied in the same tick can collectively oversubscribe
    /// the grid — each one validates against the same stale balance. The
    /// overshoot is deterministic and self-punishing via the low-power
    /// multiplier from the next recompute on; a per-tick placement limit is
    /// a registered Q-040 candidate.
    /// </para>
    /// <para>
    /// Footprint sweep timing (documented, Q-040 candidate): the footprint
    /// of a combat-destroyed placement is freed by this system's sweep in
    /// the NEXT tick (phase 8 combat runs after phase 4), so a PlaceBuilding
    /// applied in the tick immediately after the destruction can still find
    /// the cell occupied — deterministic, and it resolves itself one tick
    /// later.
    /// </para>
    /// <para>
    /// Sites: a site is a live entity carrying its DEFINITION role (since
    /// 16.3, #44 — the generic <see cref="UnitRole.Unit"/> slot was armed by
    /// the weapon-table fallback, so every site shot). Combat also consults
    /// <see cref="IsActiveSite"/> explicitly: even a DefensePlatform site is
    /// neither an attacker nor a target before completion. The entity sits
    /// at the footprint center with 1 HP of its definition's
    /// MaxHealth — construction HP interpolation is deliberately NOT
    /// modeled (provisional, Q-040 candidate). The readers that resolved a
    /// site by its old generic role are compensated at the source: the
    /// economy's power recompute skips sites through the bound
    /// <see cref="IsActiveSite"/> lookup, and the view layer maps sites back
    /// to the site look until completion. Builder assignment: the
    /// PlaceBuilding payload names no builder, so the site auto-assigns the
    /// own Builder with the lowest entity index (ascending-index scan,
    /// deterministic); when the assigned builder dies the next tick
    /// re-assigns the same way, and with no own Builder alive the site
    /// pauses. A tick of progress happens only while the assigned builder
    /// stands in reach — its grid cell within Chebyshev distance &lt;= 1 of
    /// the 3x3 footprint rectangle (same documented reach rule as the
    /// economy). Progress accumulates in exact Q16.16:
    /// <see cref="PlayerEconomyState.ProductionSpeedMultiplierQ16"/> raw per
    /// progressed tick (1.0 at full power, exactly 0.5 under low power —
    /// no rounding, 0.5 is exact in Q16.16, so low power means exactly one
    /// tick of progress per two ticks). Completion restores full HP (the
    /// role is already the definition's) and — for a ResearchLab — sets the
    /// owner's T2 unlock (phase 5;
    /// mvp-v1.json technology.researchLabCompletionUnlocksTier2; there are
    /// no research upgrades, no research queue and no tier 3 in MS-1).
    /// A site missing from the entity store is swept without refund; active
    /// sites themselves are excluded as combat targets since 16.3 (#44).
    /// </para>
    /// <para>
    /// Cancel/sell/repair (documented provisional economy rules, Q-040
    /// candidates): CancelConstruction forfeits all progress and refunds
    /// 75% of the cost (floor); Sell on a COMPLETED building refunds 50%
    /// (floor) and despawns it; a running production queue on a sold or
    /// destroyed building is lost without refund (production domain).
    /// Repair assigns a Builder a standing repair order on an own completed
    /// damaged building. In reach (same Chebyshev rule) the target gains
    /// <see cref="RepairRateHpPerTick"/> HP per tick, or exactly
    /// <see cref="LowPowerRepairRateHpPerTick"/> under low power, and pays an
    /// exact cumulative integer share of 30% of its new price. At most one
    /// reachable Builder may repair a target per tick; out-of-reach orders are
    /// HELD, never dropped, and Stop clears them.
    /// </para>
    /// <para>
    /// State (snapshot block <see cref="SnapshotBlockIds.Construction"/>,
    /// v1): the T2 unlock bitmask, every site (definition, origin, site
    /// entity, assigned builder, fixed-point progress), every completed
    /// placement (definition, origin, entity) and every standing repair
    /// order. The per-slot T2 flag lives here (not in the economy block)
    /// because the ResearchLab completion that sets it is a construction
    /// event — documented placement decision of this slice. The 128x128
    /// occupancy grid is a DERIVED cache rebuilt from the placements on
    /// restore (SimulationCore.md section 3: derived caches may be absent
    /// when a test proves an identical rebuild); hit points stay with the
    /// entities (entity store block) — exactly one home per value.
    /// </para>
    /// <para>
    /// Footprints are impassable terrain (sprint Truppenführung): when the
    /// host wires the pathfinding <see cref="CostField"/> (optional
    /// constructor argument — the canonical hosts do), every footprint
    /// change is mirrored into it as <see cref="CostField.ImpassableCost"/>/
    /// <see cref="CostField.OpenCost"/> writes, and placing a footprint onto
    /// mobile units pushes them onto the nearest free cell (restore skips
    /// the push-out: the entity store restores afterwards, so the snapshot's
    /// own units already satisfy the invariant).
    /// </para>
    /// </summary>
    public sealed class ConstructionSystem : IStatefulSimSystem
    {
        /// <summary>Serialization version of the construction snapshot block.</summary>
        public const byte StateVersion = 1;

        /// <summary>MS-1 construction grid edge length in cells (SimulationCore.md section 10).</summary>
        public const int GridSize = 128;

        /// <summary>Format capacity for concurrent construction sites.</summary>
        public const int MaxSites = 64;

        /// <summary>Format capacity for completed building placements.</summary>
        public const int MaxBuildings = 256;

        /// <summary>Format capacity for standing repair orders.</summary>
        public const int MaxRepairOrders = 64;

        /// <summary>Maximum Chebyshev ring of the push-out cell search around a displaced unit.</summary>
        public const int PushOutMaxRing = 8;

        /// <summary>Refund percentage of CancelConstruction (provisional, Q-040 candidate; integer floor).</summary>
        public const int CancelRefundPercent = 75;

        /// <summary>Refund percentage of Sell on a completed building (provisional, Q-040 candidate; integer floor).</summary>
        public const int SellRefundPercent = 50;

        /// <summary>Provisional repair rate in HP per tick per repairing Builder (Q-040 candidate).</summary>
        public const int RepairRateHpPerTick = 10;

        /// <summary>Full rebuild-equivalent repair price as a percentage of the building's new price (D-104).</summary>
        public const int RepairCostPercent = 30;

        /// <summary>
        /// Maximum footprint-aware Chebyshev distance from an own construction
        /// anchor (D-104). Under the corrected D-108 every own, living and
        /// COMPLETED building is an anchor and pushes the build zone outward
        /// by its own radius — construction sites never do (see
        /// IsInsideBuildInfluence). Deliberately accepted (D-108): a chain of
        /// cheap buildings can push the zone across the map; expansion is
        /// meant to be coupled to the economy.
        /// </summary>
        public const int BuildInfluenceRadiusCells = 8;

        /// <summary>Minimum footprint-aware Chebyshev distance between construction footprints (D-104).</summary>
        public const int MinimumBuildingDistanceCells = 2;

        /// <summary>Allowed field-distance interval for a Refinery footprint (D-104).</summary>
        public const int RefineryMinimumFieldDistanceCells = 1;
        public const int RefineryMaximumFieldDistanceCells = 3;

        /// <summary>Minimum field distance for every non-Refinery building footprint (D-104).</summary>
        public const int MinimumNonRefineryFieldDistanceCells = 2;

        /// <summary>Repair rate in HP per tick while the owner's grid is in LOW POWER (C4, Sprint 16.6).</summary>
        public const int LowPowerRepairRateHpPerTick = 5;

        private struct SiteState
        {
            public bool IsActive;
            public ushort BuildingDefId;
            public ushort OriginX;
            public ushort OriginY;
            public uint RawEntityId;
            public uint AssignedBuilderRaw; // 0 = none assigned
            public int ProgressRaw;         // Q16.16 ticks progressed
        }

        private struct PlacementState
        {
            public bool IsActive;
            public ushort BuildingDefId;
            public ushort OriginX;
            public ushort OriginY;
            public uint RawEntityId;
        }

        private struct RepairOrderState
        {
            public bool IsActive;
            public uint BuilderRaw;
            public uint TargetRaw;
        }

        private readonly EntityManager _entityManager;
        private readonly EconomySystem _economy;
        private readonly SiteState[] _sites;
        private readonly PlacementState[] _buildings;
        private readonly RepairOrderState[] _repairs;
        private readonly bool[] _t2Unlocked;
        private INovaLogger _logger = NullNovaLogger.Instance;

        // Derived occupancy cache (rebuilt from the placements on restore).
        private readonly byte[] _occupied;

        // Pathfinding cost field the footprints are mirrored into (optional:
        // hosts without movement — pure economy/construction test rigs —
        // leave it null and footprints simply do not block pathing there).
        private readonly CostField _costField;

        public string Name => "ConstructionSystem";

        public ushort StateBlockId => SnapshotBlockIds.Construction;

        public ConstructionSystem(EntityManager entityManager, EconomySystem economy, CostField costField = null)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _sites = new SiteState[MaxSites];
            _buildings = new PlacementState[MaxBuildings];
            _repairs = new RepairOrderState[MaxRepairOrders];
            _t2Unlocked = new bool[EconomySystem.MaxPlayers];
            _occupied = new byte[GridSize * GridSize];
            _costField = costField;
            // 16.3 (#44): a site carries its definition role, so the power
            // and capacity scans can no longer skip sites by role. Both use
            // this authoritative register; binding here means no host can
            // forget the dependency.
            _economy.BindSiteLookup(IsActiveSite);
        }

        public void Initialize(SimulationKernel kernel)
        {
            _logger = kernel?.Logger ?? NullNovaLogger.Instance;
            _logger.LogInfo(
                $"[{Name}] Initialized canonical construction ({GridSize}x{GridSize} grid, {MaxSites} sites, {MaxBuildings} placements).");
        }

        public void Shutdown()
        {
        }

        // ------------------------------------------------------------------
        // Read-only queries
        // ------------------------------------------------------------------

        /// <summary>True when the slot completed a ResearchLab (phase-5 T2 unlock; mvp-v1.json technology model).</summary>
        public bool IsT2Unlocked(byte playerSlot)
        {
            return playerSlot < EconomySystem.MaxPlayers && _t2Unlocked[playerSlot];
        }

        /// <summary>True when the grid cell is inside the map and not covered by any placement footprint.</summary>
        public bool IsCellFree(int x, int y)
        {
            if (x < 0 || y < 0 || x >= GridSize || y >= GridSize) return false;
            return _occupied[y * GridSize + x] == 0;
        }

        /// <summary>Number of active construction sites.</summary>
        public int SiteCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxSites; i++) if (_sites[i].IsActive) count++;
                return count;
            }
        }

        /// <summary>Number of completed building placements.</summary>
        public int BuildingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxBuildings; i++) if (_buildings[i].IsActive) count++;
                return count;
            }
        }

        /// <summary>Read-only snapshot of one site's state for tests and diagnostics.</summary>
        public bool TryGetSite(uint rawEntityId, out ushort buildingDefId, out int progressRaw, out uint assignedBuilderRaw)
        {
            int index = IndexOfSite(rawEntityId);
            if (index >= 0)
            {
                buildingDefId = _sites[index].BuildingDefId;
                progressRaw = _sites[index].ProgressRaw;
                assignedBuilderRaw = _sites[index].AssignedBuilderRaw;
                return true;
            }
            buildingDefId = 0;
            progressRaw = 0;
            assignedBuilderRaw = 0;
            return false;
        }

        /// <summary>True when the entity is a COMPLETED building placement tracked by this system (never a site).</summary>
        public bool IsCompletedPlacement(uint rawEntityId)
        {
            return IndexOfBuilding(rawEntityId) >= 0;
        }

        /// <summary>
        /// True while the entity is an unfinished site (16.3, #44: sites now
        /// carry their definition role, so role alone no longer tells a site
        /// apart). Bound into the economy's power and capacity scans via
        /// <see cref="EconomySystem.BindSiteLookup"/>; also the read the
        /// presentation layer needs to keep the site look until completion.
        /// </summary>
        public bool IsActiveSite(EntityId id)
        {
            return IndexOfSite(UnitCommandStateView.ToRawEntityId(id)) >= 0;
        }

        /// <summary>
        /// Missing completed own building roles for an all-of prerequisite.
        /// Unknown bits stay missing (fail closed).
        /// </summary>
        public UnitRoleMask GetMissingPrerequisiteRoles(byte playerSlot, UnitRoleMask requiredRoles)
        {
            if (requiredRoles == UnitRoleMask.None) return UnitRoleMask.None;

            UnitRoleMask completedRoles = UnitRoleMask.None;
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (!_buildings[i].IsActive) continue;
                if (!SimDefinitions.TryGetBuilding(_buildings[i].BuildingDefId, out SimBuildingDefinition def)) continue;

                EntityId id = UnitCommandStateView.ToEntityId(_buildings[i].RawEntityId);
                if (!_entityManager.TryGetUnit(id, out UnitState unit) || unit.PlayerId != playerSlot) continue;

                completedRoles |= RoleMask(def.Role);
            }
            return requiredRoles & ~completedRoles;
        }

        /// <summary>True when the slot owns every COMPLETED building role in the all-of mask.</summary>
        public bool HasFinishedBuildings(byte playerSlot, UnitRoleMask requiredRoles)
        {
            return GetMissingPrerequisiteRoles(playerSlot, requiredRoles) == UnitRoleMask.None;
        }

        /// <summary>True when the slot owns a COMPLETED building of the given role (prerequisite scans).</summary>
        public bool HasFinishedBuilding(byte playerSlot, UnitRole role)
        {
            UnitRoleMask roleMask = RoleMask(role);
            return roleMask != UnitRoleMask.None && HasFinishedBuildings(playerSlot, roleMask);
        }

        private static UnitRoleMask RoleMask(UnitRole role)
        {
            int bit = (int)role;
            return bit >= 0 && bit < 32
                ? (UnitRoleMask)(1u << bit)
                : UnitRoleMask.None;
        }

        // ------------------------------------------------------------------
        // State-dependent validation (no mutation; the command view calls
        // these at the target tick in the documented fixed order)
        // ------------------------------------------------------------------

        /// <summary>
        /// The specific state-dependent reason a placement is denied, in the
        /// fixed validation order of <see cref="ValidatePlacement"/> (first
        /// failure wins, exactly like the validator's short-circuit). Schema
        /// v1 deliberately keeps ONE <see cref="CommandResultCode"/> for
        /// several of these cases — the code is part of the replay and
        /// frozen — so the fine-grained reason travels OUTSIDE the command
        /// stream through this read-only enum (issue #135: "geht nicht" was
        /// indistinguishable from "the unit is broken"). The presentation
        /// layer asks <see cref="GetPlacementDenial"/> and words the reason;
        /// nothing here mutates state or the replay.
        /// </summary>
        public enum PlacementDenial
        {
            /// <summary>Every check passed; <see cref="ValidatePlacement"/> returns Applied.</summary>
            None = 0,

            /// <summary>The definition id names no building row (RejectedInvalidTarget).</summary>
            UnknownDefinition = 1,

            /// <summary>The definition id names another faction's row (RejectedInvalidTarget).</summary>
            ForeignDefinition = 2,

            /// <summary>The 3x3 footprint would leave the 128x128 grid (RejectedInvalidTarget).</summary>
            FootprintOutsideMap = 3,

            /// <summary>A footprint cell is already occupied by a placement or site (RejectedInvalidTarget).</summary>
            FootprintOccupied = 4,

            /// <summary>A footprint cell sits on impassable terrain (D-104; RejectedInvalidTarget).</summary>
            FootprintOnImpassableTerrain = 5,

            /// <summary>
            /// No own, living, COMPLETED building within
            /// <see cref="BuildInfluenceRadiusCells"/> (D-108; RejectedInvalidTarget).
            /// The build zone is anchored to finished buildings only — never
            /// to the Builder unit and never to a construction site.
            /// </summary>
            OutsideBuildInfluence = 6,

            /// <summary>Less than <see cref="MinimumBuildingDistanceCells"/> to another footprint or site, any owner (RejectedInvalidTarget).</summary>
            TooCloseToBuilding = 7,

            /// <summary>
            /// The Aetherium-field geometry fails (D-104; RejectedInvalidTarget):
            /// a Refinery needs a field at distance
            /// <see cref="RefineryMinimumFieldDistanceCells"/>..<see cref="RefineryMaximumFieldDistanceCells"/>,
            /// every other role must keep <see cref="MinimumNonRefineryFieldDistanceCells"/>.
            /// </summary>
            FieldSpacingViolated = 8,

            /// <summary>A completed own building role of the all-of prerequisite mask is missing (D-103; RejectedPrerequisitesNotMet).</summary>
            MissingPrerequisite = 9,

            /// <summary>The last committed power balance does not cover the new draw (RejectedPrerequisitesNotMet).</summary>
            InsufficientPower = 10,

            /// <summary>The site register is full (<see cref="MaxSites"/>; RejectedPrerequisitesNotMet).</summary>
            SiteCapacityReached = 11,
        }

        /// <summary>
        /// Full state-dependent placement validation in fixed order: unknown
        /// definition, foreign-faction definition, out-of-map footprint and
        /// occupied cells; then terrain, build influence, one-cell building
        /// ring and field spacing (RejectedInvalidTarget); then prerequisite
        /// roles, power rule and site capacity (RejectedPrerequisitesNotMet).
        /// Cost is the executor's separate check
        /// (RejectedInsufficientResources) and runs BEFORE this.
        /// <para>
        /// The result is the coarse schema-v1 mapping of
        /// <see cref="GetPlacementDenial"/> — this method IS that walk mapped
        /// onto <see cref="CommandResultCode"/>, so the coarse code and the
        /// fine-grained reason can never drift apart. Behavior is unchanged:
        /// the same first failure short-circuits, in the same order.
        /// </para>
        /// </summary>
        public CommandResultCode ValidatePlacement(byte playerSlot, ushort buildingDefId, int originX, int originY)
        {
            switch (GetPlacementDenial(playerSlot, buildingDefId, originX, originY))
            {
                case PlacementDenial.None:
                    return CommandResultCode.Applied;
                case PlacementDenial.MissingPrerequisite:
                case PlacementDenial.InsufficientPower:
                case PlacementDenial.SiteCapacityReached:
                    return CommandResultCode.RejectedPrerequisitesNotMet;
                default:
                    return CommandResultCode.RejectedInvalidTarget;
            }
        }

        /// <summary>
        /// The fine-grained read of <see cref="ValidatePlacement"/>: the
        /// FIRST state-dependent denial in the validator's own fixed order,
        /// or <see cref="PlacementDenial.None"/> when the placement is legal.
        /// Pure read, no mutation, no cost check (credits are the executor's
        /// separate gate, evaluated before this walk).
        /// <para>
        /// THIS METHOD IS THE REASON SURFACE, public so the presentation
        /// layer can ASK why a cell is denied instead of re-deriving the
        /// checks — the same contract that made
        /// <see cref="IsInsideBuildInfluence"/> and
        /// <see cref="HasMinimumBuildingSpacing"/> public for the build-zone
        /// overlay. It answers for a single footprint origin, so it stays
        /// cheap enough to ask per frame while the placement ghost is armed
        /// (issue #135: the HUD names the rule, the sim stays the only
        /// owner of the rule).
        /// </para>
        /// </summary>
        public PlacementDenial GetPlacementDenial(byte playerSlot, ushort buildingDefId, int originX, int originY)
        {
            if (!SimDefinitions.TryGetBuilding(buildingDefId, out SimBuildingDefinition def))
            {
                return PlacementDenial.UnknownDefinition;
            }
            if (def.Faction != _economy.GetSlotFaction(playerSlot))
            {
                // Definition ids are faction-resolved: a slot may only place
                // its own faction's rows. A foreign id is a known id naming
                // content the slot cannot build — an invalid target, exactly
                // like an unknown one.
                return PlacementDenial.ForeignDefinition;
            }
            if (!FootprintInsideMap(originX, originY))
            {
                return PlacementDenial.FootprintOutsideMap;
            }
            if (!FootprintFree(originX, originY))
            {
                return PlacementDenial.FootprintOccupied;
            }
            if (!FootprintIsWalkable(originX, originY))
            {
                return PlacementDenial.FootprintOnImpassableTerrain;
            }
            if (!IsInsideBuildInfluence(playerSlot, originX, originY))
            {
                return PlacementDenial.OutsideBuildInfluence;
            }
            if (!HasMinimumBuildingSpacing(originX, originY))
            {
                return PlacementDenial.TooCloseToBuilding;
            }
            if (!HasValidFieldSpacing(def.Role, originX, originY))
            {
                return PlacementDenial.FieldSpacingViolated;
            }
            if (!HasFinishedBuildings(playerSlot, def.PrerequisiteRoles))
            {
                return PlacementDenial.MissingPrerequisite;
            }
            ref readonly PlayerEconomyState eco = ref _economy.GetPlayerEconomy(playerSlot);
            if (def.PowerRequired > 0 && eco.PowerProvided - eco.PowerRequired < def.PowerRequired)
            {
                return PlacementDenial.InsufficientPower;
            }
            if (FreeSiteIndex() < 0)
            {
                return PlacementDenial.SiteCapacityReached;
            }
            return PlacementDenial.None;
        }

        /// <summary>CancelConstruction legality: the entity must be an active site owned by the slot.</summary>
        public CommandResultCode ValidateCancel(byte playerSlot, uint rawEntityId)
        {
            int index = IndexOfSite(rawEntityId);
            if (index < 0) return CommandResultCode.RejectedInvalidTarget;
            return OwnsEntity(playerSlot, rawEntityId)
                ? CommandResultCode.Applied
                : CommandResultCode.RejectedInvalidTarget;
        }

        /// <summary>Sell legality: the entity must be a COMPLETED building placement owned by the slot.</summary>
        public CommandResultCode ValidateSell(byte playerSlot, uint rawEntityId)
        {
            int index = IndexOfBuilding(rawEntityId);
            if (index < 0) return CommandResultCode.RejectedInvalidTarget;
            return OwnsEntity(playerSlot, rawEntityId)
                ? CommandResultCode.Applied
                : CommandResultCode.RejectedInvalidTarget;
        }

        /// <summary>
        /// Repair legality in fixed order: every actor must be a live Builder,
        /// the target must be an own completed placement that is damaged, and
        /// the repair-order capacity must absorb the new orders.
        /// </summary>
        public CommandResultCode ValidateRepair(byte playerSlot, uint[] actorRaws, uint targetRaw)
        {
            for (int i = 0; i < actorRaws.Length; i++)
            {
                EntityId id = UnitCommandStateView.ToEntityId(actorRaws[i]);
                if (!_entityManager.TryGetUnit(id, out UnitState unit) || unit.Role != UnitRole.Builder)
                {
                    return CommandResultCode.RejectedInvalidTarget;
                }
            }

            int targetIndex = IndexOfBuilding(targetRaw);
            if (targetIndex < 0 || !OwnsEntity(playerSlot, targetRaw))
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            EntityId targetId = UnitCommandStateView.ToEntityId(targetRaw);
            ref readonly UnitState target = ref _entityManager.GetUnitRef(targetId);
            if (target.CurrentHealth >= target.MaxHealth)
            {
                return CommandResultCode.RejectedInvalidTarget; // nothing to repair
            }

            int newOrders = 0;
            for (int i = 0; i < actorRaws.Length; i++)
            {
                if (IndexOfRepairByBuilder(actorRaws[i]) < 0) newOrders++;
            }
            int active = 0;
            for (int i = 0; i < MaxRepairOrders; i++) if (_repairs[i].IsActive) active++;
            if (active + newOrders > MaxRepairOrders)
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            return CommandResultCode.Applied;
        }

        // ------------------------------------------------------------------
        // Application (command view Apply path and programmatic match
        // setup / AI; every mutating entry point re-validates or documents
        // its bypass)
        // ------------------------------------------------------------------

        /// <summary>
        /// Programmatic placement (command Apply path, AI): validates exactly
        /// as <see cref="ValidatePlacement"/> plus the credit spend, then
        /// charges the full cost, occupies the footprint, spawns the site
        /// entity (definition role, 1 HP) and auto-assigns the lowest-index
        /// own Builder. Returns false without mutating when any check fails.
        /// </summary>
        public bool TryPlaceBuilding(byte playerSlot, ushort buildingDefId, int originX, int originY)
        {
            if (ValidatePlacement(playerSlot, buildingDefId, originX, originY) != CommandResultCode.Applied)
            {
                return false;
            }
            SimDefinitions.TryGetBuilding(buildingDefId, out SimBuildingDefinition def);
            ref PlayerEconomyState eco = ref _economy.GetPlayerEconomy(playerSlot);
            if (!eco.TrySpendCredits(def.CostAE))
            {
                return false;
            }
            CreateSite(playerSlot, in def, originX, originY);
            return true;
        }

        /// <summary>
        /// Match-setup placement of a COMPLETED building: bypasses cost and all
        /// gameplay placement validation by contract, including D-104 terrain,
        /// influence and spacing (callers are deterministic host content
        /// wiring and explicit test setup). Applies the same completion effects as a
        /// finished site: building-role entity at full HP, footprint occupied,
        /// ResearchLab sets the T2 unlock. Returns EntityId.Invalid when the
        /// definition is unknown, the footprint is blocked or the placement
        /// capacity is exhausted.
        /// </summary>
        public EntityId PlaceCompletedBuilding(byte playerSlot, ushort buildingDefId, int originX, int originY)
        {
            if (!SimDefinitions.TryGetBuilding(buildingDefId, out SimBuildingDefinition def)) return EntityId.Invalid;
            if (!FootprintInsideMap(originX, originY) || !FootprintFree(originX, originY)) return EntityId.Invalid;
            int slot = FreeBuildingIndex();
            if (slot < 0) return EntityId.Invalid;

            EntityId id = SpawnBuildingEntity(playerSlot, in def, originX, originY, completed: true);
            OccupyFootprint(originX, originY, pushOutUnits: true);
            _buildings[slot] = new PlacementState
            {
                IsActive = true,
                BuildingDefId = buildingDefId,
                OriginX = (ushort)originX,
                OriginY = (ushort)originY,
                RawEntityId = UnitCommandStateView.ToRawEntityId(id),
            };
            if (def.Role == UnitRole.ResearchLab && playerSlot < EconomySystem.MaxPlayers)
            {
                _t2Unlocked[playerSlot] = true;
            }
            return id;
        }

        /// <summary>
        /// Cancels a site: progress is forfeited, 75% of the cost is refunded
        /// (floor), the site entity despawns and the footprint is freed.
        /// Returns false when the entity is not an active site.
        /// </summary>
        public bool CancelConstruction(uint rawEntityId)
        {
            int index = IndexOfSite(rawEntityId);
            if (index < 0) return false;

            ref SiteState site = ref _sites[index];
            SimDefinitions.TryGetBuilding(site.BuildingDefId, out SimBuildingDefinition def);
            EntityId id = UnitCommandStateView.ToEntityId(rawEntityId);
            if (_entityManager.TryGetUnit(id, out UnitState unit))
            {
                // 16.4: refunds obey the derived ceiling too — overflow is forfeit.
                _economy.DepositCapped(unit.PlayerId, (long)def.CostAE * CancelRefundPercent / 100);
            }
            _entityManager.DespawnUnit(id);
            FreeFootprint(site.OriginX, site.OriginY);
            site.IsActive = false;
            CompactSites();
            return true;
        }

        /// <summary>
        /// Sells a COMPLETED building: refunds 50% of the cost (floor),
        /// despawns the entity and frees the footprint. A production queue on
        /// the building is lost without refund (production domain sweep).
        /// Returns false when the entity is not a completed placement.
        /// </summary>
        public bool SellBuilding(uint rawEntityId)
        {
            int index = IndexOfBuilding(rawEntityId);
            if (index < 0) return false;

            ref PlacementState placement = ref _buildings[index];
            SimDefinitions.TryGetBuilding(placement.BuildingDefId, out SimBuildingDefinition def);
            EntityId id = UnitCommandStateView.ToEntityId(rawEntityId);
            if (_entityManager.TryGetUnit(id, out UnitState unit))
            {
                // 16.4: refunds obey the derived ceiling too — overflow is forfeit.
                _economy.DepositCapped(unit.PlayerId, (long)def.CostAE * SellRefundPercent / 100);
            }
            _entityManager.DespawnUnit(id);
            FreeFootprint(placement.OriginX, placement.OriginY);
            placement.IsActive = false;
            CompactBuildings();
            return true;
        }

        /// <summary>Assigns (or overwrites) a Builder's standing repair order. Assumes validated input.</summary>
        public void AssignRepairOrder(uint builderRaw, uint targetRaw)
        {
            int index = IndexOfRepairByBuilder(builderRaw);
            if (index >= 0)
            {
                _repairs[index].TargetRaw = targetRaw;
                return;
            }
            index = FreeRepairIndex();
            if (index < 0) return; // validated callers never exceed capacity
            _repairs[index] = new RepairOrderState { IsActive = true, BuilderRaw = builderRaw, TargetRaw = targetRaw };
        }

        /// <summary>Clears a Builder's standing repair order (Stop command); a no-op when none exists.</summary>
        public void ClearRepairOrder(uint builderRaw)
        {
            int index = IndexOfRepairByBuilder(builderRaw);
            if (index >= 0)
            {
                _repairs[index].IsActive = false;
                CompactRepairOrders();
            }
        }

        // ------------------------------------------------------------------
        // Tick (phase 4, after the economy, before movement)
        // ------------------------------------------------------------------

        /// <summary>
        /// Phase 4: sweeps placements whose entity died (sites abort without
        /// refund, completed placements free their footprint), then progresses
        /// every site with an in-reach Builder by the owner's exact Q16.16
        /// speed multiplier, then processes standing repair orders — all in
        /// strict ascending table order.
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            SweepDeadPlacements();
            ProgressSites();
            ProcessRepairOrders();
        }

        private void SweepDeadPlacements()
        {
            for (int i = 0; i < MaxSites; i++)
            {
                if (!_sites[i].IsActive) continue;
                if (!_entityManager.IsValid(UnitCommandStateView.ToEntityId(_sites[i].RawEntityId)))
                {
                    FreeFootprint(_sites[i].OriginX, _sites[i].OriginY);
                    _sites[i].IsActive = false; // destroyed site: aborted, no refund
                }
            }
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (!_buildings[i].IsActive) continue;
                if (!_entityManager.IsValid(UnitCommandStateView.ToEntityId(_buildings[i].RawEntityId)))
                {
                    FreeFootprint(_buildings[i].OriginX, _buildings[i].OriginY);
                    _buildings[i].IsActive = false;
                }
            }
            CompactSites();
            CompactBuildings();
        }

        private void ProgressSites()
        {
            for (int i = 0; i < MaxSites; i++)
            {
                ref SiteState site = ref _sites[i];
                if (!site.IsActive) continue;

                EntityId siteId = UnitCommandStateView.ToEntityId(site.RawEntityId);
                if (!_entityManager.TryGetUnit(siteId, out UnitState siteUnit)) continue; // swept next tick
                SimDefinitions.TryGetBuilding(site.BuildingDefId, out SimBuildingDefinition def);

                // Builder (re-)assignment: the assigned builder must be alive,
                // still carry the Builder role and still belong to the site
                // owner (P2-2 defense-in-depth against tampered snapshots and
                // stale assignments); a dead, role-changed or foreign builder
                // is replaced by the lowest-index own Builder.
                if (site.AssignedBuilderRaw != 0)
                {
                    EntityId assignedId = UnitCommandStateView.ToEntityId(site.AssignedBuilderRaw);
                    bool usable = _entityManager.TryGetUnit(assignedId, out UnitState assigned)
                        && assigned.Role == UnitRole.Builder
                        && assigned.PlayerId == siteUnit.PlayerId;
                    if (!usable)
                    {
                        site.AssignedBuilderRaw = 0;
                    }
                }
                if (site.AssignedBuilderRaw == 0)
                {
                    site.AssignedBuilderRaw = FindLowestIndexBuilder(siteUnit.PlayerId);
                }
                if (site.AssignedBuilderRaw == 0) continue; // no own Builder: paused

                EntityId builderId = UnitCommandStateView.ToEntityId(site.AssignedBuilderRaw);
                ref readonly UnitState builder = ref _entityManager.GetUnitRef(builderId);
                if (!IsInReachOfFootprint(in builder, site.OriginX, site.OriginY)) continue; // held, not dropped

                ref readonly PlayerEconomyState eco = ref _economy.GetPlayerEconomy(siteUnit.PlayerId);
                site.ProgressRaw += eco.ProductionSpeedMultiplierQ16.RawValue;

                if (site.ProgressRaw >= (def.BuildTicks << 16))
                {
                    CompleteSite(i, in def, siteUnit.PlayerId);
                }
            }
            CompactSites();
        }

        private void CompleteSite(int siteIndex, in SimBuildingDefinition def, byte ownerSlot)
        {
            uint rawEntityId = _sites[siteIndex].RawEntityId;
            ushort originX = _sites[siteIndex].OriginX;
            ushort originY = _sites[siteIndex].OriginY;
            _sites[siteIndex].IsActive = false;

            EntityId id = UnitCommandStateView.ToEntityId(rawEntityId);
            ref UnitState unit = ref _entityManager.GetUnitRef(id);
            // New sites already carry the definition role (16.3), while an
            // active site restored from a pre-16.3 snapshot can still carry
            // UnitRole.Unit. Normalize idempotently at completion so the old
            // snapshot becomes a valid powered/producing building.
            unit.Role = def.Role;
            unit.CurrentHealth = def.MaxHealth;

            int slot = FreeBuildingIndex();
            if (slot >= 0)
            {
                _buildings[slot] = new PlacementState
                {
                    IsActive = true,
                    BuildingDefId = def.DefinitionId,
                    OriginX = originX,
                    OriginY = originY,
                    RawEntityId = rawEntityId,
                };
            }
            if (def.Role == UnitRole.ResearchLab && ownerSlot < EconomySystem.MaxPlayers)
            {
                _t2Unlocked[ownerSlot] = true; // phase 5: T2 unlock (mvp-v1.json technology model)
            }
            if (def.Role == UnitRole.Refinery)
            {
                GrantFoundingHarvester(ownerSlot, originX, originY);
            }
        }

        /// <summary>
        /// A finished Refinery hands out a Harvester for free — once per
        /// living Harvester, not per Refinery.
        /// <para>
        /// Without it the opening can dead-end: the Harvester costs 700 AE and
        /// the Refinery is its only producer, so a player who spends down below
        /// 700 before the Refinery finishes has no way left to earn anything.
        /// Nothing in the simulation recovers from that — the run is over
        /// without an opponent doing a thing. The grant is the cheapest fix
        /// that keeps the economy reachable from every spend order.
        /// </para>
        /// <para>
        /// Sprint 16.1 (#43) changed two things. First, the LATCH: the grant
        /// fires only while the owner has NO living Harvester — derived by an
        /// ascending-index scan over the unit store, never stored (a counter
        /// field would break the economy block's fixed per-slot layout). A
        /// second Refinery or a rebuild grants nothing while any own Harvester
        /// lives; losing every Harvester re-arms the grant, which is exactly
        /// the dead-end insurance it exists for. Second, the ORDER: the
        /// granted Harvester is born with a standing harvest order on the
        /// nearest field with reserve left (measured from the footprint
        /// centre), so the loop starts on its own — the economy holds the
        /// order and the client/AI escort drives the legs. Same class of
        /// direct state write as the push-out's <c>SetTarget</c>: no command
        /// record, no new command kind.
        /// </para>
        /// <para>
        /// Deterministic by construction: it runs inside the construction phase
        /// in ascending site order, picks its cell with the same ring search as
        /// the push-out, and keeps no state of its own — the spawn either
        /// happens now or not at all. Nothing here survives a tick boundary, so
        /// the snapshot layout is untouched. Every failure path logs instead of
        /// returning silently (the pre-16.1 behaviour that made a full entity
        /// store or a walled-in Refinery indistinguishable from success).
        /// </para>
        /// </summary>
        private void GrantFoundingHarvester(byte ownerSlot, int originX, int originY)
        {
            if (HasLivingHarvester(ownerSlot))
            {
                _logger.LogInfo(
                    $"[{Name}] Refinery completed for slot {ownerSlot}: founding Harvester grant latched off (an own Harvester is alive).");
                return;
            }
            if (_entityManager.ActiveCount >= _entityManager.Capacity)
            {
                _logger.LogWarn(
                    $"[{Name}] Founding Harvester grant for slot {ownerSlot} FAILED: entity store is full ({_entityManager.Capacity}).");
                return;
            }
            if (!SimDefinitions.TryGetUnit(
                    _economy.GetSlotFaction(ownerSlot), UnitRole.Harvester,
                    out SimUnitDefinition harvester))
            {
                _logger.LogWarn(
                    $"[{Name}] Founding Harvester grant for slot {ownerSlot} FAILED: no Harvester definition for faction {_economy.GetSlotFaction(ownerSlot)}.");
                return;
            }

            // Search outward from the footprint centre; ring 0 is the centre
            // cell itself, which the footprint just occupied, so the first hit
            // is always outside the building.
            int centre = SimDefinitions.BuildingFootprintCells / 2;
            if (!TryFindPushOutCell(originX + centre, originY + centre, out int cellX, out int cellY))
            {
                _logger.LogWarn(
                    $"[{Name}] Founding Harvester grant for slot {ownerSlot} FAILED: no free cell within {PushOutMaxRing} rings of the Refinery — the player buys the Harvester the normal way.");
                return;
            }

            EntityId id = _entityManager.SpawnUnit(
                ownerSlot,
                new Transform2D(SimFixed.FromInt(cellX), SimFixed.FromInt(cellY)),
                harvester.MoveSpeed,
                maxHealth: harvester.MaxHealth,
                role: harvester.Role);

            if (_economy.TryFindNearestField(originX + centre, originY + centre, out ushort fieldId))
            {
                _entityManager.GetUnitRef(id).HarvestFieldId = fieldId;
            }
            else
            {
                _logger.LogInfo(
                    $"[{Name}] Founding Harvester spawned for slot {ownerSlot} WITHOUT a field order: no field with reserve is registered.");
            }
        }

        /// <summary>True while any own living Harvester exists (ascending-index scan, same pattern as <see cref="FindLowestIndexBuilder"/>).</summary>
        private bool HasLivingHarvester(byte playerSlot)
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (unit.IsActive && unit.Role == UnitRole.Harvester && unit.PlayerId == playerSlot)
                {
                    return true;
                }
            }
            return false;
        }

        private void ProcessRepairOrders()
        {
            Span<uint> claimedTargets = stackalloc uint[MaxRepairOrders];
            Span<byte> winningOrders = stackalloc byte[MaxRepairOrders];
            winningOrders.Clear();
            int claimedTargetCount = 0;

            // Select winners from the tick-start state before any target is
            // healed. This preserves later same-target orders even when the
            // winner reaches full health, while still clearing targets that
            // were already full when the tick began.
            for (int i = 0; i < MaxRepairOrders; i++)
            {
                ref RepairOrderState order = ref _repairs[i];
                if (!order.IsActive) continue;

                EntityId builderId = UnitCommandStateView.ToEntityId(order.BuilderRaw);
                EntityId targetId = UnitCommandStateView.ToEntityId(order.TargetRaw);
                if (!_entityManager.TryGetUnit(builderId, out UnitState builder)
                    || builder.Role != UnitRole.Builder
                    || !_entityManager.TryGetUnit(targetId, out UnitState target))
                {
                    order.IsActive = false;
                    continue;
                }

                int placementIndex = IndexOfBuilding(order.TargetRaw);
                if (placementIndex < 0)
                {
                    order.IsActive = false; // target is not (or no longer) a completed placement
                    continue;
                }

                if (!SimDefinitions.TryGetBuilding(
                        _buildings[placementIndex].BuildingDefId,
                        out _))
                {
                    order.IsActive = false;
                    continue;
                }

                if (target.CurrentHealth >= target.MaxHealth)
                {
                    order.IsActive = false; // fully repaired at tick start: the order resolves
                    continue;
                }

                if (!IsInReachOfFootprint(in builder, _buildings[placementIndex].OriginX, _buildings[placementIndex].OriginY))
                {
                    continue; // held, not dropped
                }

                // Once a damaged target has been claimed this tick, later
                // reachable orders remain standing without paying or healing.
                // The claim also survives an insufficient-credit refusal so
                // order-table multiplicity can never multiply repair work.
                bool alreadyClaimed = false;
                for (int claimed = 0; claimed < claimedTargetCount; claimed++)
                {
                    if (claimedTargets[claimed] == order.TargetRaw)
                    {
                        alreadyClaimed = true;
                        break;
                    }
                }
                if (alreadyClaimed)
                {
                    continue;
                }

                claimedTargets[claimedTargetCount++] = order.TargetRaw;
                winningOrders[i] = 1;
            }

            for (int i = 0; i < MaxRepairOrders; i++)
            {
                if (winningOrders[i] == 0) continue;

                ref RepairOrderState order = ref _repairs[i];
                EntityId targetId = UnitCommandStateView.ToEntityId(order.TargetRaw);
                int placementIndex = IndexOfBuilding(order.TargetRaw);
                ref UnitState target = ref _entityManager.GetUnitRef(targetId);

                SimDefinitions.TryGetBuilding(
                    _buildings[placementIndex].BuildingDefId,
                    out SimBuildingDefinition def);

                ref PlayerEconomyState repairEco = ref _economy.GetPlayerEconomy(target.PlayerId);
                int rate = repairEco.IsLowPower
                    ? LowPowerRepairRateHpPerTick
                    : RepairRateHpPerTick;
                int healthBefore = Math.Max(0, target.CurrentHealth);
                int healthAfter = Math.Min(target.MaxHealth, healthBefore + rate);

                long fullRepairCost = (long)def.CostAE * RepairCostPercent / 100;
                long paidBefore = RepairCostAtHealth(fullRepairCost, healthBefore, target.MaxHealth);
                long paidAfter = RepairCostAtHealth(fullRepairCost, healthAfter, target.MaxHealth);
                long tickCost = paidAfter - paidBefore;

                if (tickCost > 0 && !repairEco.TrySpendCredits(tickCost))
                {
                    continue; // atomic refusal: no debit and no healing
                }

                target.CurrentHealth = healthAfter;
                if (healthAfter >= target.MaxHealth)
                {
                    order.IsActive = false;
                }
            }
            CompactRepairOrders();
        }

        private static long RepairCostAtHealth(long fullRepairCost, int health, int maxHealth)
        {
            if (fullRepairCost <= 0 || health <= 0 || maxHealth <= 0) return 0;
            if (health >= maxHealth) return fullRepairCost;
            return fullRepairCost * health / maxHealth;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void CreateSite(byte playerSlot, in SimBuildingDefinition def, int originX, int originY)
        {
            EntityId id = SpawnBuildingEntity(playerSlot, in def, originX, originY, completed: false);
            OccupyFootprint(originX, originY, pushOutUnits: true);
            int slot = FreeSiteIndex();
            _sites[slot] = new SiteState
            {
                IsActive = true,
                BuildingDefId = def.DefinitionId,
                OriginX = (ushort)originX,
                OriginY = (ushort)originY,
                RawEntityId = UnitCommandStateView.ToRawEntityId(id),
                AssignedBuilderRaw = FindLowestIndexBuilder(playerSlot),
                ProgressRaw = 0,
            };
        }

        /// <summary>
        /// Spawns the building entity at the footprint center cell. A site
        /// carries its DEFINITION role since 16.3 (#44) — the generic
        /// <see cref="UnitRole.Unit"/> slot made every site an armed combatant
        /// through the weapon-table fallback (15 damage, D-087 auto-acquires
        /// for it). Unarmed building roles carry AttackDamage 0, so the
        /// fallback shot disappears for unarmed roles; Combat additionally
        /// excludes every active site, including DefensePlatform, as attacker
        /// and target. The readers that resolved a site BY its generic role
        /// are compensated at the source: the economy's power recompute skips sites through
        /// <see cref="IsActiveSite"/> (a site neither provides nor draws),
        /// and UnitViewManager keeps the site look until completion through
        /// the same read. A completed building carries the same definition
        /// role at full HP — completion no longer mutates the role at all.
        /// </summary>
        private EntityId SpawnBuildingEntity(byte playerSlot, in SimBuildingDefinition def, int originX, int originY, bool completed)
        {
            EntityId id = _entityManager.SpawnUnit(
                playerSlot,
                new Transform2D(SimFixed.FromInt(originX + 1), SimFixed.FromInt(originY + 1)),
                SimFixed.Zero,
                maxHealth: def.MaxHealth,
                role: def.Role);
            if (!completed)
            {
                _entityManager.GetUnitRef(id).CurrentHealth = 1;
            }
            return id;
        }

        /// <summary>Lowest-index living own Builder, or 0 (ascending-index scan, deterministic).</summary>
        private uint FindLowestIndexBuilder(byte playerSlot)
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (unit.IsActive && unit.Role == UnitRole.Builder && unit.PlayerId == playerSlot)
                {
                    return UnitCommandStateView.ToRawEntityId(unit.Id);
                }
            }
            return 0;
        }

        /// <summary>Chebyshev reach rule against a 3x3 footprint rectangle: the unit's cell is the rectangle or adjacent (&lt;= 1 per axis).</summary>
        private static bool IsInReachOfFootprint(in UnitState unit, int originX, int originY)
        {
            int ux = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionX));
            int uy = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionY));
            int f = SimDefinitions.BuildingFootprintCells;
            int dx = Math.Max(0, Math.Max(originX - ux, ux - (originX + f - 1)));
            int dy = Math.Max(0, Math.Max(originY - uy, uy - (originY + f - 1)));
            return dx <= 1 && dy <= 1;
        }

        private bool OwnsEntity(byte playerSlot, uint rawEntityId)
        {
            EntityId id = UnitCommandStateView.ToEntityId(rawEntityId);
            return _entityManager.TryGetUnit(id, out UnitState unit) && unit.PlayerId == playerSlot;
        }

        private static bool FootprintInsideMap(int originX, int originY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            return originX >= 0 && originY >= 0 && originX + f <= GridSize && originY + f <= GridSize;
        }

        private bool FootprintIsWalkable(int originX, int originY)
        {
            if (_costField == null) return true;

            int f = SimDefinitions.BuildingFootprintCells;
            for (int y = originY; y < originY + f; y++)
            {
                for (int x = originX; x < originX + f; x++)
                {
                    if (!_costField.IsWalkable((ushort)x, (ushort)y)) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Corrected D-108 anchor rule: EVERY own, living and completed
        /// building extends the build zone by its own
        /// <see cref="BuildInfluenceRadiusCells"/> — no role list. A
        /// construction site is NOT an anchor even though it already carries
        /// its definition role (16.3, #44), so the finished-only rule is
        /// checked explicitly through <see cref="IsActiveSite"/>, the same
        /// register the economy's power and capacity scans use. Deliberately
        /// accepted (D-108): a chain of cheap buildings can push the build
        /// zone across the map — that coupling of expansion to the economy
        /// is the decided behavior, not a defect.
        /// <para>
        /// THIS METHOD IS THE RULE, and it is public so that the build-zone
        /// overlay can ASK it per cell instead of re-deriving the radius or
        /// the anchor list on its own. That is what let the anchor list open
        /// (D-108) without the overlay being touched at all. Pure read, no
        /// mutation — consumed by <see cref="ValidatePlacement"/>.
        /// </para>
        /// </summary>
        public bool IsInsideBuildInfluence(byte playerSlot, int originX, int originY)
        {
            for (int i = 0; i < MaxBuildings; i++)
            {
                ref readonly PlacementState placement = ref _buildings[i];
                if (!placement.IsActive) continue;

                EntityId id = UnitCommandStateView.ToEntityId(placement.RawEntityId);
                if (IsActiveSite(id)) continue; // finished only: a site never extends the zone
                if (!_entityManager.TryGetUnit(id, out UnitState unit) || unit.PlayerId != playerSlot) continue;
                if (FootprintDistance(originX, originY, placement.OriginX, placement.OriginY) <= BuildInfluenceRadiusCells)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The D-104 clearance read: true when a 3x3 footprint with its
        /// origin at (originX, originY) keeps
        /// <see cref="MinimumBuildingDistanceCells"/> from every site and
        /// every completed placement (any owner). This is the check that
        /// rejects cells INSIDE the build zone — the build-zone overlay
        /// paints exactly the pair (IsInsideBuildInfluence,
        /// HasMinimumBuildingSpacing) as its two distinguishable states.
        /// Consumed by <see cref="ValidatePlacement"/>. Pure read, no
        /// mutation.
        /// </summary>
        public bool HasMinimumBuildingSpacing(int originX, int originY)
        {
            for (int i = 0; i < MaxSites; i++)
            {
                ref readonly SiteState site = ref _sites[i];
                if (site.IsActive
                    && FootprintDistance(originX, originY, site.OriginX, site.OriginY) < MinimumBuildingDistanceCells)
                {
                    return false;
                }
            }

            for (int i = 0; i < MaxBuildings; i++)
            {
                ref readonly PlacementState placement = ref _buildings[i];
                if (!placement.IsActive) continue;
                EntityId id = UnitCommandStateView.ToEntityId(placement.RawEntityId);
                if (!_entityManager.IsValid(id)) continue;
                if (FootprintDistance(originX, originY, placement.OriginX, placement.OriginY) < MinimumBuildingDistanceCells)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The build-zone overlay's two reads, hoisted from PER TEXEL to ONE
        /// PASS (21.4 follow-up — the per-cell calls of
        /// <see cref="IsInsideBuildInfluence"/> and
        /// <see cref="HasMinimumBuildingSpacing"/> cost the overlay several
        /// hundred million operations per repaint: every texel rescanned the
        /// placement register, and each entry paid a linear site-register
        /// probe for the finished-only rule. At the 4 Hz repaint cadence
        /// while a placement ghost is armed, that was a main-thread hitch
        /// four times a second in exactly the phase the player was placing).
        /// <para>
        /// The fill is EXACT, not an approximation: both predicates are
        /// rectangle-Chebyshev distances between uniform f×f footprints, so
        /// the set of candidate origins they answer for is a square around
        /// each register entry. With RectDist(o, p) = max(0, |o−p| − (f−1))
        /// per axis: inside-influence ⟺ |o−a| ≤ BuildInfluenceRadiusCells +
        /// f − 1 per axis; spacing-blocked ⟺ |o−b| ≤
        /// MinimumBuildingDistanceCells + f − 2 per axis. The per-cell reads
        /// stay the validators' own path — this mask exists for the overlay's
        /// repaint loop only, and the edit-mode suite pins their equivalence
        /// over the full grid (BuildZoneOverlayQueryTests).
        /// </para>
        /// <para>
        /// Filters mirror the per-cell reads precisely: influence anchors are
        /// own, living, COMPLETED placements (a site never anchors — D-108);
        /// spacing blockers are every active site and every placement with a
        /// living entity, of ANY owner. Both masks are cleared first and must
        /// cover the full grid (GridSize × GridSize).
        /// </para>
        /// </summary>
        public void FillBuildZoneMasks(byte playerSlot, bool[] influenceMask, bool[] spacingBlockedMask)
        {
            int size = GridSize;
            if (influenceMask == null || influenceMask.Length < size * size)
            {
                throw new ArgumentException("influenceMask must cover the full grid", nameof(influenceMask));
            }
            if (spacingBlockedMask == null || spacingBlockedMask.Length < size * size)
            {
                throw new ArgumentException("spacingBlockedMask must cover the full grid", nameof(spacingBlockedMask));
            }
            Array.Clear(influenceMask, 0, size * size);
            Array.Clear(spacingBlockedMask, 0, size * size);

            int f = SimDefinitions.BuildingFootprintCells;
            int influenceHalf = BuildInfluenceRadiusCells + f - 1;
            int spacingHalf = MinimumBuildingDistanceCells + f - 2;

            for (int i = 0; i < MaxSites; i++)
            {
                ref readonly SiteState site = ref _sites[i];
                if (site.IsActive)
                {
                    FillOriginBox(spacingBlockedMask, size, site.OriginX, site.OriginY, spacingHalf);
                }
            }

            for (int i = 0; i < MaxBuildings; i++)
            {
                ref readonly PlacementState placement = ref _buildings[i];
                if (!placement.IsActive) continue;
                EntityId id = UnitCommandStateView.ToEntityId(placement.RawEntityId);
                if (_entityManager.IsValid(id))
                {
                    FillOriginBox(spacingBlockedMask, size, placement.OriginX, placement.OriginY, spacingHalf);
                }

                if (IsActiveSite(id)) continue; // finished only: a site never extends the zone
                if (!_entityManager.TryGetUnit(id, out UnitState unit) || unit.PlayerId != playerSlot) continue;
                FillOriginBox(influenceMask, size, placement.OriginX, placement.OriginY, influenceHalf);
            }
        }

        /// <summary>Marks the clamped square of candidate origins (cx±half, cy±half) in a row-major grid mask.</summary>
        private static void FillOriginBox(bool[] mask, int size, int centreX, int centreY, int half)
        {
            int minX = Math.Max(0, centreX - half);
            int maxX = Math.Min(size - 1, centreX + half);
            int minY = Math.Max(0, centreY - half);
            int maxY = Math.Min(size - 1, centreY + half);
            for (int y = minY; y <= maxY; y++)
            {
                int row = y * size;
                for (int x = minX; x <= maxX; x++)
                {
                    mask[row + x] = true;
                }
            }
        }

        private bool HasValidFieldSpacing(UnitRole role, int originX, int originY)
        {
            bool refineryHasFieldInRange = false;
            for (int i = 0; i < _economy.FieldCount; i++)
            {
                if (!_economy.TryGetFieldAtIndex(i, out AetheriumField field)) return false;
                int distance = PointToFootprintDistance(field.GridPos.X, field.GridPos.Y, originX, originY);
                if (distance == 0) return false;

                if (role == UnitRole.Refinery)
                {
                    if (distance >= RefineryMinimumFieldDistanceCells
                        && distance <= RefineryMaximumFieldDistanceCells)
                    {
                        refineryHasFieldInRange = true;
                    }
                }
                else if (distance < MinimumNonRefineryFieldDistanceCells)
                {
                    return false;
                }
            }

            return role != UnitRole.Refinery || refineryHasFieldInRange;
        }

        private static int PointToFootprintDistance(int pointX, int pointY, int originX, int originY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            return RectangleDistance(
                pointX, pointY, pointX, pointY,
                originX, originY, originX + f - 1, originY + f - 1);
        }

        private static int FootprintDistance(int leftOriginX, int leftOriginY, int rightOriginX, int rightOriginY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            return RectangleDistance(
                leftOriginX, leftOriginY, leftOriginX + f - 1, leftOriginY + f - 1,
                rightOriginX, rightOriginY, rightOriginX + f - 1, rightOriginY + f - 1);
        }

        private static int RectangleDistance(
            int leftMinX, int leftMinY, int leftMaxX, int leftMaxY,
            int rightMinX, int rightMinY, int rightMaxX, int rightMaxY)
        {
            int dx = leftMaxX < rightMinX
                ? rightMinX - leftMaxX
                : rightMaxX < leftMinX ? leftMinX - rightMaxX : 0;
            int dy = leftMaxY < rightMinY
                ? rightMinY - leftMaxY
                : rightMaxY < leftMinY ? leftMinY - rightMaxY : 0;
            return Math.Max(dx, dy);
        }

        private bool FootprintFree(int originX, int originY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            for (int y = originY; y < originY + f; y++)
            {
                for (int x = originX; x < originX + f; x++)
                {
                    if (!IsCellFree(x, y)) return false;
                }
            }
            return true;
        }

        private void OccupyFootprint(int originX, int originY, bool pushOutUnits)
        {
            SetFootprint(originX, originY, 1);
            if (pushOutUnits)
            {
                PushMobileUnitsOutOfFootprint(originX, originY);
            }
        }

        private void FreeFootprint(int originX, int originY) => SetFootprint(originX, originY, 0);

        private void SetFootprint(int originX, int originY, byte value)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            for (int y = originY; y < originY + f; y++)
            {
                for (int x = originX; x < originX + f; x++)
                {
                    _occupied[y * GridSize + x] = value;
                    // Mirror into the pathfinding cost field: buildings are
                    // impassable terrain. Out-of-bounds cells (cost field
                    // smaller than the 128 construction grid) are no-ops.
                    _costField?.SetCost((ushort)x, (ushort)y,
                        value == 1 ? CostField.ImpassableCost : CostField.OpenCost);
                }
            }
        }

        /// <summary>
        /// Displaces every mobile unit standing inside a freshly occupied
        /// footprint onto the nearest free cell (expanding Chebyshev rings
        /// around its cell, ascending (y, x) — the spawn-search convention;
        /// free = no placement footprint, walkable, no unit on it). A unit
        /// whose personal goal cell lies inside the new footprint is
        /// retargeted to the cell it was pushed to, so it stops there
        /// instead of walking back into the wall. Immobile entities
        /// (MoveSpeed 0: the site/building entity itself) are skipped. A
        /// unit with no free cell inside <see cref="PushOutMaxRing"/> stays
        /// where it is (documented edge case: walled in at the map edge).
        /// </summary>
        private void PushMobileUnitsOutOfFootprint(int originX, int originY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState unit = ref units[i];
                if (!unit.IsActive || unit.MoveSpeed <= SimFixed.Zero) continue;

                int ux = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionX));
                int uy = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionY));
                if (ux < originX || ux >= originX + f || uy < originY || uy >= originY + f) continue;

                if (!TryFindPushOutCell(ux, uy, out int nx, out int ny))
                {
                    continue;
                }

                unit.Transform = new Transform2D(
                    SimFixed.FromInt(nx), SimFixed.FromInt(ny), unit.Transform.Rotation);
                if (unit.IsMoving && unit.GoalGridPos.IsValid
                    && unit.GoalGridPos.X >= originX && unit.GoalGridPos.X < originX + f
                    && unit.GoalGridPos.Y >= originY && unit.GoalGridPos.Y < originY + f)
                {
                    unit.SetTarget(new GridPos2D(nx, ny));
                }
            }
        }

        /// <summary>Deterministic ring search of the push-out: nearest free cell around the displaced unit.</summary>
        private bool TryFindPushOutCell(int fromX, int fromY, out int cellX, out int cellY)
        {
            for (int ring = 0; ring <= PushOutMaxRing; ring++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                        int x = fromX + dx;
                        int y = fromY + dy;
                        if (!IsCellFree(x, y)) continue;
                        if (_costField != null
                            && (!_costField.IsInBounds(x, y) || !_costField.IsWalkable((ushort)x, (ushort)y)))
                        {
                            continue;
                        }
                        if (_entityManager.HasActiveUnitOnCell(x, y)) continue;
                        cellX = x;
                        cellY = y;
                        return true;
                    }
                }
            }
            cellX = 0;
            cellY = 0;
            return false;
        }

        private int IndexOfSite(uint rawEntityId)
        {
            for (int i = 0; i < MaxSites; i++)
            {
                if (_sites[i].IsActive && _sites[i].RawEntityId == rawEntityId) return i;
            }
            return -1;
        }

        private int IndexOfBuilding(uint rawEntityId)
        {
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (_buildings[i].IsActive && _buildings[i].RawEntityId == rawEntityId) return i;
            }
            return -1;
        }

        private int IndexOfRepairByBuilder(uint builderRaw)
        {
            for (int i = 0; i < MaxRepairOrders; i++)
            {
                if (_repairs[i].IsActive && _repairs[i].BuilderRaw == builderRaw) return i;
            }
            return -1;
        }

        private int FreeSiteIndex()
        {
            for (int i = 0; i < MaxSites; i++) if (!_sites[i].IsActive) return i;
            return -1;
        }

        private int FreeBuildingIndex()
        {
            for (int i = 0; i < MaxBuildings; i++) if (!_buildings[i].IsActive) return i;
            return -1;
        }

        private int FreeRepairIndex()
        {
            for (int i = 0; i < MaxRepairOrders; i++) if (!_repairs[i].IsActive) return i;
            return -1;
        }

        private void CompactSites()
        {
            int write = 0;
            for (int read = 0; read < MaxSites; read++)
            {
                if (!_sites[read].IsActive) continue;
                if (write != read) _sites[write] = _sites[read];
                write++;
            }
            for (int i = write; i < MaxSites; i++) _sites[i] = default;
        }

        private void CompactBuildings()
        {
            int write = 0;
            for (int read = 0; read < MaxBuildings; read++)
            {
                if (!_buildings[read].IsActive) continue;
                if (write != read) _buildings[write] = _buildings[read];
                write++;
            }
            for (int i = write; i < MaxBuildings; i++) _buildings[i] = default;
        }

        private void CompactRepairOrders()
        {
            int write = 0;
            for (int read = 0; read < MaxRepairOrders; read++)
            {
                if (!_repairs[read].IsActive) continue;
                if (write != read) _repairs[write] = _repairs[read];
                write++;
            }
            for (int i = write; i < MaxRepairOrders; i++) _repairs[i] = default;
        }

        // ------------------------------------------------------------------
        // Snapshot block 105 (v1)
        // ------------------------------------------------------------------

        /// <summary>
        /// Block content v1: version, T2 bitmask (bit p = slot p), then the
        /// sites, completed placements and repair orders in ascending table
        /// order. Hash-sensitive by construction: any placement, progress or
        /// unlock change moves the block bytes and therefore the canonical
        /// state hash.
        /// </summary>
        public void WriteState(SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);

            byte t2Mask = 0;
            for (int p = 0; p < EconomySystem.MaxPlayers; p++)
            {
                if (_t2Unlocked[p]) t2Mask |= (byte)(1 << p);
            }
            writer.WriteUInt8(t2Mask);

            writer.WriteUInt16(unchecked((ushort)SiteCount));
            for (int i = 0; i < MaxSites; i++)
            {
                if (!_sites[i].IsActive) continue;
                writer.WriteUInt16(_sites[i].BuildingDefId);
                writer.WriteUInt16(_sites[i].OriginX);
                writer.WriteUInt16(_sites[i].OriginY);
                writer.WriteUInt32(_sites[i].RawEntityId);
                writer.WriteUInt32(_sites[i].AssignedBuilderRaw);
                writer.WriteInt32(_sites[i].ProgressRaw);
            }

            writer.WriteUInt16(unchecked((ushort)BuildingCount));
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (!_buildings[i].IsActive) continue;
                writer.WriteUInt16(_buildings[i].BuildingDefId);
                writer.WriteUInt16(_buildings[i].OriginX);
                writer.WriteUInt16(_buildings[i].OriginY);
                writer.WriteUInt32(_buildings[i].RawEntityId);
            }

            int repairCount = 0;
            for (int i = 0; i < MaxRepairOrders; i++) if (_repairs[i].IsActive) repairCount++;
            writer.WriteUInt16(unchecked((ushort)repairCount));
            for (int i = 0; i < MaxRepairOrders; i++)
            {
                if (!_repairs[i].IsActive) continue;
                writer.WriteUInt32(_repairs[i].BuilderRaw);
                writer.WriteUInt32(_repairs[i].TargetRaw);
            }
        }

        /// <summary>Fully validates a construction block without mutating the system.</summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _);
        }

        /// <summary>
        /// Restores sites, placements, repair orders and T2 flags and rebuilds
        /// the derived occupancy grid; malformed input returns false and
        /// leaves the system untouched (two-phase contract of
        /// <see cref="IStatefulSimSystem"/>).
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out ParsedConstruction parsed)) return false;

            // Remove the pre-restore dynamic footprint overlay before
            // replacing the authoritative placement tables. Clearing only
            // _occupied would leave cells that exist solely in the old live
            // state impassable in the shared CostField and make placement or
            // pathfinding after restore depend on the target host's history.
            for (int i = 0; i < MaxSites; i++)
            {
                if (_sites[i].IsActive) FreeFootprint(_sites[i].OriginX, _sites[i].OriginY);
            }
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (_buildings[i].IsActive) FreeFootprint(_buildings[i].OriginX, _buildings[i].OriginY);
            }

            Array.Copy(parsed.Sites, _sites, MaxSites);
            Array.Copy(parsed.Buildings, _buildings, MaxBuildings);
            Array.Copy(parsed.Repairs, _repairs, MaxRepairOrders);
            Array.Copy(parsed.T2Unlocked, _t2Unlocked, EconomySystem.MaxPlayers);

            // Rebuild the derived occupancy cache from the placements. No
            // push-out here: the entity store block restores AFTER this one
            // (registration order), so the live units at this point are the
            // PRE-restore ones — the snapshot's own units already satisfy
            // the outside-the-footprint invariant the runtime push-out
            // upholds.
            Array.Clear(_occupied, 0, _occupied.Length);
            for (int i = 0; i < MaxSites; i++)
            {
                if (_sites[i].IsActive) OccupyFootprint(_sites[i].OriginX, _sites[i].OriginY, pushOutUnits: false);
            }
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (_buildings[i].IsActive) OccupyFootprint(_buildings[i].OriginX, _buildings[i].OriginY, pushOutUnits: false);
            }
            return true;
        }

        /// <summary>Validated intermediate of a parsed construction block.</summary>
        private sealed class ParsedConstruction
        {
            public SiteState[] Sites;
            public PlacementState[] Buildings;
            public RepairOrderState[] Repairs;
            public bool[] T2Unlocked;
        }

        /// <summary>
        /// Parses and fully validates block content — exact lengths, known
        /// definition ids, in-map non-overlapping footprints, canonical entity
        /// ids, progress bounds and uniqueness invariants — into a
        /// commit-ready intermediate. Never mutates this system.
        /// </summary>
        private bool TryParseState(ReadOnlySpan<byte> blockContent, out ParsedConstruction parsed)
        {
            parsed = null;
            var reader = new SnapshotBlockReader(blockContent);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt8(out byte t2Mask)) return false;

            var t2 = new bool[EconomySystem.MaxPlayers];
            for (int p = 0; p < EconomySystem.MaxPlayers; p++)
            {
                t2[p] = (t2Mask & (1 << p)) != 0;
            }

            var occupancy = new byte[GridSize * GridSize];

            if (!reader.TryReadUInt16(out ushort siteCount) || siteCount > MaxSites) return false;
            var sites = new SiteState[MaxSites];
            var seenEntities = new bool[1024]; // entity index uniqueness across sites and placements
            for (int i = 0; i < siteCount; i++)
            {
                if (!reader.TryReadUInt16(out ushort defId)) return false;
                if (!SimDefinitions.TryGetBuilding(defId, out SimBuildingDefinition def)) return false;
                if (!reader.TryReadUInt16(out ushort originX)) return false;
                if (!reader.TryReadUInt16(out ushort originY)) return false;
                if (!reader.TryReadUInt32(out uint rawEntityId)) return false;
                if (!CommandIds.IsCanonicalEntityId(rawEntityId)) return false;
                if (!reader.TryReadUInt32(out uint builderRaw)) return false;
                if (builderRaw != 0 && !CommandIds.IsCanonicalEntityId(builderRaw)) return false;
                if (builderRaw != 0 && !ValidateAssignedBuilder(builderRaw, rawEntityId)) return false;
                if (!reader.TryReadInt32(out int progressRaw)) return false;
                if (progressRaw < 0 || progressRaw > (def.BuildTicks << 16)) return false;
                if (!FootprintInsideMap(originX, originY)) return false;
                if (!TryMarkFootprint(occupancy, originX, originY)) return false; // overlap
                int entityIndex = (int)(rawEntityId & 0x3FFu);
                if (seenEntities[entityIndex]) return false; // duplicate placement entity
                seenEntities[entityIndex] = true;

                sites[i] = new SiteState
                {
                    IsActive = true,
                    BuildingDefId = defId,
                    OriginX = originX,
                    OriginY = originY,
                    RawEntityId = rawEntityId,
                    AssignedBuilderRaw = builderRaw,
                    ProgressRaw = progressRaw,
                };
            }

            if (!reader.TryReadUInt16(out ushort buildingCount) || buildingCount > MaxBuildings) return false;
            var buildings = new PlacementState[MaxBuildings];
            for (int i = 0; i < buildingCount; i++)
            {
                if (!reader.TryReadUInt16(out ushort defId)) return false;
                if (!SimDefinitions.TryGetBuilding(defId, out _)) return false;
                if (!reader.TryReadUInt16(out ushort originX)) return false;
                if (!reader.TryReadUInt16(out ushort originY)) return false;
                if (!reader.TryReadUInt32(out uint rawEntityId)) return false;
                if (!CommandIds.IsCanonicalEntityId(rawEntityId)) return false;
                if (!FootprintInsideMap(originX, originY)) return false;
                if (!TryMarkFootprint(occupancy, originX, originY)) return false; // overlap
                int entityIndex = (int)(rawEntityId & 0x3FFu);
                if (seenEntities[entityIndex]) return false; // duplicate placement entity
                seenEntities[entityIndex] = true;

                buildings[i] = new PlacementState
                {
                    IsActive = true,
                    BuildingDefId = defId,
                    OriginX = originX,
                    OriginY = originY,
                    RawEntityId = rawEntityId,
                };
            }

            if (!reader.TryReadUInt16(out ushort repairCount) || repairCount > MaxRepairOrders) return false;
            var repairs = new RepairOrderState[MaxRepairOrders];
            for (int i = 0; i < repairCount; i++)
            {
                if (!reader.TryReadUInt32(out uint builderRaw)) return false;
                if (!reader.TryReadUInt32(out uint targetRaw)) return false;
                if (!CommandIds.IsCanonicalEntityId(builderRaw) || !CommandIds.IsCanonicalEntityId(targetRaw)) return false;
                for (int j = 0; j < i; j++)
                {
                    if (repairs[j].BuilderRaw == builderRaw) return false; // one order per builder
                }
                repairs[i] = new RepairOrderState { IsActive = true, BuilderRaw = builderRaw, TargetRaw = targetRaw };
            }
            if (reader.Remaining != 0) return false;

            parsed = new ParsedConstruction
            {
                Sites = sites,
                Buildings = buildings,
                Repairs = repairs,
                T2Unlocked = t2,
            };
            return true;
        }

        /// <summary>Marks a footprint in a scratch occupancy map; false on overlap (parse-time invariant).</summary>
        private static bool TryMarkFootprint(byte[] occupancy, int originX, int originY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            for (int y = originY; y < originY + f; y++)
            {
                for (int x = originX; x < originX + f; x++)
                {
                    int cell = y * GridSize + x;
                    if (occupancy[cell] != 0) return false;
                    occupancy[cell] = 1;
                }
            }
            return true;
        }

        /// <summary>
        /// Cross-block invariant of a site's assigned builder (P2-2): when the
        /// referenced builder entity is visible in the CURRENT entity store,
        /// it must carry the Builder role and — when the site entity is also
        /// visible — belong to the site owner's slot; a violation rejects the
        /// block (restore leaves the host untouched). A builder reference
        /// whose entity is NOT in the current store cannot be judged here:
        /// the kernel validates every block against the pre-restore state, so
        /// a restore into a fresh host resolves entity ids only after this
        /// block was validated — that case is covered by the defense-in-depth
        /// role/owner re-check in <see cref="ProgressSites"/>, which
        /// re-assigns any unusable builder deterministically.
        /// </summary>
        private bool ValidateAssignedBuilder(uint builderRaw, uint siteRawEntityId)
        {
            EntityId builderId = UnitCommandStateView.ToEntityId(builderRaw);
            if (!_entityManager.TryGetUnit(builderId, out UnitState builderUnit))
            {
                return true; // not judgeable against the pre-restore store (see remarks)
            }
            if (builderUnit.Role != UnitRole.Builder) return false;
            EntityId siteId = UnitCommandStateView.ToEntityId(siteRawEntityId);
            if (_entityManager.TryGetUnit(siteId, out UnitState siteUnit)
                && builderUnit.PlayerId != siteUnit.PlayerId)
            {
                return false;
            }
            return true;
        }
    }
}
