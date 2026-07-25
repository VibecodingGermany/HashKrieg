using System;

namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Hardened parser for the canonical snapshot container
    /// (docs/tech/SimulationCore.md section 7, point 4). All lengths and
    /// capacities are validated arithmetically before any allocation or
    /// payload parse: files over the 64 MiB hard cap are rejected from their
    /// length alone, and the declared PayloadBytes/table lengths must be
    /// exactly consistent with the actual file size before a single payload
    /// byte is touched. Malformed input always yields
    /// <c>false</c> plus a deterministic <see cref="SnapshotReadError"/>;
    /// this parser never throws on bad input and produces no partial result.
    /// </summary>
    public static class SnapshotReader
    {
        /// <summary>
        /// Parses and fully validates a snapshot file: hard cap, magic,
        /// format version, block count, table fit, strictly ascending
        /// duplicate-free BlockIds, exact length consistency, every block
        /// hash and the state hash — in that order.
        /// </summary>
        public static bool TryRead(
            ReadOnlySpan<byte> file, out SnapshotFile snapshot, out SnapshotReadError error)
        {
            snapshot = null;

            // Hard cap first: rejection happens before any header field
            // beyond the length itself is inspected (SimulationCore.md 7).
            if (file.Length > SnapshotFormat.MaxFileBytes)
            {
                error = SnapshotReadError.FileTooLarge;
                return false;
            }
            if (file.Length < SnapshotFormat.HeaderBytes)
            {
                error = SnapshotReadError.TruncatedHeader;
                return false;
            }
            for (int i = 0; i < SnapshotFormat.Magic.Length; i++)
            {
                if (file[i] != SnapshotFormat.Magic[i])
                {
                    error = SnapshotReadError.BadMagic;
                    return false;
                }
            }

            ushort formatVersion = ReadUInt16(file, 8);
            if (formatVersion != SnapshotFormat.FormatVersion)
            {
                error = SnapshotReadError.UnsupportedFormatVersion;
                return false;
            }

            ushort blockCount = ReadUInt16(file, 10);
            if (blockCount == 0)
            {
                error = SnapshotReadError.EmptyBlockTable;
                return false;
            }

            uint payloadBytes = ReadUInt32(file, 12);
            ulong stateHash = ReadUInt64(file, 16);

            // All length arithmetic in long: forged counts/lengths can never
            // overflow into a small allocation.
            long tableBytes = (long)SnapshotFormat.BlockTableEntryBytes * blockCount;
            long payloadOffset = SnapshotFormat.HeaderBytes + tableBytes;
            if (payloadOffset > file.Length)
            {
                error = SnapshotReadError.TruncatedBlockTable;
                return false;
            }

            var ids = new ushort[blockCount];
            var lengths = new uint[blockCount];
            var hashes = new ulong[blockCount];
            long lengthSum = 0;
            for (int i = 0; i < blockCount; i++)
            {
                int entryOffset = SnapshotFormat.HeaderBytes
                    + i * SnapshotFormat.BlockTableEntryBytes;
                ids[i] = ReadUInt16(file, entryOffset);
                lengths[i] = ReadUInt32(file, entryOffset + 2);
                hashes[i] = ReadUInt64(file, entryOffset + 6);
                if (i > 0)
                {
                    if (ids[i] == ids[i - 1])
                    {
                        error = SnapshotReadError.DuplicateBlockId;
                        return false;
                    }
                    if (ids[i] < ids[i - 1])
                    {
                        error = SnapshotReadError.NonCanonicalBlockOrder;
                        return false;
                    }
                }
                lengthSum += lengths[i];
            }

            // Exact length consistency, checked arithmetically before any
            // payload byte is parsed or copied: covers truncation inside the
            // payload, block lengths beyond the remaining bytes, trailing
            // bytes and forged overlong length fields in one comparison.
            if (lengthSum != payloadBytes || payloadOffset + payloadBytes != file.Length)
            {
                error = SnapshotReadError.PayloadLengthMismatch;
                return false;
            }

            // Only now, with every length proven, allocate and verify hashes.
            var blocks = new SnapshotBlock[blockCount];
            var contents = new byte[blockCount][];
            long cursor = payloadOffset;
            for (int i = 0; i < blockCount; i++)
            {
                var content = new byte[lengths[i]];
                file.Slice((int)cursor, (int)lengths[i]).CopyTo(content);
                cursor += lengths[i];
                ulong blockHash = SnapshotWriter.ComputeBlockHash(content);
                if (blockHash != hashes[i])
                {
                    error = SnapshotReadError.BlockHashMismatch;
                    return false;
                }
                contents[i] = content;
                blocks[i] = new SnapshotBlock(ids[i], blockHash, content);
            }

            if (SnapshotWriter.ComputeStateHash(ids, contents) != stateHash)
            {
                error = SnapshotReadError.StateHashMismatch;
                return false;
            }

            snapshot = new SnapshotFile(
                formatVersion, stateHash, blocks,
                file.Length > SnapshotFormat.SoftTargetBytes);
            error = SnapshotReadError.None;
            return true;
        }

        private static ushort ReadUInt16(ReadOnlySpan<byte> file, int offset)
        {
            return (ushort)(file[offset] | (file[offset + 1] << 8));
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> file, int offset)
        {
            return (uint)file[offset]
                | ((uint)file[offset + 1] << 8)
                | ((uint)file[offset + 2] << 16)
                | ((uint)file[offset + 3] << 24);
        }

        private static ulong ReadUInt64(ReadOnlySpan<byte> file, int offset)
        {
            return (ulong)file[offset]
                | ((ulong)file[offset + 1] << 8)
                | ((ulong)file[offset + 2] << 16)
                | ((ulong)file[offset + 3] << 24)
                | ((ulong)file[offset + 4] << 32)
                | ((ulong)file[offset + 5] << 40)
                | ((ulong)file[offset + 6] << 48)
                | ((ulong)file[offset + 7] << 56);
        }
    }
}
