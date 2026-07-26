namespace Nova.Simulation.Combat
{
    /// <summary>
    /// Canonical damage type of a weapon
    /// (docs/gamedesign/ArmorSystem.md, "Schaden-gegen-Panzerung-Matrix").
    /// One of the two axes of <see cref="DamageMatrix"/>; every armed entity
    /// carries exactly one damage type.
    /// <para>
    /// THE NUMERIC ORDER IS A WIRE CONTRACT. The values are the row index into
    /// the flat 36-entry <see cref="DamageMatrix"/> table and travel with the
    /// content definitions (<see cref="Definitions.SimUnitDefinition"/>,
    /// <see cref="Definitions.SimBuildingDefinition"/>), so renaming a member
    /// is free but renumbering or reordering one silently redefines the whole
    /// counter table — exactly the class of change that desynchronises a
    /// lockstep match between two builds. New damage types append at the end.
    /// </para>
    /// <para>
    /// All six types are declared even though MS-1 only exercises Kinetic and
    /// Explosive: the matrix is a flat set of 36 numbers by design
    /// (ArmorSystem.md, "SO-tauglich"), and re-cutting the table later is far
    /// more expensive than carrying two unused rows now. "Kristall" from
    /// docs/gamedesign/Infantry.md is deliberately NOT a damage type — it is
    /// Evolvierte-only content outside the MVP scope (see the ScopeLedger).
    /// </para>
    /// </summary>
    public enum DamageType : byte
    {
        /// <summary>Kinetisch — guns, autocannons, flak. Full damage to infantry, breaks on heavy armor.</summary>
        Kinetic = 0,

        /// <summary>Energie — the balanced high-tech profile; no hard weakness, but no siege value either.</summary>
        Energy = 1,

        /// <summary>Explosiv — rockets and artillery; the armor and building breaker.</summary>
        Explosive = 2,

        /// <summary>Feuer/Thermobarisch — the infantry killer; useless against heavy armor and air.</summary>
        Fire = 3,

        /// <summary>Bio/Säure — the Evolvierte mirror of Fire, with building relevance.</summary>
        Bio = 4,

        /// <summary>Strahlung — deliberately flat (no value above 1.00); denies zones instead of countering.</summary>
        Radiation = 5,
    }
}
