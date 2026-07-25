namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Structural rejection reasons (docs/tech/Commands.md section 4). A
    /// structural failure is rejected before the command enters the canonical
    /// stream and is never recorded; the value is deterministic for a given
    /// input and therefore safe to log and test against.
    /// </summary>
    public enum CommandRejectReason : ushort
    {
        None = 0,

        // Parser limits (length checks before allocation).
        RecordLengthInvalid = 1,
        RecordTruncated = 2,

        // Register and version.
        UnknownKind = 3,
        UnknownPayloadVersion = 4,
        SessionActionInStream = 5,

        // Session binding.
        InactiveSlot = 6,

        // Payload content.
        PayloadMalformed = 7,
        InvalidEntityId = 8,
        EmptyEntityList = 9,
        TooManyEntityIds = 10,
        UnsortedEntityList = 11,
        InvalidDefinitionId = 12,
        InvalidCount = 13,

        // Tick window.
        TickWindowViolation = 14,

        // Sequence and dedupe.
        SequenceZero = 15,
        SequenceOverflow = 16,
        DedupeConflict = 17,

        // Backpressure, checked before sealing.
        PendingQueueFull = 18,
        BatchCapacityExceeded = 19,
    }

    /// <summary>Outcome of handing one record to the ingress.</summary>
    public enum CommandIngressResult
    {
        /// <summary>Record entered the canonical pending stream exactly once.</summary>
        Accepted = 0,

        /// <summary>
        /// Byte-identical re-delivery of an already accepted or completed
        /// (PlayerSlot, Sequence); ignored — the command still applies exactly
        /// once (docs/tech/Commands.md section 3).
        /// </summary>
        DuplicateIgnored = 1,

        /// <summary>Structural failure; rejected and not recorded.</summary>
        Rejected = 2,
    }
}
