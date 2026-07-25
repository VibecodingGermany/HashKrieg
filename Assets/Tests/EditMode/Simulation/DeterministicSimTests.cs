using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class DeterministicSimTests
    {
        [Test]
        public void SimRandom_WithSameSeed_ProducesIdenticalSequence()
        {
            const ulong seed = 123456789UL;
            var rngA = new SimRandom(seed);
            var rngB = new SimRandom(seed);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(rngA.NextUInt(), rngB.NextUInt(), $"Mismatch at uint index {i}");
                Assert.AreEqual(rngA.NextFloat(), rngB.NextFloat(), $"Mismatch at float index {i}");
            }
        }

        [Test]
        public void SimulationKernel_MultiTickRun_IsDeterministic()
        {
            const ulong seed = 42UL;

            var kernelA = new SimulationKernel(new SimRandom(seed));
            var kernelB = new SimulationKernel(new SimRandom(seed));

            kernelA.Start();
            kernelB.Start();

            for (int i = 0; i < 100; i++)
            {
                kernelA.StepTick();
                kernelB.StepTick();

                Assert.AreEqual(kernelA.CurrentTick, kernelB.CurrentTick);
                Assert.AreEqual(kernelA.CalculateStateHash(), kernelB.CalculateStateHash(), $"State hash mismatch at tick {i}");
            }
        }

        [Test]
        public void SimulationKernel_RepeatedStateHash_IsStable_AndDoesNotConsumePrng()
        {
            // F-005 regression: the canonical state hash is read-only. Two
            // consecutive hashes are identical and the PRNG state words are
            // untouched by hashing (the old hash consumed Random.NextUInt()).
            var kernel = new SimulationKernel(new SimRandom(100));
            kernel.Start();
            kernel.StepTick();

            kernel.Random.GetState(out ulong s0Before, out ulong s1Before);
            ulong first = kernel.CalculateStateHash();
            ulong second = kernel.CalculateStateHash();
            kernel.Random.GetState(out ulong s0After, out ulong s1After);

            Assert.AreEqual(first, second, "repeated state hash must be identical");
            Assert.AreEqual(s0Before, s0After, "hashing must not touch PRNG word s0");
            Assert.AreEqual(s1Before, s1After, "hashing must not touch PRNG word s1");
        }
    }
}
