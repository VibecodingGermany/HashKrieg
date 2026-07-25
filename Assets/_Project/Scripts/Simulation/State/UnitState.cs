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
        public Transform2D Transform;
        public SimFixed MoveSpeed;
        public SimFixed Radius;

        /// <summary>
        /// Authoritative sight radius in meters (docs/tech/FogOfWar.md
        /// section 3, MS-1: pure radii). Drives the canonical Fog of War
        /// recompute, so it is part of the hashed/serialized unit state.
        /// </summary>
        public SimFixed SightRadius;
        public GridPos2D TargetGridPos;
        public int CurrentHealth;
        public int MaxHealth;
        public EntityId AttackTarget;
        public int WeaponCooldownTicks;
        public bool IsActive;
        public bool IsMoving;

        public UnitState(
            EntityId id,
            byte playerId,
            Transform2D transform,
            SimFixed moveSpeed,
            SimFixed? radius = null,
            int maxHealth = 100,
            SimFixed? sightRadius = null)
        {
            Id = id;
            PlayerId = playerId;
            Transform = transform;
            MoveSpeed = moveSpeed;
            Radius = radius ?? SimFixed.FromRaw(SimFixed.OneRaw / 2); // default 0.5 m
            SightRadius = sightRadius ?? DefaultSightRadius;
            CurrentHealth = maxHealth;
            MaxHealth = maxHealth;
            AttackTarget = EntityId.Invalid;
            WeaponCooldownTicks = 0;
            TargetGridPos = GridPos2D.Invalid;
            IsActive = true;
            IsMoving = false;
        }

        public void SetTarget(GridPos2D target)
        {
            TargetGridPos = target;
            IsMoving = target.IsValid;
        }

        public void Stop()
        {
            TargetGridPos = GridPos2D.Invalid;
            IsMoving = false;
        }
    }
}
