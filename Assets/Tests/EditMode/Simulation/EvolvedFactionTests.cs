using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Factions;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class EvolvedFactionTests
    {
        [Test]
        public void BiomassGrid_SetBiomassCircle_ValidatesCells()
        {
            var grid = new BiomassGrid(32, 32);

            Assert.IsFalse(grid.HasBiomass(15, 15));
            grid.SetBiomassCircle(15, 15, radius: 4);

            Assert.IsTrue(grid.HasBiomass(15, 15));
            Assert.IsTrue(grid.IsOnBiomass(15.5f, 15.5f));
            Assert.IsFalse(grid.HasBiomass(25, 25));
        }

        [Test]
        public void EvolvedFactionSystem_BiomassRegeneration_HealsDamagedUnit()
        {
            var entities = new EntityManager(10);
            var grid = new BiomassGrid(64, 64);
            var evolved = new EvolvedFactionSystem(entities, grid);

            var kernel = new SimulationKernel(new SimRandom(444));
            kernel.RegisterSystem(evolved);
            kernel.Start();

            // Spread biomass around (20, 20)
            grid.SetBiomassCircle(20, 20, 5);

            EntityId id = entities.SpawnUnit(0, new Transform2D(20f, 20f), moveSpeed: 5f, radius: 0.5f, maxHealth: 100);
            ref UnitState u = ref entities.GetUnitRef(id);
            u.CurrentHealth -= 20; // Reduce health to 80

            Assert.AreEqual(80, u.CurrentHealth);

            // Step 10 ticks to trigger regeneration (+2 HP)
            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }

            Assert.AreEqual(82, u.CurrentHealth);
        }
    }
}
