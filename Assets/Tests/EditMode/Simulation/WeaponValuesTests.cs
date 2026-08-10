using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Replays;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Per-role weapon values (EditMode lane): pins the authored numbers from
    /// docs/gamedesign/Weapons.md (führend per D-047) against the definition
    /// tables OF BOTH FACTIONS, proves the faction-by-role table is complete,
    /// pins the canonical DefinitionsHash64 (the real content hash behind the
    /// match fingerprint), and drives a live kernel to show the values really
    /// reach the tick path — including that unarmed roles never take a point
    /// of health off anything.
    /// Mirror of the .NET lane WeaponValuesTests.
    /// </summary>
    [TestFixture]
    public class WeaponValuesTests
    {
        private const ulong Seed = 0xC0BA7UL;
        private static readonly SimFixed HalfCell = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>The ruling's per-faction per-role table, restated independently of the production definitions.</summary>
        private static readonly object[] AuthoredUnitValues =
        {
            //          faction,            role,                       armor,                damage type,             dmg, range, cooldown
            new object[] { FactionId.Alliance, UnitRole.Builder,           ArmorClass.Light,    DamageType.Kinetic,     0,   0,  0 },
            new object[] { FactionId.Alliance, UnitRole.Harvester,         ArmorClass.Light,    DamageType.Kinetic,     0,   0,  0 },
            new object[] { FactionId.Alliance, UnitRole.BasicInfantry,     ArmorClass.Infantry, DamageType.Kinetic,    10,   7,  9 },
            new object[] { FactionId.Alliance, UnitRole.AntiArmorInfantry, ArmorClass.Infantry, DamageType.Explosive,  50,  10, 25 },
            new object[] { FactionId.Alliance, UnitRole.ScoutVehicle,      ArmorClass.Light,    DamageType.Kinetic,    12,   8, 10 },
            new object[] { FactionId.Alliance, UnitRole.LightTank,         ArmorClass.Medium,   DamageType.Kinetic,    35,   9, 20 },
            new object[] { FactionId.Alliance, UnitRole.BattleTank,        ArmorClass.Heavy,    DamageType.Kinetic,    60,  10, 25 },
            new object[] { FactionId.Alliance, UnitRole.Artillery,         ArmorClass.Light,    DamageType.Explosive, 110,  20, 70 },
            // Legion (Weapons.md Legion lines where they exist — Gewehr
            // 6–10/6 m/1.0 s, Raketenwerfer 40–60/9–11 m/2.5 s band minimum —
            // the concrete Vehicles.md damage lines for the three combat
            // vehicles (D-075), otherwise the documented integer-percent
            // derivation).
            new object[] { FactionId.Legion,   UnitRole.Builder,           ArmorClass.Light,    DamageType.Kinetic,     0,   0,  0 },
            new object[] { FactionId.Legion,   UnitRole.Harvester,         ArmorClass.Light,    DamageType.Kinetic,     0,   0,  0 },
            new object[] { FactionId.Legion,   UnitRole.BasicInfantry,     ArmorClass.Infantry, DamageType.Kinetic,     8,   6, 10 },
            new object[] { FactionId.Legion,   UnitRole.AntiArmorInfantry, ArmorClass.Infantry, DamageType.Explosive,  40,   9, 25 },
            new object[] { FactionId.Legion,   UnitRole.ScoutVehicle,      ArmorClass.Light,    DamageType.Kinetic,    10,   7, 10 },
            new object[] { FactionId.Legion,   UnitRole.LightTank,         ArmorClass.Medium,   DamageType.Kinetic,    28,   8, 20 },
            new object[] { FactionId.Legion,   UnitRole.BattleTank,        ArmorClass.Heavy,    DamageType.Explosive,  50,   8, 25 },
            new object[] { FactionId.Legion,   UnitRole.Artillery,         ArmorClass.Light,    DamageType.Explosive,  60,  18, 70 },
        };

        [Test]
        public void LegionVehicleDamage_UsesTheConcreteVehiclesMdValues_NotTheDerivation()
        {
            // D-075 (Teil-Entscheidung): where Vehicles.md names a concrete
            // Legion per-shot damage, that value wins over the integer-percent
            // derivation — the derivation would have produced 29/51/93 here.
            Assert.That(SimDefinitions.TryGetUnit(FactionId.Legion, UnitRole.LightTank, out SimUnitDefinition raeuber), Is.True);
            Assert.That(raeuber.AttackDamage, Is.EqualTo(28), "Räuber: concrete Vehicles.md value, not (35 x 85) / 100 = 29");
            Assert.That(SimDefinitions.TryGetUnit(FactionId.Legion, UnitRole.BattleTank, out SimUnitDefinition koloss), Is.True);
            Assert.That(koloss.AttackDamage, Is.EqualTo(50), "Koloss: concrete Vehicles.md value, not (60 x 85) / 100 = 51");
            Assert.That(SimDefinitions.TryGetUnit(FactionId.Legion, UnitRole.Artillery, out SimUnitDefinition donnerkanone), Is.True);
            Assert.That(donnerkanone.AttackDamage, Is.EqualTo(60), "Donnerkanone: concrete Vehicles.md value, not (110 x 85) / 100 = 93");

            // The derivation survives exactly where the GDDs are silent: the
            // Scout keeps (12 x 85) / 100 = 10.
            Assert.That(SimDefinitions.TryGetUnit(FactionId.Legion, UnitRole.ScoutVehicle, out SimUnitDefinition hyaene), Is.True);
            Assert.That(hyaene.AttackDamage, Is.EqualTo(10), "Scout: no concrete ruling — the derivation stands");
        }

        [Test]
        public void UnitDefinitions_CarryTheAuthoredWeaponValues()
        {
            Assert.That(AuthoredUnitValues.Length, Is.EqualTo(2 * SimDefinitions.UnitsPerFaction),
                "every MS-1 unit role is covered for BOTH factions");

            foreach (object entry in AuthoredUnitValues)
            {
                var row = (object[])entry;
                var faction = (FactionId)row[0];
                var role = (UnitRole)row[1];
                Assert.That(SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition def), Is.True,
                    $"{faction} {role} has a definition");
                Assert.That(def.Faction, Is.EqualTo(faction));

                Assert.That(def.ArmorClass, Is.EqualTo((ArmorClass)row[2]), $"{faction} {role} armor class");
                Assert.That(def.AttackDamage, Is.EqualTo((int)row[4]), $"{faction} {role} base damage");
                Assert.That(def.AttackRangeTiles, Is.EqualTo((int)row[5]), $"{faction} {role} range in tiles");
                Assert.That(def.AttackCooldownTicks, Is.EqualTo((int)row[6]), $"{faction} {role} cooldown in ticks");
                if (def.AttackDamage > 0)
                {
                    Assert.That(def.DamageType, Is.EqualTo((DamageType)row[3]), $"{faction} {role} damage type");
                }
            }
        }

        [Test]
        public void BuildingDefinitions_OnlyTheDefensePlatformIsArmed_InBothFactions()
        {
            int count = 0;
            foreach (SimBuildingDefinition def in SimDefinitions.AllBuildings)
            {
                count++;
                Assert.That(def.ArmorClass, Is.EqualTo(ArmorClass.Building), $"{def.Faction} {def.Role} is armor class Building");

                if (def.Role == UnitRole.DefensePlatform)
                {
                    // Buildings CAN shoot — the DefensePlatform does. The
                    // platform modules are faction-neutral content
                    // (Buildings.md section 3): identical weapon both sides.
                    Assert.That(def.DamageType, Is.EqualTo(DamageType.Kinetic));
                    Assert.That(def.AttackDamage, Is.EqualTo(20), $"{def.Faction} DefensePlatform damage");
                    Assert.That(def.AttackRangeTiles, Is.EqualTo(10), $"{def.Faction} DefensePlatform range");
                    Assert.That(def.AttackCooldownTicks, Is.EqualTo(10), $"{def.Faction} DefensePlatform cadence");
                }
                else
                {
                    Assert.That(def.AttackDamage, Is.EqualTo(0), $"{def.Faction} {def.Role} is unarmed");
                    Assert.That(def.AttackRangeTiles, Is.EqualTo(0), $"{def.Faction} {def.Role} has no weapon range");
                    Assert.That(def.AttackCooldownTicks, Is.EqualTo(0), $"{def.Faction} {def.Role} has no firing cadence");
                }
            }
            Assert.That(count, Is.EqualTo(2 * SimDefinitions.BuildingsPerFaction),
                "every MS-1 building role is covered for BOTH factions");
        }

        [Test]
        public void RoleTable_IsCompleteAndMirrorsTheDefinitions_ForBothFactions()
        {
            for (int factionIndex = 0; factionIndex < WeaponProfiles.FactionCount; factionIndex++)
            {
                var faction = (FactionId)factionIndex;
                for (int index = 0; index < WeaponProfiles.RoleCount; index++)
                {
                    var role = (UnitRole)index;
                    WeaponProfile profile = WeaponProfiles.Get(faction, role);

                    if (role == UnitRole.Unit)
                    {
                        // The generic fallback: kept armed on purpose, faction-
                        // independent, and scored at exactly 1.00 against
                        // itself so a roleless engagement applies its base
                        // damage unscaled.
                        Assert.That(profile.AttackDamage, Is.EqualTo(WeaponProfiles.FallbackAttackDamage));
                        Assert.That(profile.AttackCooldownTicks, Is.EqualTo(WeaponProfiles.FallbackAttackCooldownTicks));
                        Assert.That(profile.AttackRange, Is.EqualTo(SimFixed.FromInt(WeaponProfiles.FallbackAttackRangeTiles)));
                        Assert.That(
                            DamageMatrix.GetMultiplierPercent(profile.DamageType, profile.ArmorClass),
                            Is.EqualTo(DamageMatrix.NeutralPercent),
                            "the fallback must stay neutral against itself, or roleless combat silently rescales");
                        continue;
                    }

                    if (SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition building))
                    {
                        Assert.That(profile.ArmorClass, Is.EqualTo(building.ArmorClass));
                        Assert.That(profile.AttackDamage, Is.EqualTo(building.AttackDamage));
                        Assert.That(profile.AttackCooldownTicks, Is.EqualTo(building.AttackCooldownTicks));
                        Assert.That(profile.AttackRange, Is.EqualTo(SimFixed.FromInt(building.AttackRangeTiles)));
                    }
                    else
                    {
                        Assert.That(SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition unit), Is.True,
                            $"{faction} {role} must resolve to a definition or the weapon table is incomplete");
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
        }

        [Test]
        public void UnarmedRoles_AreExactlyBuilderHarvesterAndTheEightPassiveBuildings_InBothFactions()
        {
            var expectedUnarmed = new[]
            {
                UnitRole.Builder, UnitRole.Harvester,
                UnitRole.HQ, UnitRole.Power, UnitRole.Refinery, UnitRole.Storage,
                UnitRole.Barracks, UnitRole.VehicleFactory, UnitRole.ResearchLab, UnitRole.Radar,
            };

            for (int factionIndex = 0; factionIndex < WeaponProfiles.FactionCount; factionIndex++)
            {
                var faction = (FactionId)factionIndex;
                for (int index = 0; index < WeaponProfiles.RoleCount; index++)
                {
                    var role = (UnitRole)index;
                    bool shouldBeUnarmed = System.Array.IndexOf(expectedUnarmed, role) >= 0;
                    Assert.That(WeaponProfiles.Get(faction, role).IsArmed, Is.EqualTo(!shouldBeUnarmed),
                        $"{faction} {role} armed state");
                }
            }
        }

        // ---------- canonical definitions content hash (DefinitionsHash64) ----------

        [Test]
        public void DefinitionsHash64_IsStable_CoversBothFactions_AndIsNotAStub()
        {
            ulong hash = SimDefinitions.ComputeDefinitionsHash64();
            Assert.That(SimDefinitions.ComputeDefinitionsHash64(), Is.EqualTo(hash),
                "the same table must hash identically every time");
            Assert.That(hash, Is.Not.EqualTo(MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions)),
                "the real table hash replaces the empty-content stub");
            Assert.That(hash, Is.Not.EqualTo(MatchFingerprint.ComputeCurrentRulesHash64()));
            Assert.That(hash, Is.Not.EqualTo(MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map)));

            // Row coverage: mutating ANY single row — first Alliance, first
            // Legion, last Legion — moves the hash, so no row is skipped.
            Assert.That(HashWithMutatedBuilding(0), Is.Not.EqualTo(hash), "an Alliance building row is covered");
            Assert.That(HashWithMutatedBuilding(SimDefinitions.BuildingsPerFaction), Is.Not.EqualTo(hash),
                "a Legion building row is covered");
            Assert.That(HashWithMutatedUnit(2 * SimDefinitions.UnitsPerFaction - 1), Is.Not.EqualTo(hash),
                "the last Legion unit row is covered");
        }

        [Test]
        public void DefinitionsHash64_ChangesWhenAnyWeaponValueChanges()
        {
            // The fingerprint contract (SimulationCore.md section 6): a replay
            // recorded against other weapon values must hash differently.
            ulong canonical = SimDefinitions.ComputeDefinitionsHash64();

            // Mutate the Legion Rekrut's base damage (8 -> 9) in a table copy.
            var units = SimDefinitions.AllUnits.ToArray();
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i].Faction == FactionId.Legion && units[i].Role == UnitRole.BasicInfantry)
                {
                    units[i] = new SimUnitDefinition(
                        units[i].DefinitionId, units[i].Faction, units[i].Role, units[i].CostAE, units[i].BuildTicks,
                        units[i].Tier, units[i].ProducerRole, units[i].MaxHealth, units[i].MoveSpeed,
                        units[i].ArmorClass, units[i].DamageType,
                        attackDamage: units[i].AttackDamage + 1, units[i].AttackRangeTiles, units[i].AttackCooldownTicks);
                }
            }
            Assert.That(SimDefinitions.ComputeDefinitionsHash64(SimDefinitions.AllBuildings, units),
                Is.Not.EqualTo(canonical), "a mutated weapon damage must move the definitions hash");

            // Mutate the DefensePlatform weapon (20 -> 21) in a table copy.
            var buildings = SimDefinitions.AllBuildings.ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i].Role == UnitRole.DefensePlatform && buildings[i].Faction == FactionId.Alliance)
                {
                    buildings[i] = new SimBuildingDefinition(
                        buildings[i].DefinitionId, buildings[i].Faction, buildings[i].Role,
                        buildings[i].CostAE, buildings[i].BuildTicks, buildings[i].PowerProvided, buildings[i].PowerRequired,
                        buildings[i].HasPrerequisite, buildings[i].PrerequisiteRole, buildings[i].MaxHealth,
                        buildings[i].ArmorClass, buildings[i].DamageType,
                        attackDamage: buildings[i].AttackDamage + 1, buildings[i].AttackRangeTiles, buildings[i].AttackCooldownTicks);
                }
            }
            Assert.That(SimDefinitions.ComputeDefinitionsHash64(buildings, SimDefinitions.AllUnits),
                Is.Not.EqualTo(canonical), "a mutated platform weapon must move the definitions hash");
        }

        private static ulong HashWithMutatedBuilding(int index)
        {
            var buildings = SimDefinitions.AllBuildings.ToArray();
            buildings[index] = new SimBuildingDefinition(
                buildings[index].DefinitionId, buildings[index].Faction, buildings[index].Role,
                costAE: buildings[index].CostAE + 1, buildings[index].BuildTicks,
                buildings[index].PowerProvided, buildings[index].PowerRequired,
                buildings[index].HasPrerequisite, buildings[index].PrerequisiteRole, buildings[index].MaxHealth,
                buildings[index].ArmorClass, buildings[index].DamageType,
                buildings[index].AttackDamage, buildings[index].AttackRangeTiles, buildings[index].AttackCooldownTicks);
            return SimDefinitions.ComputeDefinitionsHash64(buildings, SimDefinitions.AllUnits);
        }

        private static ulong HashWithMutatedUnit(int index)
        {
            var units = SimDefinitions.AllUnits.ToArray();
            units[index] = new SimUnitDefinition(
                units[index].DefinitionId, units[index].Faction, units[index].Role,
                costAE: units[index].CostAE + 1, units[index].BuildTicks,
                units[index].Tier, units[index].ProducerRole, units[index].MaxHealth, units[index].MoveSpeed,
                units[index].ArmorClass, units[index].DamageType,
                units[index].AttackDamage, units[index].AttackRangeTiles, units[index].AttackCooldownTicks);
            return SimDefinitions.ComputeDefinitionsHash64(SimDefinitions.AllBuildings, units);
        }

        // ---------- live kernel: the values actually reach the tick path ----------

        private sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public EconomySystem Factions { get; }

            private TestHost(SimulationKernel kernel, EntityManager entities, EconomySystem factions)
            {
                Kernel = kernel;
                Entities = entities;
                Factions = factions;
            }

            public static TestHost Create()
            {
                var entities = new EntityManager(64);
                var pathfinding = new PathfindingSystem(64, 64);
                var movement = new MovementSystem(entities, pathfinding);
                // Faction source for the combat weapon table: an unregistered
                // economy state (all slots default to Alliance). 16.5: the
                // FoW radar read also requires the placement register.
                var factions = new EconomySystem(entities);
                var construction = new Nova.Simulation.Construction.ConstructionSystem(entities, factions);
                var fog = new FogOfWarSystem(entities, construction, factions, teamCount: 2, 64, 64);
                var combat = new CombatSystem(entities, fog, factions, construction);

                var kernel = new SimulationKernel(new SimRandom(Seed));
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fog);
                kernel.RegisterSystem(combat);
                kernel.Start();
                return new TestHost(kernel, entities, factions);
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
        public void LiveKernel_LegionProfiles_ReachTheTickPath()
        {
            // The attacker's slot faction selects the weapon row: a Legion
            // Rekrut deals 8 (not the Alliance rifleman's 10), a Legion
            // Koloss deals 50 Explosive (1.00 vs the Medium LightTank).
            var host = TestHost.Create();
            host.Factions.SetSlotFaction(0, FactionId.Legion);
            EntityId rekrut = Spawn(host, 0, UnitRole.BasicInfantry, 10, 10);
            EntityId target = Spawn(host, 1, UnitRole.BasicInfantry, 13, 10);
            host.Entities.GetUnitRef(rekrut).AttackTarget = target;
            int before = HealthOf(host, target);
            host.Step(2);
            Assert.That(before - HealthOf(host, target), Is.EqualTo(8),
                "Legion BasicInfantry fires the Legion row (8 Kinetic)");

            var host2 = TestHost.Create();
            host2.Factions.SetSlotFaction(0, FactionId.Legion);
            host2.Factions.SetSlotFaction(1, FactionId.Legion);
            EntityId koloss = Spawn(host2, 0, UnitRole.BattleTank, 10, 10);
            EntityId raeuber = Spawn(host2, 1, UnitRole.LightTank, 13, 10);
            host2.Entities.GetUnitRef(koloss).AttackTarget = raeuber;
            int before2 = HealthOf(host2, raeuber);
            host2.Step(2);
            Assert.That(before2 - HealthOf(host2, raeuber), Is.EqualTo(50),
                "Legion Koloss fires 50 Explosive at 1.00 vs Medium");

            // Legion range is the Legion row's: 6 m does not reach 7 cells.
            var host3 = TestHost.Create();
            host3.Factions.SetSlotFaction(0, FactionId.Legion);
            EntityId rekrut3 = Spawn(host3, 0, UnitRole.BasicInfantry, 10, 10);
            EntityId target3 = Spawn(host3, 1, UnitRole.BasicInfantry, 17, 10);
            host3.Entities.GetUnitRef(rekrut3).AttackTarget = target3;
            int before3 = HealthOf(host3, target3);
            host3.Step(60);
            Assert.That(before3 - HealthOf(host3, target3), Is.EqualTo(0),
                "the Legion rifle (6 m) cannot reach 7 m, however long it waits");
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
