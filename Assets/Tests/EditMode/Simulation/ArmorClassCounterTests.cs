using NUnit.Framework;
using Nova.Simulation.Combat;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// The counter triangle (EditMode lane): the whole point of replacing the flat
    /// 15-damage placeholder. Pins the armor class every MS-1 role presents
    /// and — the load-bearing assertion — that swapping the TARGET's armor
    /// class inverts which of two attackers is the right answer.
    /// Mirror of the .NET lane ArmorClassCounterTests.
    /// </summary>
    [TestFixture]
    public class ArmorClassCounterTests
    {
        private static int PerShot(UnitRole attacker, UnitRole target)
        {
            WeaponProfile weapon = WeaponProfiles.Get(attacker);
            return DamageMatrix.Resolve(
                weapon.AttackDamage, weapon.DamageType, WeaponProfiles.GetArmorClass(target));
        }

        [Test]
        public void ArmorClasses_MatchTheAuthoritativeAssignment()
        {
            // ArmorSystem.md is authoritative over the drifted Infantry.md /
            // Vehicles.md summaries; these are its assignments.
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.BasicInfantry), Is.EqualTo(ArmorClass.Infantry));
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.AntiArmorInfantry), Is.EqualTo(ArmorClass.Infantry));

            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.Builder), Is.EqualTo(ArmorClass.Light));
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.Harvester), Is.EqualTo(ArmorClass.Light));
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.ScoutVehicle), Is.EqualTo(ArmorClass.Light));
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.Artillery), Is.EqualTo(ArmorClass.Light));

            // ArmorSystem.md puts BOTH tanks in Medium and reserves Heavy for
            // the (non-MS-1) Heavy Tank. The owner overrode that for the
            // BattleTank so the Heavy column is actually exercised in MS-1 and
            // the "Kinetic 0.25 vs Heavy forces rockets" counter can play.
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.LightTank), Is.EqualTo(ArmorClass.Medium));
            Assert.That(WeaponProfiles.GetArmorClass(UnitRole.BattleTank), Is.EqualTo(ArmorClass.Heavy));

            foreach (UnitRole role in new[]
                     {
                         UnitRole.HQ, UnitRole.Power, UnitRole.Refinery, UnitRole.Storage, UnitRole.Barracks,
                         UnitRole.VehicleFactory, UnitRole.ResearchLab, UnitRole.Radar, UnitRole.DefensePlatform,
                     })
            {
                Assert.That(WeaponProfiles.GetArmorClass(role), Is.EqualTo(ArmorClass.Building),
                    $"{role} is a building and every building type is class Building");
            }
        }

        [Test]
        public void AirHasNoMs1Carrier_ButHeavyDoes()
        {
            // Air still has no shipped carrier, so that column stays unexercised
            // and is asserted here: the day an aircraft lands this fails and the
            // fact gets re-decided instead of drifting.
            // Heavy DOES have a carrier since the owner promoted the BattleTank,
            // which is asserted positively so a silent demotion back to Medium
            // cannot pass unnoticed.
            bool anyHeavy = false;
            for (int index = 0; index < WeaponProfiles.RoleCount; index++)
            {
                ArmorClass armor = WeaponProfiles.GetArmorClass((UnitRole)index);
                Assert.That(armor, Is.Not.EqualTo(ArmorClass.Air), $"no MS-1 role is Air (role {(UnitRole)index})");
                anyHeavy |= armor == ArmorClass.Heavy;
            }

            Assert.That(anyHeavy, Is.True, "the Heavy column must have at least one MS-1 carrier (BattleTank)");
        }

        [Test]
        public void CounterTriangle_TargetArmorInvertsWhichAttackerIsEfficient()
        {
            // Per-shot damage of the two infantry roles against a Medium
            // target (LightTank) and an Infantry target (BasicInfantry).
            int kineticVsMedium = PerShot(UnitRole.BasicInfantry, UnitRole.LightTank);
            int explosiveVsMedium = PerShot(UnitRole.AntiArmorInfantry, UnitRole.LightTank);
            int kineticVsInfantry = PerShot(UnitRole.BasicInfantry, UnitRole.BasicInfantry);
            int explosiveVsInfantry = PerShot(UnitRole.AntiArmorInfantry, UnitRole.BasicInfantry);

            Assert.That(kineticVsMedium, Is.EqualTo(5), "10 base Kinetic x 0.50 vs Medium");
            Assert.That(explosiveVsMedium, Is.EqualTo(50), "50 base Explosive x 1.00 vs Medium");
            Assert.That(kineticVsInfantry, Is.EqualTo(10), "10 base Kinetic x 1.00 vs Infantry");
            Assert.That(explosiveVsInfantry, Is.EqualTo(37), "50 base Explosive x 0.75 vs Infantry, truncated");

            // (1) The matrix-level relation — the counter triangle itself.
            //     Explosive beats Kinetic on Medium and loses on Infantry.
            int explosiveMediumPercent = DamageMatrix.GetMultiplierPercent(DamageType.Explosive, ArmorClass.Medium);
            int kineticMediumPercent = DamageMatrix.GetMultiplierPercent(DamageType.Kinetic, ArmorClass.Medium);
            int explosiveInfantryPercent = DamageMatrix.GetMultiplierPercent(DamageType.Explosive, ArmorClass.Infantry);
            int kineticInfantryPercent = DamageMatrix.GetMultiplierPercent(DamageType.Kinetic, ArmorClass.Infantry);

            Assert.That(explosiveMediumPercent, Is.GreaterThan(kineticMediumPercent),
                "against Medium, Explosive is the efficient answer");
            Assert.That(explosiveInfantryPercent, Is.LessThan(kineticInfantryPercent),
                "against Infantry the relation INVERTS — Kinetic is the efficient answer");

            // (2) The gameplay-level relation, normalised for what a player
            //     actually spends: damage per tick per AE. Cross-multiplied so
            //     the comparison stays exact integer arithmetic.
            //     eff = damagePerShot / (cooldownTicks * costAE)
            //     effA > effB  <=>  dmgA * cdB * costB > dmgB * cdA * costA
            const int basicCooldown = 9, basicCost = 100;
            const int antiArmorCooldown = 25, antiArmorCost = 300;

            long antiArmorVsMedium = (long)explosiveVsMedium * basicCooldown * basicCost;
            long basicVsMedium = (long)kineticVsMedium * antiArmorCooldown * antiArmorCost;
            Assert.That(antiArmorVsMedium, Is.GreaterThan(basicVsMedium),
                "per AE and per tick, AntiArmorInfantry out-damages BasicInfantry against a Medium target");

            long antiArmorVsInfantry = (long)explosiveVsInfantry * basicCooldown * basicCost;
            long basicVsInfantry = (long)kineticVsInfantry * antiArmorCooldown * antiArmorCost;
            Assert.That(antiArmorVsInfantry, Is.LessThan(basicVsInfantry),
                "against an Infantry target the relation INVERTS — massed BasicInfantry is the efficient answer");
        }

        [Test]
        public void CounterTriangle_RawPerShotDamageDoesNotInvert_ByDesign()
        {
            // Recorded explicitly so nobody "fixes" a non-bug: the
            // AntiArmorInfantry's base damage is five times higher, so its RAW
            // per-shot number stays larger even where the multiplier turns
            // against it (37 > 10). The counter lives in efficiency —
            // multiplier, cadence and cost — not in the raw hit.
            Assert.That(
                PerShot(UnitRole.AntiArmorInfantry, UnitRole.BasicInfantry),
                Is.GreaterThan(PerShot(UnitRole.BasicInfantry, UnitRole.BasicInfantry)),
                "raw per-shot damage does not invert; only the normalised efficiency does");
        }

        [Test]
        public void CounterTriangle_BitesThroughMediumAndBuilding()
        {
            // The two columns MS-1 actually exercises. Medium: a 2x swing.
            Assert.That(
                DamageMatrix.GetMultiplierPercent(DamageType.Explosive, ArmorClass.Medium),
                Is.EqualTo(2 * DamageMatrix.GetMultiplierPercent(DamageType.Kinetic, ArmorClass.Medium)),
                "Kinetic 0.50 vs Explosive 1.00 against Medium");

            // Building: guns are near-useless, rockets are the siege answer.
            Assert.That(DamageMatrix.GetMultiplierPercent(DamageType.Kinetic, ArmorClass.Building), Is.EqualTo(30));
            Assert.That(DamageMatrix.GetMultiplierPercent(DamageType.Explosive, ArmorClass.Building), Is.EqualTo(75));

            Assert.That(PerShot(UnitRole.BasicInfantry, UnitRole.Barracks), Is.EqualTo(3), "10 x 0.30");
            Assert.That(PerShot(UnitRole.AntiArmorInfantry, UnitRole.Barracks), Is.EqualTo(37), "50 x 0.75");
            Assert.That(PerShot(UnitRole.Artillery, UnitRole.Barracks), Is.EqualTo(82), "110 x 0.75, truncated");
        }

        [Test]
        public void SameChassisDifferentWeapon_IsNoLongerIdentical()
        {
            // The defect this sprint exists to remove: a BattleTank and a
            // BasicInfantry used to hit for exactly the same 15.
            Assert.That(
                PerShot(UnitRole.BattleTank, UnitRole.BasicInfantry),
                Is.Not.EqualTo(PerShot(UnitRole.BasicInfantry, UnitRole.BasicInfantry)),
                "a BattleTank and a Rifleman must not be offensively identical");
            Assert.That(PerShot(UnitRole.BattleTank, UnitRole.BasicInfantry), Is.EqualTo(60), "60 Kinetic x 1.00");
            Assert.That(PerShot(UnitRole.BattleTank, UnitRole.LightTank), Is.EqualTo(30), "60 Kinetic x 0.50 vs Medium");
        }
    }
}
