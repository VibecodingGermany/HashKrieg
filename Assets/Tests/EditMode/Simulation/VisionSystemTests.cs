using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class VisionSystemTests
    {
        [Test]
        public void VisionGrid_TransitionsFromUnexploredToExploredAndVisible()
        {
            var grid = new VisionGrid(32, 32);

            Assert.AreEqual(VisionState.Unexplored, grid.GetVisionState(0, 15, 15));

            grid.RevealCircle(0, 15, 15, radius: 5);
            Assert.AreEqual(VisionState.Visible, grid.GetVisionState(0, 15, 15));

            grid.DemoteVisibleToExplored(0);
            Assert.AreEqual(VisionState.Explored, grid.GetVisionState(0, 15, 15));
        }

        [Test]
        public void VisionSystem_UnitPosition_RevealsSightRadiusOnTick()
        {
            var entities = new EntityManager(10);
            var grid = new VisionGrid(64, 64);
            var vision = new VisionSystem(entities, grid);

            var kernel = new SimulationKernel(new SimRandom(555));
            kernel.RegisterSystem(vision);
            kernel.Start();

            // Spawn unit for Player 0 at (30.5, 30.5)
            EntityId id = entities.SpawnUnit(0, new Transform2D(30.5f, 30.5f), 5f);

            Assert.AreEqual(VisionState.Unexplored, grid.GetVisionState(0, 30, 30));

            // Step 4 ticks to trigger vision system update
            for (int i = 0; i < 4; i++)
            {
                kernel.StepTick();
            }

            Assert.AreEqual(VisionState.Visible, grid.GetVisionState(0, 30, 30));
        }
    }
}
