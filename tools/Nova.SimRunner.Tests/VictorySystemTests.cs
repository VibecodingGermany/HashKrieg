using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;
using Nova.Simulation.Victory;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Canonical MS-1 victory suite (.NET lane, docs/gamedesign/VictoryConditions.md
    /// section "MS-1-Override (D-056)" plus the D-077 second defeat trigger):
    /// the three decided outcomes (elimination, mutual annihilation, time
    /// limit), elimination's two triggers (total annihilation per D-056 and
    /// last-HQ loss per D-077), the undecided match, the "once decided,
    /// final" property across later ticks AND snapshot save/restore,
    /// construction sites counting as buildings, the last-unit reveal hold
    /// with its reset rule, block hardening (v2 format, clean break from v1)
    /// and determinism. Mirror of the EditMode lane VictorySystemTests.
    /// </summary>
    [TestFixture]
    public sealed class VictorySystemTests
    {
        private const ulong Seed = 0x5EED0056UL;
        private const int Capacity = 64;
        private const ushort MapSize = 64;

        /// <summary>Power plant / Barracks definition ids (SimDefinitions MS-1 table).</summary>
        private const ushort DefPower = 5;
        private const ushort DefBarracks = 7;

        /// <summary>
        /// Minimal canonical host: the systems the victory contract actually
        /// reads (entity store via Movement, construction sites) plus the
        /// victory system LAST, mirroring the canonical tick order.
        /// </summary>
        private sealed class TestHost
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public VictorySystem Victory;

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) Kernel.StepTick();
            }

            public EntityId SpawnUnit(byte slot, int x, int y, UnitRole role = UnitRole.BasicInfantry)
            {
                return Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                    SimFixed.FromInt(3),
                    role: role);
            }

            /// <summary>Despawns every living entity of a slot (the "wiped out" state D-056 judges).</summary>
            public void WipeSlot(byte slot)
            {
                UnitState[] units = Entities.RawUnits;
                for (int i = 0; i < Entities.Capacity; i++)
                {
                    if (units[i].IsActive && units[i].PlayerId == slot)
                    {
                        Entities.DespawnUnit(units[i].Id);
                    }
                }
            }

            /// <summary>Despawns every living HQ of a slot (the D-077 "HQ sniped" state, other entities survive).</summary>
            public void SnipeHq(byte slot)
            {
                UnitState[] units = Entities.RawUnits;
                for (int i = 0; i < Entities.Capacity; i++)
                {
                    if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == UnitRole.HQ)
                    {
                        Entities.DespawnUnit(units[i].Id);
                    }
                }
            }
        }

        private static TestHost NewHost(uint initialTick = 0, long startingCredits = 1000)
        {
            var kernel = new SimulationKernel(new SimRandom(Seed));
            var entities = new EntityManager(Capacity);
            var pathfinding = new PathfindingSystem(MapSize, MapSize);
            var movement = new MovementSystem(entities, pathfinding);
            var economy = new EconomySystem(entities, startingCredits);
            var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
            var victory = new VictorySystem(entities, construction);

            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.RegisterSystem(victory);
            kernel.Start(new Tick(initialTick));

            return new TestHost
            {
                Kernel = kernel,
                Entities = entities,
                Economy = economy,
                Construction = construction,
                Victory = victory,
            };
        }

        /// <summary>The standard two-sided opening: one building and one unit per slot.</summary>
        private static TestHost NewTwoSidedHost(uint initialTick = 0)
        {
            TestHost host = NewHost(initialTick);
            host.SpawnUnit(0, 10, 10, UnitRole.HQ);
            host.SpawnUnit(0, 12, 10);
            host.SpawnUnit(1, 50, 50, UnitRole.HQ);
            host.SpawnUnit(1, 52, 50);
            host.Step(1); // both slots latch as engaged
            return host;
        }

        // ------------------------------------------------------------------
        // (a) The undecided match
        // ------------------------------------------------------------------

        [Test]
        public void RunningMatch_ReportsUndecided()
        {
            TestHost host = NewTwoSidedHost();
            host.Step(25);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided),
                "both sides still own entities, so the match must not be decided");
            Assert.That(host.Victory.IsDecided, Is.False);
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo(VictorySystem.NoWinnerSlot));
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(0u));
            Assert.That(host.Victory.IsEngaged(0), Is.True);
            Assert.That(host.Victory.IsEngaged(1), Is.True);
            Assert.That(host.Victory.IsEngaged(2), Is.False, "an unused slot never engages");
        }

        [Test]
        public void EmptyHost_NeverDecides_BecauseNoSideWasEverOnTheMap()
        {
            // Without the engagement latch a fresh host would report
            // MutualAnnihilation on tick 1 — nobody owns anything.
            TestHost host = NewHost();
            host.Step(10);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided));
            Assert.That(host.Victory.IsEngaged(0), Is.False);
        }

        [Test]
        public void PartiallyWipedSide_WithEntitiesLeft_StaysUndecided()
        {
            // Slot 1 fields only units, so it never owns an HQ: the D-077
            // HQ-loss trigger never arms for it and D-056 alone judges it.
            TestHost host = NewHost();
            host.SpawnUnit(0, 10, 10, UnitRole.HQ);
            host.SpawnUnit(0, 12, 10);
            EntityId first = host.SpawnUnit(1, 50, 50);
            host.SpawnUnit(1, 52, 50);
            host.Step(1);

            // Slot 1 loses one of its two units: not eliminated.
            host.Entities.DespawnUnit(first);
            host.Step(1);

            host.Victory.CountLiving(1, out int remainingUnits, out int remainingBuildings);
            Assert.That(remainingUnits, Is.EqualTo(1));
            Assert.That(remainingBuildings, Is.EqualTo(0));
            Assert.That(host.Victory.HadHq(1), Is.False, "slot 1 never owned an HQ");
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided),
                "D-056 defeat needs zero units AND zero buildings, and slot 1 still owns a unit");
        }

        // ------------------------------------------------------------------
        // (b) Victory.Elimination
        // ------------------------------------------------------------------

        [Test]
        public void LosingSideWipedOut_EndsTheMatchWithTheSurvivorAsWinner()
        {
            TestHost host = NewTwoSidedHost();
            host.WipeSlot(1);
            host.Step(1);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)0), "slot 0 is the only side left");
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(host.Kernel.CurrentTick.Value));
            Assert.That(host.Victory.IsDecided, Is.True);
        }

        [Test]
        public void EitherSideCanWin_TheWinnerIsNotHardcoded()
        {
            TestHost host = NewTwoSidedHost();
            host.WipeSlot(0);
            host.Step(1);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)1));
        }

        // ------------------------------------------------------------------
        // (b2) The D-077 second defeat trigger: losing the last HQ
        // ------------------------------------------------------------------

        [Test]
        public void HqSnipedWithOtherEntitiesLeft_IsDefeated_TheOtherSlotWins()
        {
            // D-077: slot 1 loses its HQ but keeps its unit — the defeat
            // lands anyway, because the slot had owned a completed HQ.
            TestHost host = NewTwoSidedHost();
            host.SnipeHq(1);
            host.Step(1);

            host.Victory.CountLiving(1, out int remainingUnits, out int remainingBuildings);
            Assert.That(remainingUnits, Is.EqualTo(1), "the unit survives — the defeat comes from the HQ loss alone");
            Assert.That(remainingBuildings, Is.EqualTo(0));
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)0), "slot 0 still owns its HQ");
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(host.Kernel.CurrentTick.Value),
                "the defeat lands immediately, on the tick the HQ died");
        }

        [Test]
        public void BothHqsSnipedInTheSameTick_IsMutualAnnihilationDraw()
        {
            // Both sides keep their units — the draw comes from two HQ-loss
            // defeats in the same tick, not from total annihilation.
            TestHost host = NewTwoSidedHost();
            host.SnipeHq(0);
            host.SnipeHq(1);
            host.Step(1);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.DrawMutualAnnihilation));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo(VictorySystem.NoWinnerSlot), "a draw has no winner");
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(host.Kernel.CurrentTick.Value));
        }

        [Test]
        public void SideThatNeverHadAnHq_IsNotDefeatedByHqRule_ButStillByTotalAnnihilation()
        {
            // Slot 1 fields only units — the D-077 trigger never arms for it.
            TestHost host = NewHost();
            host.SpawnUnit(0, 10, 10, UnitRole.HQ);
            host.SpawnUnit(1, 50, 50);
            host.Step(5);

            Assert.That(host.Victory.HadHq(0), Is.True);
            Assert.That(host.Victory.HadHq(1), Is.False);
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided),
                "a slot that never owned an HQ cannot lose it");

            // D-056 total annihilation still defeats it.
            host.WipeSlot(1);
            host.Step(1);
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)0));
        }

        [Test]
        public void DecidedOutcome_IsFinal_WhenTheWinnersHqDiesLater()
        {
            TestHost host = NewTwoSidedHost();
            host.SnipeHq(1);
            host.Step(1);
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)0));
            uint decidedTick = host.Victory.DecidedTick;

            // The winner's HQ dies AFTER the decision — a naive re-evaluation
            // would flip this to MutualAnnihilation.
            host.SnipeHq(0);
            host.Step(50);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination), "the outcome is final (D-056)");
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)0));
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(decidedTick), "the decision tick never moves");
        }

        [Test]
        public void DecidedOutcome_DoesNotChangeOnLaterTicks()
        {
            TestHost host = NewTwoSidedHost();
            host.WipeSlot(1);
            host.Step(1);

            MatchOutcome decided = host.Victory.Outcome;
            byte winner = host.Victory.WinnerSlot;
            uint decidedTick = host.Victory.DecidedTick;
            Assert.That(decided, Is.EqualTo(MatchOutcome.VictoryElimination));

            // The winner is wiped out AFTER the decision — a naive
            // re-evaluation would flip this to MutualAnnihilation.
            host.WipeSlot(0);
            host.Step(50);

            Assert.That(host.Victory.Outcome, Is.EqualTo(decided), "the outcome is final (D-056)");
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo(winner));
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(decidedTick), "the decision tick never moves");
        }

        // ------------------------------------------------------------------
        // (c) Draw.MutualAnnihilation and Draw.TimeLimit
        // ------------------------------------------------------------------

        [Test]
        public void BothSidesWipedInTheSameTick_IsMutualAnnihilationDraw()
        {
            TestHost host = NewTwoSidedHost();
            host.WipeSlot(0);
            host.WipeSlot(1);
            host.Step(1);

            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.DrawMutualAnnihilation));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo(VictorySystem.NoWinnerSlot), "a draw has no winner");
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(host.Kernel.CurrentTick.Value));
        }

        [Test]
        public void TimeLimitTick_WithoutElimination_IsTimeLimitDraw()
        {
            TestHost host = NewTwoSidedHost(VictorySystem.TimeLimitTick - 3);

            Assert.That(host.Kernel.CurrentTick.Value, Is.EqualTo(VictorySystem.TimeLimitTick - 2));
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided));

            host.Step(1); // tick 26999
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided),
                "the tick before the limit must still be a running match");

            host.Step(1); // tick 27000
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.DrawTimeLimit));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo(VictorySystem.NoWinnerSlot));
            Assert.That(host.Victory.DecidedTick, Is.EqualTo(VictorySystem.TimeLimitTick));
        }

        [Test]
        public void EliminationOnTheLimitTick_BeatsTheTimeLimitDraw()
        {
            // D-056 reads "Tick 27.000 OHNE Eliminierung" — the elimination
            // check runs first, so the tie on the limit tick is a victory.
            TestHost host = NewTwoSidedHost(VictorySystem.TimeLimitTick - 2);
            host.WipeSlot(1);
            host.Step(1);

            Assert.That(host.Kernel.CurrentTick.Value, Is.EqualTo(VictorySystem.TimeLimitTick));
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)0));
        }

        // ------------------------------------------------------------------
        // (d) Construction sites count as buildings ("einschließlich Baustellen")
        // ------------------------------------------------------------------

        [Test]
        public void ConstructionSite_CountsAsBuilding_AndKeepsTheSideAlive()
        {
            TestHost host = NewHost();

            // Slot 0 gets a real construction site: power provider + builder
            // + credits are the placement prerequisites.
            EntityId power = host.Construction.PlaceCompletedBuilding(0, DefPower, 26, 20);
            Assert.That(power.IsValid, Is.True, "power provider");
            EntityId builder = host.SpawnUnit(0, 19, 20, UnitRole.Builder);
            host.Step(1);
            Assert.That(host.Construction.TryPlaceBuilding(0, DefBarracks, 20, 20), Is.True, "Barracks site");
            Assert.That(host.Construction.SiteCount, Is.EqualTo(1));

            // Slot 1 is the opponent that keeps the match two-sided.
            host.SpawnUnit(1, 50, 50);
            host.Step(1);

            // Strip slot 0 down to the bare site.
            host.Entities.DespawnUnit(power);
            host.Entities.DespawnUnit(builder);
            host.Step(1);

            host.Victory.CountLiving(0, out int units, out int buildings);
            Assert.That(units, Is.EqualTo(0), "the site must NOT be counted as a unit");
            Assert.That(buildings, Is.EqualTo(1), "a construction site counts on the building side (D-056)");
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided),
                "an unfinished site keeps its owner in the match");

            // Losing the site is the actual elimination.
            host.WipeSlot(0);
            host.Step(1);
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)1));
        }

        // ------------------------------------------------------------------
        // (e) The last-unit reveal hold
        // ------------------------------------------------------------------

        [Test]
        public void RevealHold_CountsUninterruptedTicks_AndResetsWhenTheConditionEnds()
        {
            TestHost host = NewHost();
            // Slot 0's building is a Barracks, not the HQ: the D-077 HQ-loss
            // trigger never arms for it, so stripping the building starts the
            // reveal hold instead of ending the match.
            EntityId building = host.SpawnUnit(0, 10, 10, UnitRole.Barracks);
            host.SpawnUnit(0, 12, 10);
            host.SpawnUnit(1, 50, 50, UnitRole.HQ);
            host.Step(1);

            Assert.That(host.Victory.RevealHoldTicksOf(0), Is.EqualTo(0),
                "a side that still owns a building never starts the hold");

            host.Entities.DespawnUnit(building); // no buildings, one unit left
            host.Step(5);
            Assert.That(host.Victory.RevealHoldTicksOf(0), Is.EqualTo(5));
            Assert.That(host.Victory.IsRevealed(0), Is.False);

            // The condition ends (a new building) — D-056 resets the counter.
            EntityId rebuilt = host.SpawnUnit(0, 14, 10, UnitRole.Barracks);
            host.Step(1);
            Assert.That(host.Victory.RevealHoldTicksOf(0), Is.EqualTo(0), "endet die Bedingung, wird der Zähler zurückgesetzt");
            Assert.That(host.Victory.IsRevealed(0), Is.False);

            host.Entities.DespawnUnit(rebuilt);
            host.Step(VictorySystem.RevealHoldTicks);
            Assert.That(host.Victory.RevealHoldTicksOf(0), Is.EqualTo(VictorySystem.RevealHoldTicks));
            Assert.That(host.Victory.IsRevealed(0), Is.True, "600 uninterrupted ticks reveal the last units");

            host.Step(10);
            Assert.That(host.Victory.RevealHoldTicksOf(0), Is.EqualTo(VictorySystem.RevealHoldTicks),
                "the counter saturates instead of growing without bound");
        }

        [Test]
        public void RevealHold_DoesNotStartAboveTheUnitThreshold()
        {
            TestHost host = NewHost();
            for (int i = 0; i < VictorySystem.RevealMaxUnits + 1; i++)
            {
                host.SpawnUnit(0, 10 + i, 10);
            }
            host.SpawnUnit(1, 50, 50);
            host.Step(20);

            Assert.That(host.Victory.RevealHoldTicksOf(0), Is.EqualTo(0),
                "four units are more than the D-056 threshold of three");
            Assert.That(host.Victory.RevealHoldTicksOf(1), Is.EqualTo(20),
                "the single-unit side is inside the threshold");
        }

        // ------------------------------------------------------------------
        // (f) Snapshot round trip
        // ------------------------------------------------------------------

        [Test]
        public void DecidedOutcome_SurvivesSnapshotSaveAndRestore()
        {
            TestHost source = NewTwoSidedHost();
            source.WipeSlot(1);
            source.Step(3);
            Assert.That(source.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));

            byte[] snapshot = source.Kernel.SaveSnapshot();
            ulong sourceHash = source.Kernel.CalculateStateHash();

            TestHost restored = NewHost();
            Assert.That(restored.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided), "a fresh host starts undecided");
            Assert.That(restored.Kernel.TryRestoreSnapshot(snapshot), Is.True);

            Assert.That(restored.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(restored.Victory.WinnerSlot, Is.EqualTo(source.Victory.WinnerSlot));
            Assert.That(restored.Victory.DecidedTick, Is.EqualTo(source.Victory.DecidedTick));
            Assert.That(restored.Victory.IsEngaged(0), Is.True);
            Assert.That(restored.Victory.IsEngaged(1), Is.True, "the engagement latch of the dead side restores too");
            Assert.That(restored.Kernel.CalculateStateHash(), Is.EqualTo(sourceHash),
                "the restored host is bit-identical, victory block included");

            // And it stays final on the restored host.
            restored.Step(20);
            Assert.That(restored.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(restored.Victory.DecidedTick, Is.EqualTo(source.Victory.DecidedTick));
        }

        [Test]
        public void VictoryBlockV2_RoundTripsHadHqLatches()
        {
            TestHost source = NewTwoSidedHost();
            source.Step(3);
            Assert.That(source.Victory.HadHq(0), Is.True, "both slots opened with a completed HQ");
            Assert.That(source.Victory.HadHq(1), Is.True);
            Assert.That(source.Victory.HadHq(2), Is.False, "an unused slot never owns an HQ");

            byte[] snapshot = source.Kernel.SaveSnapshot();
            TestHost restored = NewHost();
            Assert.That(restored.Victory.HadHq(0), Is.False, "a fresh host has no latches");
            Assert.That(restored.Kernel.TryRestoreSnapshot(snapshot), Is.True);

            Assert.That(restored.Victory.HadHq(0), Is.True, "the had-HQ latch restores");
            Assert.That(restored.Victory.HadHq(1), Is.True);
            Assert.That(restored.Victory.HadHq(2), Is.False);

            // Re-serializing the restored state reproduces the source block
            // byte-exactly — the latches live in the v2 block.
            var sourceWriter = new SnapshotBlockWriter();
            source.Victory.WriteState(sourceWriter);
            var restoredWriter = new SnapshotBlockWriter();
            restored.Victory.WriteState(restoredWriter);
            Assert.That(restoredWriter.ToArray(), Is.EqualTo(sourceWriter.ToArray()));
        }

        [Test]
        public void UndecidedStateWithRevealHold_SurvivesSnapshotSaveAndRestore()
        {
            TestHost source = NewHost();
            // Barracks, not the HQ: removing it must start the reveal hold,
            // not the D-077 HQ-loss defeat.
            EntityId building = source.SpawnUnit(0, 10, 10, UnitRole.Barracks);
            source.SpawnUnit(0, 12, 10);
            source.SpawnUnit(1, 50, 50, UnitRole.HQ);
            source.Step(1);
            source.Entities.DespawnUnit(building);
            source.Step(7);
            Assert.That(source.Victory.RevealHoldTicksOf(0), Is.EqualTo(7));

            byte[] snapshot = source.Kernel.SaveSnapshot();
            TestHost restored = NewHost();
            Assert.That(restored.Kernel.TryRestoreSnapshot(snapshot), Is.True);

            Assert.That(restored.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided));
            Assert.That(restored.Victory.RevealHoldTicksOf(0), Is.EqualTo(7), "the hold continues, it does not restart");
            restored.Step(3);
            Assert.That(restored.Victory.RevealHoldTicksOf(0), Is.EqualTo(10));
        }

        // ------------------------------------------------------------------
        // (g) Hash sensitivity and determinism
        // ------------------------------------------------------------------

        [Test]
        public void VictoryState_IsPartOfTheCanonicalStateHash()
        {
            TestHost undecided = NewTwoSidedHost();
            undecided.Step(1);
            ulong undecidedHash = undecided.Kernel.CalculateStateHash();

            TestHost decided = NewTwoSidedHost();
            decided.WipeSlot(1);
            decided.Step(1);

            Assert.That(decided.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination));
            Assert.That(decided.Kernel.CalculateStateHash(), Is.Not.EqualTo(undecidedHash),
                "a decided match must be visible in the canonical state hash");
        }

        [Test]
        public void TwoIdenticalHosts_ProduceIdenticalVictoryStateAndHashes()
        {
            TestHost a = NewTwoSidedHost();
            TestHost b = NewTwoSidedHost();
            a.WipeSlot(1);
            b.WipeSlot(1);
            a.Step(12);
            b.Step(12);

            Assert.That(b.Victory.Outcome, Is.EqualTo(a.Victory.Outcome));
            Assert.That(b.Victory.WinnerSlot, Is.EqualTo(a.Victory.WinnerSlot));
            Assert.That(b.Victory.DecidedTick, Is.EqualTo(a.Victory.DecidedTick));
            Assert.That(b.Kernel.CalculateStateHash(), Is.EqualTo(a.Kernel.CalculateStateHash()));
        }

        // ------------------------------------------------------------------
        // (h) Block hardening
        // ------------------------------------------------------------------

        [Test]
        public void VictoryBlock_HasTheCanonicalLayout()
        {
            TestHost host = NewTwoSidedHost();
            var writer = new SnapshotBlockWriter();
            host.Victory.WriteState(writer);
            byte[] content = writer.ToArray();

            Assert.That(host.Victory.StateBlockId, Is.EqualTo(SnapshotBlockIds.Victory));
            Assert.That(content.Length, Is.EqualTo(4 + 4 + VictorySystem.MaxSlots * 6),
                "version + slot count + outcome + winner + decided tick, then 6 bytes per slot (engaged, had-HQ, reveal hold)");
            Assert.That(content[0], Is.EqualTo(VictorySystem.StateVersion));
            Assert.That(content[1], Is.EqualTo((byte)VictorySystem.MaxSlots));
            Assert.That(content[2], Is.EqualTo((byte)MatchOutcome.Undecided));
            Assert.That(content[3], Is.EqualTo(VictorySystem.NoWinnerSlot));
            Assert.That(content[8], Is.EqualTo((byte)1), "slot 0 engaged");
            Assert.That(content[9], Is.EqualTo((byte)1), "slot 0 owned a completed HQ (D-077 latch)");
            Assert.That(content[8 + 6], Is.EqualTo((byte)1), "slot 1 engaged");
            Assert.That(content[8 + 6 + 1], Is.EqualTo((byte)1), "slot 1 owned a completed HQ");
            Assert.That(content[8 + (VictorySystem.MaxSlots - 1) * 6], Is.EqualTo((byte)0),
                "an unused slot carries zero latches");
            Assert.That(host.Victory.TryValidateState(content), Is.True);
        }

        [Test]
        public void VictoryBlock_RejectsMalformedContent_WithoutMutating()
        {
            TestHost host = NewTwoSidedHost();
            var writer = new SnapshotBlockWriter();
            host.Victory.WriteState(writer);
            byte[] valid = writer.ToArray();

            Assert.That(host.Victory.TryValidateState(ReadOnlySpan<byte>.Empty), Is.False, "empty");
            Assert.That(host.Victory.TryValidateState(new ReadOnlySpan<byte>(valid, 0, valid.Length - 1)), Is.False,
                "truncated");

            byte[] trailing = new byte[valid.Length + 1];
            Array.Copy(valid, trailing, valid.Length);
            Assert.That(host.Victory.TryValidateState(trailing), Is.False, "trailing bytes");

            Assert.That(host.Victory.TryValidateState(Mutate(valid, 0, 99)), Is.False, "unknown block version");
            Assert.That(host.Victory.TryValidateState(Mutate(valid, 1, 4)), Is.False, "wrong slot capacity");
            Assert.That(host.Victory.TryValidateState(Mutate(valid, 2, 9)), Is.False, "unknown outcome code");
            Assert.That(host.Victory.TryValidateState(Mutate(valid, 3, 0)), Is.False,
                "an undecided match must carry the winner sentinel");

            // A draw that names a winner.
            byte[] draw = (byte[])valid.Clone();
            draw[2] = (byte)MatchOutcome.DrawTimeLimit;
            draw[3] = 1;
            Assert.That(host.Victory.TryValidateState(draw), Is.False, "a draw has no winner slot");

            // An undecided match with a decision tick.
            byte[] tickOnUndecided = (byte[])valid.Clone();
            tickOnUndecided[4] = 7;
            Assert.That(host.Victory.TryValidateState(tickOnUndecided), Is.False,
                "an undecided match has no decision tick");

            // A victory whose winner never was on the map (slot 5 is unengaged).
            byte[] ghostWinner = (byte[])valid.Clone();
            ghostWinner[2] = (byte)MatchOutcome.VictoryElimination;
            ghostWinner[3] = 5;
            ghostWinner[4] = 3;
            Assert.That(host.Victory.TryValidateState(ghostWinner), Is.False, "the winner must be an engaged slot");

            // A reveal hold above the saturation bound (slot 0's hold starts
            // two flag bytes into its 6-byte record).
            byte[] overRun = (byte[])valid.Clone();
            WriteUInt32(overRun, 8 + 2, (uint)VictorySystem.RevealHoldTicks + 1);
            Assert.That(host.Victory.TryValidateState(overRun), Is.False, "the hold saturates at 600");

            // A hold on a slot that was never engaged (slot 7).
            byte[] ghostHold = (byte[])valid.Clone();
            WriteUInt32(ghostHold, 8 + (VictorySystem.MaxSlots - 1) * 6 + 2, 3u);
            Assert.That(host.Victory.TryValidateState(ghostHold), Is.False,
                "a slot that never was on the map cannot hold the reveal condition");

            // An illegal had-HQ flag value on an engaged slot (slot 0).
            Assert.That(host.Victory.TryValidateState(Mutate(valid, 8 + 1, 2)), Is.False,
                "the had-HQ latch is a 0/1 flag");

            // A had-HQ latch on a slot that was never engaged (slot 7) —
            // owning an HQ would have latched the engagement (D-077).
            Assert.That(host.Victory.TryValidateState(Mutate(valid, 8 + (VictorySystem.MaxSlots - 1) * 6 + 1, 1)), Is.False,
                "an unengaged slot cannot carry the had-HQ latch");

            // Clean break: a v1 block (D-056 layout, 48 bytes, version byte 1)
            // is rejected, not migrated (pre-release format reset).
            var v1Writer = new SnapshotBlockWriter();
            v1Writer.WriteUInt8(1);
            v1Writer.WriteUInt8((byte)VictorySystem.MaxSlots);
            v1Writer.WriteUInt8((byte)MatchOutcome.Undecided);
            v1Writer.WriteUInt8(VictorySystem.NoWinnerSlot);
            v1Writer.WriteUInt32(0);
            for (int slot = 0; slot < VictorySystem.MaxSlots; slot++)
            {
                v1Writer.WriteUInt8(0); // engagement latch
                v1Writer.WriteUInt32(0); // reveal hold
            }
            byte[] v1 = v1Writer.ToArray();
            Assert.That(v1.Length, Is.EqualTo(48), "the v1 fixture matches the documented D-056 layout");
            Assert.That(host.Victory.TryValidateState(v1), Is.False, "v1 blocks are rejected, not migrated");
            // Even a version-byte-patched v1 block fails: it is 8 bytes short
            // of the v2 layout.
            Assert.That(host.Victory.TryValidateState(Mutate(v1, 0, VictorySystem.StateVersion)), Is.False,
                "a relabeled v1 block is still the wrong length");

            // Nothing above touched the live system.
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.Undecided));
            Assert.That(host.Victory.TryValidateState(valid), Is.True);
        }

        private static byte[] Mutate(byte[] source, int offset, byte value)
        {
            var copy = (byte[])source.Clone();
            copy[offset] = value;
            return copy;
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        // ------------------------------------------------------------------
        // (i) Registration contract
        // ------------------------------------------------------------------

        [Test]
        public void VictorySystem_IsStateful_AndOwnsAnUncontestedBlockId()
        {
            TestHost host = NewHost();
            Assert.That(host.Victory, Is.InstanceOf<IStatefulSimSystem>());
            Assert.That(host.Victory.StateBlockId, Is.Not.EqualTo(SnapshotBlockIds.Kernel));
            Assert.That(host.Victory.StateBlockId, Is.EqualTo(SnapshotBlockIds.FirstSystemBlock + 7));
            Assert.That(VictorySystem.MaxSlots, Is.EqualTo(CommandLimits.ReservedPlayerSlots));
            Assert.That(VictorySystem.TimeLimitTick,
                Is.EqualTo(45u * 60u * (uint)SimClock.TicksPerSecond), "27,000 ticks = 45 min at 10 Hz");
            Assert.That(VictorySystem.RevealHoldTicks,
                Is.EqualTo(60 * SimClock.TicksPerSecond), "600 ticks = 60 s at 10 Hz");
        }
    }
}
