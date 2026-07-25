namespace Nova.Simulation.Replays
{
    /// <summary>
    /// Deterministic rejection codes of the replay parser
    /// (docs/tech/SimulationCore.md sections 7.4 and 8). Every malformed
    /// input maps to exactly one code; the parser never throws on bad input
    /// and validates every length before any allocation.
    /// </summary>
    public enum ReplayReadError
    {
        /// <summary>Parse succeeded.</summary>
        None = 0,

        /// <summary>
        /// File exceeds the hard cap; rejected before the payload parse and
        /// before any allocation driven by file content.
        /// </summary>
        FileTooLarge,

        /// <summary>Fewer bytes than the fixed header size.</summary>
        TruncatedHeader,

        /// <summary>Magic bytes are not ASCII "NOVAPLAY".</summary>
        BadMagic,

        /// <summary>FormatVersion is not a version this reader supports.</summary>
        UnsupportedFormatVersion,

        /// <summary>
        /// Fingerprint length is zero, beyond the parser bound or beyond the
        /// remaining bytes; rejected before allocation.
        /// </summary>
        FingerprintLengthInvalid,

        /// <summary>The fingerprint bytes do not parse as a canonical fingerprint.</summary>
        FingerprintMalformed,

        /// <summary>
        /// Initial snapshot length is zero, beyond the snapshot container cap
        /// or beyond the remaining bytes; rejected before allocation.
        /// </summary>
        SnapshotLengthInvalid,

        /// <summary>The embedded initial snapshot fails the hardened container parse.</summary>
        SnapshotMalformed,

        /// <summary>
        /// The embedded snapshot's header state hash differs from the
        /// fingerprint's InitialStateHash; the replay is internally
        /// inconsistent.
        /// </summary>
        FingerprintSnapshotMismatch,

        /// <summary>
        /// TickCount cannot fit the remaining bytes even as empty frames;
        /// rejected arithmetically before any frame allocation.
        /// </summary>
        TickCountInvalid,

        /// <summary>A tick frame or one of its records ends early.</summary>
        FrameTruncated,

        /// <summary>Frame ticks are not strictly consecutive (+1 per frame).</summary>
        NonConsecutiveTicks,

        /// <summary>RecordCount exceeds MaxBatchRecordsPerTick (256).</summary>
        RecordCountExceeded,

        /// <summary>
        /// A record's outer length prefix is below the record header size,
        /// above MaxRecordBytes or beyond the remaining bytes; rejected
        /// before allocation.
        /// </summary>
        RecordLengthInvalid,

        /// <summary>The record bytes fail the canonical record parse.</summary>
        RecordMalformed,

        /// <summary>
        /// The record fails structural stream validation (Commands.md
        /// section 4): structurally invalid commands never enter the
        /// canonical stream, so they must never appear in a replay.
        /// </summary>
        StructurallyInvalidRecord,

        /// <summary>Records of one frame are not in strictly ascending canonical order.</summary>
        NonCanonicalRecordOrder,

        /// <summary>A record's TargetTick differs from its frame tick.</summary>
        RecordTargetTickMismatch,

        /// <summary>A recorded result code is not a defined CommandResultCode.</summary>
        UnknownResultCode,

        /// <summary>
        /// The recomputed chain after a tick differs from the chain value
        /// stored in that frame; the parser verifies incrementally, so the
        /// first mismatching frame is the first tampered tick.
        /// </summary>
        ChainMismatch,

        /// <summary>Fewer bytes than the trailer size after the last frame.</summary>
        TruncatedTrailer,

        /// <summary>The recomputed final chain hash differs from the trailer.</summary>
        FinalChainMismatch,

        /// <summary>Bytes follow the trailer; the canonical file must end exactly.</summary>
        TrailingBytes,
    }
}
