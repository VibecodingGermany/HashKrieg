using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nova.AI;
using Nova.AI.Data;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// The per-match AI personality (behaviour r9): the derivation is a pure
    /// function of (seed, slot), the neutral personality changes nothing, and
    /// every personality still plays a match to its end. The neutral half of
    /// the claim is pinned by <c>CanonicalAiOutcomeTests</c>, which keeps
    /// playing <c>default</c> and must stay green unchanged.
    /// </summary>
    [TestFixture]
    public sealed class AiPersonalityTests
    {
        private const byte AiSlot = 1;
        private const byte HumanSlot = 0;

        // ----------------------------------------------------------------
        // Derive and Apply — pure data
        // ----------------------------------------------------------------

        [Test]
        public void Derive_IsPure_AndCoversEveryBandOver512Seeds()
        {
            var openings = new int[4];
            var flanks = new HashSet<int>();
            var layouts = new HashSet<int>();
            const int count = 512;
            for (ulong seed = 0; seed < count; seed++)
            {
                AiPersonality p = AiPersonality.Derive(seed * 0x9E3779B97F4A7C15UL + 1, AiSlot);
                Assert.That(AiPersonality.Derive(seed * 0x9E3779B97F4A7C15UL + 1, AiSlot), Is.EqualTo(p));
                openings[(int)p.Opening]++;
                flanks.Add(p.FlankOffsetCells);
                layouts.Add(p.LayoutVariant);

                Assert.That(p.FlankOffsetCells, Is.AnyOf(-10, 0, 10));
                Assert.That(p.LayoutVariant, Is.InRange((byte)0, (byte)7));
                // Resulting values against the MatchRunner base (12 / 6 / 2 / 60 / 10).
                int army = 12 + p.ArmySizeDelta;
                int retreat = 60 + p.RetreatPercentDelta;
                switch (p.Opening)
                {
                    case AiOpening.Balanced:
                        Assert.That(army, Is.InRange(11, 13));
                        Assert.That(retreat, Is.InRange(55, 65));
                        Assert.That(p.DefensePlatforms, Is.Zero);
                        break;
                    case AiOpening.Rush:
                        Assert.That(army, Is.InRange(7, 9));
                        Assert.That(6 + p.SquadThresholdDelta, Is.EqualTo(4));
                        Assert.That(retreat, Is.InRange(40, 50));
                        break;
                    case AiOpening.Economy:
                        Assert.That(army, Is.InRange(15, 17));
                        Assert.That(2 + p.HarvesterDelta, Is.EqualTo(3));
                        Assert.That(retreat, Is.InRange(60, 70));
                        break;
                    case AiOpening.Turtle:
                        Assert.That(army, Is.InRange(13, 15));
                        Assert.That(retreat, Is.InRange(60, 70));
                        // Asks for 10 + 4; Apply caps it one short of the staging cell (12).
                        Assert.That(p.Apply(AiProfiles.Ms1Canonical).DefendHomeCells, Is.EqualTo(11));
                        Assert.That(p.DefensePlatforms, Is.InRange(1, 2));
                        break;
                }
            }

            for (int o = 0; o < 4; o++)
            {
                double share = openings[o] / (double)count;
                Assert.That(share, Is.InRange(0.15, 0.35), $"opening {(AiOpening)o} share");
            }
            Assert.That(flanks, Is.EquivalentTo(new[] { -10, 0, 10 }));
            Assert.That(layouts.Count, Is.EqualTo(8));
        }

        [Test]
        public void Derive_DependsOnSlotToo()
        {
            int differing = 0;
            for (ulong seed = 1; seed <= 64; seed++)
            {
                if (!AiPersonality.Derive(seed, 0).Equals(AiPersonality.Derive(seed, 1))) differing++;
            }
            Assert.That(differing, Is.GreaterThan(32));
        }

        [Test]
        public void Neutral_ApplyReturnsTheBaseProfileUnchanged()
        {
            Assert.That(AiPersonality.Neutral, Is.EqualTo(default(AiPersonality)));
            Assert.That(default(AiPersonality).IsNeutral, Is.True);
            AiProfile shipped = AiProfiles.Ms1Canonical;
            Assert.That(AiPersonality.Neutral.Apply(shipped), Is.EqualTo(shipped));
            Assert.That(AiBehaviorId.ComputeProfileHash(AiPersonality.Neutral.Apply(shipped)),
                Is.EqualTo(AiBehaviorId.ProfileHash));
        }

        [Test]
        public void Apply_NeverSwitchesOnARuleTheBaseProfileHasOff()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            AiProfile off = new AiProfile(
                "off", shipped.DecisionTickInterval, shipped.PlacementSearchRadius, 0, 2,
                shipped.HarvesterQueueBatch, 12, 6, shipped.InfantryQueueBatch,
                shipped.TargetDamageWeight, shipped.TargetThreatWeight, shipped.TargetFinishWeight,
                shipped.TargetDistanceWeight, shipped.WaveSize, shipped.StagingDistanceCells,
                shipped.StagingToleranceCells, retreatHealthPercent: 0, shipped.RetreatDangerCells,
                shipped.WaveStrengthPoints, defendHomeCells: 0);
            foreach (AiOpening opening in new[] { AiOpening.Balanced, AiOpening.Rush, AiOpening.Economy, AiOpening.Turtle })
            {
                AiProfile applied = AiPersonality.Create(opening, 0, 0, 1, 1, 1).Apply(off);
                Assert.That(applied.RetreatHealthPercent, Is.Zero, opening.ToString());
                Assert.That(applied.DefendHomeCells, Is.Zero, opening.ToString());
            }
        }

        [Test]
        public void Apply_RenamesAndClamps()
        {
            AiProfile applied = AiPersonality.Create(AiOpening.Rush, 0, 0, -1, -1).Apply(AiProfiles.Ms1Canonical);
            Assert.That(applied.ProfileId, Is.EqualTo(AiProfiles.Ms1Canonical.ProfileId + "+rush"));
            Assert.That(applied.AttackSquadThreshold, Is.GreaterThanOrEqualTo(1));
            Assert.That(applied.TargetArmySize, Is.GreaterThanOrEqualTo(applied.AttackSquadThreshold));
            Assert.That(applied.RetreatHealthPercent, Is.InRange(0, 95));
        }

        // ----------------------------------------------------------------
        // Whole matches
        // ----------------------------------------------------------------

        private sealed class Outcome
        {
            public uint DecidedTick;
            public ulong EndState;
            public int BarracksTick, RefineryTick, PlatformCount;
        }

        private static Outcome Play(ulong seed, AiPersonality personality, FactionId aiFaction)
        {
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(
                seed, personality: personality, aiFaction: aiFaction);
            var outcome = new Outcome();
            for (int i = 0; i < SkirmishAiTests.EndToEndBudgetTicks && !host.Victory.IsDecided; i++)
            {
                host.Step();
                uint tick = host.Kernel.CurrentTick.Value;
                if (outcome.BarracksTick == 0 && CountCompleted(host, UnitRole.Barracks) > 0) outcome.BarracksTick = (int)tick;
                if (outcome.RefineryTick == 0 && CountCompleted(host, UnitRole.Refinery) > 0) outcome.RefineryTick = (int)tick;
            }
            outcome.PlatformCount = CountCompleted(host, UnitRole.DefensePlatform);
            outcome.DecidedTick = host.Victory.DecidedTick;
            outcome.EndState = host.Kernel.CalculateStateHash();
            return outcome;
        }

        /// <summary>Completed own buildings of a role — an unfinished site carries its definition role too, so sites are excluded.</summary>
        private static int CountCompleted(SkirmishAiTests.AiHost host, UnitRole role)
        {
            int count = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != AiSlot || u.Role != role) continue;
                uint raw = UnitCommandStateView.ToRawEntityId(u.Id);
                if (host.Construction.TryGetSite(raw, out _, out _, out _)) continue;
                count++;
            }
            return count;
        }

        [Test]
        public void EveryOpening_FlankAndFaction_FinishesTheMatch()
        {
            var ticks = new Dictionary<string, List<uint>>();
            foreach (FactionId faction in new[] { FactionId.Legion, FactionId.Alliance })
            {
                foreach (AiOpening opening in new[] { AiOpening.Balanced, AiOpening.Rush, AiOpening.Economy, AiOpening.Turtle })
                {
                    foreach (int flank in new[] { -10, 0, 10 })
                    {
                        // Layout 5 (x descending, x outer) exercises the non-default scan.
                        AiPersonality p = AiPersonality.Create(opening, flank, layoutVariant: 5, extraDefensePlatform: 1);
                        Outcome o = Play(SkirmishAiTests.Seed, p, faction);
                        string label = $"{faction} {opening} flank {flank}";
                        Assert.That(o.DecidedTick, Is.Not.Zero, label + " did not decide the match");
                        TestContext.Progress.WriteLine($"[personality] {label}: decided {o.DecidedTick}, " +
                            $"barracks {o.BarracksTick}, refinery {o.RefineryTick}, platforms {o.PlatformCount}");
                        if (opening == AiOpening.Turtle)
                            Assert.That(o.PlatformCount, Is.GreaterThanOrEqualTo(1), label + " built no defence platform");
                        if (opening == AiOpening.Rush)
                            Assert.That(o.BarracksTick, Is.GreaterThan(0).And.LessThan(o.RefineryTick), label + " Barracks before Refinery");
                    }
                }
            }
        }

        [Test]
        public void SameSeedSamePersonality_SameMatch()
        {
            const ulong seed = 0xDE7E000000010271UL;
            AiPersonality p = AiPersonality.Derive(seed, AiSlot);
            Outcome a = Play(seed, p, FactionId.Legion);
            Outcome b = Play(seed, p, FactionId.Legion);
            Assert.That(a.DecidedTick, Is.Not.Zero);
            Assert.That(b.DecidedTick, Is.EqualTo(a.DecidedTick));
            Assert.That(b.EndState, Is.EqualTo(a.EndState));
        }

        [Test]
        public void SeedsWithDifferentOpenings_PlayDifferentMatches()
        {
            var seedByOpening = new Dictionary<AiOpening, ulong>();
            for (ulong seed = 1; seed < 200 && seedByOpening.Count < 4; seed++)
            {
                AiOpening o = AiPersonality.Derive(seed, AiSlot).Opening;
                if (!seedByOpening.ContainsKey(o)) seedByOpening[o] = seed;
            }
            Assert.That(seedByOpening.Count, Is.EqualTo(4));

            var signatures = new HashSet<(uint, ulong)>();
            foreach (KeyValuePair<AiOpening, ulong> pair in seedByOpening)
            {
                Outcome o = Play(pair.Value, AiPersonality.Derive(pair.Value, AiSlot), FactionId.Legion);
                Assert.That(o.DecidedTick, Is.Not.Zero, $"seed {pair.Value} ({pair.Key})");
                signatures.Add((o.DecidedTick, o.EndState));
            }
            Assert.That(signatures.Count, Is.GreaterThan(1), "four different openings produced identical matches");
        }

        // ----------------------------------------------------------------
        // Flank cells on the real map
        // ----------------------------------------------------------------

        /// <summary>
        /// Every flank on both seats keeps the gathering point inside the
        /// staging ring, on the grid and off the rock.
        /// <para>
        /// THE RING IS THE POINT. <c>IsCommittedToTheWave</c> measures from the
        /// HQ cell and calls everything beyond
        /// <c>StagingDistanceCells + StagingToleranceCells</c> a unit of an
        /// earlier wave. The first version of the flank pushed the cell
        /// perpendicular to the diagonal of the Glutrinne, Chebyshev 22 from
        /// the HQ — every unit on its way there turned "committed" and marched
        /// alone (review of PR #144, B1).
        /// </para>
        /// <para>
        /// The HQ and the enemy field are read off the built match the way the
        /// AI reads them — the HQ cell is <c>GridCellOf</c> of its position, not
        /// the footprint origin the previous version of this test used (B4).
        /// </para>
        /// </summary>
        [TestCase((byte)1)]
        [TestCase((byte)0)]
        public void FlankedStagingCells_StayInsideTheRingAndWalkable(byte slot)
        {
            AiProfile profile = AiProfiles.Ms1Canonical;
            int distance = profile.StagingDistanceCells;
            int ring = distance + profile.StagingToleranceCells;

            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed);
            Assert.That(SkirmishGoalTests.TryHqCell(host, slot, out int hqX, out int hqY), Is.True);
            FarthestFieldCell(host, hqX, hqY, out int enemyX, out int enemyY);
            MirrorStagingCell(hqX, hqY, enemyX, enemyY, distance, 0, out int centreX, out int centreY);

            foreach (int flank in new[] { -10, 0, 10 })
            {
                MirrorStagingCell(hqX, hqY, enemyX, enemyY, distance, flank, out int x, out int y);
                string label = $"slot {slot} (HQ cell {hqX},{hqY}) flank {flank}: cell ({x},{y})";
                int reach = Math.Max(Math.Abs(x - hqX), Math.Abs(y - hqY));
                TestContext.Progress.WriteLine($"[personality] {label}, Chebyshev {reach} from the HQ");

                Assert.That(reach + profile.StagingToleranceCells, Is.LessThanOrEqualTo(ring),
                    label + " lies outside the staging ring");
                Assert.That(x, Is.InRange(0, ConstructionSystem.GridSize - 1), label);
                Assert.That(y, Is.InRange(0, ConstructionSystem.GridSize - 1), label);
                Assert.That(CanonicalTerrainMirror.IsImpassable(x, y), Is.False, label + " is rock");
                if (flank != 0)
                    Assert.That(x != centreX || y != centreY, Is.True, label + ": the flank must actually move the cell");
            }
        }

        /// <summary>
        /// The mirror above is only worth something while it computes what the
        /// AI computes. The AI reports its staging cell to a goal observer, so
        /// on the seat the harness can play (slot 1) the mirror is checked
        /// against the production value instead of against itself.
        /// </summary>
        [TestCase(-10)]
        [TestCase(0)]
        [TestCase(10)]
        public void FlankedStagingCell_IsTheOneTheAiReports(int flank)
        {
            var observer = new WaveObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(
                SkirmishAiTests.Seed, goalObserver: observer,
                personality: AiPersonality.Create(AiOpening.Balanced, flank, layoutVariant: 0));
            for (int i = 0; i < SkirmishAiTests.EndToEndBudgetTicks && observer.FirstStagingX < 0; i++) host.Step();
            Assert.That(observer.FirstStagingX, Is.GreaterThanOrEqualTo(0), "the AI never resolved a staging cell");

            Assert.That(SkirmishGoalTests.TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);
            FarthestFieldCell(host, hqX, hqY, out int enemyX, out int enemyY);
            MirrorStagingCell(hqX, hqY, enemyX, enemyY, AiProfiles.Ms1Canonical.StagingDistanceCells, flank,
                out int x, out int y);
            Assert.That((observer.FirstStagingX, observer.FirstStagingY), Is.EqualTo((x, y)));
        }

        /// <summary>
        /// The regression test for B1, on the behaviour rather than on the
        /// geometry: until the first wave is ready, nobody is out with an
        /// earlier wave — so nothing may be committed and nobody may be sent to
        /// attack. With the gathering point outside the ring the review counted
        /// 91 to 222 attack orders in that window; with flank 0 there were none.
        /// </summary>
        [Test]
        public void FlankedWave_SendsNobodyBeforeTheFirstWaveIsReady()
        {
            foreach (FactionId faction in new[] { FactionId.Legion, FactionId.Alliance })
            {
                foreach (int flank in new[] { -10, 0, 10 })
                {
                    var observer = new WaveObserver();
                    SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(
                        SkirmishAiTests.Seed, goalObserver: observer, aiFaction: faction,
                        personality: AiPersonality.Create(AiOpening.Balanced, flank, layoutVariant: 0));
                    for (int i = 0; i < SkirmishAiTests.EndToEndBudgetTicks && observer.FirstWaveTick == 0
                        && !host.Victory.IsDecided; i++)
                    {
                        host.Step();
                    }

                    string label = $"{faction} flank {flank}";
                    TestContext.Progress.WriteLine($"[personality] {label}: staging ({observer.FirstStagingX}," +
                        $"{observer.FirstStagingY}), {observer.GatheringDecisions} gathering decisions, " +
                        $"first wave at tick {observer.FirstWaveTick}, committed early {observer.CommittedEarly}, " +
                        $"attacks early {observer.AttacksEarly}");
                    Assert.That(observer.FirstWaveTick, Is.Not.Zero, label + ": no wave was ever ready");
                    Assert.That(observer.GatheringDecisions, Is.GreaterThan(0), label + ": the army never gathered");
                    Assert.That(observer.CommittedEarly, Is.Zero, label + ": units counted as committed before any wave marched");
                    Assert.That(observer.AttacksEarly, Is.Zero, label + ": attack orders before the first wave was ready");
                }
            }
        }

        /// <summary>
        /// Watches the window before the first wave: army decisions that act
        /// while the wave is not ready, and every unit goal inside them.
        /// </summary>
        private sealed class WaveObserver : IAiGoalObserver
        {
            public int FirstStagingX = -1, FirstStagingY = -1;
            public uint FirstWaveTick;
            public int GatheringDecisions, CommittedEarly, AttacksEarly;
            private bool _gathering;

            public void OnArmyGoal(byte slot, uint tick, in AiArmyGoal army)
            {
                _gathering = false;
                if (!army.Engages) return;
                if (FirstStagingX < 0 && army.StagingCellX >= 0)
                {
                    FirstStagingX = army.StagingCellX;
                    FirstStagingY = army.StagingCellY;
                }
                if (FirstWaveTick != 0) return;
                if (army.WaveReady)
                {
                    FirstWaveTick = tick;
                    return;
                }
                _gathering = true;
                GatheringDecisions++;
                CommittedEarly += army.Committed;
            }

            public void OnUnitGoal(byte slot, uint tick, in AiUnitGoal goal)
            {
                if (_gathering && goal.Goal == GoalKind.Attack) AttacksEarly++;
            }
        }

        // ----------------------------------------------------------------
        // Turtle home radius
        // ----------------------------------------------------------------

        /// <summary>
        /// The Turtle widens the home guard, but not onto the gathering point:
        /// a radius that reaches the staging cell turns a skirmish there into a
        /// call home for the whole waiting wave (review of PR #144, B2; the
        /// invariant is documented on <c>AiProfiles.Ms1Canonical</c>).
        /// </summary>
        [Test]
        public void TurtleHomeRadius_StaysShortOfTheStagingCell()
        {
            var shipped = AiProfiles.Ms1Canonical;
            var matchRunnerBase = new AiFactionProfile("Legion",
                targetPowerMargin: 0, targetArmySize: 12, attackSquadThreshold: 6, targetHarvesterCount: 2).Profile;
            foreach (AiProfile baseProfile in new[] { shipped, matchRunnerBase })
            {
                AiProfile turtle = AiPersonality.Create(AiOpening.Turtle, 0, 0).Apply(baseProfile);
                Assert.That(turtle.DefendHomeCells, Is.LessThan(turtle.StagingDistanceCells));
                Assert.That(turtle.DefendHomeCells, Is.GreaterThan(baseProfile.DefendHomeCells),
                    "the Turtle still guards a wider home than the base profile");
            }

            // Only what the personality adds is capped: a base profile that set
            // a wider radius itself — to measure it, say — keeps its own value.
            AiProfile wide = new AiProfile(
                "wide", shipped.DecisionTickInterval, shipped.PlacementSearchRadius, 0, 2,
                shipped.HarvesterQueueBatch, 12, 6, shipped.InfantryQueueBatch,
                shipped.TargetDamageWeight, shipped.TargetThreatWeight, shipped.TargetFinishWeight,
                shipped.TargetDistanceWeight, shipped.WaveSize, shipped.StagingDistanceCells,
                shipped.StagingToleranceCells, shipped.RetreatHealthPercent, shipped.RetreatDangerCells,
                shipped.WaveStrengthPoints, defendHomeCells: 13);
            Assert.That(AiPersonality.Create(AiOpening.Turtle, 0, 0).Apply(wide).DefendHomeCells, Is.EqualTo(13));
        }

        // ----------------------------------------------------------------
        // Mirrors of the AI's static map knowledge
        // ----------------------------------------------------------------

        /// <summary>Mirror of <c>SkirmishAiSystem.GetEnemyStartAreaCell</c>: the registered field farthest from the HQ, lowest id on a tie.</summary>
        private static void FarthestFieldCell(SkirmishAiTests.AiHost host, int hqX, int hqY, out int cellX, out int cellY)
        {
            cellX = ConstructionSystem.GridSize / 2;
            cellY = ConstructionSystem.GridSize / 2;
            long best = -1;
            for (ushort id = 1; id <= EconomySystem.MaxFields; id++)
            {
                if (!host.Economy.TryGetField(id, out AetheriumField field)) continue;
                long dx = field.GridPos.X - hqX;
                long dy = field.GridPos.Y - hqY;
                if (dx * dx + dy * dy <= best) continue;
                best = dx * dx + dy * dy;
                cellX = field.GridPos.X;
                cellY = field.GridPos.Y;
            }
        }

        /// <summary>Mirror of <c>SkirmishAiSystem.GetStagingCell</c>, flank branch included.</summary>
        private static void MirrorStagingCell(
            int hqX, int hqY, int enemyX, int enemyY, int distance, int flank, out int cellX, out int cellY)
        {
            int dx = enemyX - hqX;
            int dy = enemyY - hqY;
            int span = Math.Max(Math.Abs(dx), Math.Abs(dy));
            Assert.That(span, Is.GreaterThan(distance), "the enemy field lies inside the staging distance");
            cellX = Clamp(hqX + (dx * distance / span));
            cellY = Clamp(hqY + (dy * distance / span));
            if (flank == 0) return;
            int offsetX = cellX - hqX + (-dy * flank / span);
            int offsetY = cellY - hqY + (dx * flank / span);
            int reach = Math.Max(Math.Abs(offsetX), Math.Abs(offsetY));
            if (reach > distance)
            {
                offsetX = offsetX * distance / reach;
                offsetY = offsetY * distance / reach;
            }
            cellX = Clamp(hqX + offsetX);
            cellY = Clamp(hqY + offsetY);
        }

        private static int Clamp(int c) => Math.Max(0, Math.Min(ConstructionSystem.GridSize - 1, c));
    }
}
