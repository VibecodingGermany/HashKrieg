using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Adapter from the canonical command executor's state view
    /// (<see cref="ICommandStateView"/>) to the real unit state of this G1
    /// slice: the <see cref="EntityManager"/> unit store, the
    /// <see cref="PathfindingSystem"/> flow fields, the
    /// <see cref="EconomySystem"/> field registry and credit balances and
    /// the construction/production domains. The kernel applies due sealed
    /// batches exclusively through this view in tick phase 1
    /// (docs/tech/SimulationCore.md section 2); systems never see commands.
    /// <para>
    /// Wire ids are the packed uint32 layout of SimulationCore.md section 1
    /// (bits 0–9 index, bits 10–31 generation); the prototype
    /// <see cref="EntityId"/> still stores index/version separately, so the
    /// adapter translates (migration tracked as Q-040(e)). A generation that
    /// does not fit the prototype's ushort version simply refers to no live
    /// entity.
    /// </para>
    /// <para>
    /// Slice scope: Move, Stop, AttackTarget, Harvest and ReturnCargo mutate
    /// unit state directly; the construction/production kinds
    /// (PlaceBuilding, CancelConstruction, Repair, Sell, QueueUnit,
    /// CancelProduction, SetRallyPoint) are validated state-dependently and
    /// applied through the canonical <see cref="ConstructionSystem"/> and
    /// <see cref="ProductionSystem"/>. Harvest legality is validated
    /// state-dependently by the executor before <see cref="Apply"/> runs
    /// (review fixes P2-1/P2-2). InstallDefenseModule carries a schema-v1
    /// payload, but defense modules are G2/G4 content — it is rejected
    /// deterministically with RejectedPrerequisitesNotMet in this slice and
    /// mutates nothing. A host that does not wire the construction and
    /// production systems (optional constructor arguments) rejects every
    /// construction/production kind with RejectedPrerequisitesNotMet
    /// (documented "domain not wired" result).
    /// </para>
    /// <para>
    /// Cost model: <see cref="CanAfford"/> covers the single-definition
    /// kinds (PlaceBuilding; InstallDefenseModule stays free until G2/G4)
    /// against the canonical definition table
    /// (<see cref="SimDefinitions"/>). The QueueUnit cost scales with the
    /// payload count, so it is checked inside
    /// <see cref="ValidateDomain"/> where the count is available. An
    /// UNKNOWN definition id is never reported as unaffordable —
    /// <see cref="CanAfford"/> returns true for it and the domain check
    /// rejects it as RejectedInvalidTarget, keeping the deterministic
    /// result honest.
    /// </para>
    /// </summary>
    public sealed class UnitCommandStateView : ICommandStateView
    {
        private readonly EntityManager _entityManager;
        private readonly PathfindingSystem _pathfindingSystem;
        private readonly EconomySystem _economySystem;
        private readonly ConstructionSystem _constructionSystem;
        private readonly ProductionSystem _productionSystem;

        public UnitCommandStateView(
            EntityManager entityManager,
            PathfindingSystem pathfindingSystem,
            EconomySystem economySystem,
            ConstructionSystem constructionSystem = null,
            ProductionSystem productionSystem = null)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _pathfindingSystem = pathfindingSystem ?? throw new ArgumentNullException(nameof(pathfindingSystem));
            _economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));
            _constructionSystem = constructionSystem;
            _productionSystem = productionSystem;
        }

        /// <summary>Converts a packed wire id to a prototype handle; invalid when the generation does not fit.</summary>
        public static EntityId ToEntityId(uint rawEntityId)
        {
            int index = (int)(rawEntityId & 0x3FFu);
            uint generation = rawEntityId >> 10;
            if (generation == 0 || generation > ushort.MaxValue)
            {
                return EntityId.Invalid;
            }
            return new EntityId(index, (ushort)generation);
        }

        /// <summary>Converts a prototype handle to its packed wire id (0 for invalid handles).</summary>
        public static uint ToRawEntityId(EntityId id)
        {
            if (!id.IsValid || id.Index < 0 || id.Index > 0x3FF || id.Version == 0)
            {
                return 0u;
            }
            return unchecked((uint)((id.Version << 10) | id.Index));
        }

        public bool EntityExists(uint rawEntityId)
        {
            return _entityManager.IsValid(ToEntityId(rawEntityId));
        }

        public bool IsOwnedBy(byte playerSlot, uint rawEntityId)
        {
            EntityId id = ToEntityId(rawEntityId);
            return _entityManager.TryGetUnit(id, out UnitState unit) && unit.PlayerId == playerSlot;
        }

        /// <summary>
        /// State-dependent Harvest legality: true when the id names a
        /// registered Aetherium field at the target tick.
        /// </summary>
        public bool AetheriumFieldExists(uint fieldId)
        {
            return fieldId <= ushort.MaxValue && _economySystem.TryGetField((ushort)fieldId, out _);
        }

        /// <summary>
        /// State-dependent Harvest legality: true when the raw entity id
        /// refers to a live unit with the Harvester role at the target tick.
        /// </summary>
        public bool IsHarvester(uint rawEntityId)
        {
            EntityId id = ToEntityId(rawEntityId);
            return _entityManager.TryGetUnit(id, out UnitState unit) && unit.Role == UnitRole.Harvester;
        }

        /// <summary>
        /// Generic cost check of the single-definition kinds against the
        /// canonical definition table: PlaceBuilding costs the building's
        /// full price; InstallDefenseModule stays free until the G2/G4
        /// module slice wires module costs. Unknown definition ids report
        /// true here and fail as RejectedInvalidTarget in the domain check
        /// (see class remarks). QueueUnit is deliberately NOT consulted by
        /// the executor — its count-scaled cost is a domain check.
        /// </summary>
        public bool CanAfford(byte playerSlot, CommandKind kind, ushort definitionId)
        {
            if (kind == CommandKind.PlaceBuilding
                && SimDefinitions.TryGetBuilding(definitionId, out SimBuildingDefinition building))
            {
                return _economySystem.GetPlayerEconomy(playerSlot).AetheriumCredits >= building.CostAE;
            }
            return true;
        }

        /// <summary>
        /// Domain-specific state-dependent validation of the
        /// construction/production kinds in each kind's documented fixed
        /// order (see <see cref="ConstructionSystem"/> and
        /// <see cref="ProductionSystem"/>); every other kind has no domain
        /// checks in this slice.
        /// </summary>
        public CommandResultCode ValidateDomain(in CommandRecord record, in CommandPayloadRefs refs)
        {
            switch (record.Kind)
            {
                case CommandKind.PlaceBuilding:
                {
                    if (_constructionSystem == null) return CommandResultCode.RejectedPrerequisitesNotMet;
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!PlaceBuildingPayload.TryParse(ref reader, out PlaceBuildingPayload place))
                    {
                        throw new InvalidOperationException("Sealed PlaceBuilding payload failed to parse.");
                    }
                    return _constructionSystem.ValidatePlacement(
                        record.PlayerSlot, place.BuildingDefId, place.GridX, place.GridY);
                }
                case CommandKind.CancelConstruction:
                    return _constructionSystem == null
                        ? CommandResultCode.RejectedPrerequisitesNotMet
                        : _constructionSystem.ValidateCancel(record.PlayerSlot, refs.SingleEntityId);
                case CommandKind.Sell:
                    return _constructionSystem == null
                        ? CommandResultCode.RejectedPrerequisitesNotMet
                        : _constructionSystem.ValidateSell(record.PlayerSlot, refs.SingleEntityId);
                case CommandKind.Repair:
                    return _constructionSystem == null
                        ? CommandResultCode.RejectedPrerequisitesNotMet
                        : _constructionSystem.ValidateRepair(record.PlayerSlot, refs.ActingEntities, refs.TargetEntityId);
                case CommandKind.QueueUnit:
                {
                    if (_productionSystem == null) return CommandResultCode.RejectedPrerequisitesNotMet;
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!QueueUnitPayload.TryParse(ref reader, out QueueUnitPayload queue))
                    {
                        throw new InvalidOperationException("Sealed QueueUnit payload failed to parse.");
                    }
                    return _productionSystem.ValidateQueueUnit(
                        record.PlayerSlot, queue.BuildingEntityId, queue.UnitDefId, queue.Count);
                }
                case CommandKind.CancelProduction:
                {
                    if (_productionSystem == null) return CommandResultCode.RejectedPrerequisitesNotMet;
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!CancelProductionPayload.TryParse(ref reader, out CancelProductionPayload cancel))
                    {
                        throw new InvalidOperationException("Sealed CancelProduction payload failed to parse.");
                    }
                    return _productionSystem.ValidateCancelProduction(
                        record.PlayerSlot, cancel.BuildingEntityId, cancel.QueueIndex);
                }
                case CommandKind.SetRallyPoint:
                    return _productionSystem == null
                        ? CommandResultCode.RejectedPrerequisitesNotMet
                        : _productionSystem.ValidateSetRallyPoint(record.PlayerSlot, refs.SingleEntityId);
                case CommandKind.InstallDefenseModule:
                    // Schema-v1 payload exists, but defense modules are G2/G4
                    // content (mvp-v1.json defenseModules): deterministic
                    // rejection, no mutation, the record stays in the stream.
                    return CommandResultCode.RejectedPrerequisitesNotMet;
                default:
                    return CommandResultCode.Applied;
            }
        }

        /// <summary>
        /// Applies a sealed, fully checked record. Move/Stop/AttackTarget and
        /// the economy orders Harvest/ReturnCargo mutate the unit store; the
        /// construction/production kinds mutate their domain systems.
        /// </summary>
        public void Apply(in CommandRecord record)
        {
            switch (record.Kind)
            {
                case CommandKind.Move:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!MovePayload.TryParse(ref reader, out MovePayload move))
                    {
                        throw new InvalidOperationException("Sealed Move payload failed to parse.");
                    }
                    ApplyMove(in move);
                    break;
                }
                case CommandKind.Stop:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!StopPayload.TryParse(ref reader, out StopPayload stop))
                    {
                        throw new InvalidOperationException("Sealed Stop payload failed to parse.");
                    }
                    for (int i = 0; i < stop.EntityIds.Length; i++)
                    {
                        EntityId id = ToEntityId(stop.EntityIds[i]);
                        if (_entityManager.IsValid(id))
                        {
                            ref UnitState unit = ref _entityManager.GetUnitRef(id);
                            unit.Stop();
                            // Stop cancels every standing order, economy and
                            // repair orders included; the unit keeps its cargo.
                            unit.HarvestFieldId = 0;
                            unit.IsReturningCargo = false;
                            _constructionSystem?.ClearRepairOrder(stop.EntityIds[i]);
                        }
                    }
                    break;
                }
                case CommandKind.AttackTarget:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!AttackTargetPayload.TryParse(ref reader, out AttackTargetPayload attack))
                    {
                        throw new InvalidOperationException("Sealed AttackTarget payload failed to parse.");
                    }
                    EntityId target = ToEntityId(attack.TargetEntityId);
                    for (int i = 0; i < attack.EntityIds.Length; i++)
                    {
                        EntityId id = ToEntityId(attack.EntityIds[i]);
                        if (_entityManager.IsValid(id))
                        {
                            _entityManager.GetUnitRef(id).AttackTarget = target;
                        }
                    }
                    break;
                }
                case CommandKind.Harvest:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!HarvestPayload.TryParse(ref reader, out HarvestPayload harvest))
                    {
                        throw new InvalidOperationException("Sealed Harvest payload failed to parse.");
                    }
                    // Defensive contract of Apply: the executor's
                    // state-dependent field check already rejected unknown
                    // ids, so this guard can only trigger on a broken
                    // executor/view pairing, never on canonical input.
                    if (!_economySystem.TryGetField(harvest.FieldId, out _))
                    {
                        break;
                    }
                    for (int i = 0; i < harvest.EntityIds.Length; i++)
                    {
                        EntityId id = ToEntityId(harvest.EntityIds[i]);
                        if (_entityManager.IsValid(id))
                        {
                            ref UnitState unit = ref _entityManager.GetUnitRef(id);
                            unit.HarvestFieldId = harvest.FieldId;
                            unit.IsReturningCargo = false;
                        }
                    }
                    break;
                }
                case CommandKind.ReturnCargo:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!ReturnCargoPayload.TryParse(ref reader, out ReturnCargoPayload returnCargo))
                    {
                        throw new InvalidOperationException("Sealed ReturnCargo payload failed to parse.");
                    }
                    for (int i = 0; i < returnCargo.EntityIds.Length; i++)
                    {
                        EntityId id = ToEntityId(returnCargo.EntityIds[i]);
                        if (_entityManager.IsValid(id))
                        {
                            ref UnitState unit = ref _entityManager.GetUnitRef(id);
                            unit.IsReturningCargo = true;
                            unit.HarvestFieldId = 0;
                        }
                    }
                    break;
                }
                case CommandKind.PlaceBuilding:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!PlaceBuildingPayload.TryParse(ref reader, out PlaceBuildingPayload place))
                    {
                        throw new InvalidOperationException("Sealed PlaceBuilding payload failed to parse.");
                    }
                    // Validated by CanAfford + ValidateDomain immediately
                    // before; a false return here is a broken view contract.
                    _constructionSystem?.TryPlaceBuilding(
                        record.PlayerSlot, place.BuildingDefId, place.GridX, place.GridY);
                    break;
                }
                case CommandKind.CancelConstruction:
                    _constructionSystem?.CancelConstruction(ParseSingleEntityId(record));
                    break;
                case CommandKind.Sell:
                    _constructionSystem?.SellBuilding(ParseSingleEntityId(record));
                    break;
                case CommandKind.Repair:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!RepairPayload.TryParse(ref reader, out RepairPayload repair))
                    {
                        throw new InvalidOperationException("Sealed Repair payload failed to parse.");
                    }
                    for (int i = 0; i < repair.EntityIds.Length; i++)
                    {
                        _constructionSystem?.AssignRepairOrder(repair.EntityIds[i], repair.TargetEntityId);
                    }
                    break;
                }
                case CommandKind.QueueUnit:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!QueueUnitPayload.TryParse(ref reader, out QueueUnitPayload queue))
                    {
                        throw new InvalidOperationException("Sealed QueueUnit payload failed to parse.");
                    }
                    _productionSystem?.TryQueueUnit(
                        record.PlayerSlot, queue.BuildingEntityId, queue.UnitDefId, queue.Count);
                    break;
                }
                case CommandKind.CancelProduction:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!CancelProductionPayload.TryParse(ref reader, out CancelProductionPayload cancel))
                    {
                        throw new InvalidOperationException("Sealed CancelProduction payload failed to parse.");
                    }
                    _productionSystem?.CancelProduction(cancel.BuildingEntityId, cancel.QueueIndex);
                    break;
                }
                case CommandKind.SetRallyPoint:
                {
                    var reader = new CommandPayloadReader(record.Payload.Span);
                    if (!SetRallyPointPayload.TryParse(ref reader, out SetRallyPointPayload rally))
                    {
                        throw new InvalidOperationException("Sealed SetRallyPoint payload failed to parse.");
                    }
                    _productionSystem?.SetRallyPoint(rally.BuildingEntityId, rally.TargetX, rally.TargetY);
                    break;
                }
                default:
                    // No executable domain state for this kind in this slice
                    // (InstallDefenseModule is G2/G4 content and was already
                    // rejected by the domain check): deliberately no mutation.
                    break;
            }
        }

        private static uint ParseSingleEntityId(in CommandRecord record)
        {
            // CancelConstruction/Sell payloads are a single uint32 entity id.
            var reader = new CommandPayloadReader(record.Payload.Span);
            if (!reader.TryReadUInt32(out uint rawId))
            {
                throw new InvalidOperationException("Sealed single-entity payload failed to parse.");
            }
            return rawId;
        }

        private void ApplyMove(in MovePayload move)
        {
            // Canonical world-to-grid mapping: floor, also for negative values
            // (SimulationCore.md section 1), clamped into the map.
            int gridX = SimMath.Clamp(SimFixed.WorldToGrid(move.TargetX), 0, _pathfindingSystem.CostField.Width - 1);
            int gridY = SimMath.Clamp(SimFixed.WorldToGrid(move.TargetY), 0, _pathfindingSystem.CostField.Height - 1);
            var target = new GridPos2D(gridX, gridY);

            _pathfindingSystem.RequestFlowField(target);

            for (int i = 0; i < move.EntityIds.Length; i++)
            {
                EntityId id = ToEntityId(move.EntityIds[i]);
                if (_entityManager.IsValid(id))
                {
                    _entityManager.GetUnitRef(id).SetTarget(target);
                }
            }
        }
    }
}
