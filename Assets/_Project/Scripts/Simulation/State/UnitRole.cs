namespace Nova.Simulation.State
{
    /// <summary>
    /// Provisional runtime role of a simulation entity (Q-040 candidate).
    /// The MS-1 content model (quality/content/mvp-v1.json) knows buildings
    /// and units by role, but no canonical building model exists yet; the
    /// documented minimal variant of this slice models buildings as entities
    /// carrying one of these roles. The economy derives power provided /
    /// required from the building roles; harvest orders are only effective
    /// for <see cref="Harvester"/>. Values are stable wire identifiers of
    /// the entity store block v4 — renaming a member never changes the wire
    /// value.
    /// </summary>
    public enum UnitRole : byte
    {
        /// <summary>Plain mobile unit without an economic or building function.</summary>
        Unit = 0,

        /// <summary>Construction unit (construction domain slice; no canonical behavior here).</summary>
        Builder = 1,

        /// <summary>Resource collector; the only role the economy's harvest orders apply to.</summary>
        Harvester = 2,

        /// <summary>Headquarters building; provides a provisional base amount of power.</summary>
        HQ = 3,

        /// <summary>Refinery building; the cargo drop-off point of the harvest cycle.</summary>
        Refinery = 4,

        /// <summary>Power plant building; provides power to its owner's grid.</summary>
        Power = 5,
    }
}
