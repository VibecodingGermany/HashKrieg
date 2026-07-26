namespace Nova.Simulation.State
{
    /// <summary>
    /// Read access to the faction assignment of the player slots — the
    /// single source is the economy state (economy snapshot block v2), so
    /// systems that resolve faction-differentiated content (weapon profiles,
    /// definition tables) depend on this narrow view instead of the full
    /// economy system. Implemented by
    /// <see cref="Nova.Simulation.Economy.EconomySystem"/>.
    /// </summary>
    public interface ISlotFactionLookup
    {
        /// <summary>The faction one slot plays. Slots outside the reserved range are a programming error and may throw.</summary>
        FactionId GetSlotFaction(byte playerSlot);
    }
}
