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
    /// (2) AUTO-ACQUISITION (D-087): every armed entity without a valid
    /// attack order picks the nearest hostile, visible, in-range target —
    /// buildings included; explicit orders are never retargeted;
    /// (3) every unit with an <see cref="UnitState.AttackTarget"/> validates
    /// its target — a dead/despawned target is cleared from the order; a
    /// living target must be in range AND <see cref="VisionState.Visible"/>
    /// in the committed team view before a shot is legal;
    /// (4) a unit whose cooldown reached zero fires: the damage is applied
    /// instantly (MS-1 hitscan — no projectiles, no flight time, no splash)
    /// and the cooldown restarts; a target at or below zero health dies
    /// deterministically via <see cref="EntityManager.DespawnUnit"/> and the
    /// dead id is cleared from every unit's attack order in the same tick.
    /// </para>
    /// <para>
    /// DAMAGE MODEL (this is what a shot is worth, as opposed to when it is
    /// scheduled): every entity's weapon values — base damage, range, cooldown
    /// and <see cref="Combat.DamageType"/> — and its own
    /// <see cref="Combat.ArmorClass"/> come from its role's
    /// <see cref="WeaponProfile"/>, i.e. from the content definitions in
    /// <see cref="Definitions.SimDefinitions"/>. The landed damage is
    /// <c>DamageMatrix.Resolve(profile.AttackDamage, profile.DamageType,
    /// targetProfile.ArmorClass)</c> — an integer percent multiplier, no
    /// floats, no fixed-point multiply. A role with base damage 0 is unarmed
    /// and never fires at all: Builder, Harvester and the eight non-defensive
    /// buildings hold their attack order forever, while the DefensePlatform
    /// shoots like any unit because buildings CAN shoot.
    /// </para>
    /// <para>
    /// Duel asymmetry (review finding): because engagements run in ascending
    /// entity-index order and death takes effect immediately, mutual kills
    /// inside one tick are won by the unit with the LOWER index — spawn order
    /// decides an even duel. This is deterministic and spec-conform
    /// (SimulationCore.md section 2 phase order) but balance-relevant and
    /// therefore stated explicitly.
    /// </para>
    /// <para>
    /// Range rule (simplest documented rule, boundary inclusive): the
    /// center-to-center distance must satisfy
    /// <c>dist &lt;= attackerWeaponRange + target.Radius</c> — edge-adjusted on
    /// the target side only, the attacker's own radius is ignored. The
    /// range is the attacker role's, so an Artillery (20 m) outranges a
    /// BasicInfantry (7 m) on the same board. The comparison
    /// is exact fixed-point in widened Q32.32 long arithmetic; a squared
    /// distance can overflow only beyond ~46 km, far outside any map domain.
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
    /// Per-unit-type weapons from docs/gamedesign/Weapons.md ARE wired now —
    /// the flat 15-damage-for-everyone placeholder is gone. What remains
    /// provisional: same-team attack orders are not filtered in this slice
    /// (command-side validation is a Q-040 candidate); the visibility rule
    /// applies to every target alike.
    /// </para>
    /// </summary>
    public sealed class CombatSystem : ISimSystem
    {
        // The three Default* members below are NOT the damage model any more.
        // They are named aliases of the generic-role fallback profile
        // (WeaponProfiles.Fallback, applied only to UnitRole.Unit — see the
        // remarks there), kept because the canonical combat suites in both
        // test lanes express their expectations through them. Nothing on the
        // content path reads them: a role-carrying entity resolves through
        // WeaponProfiles.Get, and the values below are simply that role's
        // numbers restated. They are deliberately not [Obsolete]: an alias
        // that is still the correct answer for the role it describes is not
        // deprecated, and marking it so would only spray warnings across two
        // test suites that this sprint's write scope forbids editing.

        /// <summary>Weapon range in meters of the generic-role fallback profile (alias of <see cref="WeaponProfiles.FallbackAttackRangeTiles"/>).</summary>
        public static readonly SimFixed DefaultWeaponRange = SimFixed.FromInt(WeaponProfiles.FallbackAttackRangeTiles);

        /// <summary>Damage per shot of the generic-role fallback profile (alias of <see cref="WeaponProfiles.FallbackAttackDamage"/>).</summary>
        public const int DefaultWeaponDamage = WeaponProfiles.FallbackAttackDamage;

        /// <summary>Firing interval in ticks of the generic-role fallback profile (alias of <see cref="WeaponProfiles.FallbackAttackCooldownTicks"/>).</summary>
        public const int DefaultCooldownTicks = WeaponProfiles.FallbackAttackCooldownTicks;

        private readonly EntityManager _entityManager;
        private readonly FogOfWarSystem _fogOfWar;
        private readonly ISlotFactionLookup _factions;

        public string Name => "CombatSystem";

        /// <summary>
        /// Combat needs the slot factions because weapon profiles are
        /// faction-resolved (faction identity slice): the same role fights
        /// with different numbers depending on the owner slot's faction.
        /// The lookup is the economy state — the single home of the faction
        /// assignment — injected at construction like the FoW reference.
        /// </summary>
        public CombatSystem(EntityManager entityManager, FogOfWarSystem fogOfWar, ISlotFactionLookup factions)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _fogOfWar = fogOfWar ?? throw new ArgumentNullException(nameof(fogOfWar));
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
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

            // Phase 2 (D-087): auto-acquisition. Every active entity WITHOUT
            // a valid attack order picks the NEAREST hostile, visible,
            // in-range target — buildings included, so the DefensePlatform
            // finally fires (it is armed by definition but could never
            // receive an explicit order). Unarmed roles (damage 0) skip.
            // Deterministic: strict ascending scans, squared fixed-point
            // distances in widened long arithmetic, lowest entity index wins
            // ties (strict less-than keeps the earliest candidate). A unit
            // with a HELD explicit order (out of range / hidden target) is
            // never retargeted by this phase.
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState attacker = ref units[i];
                if (!attacker.IsActive || attacker.AttackTarget.IsValid) continue;

                WeaponProfile weapon = WeaponProfiles.Get(_factions.GetSlotFaction(attacker.PlayerId), attacker.Role);
                if (!weapon.IsArmed) continue;
                if (attacker.PlayerId >= _fogOfWar.TeamCount) continue;
                TeamView view = _fogOfWar.GetTeamView(attacker.PlayerId);

                EntityId best = EntityId.Invalid;
                long bestDistanceSquared = long.MaxValue;
                for (int c = 0; c < capacity; c++)
                {
                    ref readonly UnitState candidate = ref units[c];
                    if (!candidate.IsActive || candidate.PlayerId == attacker.PlayerId) continue;
                    if (!IsInRange(in attacker, in candidate, weapon.AttackRange)) continue;
                    if (!IsVisibleToAttacker(view, in candidate)) continue;

                    long dx = (long)attacker.Transform.PositionX.RawValue - candidate.Transform.PositionX.RawValue;
                    long dy = (long)attacker.Transform.PositionY.RawValue - candidate.Transform.PositionY.RawValue;
                    long distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared >= bestDistanceSquared) continue; // equal distance: lowest index already held

                    bestDistanceSquared = distanceSquared;
                    best = candidate.Id;
                }

                if (best.IsValid)
                {
                    attacker.AttackTarget = best;
                }
            }

            // Phase 3: engagements in strict ascending entity-index order.
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

                // The attacker's own faction and role decide damage, type,
                // range and cadence. An unarmed role (base damage 0) holds
                // its order exactly like an out-of-range one — it simply has
                // nothing to fire, so it never starts a cooldown either.
                WeaponProfile weapon = WeaponProfiles.Get(_factions.GetSlotFaction(attacker.PlayerId), attacker.Role);
                if (!weapon.IsArmed) continue;

                // Legality: range AND committed visibility. A living target
                // that fails either check is held, never dropped.
                if (!IsInRange(in attacker, in target, weapon.AttackRange)) continue;
                if (!IsVisibleToAttacker(in attacker, in target)) continue;

                if (attacker.WeaponCooldownTicks != 0) continue;

                // MS-1 hitscan: damage lands in the same tick, no projectile.
                // The counter table is applied here and nowhere else —
                // attacker damage type versus target armor class, integer
                // percent, truncating once against the untouched base value.
                int damage = DamageMatrix.Resolve(
                    weapon.AttackDamage,
                    weapon.DamageType,
                    WeaponProfiles.GetArmorClass(_factions.GetSlotFaction(target.PlayerId), target.Role));

                target.CurrentHealth -= damage;
                attacker.WeaponCooldownTicks = weapon.AttackCooldownTicks;

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
        /// <paramref name="weaponRange"/> is the ATTACKER role's range, so the
        /// same call answers for a 7 m rifle and a 20 m artillery piece.
        /// </summary>
        private static bool IsInRange(in UnitState attacker, in UnitState target, SimFixed weaponRange)
        {
            long dx = (long)attacker.Transform.PositionX.RawValue - target.Transform.PositionX.RawValue;
            long dy = (long)attacker.Transform.PositionY.RawValue - target.Transform.PositionY.RawValue;
            long distanceSquared = dx * dx + dy * dy;

            SimFixed reach = weaponRange + target.Radius;
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
            return IsVisibleToAttacker(view, in target);
        }

        /// <summary>
        /// The cell check over an already-resolved committed view — the
        /// auto-acquisition phase fetches the attacker's view once per
        /// attacker instead of once per candidate.
        /// </summary>
        private static bool IsVisibleToAttacker(TeamView view, in UnitState target)
        {
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
