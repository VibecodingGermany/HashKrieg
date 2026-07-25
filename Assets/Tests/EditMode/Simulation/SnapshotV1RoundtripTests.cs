using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// SimulationCore.md section 7, point 1 (EditMode lane): serialize →
    /// deserialize → serialize yields byte-identical bytes, across block
    /// combinations, empty blocks and arbitrary insertion order. Mirror of
    /// the .NET lane SnapshotRoundtripTests with Unity Test Framework
    /// asserts.
    /// </summary>
    [TestFixture]
    public class SnapshotV1RoundtripTests
    {
        [Test]
        public void Roundtrip_SampleFile_IsByteIdentical()
        {
            byte[] first = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            Assert.That(
                SnapshotReader.TryRead(first, out SnapshotFile parsed, out SnapshotReadError error),
                Is.True, $"unexpected rejection: {error}");

            var rewrite = new SnapshotWriter();
            foreach (SnapshotBlock block in parsed.Blocks)
            {
                rewrite.AddBlock(block.BlockId, block.Content);
            }
            Assert.That(rewrite.ToArray(), Is.EqualTo(first));
        }

        [Test]
        public void Roundtrip_SingleBlock_IsByteIdentical()
        {
            var writer = new SnapshotWriter();
            var content = new SnapshotBlockWriter();
            content.WriteUInt64(0x0102030405060708UL);
            content.WriteLengthPrefixed(new byte[] { 1, 2, 3 });
            writer.AddBlock(7, content);

            byte[] first = writer.ToArray();
            Assert.That(SnapshotReader.TryRead(first, out SnapshotFile parsed, out _));
            Assert.That(parsed.Blocks.Count, Is.EqualTo(1));
            Assert.That(parsed.Blocks[0].Content, Is.EqualTo(content.ToArray()));

            var rewrite = new SnapshotWriter();
            rewrite.AddBlock(parsed.Blocks[0].BlockId, parsed.Blocks[0].Content);
            Assert.That(rewrite.ToArray(), Is.EqualTo(first));
        }

        [Test]
        public void Roundtrip_EmptyBlock_IsPreserved()
        {
            byte[] file = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            Assert.That(SnapshotReader.TryRead(file, out SnapshotFile parsed, out _));
            Assert.That(
                parsed.TryGetBlock(SnapshotV1TestUtil.BlockIdC, out byte[] content), Is.True);
            Assert.That(content.Length, Is.EqualTo(0));
        }

        [Test]
        public void InsertionOrder_DoesNotChangeBytes()
        {
            byte[] ascending = SnapshotV1TestUtil.CreateSampleWriter().ToArray();

            var reversed = new SnapshotWriter();
            var b = new SnapshotBlockWriter();
            b.WriteFieldTag(2);
            b.WriteEntityId(new EntityId(5, 1));
            b.WriteSimAngle(SimAngle.FromRaw(0x8000));
            reversed.AddBlock(SnapshotV1TestUtil.BlockIdC, ReadOnlySpan<byte>.Empty);
            reversed.AddBlock(SnapshotV1TestUtil.BlockIdB, b);
            var a = new SnapshotBlockWriter();
            a.WriteFieldTag(1);
            a.WriteTick(new Tick(42));
            a.WriteSimFixed(SimFixed.FromInt(-3));
            reversed.AddBlock(SnapshotV1TestUtil.BlockIdA, a);

            Assert.That(reversed.ToArray(), Is.EqualTo(ascending));
        }

        [Test]
        public void Deterministic_SameInputSameBytesAndStateHash()
        {
            byte[] first = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            byte[] second = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            Assert.That(second, Is.EqualTo(first));

            Assert.That(SnapshotReader.TryRead(first, out SnapshotFile one, out _));
            Assert.That(SnapshotReader.TryRead(second, out SnapshotFile two, out _));
            Assert.That(two.StateHash, Is.EqualTo(one.StateHash));
        }

        [Test]
        public void Writer_ZeroBlocks_ThrowsStructuralError()
        {
            var writer = new SnapshotWriter();
            Assert.That(() => writer.ToArray(), Throws.InvalidOperationException);
        }

        [Test]
        public void Writer_DuplicateBlockId_ThrowsStructuralError()
        {
            var writer = new SnapshotWriter();
            writer.AddBlock(1, new byte[] { 0x01 });
            Assert.That(
                () => writer.AddBlock(1, new byte[] { 0x02 }),
                Throws.ArgumentException);
        }

        [Test]
        public void Writer_BlockCountBeyondUInt16_ThrowsStructuralError()
        {
            var writer = new SnapshotWriter();
            for (int i = 0; i < ushort.MaxValue; i++)
            {
                writer.AddBlock(unchecked((ushort)(i + 1)), ReadOnlySpan<byte>.Empty);
            }
            // The 65.536th block would silently wrap the u16 block-count
            // field; the writer must reject it instead.
            Assert.That(
                () => writer.AddBlock(0, ReadOnlySpan<byte>.Empty),
                Throws.InvalidOperationException);
        }

        [Test]
        public void Reader_ZeroBlockCount_IsRejectedAsEmptyBlockTable()
        {
            byte[] file = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            SnapshotV1TestUtil.PatchUInt16(file, 10, 0); // BlockCount field
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.EmptyBlockTable));
        }

        [Test]
        public void Reader_ReportsVerifiedHeaderFields()
        {
            byte[] file = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            Assert.That(SnapshotReader.TryRead(file, out SnapshotFile parsed, out _));
            Assert.That(parsed.FormatVersion, Is.EqualTo(SnapshotFormat.FormatVersion));
            Assert.That(parsed.Blocks.Count, Is.EqualTo(3));
            Assert.That(parsed.ExceedsSoftTarget, Is.False);
            Assert.That(parsed.TryGetBlock(99, out _), Is.False);
        }
    }
}
