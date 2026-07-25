using System;
using Nova.Simulation.Snapshots;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// SimulationCore.md section 7, point 4, and Serialization.md sections 5
    /// and 6: parser hardening. Lengths and capacities are validated before
    /// allocation; the 64 MiB hard cap rejects before the payload parse;
    /// truncation, corruption, forged lengths, unknown versions, duplicate
    /// or unordered BlockIds are all deterministic rejections — never an
    /// exception, never a partial result.
    /// </summary>
    [TestFixture]
    public sealed class SnapshotParserHardeningTests
    {
        private static byte[] SampleFile() => SnapshotTestUtil.CreateSampleWriter().ToArray();

        [Test]
        public void FileOverHardCap_IsRejectedBeforePayloadParse()
        {
            // One single oversized allocation models a hostile file length;
            // the parser must reject it from the length alone, before
            // inspecting any header field (magic here is deliberately wrong).
            var huge = new byte[SnapshotFormat.MaxFileBytes + 1];
            Assert.That(
                SnapshotReader.TryRead(huge, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.FileTooLarge));
        }

        [Test]
        public void ForgedHugePayloadLength_IsRejectedArithmetically()
        {
            // Forged 4 GiB payload length inside an otherwise valid small
            // file: rejected by the length-consistency check without any
            // large allocation or payload access.
            byte[] file = SampleFile();
            SnapshotTestUtil.PatchUInt32(file, 12, 0xFFFFFFFF);
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.PayloadLengthMismatch));
        }

        [Test]
        public void Truncation_AtEveryPosition_IsRejectedDeterministically()
        {
            byte[] file = SampleFile();
            for (int prefix = 0; prefix < file.Length; prefix++)
            {
                var truncated = new byte[prefix];
                Array.Copy(file, truncated, prefix);
                Assert.That(
                    SnapshotReader.TryRead(truncated, out SnapshotFile parsed, out _),
                    Is.False, $"prefix length {prefix} must not parse");
                Assert.That(parsed, Is.Null, "failed parses produce no partial result");
            }
        }

        [Test]
        public void BitCorruption_AtEveryPosition_IsDetected()
        {
            byte[] file = SampleFile();
            for (int position = 0; position < file.Length; position++)
            {
                var corrupted = (byte[])file.Clone();
                corrupted[position] ^= 0x01;
                Assert.That(
                    SnapshotReader.TryRead(corrupted, out _, out SnapshotReadError error),
                    Is.False, $"bit flip at byte {position} must be detected");
                Assert.That(error, Is.Not.EqualTo(SnapshotReadError.None));
            }
        }

        [Test]
        public void TrailingBytes_AreRejected()
        {
            byte[] file = SampleFile();
            var extended = new byte[file.Length + 1];
            Array.Copy(file, extended, file.Length);
            Assert.That(
                SnapshotReader.TryRead(extended, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.PayloadLengthMismatch));
        }

        [Test]
        public void WrongMagic_IsRejected()
        {
            byte[] file = SampleFile();
            file[0] = (byte)'X';
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.BadMagic));
        }

        [Test]
        public void UnsupportedFormatVersion_IsRejected()
        {
            foreach (ushort version in new ushort[] { 0, 2, 0xFFFF })
            {
                byte[] file = SampleFile();
                SnapshotTestUtil.PatchUInt16(file, 8, version);
                Assert.That(
                    SnapshotReader.TryRead(file, out _, out SnapshotReadError error),
                    Is.False, $"version {version}");
                Assert.That(error, Is.EqualTo(SnapshotReadError.UnsupportedFormatVersion));
            }
        }

        [Test]
        public void DuplicateBlockId_IsRejected()
        {
            byte[] file = SampleFile();
            // Rewrite the second table entry's BlockId to match the first.
            SnapshotTestUtil.PatchUInt16(
                file, SnapshotTestUtil.TableEntryOffset(1), SnapshotTestUtil.BlockIdA);
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.DuplicateBlockId));
        }

        [Test]
        public void NonCanonicalBlockOrder_IsRejected()
        {
            byte[] file = SampleFile();
            // Swap the first two BlockIds: table is no longer ascending.
            SnapshotTestUtil.PatchUInt16(
                file, SnapshotTestUtil.TableEntryOffset(0), SnapshotTestUtil.BlockIdB);
            SnapshotTestUtil.PatchUInt16(
                file, SnapshotTestUtil.TableEntryOffset(1), SnapshotTestUtil.BlockIdA);
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.NonCanonicalBlockOrder));
        }

        [Test]
        public void BlockLengthBeyondRest_IsRejected()
        {
            byte[] file = SampleFile();
            // Last table entry claims a length beyond the remaining bytes.
            SnapshotTestUtil.PatchUInt32(
                file, SnapshotTestUtil.TableEntryOffset(2) + 2, 0x00FFFFFF);
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.PayloadLengthMismatch));
        }

        [Test]
        public void PayloadBitFlip_YieldsBlockHashMismatch()
        {
            byte[] file = SampleFile();
            int payloadOffset = SnapshotFormat.HeaderBytes
                + 3 * SnapshotFormat.BlockTableEntryBytes;
            file[payloadOffset] ^= 0x01;
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.BlockHashMismatch));
        }

        [Test]
        public void StateHashBitFlip_YieldsStateHashMismatch()
        {
            byte[] file = SampleFile();
            file[16] ^= 0x01; // first state-hash byte
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.StateHashMismatch));
        }

        [Test]
        public void SoftTarget_Exceeded_IsInfoNotFailure()
        {
            // 4 MiB target (SimulationCore.md section 7): documented
            // warning/info path, never a hard error.
            var writer = new SnapshotWriter();
            writer.AddBlock(1, new byte[SnapshotFormat.SoftTargetBytes]);
            Assert.That(writer.ExceedsSoftTarget(), Is.True);

            byte[] file = writer.ToArray();
            Assert.That(SnapshotReader.TryRead(file, out SnapshotFile parsed, out _));
            Assert.That(parsed.ExceedsSoftTarget, Is.True);
        }
    }
}
