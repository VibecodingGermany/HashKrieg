using Nova.Core;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Smoke tests proving the shared Core/Simulation sources compile and behave
    /// identically outside the Unity host (G0-B .NET test lane). These are not
    /// G1 acceptance tests; the canonical fixed-point/hash contracts are built
    /// with G1 per docs/tech/SimulationCore.md.
    /// </summary>
    [TestFixture]
    public sealed class CoreContractTests
    {
        [Test]
        public void Tick_ValueSemantics_AndOrdering()
        {
            Assert.That(new Tick(0), Is.EqualTo(Tick.Zero));
            Assert.That(new Tick(1), Is.Not.EqualTo(Tick.Zero));
            Assert.That(new Tick(5), Is.LessThan(new Tick(6)));
            Assert.That(new Tick(7) == new Tick(7), Is.True);
        }

        [Test]
        public void EntityId_Invalid_IsNotValid()
        {
            Assert.That(EntityId.Invalid.IsValid, Is.False);
            Assert.That(new EntityId(0, 1).IsValid, Is.True);
            Assert.That(new EntityId(3, 2), Is.EqualTo(new EntityId(3, 2)));
            Assert.That(new EntityId(3, 3), Is.Not.EqualTo(new EntityId(3, 2)));
        }

        [Test]
        public void SimRandom_SameSeed_ProducesIdenticalSequence()
        {
            var first = new SimRandom(42UL);
            var second = new SimRandom(42UL);
            for (int i = 0; i < 32; i++)
            {
                Assert.That(second.NextUInt(), Is.EqualTo(first.NextUInt()), $"sequence diverged at index {i}");
            }
        }

        [Test]
        public void SimRandom_DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new SimRandom(1UL);
            var second = new SimRandom(2UL);
            bool anyDifference = false;
            for (int i = 0; i < 32; i++)
            {
                anyDifference |= first.NextUInt() != second.NextUInt();
            }
            Assert.That(anyDifference, Is.True);
        }
    }
}
