using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.AI
{
    /// <summary>
    /// What one entity is worth in a fight, as a single whole number.
    /// <para>
    /// THE FORMULA IS <c>AttackDamage * CurrentHealth / AttackCooldownTicks</c>:
    /// damage times staying power per firing interval. It is the smallest thing
    /// that can tell an Alliance rifleman from a Legion recruit — 100 against
    /// 44 at full health — and a head count cannot, which is the whole reason
    /// this type exists. A wave of twelve Legion recruits carries 44 % of the
    /// attack strength of a wave of twelve Alliance riflemen and today both are
    /// called "a full wave".
    /// </para>
    /// <para>
    /// INTEGER, ONE DIVISION, ONE PINNED TRUNCATION. The division truncates
    /// and the truncation is part of the value: an Alliance LightTank is 962,
    /// not 963. That is deterministic on every machine, which is the only
    /// property the netcode cares about.
    /// <para>
    /// THE GUARD NOW WATCHES THIS DIRECTORY TOO. <c>NoFloatInSimulationTests</c>
    /// scans <c>Scripts/AI</c> and <c>Scripts/AI.Data</c> alongside
    /// <c>Scripts/Core</c> and <c>Scripts/Simulation</c> — in both lanes, the
    /// EditMode mirror and the .NET SimRunner suite the CI actually runs — so
    /// a float under this directory fails the chain exactly like one in the
    /// kernel would.
    /// </para>
    /// </para>
    /// <para>
    /// UNARMED IS ZERO, WITHOUT A SPECIAL CASE. Builders, Harvesters and eight
    /// of the nine building roles carry <c>AttackDamage == 0</c>, so the
    /// product is 0 before the division is even reached. There is no second
    /// flag that could disagree with the number — the same rule
    /// <see cref="WeaponProfile.IsArmed"/> already states.
    /// </para>
    /// <para>
    /// WHAT IS DELIBERATELY NOT IN HERE: the armor class, the counter table and
    /// the weapon range. A strength that resolved the damage matrix would have
    /// to know what it is fighting, and the wave gate asks its question BEFORE
    /// it has an opponent ("is what stands here enough to march?"). Range would
    /// reward the Artillery for a reach the AI cannot use while it walks in one
    /// clump. Both belong to a mixed army — that is, to the vehicles — and not
    /// to an infantry-only wave.
    /// </para>
    /// <para>
    /// WHY THIS LIVES IN <c>Nova.AI</c> AND NOT IN <c>Nova.AI.Data</c>: the
    /// data assembly references <c>Nova.Core</c> and deliberately not
    /// <c>Nova.Simulation</c>, so that a profile can never name a
    /// <see cref="UnitRole"/> and quietly become a second definition table.
    /// The formula needs the role, the faction and the weapon table. Behaviour
    /// in C#, numbers in the profile — the split stays where it was.
    /// </para>
    /// </summary>
    public static class CombatStrength
    {
        /// <summary>
        /// Strength of one entity at the health it has right now. Reads the
        /// definitions through <see cref="WeaponProfiles"/> and changes
        /// nothing; the definition table stays the single source of the
        /// numbers.
        /// </summary>
        /// <param name="faction">Owner faction — the values differ per faction, that is the point.</param>
        /// <param name="role">The entity's role.</param>
        /// <param name="currentHealth">Current health; values at or below 0 score 0.</param>
        public static int Of(FactionId faction, UnitRole role, int currentHealth)
        {
            if (currentHealth <= 0) return 0;

            WeaponProfile weapon = WeaponProfiles.Get(faction, role);

            // Unarmed scores 0, and a role without a firing interval cannot
            // fire either — the guard is against a division by zero, not a
            // judgement about the role.
            if (weapon.AttackDamage <= 0 || weapon.AttackCooldownTicks <= 0) return 0;

            // long for the product only: 110 damage times 1.250 health is far
            // inside int, but the sum over an army is not obliged to stay
            // there, and a strength that overflowed would read as a weak army
            // and launch a wave that should have waited.
            long strength = (long)weapon.AttackDamage * currentHealth / weapon.AttackCooldownTicks;
            return strength > int.MaxValue ? int.MaxValue : (int)strength;
        }

        /// <summary>
        /// Strength of one role at FULL health — what a unit still in
        /// production will be worth when it comes out. The wave gate needs
        /// exactly this to answer "can production still reach the threshold, or
        /// is this everything we are going to get?".
        /// </summary>
        public static int OfFullHealth(FactionId faction, UnitRole role)
        {
            return Of(faction, role, MaxHealthOf(faction, role));
        }

        /// <summary>
        /// Full health of a role from the definition table; 0 for a role the
        /// table does not carry, which then scores 0 strength rather than
        /// throwing at a decision point.
        /// </summary>
        private static int MaxHealthOf(FactionId faction, UnitRole role)
        {
            if (SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition unit)) return unit.MaxHealth;
            if (SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition building)) return building.MaxHealth;
            return 0;
        }
    }
}
