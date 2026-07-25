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
            var energy = new EnergyGridSystem(startingCredits: 500);
            var research = new ResearchTreeSystem();
            var grid = new ConstructionGrid(64, 64);
            var construction = new ConstructionSystem(grid, energy);
            var production = new ProductionQueueSystem(entities, energy, research);

            var profile = new AiFactionProfile("Alliance");
            var aiSystem = new SkirmishAiSystem(1, profile, entities, energy, construction, production);

            var kernel = new SimulationKernel(new SimRandom(333));
            kernel.RegisterSystem(energy);
            kernel.RegisterSystem(research);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(aiSystem);
            kernel.Start();

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
