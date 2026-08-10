using System;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Simulation.Combat
{
    /// <summary>
    /// The resolved combat attributes of one entity role: what it presents to
    /// incoming fire (<see cref="ArmorClass"/>) and what it deals out
    /// (everything else). A flattened, tick-path-ready view of the
    /// <see cref="SimUnitDefinition"/> / <see cref="SimBuildingDefinition"/>
    /// combat fields — the definitions stay the single source of the numbers,
    /// this struct is just the shape combat wants them in.
    /// </summary>
    public readonly struct WeaponProfile
    {
        /// <summary>Armor class this role presents to incoming fire — the target-side axis of <see cref="DamageMatrix"/>.</summary>
        public ArmorClass ArmorClass { get; }

        /// <summary>Damage type this role deals — the attacker-side axis of <see cref="DamageMatrix"/>. Inert while <see cref="AttackDamage"/> is 0.</summary>
        public DamageType DamageType { get; }

        /// <summary>Base damage per shot BEFORE the armor multiplier; 0 means unarmed (see <see cref="IsArmed"/>).</summary>
        public int AttackDamage { get; }

        /// <summary>Weapon range in meters as Q16.16, converted from the definition's tile value (1 tile == 1 m, D-034/D-047).</summary>
        public SimFixed AttackRange { get; }

        /// <summary>Firing interval in whole ticks at the canonical 10 Hz clock.</summary>
        public int AttackCooldownTicks { get; }

        /// <summary>
        /// THE unambiguous armed/unarmed test: a role is armed exactly when
        /// its base damage is positive. There is no second flag that could
        /// disagree with the number, and an unarmed role can never fire, never
        /// start a cooldown and never reduce a target's health.
        /// </summary>
        public bool IsArmed => AttackDamage > 0;

        public WeaponProfile(
            ArmorClass armorClass, DamageType damageType,
            int attackDamage, SimFixed attackRange, int attackCooldownTicks)
        {
            ArmorClass = armorClass;
            DamageType = damageType;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackCooldownTicks = attackCooldownTicks;
        }
    }

    /// <summary>
    /// Faction-and-role-indexed weapon and armor table: the O(1),
    /// allocation-free bridge from an entity's faction and
    /// <see cref="UnitState.Role"/> to the combat values authored in
    /// <see cref="SimDefinitions"/>. Built once at type initialization into a
    /// dense array indexed by raw <see cref="FactionId"/> then raw
    /// <see cref="UnitRole"/>, so the tick path never scans the definition
    /// arrays and never allocates.
    /// <para>
    /// Buildings and units share this table because the MS-1 entity model
    /// stores both as <see cref="UnitState"/> rows carrying a role
    /// (see <see cref="UnitRole"/>) — one lookup therefore serves an attacking
    /// DefensePlatform and an attacked Barracks alike. The FACTION comes from
    /// the entity's owner slot (<see cref="ISlotFactionLookup"/>), never from
    /// the entity itself: the value exists exactly once, in the economy
    /// state.
    /// </para>
    /// <para>
    /// THE GENERIC <see cref="UnitRole.Unit"/> ROLE keeps the pre-canonical
    /// provisional weapon (15 damage, 8 m, 5 ticks) as its
    /// <see cref="Fallback"/> profile — identical for both factions — with
    /// <see cref="ArmorClass.Infantry"/> armor and
    /// <see cref="DamageType.Kinetic"/> damage, a combination the matrix
    /// scores at exactly 1.00, so a generic-on-generic engagement still
    /// applies its 15 damage unscaled. This is deliberate and load-bearing:
    /// <see cref="UnitRole.Unit"/> is the fallback role of a directly spawned
    /// roleless entity; sites carry their definition role since 16.3 (#44).
    /// The generic role has no content definition of its own, and the canonical combat suites exercise combat
    /// through exactly such roleless units. Every entity that comes out of
    /// production or construction carries a real role and is therefore
    /// governed by the real table — nothing on the content path resolves to
    /// the fallback.
    /// </para>
    /// </summary>
    public static class WeaponProfiles
    {
        /// <summary>Base damage of the generic-role fallback weapon (pre-canonical provisional, retained for roleless entities).</summary>
        public const int FallbackAttackDamage = 15;

        /// <summary>Range in tiles/meters of the generic-role fallback weapon.</summary>
        public const int FallbackAttackRangeTiles = 8;

        /// <summary>Firing interval in ticks of the generic-role fallback weapon: 5 ticks == 0.5 s at 10 Hz.</summary>
        public const int FallbackAttackCooldownTicks = 5;

        /// <summary>Number of table slots per faction: every declared <see cref="UnitRole"/> has one.</summary>
        public const int RoleCount = (int)UnitRole.Artillery + 1;

        /// <summary>Number of declared factions (table's first axis).</summary>
        public const int FactionCount = (int)FactionId.Legion + 1;

        private static readonly WeaponProfile[] ByFactionAndRole = BuildTable();

        /// <summary>
        /// Profile of the generic <see cref="UnitRole.Unit"/> role — see the
        /// class remarks for why it stays armed. Faction-independent.
        /// </summary>
        public static WeaponProfile Fallback => ByFactionAndRole[(int)UnitRole.Unit];

        /// <summary>
        /// The combat profile of one faction's role. Allocation-free: one
        /// bounds check and one indexed read of a struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The faction or role is outside the declared enum range. That is a
        /// programming error — the entity store's snapshot parser already
        /// rejects unknown role bytes on the wire — so it fails loudly rather
        /// than silently giving an unknown entity someone else's weapon.
        /// </exception>
        public static WeaponProfile Get(FactionId faction, UnitRole role)
        {
            int factionIndex = (int)faction;
            int roleIndex = (int)role;
            if (factionIndex < 0 || factionIndex >= FactionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(faction), faction, "unknown faction");
            }
            if (roleIndex < 0 || roleIndex >= RoleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(role), role, "unknown entity role");
            }
            return ByFactionAndRole[factionIndex * RoleCount + roleIndex];
        }

        /// <summary>
        /// Convenience for the target side: the armor class one faction's
        /// role presents to incoming fire.
        /// </summary>
        public static ArmorClass GetArmorClass(FactionId faction, UnitRole role) => Get(faction, role).ArmorClass;

        /// <summary>
        /// Materializes the dense faction-by-role table from
        /// <see cref="SimDefinitions"/>. Every slot is filled exactly once:
        /// per faction the eighteen declared roles are the generic fallback
        /// plus the nine building and eight unit definitions, so no slot can
        /// silently keep a default-initialized (and therefore
        /// Infantry-armored, unarmed) profile.
        /// </summary>
        private static WeaponProfile[] BuildTable()
        {
            var table = new WeaponProfile[FactionCount * RoleCount];

            for (int factionIndex = 0; factionIndex < FactionCount; factionIndex++)
            {
                var faction = (FactionId)factionIndex;
                int rowBase = factionIndex * RoleCount;

                // The generic/roleless fallback (see class remarks) — one
                // profile, stamped identically for every faction.
                table[rowBase + (int)UnitRole.Unit] = new WeaponProfile(
                    ArmorClass.Infantry,
                    DamageType.Kinetic,
                    FallbackAttackDamage,
                    SimFixed.FromInt(FallbackAttackRangeTiles),
                    FallbackAttackCooldownTicks);

                for (int roleIndex = 1; roleIndex < RoleCount; roleIndex++)
                {
                    var role = (UnitRole)roleIndex;

                    if (SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition building))
                    {
                        table[rowBase + roleIndex] = new WeaponProfile(
                            building.ArmorClass,
                            building.DamageType,
                            building.AttackDamage,
                            SimFixed.FromInt(building.AttackRangeTiles),
                            building.AttackCooldownTicks);
                        continue;
                    }

                    if (SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition unit))
                    {
                        table[rowBase + roleIndex] = new WeaponProfile(
                            unit.ArmorClass,
                            unit.DamageType,
                            unit.AttackDamage,
                            SimFixed.FromInt(unit.AttackRangeTiles),
                            unit.AttackCooldownTicks);
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"{faction} UnitRole.{role} has no SimDefinitions entry — the weapon table would be incomplete.");
                }
            }

            return table;
        }
    }
}
