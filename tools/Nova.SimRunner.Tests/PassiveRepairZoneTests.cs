using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Passive producer repair zone suite (.NET lane), Issue #55 / owner
    /// decision E-3 (2026-08-31): a completed Barracks or VehicleFactory
    /// heals its OWNER's damaged units of the roles it can produce, inside
    /// a footprint-aware Chebyshev radius of
    /// <see cref="ConstructionSystem.PassiveRepairRadiusCells"/>, at
    /// <see cref="ConstructionSystem.PassiveRepairRateHpPerTick"/> HP per
    /// tick, free of charge, without stacking, with the exact low-power
    /// even-tick halving — and never beyond MaxHealth, never the dead,
    /// never the under-construction (target or anchor), never buildings,
    /// never enemies.
    /// Mirror of the EditMode lane PassiveRepairZoneTests.
    /// </summary>
    [TestFixture]
    public sealed class PassiveRepairZoneTests
    {
        /// <summary>
        /// The same minimal host the construction suite's repair tests use:
        /// economy (phases 2/3) then construction (phase 4) — no movement,
        /// no combat, so the zone is the only actor that can move unit hit
        /// points. No Aetherium field: the command-path placement checks of
        /// the site tests keep their field spacing trivially satisfied.
        /// </summary>
        private sealed class Fixture
        {
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public ConstructionSystem Construction { get; }
            public SimulationKernel Kernel { get; }

            public Fixture(long startingCredits = 1000, System.Action<EconomySystem> configure = null)
            {
                Entities = new EntityManager(64);
                Economy = new EconomySystem(Entities, startingCredits);
                var costField = new CostField(ConstructionSystem.GridSize, ConstructionSystem.GridSize);
                Construction = new ConstructionSystem(Entities, Economy, costField);
                Kernel = new SimulationKernel(new SimRandom(42UL));
                Kernel.RegisterSystem(Economy);
                Kernel.RegisterSystem(Construction);
                // Pre-start configuration hook (e.g. slot factions): the
                // SetSlotFaction guard locks the assignment at Kernel.Start().
                configure?.Invoke(Economy);
                Kernel.Start();
            }

            public EntityId SpawnUnit(byte slot, int x, int y, UnitRole role, int maxHealth, int currentHealth)
            {
                EntityId id = Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                    SimFixed.FromInt(3),
                    maxHealth: maxHealth,
                    role: role);
                Entities.GetUnitRef(id).CurrentHealth = currentHealth;
                return id;
            }

            public int HealthOf(EntityId id)
            {
                return Entities.GetUnitRef(id).CurrentHealth;
            }

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) Kernel.StepTick();
            }
        }

        /// <summary>Full-power base: HQ (capacity + 30 power), a Power plant and the named producer.</summary>
        private static EntityId PlaceFullPowerProducer(Fixture f, ushort producerDefId, int originX, int originY, int anchorY)
        {
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 30, anchorY).IsValid, Is.True,
                "HQ keeps the starting credits inside the D-106 capacity");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 26, anchorY).IsValid, Is.True,
                "Power plant: 130 provided, so the producer never sees low power");
            EntityId producer = f.Construction.PlaceCompletedBuilding(0, producerDefId, originX, originY);
            Assert.That(producer.IsValid, Is.True, "producer placed completed");
            return producer;
        }

        private static UnitRoleMask MaskOf(UnitRole role)
        {
            return (UnitRoleMask)(1u << (int)role);
        }

        [Test]
        public void Mapping_IsDerivedFromTheProducerAssignment_ContentPinned()
        {
            // E-3 restated as content (D-077 producer assignment, both
            // factions identical): the Barracks heals the two infantry
            // roles, the VehicleFactory the four vehicle roles. A producer
            // reassignment in SimDefinitions moves this pin deliberately —
            // the mapping must follow the table, never a second list.
            Assert.That(ConstructionSystem.GetPassiveRepairableRoles(UnitRole.Barracks),
                Is.EqualTo(MaskOf(UnitRole.BasicInfantry) | MaskOf(UnitRole.AntiArmorInfantry)));
            Assert.That(ConstructionSystem.GetPassiveRepairableRoles(UnitRole.VehicleFactory),
                Is.EqualTo(MaskOf(UnitRole.ScoutVehicle) | MaskOf(UnitRole.LightTank)
                    | MaskOf(UnitRole.BattleTank) | MaskOf(UnitRole.Artillery)));

            // Every other role projects no zone: the issue scope is the two
            // combat-unit producers — not the HQ (Builder), not the
            // Refinery (Harvester), and never a non-producer.
            foreach (UnitRole role in new[]
            {
                UnitRole.Unit, UnitRole.Builder, UnitRole.Harvester,
                UnitRole.HQ, UnitRole.Refinery, UnitRole.Power, UnitRole.Storage,
                UnitRole.ResearchLab, UnitRole.Radar, UnitRole.DefensePlatform,
                UnitRole.BasicInfantry, UnitRole.Artillery,
            })
            {
                Assert.That(ConstructionSystem.GetPassiveRepairableRoles(role), Is.EqualTo(UnitRoleMask.None),
                    $"{role} projects no passive repair zone (Issue #55 scope)");
            }
        }

        [Test]
        public void VehicleFactory_HealsOwnDamagedVehicle_EveryTick_ForFree()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 8, 20, 20, anchorY: 20);
            EntityId tank = f.SpawnUnit(0, 25, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);

            f.Step(10);

            Assert.That(f.HealthOf(tank), Is.EqualTo(110),
                "1 HP per tick — deliberately far below the active repair's 10 (heals between engagements)");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L),
                "the zone is free: not one AE is debited, whatever the account holds");
        }

        [Test]
        public void ZoneRadius_IsFootprintAwareChebyshev_BoundaryPinned()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 8, 20, 20, anchorY: 20);
            // Footprint x/y 20..22; radius 3 covers the cells 17..25 per axis.
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 17, 17), Is.True, "near corner, distance 3");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 25, 25), Is.True, "far corner, Chebyshev 3");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 25, 21), Is.True, "edge cell, distance 3");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 16, 21), Is.False, "distance 4 is outside");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 21, 26), Is.False, "distance 4 is outside");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 26, 25), Is.False, "off the corner, Chebyshev 4");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 21, 21), Is.True,
                "the footprint itself reads as distance 0 — consistent for an overlay; no unit can stand there");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(1, 25, 21), Is.False,
                "the zone answer is per owner: slot 1 owns no factory here");

            // The same boundary as behavior, not only as a query.
            EntityId inside = f.SpawnUnit(0, 25, 25, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            EntityId outside = f.SpawnUnit(0, 26, 25, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            f.Step(10);
            Assert.That(f.HealthOf(inside), Is.EqualTo(110), "Chebyshev 3 from the footprint heals");
            Assert.That(f.HealthOf(outside), Is.EqualTo(100), "Chebyshev 4 does not");
        }

        [Test]
        public void E3_BarracksHealsInfantryNotVehicles_VehicleFactoryHealsVehiclesNotInfantry()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 7, 20, 20, anchorY: 20);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 8, 20, 40).IsValid, Is.True, "VehicleFactory");
            EntityId infantry = f.SpawnUnit(0, 23, 21, UnitRole.BasicInfantry, maxHealth: 90, currentHealth: 10);
            EntityId tankAtBarracks = f.SpawnUnit(0, 24, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            EntityId tank = f.SpawnUnit(0, 23, 41, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            EntityId infantryAtFactory = f.SpawnUnit(0, 24, 41, UnitRole.BasicInfantry, maxHealth: 90, currentHealth: 10);

            f.Step(10);

            Assert.That(f.HealthOf(infantry), Is.EqualTo(20), "the Barracks heals what it produces");
            Assert.That(f.HealthOf(tankAtBarracks), Is.EqualTo(100),
                "a Barracks repairing tanks is illogical (E-3) — the building choice keeps its meaning");
            Assert.That(f.HealthOf(tank), Is.EqualTo(110), "the VehicleFactory heals what it produces");
            Assert.That(f.HealthOf(infantryAtFactory), Is.EqualTo(10), "the VehicleFactory does not heal infantry");
        }

        [Test]
        public void ZoneScope_HqAndRefineryProjectNoZone()
        {
            // The derivation would yield Builder/Harvester for HQ/Refinery
            // from the same table — the ISSUE SCOPE grants the zone only to
            // the two combat-unit producers. Pin the scope so extending it
            // is a deliberate decision, never a silent side effect.
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 20, 20).IsValid, Is.True, "HQ");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 40, 20).IsValid, Is.True,
                "Refinery (20 required, HQ provides 30: full power)");
            EntityId builder = f.SpawnUnit(0, 23, 21, UnitRole.Builder, maxHealth: 350, currentHealth: 100);
            EntityId harvester = f.SpawnUnit(0, 43, 21, UnitRole.Harvester, maxHealth: 800, currentHealth: 100);

            f.Step(10);

            Assert.That(f.HealthOf(builder), Is.EqualTo(100), "the HQ projects no zone (issue scope)");
            Assert.That(f.HealthOf(harvester), Is.EqualTo(100), "the Refinery projects no zone (issue scope)");
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 23, 21), Is.False);
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 43, 21), Is.False);
        }

        [Test]
        public void NoStacking_TwoCoveringFactories_HealOncePerTick()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 8, 20, 20, anchorY: 20);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 8, 20, 24).IsValid, Is.True, "second VehicleFactory");
            // Cell (21,23): distance 1 to BOTH footprints (20..22 and y 24..26).
            EntityId tank = f.SpawnUnit(0, 21, 23, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 21, 23), Is.True, "doubly covered cell");

            f.Step(10);

            Assert.That(f.HealthOf(tank), Is.EqualTo(110),
                "two covering zones heal once, not twice — the building count must not buy healing");
        }

        [Test]
        public void NoOverheal_CapsAtMaxHealth_AndFullUnitsAreSkipped()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 8, 20, 20, anchorY: 20);
            EntityId almostFull = f.SpawnUnit(0, 25, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 549);
            EntityId full = f.SpawnUnit(0, 24, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 550);

            f.Step(5);

            Assert.That(f.HealthOf(almostFull), Is.EqualTo(550), "healing caps at MaxHealth, never beyond");
            Assert.That(f.HealthOf(full), Is.EqualTo(550), "a full unit is skipped, not re-topped");
        }

        [Test]
        public void NoHealing_ForTheDead_ForSites_ForSiteAnchors_AndForBuildings()
        {
            var f = new Fixture();
            EntityId factory = PlaceFullPowerProducer(f, 8, 20, 60, anchorY: 60);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 8, 20, 64).IsValid, Is.True,
                "second VehicleFactory, its zone covering the first (distance 2)");
            f.Step(1); // commit the power balance: the rule-path placements below read it

            // (a) The dead: a despawned tank is a store slot, not a patient.
            EntityId dead = f.SpawnUnit(0, 25, 61, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            Assert.That(f.Entities.DespawnUnit(dead), Is.True);

            // (c) Buildings are never patients: the first factory itself,
            // damaged, standing inside the second factory's zone.
            f.Entities.GetUnitRef(factory).CurrentHealth = 100;

            // (b) The under-construction TARGET: a paused Barracks site at
            // 1 HP inside the zone (distance 2 to the factory footprint, no
            // Builder alive to progress it).
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 56), Is.True, "site placed through the rule path");
            EntityId site = EntityId.Invalid;
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == UnitRole.Barracks) site = units[i].Id;
            }
            Assert.That(site.IsValid, Is.True, "the site entity exists (16.3: it carries its definition role)");
            Assert.That(f.HealthOf(site), Is.EqualTo(1), "a fresh site sits at 1 HP");

            // (d) The under-construction ANCHOR: an unfinished Barracks
            // site projects no zone for the infantry standing beside it
            // (distance 1 — inside where a COMPLETED Barracks would heal).
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 26, 56), Is.True, "paused Barracks site as anchor");
            EntityId infantry = f.SpawnUnit(0, 29, 57, UnitRole.BasicInfantry, maxHealth: 90, currentHealth: 10);
            Assert.That(f.Construction.IsCellInsidePassiveRepairZone(0, 29, 57), Is.False,
                "the site is no zone anchor — the query says so too");

            f.Step(10);

            Assert.That(f.Entities.IsValid(dead), Is.False, "the dead stay dead — no resurrection, no crash");
            Assert.That(f.HealthOf(site), Is.EqualTo(1),
                "units under construction are never healed (a site is a 1 HP entity of a building role)");
            Assert.That(f.HealthOf(factory), Is.EqualTo(100),
                "buildings are never healed by zones — building repair stays the Builder's job");
            Assert.That(f.HealthOf(infantry), Is.EqualTo(10),
                "a site projects no zone: only COMPLETED placements heal");
        }

        [Test]
        public void OwnOnly_EnemyUnitsInsideTheZone_DoNotHeal()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 8, 20, 20, anchorY: 20);
            EntityId own = f.SpawnUnit(0, 25, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            EntityId enemy = f.SpawnUnit(1, 24, 21, UnitRole.LightTank, maxHealth: 480, currentHealth: 100);

            f.Step(10);

            Assert.That(f.HealthOf(own), Is.EqualTo(110), "the owner's units heal");
            Assert.That(f.HealthOf(enemy), Is.EqualTo(100), "an enemy standing in the same cells gains nothing");
        }

        [Test]
        public void LowPower_HealsOnEvenTicksOnly_ExactHalving()
        {
            // 45 required (Refinery 20 + VehicleFactory 25) against the HQ's
            // 30 provided: low power from the first recompute on.
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 30, 20).IsValid, Is.True,
                "HQ keeps the starting credits inside the D-106 capacity");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 40, 40).IsValid, Is.True, "Refinery");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 8, 20, 20).IsValid, Is.True, "VehicleFactory");
            EntityId tank = f.SpawnUnit(0, 25, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);

            f.Step(1); // tick 1 (odd): commits the balance, no heal
            Assert.That(f.Economy.GetPlayerEconomy(0).IsLowPower, Is.True, "45 required vs 30 provided");
            Assert.That(f.HealthOf(tank), Is.EqualTo(100), "odd tick: the zone is silent");

            f.Step(10); // ticks 2..11: the five even ticks heal
            Assert.That(f.HealthOf(tank), Is.EqualTo(105),
                "exactly half rate under low power: one heal per two ticks, no rounding (C4 precedent)");

            f.Step(1); // tick 12 (even)
            Assert.That(f.HealthOf(tank), Is.EqualTo(106), "even tick: the zone heals");
            f.Step(1); // tick 13 (odd)
            Assert.That(f.HealthOf(tank), Is.EqualTo(106), "odd tick: the zone is silent again");
        }

        [Test]
        public void Deterministic_IdenticalFixtures_IdenticalStateHash()
        {
            ulong first = RunZoneScenario();
            ulong second = RunZoneScenario();
            Assert.That(second, Is.EqualTo(first),
                "the zone scan is ascending-index and parity-gated: identical setups hash identically");
        }

        private static ulong RunZoneScenario()
        {
            var f = new Fixture();
            PlaceFullPowerProducer(f, 8, 20, 20, anchorY: 20);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 7, 40, 40).IsValid, Is.True, "Barracks");
            f.SpawnUnit(0, 25, 21, UnitRole.LightTank, maxHealth: 550, currentHealth: 100);
            f.SpawnUnit(0, 24, 22, UnitRole.BattleTank, maxHealth: 1100, currentHealth: 700);
            f.SpawnUnit(0, 43, 41, UnitRole.BasicInfantry, maxHealth: 90, currentHealth: 10);
            f.SpawnUnit(1, 25, 25, UnitRole.LightTank, maxHealth: 480, currentHealth: 100); // enemy: untouched
            f.Step(50);
            return f.Kernel.CalculateStateHash();
        }
    }
}
