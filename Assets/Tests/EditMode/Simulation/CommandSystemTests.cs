using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Commands;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class CommandSystemTests
    {
        [Test]
        public void CommandProcessor_MoveCommand_UpdatesEntityTarget()
        {
            var entities = new EntityManager(10);
            var pathfinding = new PathfindingSystem(64, 64);
            var processor = new CommandProcessorSystem(entities, pathfinding);

            var kernel = new SimulationKernel(new SimRandom(1234));
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(processor);
            kernel.Start();

            EntityId id = entities.SpawnUnit(1, new Transform2D(10f, 10f), 5f);

            var cmd = new CommandEnvelope(
                type: CommandType.Move,
                issuer: CommandIssuer.Human,
                playerId: 1,
                sequence: 1,
                targetTick: new Tick(1),
                entityId: id,
                targetPositionX: 30f,
                targetPositionY: 30f
            );

            processor.SubmitCommand(in cmd);

            // Step tick
            kernel.StepTick();

            ref UnitState unit = ref entities.GetUnitRef(id);
            Assert.IsTrue(unit.IsMoving);
            Assert.AreEqual(30, unit.TargetGridPos.X);
            Assert.AreEqual(30, unit.TargetGridPos.Y);
        }
    }
}
