using Nova.Core;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 PRNG suite for XorShift128PlusV1 (docs/tech/Testing.md section 3):
    /// deterministic sequences, golden vectors and state continuation.
    /// </summary>
    [TestFixture]
    public sealed class SimRandomGoldenTests
    {
        // Golden master for regression only, NOT spec truth: the spec fixes
        // "XorShift128PlusV1" with two uint64 words but neither seeding nor the
        // 64-to-32 bit output reduction. These are the first 8 NextUInt values
        // (high 32 bits of the xorshift128+ sum output) generated once from the
        // SimRandom implementation on this branch (feat/g1-simfixed-core).
        private static readonly uint[] GoldenSeed0 =
        {
            4276001226U, 4276001175U, 421263670U, 246480255U,
            1318571328U, 4294698456U, 288549032U, 4250171493U
        };

        private static readonly uint[] GoldenSeed1 =
        {
            1511669830U, 2003738471U, 4173142179U, 2907444091U,
            1161169546U, 1419021331U, 882423014U, 3385031663U
        };

        [Test]
        public void SameSeed_ProducesIdenticalSequence_Over1000Values()
        {
            var first = new SimRandom(123456789UL);
            var second = new SimRandom(123456789UL);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(second.NextUInt(), Is.EqualTo(first.NextUInt()), $"sequence diverged at index {i}");
            }
        }

        [Test]
        public void GoldenVectors_Seed0_MatchPinnedStream()
        {
            var rng = new SimRandom(0UL);
            for (int i = 0; i < GoldenSeed0.Length; i++)
            {
                Assert.That(rng.NextUInt(), Is.EqualTo(GoldenSeed0[i]), $"golden mismatch at index {i}");
            }
        }

        [Test]
        public void GoldenVectors_Seed1_MatchPinnedStream()
        {
            var rng = new SimRandom(1UL);
            for (int i = 0; i < GoldenSeed1.Length; i++)
            {
                Assert.That(rng.NextUInt(), Is.EqualTo(GoldenSeed1[i]), $"golden mismatch at index {i}");
            }
        }

        [Test]
        public void Clone_ContinuesStateIdentically()
        {
            // Snapshot-continuation via the ISimRandom surface: a clone taken
            // mid-stream must produce the identical continuation.
            var original = new SimRandom(42UL);
            for (int i = 0; i < 100; i++)
            {
                original.NextUInt();
            }
            var restored = original.Clone();
            for (int i = 0; i < 100; i++)
            {
                Assert.That(restored.NextUInt(), Is.EqualTo(original.NextUInt()), $"continuation diverged at index {i}");
            }
        }

        [Test]
        public void NextInt_StaysWithinRequestedRange()
        {
            var rng = new SimRandom(7UL);
            for (int i = 0; i < 1000; i++)
            {
                int value = rng.NextInt(-5, 17);
                Assert.That(value, Is.GreaterThanOrEqualTo(-5).And.LessThan(17));
            }
        }
    }
}
