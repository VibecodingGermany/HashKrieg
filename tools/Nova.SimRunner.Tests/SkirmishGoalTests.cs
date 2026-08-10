using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nova.AI.Data;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// The named goals of the skirmish AI, and the two seams the lab's admin
    /// panel hangs on them: an observer that watches the decision and a mask
    /// that forces one.
    /// <para>
    /// WHAT THIS SUITE IS ACTUALLY GUARDING. Naming the branches of the army
    /// step was supposed to change nothing at all — the proof of that is the
    /// pinned end state in <see cref="CanonicalAiOutcomeTests"/> and a lab run
    /// whose artifacts stayed byte-identical, not an assertion here. What a test
    /// CAN hold, and what the pin cannot, is the property that makes the names
    /// worth having: the goal that is reported is the goal that produced the
    /// orders. A panel drawing a goal the unit is not under would be worse than
    /// a panel drawing nothing, because it looks like an answer.
    /// </para>
    /// <para>
    /// The observer and the mask are both null in the shipped game
    /// (<c>MatchRunner</c> passes neither), so two of the tests below simply
    /// check that handing one in does not move the match. That is the whole
    /// licence for compiling them into the delivered build.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class SkirmishGoalTests
    {
        private const byte AiSlot = 1;

        // ----------------------------------------------------------------
        // Test doubles
        // ----------------------------------------------------------------

        /// <summary>One reported unit decision, with the tick it was taken at.</summary>
        private readonly struct UnitEntry
        {
            public readonly uint Tick;
            public readonly AiUnitGoal Goal;

            public UnitEntry(uint tick, in AiUnitGoal goal)
            {
                Tick = tick;
                Goal = goal;
            }
        }

        /// <summary>One reported army decision, with the tick it was taken at.</summary>
        private readonly struct ArmyEntry
        {
            public readonly uint Tick;
            public readonly AiArmyGoal Goal;

            public ArmyEntry(uint tick, in AiArmyGoal goal)
            {
                Tick = tick;
                Goal = goal;
            }
        }

        /// <summary>
        /// Keeps every decision it is told about. A pure sink: it reads nothing
        /// back into the match, which is the property the byte-identity test
        /// below actually verifies.
        /// </summary>
        private sealed class RecordingObserver : IAiGoalObserver
        {
            public readonly List<ArmyEntry> Army = new List<ArmyEntry>();
            public readonly List<UnitEntry> Units = new List<UnitEntry>();

            public void OnArmyGoal(byte slot, uint tick, in AiArmyGoal army)
            {
                Army.Add(new ArmyEntry(tick, in army));
            }

            public void OnUnitGoal(byte slot, uint tick, in AiUnitGoal goal)
            {
                Units.Add(new UnitEntry(tick, in goal));
            }

            /// <summary>The army decision of the tick a unit decision belongs to.</summary>
            public AiArmyGoal ArmyAt(uint tick)
            {
                for (int i = 0; i < Army.Count; i++)
                {
                    if (Army[i].Tick == tick) return Army[i].Goal;
                }
                throw new InvalidOperationException(
                    $"no army decision was reported for tick {tick}, but a unit decision was");
            }
        }

        /// <summary>The same goal for every unit — or none at all, which is the off setting.</summary>
        private sealed class FixedMask : IAiGoalOverride
        {
            private readonly GoalKind _goal;

            public FixedMask(GoalKind goal)
            {
                _goal = goal;
            }

            public GoalKind ResolveGoal(uint entityRaw) => _goal;
        }

        // ----------------------------------------------------------------
        // The names describe the decision
        // ----------------------------------------------------------------

        /// <summary>
        /// Every judged unit is under exactly one of the four goals, and the
        /// three the canonical match has to contain all show up.
        /// <para>
        /// <c>Retreat</c> is NOT among them and cannot be: the opponent of this
        /// match is passive and owns no armed unit, so no threat is ever visible
        /// and nothing ever turns back. It has its own test below with the
        /// wound written in — the same reason
        /// <c>SkirmishAi_PullsWoundedUnitsBackTowardTheirOwnBase</c> exists
        /// beside the pinned end-to-end run.
        /// </para>
        /// </summary>
        [Test]
        public void EveryJudgedUnitIsUnderExactlyOneNamedGoal()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed, goalObserver: observer);
            host.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            Assert.That(observer.Units, Is.Not.Empty, "no unit decision was reported at all");

            bool attacked = false, held = false, advanced = false;
            for (int i = 0; i < observer.Units.Count; i++)
            {
                GoalKind goal = observer.Units[i].Goal.Goal;
                Assert.That(goal, Is.Not.EqualTo(GoalKind.None),
                    "a unit was judged and came out unnamed — the catalogue does not cover the decision");
                if (goal == GoalKind.Attack) attacked = true;
                else if (goal == GoalKind.Hold) held = true;
                else if (goal == GoalKind.Advance) advanced = true;
            }

            Assert.Multiple(() =>
            {
                Assert.That(attacked, Is.True, "no unit ever marched on the target");
                Assert.That(held, Is.True, "no reinforcement ever waited at the staging cell");
                Assert.That(advanced, Is.True, "no reinforcement ever walked to the staging cell");
            });
        }

        /// <summary>
        /// THE ORDERS ARE THE ONES THE REPORTED GOAL PRODUCES — the single
        /// property that makes a goal panel trustworthy.
        /// <para>
        /// It is checked against the ARMY decision of the same tick rather than
        /// against a copy of the effect table, so a change that moved an effect
        /// without moving the name would fail here. That is the failure mode
        /// worth catching: names and effects drifting apart is invisible in
        /// every other artifact, because the match plays on regardless.
        /// </para>
        /// </summary>
        [Test]
        public void TheOrdersThatWentOutAreTheOnesTheReportedGoalProduces()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed, goalObserver: observer);
            host.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            for (int i = 0; i < observer.Units.Count; i++)
            {
                UnitEntry entry = observer.Units[i];
                AiUnitGoal unit = entry.Goal;
                AiArmyGoal army = observer.ArmyAt(entry.Tick);
                string where = $"unit {unit.EntityRaw} at tick {entry.Tick} under {unit.Goal}";

                switch (unit.Goal)
                {
                    case GoalKind.Attack:
                        Assert.That(unit.MoveCellX, Is.EqualTo(army.MoveCellX), where);
                        Assert.That(unit.MoveCellY, Is.EqualTo(army.MoveCellY), where);
                        Assert.That(unit.AttackTargetRaw, Is.EqualTo(army.TargetRaw), where);
                        break;

                    case GoalKind.Hold:
                        Assert.That(unit.MoveCellX, Is.EqualTo(-1), where + " — Hold must say nothing about movement");
                        Assert.That(unit.MoveCellY, Is.EqualTo(-1), where);
                        break;

                    case GoalKind.Advance:
                        Assert.That(unit.MoveCellX, Is.EqualTo(army.StagingCellX), where);
                        Assert.That(unit.MoveCellY, Is.EqualTo(army.StagingCellY), where);
                        Assert.That(unit.AttackTargetRaw, Is.EqualTo(0u),
                            where + " — a reinforcement on its way must not carry an explicit target (F001)");
                        break;

                    case GoalKind.Retreat:
                        Assert.That(unit.MoveCellX, Is.EqualTo(army.StagingCellX), where);
                        Assert.That(unit.MoveCellY, Is.EqualTo(army.StagingCellY), where);
                        break;

                    default:
                        Assert.Fail(where + " — unnamed goal");
                        break;
                }
            }
        }

        /// <summary>
        /// A unit under the retreat rule is reported as <c>Retreat</c>, and the
        /// numbers reported beside it are the ones the rule compared.
        /// <para>
        /// The setup is the one from
        /// <c>SkirmishAi_PullsWoundedUnitsBackTowardTheirOwnBase</c>: the wave
        /// has to be out, an ARMED enemy has to stand inside the danger radius,
        /// and the wound is written into the state rather than shot in — this
        /// asks what the AI decides about a wounded unit, not whether a rifle
        /// can hit one.
        /// </para>
        /// </summary>
        [Test]
        public void AWoundedUnitUnderFireIsReportedAsRetreat_WithTheNumbersTheRuleWeighed()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed, goalObserver: observer);
            int ring = shipped.StagingDistanceCells + shipped.StagingToleranceCells;

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);

            int budget = SkirmishAiTests.EndToEndBudgetTicks;
            while (budget-- > 0 && FarthestCombatDistance(host, AiSlot, hqX, hqY) <= ring) host.Step();
            Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.GreaterThan(ring),
                "the army never marched, so nothing could turn back");

            Assert.That(TryFirstCombatUnit(host, AiSlot, out EntityId woundedId, out int armyX, out int armyY),
                Is.True);
            Assert.That(host.Entities.TryGetUnit(woundedId, out UnitState marching), Is.True);
            Assert.That(marching.TargetGridPos.IsValid, Is.True, "the subject has to be marching somewhere");

            int aheadX = armyX + Math.Sign(marching.TargetGridPos.X - armyX) * shipped.RetreatDangerCells;
            int aheadY = armyY + Math.Sign(marching.TargetGridPos.Y - armyY) * shipped.RetreatDangerCells;
            SpawnEnemyInfantry(host, aheadX, aheadY);

            ref UnitState target = ref host.Entities.GetUnitRef(woundedId);
            target.CurrentHealth = target.MaxHealth * (shipped.RetreatHealthPercent - 20) / 100;

            uint woundedRaw = UnitCommandStateView.ToRawEntityId(woundedId);
            int before = observer.Units.Count;
            RunToNextDecision(host);

            bool seen = false;
            for (int i = before; i < observer.Units.Count; i++)
            {
                AiUnitGoal goal = observer.Units[i].Goal;
                if (goal.EntityRaw != woundedRaw) continue;
                seen = true;

                Assert.That(goal.Goal, Is.EqualTo(GoalKind.Retreat),
                    "a wounded unit with an armed enemy beside it was not reported as retreating");
                Assert.That(goal.HealthPercent, Is.LessThan(shipped.RetreatHealthPercent),
                    "the reported health is not the one that put the unit under the rule");
                Assert.That(goal.ThreatDistanceCells, Is.InRange(0, shipped.RetreatDangerCells),
                    "the reported threat distance does not explain why the rule fired");
                break;
            }
            Assert.That(seen, Is.True, "the wounded unit was never judged after the wound");
        }

        // ----------------------------------------------------------------
        // The two seams cost the shipped game nothing
        // ----------------------------------------------------------------

        /// <summary>
        /// WATCHING IS NOT PLAYING. A run with an observer attached reaches the
        /// same end state on the same tick as one without — otherwise the panel
        /// would be describing a match that only exists while somebody looks at
        /// it.
        /// </summary>
        [Test]
        public void AnObserverDoesNotMoveTheMatch()
        {
            SkirmishAiTests.AiHost plain = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed);
            uint plainDecided = plain.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            SkirmishAiTests.AiHost watched = SkirmishAiTests.BuildMatch(
                SkirmishAiTests.Seed, goalObserver: new RecordingObserver());
            uint watchedDecided = watched.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            Assert.Multiple(() =>
            {
                Assert.That(watchedDecided, Is.EqualTo(plainDecided));
                Assert.That(watched.Kernel.CalculateStateHash(), Is.EqualTo(plain.Kernel.CalculateStateHash()),
                    "attaching an observer changed the match");
            });
        }

        /// <summary>
        /// An EMPTY mask is the off setting, and it has to be bit-exactly off:
        /// <see cref="GoalKind.None"/> for every unit means "the AI decides",
        /// which is what the shipped game does by passing no mask at all.
        /// </summary>
        [Test]
        public void AnEmptyGoalMaskDoesNotMoveTheMatch()
        {
            SkirmishAiTests.AiHost plain = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed);
            uint plainDecided = plain.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            SkirmishAiTests.AiHost masked = SkirmishAiTests.BuildMatch(
                SkirmishAiTests.Seed, goalOverride: new FixedMask(GoalKind.None));
            uint maskedDecided = masked.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            Assert.Multiple(() =>
            {
                Assert.That(maskedDecided, Is.EqualTo(plainDecided));
                Assert.That(masked.Kernel.CalculateStateHash(), Is.EqualTo(plain.Kernel.CalculateStateHash()),
                    "a mask that names nothing still changed the match");
            });
        }

        // ----------------------------------------------------------------
        // …and a mask that names something is visible in the match
        // ----------------------------------------------------------------

        /// <summary>
        /// A mask that holds every unit keeps the whole army inside the staging
        /// ring for the entire match.
        /// <para>
        /// Asserted on POSITIONS, not on the absence of intents: what the
        /// override is for is being able to see the consequence of a goal, and
        /// the consequence of <c>Hold</c> is that nobody goes anywhere.
        /// </para>
        /// <para>
        /// THE CONTROL RUNS IN THE SAME TEST, on the same seed and the same
        /// budget, because "the army stayed home" is the kind of assertion that
        /// passes just as happily when nothing was built, when the match ended
        /// early, or when the ring was measured against the wrong cell. Without
        /// the unmasked half beside it, this test proves that a number is small.
        /// </para>
        /// </summary>
        [Test]
        public void AMaskThatHoldsEveryUnitKeepsTheArmyInTheRing()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            int ring = shipped.StagingDistanceCells + shipped.StagingToleranceCells;

            SkirmishAiTests.AiHost loose = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed);
            loose.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);
            Assert.That(TryHqCell(loose, AiSlot, out int looseX, out int looseY), Is.True);
            Assert.That(FarthestCombatDistance(loose, AiSlot, looseX, looseY), Is.GreaterThan(ring),
                "the control run never left the ring either, so holding proves nothing");

            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(
                SkirmishAiTests.Seed, goalOverride: new FixedMask(GoalKind.Hold));
            host.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);
            Assert.That(CountCombatUnits(host, AiSlot), Is.GreaterThan(0),
                "nothing was produced, so 'nobody left' says nothing");
            Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.LessThanOrEqualTo(ring),
                "a unit left the staging ring although every unit was held");
        }

        /// <summary>
        /// A forced goal produces the ORDERS OF THAT GOAL and is reported as
        /// forced. The mask replaces the pick, never the effect — so a panel
        /// cannot conjure a behaviour the AI has no code for.
        /// </summary>
        [Test]
        public void AForcedGoalProducesTheOrdersOfThatGoal_AndSaysItWasForced()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(
                SkirmishAiTests.Seed,
                goalObserver: observer,
                goalOverride: new FixedMask(GoalKind.Advance));
            host.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            Assert.That(observer.Units, Is.Not.Empty, "no unit decision was reported at all");
            for (int i = 0; i < observer.Units.Count; i++)
            {
                UnitEntry entry = observer.Units[i];
                AiUnitGoal unit = entry.Goal;
                AiArmyGoal army = observer.ArmyAt(entry.Tick);

                Assert.That(unit.Goal, Is.EqualTo(GoalKind.Advance),
                    $"unit {unit.EntityRaw} at tick {entry.Tick} kept its own goal against the mask");
                Assert.That(unit.Forced, Is.True, "the report does not admit that the goal was forced");
                Assert.That(unit.MoveCellX, Is.EqualTo(army.StagingCellX));
                Assert.That(unit.MoveCellY, Is.EqualTo(army.StagingCellY));
            }
        }

        // ----------------------------------------------------------------
        // The army report
        // ----------------------------------------------------------------

        /// <summary>
        /// The wave verdict and the numbers reported beside it are the SAME
        /// arithmetic: what the ring holds against the threshold it is measured
        /// with. That is what lets a panel say "another 140 points" instead of
        /// repeating the gate's rules in a second language and getting them
        /// subtly wrong — which is exactly what the recorded player did before
        /// these numbers existed, and it had to label every one of them derived.
        /// </summary>
        [Test]
        public void TheArmyReportExplainsItsOwnWaveVerdict()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed, goalObserver: observer);
            host.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);

            bool weighed = false;
            for (int i = 0; i < observer.Army.Count; i++)
            {
                AiArmyGoal army = observer.Army[i].Goal;
                if (!army.Engages) continue;

                switch (army.WaveMode)
                {
                    case WaveGateMode.Strength:
                        weighed = true;
                        Assert.That(army.WaveReady, Is.EqualTo(army.GatheredStrength >= army.WaveThreshold),
                            $"tick {observer.Army[i].Tick}: the verdict does not follow from the reported numbers");
                        break;

                    case WaveGateMode.Count:
                        Assert.That(army.WaveReady, Is.EqualTo(army.Gathered >= army.WaveThreshold),
                            $"tick {observer.Army[i].Tick}: the verdict does not follow from the reported numbers");
                        break;

                    default:
                        Assert.That(army.WaveReady, Is.True, "waves are off, so every unit is its own wave");
                        break;
                }
            }

            Assert.That(weighed, Is.True,
                "the shipped profile measures the wave in strength, and no such decision was reported");
        }

        // ================================================================
        // DefendHome (r8) — breaking off the gathering when the base burns
        //
        // The five checks VERTEIDIGUNG.md asks for, and the reason each of
        // them exists is a way the rule could pass while being wrong. Test 2
        // in particular: without it, "breaks off" and "attacks early" are
        // both green, and only one of them is the rule.
        // ================================================================

        /// <summary>
        /// AN ARMED ENEMY AT THE BASE PUTS THE GATHERERS UNDER
        /// <see cref="GoalKind.DefendHome"/>, and their orders point home.
        /// <para>
        /// Asserted over the ORDER THAT WENT OUT and not only over the reported
        /// goal: a name in the recording that no unit acts on would be the
        /// panel lying in a new place.
        /// </para>
        /// </summary>
        [Test]
        public void AnArmedEnemyAtTheBaseTurnsTheGatherersIntoDefenders()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = GatheringHost(AiProfiles.Ms1Canonical, observer, out int hqX, out int hqY);

            SpawnEnemyInfantry(host, hqX + 2, hqY);
            int before = observer.Units.Count;
            RunToNextDecision(host);

            int defenders = 0;
            for (int i = before; i < observer.Units.Count; i++)
            {
                AiUnitGoal goal = observer.Units[i].Goal;
                if (goal.Goal != GoalKind.DefendHome) continue;
                defenders++;
                Assert.That(goal.MoveCellX, Is.EqualTo(hqX), "a defender was not sent to the headquarters");
                Assert.That(goal.MoveCellY, Is.EqualTo(hqY), "a defender was not sent to the headquarters");
                Assert.That(goal.AttackTargetRaw, Is.Not.Zero,
                    "a defender walks home carrying no target — finding F001, it would fire at nothing");
            }
            Assert.That(defenders, Is.GreaterThan(0),
                "the headquarters is under attack and not one waiting unit broke off");

            // And the standing order really is the one the goal names.
            Assert.That(AnyCombatUnitOrderedTo(host, AiSlot, hqX, hqY), Is.True,
                "no unit actually carries the march order the recording claims");
        }

        /// <summary>
        /// THE WAVE IS INTERRUPTED, NOT RELEASED. No gatherer is sent toward
        /// the army's target.
        /// <para>
        /// WITHOUT THIS TEST BOTH BEHAVIOURS ARE GREEN. "Everyone marches out
        /// early" also empties the staging ring and also ends with units
        /// fighting, and it is the opposite of the rule: it takes the defenders
        /// AWAY from the base that is being shot.
        /// </para>
        /// </summary>
        [Test]
        public void TheDefenceDoesNotSendTheWaveOffEarly()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = GatheringHost(AiProfiles.Ms1Canonical, observer, out int hqX, out int hqY);

            SpawnEnemyInfantry(host, hqX + 2, hqY);
            int before = observer.Units.Count;
            RunToNextDecision(host);

            for (int i = before; i < observer.Units.Count; i++)
            {
                AiUnitGoal goal = observer.Units[i].Goal;
                if (goal.Goal != GoalKind.DefendHome) continue;

                // Every step toward the enemy start area is a step away from
                // the fight at home, so the only acceptable destination is the
                // headquarters itself.
                Assert.That(Math.Max(Math.Abs(goal.MoveCellX - hqX), Math.Abs(goal.MoveCellY - hqY)),
                    Is.Zero,
                    $"unit {goal.EntityRaw} is under DefendHome and walking to " +
                    $"{goal.MoveCellX},{goal.MoveCellY} instead of home at {hqX},{hqY}");
            }
        }

        /// <summary>
        /// COMMITTED STAYS COMMITTED. A wave that is already out is not called
        /// back — the r3 rule that made a wave a wave, and the V002 failure
        /// mode if it fell through the back door.
        /// </summary>
        [Test]
        public void AWaveThatIsAlreadyOutIsNotCalledBack()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            int ring = shipped.StagingDistanceCells + shipped.StagingToleranceCells;

            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed, goalObserver: observer);
            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);

            int budget = SkirmishAiTests.EndToEndBudgetTicks;
            while (budget-- > 0 && FarthestCombatDistance(host, AiSlot, hqX, hqY) <= ring) host.Step();
            Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.GreaterThan(ring),
                "the army never marched, so nothing could be called back");

            SpawnEnemyInfantry(host, hqX + 2, hqY);
            int before = observer.Units.Count;
            RunToNextDecision(host);

            bool judgedSomebodyOutside = false;
            for (int i = before; i < observer.Units.Count; i++)
            {
                AiUnitGoal goal = observer.Units[i].Goal;
                if (goal.HomeDistanceCells <= ring) continue;
                judgedSomebodyOutside = true;
                Assert.That(goal.Goal, Is.Not.EqualTo(GoalKind.DefendHome),
                    $"unit {goal.EntityRaw} is {goal.HomeDistanceCells} cells out, past the ring at " +
                    $"{ring}, and the defence called it back");
            }
            Assert.That(judgedSomebodyOutside, Is.True, "no unit was outside the ring when the enemy arrived");
        }

        /// <summary>
        /// THE OFF SETTING IS OFF. With <c>defendHomeCells: 0</c> the same scene
        /// produces no defender at all.
        /// <para>
        /// That the off path is bit-identical to r7 is a claim about two
        /// BUILDS and cannot be asserted from inside one — it is measured in
        /// the lab (`compare` against `defend-off`, and the hash chain of the
        /// canonical match). What belongs here is the half that is checkable:
        /// zero means the rule cannot fire, which is what makes the one-sided
        /// measurement mean anything at all (finding M001).
        /// </para>
        /// </summary>
        [Test]
        public void WithTheRuleOffNobodyDefends()
        {
            var observer = new RecordingObserver();
            SkirmishAiTests.AiHost host = GatheringHost(DefenceOff(), observer, out int hqX, out int hqY);

            SpawnEnemyInfantry(host, hqX + 2, hqY);
            int before = observer.Units.Count;
            RunToNextDecision(host);

            for (int i = before; i < observer.Units.Count; i++)
            {
                Assert.That(observer.Units[i].Goal.Goal, Is.Not.EqualTo(GoalKind.DefendHome),
                    "defendHomeCells is 0 and a unit was still put under DefendHome");
            }
            for (int i = before; i < observer.Army.Count; i++)
            {
                Assert.That(observer.Army[i].Goal.HomeThreatened, Is.False,
                    "defendHomeCells is 0 and the posture still reports the base as threatened");
            }
        }

        /// <summary>
        /// NO COMMAND STREAM — the test V002 did not have.
        /// <para>
        /// An unchanged situation over several cadences must not produce a
        /// second order. That is the whole reason the destination is the
        /// headquarters, a cell that does not move: <c>DefendBase</c> aimed at
        /// the enemy, handed every unit a fresh destination every cadence, and
        /// died of 23 % more intents. The enemy here is deliberately left
        /// standing so the trigger holds while the defenders arrive.
        /// </para>
        /// </summary>
        [Test]
        public void AHeldDefenceDoesNotProduceAnOrderEveryCadence()
        {
            SkirmishAiTests.AiHost host = GatheringHost(AiProfiles.Ms1Canonical, null, out int hqX, out int hqY);

            SpawnEnemyInfantry(host, hqX + 2, hqY);
            RunToNextDecision(host);              // the decision that turns them around

            int cadence = host.Ai.DecisionTickInterval;
            int afterTurn = host.IntentsSubmitted;
            for (int i = 0; i < 5; i++) RunToNextDecision(host);

            int perCadence = (host.IntentsSubmitted - afterTurn) / 5;
            Assert.That(perCadence, Is.LessThanOrEqualTo(2),
                $"five cadences of an unchanged defence cost {host.IntentsSubmitted - afterTurn} intents " +
                $"({perCadence} per cadence of {cadence} ticks) — a static destination must be suppressed " +
                "after the first order, and this is the shape DefendBase died of (journal V002)");
        }

        // ----------------------------------------------------------------
        // Deterministic read helpers (ascending entity index)
        // ----------------------------------------------------------------

        /// <summary>The shipped profile with the defence switched off, and nothing else changed.</summary>
        private static AiProfile DefenceOff()
        {
            AiProfile s = AiProfiles.Ms1Canonical;
            return new AiProfile(
                profileId: "defence-off-probe",
                decisionTickInterval: s.DecisionTickInterval,
                placementSearchRadius: s.PlacementSearchRadius,
                powerReserve: s.PowerReserve,
                targetHarvesters: s.TargetHarvesters,
                harvesterQueueBatch: s.HarvesterQueueBatch,
                targetArmySize: s.TargetArmySize,
                attackSquadThreshold: s.AttackSquadThreshold,
                infantryQueueBatch: s.InfantryQueueBatch,
                targetDamageWeight: s.TargetDamageWeight,
                targetThreatWeight: s.TargetThreatWeight,
                targetFinishWeight: s.TargetFinishWeight,
                targetDistanceWeight: s.TargetDistanceWeight,
                waveSize: s.WaveSize,
                stagingDistanceCells: s.StagingDistanceCells,
                stagingToleranceCells: s.StagingToleranceCells,
                retreatHealthPercent: s.RetreatHealthPercent,
                retreatDangerCells: s.RetreatDangerCells,
                waveStrengthPoints: s.WaveStrengthPoints,
                defendHomeCells: 0);
        }

        /// <summary>
        /// A host run forward to the point where the army is GATHERING: it acts
        /// (so the army step judges anybody at all) and its units are still
        /// inside the staging ring (so there is something to break off).
        /// </summary>
        private static SkirmishAiTests.AiHost GatheringHost(
            AiProfile profile, RecordingObserver observer, out int hqX, out int hqY)
        {
            int ring = profile.StagingDistanceCells + profile.StagingToleranceCells;
            SkirmishAiTests.AiHost host =
                SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed, profile, goalObserver: observer);

            Assert.That(TryHqCell(host, AiSlot, out hqX, out hqY), Is.True);

            int budget = SkirmishAiTests.EndToEndBudgetTicks;
            while (budget-- > 0
                   && (CountCombatUnits(host, AiSlot) < profile.AttackSquadThreshold
                       || FarthestCombatDistance(host, AiSlot, hqX, hqY) > ring))
            {
                host.Step();
            }

            Assert.That(CountCombatUnits(host, AiSlot), Is.GreaterThanOrEqualTo(profile.AttackSquadThreshold),
                "the army never reached its squad threshold, so no goal is handed out at all");
            Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.LessThanOrEqualTo(ring),
                "no unit is gathering inside the ring, so there is nothing to break off");
            return host;
        }

        /// <summary>Whether any combat unit of the seat carries a march order to this cell.</summary>
        private static bool AnyCombatUnitOrderedTo(
            SkirmishAiTests.AiHost host, byte slot, int cellX, int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || !IsCombat(u.Role)) continue;
                if (!u.TargetGridPos.IsValid) continue;
                if (u.TargetGridPos.X == cellX && u.TargetGridPos.Y == cellY) return true;
            }
            return false;
        }

        private static bool TryHqCell(SkirmishAiTests.AiHost host, byte slot, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || u.Role != UnitRole.HQ) continue;
                if (host.Construction.IsActiveSite(u.Id)) continue;
                cellX = SimFixed.WorldToGrid(u.Transform.PositionX);
                cellY = SimFixed.WorldToGrid(u.Transform.PositionY);
                return true;
            }
            cellX = -1;
            cellY = -1;
            return false;
        }

        private static int FarthestCombatDistance(SkirmishAiTests.AiHost host, byte slot, int cellX, int cellY)
        {
            int farthest = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || !IsCombat(u.Role)) continue;
                int distance = Math.Max(
                    Math.Abs(SimFixed.WorldToGrid(u.Transform.PositionX) - cellX),
                    Math.Abs(SimFixed.WorldToGrid(u.Transform.PositionY) - cellY));
                if (distance > farthest) farthest = distance;
            }
            return farthest;
        }

        private static int CountCombatUnits(SkirmishAiTests.AiHost host, byte slot)
        {
            int count = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.PlayerId == slot && IsCombat(u.Role)) count++;
            }
            return count;
        }

        private static bool TryFirstCombatUnit(
            SkirmishAiTests.AiHost host, byte slot, out EntityId id, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || !IsCombat(u.Role)) continue;
                id = u.Id;
                cellX = SimFixed.WorldToGrid(u.Transform.PositionX);
                cellY = SimFixed.WorldToGrid(u.Transform.PositionY);
                return true;
            }
            id = EntityId.Invalid;
            cellX = -1;
            cellY = -1;
            return false;
        }

        private static void SpawnEnemyInfantry(SkirmishAiTests.AiHost host, int cellX, int cellY)
        {
            const byte enemySlot = 0;
            FactionId faction = host.Economy.GetSlotFaction(enemySlot);
            Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.BasicInfantry, out SimUnitDefinition def), Is.True);
            host.Entities.SpawnUnit(
                enemySlot,
                new Transform2D(SimFixed.FromInt(cellX), SimFixed.FromInt(cellY)),
                def.MoveSpeed,
                maxHealth: def.MaxHealth,
                role: UnitRole.BasicInfantry);
        }

        /// <summary>
        /// To the next decision cadence and two ticks further, so the sealed
        /// intent has landed. Not further: the subject keeps walking, and a long
        /// window lets it leave the danger radius on its own.
        /// </summary>
        private static void RunToNextDecision(SkirmishAiTests.AiHost host)
        {
            ushort cadence = host.Ai.DecisionTickInterval;
            do
            {
                host.Step();
            }
            while (host.Kernel.CurrentTick.Value % cadence != 0);
            host.Step();
            host.Step();
        }

        private static bool IsCombat(UnitRole role) =>
            role >= UnitRole.BasicInfantry && role <= UnitRole.Artillery;
    }
}
