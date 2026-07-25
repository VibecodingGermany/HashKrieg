using System;
using NUnit.Framework;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// SimulationCore.md section 7, point 4, and Serialization.md sections 5
    /// and 6 (EditMode lane): parser hardening. Mirror of the .NET lane
    /// SnapshotParserHardeningTests.
    /// </summary>
    [TestFixture]
    public class SnapshotV1ParserHardeningTests
    {
        private static byte[] SampleFile() => SnapshotV1TestUtil.CreateSampleWriter().ToArray();

        [Test]
        public void FileOverHardCap_IsRejectedBeforePayloadParse()
        {
            var huge = new byte[SnapshotFormat.MaxFileBytes + 1];
            Assert.That(
                SnapshotReader.TryRead(huge, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.FileTooLarge));
        }

        [Test]
        public void ForgedHugePayloadLength_IsRejectedArithmetically()
        {
            byte[] file = SampleFile();
            SnapshotV1TestUtil.PatchUInt32(file, 12, 0xFFFFFFFF);
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
                SnapshotV1TestUtil.PatchUInt16(file, 8, version);
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
            SnapshotV1TestUtil.PatchUInt16(
                file, SnapshotV1TestUtil.TableEntryOffset(1), SnapshotV1TestUtil.BlockIdA);
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.DuplicateBlockId));
        }

        [Test]
        public void NonCanonicalBlockOrder_IsRejected()
        {
            byte[] file = SampleFile();
            SnapshotV1TestUtil.PatchUInt16(
                file, SnapshotV1TestUtil.TableEntryOffset(0), SnapshotV1TestUtil.BlockIdB);
            SnapshotV1TestUtil.PatchUInt16(
                file, SnapshotV1TestUtil.TableEntryOffset(1), SnapshotV1TestUtil.BlockIdA);
            Assert.That(
                SnapshotReader.TryRead(file, out _, out SnapshotReadError error), Is.False);
            Assert.That(error, Is.EqualTo(SnapshotReadError.NonCanonicalBlockOrder));
        }

        [Test]
        public void BlockLengthBeyondRest_IsRejected()
        {
            byte[] file = SampleFile();
            SnapshotV1TestUtil.PatchUInt32(
                file, SnapshotV1TestUtil.TableEntryOffset(2) + 2, 0x00FFFFFF);
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
            var writer = new SnapshotWriter();
            writer.AddBlock(1, new byte[SnapshotFormat.SoftTargetBytes]);
            Assert.That(writer.ExceedsSoftTarget(), Is.True);

            byte[] file = writer.ToArray();
            Assert.That(SnapshotReader.TryRead(file, out SnapshotFile parsed, out _));
            Assert.That(parsed.ExceedsSoftTarget, Is.True);
        }
    }
}
