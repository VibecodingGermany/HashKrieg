using NUnit.Framework;
using Nova.AI;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// The combat strength formula: damage times health per firing interval,
    /// as one whole number per entity.
    /// <para>
    /// WHY THIS IS PINNED VALUE FOR VALUE and not just spot-checked: the wave
    /// gate compares a SUM of these against a profile threshold, so every unit
    /// that scores differently moves the tick a wave marches on. A drifting
    /// formula would not fail loudly — it would quietly retune the AI, and the
    /// only thing left to notice it would be the end-state pin, which cannot
    /// say why it moved.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class CombatStrengthTests
    {
        // ----------------------------------------------------------------
        // (a) The table, value for value
        // ----------------------------------------------------------------

        /// <summary>
        /// The six combat roles of both factions at full health. These twelve
        /// numbers ARE the argument for the whole rule: an Alliance rifleman is
        /// 100 and a Legion recruit is 44, so twelve of each are 1.200 against
        /// 528 — and a head count reports both as "a full wave of twelve".
        /// </summary>
        [TestCase(FactionId.Alliance, UnitRole.BasicInfantry, 100)]
        [TestCase(FactionId.Alliance, UnitRole.AntiArmorInfantry, 200)]
        [TestCase(FactionId.Alliance, UnitRole.ScoutVehicle, 264)]
        [TestCase(FactionId.Alliance, UnitRole.LightTank, 962)]
        [TestCase(FactionId.Alliance, UnitRole.BattleTank, 2640)]
        [TestCase(FactionId.Alliance, UnitRole.Artillery, 550)]
        [TestCase(FactionId.Legion, UnitRole.BasicInfantry, 44)]
        [TestCase(FactionId.Legion, UnitRole.AntiArmorInfantry, 144)]
        [TestCase(FactionId.Legion, UnitRole.ScoutVehicle, 180)]
        [TestCase(FactionId.Legion, UnitRole.LightTank, 672)]
        [TestCase(FactionId.Legion, UnitRole.BattleTank, 2500)]
        [TestCase(FactionId.Legion, UnitRole.Artillery, 274)]
        public void FullHealthStrengthMatchesTheDefinitionTable(
            FactionId faction, UnitRole role, int expected)
        {
            Assert.That(CombatStrength.OfFullHealth(faction, role), Is.EqualTo(expected));
        }

        /// <summary>
        /// The truncation is part of the value, not an accident of it.
        /// <para>
        /// An Alliance LightTank is 35 damage times 550 health over 20 ticks —
        /// 962.5 exactly. It scores 962. The arithmetic is integer throughout,
        /// so the only question is WHICH integer, and "the one integer division
        /// gives" is the answer both machines in a network match will reach.
        /// A Legion Artillery is 60 times 320 over 70 — 274.28 — and scores
        /// 274 for the same reason.
        /// </para>
        /// </summary>
        [Test]
        public void TheDivisionTruncatesAndTheTruncationIsPinned()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CombatStrength.OfFullHealth(FactionId.Alliance, UnitRole.LightTank),
                    Is.EqualTo(962), "35 * 550 / 20 is 962.5 — the value is 962, never 963");
                Assert.That(CombatStrength.OfFullHealth(FactionId.Legion, UnitRole.Artillery),
                    Is.EqualTo(274), "60 * 320 / 70 is 274.28 — the value is 274");
            });
        }

        // ----------------------------------------------------------------
        // (b) Unarmed is zero, without a special case
        // ----------------------------------------------------------------

        /// <summary>
        /// Every unarmed role of both factions scores 0 — and it does so
        /// because <c>AttackDamage</c> is 0, not because a list somewhere names
        /// them. The point of the assertion is that there is no second place
        /// that could disagree with the definition table: add an armed
        /// building tomorrow and this test starts counting it, which is the
        /// behaviour we want.
        /// </summary>
        [Test]
        public void EveryUnarmedRoleScoresZero()
        {
            Assert.Multiple(() =>
            {
                for (int factionIndex = 0; factionIndex < WeaponProfiles.FactionCount; factionIndex++)
                {
                    var faction = (FactionId)factionIndex;
                    for (int roleIndex = 0; roleIndex < WeaponProfiles.RoleCount; roleIndex++)
                    {
                        var role = (UnitRole)roleIndex;
                        if (WeaponProfiles.Get(faction, role).IsArmed) continue;

                        Assert.That(CombatStrength.OfFullHealth(faction, role), Is.EqualTo(0),
                            $"{faction} {role} carries no weapon and must weigh nothing");
                    }
                }
            });
        }

        /// <summary>
        /// The two roles that make the rule readable: a Builder and a Harvester
        /// are worth nothing to a wave, whatever their health bar says. The
        /// Alliance Harvester has 800 health — more than a LightTank — and
        /// still weighs 0, because a wave is not measured in hit points.
        /// </summary>
        [Test]
        public void HealthAloneIsWorthNothing()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CombatStrength.OfFullHealth(FactionId.Alliance, UnitRole.Harvester), Is.EqualTo(0));
                Assert.That(CombatStrength.OfFullHealth(FactionId.Alliance, UnitRole.Builder), Is.EqualTo(0));
                Assert.That(CombatStrength.OfFullHealth(FactionId.Legion, UnitRole.Harvester), Is.EqualTo(0));
                Assert.That(CombatStrength.OfFullHealth(FactionId.Legion, UnitRole.Builder), Is.EqualTo(0));

                // ...and the health it does have is not small: MORE than a
                // LightTank's, which is worth 962. Health against health — an
                // earlier version of this line compared health against half a
                // STRENGTH (800 against 481) and was a numeric coincidence
                // dressed as a control.
                Assert.That(SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.Harvester,
                    out SimUnitDefinition harvester), Is.True);
                Assert.That(SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.LightTank,
                    out SimUnitDefinition lightTank), Is.True);
                Assert.That(harvester.MaxHealth, Is.GreaterThan(lightTank.MaxHealth),
                    "the Harvester outlives a LightTank and is still worth nothing to a wave");
            });
        }

        // ----------------------------------------------------------------
        // (c) Health scales the value — that is the second half of the rule
        // ----------------------------------------------------------------

        /// <summary>
        /// A wounded unit is worth less, proportionally and by the same
        /// truncating division. This is what makes the gate say something a
        /// head count cannot: six riflemen at half health are not six
        /// riflemen.
        /// </summary>
        [Test]
        public void AWoundedUnitWeighsLess()
        {
            int full = CombatStrength.Of(FactionId.Alliance, UnitRole.BasicInfantry, 90);
            int half = CombatStrength.Of(FactionId.Alliance, UnitRole.BasicInfantry, 45);
            int sliver = CombatStrength.Of(FactionId.Alliance, UnitRole.BasicInfantry, 9);

            // AND ONE VALUE THAT DOES NOT DIVIDE EVENLY. The three above are
            // all multiples of the 9-tick cooldown, so they would pass even if
            // the expression associated as damage * (health / cooldown) — and
            // the wounded path is the only one the gate actually walks in a
            // match, because CombatStrength.Of reads CurrentHealth.
            int awkward = CombatStrength.Of(FactionId.Alliance, UnitRole.BasicInfantry, 50);

            Assert.Multiple(() =>
            {
                Assert.That(full, Is.EqualTo(100));
                Assert.That(half, Is.EqualTo(50), "10 * 45 / 9");
                Assert.That(sliver, Is.EqualTo(10), "10 * 9 / 9");
                Assert.That(awkward, Is.EqualTo(55),
                    "10 * 50 / 9 is 55.55 — the product is formed first, then truncated once");
            });
        }

        /// <summary>
        /// A dead or negative health value scores 0 rather than a negative
        /// strength. A negative summand would let one corpse pull a whole
        /// gathering wave back below its threshold — the wave would un-form.
        /// </summary>
        [Test]
        public void NoHealthIsNoStrength()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CombatStrength.Of(FactionId.Alliance, UnitRole.BasicInfantry, 0), Is.EqualTo(0));
                Assert.That(CombatStrength.Of(FactionId.Alliance, UnitRole.BasicInfantry, -50), Is.EqualTo(0));
            });
        }

        // ----------------------------------------------------------------
        // (d) The asymmetry the rule exists for
        // ----------------------------------------------------------------

        /// <summary>
        /// Twelve against twelve, stated as one assertion: the shipped wave
        /// size means two very different armies depending on which seat plays
        /// it, and the Legion is the one that marches short. This is the
        /// measured reason the loss column reads 51 against 23.
        /// </summary>
        [Test]
        public void TwelveOfEachIsNotTheSameWave()
        {
            int alliance = 12 * CombatStrength.OfFullHealth(FactionId.Alliance, UnitRole.BasicInfantry);
            int legion = 12 * CombatStrength.OfFullHealth(FactionId.Legion, UnitRole.BasicInfantry);

            Assert.Multiple(() =>
            {
                Assert.That(alliance, Is.EqualTo(1200));
                Assert.That(legion, Is.EqualTo(528));
                Assert.That(legion * 100 / alliance, Is.EqualTo(44),
                    "the Legion's full wave is 44 % of the Alliance's, and the head count calls them equal");
            });
        }
    }
}
