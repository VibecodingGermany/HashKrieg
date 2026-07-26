using System;

namespace Nova.Simulation.Combat
{
    /// <summary>
    /// The canonical damage-versus-armor matrix
    /// (docs/gamedesign/ArmorSystem.md, "Schaden-gegen-Panzerung-Matrix",
    /// Startwerte v0.1): a flat set of 36 numbers indexed by
    /// <see cref="DamageType"/> x <see cref="ArmorClass"/>, the single place
    /// where counter relations live.
    /// <para>
    /// AUTHORITY. ArmorSystem.md is the leading source for this table. The
    /// matrices in docs/gamedesign/Infantry.md (6x4 plus a seventh type
    /// "Kristall") and docs/gamedesign/Vehicles.md (5x4) are drifted derived
    /// summaries that contradict it — Energie vs. Schwer is 0.75 here and 1.25
    /// there, i.e. opposite directions — and are superseded. The grounds are
    /// in the repo, not invented: armor class is a per-entity attribute
    /// ("jede Einheit hat genau eine Klasse") whereas the derived tables'
    /// "vs. Fahrzeug" collapses Light/Medium/Heavy and destroys exactly the
    /// distinction counterplay needs; Vehicles.md's own D-047 note routes
    /// binding weapon values to Weapons.md and counter logic to
    /// ArmorSystem.md; and only ArmorSystem.md is written as the flat,
    /// data-driven 36-number set this class implements.
    /// </para>
    /// <para>
    /// REPRESENTATION: integer PERCENT, not a float and not a
    /// <see cref="Core.SimFixed"/>. 100 means 1.00, 75 means 0.75. Damage
    /// resolves as <c>(baseDamage * percent) / 100</c> in plain int
    /// arithmetic, truncating toward zero — see
    /// <see cref="Resolve(int, DamageType, ArmorClass)"/>. Percent keeps the
    /// authored numbers exactly representable (every documented value is a
    /// whole percent), keeps the arithmetic trivially portable across the
    /// Unity and .NET lanes, and stays inside hard rule 5 (integer or Q16.16
    /// only) without spending a fixed-point multiply per shot.
    /// </para>
    /// <para>
    /// ALLOCATION-FREE: the table is a single <c>static readonly int[]</c>
    /// allocated once at type initialization; every lookup is one bounds check
    /// and one indexed read, with no per-call allocation, no boxing and no
    /// enum-keyed dictionary.
    /// </para>
    /// <para>
    /// OVERFLOW: the widest MS-1 product is 110 (Artillery) x 150 (Fire vs.
    /// Infantry) = 16 500, six orders of magnitude inside int range; a caller
    /// would need a base damage above ~14 million to overflow.
    /// </para>
    /// </summary>
    public static class DamageMatrix
    {
        /// <summary>Number of matrix rows — one per <see cref="DamageType"/>.</summary>
        public const int DamageTypeCount = 6;

        /// <summary>Number of matrix columns — one per <see cref="ArmorClass"/>.</summary>
        public const int ArmorClassCount = 6;

        /// <summary>The neutral multiplier in percent: 100 == 1.00, no counter and no penalty.</summary>
        public const int NeutralPercent = 100;

        /// <summary>The divisor of the percent representation.</summary>
        public const int PercentScale = 100;

        /// <summary>
        /// The 36 canonical multipliers in percent, row-major:
        /// <c>index = (int)damageType * ArmorClassCount + (int)armorClass</c>.
        /// Transcribed verbatim from the ArmorSystem.md table; the column
        /// order is Infanterie, Leicht, Mittel, Schwer, Gebäude, Luft.
        /// </summary>
        private static readonly int[] MultiplierPercents =
        {
            //          Infantry  Light  Medium  Heavy  Building  Air
            /* Kinetic   */ 100,    75,     50,    25,      30,    75,
            /* Energy    */  75,   100,    100,    75,      50,   100,
            /* Explosive */  75,    75,    100,   100,      75,    50,
            /* Fire      */ 150,    75,     50,    25,     100,    25,
            /* Bio       */ 125,    75,     75,    50,      75,    50,
            /* Radiation */ 100,    75,     75,    75,      50,    75,
        };

        /// <summary>
        /// The raw multiplier in percent for one damage type against one armor
        /// class (100 == 1.00). Prefer
        /// <see cref="Resolve(int, DamageType, ArmorClass)"/> on the tick path;
        /// this accessor exists for tooling, UI and the matrix tests.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The enum value is outside the declared range — a programming error
        /// (the entity store rejects unknown role/armor values on the wire),
        /// so it fails loudly instead of silently resolving to some default.
        /// </exception>
        public static int GetMultiplierPercent(DamageType damageType, ArmorClass armorClass)
        {
            int row = (int)damageType;
            int column = (int)armorClass;
            if (row < 0 || row >= DamageTypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(damageType), damageType, "unknown damage type");
            }
            if (column < 0 || column >= ArmorClassCount)
            {
                throw new ArgumentOutOfRangeException(nameof(armorClass), armorClass, "unknown armor class");
            }
            return MultiplierPercents[row * ArmorClassCount + column];
        }

        /// <summary>
        /// THE combat entry point: the effective damage one shot of
        /// <paramref name="baseDamage"/> and <paramref name="damageType"/>
        /// deals to a target of <paramref name="armorClass"/>.
        /// <para>
        /// Integer, truncating toward zero:
        /// <c>(baseDamage * percent) / 100</c>. Truncation is applied ONCE per
        /// shot against the untouched base value, so repeated shots cannot
        /// accumulate rounding drift — N shots always remove exactly
        /// <c>N * Resolve(...)</c> health, never a drifting sum.
        /// </para>
        /// <para>
        /// A non-positive <paramref name="baseDamage"/> resolves to 0. That is
        /// what makes "unarmed" unambiguous: an entity whose definition
        /// carries AttackDamage 0 can never reduce health, whatever multiplier
        /// its nominal damage type would imply, and no negative product can be
        /// truncated asymmetrically (C# truncates toward zero, so a negative
        /// input would round the wrong way).
        /// </para>
        /// </summary>
        public static int Resolve(int baseDamage, DamageType damageType, ArmorClass armorClass)
        {
            if (baseDamage <= 0)
            {
                return 0;
            }
            return (baseDamage * GetMultiplierPercent(damageType, armorClass)) / PercentScale;
        }
    }
}
