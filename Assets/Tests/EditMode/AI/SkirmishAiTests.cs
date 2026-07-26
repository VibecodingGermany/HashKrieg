using NUnit.Framework;
using Nova.Core;
using Nova.AI;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Production;
using Nova.Simulation.State;

namespace Nova.AI.Tests
{
    [TestFixture]
    public class SkirmishAiTests
    {
        [Test]
        public void SkirmishAiSystem_ExecutesDecisionLoop_TriggersProduction()
        {
            var entities = new EntityManager(100);
            var economy = new EconomySystem(entities, startingCredits: 500);
            var research = new ResearchTreeSystem();
            var grid = new ConstructionGrid(64, 64);
            var construction = new ConstructionSystem(grid, economy);
            var production = new ProductionQueueSystem(entities, economy, research);

            var profile = new AiFactionProfile("Alliance");
            var aiSystem = new SkirmishAiSystem(1, profile, entities, economy, construction, production);

            var kernel = new SimulationKernel(new SimRandom(333));
            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(research);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(aiSystem);
            kernel.Start();

            // The canonical power balance derives from building-role
            // entities: an HQ gives the AI slot a positive margin
            // (provisional 30), so the decision loop skips the power-plant
            // branch and goes straight to production.
            entities.SpawnUnit(
                1,
                new Transform2D(SimFixed.FromInt(30), SimFixed.FromInt(30)),
                SimFixed.Zero,
                role: UnitRole.HQ);

            Assert.AreEqual(0, production.ActiveQueueCount);

            // Step 20 ticks to trigger AI decision loop
            for (int i = 0; i < 20; i++)
            {
                kernel.StepTick();
            }

            // AI should have enqueued unit production
            Assert.Greater(production.ActiveQueueCount, 0);
        }
    }
}
