using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Commands;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Replays;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class LockstepReplayTests
    {
        [Test]
        public void ReplayBuffer_RecordsAndVerifiesStateHash()
        {
            var entities = new EntityManager(100);
            var pathfinding = new PathfindingSystem(64, 64);
            var movement = new MovementSystem(entities, pathfinding);
            var replay = new ReplayBuffer();

            var kernel = new SimulationKernel(new SimRandom(999));
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.Start();

            EntityId id = entities.SpawnUnit(1, new Transform2D(10f, 10f), 5f);
            var cmd = new CommandEnvelope(
                type: CommandType.Move,
                issuer: CommandIssuer.Human,
                playerId: 1,
                sequence: 1,
                targetTick: new Tick(1),
                entityId: id,
                targetPositionX: 10f,
                targetPositionY: 10f
            );
            replay.RecordCommand(1, in cmd);

            kernel.StepTick();

            ulong hash1 = StateHashUtility.CalculateStateHash(entities, kernel.CurrentTick.Value);
            Assert.AreEqual(1, replay.TotalCommands);
            Assert.AreNotEqual(0UL, hash1);
        }
    }
}
