using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Per-role weapon values (.NET lane): pins the authored numbers from
    /// docs/gamedesign/Weapons.md (führend per D-047) against the definition
    /// tables, proves the role table is complete, and drives a live kernel to
    /// show the values really reach the tick path — including that unarmed
    /// roles never take a point of health off anything.
    /// Mirror of the EditMode lane WeaponValuesTests.
    /// </summary>
    [TestFixture]
    public sealed class WeaponValuesTests
    {
        private const ulong Seed = 0xC0BA7UL;
        private static readonly SimFixed HalfCell = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>The ruling's per-role table, restated independently of the production definitions.</summary>
        private static readonly object[] AuthoredUnitValues =
        {
            //          role,                       armor,                damage type,             dmg, range, cooldown
            new object[] { UnitRole.Builder,           ArmorClass.Light,    DamageType.Kinetic,     0,   0,  0 },
            new object[] { UnitRole.Harvester,         ArmorClass.Light,    DamageType.Kinetic,     0,   0,  0 },
            new object[] { UnitRole.BasicInfantry,     ArmorClass.Infantry, DamageType.Kinetic,    10,   7,  9 },
            new object[] { UnitRole.AntiArmorInfantry, ArmorClass.Infantry, DamageType.Explosive,  50,  10, 25 },
            new object[] { UnitRole.ScoutVehicle,      ArmorClass.Light,    DamageType.Kinetic,    12,   8, 10 },
            new object[] { UnitRole.LightTank,         ArmorClass.Medium,   DamageType.Kinetic,    35,   9, 20 },
            new object[] { UnitRole.BattleTank,        ArmorClass.Heavy,    DamageType.Kinetic,    60,  10, 25 },
            new object[] { UnitRole.Artillery,         ArmorClass.Light,    DamageType.Explosive, 110,  20, 70 },
        };

        [Test]
        public void UnitDefinitions_CarryTheAuthoredWeaponValues()
        {
            Assert.That(AuthoredUnitValues.Length, Is.EqualTo(SimDefinitions.UnitCount),
                "every MS-1 unit role is covered by the authored table");

            foreach (object entry in AuthoredUnitValues)
            {
                var row = (object[])entry;
                var role = (UnitRole)row[0];
                Assert.That(SimDefinitions.TryGetUnit(role, out SimUnitDefinition def), Is.True, $"{role} has a definition");

                Assert.That(def.ArmorClass, Is.EqualTo((ArmorClass)row[1]), $"{role} armor class");
                Assert.That(def.AttackDamage, Is.EqualTo((int)row[3]), $"{role} base damage");
                Assert.That(def.AttackRangeTiles, Is.EqualTo((int)row[4]), $"{role} range in tiles");
                Assert.That(def.AttackCooldownTicks, Is.EqualTo((int)row[5]), $"{role} cooldown in ticks");
                if (def.AttackDamage > 0)
                {
                    Assert.That(def.DamageType, Is.EqualTo((DamageType)row[2]), $"{role} damage type");
                }
            }
        }

        [Test]
        public void BuildingDefinitions_OnlyTheDefensePlatformIsArmed()
        {
            for (ushort id = 1; id <= SimDefinitions.BuildingCount; id++)
            {
                Assert.That(SimDefinitions.TryGetBuilding(id, out SimBuildingDefinition def), Is.True);
                Assert.That(def.ArmorClass, Is.EqualTo(ArmorClass.Building), $"{def.Role} is armor class Building");

                if (def.Role == UnitRole.DefensePlatform)
                {
                    // Buildings CAN shoot — the DefensePlatform does.
                    Assert.That(def.DamageType, Is.EqualTo(DamageType.Kinetic));
                    Assert.That(def.AttackDamage, Is.EqualTo(20));
                    Assert.That(def.AttackRangeTiles, Is.EqualTo(10));
                    Assert.That(def.AttackCooldownTicks, Is.EqualTo(10));
                }
                else
                {
                    Assert.That(def.AttackDamage, Is.EqualTo(0), $"{def.Role} is unarmed");
                    Assert.That(def.AttackRangeTiles, Is.EqualTo(0), $"{def.Role} has no weapon range");
                    Assert.That(def.AttackCooldownTicks, Is.EqualTo(0), $"{def.Role} has no firing cadence");
                }
            }
        }

        [Test]
        public void RoleTable_IsCompleteAndMirrorsTheDefinitions()
        {
            for (int index = 0; index < WeaponProfiles.RoleCount; index++)
            {
                var role = (UnitRole)index;
                WeaponProfile profile = WeaponProfiles.Get(role);

                if (role == UnitRole.Unit)
                {
                    // The generic fallback: kept armed on purpose, and scored
                    // at exactly 1.00 against itself so a roleless engagement
                    // applies its base damage unscaled.
                    Assert.That(profile.AttackDamage, Is.EqualTo(WeaponProfiles.FallbackAttackDamage));
                    Assert.That(profile.AttackCooldownTicks, Is.EqualTo(WeaponProfiles.FallbackAttackCooldownTicks));
                    Assert.That(profile.AttackRange, Is.EqualTo(SimFixed.FromInt(WeaponProfiles.FallbackAttackRangeTiles)));
                    Assert.That(
                        DamageMatrix.GetMultiplierPercent(profile.DamageType, profile.ArmorClass),
                        Is.EqualTo(DamageMatrix.NeutralPercent),
                        "the fallback must stay neutral against itself, or roleless combat silently rescales");
                    continue;
                }

                if (SimDefinitions.TryGetBuilding(role, out SimBuildingDefinition building))
                {
                    Assert.That(profile.ArmorClass, Is.EqualTo(building.ArmorClass));
                    Assert.That(profile.AttackDamage, Is.EqualTo(building.AttackDamage));
                    Assert.That(profile.AttackCooldownTicks, Is.EqualTo(building.AttackCooldownTicks));
                    Assert.That(profile.AttackRange, Is.EqualTo(SimFixed.FromInt(building.AttackRangeTiles)));
                }
                else
                {
                    Assert.That(SimDefinitions.TryGetUnit(role, out SimUnitDefinition unit), Is.True,
                        $"{role} must resolve to a definition or the weapon table is incomplete");
                    Assert.That(profile.ArmorClass, Is.EqualTo(unit.ArmorClass));
                    Assert.That(profile.AttackDamage, Is.EqualTo(unit.AttackDamage));
                    Assert.That(profile.AttackCooldownTicks, Is.EqualTo(unit.AttackCooldownTicks));
                    Assert.That(profile.AttackRange, Is.EqualTo(SimFixed.FromInt(unit.AttackRangeTiles)));
                }

                // 1 tile == 1 m (D-034/D-047): the conversion is the identity.
                Assert.That(profile.IsArmed, Is.EqualTo(profile.AttackDamage > 0),
                    "armed is defined by base damage and nothing else");
                if (profile.IsArmed)
                {
                    Assert.That(profile.AttackCooldownTicks, Is.GreaterThan(0),
                        "an armed role needs a positive cadence or it would fire every tick");
                    Assert.That(profile.AttackRange.RawValue, Is.GreaterThan(0), "an armed role needs reach");
                }
            }
        }

        [Test]
        public void UnarmedRoles_AreExactlyBuilderHarvesterAndTheEightPassiveBuildings()
        {
            var expectedUnarmed = new[]
            {
                UnitRole.Builder, UnitRole.Harvester,
                UnitRole.HQ, UnitRole.Power, UnitRole.Refinery, UnitRole.Storage,
                UnitRole.Barracks, UnitRole.VehicleFactory, UnitRole.ResearchLab, UnitRole.Radar,
            };

            for (int index = 0; index < WeaponProfiles.RoleCount; index++)
            {
                var role = (UnitRole)index;
                bool shouldBeUnarmed = System.Array.IndexOf(expectedUnarmed, role) >= 0;
                Assert.That(WeaponProfiles.Get(role).IsArmed, Is.EqualTo(!shouldBeUnarmed), $"{role} armed state");
            }
        }

        // ---------- live kernel: the values actually reach the tick path ----------

        private sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }

            private TestHost(SimulationKernel kernel, EntityManager entities)
            {
                Kernel = kernel;
                Entities = entities;
            }

            public static TestHost Create()
            {
                var entities = new EntityManager(64);
                var pathfinding = new PathfindingSystem(64, 64);
                var movement = new MovementSystem(entities, pathfinding);
                var fog = new FogOfWarSystem(entities, teamCount: 2, 64, 64);
                var combat = new CombatSystem(entities, fog);

                var kernel = new SimulationKernel(new SimRandom(Seed));
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fog);
                kernel.RegisterSystem(combat);
                kernel.Start();
                return new TestHost(kernel, entities);
            }

            public void Step(int count = 1)
            {
                for (int i = 0; i < count; i++) Kernel.StepTick();
            }
        }

        private static EntityId Spawn(
            TestHost host, byte team, UnitRole role, int cellX, int cellY,
            int sightRadius = 25, int maxHealth = 5000)
        {
            return host.Entities.SpawnUnit(
                team,
                new Transform2D(SimFixed.FromInt(cellX) + HalfCell, SimFixed.FromInt(cellY) + HalfCell),
                SimFixed.FromInt(1),
                radius: null,
                maxHealth: maxHealth,
                sightRadius: SimFixed.FromInt(sightRadius),
                role: role);
        }

        private static int HealthOf(TestHost host, EntityId id)
        {
            Assert.That(host.Entities.TryGetUnit(id, out UnitState u), Is.True, "unit must be alive");
            return u.CurrentHealth;
        }

        /// <summary>
        /// Runs one engagement for <paramref name="ticks"/> ticks and returns
        /// the health the target lost. The first legal shot lands on tick 2,
        /// when the 5 Hz Fog of War recompute first commits a view.
        /// </summary>
        private static int DamageDealt(UnitRole attackerRole, UnitRole targetRole, int distanceCells, int ticks)
        {
            var host = TestHost.Create();
            EntityId attacker = Spawn(host, 0, attackerRole, 10, 10);
            EntityId target = Spawn(host, 1, targetRole, 10 + distanceCells, 10);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            int before = HealthOf(host, target);
            host.Step(ticks);
            return before - HealthOf(host, target);
        }

        [Test]
        public void LiveKernel_AppliesTheRolesDamageAndCadence()
        {
            // BasicInfantry (10 Kinetic) vs an Infantry-armored target: 1.00.
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.BasicInfantry, 3, 2), Is.EqualTo(10),
                "one shot lands on the first committed view");
            // Cooldown 9: shots at tick 2 and 11, nothing in between.
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.BasicInfantry, 3, 10), Is.EqualTo(10),
                "the second shot is not due until tick 11");
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.BasicInfantry, 3, 11), Is.EqualTo(20),
                "exactly nine ticks between shots");

            // Same attacker, Medium target: 0.50.
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.LightTank, 3, 2), Is.EqualTo(5));
            // Same attacker, Building target: 0.30.
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.Barracks, 3, 2), Is.EqualTo(3));

            // BattleTank (60 Kinetic) — no longer identical to the rifleman.
            Assert.That(DamageDealt(UnitRole.BattleTank, UnitRole.BasicInfantry, 3, 2), Is.EqualTo(60));
            Assert.That(DamageDealt(UnitRole.BattleTank, UnitRole.LightTank, 3, 2), Is.EqualTo(30));

            // AntiArmorInfantry (50 Explosive): 1.00 on Medium, 0.75 on Infantry.
            Assert.That(DamageDealt(UnitRole.AntiArmorInfantry, UnitRole.LightTank, 3, 2), Is.EqualTo(50));
            Assert.That(DamageDealt(UnitRole.AntiArmorInfantry, UnitRole.BasicInfantry, 3, 2), Is.EqualTo(37));

            // A building that shoots (DefensePlatform, 20 Kinetic, range 10).
            Assert.That(DamageDealt(UnitRole.DefensePlatform, UnitRole.BasicInfantry, 8, 2), Is.EqualTo(20));
        }

        [Test]
        public void LiveKernel_UnarmedRolesNeverReduceHealth()
        {
            foreach (UnitRole role in new[] { UnitRole.Builder, UnitRole.Harvester, UnitRole.Barracks, UnitRole.HQ })
            {
                var host = TestHost.Create();
                EntityId attacker = Spawn(host, 0, role, 10, 10);
                EntityId target = Spawn(host, 1, UnitRole.BasicInfantry, 11, 10);
                host.Entities.GetUnitRef(attacker).AttackTarget = target;

                int before = HealthOf(host, target);
                host.Step(200); // far past every cadence in the table

                Assert.That(HealthOf(host, target), Is.EqualTo(before),
                    $"{role} is unarmed and must never reduce a target's health");
                Assert.That(host.Entities.GetUnitRef(attacker).WeaponCooldownTicks, Is.EqualTo(0),
                    $"{role} never fires, so it never starts a cooldown");
                Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(target),
                    $"{role} holds the order it cannot act on, exactly like an out-of-range attacker");
            }
        }

        [Test]
        public void LiveKernel_RangeIsPerRole()
        {
            // 15 cells apart: inside Artillery's 20, far outside the rifle's 7.
            Assert.That(DamageDealt(UnitRole.Artillery, UnitRole.BasicInfantry, 15, 2), Is.EqualTo(82),
                "110 Explosive x 0.75 vs Infantry, truncated");
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.BasicInfantry, 15, 60), Is.EqualTo(0),
                "a 7 m rifle cannot reach 15 m, however long it waits");

            // The rifle's own boundary still works: 7 cells is inside 7 + 0.5.
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.BasicInfantry, 7, 2), Is.EqualTo(10));
            Assert.That(DamageDealt(UnitRole.BasicInfantry, UnitRole.BasicInfantry, 8, 60), Is.EqualTo(0),
                "8 m is beyond range 7 + target radius 0.5");
        }

        [Test]
        public void LiveKernel_RepeatedShotsAccumulateWithoutDrift()
        {
            // LightTank: 35 Kinetic vs Heavy = 8 per shot (35 * 0.25 = 8.75,
            // truncated). Over many shots the total must be an exact multiple
            // of 8 — a fractional remainder carried between shots would show
            // up here immediately. The 0.75 remainder makes this a sharper
            // drift probe than the 0.5 the BattleTank produced as Medium.
            var host = TestHost.Create();
            EntityId attacker = Spawn(host, 0, UnitRole.LightTank, 10, 10);
            EntityId target = Spawn(host, 1, UnitRole.BattleTank, 13, 10, maxHealth: 5000);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            const int perShot = 8;
            // Cooldown 20: shots land on ticks 2, 22, 42, ... 182 -> 10 shots.
            host.Step(200);
            Assert.That(5000 - HealthOf(host, target), Is.EqualTo(10 * perShot),
                "ten shots remove exactly ten truncated hits, never nine or eleven");
        }
    }
}
