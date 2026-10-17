using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nova.AI;
using Nova.AI.Data;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
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
                        Assert.That(10 + p.DefendHomeCellsDelta, Is.EqualTo(14));
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

        [TestCase((byte)1, 118, 117, 7)]
        [TestCase((byte)0, 4, 7, 117)]
        public void FlankedStagingCells_AreInsideTheGridAndWalkable(byte slot, int hqOrigin, int ownField, int enemyField)
        {
            _ = ownField;
            const int distance = 12; // AiProfiles.Ms1Canonical.StagingDistanceCells
            Assert.That(AiProfiles.Ms1Canonical.StagingDistanceCells, Is.EqualTo(distance));
            foreach (int flank in new[] { -10, 10 })
            {
                // Mirror of SkirmishAiSystem.GetStagingCell (flank branch); the enemy start area is the far field.
                int dx = enemyField - hqOrigin;
                int dy = enemyField - hqOrigin;
                int span = Math.Max(Math.Abs(dx), Math.Abs(dy));
                Assert.That(span, Is.GreaterThan(distance));
                int cx = Clamp(hqOrigin + dx * distance / span);
                int cy = Clamp(hqOrigin + dy * distance / span);
                int fx = Clamp(cx + (-dy * flank / span));
                int fy = Clamp(cy + (dx * flank / span));
                Assert.That(fx, Is.InRange(0, ConstructionSystem.GridSize - 1), $"slot {slot} flank {flank}");
                Assert.That(fy, Is.InRange(0, ConstructionSystem.GridSize - 1), $"slot {slot} flank {flank}");
                Assert.That(CanonicalTerrainMirror.IsImpassable(fx, fy), Is.False,
                    $"slot {slot} flank {flank}: cell ({fx},{fy}) is rock");
                Assert.That(fx != cx || fy != cy, Is.True, "the flank must actually move the cell");
            }
        }

        private static int Clamp(int c) => Math.Max(0, Math.Min(ConstructionSystem.GridSize - 1, c));
    }
}
