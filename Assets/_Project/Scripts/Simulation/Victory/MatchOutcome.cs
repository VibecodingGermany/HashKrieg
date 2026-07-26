namespace Nova.Simulation.Victory
{
    /// <summary>
    /// The canonical MS-1 match result codes of the D-056 victory contract
    /// (docs/gamedesign/VictoryConditions.md, section "MS-1-Override"). The
    /// document names exactly three decided results —
    /// <c>Victory.Elimination</c>, <c>Draw.MutualAnnihilation</c> and
    /// <c>Draw.TimeLimit</c> — plus the running match; this enum is that set
    /// and nothing more.
    /// <para>
    /// Values are stable WIRE identifiers of the victory snapshot block
    /// (<see cref="Nova.Simulation.Snapshots.SnapshotBlockIds.Victory"/>) and
    /// therefore of the canonical state hash: renaming a member never changes
    /// its number, and a number is never reused for a different meaning.
    /// Post-MVP result codes (surrender, point victory, survival) get NEW
    /// numbers behind a new D-ID — they must not re-purpose these.
    /// </para>
    /// </summary>
    public enum MatchOutcome : byte
    {
        /// <summary>The match is still running; no side has been eliminated and the time limit has not been reached.</summary>
        Undecided = 0,

        /// <summary>
        /// <c>Victory.Elimination</c>: exactly one engaged side still owns
        /// living entities after at least one other engaged side lost its
        /// last unit, building and construction site. The surviving slot is
        /// <see cref="VictorySystem.WinnerSlot"/>.
        /// </summary>
        VictoryElimination = 1,

        /// <summary>
        /// <c>Draw.MutualAnnihilation</c>: every engaged side was eliminated
        /// in the same tick (D-056; the classic superweapon exchange). No
        /// winner slot.
        /// </summary>
        DrawMutualAnnihilation = 2,

        /// <summary>
        /// <c>Draw.TimeLimit</c>: tick
        /// <see cref="VictorySystem.TimeLimitTick"/> was reached without an
        /// elimination. No winner slot.
        /// </summary>
        DrawTimeLimit = 3,
    }
}
