using Nova.Core;
using Nova.Simulation.Snapshots;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Format-freeze regression master for the snapshot container v1
    /// (SimulationCore.md section 7, Serialization.md section 2). The hex
    /// constant was generated once from the canonical serializer and must
    /// never change; a mismatch means the wire format moved, which after
    /// the G1 freeze requires a new format version and new goldens.
    /// <para>
    /// Golden fixture, fully specified:
    /// block 1 = FieldTag(1), Tick(42), SimFixed(-3);
    /// block 2 = FieldTag(2), EntityId(index 5, version 1), SimAngle(0x8000);
    /// block 3 = empty.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class SnapshotGoldenBytesTests
    {
        private const string GoldenHex =
            "4e4f5641534e4150010003001800000019cf8141fe427afb"
            + "01000c000000d5123e09ba5d4351"
            + "02000c000000aa3342ec294cdc83"
            + "030000000000f332c9aa8d8a37ac"
            + "010000002a0000000000fdff"
            + "020000000500000001000080";

        [Test]
        public void GoldenBytes_SampleFile_MatchesFrozenMaster()
        {
            byte[] file = SnapshotTestUtil.CreateSampleWriter().ToArray();
            Assert.That(SnapshotTestUtil.ToHex(file), Is.EqualTo(GoldenHex));
        }
    }
}
