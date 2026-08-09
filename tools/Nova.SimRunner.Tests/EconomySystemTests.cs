using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Canonical economy suite (.NET lane): per-slot credits and power
    /// (SimulationCore.md section 2, phase 2), the finite Aetherium harvest
    /// cycle (phase 3) and the snapshot block 104 v1 contract. G2
    /// reservation: no D-010 regrowth/spread/overharvest — fields are finite
    /// and stay exhausted.
    /// Mirror of the EditMode lane EconomySystemTests.
    /// </summary>
    [TestFixture]
    public sealed class EconomySystemTests
    {
        private static EntityManager CreateEntities() => new EntityManager(64);

        private static EntityId SpawnHarvester(EntityManager entities, byte player, int x, int y)
        {
            return entities.SpawnUnit(
                player,
                new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                SimFixed.FromInt(4),
                role: UnitRole.Harvester);
        }

        [Test]
        public void StartConditions_AreCanonicalManifestValues()
        {
            // The canonical match start balance (D-077 —
            // startStatePerPlayer.aetheriumAE of quality/content/mvp-v1.json)
            // lives in ONE named constant the match hosts pass explicitly.
            Assert.That(EconomySystem.CanonicalMatchStartingCreditsAE, Is.EqualTo(3000L),
                "startStatePerPlayer.aetheriumAE of quality/content/mvp-v1.json (D-077)");

            var economy = new EconomySystem(CreateEntities(), EconomySystem.CanonicalMatchStartingCreditsAE);
            for (byte slot = 0; slot < EconomySystem.MaxPlayers; slot++)
            {
                Assert.That(economy.GetPlayerEconomy(slot).AetheriumCredits, Is.EqualTo(3000L));
            }
        }

        [Test]
        public void ParameterlessStartCredits_StayTheLibraryDefault()
        {
            // The constructor default is deliberately NOT the match rule
            // (D-077): existing fixtures keep their 1.000 AE arithmetic;
            // match hosts pass CanonicalMatchStartingCreditsAE explicitly.
            var economy = new EconomySystem(CreateEntities());
            for (byte slot = 0; slot < EconomySystem.MaxPlayers; slot++)
            {
                Assert.That(economy.GetPlayerEconomy(slot).AetheriumCredits, Is.EqualTo(1000L),
                    "library default — the canonical 3.000 AE are opt-in via the constant");
            }
        }

        [Test]
        public void Credits_NeverGoNegative_SpendingIsAtomic()
        {
            var economy = new EconomySystem(CreateEntities());
            ref PlayerEconomyState eco = ref economy.GetPlayerEconomy(0);

            Assert.That(eco.TrySpendCredits(2000), Is.False, "overspending must be refused");
            Assert.That(eco.AetheriumCredits, Is.EqualTo(1000L), "a refused spend mutates nothing");

            Assert.That(eco.TrySpendCredits(1000), Is.True);
            Assert.That(eco.AetheriumCredits, Is.EqualTo(0L));
            Assert.That(eco.TrySpendCredits(1), Is.False);
            Assert.That(eco.AetheriumCredits, Is.EqualTo(0L), "the balance can never go negative");

            eco.AddCredits(330);
            Assert.That(eco.AetheriumCredits, Is.EqualTo(330L));
        }

        [Test]
        public void LowPowerMultiplier_IsExactQ16Half()
        {
            var eco = new PlayerEconomyState(0)
            {
                PowerProvided = 0,
                PowerRequired = 1,
            };
            Assert.That(eco.IsLowPower, Is.True);
            Assert.That(eco.ProductionSpeedMultiplierQ16.RawValue, Is.EqualTo(32768),
                "0.5 is exact in Q16.16 — no float relic");

            eco.PowerProvided = 1;
            Assert.That(eco.IsLowPower, Is.False);
            Assert.That(eco.ProductionSpeedMultiplierQ16.RawValue, Is.EqualTo(SimFixed.OneRaw));
        }

        [Test]
        public void PowerRecompute_DerivesFromBuildingRoles_AndDropsOnDespawn()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();

            EntityId hq = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.HQ);
            EntityId plant = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(8), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Power);
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Refinery);

            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(130), "HQ 30 + plant 100 (provisional)");
            Assert.That(economy.GetPlayerEconomy(0).PowerRequired, Is.EqualTo(20), "refinery 20 (provisional)");
            Assert.That(economy.GetPlayerEconomy(0).IsLowPower, Is.False);

            // Combat-style despawn of the power plant: the next recompute
            // reflects the loss deterministically.
            entities.DespawnUnit(plant);
            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(30));
            Assert.That(economy.GetPlayerEconomy(0).IsLowPower, Is.False);

            entities.DespawnUnit(hq);
            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(0));
            Assert.That(economy.GetPlayerEconomy(0).IsLowPower, Is.True);
            Assert.That(economy.GetPlayerEconomy(0).ProductionSpeedMultiplierQ16.RawValue, Is.EqualTo(32768));
        }

        [Test]
        public void PowerRecompute_IsFactionResolved_LegionPowerPlantProvides80()
        {
            // The same building ROLE feeds different power depending on the
            // owner slot's faction (Buildings.md section 2: Alliance 100,
            // Legion 80) — the recompute resolves (faction, role), not role.
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            economy.SetSlotFaction(1, FactionId.Legion); // before Start — the guard requires it
            kernel.Start();

            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Power);
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(8), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Power);
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Barracks);

            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(100), "Alliance plant");
            Assert.That(economy.GetPlayerEconomy(1).PowerProvided, Is.EqualTo(80), "Legion plant");
            Assert.That(economy.GetPlayerEconomy(1).PowerRequired, Is.EqualTo(10), "Legion Barracks draws 10");
        }

        [Test]
        public void HarvestCycle_GathersExactRate_AndDepositRaisesCreditsExactly()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            // 16.4: deposits obey the derived storage ceiling — a completed
            // HQ provides the 2.000 AE base. Far away, so no reach rule here
            // is touched.
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(60), SimFixed.FromInt(60)), SimFixed.Zero, role: UnitRole.HQ);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(20), "exactly 2 AE per tick");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8980L), "the field reserve sinks exactly");

            // Deposit at an own refinery in reach (adjacent cell).
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(10)), SimFixed.Zero, role: UnitRole.Refinery);
            entities.GetUnitRef(harvester).HarvestFieldId = 0;
            entities.GetUnitRef(harvester).IsReturningCargo = true;

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0));
            Assert.That(entities.GetUnitRef(harvester).IsReturningCargo, Is.False, "the deposit resolves the order");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1020L),
                "credits rise by exactly the cargo");
        }

        [Test]
        public void HarvesterDeposit_OverflowIsForfeitAtTheStorageCeiling()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities, startingCredits: 1995);
            kernel.RegisterSystem(economy);
            kernel.Start();

            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(60), SimFixed.FromInt(60)), SimFixed.Zero,
                role: UnitRole.HQ);
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(10)), SimFixed.Zero,
                role: UnitRole.Refinery);
            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.CargoAE = 10;
            unit.IsReturningCargo = true;

            kernel.StepTick();

            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(EconomySystem.HqBaseCapacityAE),
                "only 5 of the 10 AE cargo fit below the HQ ceiling");
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0),
                "overflow is forfeit, so the full cargo leaves the Harvester");
            Assert.That(entities.GetUnitRef(harvester).IsReturningCargo, Is.False);
        }

        [Test]
        public void ReturnOrder_RefineryFootprintEdgeInReach_DepositsWithCentreTwoCellsAway()
        {
            // Regression for the GB-004 deposit fix: the construction path
            // spawns the refinery entity at the footprint CENTRE (origin+1),
            // so a harvester adjacent to the footprint edge is Chebyshev 2
            // from the entity cell. Reach is measured against the footprint —
            // under the old centre-cell rule the opening harvesters' full
            // cargo never deposited (credits frozen at 1000, GB-001 finding).
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            kernel.RegisterSystem(economy);
            kernel.Start();

            // Real placement path: entity at centre (9,5) of footprint
            // (8,4)-(10,6) — the canonical opening layout.
            EntityId refinery = construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Refinery), 8, 4);
            Assert.That(refinery.IsValid, Is.True);

            // 16.4: the deposit obeys the derived ceiling — completed HQ,
            // far away so no reach rule here is touched.
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.HQ), 40, 40).IsValid, Is.True);

            // Adjacent to the footprint's west edge cell (8,6), Chebyshev 2
            // from the centre (9,5).
            EntityId harvester = SpawnHarvester(entities, 0, 7, 6);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.CargoAE = 100;
            unit.IsReturningCargo = true;

            kernel.StepTick();

            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0),
                "the footprint-adjacent refinery accepts the deposit");
            Assert.That(entities.GetUnitRef(harvester).IsReturningCargo, Is.False);
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1100L));
        }

        [Test]
        public void AutoCycle_CanonicalOpeningDistances_CompletesRoundTripAndResumes()
        {
            // End-to-end over the fixed cycle: field cell (7,7), refinery
            // footprint (8,4)-(10,6), harvester at (7,6) — the exact opening
            // geometry. 330 AE at 2 AE/tick fills in 165 ticks, the deposit
            // lands the tick after, and the retained field id resumes harvest.
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(7, 7), 9000), Is.True);
            construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Refinery), 8, 4);
            // 16.4: deposits obey the derived ceiling — completed HQ, far
            // away so the opening geometry under test is untouched.
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.HQ), 40, 40).IsValid, Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 7, 6);
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            for (int i = 0; i < 166; i++)
            {
                kernel.StepTick();
            }
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1330L),
                "full cargo delivered at footprint reach");
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0));

            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(20),
                "the retained field id resumes the auto-cycle after the deposit");
        }

        [Test]
        public void Harvest_StopsAtCapacity_AndStartsTheReturnLeg()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.HarvestFieldId = 1;
            unit.CargoAE = SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance) - 1; // 329 of 330

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance)),
                "only the free cargo space is gathered");
            Assert.That(unit.HarvestFieldId, Is.EqualTo((ushort)1), "the field id is retained for the auto-cycle");
            Assert.That(unit.IsReturningCargo, Is.True, "a full cargo starts the return leg");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8999L));

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance)),
                "no further gathering while the return leg holds without a refinery in reach");
        }

        [Test]
        public void Harvest_StopsAtFactionCapacity_Legion300_Alliance330()
        {
            // The capacities come from the canonical definition rows
            // (factions[i].identity.harvesterCargoAE of mvp-v1.json).
            Assert.That(SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance), Is.EqualTo(330));
            Assert.That(SimDefinitions.HarvesterCargoCapacityAE(FactionId.Legion), Is.EqualTo(300));
            Assert.That(SimDefinitions.MaxHarvesterCargoCapacityAE, Is.EqualTo(330),
                "the hard cap is the larger of the two faction capacities");

            // A Legion harvester clamps at 300, NOT at the Alliance 330.
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            economy.SetSlotFaction(0, FactionId.Legion); // before Start — the guard requires it
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.HarvestFieldId = 1;
            unit.CargoAE = SimDefinitions.HarvesterCargoCapacityAE(FactionId.Legion) - 1; // 299 of 300

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(300),
                "the Legion harvester clamps at the Legion capacity, not the Alliance 330");
            Assert.That(unit.HarvestFieldId, Is.EqualTo((ushort)1), "the field id is retained for the auto-cycle");
            Assert.That(unit.IsReturningCargo, Is.True,
                "a full Legion cargo starts the return leg at 300");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8999L), "only the single free AE was gathered");
        }

        [Test]
        public void FiniteField_CollectsOnlyRemainder_ThenStaysExhausted()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 3), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            kernel.StepTick(); // gathers 2, remainder 1
            kernel.StepTick(); // gathers the last 1, field exhausted, order resolves
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(3),
                "a field with less than the rate left yields only the remainder");
            Assert.That(entities.GetUnitRef(harvester).HarvestFieldId, Is.EqualTo((ushort)0),
                "exhaustion resolves the order — the harvester goes idle");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(0L));
            Assert.That(field.IsExhausted, Is.True);

            kernel.StepTick(); // G2 reservation: no regrowth — the field stays exhausted
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(3));
            Assert.That(economy.TryGetField(1, out field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(0L));
        }

        [Test]
        public void HarvestOrder_OutOfReach_IsHeldNotDropped()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 20, 20); // far away
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0));
            Assert.That(entities.GetUnitRef(harvester).HarvestFieldId, Is.EqualTo((ushort)1),
                "out-of-reach orders are held — closing the distance is Movement's concern");
        }

        [Test]
        public void HarvestOrder_OnNonHarvesterRole_IsIneffective()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId soldier = entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(4));
            entities.GetUnitRef(soldier).HarvestFieldId = 1;

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(soldier).CargoAE, Is.EqualTo(0),
                "harvest orders apply to the Harvester role only (documented provisional rule)");
        }

        [Test]
        public void ReturnOrder_WithoutRefineryInReach_IsHeld()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.CargoAE = 50;
            unit.IsReturningCargo = true;

            // A foreign refinery in reach does not count.
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(10)), SimFixed.Zero, role: UnitRole.Refinery);

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(50));
            Assert.That(unit.IsReturningCargo, Is.True, "no own refinery in reach: the order holds");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L));
        }

        [Test]
        public void TryAddField_ValidatesIdentityAndReserve()
        {
            var economy = new EconomySystem(CreateEntities());
            Assert.That(economy.TryAddField(0, new GridPos2D(1, 1), 9000), Is.False, "id 0 is invalid");
            Assert.That(economy.TryAddField(1, new GridPos2D(1, 1), 0), Is.False, "the reserve must be positive");
            Assert.That(economy.TryAddField(1, GridPos2D.Invalid, 9000), Is.False, "the position must be valid");
            Assert.That(economy.TryAddField(1, new GridPos2D(1, 1), 9000), Is.True);
            Assert.That(economy.TryAddField(1, new GridPos2D(2, 2), 9000), Is.False, "duplicate id");
            Assert.That(economy.FieldCount, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------
        // 16.4 (#53, D-024/D-096): the derived AE ceiling
        // ------------------------------------------------------------------

        [Test]
        public void DepositCapped_ClampsAtTheDerivedCeiling_OverflowIsForfeit()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.HQ), 40, 40).IsValid, Is.True);

            Assert.That(economy.CapacityFor(0), Is.EqualTo(EconomySystem.HqBaseCapacityAE), "one completed HQ: the 2.000 AE base");

            Assert.That(economy.DepositCapped(0, 1500), Is.EqualTo(1000L),
                "only what fits under the ceiling lands");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(2000L),
                "1000 start + 1000 that fit — the remaining 500 are forfeit");
            Assert.That(economy.DepositCapped(0, 500), Is.EqualTo(0L), "at the ceiling nothing more lands");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(2000L));
            Assert.That(economy.CapacityFor(1), Is.EqualTo(0L), "no buildings, no ceiling — the other slot is unaffected");
        }

        [Test]
        public void CapacityFor_CountsCompletedStorage_AndExcludesSites()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities, startingCredits: 3000);
            var construction = new ConstructionSystem(entities, economy);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.HQ), 40, 40).IsValid, Is.True);
            kernel.StepTick(); // commit the grid (30 provided) for the placement power rule

            // A storage SITE holds nothing yet.
            Assert.That(construction.TryPlaceBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Storage), 20, 20), Is.True,
                "storage site placed (cost fits the 3.000 start)");
            Assert.That(economy.CapacityFor(0), Is.EqualTo(EconomySystem.HqBaseCapacityAE),
                "an unfinished silo holds nothing");

            // A COMPLETED storage adds its 2.000.
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Storage), 50, 50).IsValid, Is.True);
            Assert.That(economy.CapacityFor(0), Is.EqualTo(EconomySystem.HqBaseCapacityAE + EconomySystem.StorageCapacityBonusAE),
                "HQ base + one completed storage");
        }

        [Test]
        public void DecayExcessBalance_QuarterPerSecond_ConvergesBelowTheCeiling()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.HQ), 40, 40).IsValid, Is.True);

            economy.GetPlayerEconomy(0).AddCredits(2000); // raw write: 3.000 total, 1.000 over the 2.000 ceiling
            for (int i = 0; i < 9; i++) kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(3000L),
                "no decay between the per-second decay ticks");

            kernel.StepTick(); // tick 10: first decay — 25% of the 1.000 excess
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(2750L));

            for (int i = 0; i < 10; i++) kernel.StepTick(); // tick 20: 25% of 750 (floor 187)
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(2563L),
                "integer floor decay, once per second");

            for (int i = 0; i < 80; i++) kernel.StepTick(); // tick 100: converging, minimum-1-AE steps
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(2058L));
        }

        [Test]
        public void DecayExcessBalance_NeverTouchesBalancesAtOrBelowTheCeiling()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(construction.PlaceCompletedBuilding(
                0, SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.HQ), 40, 40).IsValid, Is.True);

            for (int i = 0; i < 25; i++) kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L),
                "1.000 under the 2.000 ceiling: the decay never runs");

            // Without any building the ceiling is zero and even the start
            // stock decays — the destruction path of D-024.
            var lone = new EconomySystem(CreateEntities());
            var loneKernel = new SimulationKernel(new SimRandom(42UL));
            loneKernel.RegisterSystem(lone);
            loneKernel.Start();
            Assert.That(lone.DepositCapped(0, 500), Is.EqualTo(0L), "no ceiling, no deposit");
            for (int i = 0; i < 10; i++) loneKernel.StepTick();
            Assert.That(lone.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(750L),
                "no HQ and no storage: the 1.000 start decays (excess 1.000 over ceiling 0)");
        }

        private static byte[] SerializeBlock(EconomySystem economy)
        {
            var writer = new SnapshotBlockWriter();
            economy.WriteState(writer);
            return writer.ToArray();
        }

        [Test]
        public void Block104_RoundtripsByteIdentical_AndRestoresExactState()
        {
            EntityManager entities = CreateEntities();
            var economy = new EconomySystem(entities);
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            Assert.That(economy.TryAddField(2, new GridPos2D(50, 50), 15000), Is.True);
            economy.GetPlayerEconomy(0).AddCredits(330);
            economy.GetPlayerEconomy(1).PowerProvided = 30;
            economy.GetPlayerEconomy(1).PowerRequired = 20;

            byte[] bytes = SerializeBlock(economy);

            var restored = new EconomySystem(CreateEntities());
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);
            Assert.That(SerializeBlock(restored), Is.EqualTo(bytes), "restore -> serialize must be byte-identical");

            Assert.That(restored.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1330L));
            Assert.That(restored.GetPlayerEconomy(1).PowerProvided, Is.EqualTo(30));
            Assert.That(restored.TryGetField(2, out AetheriumField field), Is.True);
            Assert.That(field.GridPos, Is.EqualTo(new GridPos2D(50, 50)));
            Assert.That(field.RemainingAE, Is.EqualTo(15000L));
        }

        [Test]
        public void Block104_RejectsNegativeCreditsReserveAndDuplicateFields_WithoutMutating()
        {
            EntityManager entities = CreateEntities();
            var economy = new EconomySystem(entities);
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            byte[] valid = SerializeBlock(economy);

            // Layout v2: version(1) + 8 slots x (i64 + i32 + i32 + u8) = 1 + 136
            // bytes of slot state, then fieldCount u16, then the field record.
            byte[] negativeCredits = (byte[])valid.Clone();
            negativeCredits[8] = 0xFF; // slot 0 credits: highest byte -> negative
            byte[] negativeReserve = (byte[])valid.Clone();
            negativeReserve[negativeReserve.Length - 1] = 0xFF; // reserve i64: highest byte -> negative

            foreach (byte[] tampered in new[] { negativeCredits, negativeReserve })
            {
                var victim = new EconomySystem(CreateEntities());
                Assert.That(victim.TryValidateState(tampered), Is.False);
                Assert.That(victim.TryRestoreState(tampered), Is.False);
                Assert.That(victim.FieldCount, Is.EqualTo(0), "a rejected restore must not mutate the system");
                Assert.That(victim.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L));
            }

            // Duplicate field ids are rejected.
            var economy2 = new EconomySystem(CreateEntities());
            Assert.That(economy2.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            Assert.That(economy2.TryAddField(2, new GridPos2D(50, 50), 15000), Is.True);
            byte[] twoFields = SerializeBlock(economy2);
            byte[] duplicate = (byte[])twoFields.Clone();
            int secondFieldIdOffset = 1 + EconomySystem.MaxPlayers * 17 + 2 + 14; // second field record starts with its id
            duplicate[secondFieldIdOffset] = 1;
            duplicate[secondFieldIdOffset + 1] = 0;
            var victim2 = new EconomySystem(CreateEntities());
            Assert.That(victim2.TryValidateState(duplicate), Is.False, "duplicate field id must fail validation");
        }

        [Test]
        public void Block104_SingleCreditChange_ChangesBlockBytes()
        {
            var economy = new EconomySystem(CreateEntities());
            byte[] before = SerializeBlock(economy);
            economy.GetPlayerEconomy(0).AddCredits(1);
            byte[] after = SerializeBlock(economy);
            Assert.That(after, Is.Not.EqualTo(before),
                "one AE of credits must move the block bytes and therefore the canonical state hash");
        }

        // ----------------------------------------------------------------
        // Faction axis (economy block v2)
        // ----------------------------------------------------------------

        [Test]
        public void SlotFaction_DefaultsToAlliance_OnEverySlot()
        {
            var economy = new EconomySystem(CreateEntities());
            for (byte slot = 0; slot < EconomySystem.MaxPlayers; slot++)
            {
                Assert.That(economy.GetSlotFaction(slot), Is.EqualTo(FactionId.Alliance));
                Assert.That(economy.GetPlayerEconomy(slot).Faction, Is.EqualTo(FactionId.Alliance));
            }
        }

        [Test]
        public void SetSlotFaction_AssignsAndReadsBack_ValidatesInput()
        {
            var economy = new EconomySystem(CreateEntities());
            economy.SetSlotFaction(0, FactionId.Alliance);
            economy.SetSlotFaction(1, FactionId.Legion);

            Assert.That(economy.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance));
            Assert.That(economy.GetSlotFaction(1), Is.EqualTo(FactionId.Legion));

            Assert.Throws<ArgumentOutOfRangeException>(() => economy.SetSlotFaction(8, FactionId.Legion));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.SetSlotFaction(0, (FactionId)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.GetSlotFaction(8));
        }

        [Test]
        public void SetSlotFaction_AfterKernelStart_ThrowsAndLeavesStateUntouched()
        {
            // The faction is part of the hashed initial state and the match
            // fingerprint: once the kernel this economy is registered with
            // has started, the assignment window is closed for good.
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);

            economy.SetSlotFaction(1, FactionId.Legion); // legal before Start
            kernel.Start();

            Assert.Throws<InvalidOperationException>(() => economy.SetSlotFaction(1, FactionId.Alliance),
                "after Start the faction is locked, even at tick zero");
            Assert.Throws<InvalidOperationException>(() => economy.SetSlotFaction(0, FactionId.Legion));
            Assert.That(economy.GetSlotFaction(1), Is.EqualTo(FactionId.Legion),
                "a rejected call mutates nothing");
            Assert.That(economy.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance));

            kernel.StepTick();
            Assert.Throws<InvalidOperationException>(() => economy.SetSlotFaction(1, FactionId.Alliance),
                "and stays locked once ticks have run");
            Assert.That(economy.GetSlotFaction(1), Is.EqualTo(FactionId.Legion));
        }

        [Test]
        public void Block104_Roundtrip_PreservesTheSlotFaction()
        {
            var economy = new EconomySystem(CreateEntities());
            economy.SetSlotFaction(1, FactionId.Legion);
            byte[] bytes = SerializeBlock(economy);

            var restored = new EconomySystem(CreateEntities());
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);
            Assert.That(restored.GetSlotFaction(1), Is.EqualTo(FactionId.Legion));
            Assert.That(restored.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance));
            Assert.That(SerializeBlock(restored), Is.EqualTo(bytes),
                "the faction byte must roundtrip byte-identical");
        }

        [Test]
        public void Block104_RejectsUndefinedFactionBytes_AndTheRetiredV1Layout()
        {
            var economy = new EconomySystem(CreateEntities());
            economy.SetSlotFaction(1, FactionId.Legion);
            byte[] valid = SerializeBlock(economy);

            // Faction byte of slot 0 sits right after its i64 + i32 + i32.
            byte[] badFaction = (byte[])valid.Clone();
            badFaction[1 + 16] = 2;
            var victim = new EconomySystem(CreateEntities());
            Assert.That(victim.TryValidateState(badFaction), Is.False, "faction 2 is not declared");
            Assert.That(victim.TryRestoreState(badFaction), Is.False);
            Assert.That(victim.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance),
                "a rejected restore must not mutate the system");

            // The retired v1 layout (no faction bytes) is rejected, not migrated.
            var writer = new SnapshotBlockWriter();
            writer.WriteUInt8(1);
            for (int p = 0; p < EconomySystem.MaxPlayers; p++)
            {
                writer.WriteInt64(1000);
                writer.WriteInt32(0);
                writer.WriteInt32(0);
            }
            writer.WriteUInt16(0);
            byte[] v1Block = writer.ToArray();
            var legacy = new EconomySystem(CreateEntities());
            Assert.That(legacy.TryValidateState(v1Block), Is.False,
                "v1 blocks predate the faction axis and are refused outright");
            Assert.That(legacy.TryRestoreState(v1Block), Is.False);
        }

        [Test]
        public void Block104_FactionChange_ChangesBlockBytes()
        {
            var economy = new EconomySystem(CreateEntities());
            byte[] before = SerializeBlock(economy);
            economy.SetSlotFaction(1, FactionId.Legion);
            byte[] after = SerializeBlock(economy);
            Assert.That(after, Is.Not.EqualTo(before),
                "the faction assignment must move the block bytes and therefore the initial state hash");
        }
    }
}
