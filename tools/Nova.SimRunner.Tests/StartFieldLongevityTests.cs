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
    /// Sprint 21 package 21.3 (issue #87): the start field's longevity is
    /// MEASURED, not guessed. The T-01 tester estimated the start reserve at
    /// 5.000 AE — it is 9.000 (D-102/D-107) — because nothing showed it; the
    /// fix for that was 21.2. What was still missing is the honest number for
    /// the design question "how long should a start field carry": this suite
    /// runs the real auto-cycle (gather 2 AE/tick until the 330-AE cargo is
    /// full, deposit on the return leg, resume the retained field id —
    /// EconomySystem's own loop, driven exactly like the
    /// HarvesterAutoCycleTests fixture) on the canonical opening geometry:
    /// field (7,7), refinery footprint centre (9,5), harvesters adjacent to
    /// the field. The footprint reach rule (D-104,
    /// EconomySystem.HasOwnRefineryInReach) lets a harvester deposit from the
    /// field cell itself, so no movement is registered — the measured time is
    /// the pure gather cycle, exactly the regime the opening is built for.
    /// 10 ticks = 1 second (SimClock.TicksPerSecond).
    /// <para>
    /// The pins are measurements of the CURRENT constants (reserve 9.000,
    /// HarvestRateAE 2, Alliance cargo 330). A change to any of them —
    /// including a decided reserve change of the four mirrored field-layout
    /// literals — moves these numbers on purpose and reopens the package.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class StartFieldLongevityTests
    {
        private const ushort FieldId = 1;
        private const long StartReserveAE = 9000L; // D-102/D-107 canonical start field
        private const long TickCap = 20000;

        /// <summary>Ticks until the start field is exhausted with <paramref name="harvesterCount"/> harvesters working it from tick 0.</summary>
        private static long MeasureExhaustionTick(int harvesterCount)
        {
            var entities = new EntityManager(64);
            var economy = new EconomySystem(entities);
            var kernel = new SimulationKernel(new SimRandom(0x5EED42UL));
            kernel.RegisterSystem(economy);

            Assert.That(economy.TryAddField(FieldId, new GridPos2D(7, 7), StartReserveAE), Is.True,
                "canonical start field (7,7) with 9.000 AE");

            // Refinery entity at its footprint centre (8,4)-(10,6) -> (9,5).
            entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(9), SimFixed.FromInt(5)),
                SimFixed.Zero, role: UnitRole.Refinery);

            // An HQ, because since #136 the harvester stops extracting once the
            // account sits at its ceiling — and CapacityFor returns ZERO for a
            // slot without a completed HQ. Without one this fixture would read
            // as permanently full and no harvester would ever move: the
            // measurement would have no answer rather than a different one.
            // The canonical opening has an HQ, so this restores the fixture to
            // the situation it always claimed to measure.
            entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)),
                SimFixed.Zero, role: UnitRole.HQ);

            Assert.That(
                SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.Harvester, out SimUnitDefinition harvesterDef),
                Is.True, "Alliance harvester definition");

            // Adjacent-to-field cells that are ALSO within the refinery
            // footprint's deposit reach: measured against the centre (9,5)
            // with reach 2, the field ring (6..8, 6..8) intersects it only on
            // (7,6), (8,6) and (8,7). A harvester anywhere else fills one
            // cargo and then HOLDS forever — which is itself part of the 21.3
            // finding: the canonical opening supports at most ~3 walk-free
            // harvesters per field.
            (int X, int Y)[] spots = { (7, 6), (8, 6), (8, 7) };
            for (int i = 0; i < harvesterCount; i++)
            {
                EntityId id = entities.SpawnUnit(
                    0,
                    new Transform2D(SimFixed.FromInt(spots[i].X), SimFixed.FromInt(spots[i].Y)),
                    harvesterDef.MoveSpeed, role: UnitRole.Harvester);
                // Start mid-cycle like a fresh harvest order: the auto-cycle
                // retains the field id across every return leg.
                entities.GetUnitRef(id).HarvestFieldId = FieldId;
            }

            kernel.Start();
            while (kernel.CurrentTick.Value < TickCap)
            {
                // The fixture models a player who SPENDS everything, and it has
                // to since #136: a harvester now stops extracting once the
                // account sits at its ceiling. Without a consumer this loop
                // stalls at the cap and the field is never exhausted — the
                // measurement would have no answer rather than a different one.
                // Draining to zero every tick isolates what 21.3 actually asks:
                // how long the FIELD lasts at the pure extraction rate, with the
                // storage ceiling deliberately taken out of the question. The
                // tick counts below are therefore unchanged from the original
                // measurement — the harvest dynamics never changed, only the
                // condition under which they pause.
                economy.GetPlayerEconomy(0).AetheriumCredits = 0L;
                kernel.StepTick();
                if (economy.TryGetField(FieldId, out AetheriumField field) && field.IsExhausted)
                {
                    return (long)kernel.CurrentTick.Value;
                }
            }
            Assert.Fail($"field not exhausted after {TickCap} ticks with {harvesterCount} harvester(s)");
            return -1;
        }

        [Test]
        public void StartField_LastsTheMeasuredTicks_OneHarvester()
        {
            long ticks = MeasureExhaustionTick(1);
            TestContext.Out.WriteLine($"1 harvester: {ticks} ticks = {ticks / 10.0:0.#} s = {ticks / 600.0:0.##} min");
            // Arithmetic expectation: 9.000 AE at 2 AE/tick = 4.500 gather
            // ticks, plus one return tick per 330-AE cargo trip (28 trips).
            Assert.That(ticks, Is.EqualTo(4527),
                "measured solo longevity of the 9.000-AE start field (~7:33 min)");
        }

        [Test]
        public void StartField_LastsTheMeasuredTicks_TwoHarvesters()
        {
            long ticks = MeasureExhaustionTick(2);
            TestContext.Out.WriteLine($"2 harvesters: {ticks} ticks = {ticks / 10.0:0.#} s = {ticks / 600.0:0.##} min");
            Assert.That(ticks, Is.EqualTo(2263),
                "measured two-harvester longevity (~3:46 min) — half the solo time plus per-trip overheads");
        }

        [Test]
        public void StartField_LastsTheMeasuredTicks_ThreeHarvesters()
        {
            long ticks = MeasureExhaustionTick(3);
            TestContext.Out.WriteLine($"3 harvesters: {ticks} ticks = {ticks / 10.0:0.#} s = {ticks / 600.0:0.##} min");
            Assert.That(ticks, Is.EqualTo(1509),
                "measured three-harvester longevity (~2:31 min) — a third of the solo time plus per-trip overheads");
        }

        [Test]
        public void StartField_LongevityScalesWithHarvesterCount()
        {
            long solo = MeasureExhaustionTick(1);
            long duo = MeasureExhaustionTick(2);
            long trio = MeasureExhaustionTick(3);

            // Near-perfect scaling: the gather rate dominates and every
            // harvester gathers in parallel at 2 AE/tick; only the per-trip
            // return tick breaks exact 1/N.
            Assert.That((double)duo / solo, Is.InRange(0.45, 0.55), "two harvesters halve the field's life");
            Assert.That((double)trio / solo, Is.InRange(0.28, 0.38), "three harvesters cut it to a third");
        }
    }
}
