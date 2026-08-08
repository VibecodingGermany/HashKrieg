namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Intent-submission and structural intake rejection reasons
    /// (docs/tech/Commands.md section 4). Every rejection happens before the
    /// command enters the canonical stream and is never recorded. Structural
    /// values are deterministic for a given input; submission readiness also
    /// exposes the host lifecycle without consuming a sequence.
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

        // Bytes after a fully parsed record (the intake accepts exactly one).
        TrailingBytes = 20,

        // Submission boundary (before session actions or sequence assignment).
        TransportNotReady = 21,
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
