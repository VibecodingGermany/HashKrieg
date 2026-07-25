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
        public void CommandEnvelope_SubmissionAndProcessing_Succeeds()
        {
            var kernel = new SimulationKernel(new SimRandom(100));
            kernel.Start();

            var cmd = new CommandEnvelope(
                type: CommandType.Move,
                issuer: CommandIssuer.Human,
                playerId: 1,
                sequence: 1,
                targetTick: new Tick(5),
                entityId: new EntityId(10, 1),
                targetPositionX: 100.5f,
                targetPositionY: 0f,
                targetPositionZ: 50.25f
            );

            bool submitted = kernel.SubmitCommand(cmd);
            Assert.IsTrue(submitted);

            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }

            Assert.AreEqual(new Tick(10), kernel.CurrentTick);
        }
    }
}
