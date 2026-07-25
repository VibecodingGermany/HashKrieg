using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Production;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class ProductionSystemTests
    {
        [Test]
        public void ResearchTreeSystem_UnlockTechTier_EnablesTier2()
        {
            var research = new ResearchTreeSystem();

            Assert.AreEqual(1, research.GetTechTier(0));
            Assert.IsFalse(research.IsTechUnlocked(0, 2));

            bool unlocked = research.UnlockTechTier(0, 2);
            Assert.IsTrue(unlocked);
            Assert.IsTrue(research.IsTechUnlocked(0, 2));
        }

        [Test]
        public void ProductionQueueSystem_EnqueueAndComplete_SpawnsUnit()
        {
            var entities = new EntityManager(100);
            var energy = new EnergyGridSystem(startingCredits: 500);
            var research = new ResearchTreeSystem();
            var production = new ProductionQueueSystem(entities, energy, research);

            var kernel = new SimulationKernel(new SimRandom(888));
            kernel.RegisterSystem(energy);
            kernel.RegisterSystem(research);
            kernel.RegisterSystem(production);
            kernel.Start();

            var riflemanDef = new UnitDefinition(
                definitionId: 10,
                stringId: "UNIT_Rifleman",
                moveSpeed: 5.0f,
                radius: 0.5f,
                maxHealth: 100,
                aetheriumCost: 100
            );

            Assert.AreEqual(0, entities.ActiveCount);

            bool enqueued = production.EnqueueUnitProduction(0, in riflemanDef, new Transform2D(20f, 20f), buildTimeTicks: 5);
            Assert.IsTrue(enqueued);
            Assert.AreEqual(1, production.ActiveQueueCount);

            ref PlayerEconomyState p0 = ref energy.GetPlayerEconomy(0);
            Assert.AreEqual(400, p0.AetheriumCredits);

            // Step 5 ticks to complete production
            for (int i = 0; i < 5; i++)
            {
                kernel.StepTick();
            }

            // Unit should now be spawned in EntityManager
            Assert.AreEqual(1, entities.ActiveCount);
            Assert.AreEqual(0, production.ActiveQueueCount);
        }
    }
}
