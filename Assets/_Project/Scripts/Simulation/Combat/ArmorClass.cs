namespace Nova.Simulation.Combat
{
    /// <summary>
    /// Canonical armor class of an entity
    /// (docs/gamedesign/ArmorSystem.md, "Panzerungsklassen"). Every unit and
    /// every building carries exactly ONE class — there is no multi-armor
    /// model — and the class is the column axis of <see cref="DamageMatrix"/>.
    /// <para>
    /// THE NUMERIC ORDER IS A WIRE CONTRACT, for the same reason as
    /// <see cref="DamageType"/>: the values are the column index into the flat
    /// 36-entry matrix and travel with the content definitions. Renaming is
    /// free, renumbering silently redefines every counter relation. New
    /// classes append at the end.
    /// </para>
    /// <para>
    /// MS-1 coverage: <see cref="Infantry"/>, <see cref="Light"/>,
    /// <see cref="Medium"/> and <see cref="Building"/> are carried by real
    /// entities. <see cref="Heavy"/> is reserved for the Heavy Tank and elite
    /// units (ArmorSystem.md, D-015), which are NOT MS-1 content, and
    /// <see cref="Air"/> has no MS-1 carrier either — both columns exist but
    /// are unexercised by the shipped roster. That is a recorded consequence
    /// of following ArmorSystem.md faithfully (Light AND Battle Tank are
    /// Medium), not an oversight: the counter triangle still bites through
    /// Medium (Kinetic 0.50 vs Explosive 1.00) and Building (0.30 vs 0.75).
    /// </para>
    /// </summary>
    public enum ArmorClass : byte
    {
        /// <summary>Infanterie — cheap and vulnerable to fire, bio and kinetic damage.</summary>
        Infantry = 0,

        /// <summary>Leicht — scouts, drones, support vehicles: fast, thinly armored.</summary>
        Light = 1,

        /// <summary>Mittel — the mid-game workhorse; Light Tank AND Battle Tank both sit here (ArmorSystem.md).</summary>
        Medium = 2,

        /// <summary>Schwer — Heavy Tank and elite units; only explosive/energy break it efficiently. No MS-1 carrier.</summary>
        Heavy = 3,

        /// <summary>Gebäude — every building type; stationary, repairable/regenerating.</summary>
        Building = 4,

        /// <summary>Luft — aircraft and flying drones. No MS-1 carrier (no air roster in the MVP set).</summary>
        Air = 5,
    }
}
