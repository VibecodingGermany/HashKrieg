using System;
using Nova.Core;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.Simulation.Combat
{
    /// <summary>
    /// Canonical combat system (docs/tech/SimulationCore.md section 2, order
    /// step 8 — after Movement and the Fog of War commit): direct
    /// unit-versus-unit hitscan fire on the canonical 10 Hz clock. Zero
    /// engine dependencies, pure fixed-point/integer arithmetic
    /// (SimulationCore.md sections 1 and 9).
    /// <para>
    /// Tick logic (deterministic, strict ascending entity-index order):
    /// (1) every living unit's weapon cooldown decrements by one tick;
    /// (2) every unit with an <see cref="UnitState.AttackTarget"/> validates
    /// its target — a dead/despawned target is cleared from the order; a
    /// living target must be in range AND <see cref="VisionState.Visible"/>
    /// in the committed team view before a shot is legal;
    /// (3) a unit whose cooldown reached zero fires: the damage is applied
    /// instantly (MS-1 hitscan — no projectiles, no flight time, no splash)
    /// and the cooldown restarts; a target at or below zero health dies
    /// deterministically via <see cref="EntityManager.DespawnUnit"/> and the
    /// dead id is cleared from every unit's attack order in the same tick.
    /// </para>
    /// <para>
    /// Range rule (simplest documented rule, boundary inclusive): the
    /// center-to-center distance must satisfy
    /// <c>dist &lt;= WeaponRange + target.Radius</c> — edge-adjusted on the
    /// target side only, the attacker's own radius is ignored. The comparison
    /// is exact fixed-point in widened Q32.32 long arithmetic, so no squared
    /// distance can overflow the Q16.16 domain.
    /// </para>
    /// <para>
    /// Targeting permission (docs/tech/FogOfWar.md sections 2 and 3): the
    /// target's canonical grid cell must be <see cref="VisionState.Visible"/>
    /// in the COMMITTED view of the attacking team — Explored is not enough
    /// and a radar ping grants no targeting right. Between two 5 Hz
    /// recomputes the last committed mask governs, so a target that leaves
    /// live sight stays engaged until the next commit demotes its cell.
    /// Team index equals the player slot (MS-1, D-058); a unit whose slot
    /// has no committed team view cannot legally fire and holds its target.
    /// An out-of-range or hidden-but-living target is HELD, not dropped —
    /// closing the distance is Movement's concern, not this slice's.
    /// </para>
    /// <para>
    /// State: this system intentionally does NOT implement
    /// <see cref="IStatefulSimSystem"/>. Every authoritative combat value —
    /// health, cooldown ticks and attack orders — lives in the entity store
    /// (<see cref="UnitState"/>) and serializes into snapshot block
    /// <see cref="Snapshots.SnapshotBlockIds.EntityStore"/> via the movement
    /// system's delegation. Hitscan is instantaneous, so combat owns no
    /// pending state of its own (projectiles would be the first own block);
    /// a stateless system satisfies the kernel registration checklist, which
    /// only forces state-HOLDING systems to be stateful.
    /// </para>
    /// <para>
    /// Fog of War wiring: the kernel offers no cross-system lookup, so the
    /// host injects the <see cref="FogOfWarSystem"/> reference at
    /// construction time (same pattern as Movement receiving the entity
    /// store and pathfinding) and registers combat AFTER the FoW system —
    /// registration order is the canonical tick order (SimulationCore.md
    /// section 2). Combat reads exclusively
    /// <see cref="FogOfWarSystem.GetTeamView"/>; no provisional or
    /// self-computed sight exists on this path.
    /// </para>
    /// <para>
    /// Provisional content values (Q-040 candidates, not ratified balancing):
    /// weapon range 8 m, damage 15, cooldown
    /// <see cref="DefaultCooldownTicks"/> = 5 ticks — 0.5 s on the canonical
    /// 10 Hz clock (the retired prototype's "10 ticks = 0.5 sec" was a
    /// 20 Hz relic). Per-unit-type weapons from docs/gamedesign/Weapons.md
    /// are content definitions and not yet wired. Same-team attack orders
    /// are not filtered in this slice (command-side validation is a Q-040
    /// candidate); the visibility rule applies to every target alike.
    /// </para>
    /// </summary>
    public sealed class CombatSystem : ISimSystem
    {
        /// <summary>Provisional default weapon range in meters (Q-040 candidate; see class remarks).</summary>
        public static readonly SimFixed DefaultWeaponRange = SimFixed.FromInt(8);

        /// <summary>Provisional default weapon damage per shot (Q-040 candidate; see class remarks).</summary>
        public const int DefaultWeaponDamage = 15;

        /// <summary>Provisional firing interval in whole ticks: 5 ticks = 0.5 s at the canonical 10 Hz (Q-040 candidate).</summary>
        public const int DefaultCooldownTicks = 5;

        private readonly EntityManager _entityManager;
        private readonly FogOfWarSystem _fogOfWar;

        public string Name => "CombatSystem";

        public CombatSystem(EntityManager entityManager, FogOfWarSystem fogOfWar)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _fogOfWar = fogOfWar ?? throw new ArgumentNullException(nameof(fogOfWar));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized canonical combat (hitscan, FoW-gated).");
        }

        public void ExecuteTick(Tick tick)
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;

            // Phase 1: cooldowns tick down for every living unit.
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState unit = ref units[i];
                if (!unit.IsActive) continue;
                if (unit.WeaponCooldownTicks > 0)
                {
                    unit.WeaponCooldownTicks--;
                }
            }

            // Phase 2: engagements in strict ascending entity-index order.
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState attacker = ref units[i];
                if (!attacker.IsActive || !attacker.AttackTarget.IsValid) continue;

                EntityId targetId = attacker.AttackTarget;

                // A dead/despawned target drops out of the order immediately.
                if (!_entityManager.IsValid(targetId))
                {
                    attacker.AttackTarget = EntityId.Invalid;
                    continue;
                }

                ref UnitState target = ref _entityManager.GetUnitRef(targetId);

                // Legality: range AND committed visibility. A living target
                // that fails either check is held, never dropped.
                if (!IsInRange(in attacker, in target)) continue;
                if (!IsVisibleToAttacker(in attacker, in target)) continue;

                if (attacker.WeaponCooldownTicks != 0) continue;

                // MS-1 hitscan: damage lands in the same tick, no projectile.
                target.CurrentHealth -= DefaultWeaponDamage;
                attacker.WeaponCooldownTicks = DefaultCooldownTicks;

                if (target.CurrentHealth <= 0)
                {
                    KillUnit(targetId);
                }
            }
        }

        public void Shutdown()
        {
        }

        /// <summary>
        /// Exact range test (boundary inclusive): center distance squared
        /// &lt;= (range + target radius)^2, computed in widened Q32.32 long
        /// arithmetic so the comparison can never overflow the Q16.16 domain.
        /// </summary>
        private static bool IsInRange(in UnitState attacker, in UnitState target)
        {
            long dx = (long)attacker.Transform.PositionX.RawValue - target.Transform.PositionX.RawValue;
            long dy = (long)attacker.Transform.PositionY.RawValue - target.Transform.PositionY.RawValue;
            long distanceSquared = dx * dx + dy * dy;

            SimFixed reach = DefaultWeaponRange + target.Radius;
            long reachSquared = (long)reach.RawValue * reach.RawValue;
            return distanceSquared <= reachSquared;
        }

        /// <summary>
        /// Targeting permission: the target's canonical grid cell (floor,
        /// clamped to the FoW grid) is <see cref="VisionState.Visible"/> in
        /// the committed view of the attacker's team. A slot without a
        /// committed team view (MS-1: team index == player slot) has no
        /// legal shots; radar pings and Explored cells grant none either.
        /// </summary>
        private bool IsVisibleToAttacker(in UnitState attacker, in UnitState target)
        {
            if (attacker.PlayerId >= _fogOfWar.TeamCount)
            {
                return false;
            }
            TeamView view = _fogOfWar.GetTeamView(attacker.PlayerId);
            int gx = Math.Max(0, Math.Min(view.Width - 1, SimFixed.WorldToGrid(target.Transform.PositionX)));
            int gy = Math.Max(0, Math.Min(view.Height - 1, SimFixed.WorldToGrid(target.Transform.PositionY)));
            return view.IsVisible(gx, gy);
        }

        /// <summary>
        /// Deterministic kill: the unit despawns and every attack order on
        /// the dead id resolves in the same tick (ascending index sweep), so
        /// no unit keeps firing at or chasing a corpse.
        /// </summary>
        private void KillUnit(EntityId deadId)
        {
            _entityManager.DespawnUnit(deadId);

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState unit = ref units[i];
                if (unit.IsActive && unit.AttackTarget == deadId)
                {
                    unit.AttackTarget = EntityId.Invalid;
                }
            }
        }
    }
}
