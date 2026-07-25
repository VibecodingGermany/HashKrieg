using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Replays;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Parser hardening for the canonical replay container (EditMode lane):
    /// truncation at every byte position, forged length fields rejected
    /// before allocation, structurally invalid records rejected
    /// (SimulationCore.md sections 7.4 and 8 — structurally invalid commands
    /// never entered the canonical stream and must reject the replay).
    /// Mirror of the .NET lane ReplayParserHardeningTests.
    /// </summary>
    [TestFixture]
    public class ReplayV1ParserHardeningTests
    {
        [Test]
        public void TruncationLoop_EveryStrictPrefixIsRejectedWithoutThrowing()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunSmallMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out _, out _), "fixture must be valid");

            for (int length = 0; length < live.ReplayBytes.Length; length++)
            {
                var prefix = new byte[length];
                Array.Copy(live.ReplayBytes, prefix, length);
                Assert.DoesNotThrow(() =>
                {
                    bool ok = ReplayFile.TryParse(prefix, out _, out ReplayReadError error);
                    Assert.IsFalse(ok, $"prefix of length {length} parsed successfully");
                    Assert.AreNotEqual(ReplayReadError.None, error);
                }, $"prefix of length {length} threw");
            }
        }

        [Test]
        public void HeaderAttacks_AreRejectedDeterministically()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunSmallMatch();
            int fingerprintLength = live.Fingerprint.Serialize().Length;
            int snapshotLengthOffset = ReplayFormat.HeaderFixedBytes + fingerprintLength;
            int tickCountOffset = snapshotLengthOffset + 4 + live.InitialSnapshotBytes.Length;

            // Bad magic.
            var badMagic = (byte[])live.ReplayBytes.Clone();
            badMagic[0] ^= 0xFF;
            Assert.IsFalse(ReplayFile.TryParse(badMagic, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.BadMagic, error);

            // Unsupported format version.
            var badVersion = (byte[])live.ReplayBytes.Clone();
            badVersion[8] = 0x02;
            Assert.IsFalse(ReplayFile.TryParse(badVersion, out _, out error));
            Assert.AreEqual(ReplayReadError.UnsupportedFormatVersion, error);

            // Forged giant fingerprint length: rejected before allocation.
            var badFingerprint = (byte[])live.ReplayBytes.Clone();
            WriteUInt32(badFingerprint, 10, 0xFFFFFFFFu);
            Assert.IsFalse(ReplayFile.TryParse(badFingerprint, out _, out error));
            Assert.AreEqual(ReplayReadError.FingerprintLengthInvalid, error);

            // Forged giant snapshot length: rejected before allocation.
            var badSnapshot = (byte[])live.ReplayBytes.Clone();
            WriteUInt32(badSnapshot, snapshotLengthOffset, 0xFFFFFFFFu);
            Assert.IsFalse(ReplayFile.TryParse(badSnapshot, out _, out error));
            Assert.AreEqual(ReplayReadError.SnapshotLengthInvalid, error);

            // Forged giant tick count: bounded arithmetically before allocation.
            var badTickCount = (byte[])live.ReplayBytes.Clone();
            WriteUInt32(badTickCount, tickCountOffset, 0xFFFFFFFFu);
            Assert.IsFalse(ReplayFile.TryParse(badTickCount, out _, out error));
            Assert.AreEqual(ReplayReadError.TickCountInvalid, error);

            // Trailing garbage after the trailer.
            var trailing = new byte[live.ReplayBytes.Length + 1];
            Array.Copy(live.ReplayBytes, trailing, live.ReplayBytes.Length);
            Assert.IsFalse(ReplayFile.TryParse(trailing, out _, out error));
            Assert.AreEqual(ReplayReadError.TrailingBytes, error);

            // Beyond the 64 MiB hard cap: rejected by length alone.
            Assert.IsFalse(ReplayFile.TryParse(new byte[ReplayFormat.MaxFileBytes + 1], out _, out error));
            Assert.AreEqual(ReplayReadError.FileTooLarge, error);
        }

        [Test]
        public void FrameAttacks_AreRejectedDeterministically()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunSmallMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));
            // Small match: frame 1 (tick 1) carries the human and the AI
            // record, frames 2 and 3 are empty.
            ReplayTickFrame frame1 = replay.Frames[0];
            Assert.AreEqual(2, frame1.RecordCount);
            ReplayTickFrame frame2 = replay.Frames[1];
            Assert.AreEqual(0, frame2.RecordCount);

            // Record count beyond the per-tick cap.
            var badCount = (byte[])live.ReplayBytes.Clone();
            WriteUInt16(badCount, frame2.SourceOffset + 4, 300);
            Assert.IsFalse(ReplayFile.TryParse(badCount, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.RecordCountExceeded, error);

            // Outer record length below the record header size.
            var badLength = (byte[])live.ReplayBytes.Clone();
            WriteUInt16(badLength, frame1.RecordSourceOffsets[0], 10);
            Assert.IsFalse(ReplayFile.TryParse(badLength, out _, out error));
            Assert.AreEqual(ReplayReadError.RecordLengthInvalid, error);

            // Unknown result code.
            var badCode = (byte[])live.ReplayBytes.Clone();
            int codeOffset = frame1.RecordSourceOffsets[0] + 2 + frame1.RecordBytes[0].Length;
            WriteUInt16(badCode, codeOffset, 0xFFFF);
            Assert.IsFalse(ReplayFile.TryParse(badCode, out _, out error));
            Assert.AreEqual(ReplayReadError.UnknownResultCode, error);

            // Non-canonical record order: swap the two equal-length record
            // blocks (the AI record may not precede the human record).
            Assert.AreEqual(frame1.RecordBytes[1].Length, frame1.RecordBytes[0].Length,
                "fixture requires equal-length records");
            var swapped = (byte[])live.ReplayBytes.Clone();
            int blockBytes = 2 + frame1.RecordBytes[0].Length + 2;
            var temp = new byte[blockBytes];
            Array.Copy(swapped, frame1.RecordSourceOffsets[0], temp, 0, blockBytes);
            Array.Copy(swapped, frame1.RecordSourceOffsets[1], swapped, frame1.RecordSourceOffsets[0], blockBytes);
            Array.Copy(temp, 0, swapped, frame1.RecordSourceOffsets[1], blockBytes);
            Assert.IsFalse(ReplayFile.TryParse(swapped, out _, out error));
            Assert.AreEqual(ReplayReadError.NonCanonicalRecordOrder, error);

            // Record target tick disagreeing with its frame tick.
            var badTarget = (byte[])live.ReplayBytes.Clone();
            badTarget[frame1.RecordSourceOffsets[0] + 2 + 6] ^= 0xFF; // TargetTick field inside the record
            Assert.IsFalse(ReplayFile.TryParse(badTarget, out _, out error));
            Assert.AreEqual(ReplayReadError.RecordTargetTickMismatch, error);
        }

        [Test]
        public void StructurallyInvalidRecord_InStream_RejectsReplay()
        {
            // Commands.md section 4: structurally invalid commands never
            // entered the canonical stream — a replay carrying one is not a
            // canonical artifact, even before the chain is consulted.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunSmallMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));
            ReplayTickFrame frame1 = replay.Frames[0];

            // Corrupt the Move payload's entity count (first two payload
            // bytes): the count no longer matches the payload length.
            var tampered = (byte[])live.ReplayBytes.Clone();
            int payloadOffset = frame1.RecordSourceOffsets[0] + 2 + CommandLimits.HeaderBytes;
            WriteUInt16(tampered, payloadOffset, 0xFFFF);

            Assert.IsFalse(ReplayFile.TryParse(tampered, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.StructurallyInvalidRecord, error);
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }
    }
}
