using System.Collections.Generic;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Harvester auto-cycle suite (.NET lane): a full cargo starts the
    /// return leg while RETAINING <see cref="UnitState.HarvestFieldId"/>, so
    /// harvest -&gt; return -&gt; harvest repeats without any command and the
    /// economy produces an observable credit curve. Also pins the two
    /// boundaries of that behaviour: a commanded return (no field id) still
    /// ends idle, and a return leg without a refinery in reach HOLDS — this
    /// system issues no movement.
    /// Mirror of the EditMode lane HarvesterAutoCycleTests.
    /// </summary>
    [TestFixture]
    public sealed class HarvesterAutoCycleTests
    {
        private static EntityManager CreateEntities() => new EntityManager(64);

        private static EntityId SpawnHarvester(EntityManager entities, byte player, int x, int y)
        {
            return entities.SpawnUnit(
                player,
                new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                SimFixed.FromInt(4),
                role: UnitRole.Harvester);
        }

        private static void SpawnRefinery(EntityManager entities, byte player, int x, int y)
        {
            entities.SpawnUnit(
                player,
                new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                SimFixed.Zero,
                role: UnitRole.Refinery);
        }

        [Test]
        public void AutoCycle_RunsTwoFullCycles_WithoutAnyCommand_AndCreditsRiseMonotonically()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 5000), Is.True);

            // The harvester stands on the field AND in reach of its own
            // refinery, so the cycle closes without movement (EconomySystem
            // issues none — see the class remarks of EconomySystem).
            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            SpawnRefinery(entities, 0, 11, 10);
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            int capacity = SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance);
            int fillTicks = capacity / EconomySystem.HarvestRateAE; // 165
            int cycleTicks = fillTicks + 1;                       // + the deposit tick
            int ticks = (cycleTicks * 2) + 8;                     // two cycles plus slack

            long previous = economy.GetPlayerEconomy(0).AetheriumCredits;
            Assert.That(previous, Is.EqualTo(1000L), "manifest start credits");

            var depositTicks = new List<int>();
            for (int t = 1; t <= ticks; t++)
            {
                kernel.StepTick();
                long credits = economy.GetPlayerEconomy(0).AetheriumCredits;
                Assert.That(credits, Is.GreaterThanOrEqualTo(previous),
                    $"credits must never fall across the cycle (tick {t})");
                if (credits > previous)
                {
                    depositTicks.Add(t);
                }
                previous = credits;
            }

            Assert.That(depositTicks.Count, Is.EqualTo(2),
                "exactly two full harvest->return->harvest cycles complete unaided in this window");
            Assert.That(depositTicks[0], Is.EqualTo(cycleTicks),
                "the load is banked one tick after the cargo fills");
            Assert.That(depositTicks[1], Is.EqualTo(cycleTicks * 2),
                "the cycle repeats with a stable period — no command in between");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L + (2L * capacity)),
                "each cycle banks exactly one full cargo");

            ref UnitState unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.HarvestFieldId, Is.EqualTo((ushort)1),
                "the retained field id IS the auto-cycle mechanism");
            Assert.That(unit.IsReturningCargo, Is.False, "the third cycle is gathering again");
            Assert.That(unit.CargoAE, Is.GreaterThan(0),
                "gathering resumed on its own after the second deposit");

            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(5000L - (2L * capacity) - unit.CargoAE),
                "every AE that left the field is either banked or still in cargo");
        }

        [Test]
        public void FullCargo_StartsReturnLeg_AndHoldsWhenNoRefineryIsInReach()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.HarvestFieldId = 1;
            unit.CargoAE = SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance) - 1; // 329 of 330

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance)),
                "only the free cargo space is gathered");
            Assert.That(unit.IsReturningCargo, Is.True, "a full cargo starts the return leg");
            Assert.That(unit.HarvestFieldId, Is.EqualTo((ushort)1),
                "the field id survives the flip so the cycle can resume after the deposit");

            // No own refinery exists at all: the return leg HOLDS, it is
            // never dropped, and nothing here closes the distance.
            for (int i = 0; i < 20; i++)
            {
                kernel.StepTick();
            }

            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance)),
                "the load is held — neither banked nor gathered further");
            Assert.That(unit.IsReturningCargo, Is.True, "an out-of-reach return order is held, not dropped");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L),
                "nothing is banked without an own refinery in reach");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8999L),
                "a returning harvester stops draining the field");
        }

        [Test]
        public void CommandIssuedReturn_EndsIdle_AndDoesNotAutoResume()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            SpawnRefinery(entities, 0, 11, 10);

            // Exactly the state a ReturnCargo command writes through
            // UnitCommandStateView: returning flag set, field id CLEARED.
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.CargoAE = 100;
            unit.IsReturningCargo = true;
            unit.HarvestFieldId = 0;

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1100L),
                "credits rise by exactly the cargo");
            Assert.That(unit.CargoAE, Is.EqualTo(0));
            Assert.That(unit.IsReturningCargo, Is.False, "the deposit resolves the return leg");
            Assert.That(unit.HarvestFieldId, Is.EqualTo((ushort)0),
                "a commanded return carries no field id — it must not auto-resume");

            // The harvester stands ON field 1: if the commanded return had
            // leaked into an auto-cycle it would start gathering here.
            kernel.StepTick();
            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0),
                "the unit stays idle after a commanded return");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1100L));
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(9000L), "an idle harvester drains nothing");
        }
    }
}
