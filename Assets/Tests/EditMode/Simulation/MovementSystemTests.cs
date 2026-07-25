using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class MovementSystemTests
    {
        [Test]
        public void EntityManager_SpawnAndDespawn_RecyclesSlotsZeroGC()
        {
            var manager = new EntityManager(10);
            Assert.AreEqual(0, manager.ActiveCount);

            EntityId id1 = manager.SpawnUnit(1, new Transform2D(10f, 10f), 5f);
            EntityId id2 = manager.SpawnUnit(1, new Transform2D(20f, 20f), 5f);

            Assert.AreEqual(2, manager.ActiveCount);
            Assert.IsTrue(manager.IsValid(id1));
            Assert.IsTrue(manager.IsValid(id2));

            bool despawned = manager.DespawnUnit(id1);
            Assert.IsTrue(despawned);
            Assert.IsFalse(manager.IsValid(id1));
            Assert.AreEqual(1, manager.ActiveCount);

            // Re-spawning should recycle slot 0 with incremented version
            EntityId id3 = manager.SpawnUnit(1, new Transform2D(30f, 30f), 5f);
            Assert.AreEqual(id1.Index, id3.Index);
            Assert.AreNotEqual(id1.Version, id3.Version);
        }

        [Test]
        public void MovementSystem_UnitMovesTowardsTarget_UpdatesPosition()
        {
            var entities = new EntityManager(100);
            var pathfinding = new PathfindingSystem(64, 64);
            var movement = new MovementSystem(entities, pathfinding);

            var kernel = new SimulationKernel(new SimRandom(42));
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.Start();

            // Spawn unit at (10.5, 10.5)
            EntityId unitId = entities.SpawnUnit(1, new Transform2D(10.5f, 10.5f), 5f);
            ref UnitState unit = ref entities.GetUnitRef(unitId);

            var target = new GridPos2D(20, 10);
            pathfinding.RequestFlowField(target);
            unit.SetTarget(target);

            float initialX = unit.Transform.PositionX;

            // Step 10 ticks (0.5 seconds of movement)
            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }

            ref UnitState updatedUnit = ref entities.GetUnitRef(unitId);
            Assert.Greater(updatedUnit.Transform.PositionX, initialX);
        }
    }
}
