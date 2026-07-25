using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Combat;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class CombatSystemTests
    {
        [Test]
        public void CombatSystem_AttacksTarget_ReducesHealthAndDespawns()
        {
            var entities = new EntityManager(10);
            var combat = new CombatSystem(entities);

            var kernel = new SimulationKernel(new SimRandom(4321));
            kernel.RegisterSystem(combat);
            kernel.Start();

            // Attacker at (10, 10), Target at (12, 10) - distance 2.0 <= range 8.0
            EntityId attackerId = entities.SpawnUnit(1, new Transform2D(10f, 10f), 5f);
            EntityId targetId = entities.SpawnUnit(2, new Transform2D(12f, 10f), 5f, maxHealth: 20);

            ref UnitState attacker = ref entities.GetUnitRef(attackerId);
            attacker.AttackTarget = targetId;

            // Tick 1: Fires shot (15 damage), target health becomes 5
            kernel.StepTick();
            ref UnitState target = ref entities.GetUnitRef(targetId);
            Assert.AreEqual(5, target.CurrentHealth);

            // Step 10 ticks for weapon cooldown to reset
            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }

            // Target health should be 0 and despawned
            Assert.IsFalse(entities.IsValid(targetId));
        }
    }
}
