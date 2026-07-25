using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Pathfinding;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Adapter from the canonical command executor's state view
    /// (<see cref="ICommandStateView"/>) to the real unit state of this G1
    /// slice: the <see cref="EntityManager"/> unit store and the
    /// <see cref="PathfindingSystem"/> flow fields. The kernel applies due
    /// sealed batches exclusively through this view in tick phase 1
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
    /// Slice scope: Move, Stop and AttackTarget mutate real unit state. All
    /// other kinds (economy, construction, production, rally, module) have no
    /// canonical domain state yet — those systems stay prototype scaffolding
    /// in this slice — so <see cref="Apply"/> deliberately mutates nothing for
    /// them; the sealed record and its deterministic result stay in the
    /// stream (Commands.md section 4). <see cref="CanAfford"/> is only
    /// consulted for definition-bearing kinds and returns true until the
    /// economy slice wires real costs (Q-040 candidate).
    /// </para>
    /// </summary>
    public sealed class UnitCommandStateView : ICommandStateView
    {
        private readonly EntityManager _entityManager;
        private readonly PathfindingSystem _pathfindingSystem;

        public UnitCommandStateView(EntityManager entityManager, PathfindingSystem pathfindingSystem)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _pathfindingSystem = pathfindingSystem ?? throw new ArgumentNullException(nameof(pathfindingSystem));
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
        /// Always true in this slice: the economy is prototype scaffolding and
        /// no canonical cost state exists yet. Consulted only for
        /// definition-bearing kinds (executor contract).
        /// </summary>
        public bool CanAfford(byte playerSlot, CommandKind kind, ushort definitionId) => true;

        /// <summary>
        /// Applies a sealed, fully checked record. Move/Stop/AttackTarget
        /// mutate the unit store; kinds without canonical domain state in
        /// this slice deliberately mutate nothing (see class remarks).
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
                            _entityManager.GetUnitRef(id).Stop();
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
                default:
                    // No canonical domain state for this kind in the G1 kernel
                    // slice (economy/construction/production stay prototype
                    // scaffolding): deliberately no mutation.
                    break;
            }
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
