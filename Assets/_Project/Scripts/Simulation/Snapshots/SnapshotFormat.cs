using System;

namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Canonical snapshot block container format, version 1
    /// (docs/tech/SimulationCore.md section 7, docs/tech/Serialization.md
    /// sections 2 and 5). This container is the envelope the authoritative
    /// state inventory is serialized into at G1 integration; it owns no state
    /// semantics itself, only blocks, hashes and limits.
    /// <para>
    /// Byte layout (all multi-byte integers little-endian, no padding):
    /// <code>
    /// offset  size  field
    /// 0       8     Magic: ASCII "NOVASNAP"
    /// 8       2     FormatVersion u16 (= 1)
    /// 10      2     BlockCount u16 (>= 1)
    /// 12      4     PayloadBytes u32 (sum of all block content lengths)
    /// 16      8     StateHash u64 (NOVA_STATE_V1 domain, see SnapshotWriter)
    /// 24      ...   block table, BlockCount entries of 14 bytes each:
    ///               BlockId u16, ContentLength u32, BlockHash u64
    /// ...     ...   payload: block contents concatenated in strictly
    ///               ascending BlockId order
    /// </code>
    /// Total file size is exactly <c>24 + 14 * BlockCount + PayloadBytes</c>;
    /// any deviation (truncation, trailing bytes, forged lengths) is a
    /// deterministic rejection, never an exception.
    /// </para>
    /// <para>
    /// Design decisions frozen with format version 1:
    /// - Magic: Serialization.md section 2 requires a file/schema identifier
    ///   but fixes no magic value; ASCII "NOVASNAP" is chosen and frozen here.
    /// - BlockHash is XXH64 seed 0 over the raw block content bytes in the
    ///   NOVA_FILE_V1 domain (SimulationCore.md sections 5 and 7). It is a
    ///   pure content hash; the BlockId binding lives in the state hash.
    /// - StateHash covers BlockId, ContentLength and content bytes per block
    ///   in strictly ascending BlockId order in the NOVA_STATE_V1 domain, so
    ///   any block mutation changes exactly its block hash and the state hash.
    /// - The on-disk table is canonical: strictly ascending BlockIds. A writer
    ///   emits ascending order regardless of insertion order, so serialize →
    ///   deserialize → serialize is byte-identical (SimulationCore.md 7.1).
    /// </para>
    /// </summary>
    public static class SnapshotFormat
    {
        /// <summary>ASCII magic bytes "NOVASNAP" at offset 0.</summary>
        public static readonly byte[] Magic = { 0x4E, 0x4F, 0x56, 0x41, 0x53, 0x4E, 0x41, 0x50 };

        /// <summary>The only format version this implementation reads and writes.</summary>
        public const ushort FormatVersion = 1;

        /// <summary>Fixed header size in bytes (magic through state hash).</summary>
        public const int HeaderBytes = 24;

        /// <summary>Per-entry block table size: BlockId u16 + Length u32 + Hash u64.</summary>
        public const int BlockTableEntryBytes = 14;

        /// <summary>
        /// Uncompressed MS-1 size target (SimulationCore.md section 7,
        /// Serialization.md section 5). Exceeding it is a documented
        /// warning/info signal, never a parse or write failure.
        /// </summary>
        public const int SoftTargetBytes = 4 * 1024 * 1024;

        /// <summary>
        /// Parser hard cap (SimulationCore.md section 7, Serialization.md
        /// section 5): files larger than this are rejected before any header
        /// field beyond the length itself is inspected, and before any
        /// payload parse or allocation happens.
        /// </summary>
        public const int MaxFileBytes = 64 * 1024 * 1024;
    }
}
