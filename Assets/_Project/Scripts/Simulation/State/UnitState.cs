using Nova.Core;
using Nova.Simulation.Pathfinding;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Represents the runtime simulation state of a single mobile unit.
    /// Contiguous, unboxed struct storage inside EntityManager.
    /// </summary>
    public struct UnitState
    {
        /// <summary>
        /// Provisional default sight radius in meters (Q-040 candidate: the
        /// per-unit-type sight classes of docs/gamedesign/FogOfWar.md are
        /// content definitions and not yet wired; until then every unit
        /// without an explicit radius sees 10 m, matching the retired
        /// prototype constant).
        /// </summary>
        public static readonly SimFixed DefaultSightRadius = SimFixed.FromInt(10);

        public EntityId Id;
        public byte PlayerId;

        /// <summary>
        /// Provisional entity role (see <see cref="UnitRole"/>): drives the
        /// economy's power accounting and harvest eligibility. Part of the
        /// hashed/serialized unit state since entity store block v4.
        /// </summary>
        public UnitRole Role;
        public Transform2D Transform;
        public SimFixed MoveSpeed;
        public SimFixed Radius;

        /// <summary>
        /// Authoritative sight radius in meters (docs/tech/FogOfWar.md
        /// section 3, MS-1: pure radii). Drives the canonical Fog of War
        /// recompute, so it is part of the hashed/serialized unit state.
        /// </summary>
        public SimFixed SightRadius;
        /// <summary>
        /// Flow-field destination of the current movement order: the SHARED
        /// group target the pathfinding cache is keyed by. Distinct from
        /// <see cref="GoalGridPos"/>, the unit's personal arrival cell —
        /// a formation move assigns every unit its own goal cell around one
        /// shared flow destination, so a group move costs exactly one flow
        /// field instead of one per unit.
        /// </summary>
        public GridPos2D TargetGridPos;

        /// <summary>
        /// Personal arrival cell of the current movement order (formation
        /// slot). Equals <see cref="TargetGridPos"/> for a single-unit order;
        /// a group order distributes goal cells over the Chebyshev rings
        /// around the shared target (UnitCommandStateView.ApplyMove). The
        /// movement system's arrival check and its direct-steering fallback
        /// aim at this cell, the flow-field lookup at
        /// <see cref="TargetGridPos"/>.
        /// </summary>
        public GridPos2D GoalGridPos;
        public int CurrentHealth;
        public int MaxHealth;
        public EntityId AttackTarget;
        public int WeaponCooldownTicks;

        /// <summary>
        /// Carried Aetherium in AE (harvesters only). Authoritative economy
        /// state living with the entity — same precedent as
        /// <see cref="AttackTarget"/> — so it serializes into the entity
        /// store block (v4) instead of a separate economy-side table.
        /// </summary>
        public int CargoAE;

        /// <summary>
        /// Standing harvest order: the stable id of the target
        /// <see cref="Economy.AetheriumField"/>; 0 means no order. Applied by
        /// the Harvest command, resolved by the economy system (cargo full or
        /// field exhausted) and cleared by Stop.
        /// </summary>
        public ushort HarvestFieldId;

        /// <summary>
        /// Standing return-cargo order: the unit deposits its cargo at an own
        /// refinery in reach. Applied by the ReturnCargo command, resolved by
        /// the economy system on deposit and cleared by Stop.
        /// </summary>
        public bool IsReturningCargo;
        public bool IsActive;
        public bool IsMoving;

        public UnitState(
            EntityId id,
            byte playerId,
            Transform2D transform,
            SimFixed moveSpeed,
            SimFixed? radius = null,
            int maxHealth = 100,
            SimFixed? sightRadius = null,
            UnitRole role = UnitRole.Unit)
        {
            Id = id;
            PlayerId = playerId;
            Role = role;
            Transform = transform;
            MoveSpeed = moveSpeed;
            Radius = radius ?? SimFixed.FromRaw(SimFixed.OneRaw / 2); // default 0.5 m
            SightRadius = sightRadius ?? DefaultSightRadius;
            CurrentHealth = maxHealth;
            MaxHealth = maxHealth;
            AttackTarget = EntityId.Invalid;
            WeaponCooldownTicks = 0;
            CargoAE = 0;
            HarvestFieldId = 0;
            IsReturningCargo = false;
            TargetGridPos = GridPos2D.Invalid;
            GoalGridPos = GridPos2D.Invalid;
            IsActive = true;
            IsMoving = false;
        }

        /// <summary>Single-cell movement order: flow destination and arrival cell coincide.</summary>
        public void SetTarget(GridPos2D target)
        {
            TargetGridPos = target;
            GoalGridPos = target;
            IsMoving = target.IsValid;
        }

        /// <summary>
        /// Group movement order: <paramref name="flowTarget"/> is the shared
        /// flow-field destination, <paramref name="goalCell"/> this unit's
        /// personal arrival cell from the formation distribution.
        /// </summary>
        public void SetTarget(GridPos2D flowTarget, GridPos2D goalCell)
        {
            TargetGridPos = flowTarget;
            GoalGridPos = goalCell;
            IsMoving = flowTarget.IsValid;
        }

        public void Stop()
        {
            TargetGridPos = GridPos2D.Invalid;
            GoalGridPos = GridPos2D.Invalid;
            IsMoving = false;
        }
    }
}
