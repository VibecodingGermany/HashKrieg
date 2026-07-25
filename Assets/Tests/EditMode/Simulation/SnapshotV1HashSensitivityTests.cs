using NUnit.Framework;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// SimulationCore.md section 7, point 3 (EditMode lane): a single-bit
    /// mutation inside block B changes exactly B's block hash and the state
    /// hash, every other block hash stays untouched. Mirror of the .NET lane
    /// SnapshotHashSensitivityTests.
    /// </summary>
    [TestFixture]
    public class SnapshotV1HashSensitivityTests
    {
        [Test]
        public void SingleBitMutation_InBlockB_ChangesOnlyItsHashAndStateHash()
        {
            byte[] originalBytes = SnapshotV1TestUtil.CreateSampleWriter().ToArray();
            byte[] mutatedBytes = SnapshotV1TestUtil.CreateSampleWriter(mutateBlockB: true).ToArray();

            Assert.That(SnapshotReader.TryRead(originalBytes, out SnapshotFile original, out _));
            Assert.That(SnapshotReader.TryRead(mutatedBytes, out SnapshotFile mutated, out _));
            Assert.That(original.Blocks.Count, Is.EqualTo(mutated.Blocks.Count));

            for (int i = 0; i < original.Blocks.Count; i++)
            {
                ushort id = original.Blocks[i].BlockId;
                if (id == SnapshotV1TestUtil.BlockIdB)
                {
                    Assert.That(
                        mutated.Blocks[i].Hash, Is.Not.EqualTo(original.Blocks[i].Hash),
                        "mutated block must change its block hash");
                }
                else
                {
                    Assert.That(
                        mutated.Blocks[i].Hash, Is.EqualTo(original.Blocks[i].Hash),
                        $"block {id} must keep its block hash");
                }
            }
            Assert.That(
                mutated.StateHash, Is.Not.EqualTo(original.StateHash),
                "any block mutation must change the state hash");
        }

        [Test]
        public void BlockContentHash_IsPureContentHash()
        {
            byte[] content = { 0x10, 0x20, 0x30 };
            Assert.That(
                SnapshotWriter.ComputeBlockHash(content),
                Is.EqualTo(SnapshotWriter.ComputeBlockHash(content)));
        }

        [Test]
        public void BlockId_ChangesStateHash_WithoutChangingBlockHash()
        {
            var one = new SnapshotWriter();
            one.AddBlock(1, new byte[] { 0x42 });
            var two = new SnapshotWriter();
            two.AddBlock(2, new byte[] { 0x42 });

            Assert.That(SnapshotReader.TryRead(one.ToArray(), out SnapshotFile first, out _));
            Assert.That(SnapshotReader.TryRead(two.ToArray(), out SnapshotFile second, out _));
            Assert.That(second.Blocks[0].Hash, Is.EqualTo(first.Blocks[0].Hash));
            Assert.That(second.StateHash, Is.Not.EqualTo(first.StateHash));
        }
    }
}
