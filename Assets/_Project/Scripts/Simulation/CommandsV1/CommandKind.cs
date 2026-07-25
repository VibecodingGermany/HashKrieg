namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Frozen numeric command register of schema v1 (docs/tech/Commands.md
    /// section 5). Values are stable identifiers; renaming a member never
    /// changes the wire value, adding a command after the G1 freeze requires a
    /// new value plus compatibility and golden-byte tests.
    /// <para>
    /// Value 0 is reserved and never valid. Non-activated abilities (research,
    /// capture, air, walls, superweapons) deliberately have no register entry
    /// in schema v1 and therefore no executable payload.
    /// </para>
    /// <para>
    /// <see cref="PauseRequest"/>, <see cref="UnpauseRequest"/>,
    /// <see cref="SaveRequest"/> and <see cref="LoadRequest"/> are versioned
    /// session actions (Commands.md section 5). They are never sealed into the
    /// canonical simulation record stream and are only executed at a completed
    /// tick boundary by the host.
    /// </para>
    /// </summary>
    public enum CommandKind : ushort
    {
        Move = 1,
        Stop = 2,
        AttackTarget = 3,
        Harvest = 4,
        ReturnCargo = 5,
        PlaceBuilding = 6,
        CancelConstruction = 7,
        Repair = 8,
        Sell = 9,
        QueueUnit = 10,
        CancelProduction = 11,
        SetRallyPoint = 12,
        InstallDefenseModule = 13,

        // Session actions: outside the canonical tick-mutation stream.
        PauseRequest = 14,
        UnpauseRequest = 15,
        SaveRequest = 16,
        LoadRequest = 17,
    }

    /// <summary>Classification helpers for the frozen <see cref="CommandKind"/> register.</summary>
    public static class CommandKindInfo
    {
        /// <summary>True when the value is a known register entry of schema v1.</summary>
        public static bool IsKnown(CommandKind kind)
        {
            return kind >= CommandKind.Move && kind <= CommandKind.LoadRequest;
        }

        /// <summary>
        /// True when the kind is a session action (Commands.md section 5) that
        /// must never appear inside the canonical simulation record stream.
        /// </summary>
        public static bool IsSessionAction(CommandKind kind)
        {
            return kind >= CommandKind.PauseRequest && kind <= CommandKind.LoadRequest;
        }

        /// <summary>
        /// True when the kind may be sealed into a <see cref="CommandBatch"/>
        /// of the canonical simulation stream.
        /// </summary>
        public static bool IsStreamKind(CommandKind kind)
        {
            return IsKnown(kind) && !IsSessionAction(kind);
        }
    }
}
