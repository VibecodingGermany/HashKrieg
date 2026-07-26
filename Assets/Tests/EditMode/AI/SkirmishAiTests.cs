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
            var economy = new EconomySystem(entities, startingCredits: 2000);
            var construction = new ConstructionSystem(entities, economy);
            var production = new ProductionSystem(entities, economy, construction);

            var profile = new AiFactionProfile("Alliance");
            var aiSystem = new SkirmishAiSystem(1, profile, entities, economy, construction, production);

            var kernel = new SimulationKernel(new SimRandom(333));
            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(aiSystem);
            kernel.Start();

            // The canonical power balance derives from building-role
            // entities: a completed HQ gives the AI slot exactly the target
            // margin (provisional 30), so the decision loop skips the
            // power-plant branch and goes straight to production.
            Assert.IsTrue(construction.PlaceCompletedBuilding(1, 3, 30, 30).IsValid, "completed HQ");

            Assert.AreEqual(0, production.TotalQueuedUnits);

            // Step 20 ticks to trigger the AI decision loop.
            for (int i = 0; i < 20; i++)
            {
                kernel.StepTick();
            }

            // The AI queued a Builder (definition id 1) at its HQ through the
            // canonical production domain; 800 AE were charged at enqueue.
            Assert.Greater(production.TotalQueuedUnits, 0);
            Assert.AreEqual(1200L, economy.GetPlayerEconomy(1).AetheriumCredits);
        }
    }
}
