using System;
using System.Collections.Generic;
using Nova.Core;

namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Deterministic serializer of the canonical snapshot container
    /// (docs/tech/SimulationCore.md section 7; layout documented in
    /// <see cref="SnapshotFormat"/>). Blocks may be added in any order; the
    /// emitted file always carries them in strictly ascending BlockId order,
    /// so the same logical snapshot produces the same bytes regardless of
    /// insertion order and serialize → deserialize → serialize is
    /// byte-identical (SimulationCore.md 7.1).
    /// <para>
    /// Hashing (SimulationCore.md sections 5 and 7):
    /// - BlockHash: XXH64 seed 0 over the raw block content bytes in the
    ///   NOVA_FILE_V1 domain — a pure content hash.
    /// - StateHash: XXH64 seed 0 in the NOVA_STATE_V1 domain over, per block
    ///   in ascending BlockId order: BlockId u16, ContentLength u32, content
    ///   bytes. A one-bit mutation in one block therefore changes exactly
    ///   that block's hash and the state hash, and no other block hash.
    /// </para>
    /// <para>
    /// A snapshot without blocks is not a canonical artifact:
    /// <see cref="ToArray"/> throws <see cref="InvalidOperationException"/>
    /// when empty (a structural error that must never be produced), and the
    /// reader rejects such files with
    /// <see cref="SnapshotReadError.EmptyBlockTable"/>.
    /// </para>
    /// </summary>
    public sealed class SnapshotWriter
    {
        private readonly List<ushort> _ids = new List<ushort>();
        private readonly List<byte[]> _contents = new List<byte[]>();

        /// <summary>Number of blocks added so far.</summary>
        public int BlockCount => _ids.Count;

        /// <summary>
        /// Adds a named block. <paramref name="blockId"/> must be unique;
        /// a duplicate throws <see cref="ArgumentException"/> because a
        /// duplicate id is a structural error and must never be serialized.
        /// The content is copied; later caller mutations cannot leak in.
        /// </summary>
        public void AddBlock(ushort blockId, ReadOnlySpan<byte> content)
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                if (_ids[i] == blockId)
                {
                    throw new ArgumentException(
                        $"Duplicate snapshot BlockId {blockId}.", nameof(blockId));
                }
            }
            _ids.Add(blockId);
            _contents.Add(content.ToArray());
        }

        /// <summary>Adds a named block from a <see cref="SnapshotBlockWriter"/>.</summary>
        public void AddBlock(ushort blockId, SnapshotBlockWriter content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            AddBlock(blockId, content.ToArray());
        }

        /// <summary>
        /// Canonical block content hash: NOVA_FILE_V1 domain over the raw
        /// content bytes (SimulationCore.md sections 5 and 7).
        /// </summary>
        public static ulong ComputeBlockHash(ReadOnlySpan<byte> content)
        {
            var hash = SimHashWriter.ForFile();
            hash.WriteBytes(content);
            return hash.Digest();
        }

        /// <summary>
        /// Canonical state hash: NOVA_STATE_V1 domain over BlockId u16,
        /// ContentLength u32 and content bytes per block in ascending
        /// BlockId order (SimulationCore.md sections 5 and 7).
        /// </summary>
        public static ulong ComputeStateHash(IReadOnlyList<ushort> orderedIds, IReadOnlyList<byte[]> orderedContents)
        {
            var hash = SimHashWriter.ForState();
            for (int i = 0; i < orderedIds.Count; i++)
            {
                hash.WriteUInt16(orderedIds[i]);
                hash.WriteUInt32(unchecked((uint)orderedContents[i].Length));
                hash.WriteBytes(orderedContents[i]);
            }
            return hash.Digest();
        }

        /// <summary>
        /// Serializes the container. Blocks are sorted by ascending BlockId
        /// first, so insertion order never influences the output bytes.
        /// Throws <see cref="InvalidOperationException"/> when no block was
        /// added (documented behavior for the zero-block case).
        /// </summary>
        public byte[] ToArray()
        {
            if (_ids.Count == 0)
            {
                throw new InvalidOperationException(
                    "A canonical snapshot requires at least one state block.");
            }

            // Canonical order: strictly ascending BlockId, independent of
            // insertion order. Ids are unique (enforced in AddBlock), so the
            // sort result is fully determined.
            int count = _ids.Count;
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            Array.Sort(order, (a, b) => _ids[a].CompareTo(_ids[b]));

            var sortedIds = new ushort[count];
            var sortedContents = new byte[count][];
            long payloadBytes = 0;
            for (int i = 0; i < count; i++)
            {
                sortedIds[i] = _ids[order[i]];
                sortedContents[i] = _contents[order[i]];
                payloadBytes += sortedContents[i].Length;
            }

            long totalBytes = SnapshotFormat.HeaderBytes
                + (long)SnapshotFormat.BlockTableEntryBytes * count
                + payloadBytes;
            if (totalBytes > SnapshotFormat.MaxFileBytes)
            {
                throw new InvalidOperationException(
                    "Snapshot exceeds MaxFileBytes; this is a structural error.");
            }

            ulong stateHash = ComputeStateHash(sortedIds, sortedContents);

            var file = new byte[totalBytes];
            int offset = 0;
            WriteBytesRaw(file, ref offset, SnapshotFormat.Magic);
            WriteUInt16(file, ref offset, SnapshotFormat.FormatVersion);
            WriteUInt16(file, ref offset, unchecked((ushort)count));
            WriteUInt32(file, ref offset, unchecked((uint)payloadBytes));
            WriteUInt64(file, ref offset, stateHash);
            for (int i = 0; i < count; i++)
            {
                WriteUInt16(file, ref offset, sortedIds[i]);
                WriteUInt32(file, ref offset, unchecked((uint)sortedContents[i].Length));
                WriteUInt64(file, ref offset, ComputeBlockHash(sortedContents[i]));
            }
            for (int i = 0; i < count; i++)
            {
                WriteBytesRaw(file, ref offset, sortedContents[i]);
            }
            return file;
        }

        /// <summary>
        /// True when the serialized form would exceed the 4 MiB
        /// uncompressed MS-1 target (SimulationCore.md section 7). This is a
        /// documented warning/info signal only — never a failure.
        /// </summary>
        public bool ExceedsSoftTarget()
        {
            long payloadBytes = 0;
            for (int i = 0; i < _contents.Count; i++) payloadBytes += _contents[i].Length;
            long totalBytes = SnapshotFormat.HeaderBytes
                + (long)SnapshotFormat.BlockTableEntryBytes * _contents.Count
                + payloadBytes;
            return totalBytes > SnapshotFormat.SoftTargetBytes;
        }

        private static void WriteBytesRaw(byte[] file, ref int offset, byte[] data)
        {
            Array.Copy(data, 0, file, offset, data.Length);
            offset += data.Length;
        }

        private static void WriteUInt16(byte[] file, ref int offset, ushort value)
        {
            file[offset++] = (byte)value;
            file[offset++] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] file, ref int offset, uint value)
        {
            file[offset++] = (byte)value;
            file[offset++] = (byte)(value >> 8);
            file[offset++] = (byte)(value >> 16);
            file[offset++] = (byte)(value >> 24);
        }

        private static void WriteUInt64(byte[] file, ref int offset, ulong value)
        {
            file[offset++] = (byte)value;
            file[offset++] = (byte)(value >> 8);
            file[offset++] = (byte)(value >> 16);
            file[offset++] = (byte)(value >> 24);
            file[offset++] = (byte)(value >> 32);
            file[offset++] = (byte)(value >> 40);
            file[offset++] = (byte)(value >> 48);
            file[offset++] = (byte)(value >> 56);
        }
    }
}
