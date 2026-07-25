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
        public EntityId Id;
        public byte PlayerId;
        public Transform2D Transform;
        public SimFixed MoveSpeed;
        public SimFixed Radius;
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
            int maxHealth = 100)
        {
            Id = id;
            PlayerId = playerId;
            Transform = transform;
            MoveSpeed = moveSpeed;
            Radius = radius ?? SimFixed.FromRaw(SimFixed.OneRaw / 2); // default 0.5 m
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
