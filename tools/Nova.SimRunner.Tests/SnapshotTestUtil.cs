using System;
using Nova.Core;
using Nova.Simulation.Snapshots;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Shared helpers for the snapshot container suites: a deterministic
    /// three-block sample writer (ids 1/2/3, block 3 intentionally empty),
    /// raw table-field patchers for wire-level attack tests and a hex dump
    /// helper for golden masters.
    /// </summary>
    internal static class SnapshotTestUtil
    {
        internal const ushort BlockIdA = 1;
        internal const ushort BlockIdB = 2;
        internal const ushort BlockIdC = 3;

        /// <summary>
        /// Builds the canonical sample writer. With
        /// <paramref name="mutateBlockB"/> one content byte of block B is
        /// XORed, modelling the smallest possible state-block mutation.
        /// </summary>
        internal static SnapshotWriter CreateSampleWriter(bool mutateBlockB = false)
        {
            var writer = new SnapshotWriter();

            var a = new SnapshotBlockWriter();
            a.WriteFieldTag(1);
            a.WriteTick(new Tick(42));
            a.WriteSimFixed(SimFixed.FromInt(-3));
            writer.AddBlock(BlockIdA, a);

            var b = new SnapshotBlockWriter();
            b.WriteFieldTag(2);
            b.WriteEntityId(new EntityId(5, 1));
            b.WriteSimAngle(SimAngle.FromRaw(0x8000));
            byte[] content = b.ToArray();
            if (mutateBlockB)
            {
                content[0] ^= 0x01; // single-bit mutation inside block B
            }
            writer.AddBlock(BlockIdB, content);

            writer.AddBlock(BlockIdC, ReadOnlySpan<byte>.Empty); // empty block is legal
            return writer;
        }

        /// <summary>Offset of block table entry <paramref name="index"/>.</summary>
        internal static int TableEntryOffset(int index)
            => SnapshotFormat.HeaderBytes + index * SnapshotFormat.BlockTableEntryBytes;

        internal static void PatchUInt16(byte[] file, int offset, ushort value)
        {
            file[offset] = (byte)value;
            file[offset + 1] = (byte)(value >> 8);
        }

        internal static void PatchUInt32(byte[] file, int offset, uint value)
        {
            file[offset] = (byte)value;
            file[offset + 1] = (byte)(value >> 8);
            file[offset + 2] = (byte)(value >> 16);
            file[offset + 3] = (byte)(value >> 24);
        }

        internal static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[bytes[i] >> 4];
                chars[i * 2 + 1] = hex[bytes[i] & 0xF];
            }
            return new string(chars);
        }
    }
}
