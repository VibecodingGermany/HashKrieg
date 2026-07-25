namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Deterministic rejection codes of the snapshot parser
    /// (docs/tech/SimulationCore.md section 7, point 4). Every malformed input
    /// maps to exactly one code; the parser never throws on bad input and
    /// never mutates caller state before full validation succeeds
    /// (Serialization.md section 5).
    /// </summary>
    public enum SnapshotReadError
    {
        /// <summary>Parse succeeded.</summary>
        None = 0,

        /// <summary>
        /// File exceeds the 64 MiB hard cap; rejected before the payload
        /// parse and before any allocation driven by file content.
        /// </summary>
        FileTooLarge,

        /// <summary>Fewer bytes than the fixed header size.</summary>
        TruncatedHeader,

        /// <summary>Magic bytes are not ASCII "NOVASNAP".</summary>
        BadMagic,

        /// <summary>FormatVersion is not a version this reader supports.</summary>
        UnsupportedFormatVersion,

        /// <summary>
        /// BlockCount is zero. A snapshot without state blocks is not a
        /// canonical artifact; the writer refuses to produce one.
        /// </summary>
        EmptyBlockTable,

        /// <summary>The block table does not fit into the remaining bytes.</summary>
        TruncatedBlockTable,

        /// <summary>The same BlockId appears twice in the table.</summary>
        DuplicateBlockId,

        /// <summary>
        /// Table entries are not in strictly ascending BlockId order; only
        /// the canonical ordering is accepted.
        /// </summary>
        NonCanonicalBlockOrder,

        /// <summary>
        /// Declared lengths are inconsistent with the actual file: table
        /// lengths do not sum to PayloadBytes, or header + table + payload
        /// does not equal the exact file size. Covers truncation inside the
        /// payload, trailing bytes, block lengths beyond the remaining bytes
        /// and forged overlong length fields — all detected arithmetically
        /// before any payload byte is parsed or allocated.
        /// </summary>
        PayloadLengthMismatch,

        /// <summary>A block content does not match its table BlockHash.</summary>
        BlockHashMismatch,

        /// <summary>The recomputed canonical state hash differs from the header.</summary>
        StateHashMismatch,
    }
}
