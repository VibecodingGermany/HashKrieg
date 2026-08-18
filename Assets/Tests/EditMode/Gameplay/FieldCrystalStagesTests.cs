using NUnit.Framework;
using Nova.Gameplay;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for the field-marker staging rule of
    /// <see cref="FieldCrystalStages"/> (21.2, #86): ceiling of the reserve
    /// fraction times the shard count — full reserve lights every shard, any
    /// reserve above zero keeps at least one, exactly 0 AE lights none, and
    /// the result never leaves [0, shardCount].
    /// </summary>
    [TestFixture]
    public class FieldCrystalStagesTests
    {
        [Test]
        public void VisibleShards_FullReserve_ShowsEveryShard()
        {
            Assert.AreEqual(7, FieldCrystalStages.VisibleShards(9000, 9000, 7));
            Assert.AreEqual(7, FieldCrystalStages.VisibleShards(15000, 15000, 7));
        }

        [Test]
        public void VisibleShards_ZeroRemaining_ShowsNone()
        {
            Assert.AreEqual(0, FieldCrystalStages.VisibleShards(0, 9000, 7));
        }

        [Test]
        public void VisibleShards_StageBoundaries_RoundUp()
        {
            // shardCount 4 over 8.000 AE: stage k holds while the reserve is
            // in ((k-1)/4, k/4] of the initial reserve — the boundary value
            // itself still shows the HIGHER stage's lower edge exactly.
            Assert.AreEqual(4, FieldCrystalStages.VisibleShards(8000, 8000, 4));
            Assert.AreEqual(4, FieldCrystalStages.VisibleShards(6001, 8000, 4));
            Assert.AreEqual(3, FieldCrystalStages.VisibleShards(6000, 8000, 4));
            Assert.AreEqual(3, FieldCrystalStages.VisibleShards(4001, 8000, 4));
            Assert.AreEqual(2, FieldCrystalStages.VisibleShards(4000, 8000, 4));
            Assert.AreEqual(1, FieldCrystalStages.VisibleShards(2000, 8000, 4));
            Assert.AreEqual(1, FieldCrystalStages.VisibleShards(1, 8000, 4), "any reserve above zero keeps one shard");
            Assert.AreEqual(0, FieldCrystalStages.VisibleShards(0, 8000, 4), "0 AE means none — the stump is the view's business");
        }

        [Test]
        public void VisibleShards_IsMonotonicallyNonIncreasing()
        {
            int previous = FieldCrystalStages.VisibleShards(9000, 9000, 7);
            for (long remaining = 8999; remaining >= 0; remaining -= 97)
            {
                int stage = FieldCrystalStages.VisibleShards(remaining, 9000, 7);
                Assert.LessOrEqual(stage, previous, $"stage must not rise as the reserve falls (remaining {remaining})");
                Assert.GreaterOrEqual(stage, 0);
                Assert.LessOrEqual(stage, 7);
                previous = stage;
            }
            Assert.AreEqual(0, FieldCrystalStages.VisibleShards(0, 9000, 7), "the walk ends at the exhausted stage");
        }

        [Test]
        public void VisibleShards_Guards_ClampIntoRange()
        {
            // Over-reserve (or a layout/console slip) can never light more
            // shards than the cluster has; degenerate inputs show nothing.
            Assert.AreEqual(7, FieldCrystalStages.VisibleShards(20000, 9000, 7));
            Assert.AreEqual(0, FieldCrystalStages.VisibleShards(100, 0, 7), "unknown initial reserve shows nothing rather than dividing by zero");
            Assert.AreEqual(0, FieldCrystalStages.VisibleShards(-5, 9000, 7));
            Assert.AreEqual(0, FieldCrystalStages.VisibleShards(100, 100, 0), "an empty cluster has no shards to light");
        }
    }
}
