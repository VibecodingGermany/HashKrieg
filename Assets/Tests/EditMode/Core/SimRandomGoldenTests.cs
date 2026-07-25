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
        // feat/g1-simfixed-core after the review-driven seeding fix
        // (canonical SplitMix64 with running state, P2-1); seeding and
        // 64-to-32 bit reduction are spec-silent implementation details.
        private static readonly uint[] GoldenSeed0 =
        {
            4284376650U, 1555263592U, 1995837922U, 3488402303U,
            1202433501U, 927087696U, 4280932643U, 2686686964U
        };

        private static readonly uint[] GoldenSeed1 =
        {
            2559615116U, 2443280914U, 3555056611U, 1532080945U,
            3125919927U, 4205842318U, 2753659703U, 582798359U
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

        [Test]
        public void GetState_SetState_ContinuesSequenceIdentically()
        {
            var original = new SimRandom(42UL);
            for (int i = 0; i < 10; i++)
            {
                original.NextUInt();
            }

            original.GetState(out ulong s0, out ulong s1);
            var restored = new SimRandom(999UL);
            restored.SetState(s0, s1);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(original.NextUInt(), restored.NextUInt(), $"continuation diverged at index {i}");
            }
        }

        [Test]
        public void GetState_DoesNotAdvanceSequence()
        {
            var rng = new SimRandom(7UL);
            rng.GetState(out ulong s0a, out ulong s1a);
            rng.GetState(out ulong s0b, out ulong s1b);
            Assert.AreEqual(s0a, s0b);
            Assert.AreEqual(s1a, s1b);
        }

        [Test]
        public void SetState_RejectsDegenerateZeroState()
        {
            var rng = new SimRandom(1UL);
            Assert.Throws<System.ArgumentException>(() => rng.SetState(0UL, 0UL));
        }
    }
}
