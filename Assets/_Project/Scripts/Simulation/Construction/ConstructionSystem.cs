using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
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
    /// deterministic: unknown definition, footprint outside the 128x128
    /// grid or occupied cells (RejectedInvalidTarget), then missing
    /// prerequisite role and the power rule (RejectedPrerequisitesNotMet).
    /// The power rule: a building with PowerRequired &gt; 0 may only be
    /// placed while the owner's last committed balance
    /// (previous tick's phase-2 recompute) covers the additional draw:
    /// PowerProvided - PowerRequired &gt;= PowerRequired of the new
    /// building. The manifest exception (quality/content/mvp-v1.json
    /// startStatePerPlayer.onlyPrerequisiteException: the STARTING Refinery
    /// needs no Power) is honored structurally: the match start is placed
    /// via <see cref="PlaceCompletedBuilding"/>, which bypasses placement
    /// validation entirely — every refinery placed through the command path
    /// requires a completed own Power plant (provisional prerequisite,
    /// Q-040) and the power rule.
    /// </para>
    /// <para>
    /// Sites: a site is a live entity carrying role
    /// <see cref="UnitRole.Unit"/> (so the economy's power recompute
    /// ignores it) at the footprint center with 1 HP of its definition's
    /// MaxHealth — construction HP interpolation is deliberately NOT
    /// modeled (provisional, Q-040 candidate). Builder assignment: the
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
    /// tick of progress per two ticks). Completion sets the entity's role
    /// to the definition's building role, restores full HP and — for a
    /// ResearchLab — sets the owner's T2 unlock (phase 5;
    /// mvp-v1.json technology.researchLabCompletionUnlocksTier2; there are
    /// no research upgrades, no research queue and no tier 3 in MS-1).
    /// A site entity destroyed by combat aborts the site without refund.
    /// </para>
    /// <para>
    /// Cancel/sell/repair (documented provisional economy rules, Q-040
    /// candidates): CancelConstruction forfeits all progress and refunds
    /// 75% of the cost (floor); Sell on a COMPLETED building refunds 50%
    /// (floor) and despawns it; a running production queue on a sold or
    /// destroyed building is lost without refund (production domain).
    /// Repair assigns a Builder a standing repair order on an own completed
    /// damaged building: in reach (same Chebyshev rule) the target gains
    /// <see cref="RepairRateHpPerTick"/> HP per tick up to its MaxHealth,
    /// where the order resolves; out of reach the order is HELD, never
    /// dropped; Stop clears it. Repair is unaffected by low power.
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

        /// <summary>Refund percentage of CancelConstruction (provisional, Q-040 candidate; integer floor).</summary>
        public const int CancelRefundPercent = 75;

        /// <summary>Refund percentage of Sell on a completed building (provisional, Q-040 candidate; integer floor).</summary>
        public const int SellRefundPercent = 50;

        /// <summary>Provisional repair rate in HP per tick per repairing Builder (Q-040 candidate).</summary>
        public const int RepairRateHpPerTick = 10;

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

        // Derived occupancy cache (rebuilt from the placements on restore).
        private readonly byte[] _occupied;

        public string Name => "ConstructionSystem";

        public ushort StateBlockId => SnapshotBlockIds.Construction;

        public ConstructionSystem(EntityManager entityManager, EconomySystem economy)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _sites = new SiteState[MaxSites];
            _buildings = new PlacementState[MaxBuildings];
            _repairs = new RepairOrderState[MaxRepairOrders];
            _t2Unlocked = new bool[EconomySystem.MaxPlayers];
            _occupied = new byte[GridSize * GridSize];
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo(
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

        /// <summary>True when the slot owns a COMPLETED building of the given role (prerequisite scans).</summary>
        public bool HasFinishedBuilding(byte playerSlot, UnitRole role)
        {
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (!_buildings[i].IsActive) continue;
                if (!SimDefinitions.TryGetBuilding(_buildings[i].BuildingDefId, out SimBuildingDefinition def)) continue;
                if (def.Role != role) continue;
                EntityId id = UnitCommandStateView.ToEntityId(_buildings[i].RawEntityId);
                if (_entityManager.TryGetUnit(id, out UnitState unit) && unit.PlayerId == playerSlot)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------
        // State-dependent validation (no mutation; the command view calls
        // these at the target tick in the documented fixed order)
        // ------------------------------------------------------------------

        /// <summary>
        /// Full state-dependent placement validation in fixed order: unknown
        /// definition, out-of-map footprint and occupied cells
        /// (RejectedInvalidTarget), then prerequisite role, power rule and
        /// site capacity (RejectedPrerequisitesNotMet). Cost is the executor's
        /// separate check (RejectedInsufficientResources) and runs BEFORE this.
        /// </summary>
        public CommandResultCode ValidatePlacement(byte playerSlot, ushort buildingDefId, int originX, int originY)
        {
            if (!SimDefinitions.TryGetBuilding(buildingDefId, out SimBuildingDefinition def))
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            if (!FootprintInsideMap(originX, originY) || !FootprintFree(originX, originY))
            {
                return CommandResultCode.RejectedInvalidTarget;
            }
            if (def.HasPrerequisite && !HasFinishedBuilding(playerSlot, def.PrerequisiteRole))
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            ref readonly PlayerEconomyState eco = ref _economy.GetPlayerEconomy(playerSlot);
            if (def.PowerRequired > 0 && eco.PowerProvided - eco.PowerRequired < def.PowerRequired)
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            if (FreeSiteIndex() < 0)
            {
                return CommandResultCode.RejectedPrerequisitesNotMet;
            }
            return CommandResultCode.Applied;
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
        /// entity (role <see cref="UnitRole.Unit"/>, 1 HP) and auto-assigns
        /// the lowest-index own Builder. Returns false without mutating when
        /// any check fails.
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
        /// Match-setup placement of a COMPLETED building: bypasses cost,
        /// prerequisite and power validation by contract (the only caller is
        /// deterministic host content wiring — this is how the manifest's
        /// starting HQ + Refinery and the start-refinery prerequisite
        /// exception are honored). Applies the same completion effects as a
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
            OccupyFootprint(originX, originY);
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
                _economy.GetPlayerEconomy(unit.PlayerId).AddCredits((long)def.CostAE * CancelRefundPercent / 100);
            }
            _entityManager.DespawnUnit(id);
            FreeFootprint(site.OriginX, site.OriginY);
            site.IsActive = false;
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
                _economy.GetPlayerEconomy(unit.PlayerId).AddCredits((long)def.CostAE * SellRefundPercent / 100);
            }
            _entityManager.DespawnUnit(id);
            FreeFootprint(placement.OriginX, placement.OriginY);
            placement.IsActive = false;
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
        }

        private void CompleteSite(int siteIndex, in SimBuildingDefinition def, byte ownerSlot)
        {
            uint rawEntityId = _sites[siteIndex].RawEntityId;
            ushort originX = _sites[siteIndex].OriginX;
            ushort originY = _sites[siteIndex].OriginY;
            _sites[siteIndex].IsActive = false;

            EntityId id = UnitCommandStateView.ToEntityId(rawEntityId);
            ref UnitState unit = ref _entityManager.GetUnitRef(id);
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
        }

        private void ProcessRepairOrders()
        {
            for (int i = 0; i < MaxRepairOrders; i++)
            {
                ref RepairOrderState order = ref _repairs[i];
                if (!order.IsActive) continue;

                EntityId builderId = UnitCommandStateView.ToEntityId(order.BuilderRaw);
                EntityId targetId = UnitCommandStateView.ToEntityId(order.TargetRaw);
                if (!_entityManager.IsValid(builderId) || !_entityManager.IsValid(targetId))
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

                ref UnitState target = ref _entityManager.GetUnitRef(targetId);
                if (target.CurrentHealth >= target.MaxHealth)
                {
                    order.IsActive = false; // fully repaired: the order resolves
                    continue;
                }

                ref readonly UnitState builder = ref _entityManager.GetUnitRef(builderId);
                if (!IsInReachOfFootprint(in builder, _buildings[placementIndex].OriginX, _buildings[placementIndex].OriginY))
                {
                    continue; // held, not dropped
                }

                int repaired = target.CurrentHealth + RepairRateHpPerTick;
                target.CurrentHealth = repaired > target.MaxHealth ? target.MaxHealth : repaired;
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void CreateSite(byte playerSlot, in SimBuildingDefinition def, int originX, int originY)
        {
            EntityId id = SpawnBuildingEntity(playerSlot, in def, originX, originY, completed: false);
            OccupyFootprint(originX, originY);
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
        /// carries role <see cref="UnitRole.Unit"/> and 1 HP; a completed
        /// building carries its definition role at full HP.
        /// </summary>
        private EntityId SpawnBuildingEntity(byte playerSlot, in SimBuildingDefinition def, int originX, int originY, bool completed)
        {
            EntityId id = _entityManager.SpawnUnit(
                playerSlot,
                new Transform2D(SimFixed.FromInt(originX + 1), SimFixed.FromInt(originY + 1)),
                SimFixed.Zero,
                maxHealth: def.MaxHealth,
                role: completed ? def.Role : UnitRole.Unit);
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

        private void OccupyFootprint(int originX, int originY) => SetFootprint(originX, originY, 1);

        private void FreeFootprint(int originX, int originY) => SetFootprint(originX, originY, 0);

        private void SetFootprint(int originX, int originY, byte value)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            for (int y = originY; y < originY + f; y++)
            {
                for (int x = originX; x < originX + f; x++)
                {
                    _occupied[y * GridSize + x] = value;
                }
            }
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

            Array.Copy(parsed.Sites, _sites, MaxSites);
            Array.Copy(parsed.Buildings, _buildings, MaxBuildings);
            Array.Copy(parsed.Repairs, _repairs, MaxRepairOrders);
            Array.Copy(parsed.T2Unlocked, _t2Unlocked, EconomySystem.MaxPlayers);

            // Rebuild the derived occupancy cache from the placements.
            Array.Clear(_occupied, 0, _occupied.Length);
            for (int i = 0; i < MaxSites; i++)
            {
                if (_sites[i].IsActive) OccupyFootprint(_sites[i].OriginX, _sites[i].OriginY);
            }
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (_buildings[i].IsActive) OccupyFootprint(_buildings[i].OriginX, _buildings[i].OriginY);
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
