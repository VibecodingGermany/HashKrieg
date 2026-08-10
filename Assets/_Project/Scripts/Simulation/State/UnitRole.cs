namespace Nova.Simulation.State
{
    /// <summary>
    /// Provisional runtime role of a simulation entity (Q-040 candidate).
    /// The MS-1 content model (quality/content/mvp-v1.json) knows buildings
    /// and units by role, but no canonical building model exists yet; the
    /// documented minimal variant of this slice models buildings as entities
    /// carrying one of these roles. The economy derives power provided /
    /// required from the building roles; harvest orders are only effective
    /// for <see cref="Harvester"/>; a construction site carries its target
    /// building role from creation and is distinguished from a completed
    /// placement by the construction register. Values are stable wire identifiers of the entity
    /// store block v4 — renaming a member never changes the wire value.
    /// <para>
    /// MS-1 roles (mvp-v1.json): nine building roles
    /// (<see cref="HQ"/>, <see cref="Power"/>, <see cref="Refinery"/>,
    /// <see cref="Storage"/>, <see cref="Barracks"/>,
    /// <see cref="VehicleFactory"/>, <see cref="ResearchLab"/>,
    /// <see cref="Radar"/>, <see cref="DefensePlatform"/>) and eight unit
    /// roles (<see cref="Builder"/>, <see cref="Harvester"/>,
    /// <see cref="BasicInfantry"/>, <see cref="AntiArmorInfantry"/>,
    /// <see cref="ScoutVehicle"/>, <see cref="LightTank"/>,
    /// <see cref="BattleTank"/>, <see cref="Artillery"/>).
    /// <see cref="Unit"/> stays the generic fallback role; unfinished sites
    /// have carried their definition role since 16.3 (#44).
    /// </para>
    /// <para>
    /// The seventeen content roles are deliberately numbered 1..17: the
    /// definition id rule of the faction slice builds on it — an Alliance
    /// definition id IS this wire value, the Legion id adds 17
    /// (<see cref="Nova.Simulation.Definitions.SimDefinitions"/>).
    /// </para>
    /// </summary>
    public enum UnitRole : byte
    {
        /// <summary>Plain mobile unit without an economic or building function; sites use their definition role and the construction register.</summary>
        Unit = 0,

        /// <summary>Construction unit; the only role that can repair buildings and progress construction sites.</summary>
        Builder = 1,

        /// <summary>Resource collector; the only role the economy's harvest orders apply to.</summary>
        Harvester = 2,

        /// <summary>Headquarters building; provides a provisional base amount of power and produces Builder/Harvester.</summary>
        HQ = 3,

        /// <summary>Refinery building; the cargo drop-off point of the harvest cycle.</summary>
        Refinery = 4,

        /// <summary>Power plant building; provides power to its owner's grid.</summary>
        Power = 5,

        /// <summary>Storage building (MS-1 role; no canonical behavior beyond power draw yet).</summary>
        Storage = 6,

        /// <summary>Barracks building; produces the infantry unit roles.</summary>
        Barracks = 7,

        /// <summary>Vehicle factory building; produces the vehicle unit roles.</summary>
        VehicleFactory = 8,

        /// <summary>Research lab building; its completion unlocks tier 2 for its owner (MS-1 technology model).</summary>
        ResearchLab = 9,

        /// <summary>Radar building (MS-1 role; radar behavior is the vision domain's, not this slice's).</summary>
        Radar = 10,

        /// <summary>Defense platform building (MS-1 role; defense modules are G2/G4 content).</summary>
        DefensePlatform = 11,

        /// <summary>Tier-1 infantry combat unit.</summary>
        BasicInfantry = 12,

        /// <summary>Tier-2 infantry combat unit (requires the owner's T2 unlock).</summary>
        AntiArmorInfantry = 13,

        /// <summary>Tier-1 scout vehicle.</summary>
        ScoutVehicle = 14,

        /// <summary>Tier-1 light tank.</summary>
        LightTank = 15,

        /// <summary>Tier-2 battle tank (requires the owner's T2 unlock).</summary>
        BattleTank = 16,

        /// <summary>Tier-2 artillery (requires the owner's T2 unlock).</summary>
        Artillery = 17,
    }
}
