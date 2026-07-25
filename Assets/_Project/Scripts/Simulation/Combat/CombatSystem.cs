using System;
using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Simulation.Combat
{
    /// <summary>
    /// Deterministic combat simulation system.
    /// Manages weapon firing cooldowns, range checks, damage application, and entity destruction.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class CombatSystem : ISimSystem
    {
        public const float DefaultWeaponRange = 8.0f;
        public const int DefaultWeaponDamage = 15;
        public const int DefaultCooldownTicks = 10; // 0.5 sec firing interval

        private readonly EntityManager _entityManager;

        public string Name => "CombatSystem";

        public CombatSystem(EntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized combat system.");
        }

        public void ExecuteTick(Tick tick)
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref UnitState attacker = ref units[i];
                if (!attacker.IsActive) continue;

                // Decrement weapon cooldown timer
                if (attacker.WeaponCooldownTicks > 0)
                {
                    attacker.WeaponCooldownTicks--;
                }

                // Check active target engagement
                if (!attacker.AttackTarget.IsValid) continue;

                if (!_entityManager.IsValid(attacker.AttackTarget))
                {
                    attacker.AttackTarget = EntityId.Invalid;
                    continue;
                }

                ref UnitState target = ref _entityManager.GetUnitRef(attacker.AttackTarget);
                float distSq = attacker.Transform.DistanceToSquared(in target.Transform);

                // Range check and firing trigger
                if (distSq <= DefaultWeaponRange * DefaultWeaponRange && attacker.WeaponCooldownTicks == 0)
                {
                    target.CurrentHealth -= DefaultWeaponDamage;
                    attacker.WeaponCooldownTicks = DefaultCooldownTicks;

                    // Despawn unit if health depleted
                    if (target.CurrentHealth <= 0)
                    {
                        _entityManager.DespawnUnit(target.Id);
                        attacker.AttackTarget = EntityId.Invalid;
                    }
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
