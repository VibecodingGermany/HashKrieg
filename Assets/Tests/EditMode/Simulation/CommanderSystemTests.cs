using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Commanders;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class CommanderSystemTests
    {
        [Test]
        public void CommanderSystem_TryActivateAbility_DeductsEnergyAndAppliesEffect()
        {
            var entities = new EntityManager(10);
            var commander = new CommanderSystem(entities);

            var kernel = new SimulationKernel(new SimRandom(111));
            kernel.RegisterSystem(commander);
            kernel.Start();

            // Spawn enemy unit for Player 1 at (20, 20) with 100 HP
            EntityId enemyId = entities.SpawnUnit(playerId: 1, new Transform2D(20f, 20f), moveSpeed: 5f, radius: 0.5f, maxHealth: 100);

            var orbitalStrikeDef = new CommanderAbilityDefinition(
                abilityId: 1,
                stringId: "CMD_OrbitalStrike",
                energyCost: 30,
                cooldownTicks: 40,
                radius: 5.0f,
                effectValue: 50
            );

            ref PlayerCommanderState p0 = ref commander.GetPlayerState(0);
            Assert.AreEqual(50, p0.CurrentEnergy);

            // Activate ability at (20, 20)
            bool success = commander.TryActivateAbility(playerId: 0, in orbitalStrikeDef, targetX: 20f, targetY: 20f);
            Assert.IsTrue(success);

            Assert.AreEqual(20, p0.CurrentEnergy);
            Assert.AreEqual(40, p0.CooldownRemainingTicks);

            // Enemy HP should now be 50
            ref UnitState enemy = ref entities.GetUnitRef(enemyId);
            Assert.AreEqual(50, enemy.CurrentHealth);
        }
    }
}
