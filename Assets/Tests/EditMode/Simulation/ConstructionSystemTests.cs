using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class ConstructionSystemTests
    {
        [Test]
        public void ConstructionGrid_CanPlaceBuilding_ValidatesOccupancy()
        {
            var grid = new ConstructionGrid(32, 32);

            Assert.IsTrue(grid.CanPlaceBuilding(10, 10, 3, 3));

            grid.OccupyCells(10, 10, 3, 3, 0);

            Assert.IsFalse(grid.CanPlaceBuilding(10, 10, 3, 3));
            Assert.IsFalse(grid.CanPlaceBuilding(11, 11, 2, 2)); // Overlap check
            Assert.IsTrue(grid.CanPlaceBuilding(15, 15, 3, 3)); // Separate location
        }

        [Test]
        public void ConstructionSystem_RequestConstruction_DeductsCreditsAndCompletes()
        {
            var grid = new ConstructionGrid(64, 64);
            var energy = new EnergyGridSystem(startingCredits: 500);
            var construction = new ConstructionSystem(grid, energy);

            var kernel = new SimulationKernel(new SimRandom(777));
            kernel.RegisterSystem(energy);
            kernel.RegisterSystem(construction);
            kernel.Start();

            var barracksDef = new BuildingDefinition(
                definitionId: 1,
                stringId: "BLD_Barracks",
                sizeX: 3,
                sizeY: 3,
                maxHealth: 500,
                powerProduced: 0,
                powerConsumed: 10,
                aetheriumCost: 150,
                buildTimeTicks: 10
            );

            bool success = construction.RequestConstruction(0, in barracksDef, 10, 10);
            Assert.IsTrue(success);

            ref PlayerEconomyState p0 = ref energy.GetPlayerEconomy(0);
            Assert.AreEqual(350, p0.AetheriumCredits);

            // Advance 10 ticks to complete construction
            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }

            // PowerConsumed should be updated to 10 upon completion
            Assert.AreEqual(10, p0.PowerConsumed);
        }
    }
}
