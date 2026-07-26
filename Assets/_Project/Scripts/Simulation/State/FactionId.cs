namespace Nova.Simulation.State
{
    /// <summary>
    /// The playable faction of a player slot (quality/content/mvp-v1.json
    /// <c>factions</c>: factions[0] "alliance", factions[1] "legion"). Values
    /// are stable wire identifiers — the byte on the wire IS the manifest
    /// index, exactly like <see cref="UnitRole"/> values are the stable wire
    /// identifiers of the entity store block. Renaming a member never changes
    /// the wire value; reordering or renumbering is a wire break and forbidden
    /// once G1 evidence exists.
    /// <para>
    /// The faction is per SLOT, not per entity: every entity derives its
    /// faction from its owner slot via <see cref="ISlotFactionLookup"/>, so
    /// the value exists exactly once (economy snapshot block) and nothing can
    /// disagree with it. The canonical two-slot match assigns
    /// <see cref="Alliance"/> to slot 0 and <see cref="Legion"/> to slot 1;
    /// unused reserved slots carry <see cref="Alliance"/> as the neutral
    /// default.
    /// </para>
    /// <para>
    /// The Evolved are deliberately absent: they are post-MVP content
    /// (docs/vision/Lore.md) and the prototype
    /// <c>EvolvedFactionSystem</c>/<c>BiomassGrid</c> scaffolding is not part
    /// of the canonical tick order.
    /// </para>
    /// </summary>
    public enum FactionId : byte
    {
        /// <summary>
        /// factions[0] of the MS-1 manifest: expensive, precise, energy-heavy,
        /// longer ranges. Wire value 0 — the neutral default of every slot.
        /// </summary>
        Alliance = 0,

        /// <summary>
        /// factions[1] of the MS-1 manifest: cheap, fast to build, individually
        /// weaker, flame/explosive-flavored. Wire value 1.
        /// </summary>
        Legion = 1,
    }
}
