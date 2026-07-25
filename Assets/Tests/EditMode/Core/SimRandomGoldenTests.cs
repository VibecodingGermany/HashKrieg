using Nova.Core;
using NUnit.Framework;

namespace Nova.Core.Tests
{
    /// <summary>
    /// G1 PRNG suite for XorShift128PlusV1 (docs/tech/Testing.md section 3).
    /// Mirror of the .NET lane SimRandomGoldenTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class SimRandomGoldenTests
    {
        // Golden master for regression only, NOT spec truth: first 8 NextUInt
        // values generated once from the SimRandom implementation on
        // feat/g1-simfixed-core (seeding and 64-to-32 bit reduction are
        // spec-silent implementation details).
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
                Assert.AreEqual(first.NextUInt(), second.NextUInt(), $"sequence diverged at index {i}");
            }
        }

        [Test]
        public void GoldenVectors_Seed0_MatchPinnedStream()
        {
            var rng = new SimRandom(0UL);
            for (int i = 0; i < GoldenSeed0.Length; i++)
            {
                Assert.AreEqual(GoldenSeed0[i], rng.NextUInt(), $"golden mismatch at index {i}");
            }
        }

        [Test]
        public void GoldenVectors_Seed1_MatchPinnedStream()
        {
            var rng = new SimRandom(1UL);
            for (int i = 0; i < GoldenSeed1.Length; i++)
            {
                Assert.AreEqual(GoldenSeed1[i], rng.NextUInt(), $"golden mismatch at index {i}");
            }
        }

        [Test]
        public void Clone_ContinuesStateIdentically()
        {
            var original = new SimRandom(42UL);
            for (int i = 0; i < 100; i++)
            {
                original.NextUInt();
            }
            var restored = original.Clone();
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(original.NextUInt(), restored.NextUInt(), $"continuation diverged at index {i}");
            }
        }
    }
}
