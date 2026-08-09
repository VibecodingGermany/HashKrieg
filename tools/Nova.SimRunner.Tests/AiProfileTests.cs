using System.Reflection;
using NUnit.Framework;
using Nova.AI;
using Nova.AI.Data;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// The AI data layer (Nova.AI.Data): profile values moved out of behaviour
    /// code, and the guarantee that the move changed nothing.
    /// <para>
    /// The load-bearing test is <see cref="ShippedProfileCarriesTodaysValuesExactly"/>.
    /// The whole migration rests on one claim — the shipped numbers are
    /// numerically identical to the constants they replaced — and the four
    /// baseline files are the other half of that proof: they stay green only
    /// while it holds.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class AiProfileTests
    {
        // ----------------------------------------------------------------
        // (a) BEHAVIOUR NEUTRALITY
        // ----------------------------------------------------------------

        [Test]
        public void ShippedProfileCarriesTodaysValuesExactly()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;

            // Four numbers MatchRunner passed explicitly...
            Assert.That(shipped.PowerReserve, Is.EqualTo(0),
                "0 = place a Power plant when the margin would go negative (the D-077 opening rule)");
            Assert.That(shipped.TargetArmySize, Is.EqualTo(12));
            Assert.That(shipped.AttackSquadThreshold, Is.EqualTo(6));
            Assert.That(shipped.TargetHarvesters, Is.EqualTo(2));

            // ...and four that were const fields inside SkirmishAiSystem.
            Assert.That(shipped.DecisionTickInterval, Is.EqualTo((ushort)20),
                "20 ticks = 2.0 s on the canonical 10 Hz clock");
            Assert.That(shipped.PlacementSearchRadius, Is.EqualTo(8));
            Assert.That(shipped.InfantryQueueBatch, Is.EqualTo(2));
            Assert.That(shipped.HarvesterQueueBatch, Is.EqualTo(2));

            // The wave values, which are NOT a copy of an older constant:
            // waves did not exist before behaviour revision 3. The size sits
            // AT the army cap on purpose — that is the rule ("attack at full
            // strength, never reinforce piecemeal") expressed as one number,
            // and the pairing is asserted here so a later change to the cap
            // cannot quietly turn the wave rule into something else.
            Assert.That(shipped.WaveSize, Is.EqualTo(12));
            Assert.That(shipped.WaveSize, Is.EqualTo(shipped.TargetArmySize),
                "the shipped wave is the whole army; 1 would be the off setting");
            Assert.That(shipped.StagingDistanceCells, Is.EqualTo(12));
            Assert.That(shipped.StagingToleranceCells, Is.EqualTo(4));
        }

        [Test]
        public void TheHistoricalConstructorReproducesWhatMatchRunnerShips()
        {
            // MatchRunner's exact call. Its signature had to survive the
            // migration: that file belongs to the network strand.
            var profile = new AiFactionProfile("Legion",
                targetPowerMargin: 0, targetArmySize: 12, attackSquadThreshold: 6, targetHarvesterCount: 2);

            Assert.That(profile.TargetPowerMargin, Is.EqualTo(0));
            Assert.That(profile.TargetArmySize, Is.EqualTo(12));
            Assert.That(profile.AttackSquadThreshold, Is.EqualTo(6));
            Assert.That(profile.TargetHarvesterCount, Is.EqualTo(2));

            // And the cadence it cannot express comes from the shipped profile.
            Assert.That(profile.Profile.DecisionTickInterval,
                Is.EqualTo(AiProfiles.Ms1Canonical.DecisionTickInterval));
            Assert.That(profile.Profile.PlacementSearchRadius,
                Is.EqualTo(AiProfiles.Ms1Canonical.PlacementSearchRadius));
            Assert.That(profile.Profile.InfantryQueueBatch,
                Is.EqualTo(AiProfiles.Ms1Canonical.InfantryQueueBatch));
            Assert.That(profile.Profile.HarvesterQueueBatch,
                Is.EqualTo(AiProfiles.Ms1Canonical.HarvesterQueueBatch));
        }

        [Test]
        public void LegacyDefaultsKeepTheOldConstructorDefaults()
        {
            var defaulted = new AiFactionProfile("Alliance");

            Assert.That(defaulted.TargetPowerMargin, Is.EqualTo(AiProfiles.LegacyDefaults.PowerReserve));
            Assert.That(defaulted.TargetArmySize, Is.EqualTo(AiProfiles.LegacyDefaults.TargetArmySize));
            Assert.That(defaulted.AttackSquadThreshold, Is.EqualTo(AiProfiles.LegacyDefaults.AttackSquadThreshold));
            Assert.That(defaulted.TargetHarvesterCount, Is.EqualTo(AiProfiles.LegacyDefaults.TargetHarvesters));
        }

        // ----------------------------------------------------------------
        // (b) THE IDENTITY CORRECTION
        // ----------------------------------------------------------------

        [Test]
        public void TwoProfilesWithTheSameNameButDifferentNumbersAreNotEqual()
        {
            // The correction the migration had to make explicitly: the old
            // AiFactionProfile compared its faction name alone. Harmless while
            // one profile ships, wrong the moment tuning starts — a tuning run
            // IS two profiles that differ only in numbers, and the old
            // comparison reported them as the same profile.
            var aggressive = new AiFactionProfile("Legion",
                targetPowerMargin: 0, targetArmySize: 20, attackSquadThreshold: 10, targetHarvesterCount: 4);
            var cautious = new AiFactionProfile("Legion",
                targetPowerMargin: 30, targetArmySize: 8, attackSquadThreshold: 4, targetHarvesterCount: 2);

            Assert.That(aggressive, Is.Not.EqualTo(cautious));
            Assert.That(aggressive == cautious, Is.False);
            Assert.That(aggressive.GetHashCode(), Is.Not.EqualTo(cautious.GetHashCode()));
        }

        [Test]
        public void IdenticalProfilesStayEqual()
        {
            var first = new AiFactionProfile("Legion",
                targetPowerMargin: 0, targetArmySize: 12, attackSquadThreshold: 6, targetHarvesterCount: 2);
            var second = new AiFactionProfile("Legion",
                targetPowerMargin: 0, targetArmySize: 12, attackSquadThreshold: 6, targetHarvesterCount: 2);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void AProfileCanBeTunedThroughTheDataType()
        {
            var tuned = new AiProfile(
                profileId: "legion-aggressive", decisionTickInterval: 10, placementSearchRadius: 6,
                powerReserve: 20, targetHarvesters: 4, harvesterQueueBatch: 3,
                targetArmySize: 20, attackSquadThreshold: 10, infantryQueueBatch: 4,
                targetDamageWeight: 12, targetThreatWeight: 8,
                targetFinishWeight: 2, targetDistanceWeight: 5,
                waveSize: 5, stagingDistanceCells: 20, stagingToleranceCells: 3,
                retreatHealthPercent: 30, retreatDangerCells: 6);

            var bound = new AiFactionProfile("Legion", tuned);

            Assert.That(bound.Profile, Is.EqualTo(tuned), "the tuning path must carry every value through");
            Assert.That(bound.TargetArmySize, Is.EqualTo(20));
            Assert.That(bound.Profile.DecisionTickInterval, Is.EqualTo((ushort)10),
                "the cadence is tunable now — it used to be a const nobody could reach");
            Assert.That(bound.Profile.TargetDamageWeight, Is.EqualTo(12),
                "the target weights are tunable data too, not constants in the behaviour");
            Assert.That(bound.Profile.WaveSize, Is.EqualTo(5),
                "the wave size is data as well — turning the rule off is a profile value, not a build");
        }

        // ----------------------------------------------------------------
        // (c) THE BINDING RULE: whole numbers only
        // ----------------------------------------------------------------

        [Test]
        public void NoProfileFieldIsAFloat()
        {
            // A float in a profile is a float in the simulation.
            // NoFloatInSimulationTests guards Scripts/Simulation and Scripts/AI*;
            // this states the same rule at the type that exists to hold numbers.
            foreach (PropertyInfo property in typeof(AiProfile).GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(float)),
                    $"AiProfile.{property.Name} is a float");
                Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(double)),
                    $"AiProfile.{property.Name} is a double");
                Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(decimal)),
                    $"AiProfile.{property.Name} is a decimal");
            }
        }

        [Test]
        public void TheDataLayerDoesNotDependOnTheSimulation()
        {
            // Nova.AI.Data references Nova.Core and nothing else, so a profile
            // cannot name a FactionId or a UnitRole and quietly turn into a
            // second definition table. In the .NET lane everything compiles
            // into one assembly, so the check is on the type surface.
            foreach (PropertyInfo property in typeof(AiProfile).GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                string ns = property.PropertyType.Namespace ?? string.Empty;
                Assert.That(ns.StartsWith("Nova.Simulation"), Is.False,
                    $"AiProfile.{property.Name} reaches into {ns} — the data layer must stay independent");
            }
        }
    }
}
