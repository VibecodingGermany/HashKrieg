using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Canonical construction suite (EditMode lane): the MS-1 definition table,
    /// placement validation (cost, occupancy, prerequisite, power rule and
    /// the manifest start-refinery exception), builder-driven progress with
    /// the exact Q16.16 low-power halving, completion into role entities,
    /// the ResearchLab T2 unlock, cancel/sell refund rules, repair orders
    /// and the snapshot block 105 v1 contract. All values are documented
    /// Q-040 provisionals of SimDefinitions.
    /// Mirror of the .NET lane ConstructionSystemTests.
    /// </summary>
    [TestFixture]
    public class ConstructionSystemTests
    {
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
                Construction = new ConstructionSystem(Entities, Economy);
                Kernel = new SimulationKernel(new SimRandom(42UL));
                Kernel.RegisterSystem(Economy);
                Kernel.RegisterSystem(Construction);
                // Pre-start configuration hook (e.g. slot factions): the
                // SetSlotFaction guard locks the assignment at Kernel.Start().
                configure?.Invoke(Economy);
                Kernel.Start();
            }

            public EntityId SpawnBuilder(byte slot, int x, int y)
            {
                return Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                    SimFixed.FromInt(3),
                    role: UnitRole.Builder);
            }

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) Kernel.StepTick();
            }
        }

        [Test]
        public void Definitions_CoverBothFactions_WithTheDocumentedIdRule()
        {
            Assert.That(SimDefinitions.BuildingsPerFaction, Is.EqualTo(9), "nine MS-1 building roles (mvp-v1.json)");
            Assert.That(SimDefinitions.UnitsPerFaction, Is.EqualTo(8), "eight MS-1 unit roles (mvp-v1.json)");
            Assert.That(SimDefinitions.AllBuildings.Length, Is.EqualTo(18), "nine roles x two factions");
            Assert.That(SimDefinitions.AllUnits.Length, Is.EqualTo(16), "eight roles x two factions");
            Assert.That(SimDefinitions.TotalDefinitionCount, Is.EqualTo(34));

            // The id rule: Alliance ids ARE the UnitRole wire values (1..17),
            // Legion ids add 17 (18..34); every id is globally unique.
            var seen = new System.Collections.Generic.HashSet<ushort>();
            foreach (SimBuildingDefinition def in SimDefinitions.AllBuildings)
            {
                Assert.That(def.DefinitionId, Is.EqualTo(SimDefinitions.ToDefinitionId(def.Faction, def.Role)),
                    $"{def.Faction} {def.Role} id follows the documented rule");
                Assert.That(seen.Add(def.DefinitionId), Is.True, $"id {def.DefinitionId} is unique");
                Assert.That(def.CostAE, Is.GreaterThan(0));
                Assert.That(def.BuildTicks, Is.GreaterThan(0));
                Assert.That(def.MaxHealth, Is.GreaterThan(0));
                Assert.That(SimDefinitions.IsBuildingRole(def.Role), Is.True);
            }
            foreach (SimUnitDefinition def in SimDefinitions.AllUnits)
            {
                Assert.That(def.DefinitionId, Is.EqualTo(SimDefinitions.ToDefinitionId(def.Faction, def.Role)),
                    $"{def.Faction} {def.Role} id follows the documented rule");
                Assert.That(seen.Add(def.DefinitionId), Is.True, $"id {def.DefinitionId} is unique");
                Assert.That(def.CostAE, Is.GreaterThan(0));
                Assert.That(def.BuildTicks, Is.GreaterThan(0));
            }
            Assert.That(seen.Count, Is.EqualTo(34), "all 34 definition ids are globally unique");

            // Id-based lookup resolves both factions; raw 0 and anything past
            // 34 is invalid.
            Assert.That(SimDefinitions.TryGetBuilding((ushort)3, out SimBuildingDefinition hqA), Is.True);
            Assert.That(hqA.Faction, Is.EqualTo(FactionId.Alliance));
            Assert.That(SimDefinitions.TryGetBuilding((ushort)20, out SimBuildingDefinition hqL), Is.True);
            Assert.That(hqL.Faction, Is.EqualTo(FactionId.Legion));
            Assert.That(SimDefinitions.TryGetBuilding((ushort)0, out _), Is.False, "raw id 0 is invalid");
            Assert.That(SimDefinitions.TryGetBuilding((ushort)35, out _), Is.False, "raw id space ends at 34");
            Assert.That(SimDefinitions.TryGetUnit((ushort)35, out _), Is.False);

            // (faction, role) lookup: both factions resolve every role, and
            // the generic Unit role has no definition.
            foreach (FactionId faction in new[] { FactionId.Alliance, FactionId.Legion })
            {
                for (int roleIndex = (int)UnitRole.Builder; roleIndex <= (int)UnitRole.Artillery; roleIndex++)
                {
                    var role = (UnitRole)roleIndex;
                    bool isBuilding = SimDefinitions.IsBuildingRole(role);
                    Assert.That(SimDefinitions.TryGetBuilding(faction, role, out _), Is.EqualTo(isBuilding),
                        $"{faction} {role} building lookup");
                    Assert.That(SimDefinitions.TryGetUnit(faction, role, out _), Is.EqualTo(!isBuilding),
                        $"{faction} {role} unit lookup");
                }
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.Unit, out _), Is.False);
            }

            // Manifest tiers: T2 = AntiArmorInfantry, BattleTank, Artillery —
            // in BOTH factions.
            foreach (FactionId faction in new[] { FactionId.Alliance, FactionId.Legion })
            {
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.AntiArmorInfantry, out SimUnitDefinition aa) && aa.Tier == 2, Is.True);
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.BattleTank, out SimUnitDefinition bt) && bt.Tier == 2, Is.True);
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.Artillery, out SimUnitDefinition ar) && ar.Tier == 2, Is.True);
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.BasicInfantry, out SimUnitDefinition rifle) && rifle.Tier == 1, Is.True);

                // Documented producer assignment (D-077): HQ -> Builder,
                // Refinery -> Harvester, Barracks -> infantry,
                // VehicleFactory -> vehicles.
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.Builder, out SimUnitDefinition builder) && builder.ProducerRole == UnitRole.HQ, Is.True);
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.Harvester, out SimUnitDefinition harvester) && harvester.ProducerRole == UnitRole.Refinery, Is.True,
                    "the Refinery, not the HQ, produces the Harvester (D-077)");
                Assert.That(rifle.ProducerRole, Is.EqualTo(UnitRole.Barracks));
                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.ScoutVehicle, out SimUnitDefinition scout) && scout.ProducerRole == UnitRole.VehicleFactory, Is.True);
            }

            // Faction-resolved power figures (Buildings.md section 2): the
            // Alliance Power plant provides 100, the Legion one 80; the
            // Refinery's power DRAW (20/15) and its missing prerequisite are
            // faction-independent (D-077).
            Assert.That(SimDefinitions.TryGetBuilding(FactionId.Alliance, UnitRole.HQ, out SimBuildingDefinition hq) && hq.PowerProvided == 30, Is.True);
            Assert.That(SimDefinitions.TryGetBuilding(FactionId.Alliance, UnitRole.Power, out SimBuildingDefinition plant) && plant.PowerProvided == 100, Is.True);
            Assert.That(SimDefinitions.TryGetBuilding(FactionId.Legion, UnitRole.Power, out SimBuildingDefinition plantL) && plantL.PowerProvided == 80, Is.True);
            Assert.That(SimDefinitions.TryGetBuilding(FactionId.Alliance, UnitRole.Refinery, out SimBuildingDefinition refinery)
                        && refinery.PowerRequired == 20 && !refinery.HasPrerequisite, Is.True,
                "the Refinery lost its Power-plant prerequisite in both factions (D-077); its draw is unchanged");
            Assert.That(SimDefinitions.TryGetBuilding(FactionId.Legion, UnitRole.Refinery, out SimBuildingDefinition refineryL)
                        && refineryL.PowerRequired == 15 && !refineryL.HasPrerequisite, Is.True);
        }

        [Test]
        public void Definitions_LegionIsCheaperAndFaster_AllianceReachesFarther()
        {
            // The faction identity of Factions.md, pinned per role: Legion
            // costs less and builds faster, Alliance reaches at least as far.
            for (int roleIndex = (int)UnitRole.Builder; roleIndex <= (int)UnitRole.Artillery; roleIndex++)
            {
                var role = (UnitRole)roleIndex;
                if (SimDefinitions.IsBuildingRole(role))
                {
                    Assert.That(SimDefinitions.TryGetBuilding(FactionId.Alliance, role, out SimBuildingDefinition a), Is.True);
                    Assert.That(SimDefinitions.TryGetBuilding(FactionId.Legion, role, out SimBuildingDefinition l), Is.True);
                    Assert.That(l.CostAE, Is.LessThan(a.CostAE), $"{role}: Legion cost < Alliance cost");
                    Assert.That(l.BuildTicks, Is.LessThan(a.BuildTicks), $"{role}: Legion builds faster");
                    Assert.That(l.AttackRangeTiles, Is.LessThanOrEqualTo(a.AttackRangeTiles), $"{role}: Alliance range >= Legion range");
                    // Building HP is the documented integer-percent derivation.
                    Assert.That(l.MaxHealth, Is.EqualTo(a.MaxHealth * SimDefinitions.LegionHealthPercent / 100),
                        $"{role}: Legion HP == (Alliance HP x {SimDefinitions.LegionHealthPercent}) / 100, exact integer arithmetic");
                }
                else
                {
                    Assert.That(SimDefinitions.TryGetUnit(FactionId.Alliance, role, out SimUnitDefinition a), Is.True);
                    Assert.That(SimDefinitions.TryGetUnit(FactionId.Legion, role, out SimUnitDefinition l), Is.True);
                    Assert.That(l.CostAE, Is.LessThan(a.CostAE), $"{role}: Legion cost < Alliance cost");
                    Assert.That(l.BuildTicks, Is.LessThan(a.BuildTicks), $"{role}: Legion builds faster");
                    Assert.That(l.AttackRangeTiles, Is.LessThanOrEqualTo(a.AttackRangeTiles), $"{role}: Alliance range >= Legion range");
                    // The Scout's damage is the documented derivation
                    // (Weapons.md has no Legion line for it); the three combat
                    // vehicles carry the concrete Vehicles.md values (D-075,
                    // pinned by WeaponValuesTests) and infantry carries
                    // concrete Weapons.md values.
                    if (role == UnitRole.ScoutVehicle)
                    {
                        Assert.That(l.AttackDamage, Is.EqualTo(a.AttackDamage * SimDefinitions.LegionDamagePercent / 100),
                            $"{role}: Legion damage == (Alliance damage x {SimDefinitions.LegionDamagePercent}) / 100, exact integer arithmetic");
                    }
                }
            }
        }

        [Test]
        public void ValidatePlacement_ForeignFactionDefinition_IsRejectedInvalidTarget()
        {
            // Definition ids are faction-resolved: a Legion slot cannot place
            // the Alliance Barracks (id 7), an Alliance slot cannot place the
            // Legion one (id 24) — a known id naming unbuildable content is an
            // invalid target, exactly like an unknown one.
            var f = new Fixture(configure: e => e.SetSlotFaction(1, FactionId.Legion));
            Assert.That(f.Construction.PlaceCompletedBuilding(1, 22, 40, 40).IsValid, Is.True,
                "Legion power provider so the power rule does not mask the faction check");
            f.Step(1); // commit the balance
            f.SpawnBuilder(0, 19, 20);
            f.SpawnBuilder(1, 19, 20);

            Assert.That(f.Construction.ValidatePlacement(1, 7, 20, 20), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "Legion slot, Alliance Barracks");
            Assert.That(f.Construction.ValidatePlacement(0, 24, 20, 20), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "Alliance slot, Legion Barracks");
            Assert.That(f.Construction.ValidatePlacement(1, 24, 20, 20), Is.EqualTo(CommandResultCode.Applied),
                "Legion slot, Legion Barracks is legal");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0), "validation never mutates");
        }

        [Test]
        public void PlaceBuilding_LegionSlot_ChargesLegionCost_AndBuildsFaster()
        {
            var f = new Fixture(configure: e => e.SetSlotFaction(1, FactionId.Legion));
            Assert.That(f.Construction.PlaceCompletedBuilding(1, 22, 40, 40).IsValid, Is.True, "Legion power provider");
            f.SpawnBuilder(1, 19, 20);
            f.Step(1); // commit the balance (Legion Power plant provides 80)
            Assert.That(f.Economy.GetPlayerEconomy(1).PowerProvided, Is.EqualTo(80),
                "the power recompute is faction-resolved");

            Assert.That(f.Construction.TryPlaceBuilding(1, 24, 20, 20), Is.True, "Legion Barracks def 24");
            Assert.That(f.Economy.GetPlayerEconomy(1).AetheriumCredits, Is.EqualTo(600L),
                "the Legion Barracks costs exactly 400 AE (Buildings.md), not the Alliance 500");

            f.Step(140); // Legion Barracks: 140 build ticks at full power
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0), "the Legion Barracks completes after 140 ticks");
            Assert.That(f.Construction.HasFinishedBuilding(1, UnitRole.Barracks), Is.True);
            Assert.That(f.Entities.TryGetUnit(
                    UnitCommandStateView.ToEntityId(
                        RawOfCompleted(f, 1, UnitRole.Barracks)), out UnitState done), Is.True);
            Assert.That(done.MaxHealth, Is.EqualTo(510), "Legion Barracks HP: (600 x 85) / 100");
        }

        /// <summary>Raw wire id of the slot's first completed placement with the role.</summary>
        private static uint RawOfCompleted(Fixture f, byte slot, UnitRole role)
        {
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == role)
                {
                    return UnitCommandStateView.ToRawEntityId(units[i].Id);
                }
            }
            throw new System.InvalidOperationException($"no completed {role} for slot {slot}");
        }

        [Test]
        public void PlaceBuilding_ChargesExactCost_AndCreatesSiteEntity()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance (phase-2 recompute)

            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True, "Barracks def 7");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(500L),
                "Barracks costs exactly 500 AE (provisional)");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(1));

            // The site entity sits at the footprint center with role Unit and 1 HP.
            bool found = false;
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (!units[i].IsActive || units[i].Role != UnitRole.Unit) continue;
                found = true;
                Assert.That(units[i].Transform.PositionX, Is.EqualTo(SimFixed.FromInt(21)));
                Assert.That(units[i].Transform.PositionY, Is.EqualTo(SimFixed.FromInt(21)));
                Assert.That(units[i].CurrentHealth, Is.EqualTo(1), "site HP stays 1 until completion (provisional)");
                Assert.That(units[i].MaxHealth, Is.EqualTo(600));
            }
            Assert.That(found, Is.True, "a site entity must exist");
        }

        [Test]
        public void PlaceBuilding_InsufficientFunds_FailsAndMutatesNothing()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);

            Assert.That(f.Construction.TryPlaceBuilding(0, 3, 20, 20), Is.False, "HQ costs 2500 (Buildings.md), balance is 1000");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L), "a refused placement mutates nothing");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0));
            Assert.That(f.Construction.IsCellFree(20, 20), Is.True);
        }

        [Test]
        public void PlaceBuilding_OccupiedOrOutOfMap_IsRejectedInvalidTarget()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 20, 20).IsValid, Is.True, "Power plant at (20,20)");
            f.Step(1); // commit the balance

            Assert.That(f.Construction.ValidatePlacement(0, 6, 21, 21), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the 3x3 footprints overlap");
            Assert.That(f.Construction.ValidatePlacement(0, 6, 24, 24), Is.EqualTo(CommandResultCode.Applied),
                "a separate location stays placeable");
            Assert.That(f.Construction.ValidatePlacement(0, 6, 126, 126), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the footprint must fit the 128x128 grid");
            Assert.That(f.Construction.ValidatePlacement(0, 99, 30, 30), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "an unknown definition id is an invalid target, not a cost failure");
        }

        [Test]
        public void PlaceBuilding_MissingPrerequisite_IsRejectedPrerequisitesNotMet()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);

            Assert.That(f.Construction.ValidatePlacement(0, 11, 20, 20), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                "a DefensePlatform requires a completed own Power plant");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True);
            f.Step(1); // commit the balance (100 provided)
            Assert.That(f.Construction.ValidatePlacement(0, 11, 20, 20), Is.EqualTo(CommandResultCode.Applied));
        }

        [Test]
        public void PlaceBuilding_PowerRule_RequiresSufficientFreePower()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);
            // Committed balance: HQ 30 provided, Refinery 20 required -> 10 free.
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 44, 40).IsValid, Is.True);
            f.Step(1); // let the economy recompute the balance

            Assert.That(f.Construction.ValidatePlacement(0, 8, 20, 20), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                "VehicleFactory draws 25 but only 10 are free");
            Assert.That(f.Construction.ValidatePlacement(0, 6, 20, 20), Is.EqualTo(CommandResultCode.Applied),
                "Storage draws 5 of the 10 free power");
            Assert.That(f.Construction.ValidatePlacement(0, 5, 60, 60), Is.EqualTo(CommandResultCode.Applied),
                "power-providing buildings are exempt from the rule");
        }

        [Test]
        public void RefineryPlacement_NeedsNoPowerPlant_TheCommandPathEnforcesOnlyThePowerRule()
        {
            var f = new Fixture();
            // D-077: the Refinery lost its Power-plant prerequisite in both
            // factions. With a completed HQ (30 provided, covering the 20
            // draw) the command path accepts it directly — the classic loop
            // start needs no Power plant first.
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True, "HQ provides 30");
            f.Step(1); // commit the balance
            Assert.That(f.Construction.ValidatePlacement(0, 4, 20, 20), Is.EqualTo(CommandResultCode.Applied),
                "no Power plant required (D-077)");

            // PlaceCompletedBuilding still bypasses the power rule: a second
            // Refinery placed completed draws into the grid unchecked, ...
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 24, 24).IsValid, Is.True);
            f.Step(1); // commit 30 provided / 20 required
            // ... while the command path keeps enforcing it: the 10 free
            // power cannot cover a third Refinery's 20.
            Assert.That(f.Construction.ValidatePlacement(0, 4, 30, 30), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
        }

        [Test]
        public void SiteProgress_RequiresBuilderInReach_PausesWhenAway()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            EntityId builder = f.SpawnBuilder(0, 60, 60); // far away
            f.Step(1); // commit the balance
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True);

            f.Step(20);
            Assert.That(f.Construction.TryGetSite(UnitCommandStateView.ToRawEntityId(SiteEntity(f)), out _, out int progressRaw, out uint assigned),
                Is.True);
            Assert.That(progressRaw, Is.EqualTo(0), "no builder in reach: the site pauses");
            Assert.That(assigned, Is.EqualTo(UnitCommandStateView.ToRawEntityId(builder)),
                "the lowest-index own Builder is auto-assigned at placement");

            // Bring the builder into reach (Chebyshev <= 1 of the footprint).
            f.Entities.GetUnitRef(builder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
            f.Step(10);
            Assert.That(f.Construction.TryGetSite(UnitCommandStateView.ToRawEntityId(SiteEntity(f)), out _, out progressRaw, out _),
                Is.True);
            Assert.That(progressRaw, Is.EqualTo(10 * SimFixed.OneRaw),
                "full power: exactly one Q16.16 tick of progress per tick");
        }

        [Test]
        public void SiteProgress_LowPower_ExactlyHalvesProgress()
        {
            var f = new Fixture();
            // Low power: a completed Refinery draws 20 with nothing provided.
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 40, 40).IsValid, Is.True);
            f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True, "Power plant def 5, 150 ticks");
            f.Step(1);
            Assert.That(f.Economy.GetPlayerEconomy(0).IsLowPower, Is.True);

            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));
            f.Step(9);
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out int progressRaw, out _), Is.True);
            Assert.That(progressRaw, Is.EqualTo(10 * (SimFixed.OneRaw / 2)),
                "low power: exactly 0.5 in Q16.16 per tick — no rounding drift");

            f.Step(279); // 289 ticks total: still short of 150 effective
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out progressRaw, out _), Is.True);
            Assert.That(progressRaw, Is.EqualTo(289 * (SimFixed.OneRaw / 2)));
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Unit));

            f.Step(11); // 300 ticks = exactly 150 effective ticks
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Power),
                "the plant completes after exactly 300 low-power ticks");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0));
            Assert.That(f.Construction.BuildingCount, Is.EqualTo(2));
        }

        [Test]
        public void Completion_BecomesRoleEntity_PowerAppliesFromNextTick()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            f.Step(149);
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Unit));
            f.Step(1); // tick 150: completion in phase 4
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Power));
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).CurrentHealth, Is.EqualTo(400),
                "completion restores full HP");
            Assert.That(f.Economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(0),
                "the economy ran before construction inside the completion tick");
            f.Step(1);
            Assert.That(f.Economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(100),
                "power applies from the next economy recompute on");
        }

        [Test]
        public void ResearchLabCompletion_UnlocksT2()
        {
            var f = new Fixture(startingCredits: 3000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True); // power
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 7, 44, 40).IsValid, Is.True); // barracks prerequisite
            f.SpawnBuilder(0, 19, 20);
            f.Step(1);

            Assert.That(f.Construction.IsT2Unlocked(0), Is.False);
            Assert.That(f.Construction.TryPlaceBuilding(0, 9, 20, 20), Is.True, "ResearchLab def 9");
            f.Step(450);
            Assert.That(f.Construction.IsT2Unlocked(0), Is.True,
                "ResearchLab completion unlocks T2 immediately (mvp-v1.json technology model)");
            Assert.That(f.Construction.IsT2Unlocked(1), Is.False, "the unlock is per slot");
        }

        [Test]
        public void PlaceCompletedBuilding_ResearchLab_UnlocksT2Immediately()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 9, 20, 20).IsValid, Is.True);
            Assert.That(f.Construction.IsT2Unlocked(0), Is.True);
        }

        private static int CountUnits(Fixture f, byte slot, UnitRole role)
        {
            UnitState[] units = f.Entities.RawUnits;
            int count = 0;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == role) count++;
            }
            return count;
        }

        [Test]
        public void RefineryCompletion_GrantsTheFirstHarvesterFree()
        {
            // The dead end this closes: the Harvester costs 700 AE and the
            // Refinery is its only producer since D-077. A player who spends
            // down below 700 before the Refinery finishes can never earn
            // again — no Harvester, no Aetherium, no money for a Harvester.
            var f = new Fixture(startingCredits: 1000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True,
                "HQ provides the 30 power the Refinery draws from");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance

            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 20, 20), Is.True, "Refinery def 4 (Alliance, 700 AE, 200 ticks)");
            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(0), "none before completion");

            f.Step(150);
            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(0), "not while the site is still running");
            long creditsBefore = f.Economy.GetPlayerEconomy(0).AetheriumCredits;

            f.Step(100); // past the 200-tick build time

            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(1),
                "a finished Refinery hands out its first Harvester");
            Assert.That(CountUnits(f, 1, UnitRole.Harvester), Is.EqualTo(0),
                "the grant belongs to the building's owner alone");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(creditsBefore),
                "the grant is free: nothing is charged at completion");
        }

        [Test]
        public void RefineryCompletion_GrantedHarvester_StartsWithNearestFieldOrder()
        {
            // #43: the granted Harvester is born with a standing harvest
            // order on the NEAREST field with reserve left — measured from
            // the Refinery's footprint centre, ties resolved by index.
            var f = new Fixture(startingCredits: 1000, configure: eco =>
            {
                Assert.That(eco.TryAddField(1, new GridPos2D(30, 30), 9000), Is.True);
                Assert.That(eco.TryAddField(2, new GridPos2D(60, 60), 9000), Is.True);
            });
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True, "HQ power");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1);

            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 20, 20), Is.True, "Refinery def 4");
            f.Step(250);

            Assert.That(TryFindHarvester(f, 0, out UnitState harvester), Is.True, "the grant happened");
            Assert.That(harvester.HarvestFieldId, Is.EqualTo(1),
                "field 1 at (30,30) is closer to the footprint centre (21,21) than field 2 at (60,60)");

            f.Step(50);
            Assert.That(TryFindHarvester(f, 0, out harvester), Is.True);
            Assert.That(harvester.HarvestFieldId, Is.EqualTo(1),
                "the standing order is held, not dropped, while the field is out of reach");
        }

        [Test]
        public void RefineryCompletion_WithoutFields_GrantedHarvesterCarriesNoOrder()
        {
            var f = new Fixture(startingCredits: 1000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True);
            f.SpawnBuilder(0, 19, 20);
            f.Step(1);

            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 20, 20), Is.True);
            f.Step(250);

            Assert.That(TryFindHarvester(f, 0, out UnitState harvester), Is.True);
            Assert.That(harvester.HarvestFieldId, Is.EqualTo(0),
                "no field registered: the grant still happens, only the order is skipped");
        }

        [Test]
        public void RefineryCompletion_SecondRefinery_GrantsNothingWhileAHarvesterLives()
        {
            // #43 latch: the grant is derived from the unit store — a second
            // Refinery (or a rebuild) grants nothing while any own Harvester
            // lives. Before 16.1 EVERY completed Refinery handed one out.
            var f = new Fixture(startingCredits: 3000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True, "HQ");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 44, 40).IsValid, Is.True,
                "power plant: two Refineries overdraw the HQ's 30 alone");
            EntityId builderOne = f.SpawnBuilder(0, 19, 20);
            f.Step(1);

            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 20, 20), Is.True, "first Refinery");
            f.Step(250);
            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(1), "the first grant");

            // The site auto-assigns the lowest-index own Builder, so the
            // second site needs the only living builder in ITS reach.
            Assert.That(f.Entities.DespawnUnit(builderOne), Is.True);
            f.SpawnBuilder(0, 25, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 26, 20), Is.True, "second Refinery");
            f.Step(250);

            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(1),
                "latched: the second Refinery grants nothing while the first Harvester lives");
        }

        [Test]
        public void RefineryCompletion_AfterLosingEveryHarvester_TheGrantReArms()
        {
            // The latch is the dead-end insurance, not a once-per-match
            // counter: with every Harvester lost the next completed Refinery
            // grants again.
            var f = new Fixture(startingCredits: 3000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 44, 40).IsValid, Is.True);
            EntityId builderOne = f.SpawnBuilder(0, 19, 20);
            f.Step(1);

            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 20, 20), Is.True);
            f.Step(250);
            Assert.That(TryFindHarvester(f, 0, out UnitState harvester), Is.True);

            Assert.That(f.Entities.DespawnUnit(harvester.Id), Is.True, "every Harvester lost");
            Assert.That(f.Entities.DespawnUnit(builderOne), Is.True);
            f.SpawnBuilder(0, 25, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 4, 26, 20), Is.True, "second Refinery");
            f.Step(250);

            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(1),
                "no living Harvester: the grant fires again");
        }

        private static bool TryFindHarvester(Fixture f, byte slot, out UnitState harvester)
        {
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == UnitRole.Harvester)
                {
                    harvester = units[i];
                    return true;
                }
            }
            harvester = default;
            return false;
        }

        [Test]
        public void PlaceCompletedBuilding_Refinery_GrantsNothing_MatchStartIsUnchanged()
        {
            // PlaceCompletedBuilding is the match-start path (starting HQ plus
            // Refinery). The grant deliberately hangs on finishing a site, not
            // on instant placement — otherwise every match would begin with a
            // free Harvester, which is a balance change nobody asked for.
            var f = new Fixture(startingCredits: 3000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 20, 20).IsValid, Is.True);

            Assert.That(CountUnits(f, 0, UnitRole.Harvester), Is.EqualTo(0),
                "an instantly placed Refinery grants nothing");
        }

        [Test]
        public void CancelConstruction_Refunds75Percent_AndFreesFootprint()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True); // 500 spent
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            Assert.That(f.Construction.ValidateCancel(0, siteRaw), Is.EqualTo(CommandResultCode.Applied));
            Assert.That(f.Construction.ValidateCancel(1, siteRaw), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "only the owning slot may cancel");

            Assert.That(f.Construction.CancelConstruction(siteRaw), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(875L),
                "1000 - 500 + 375 (75% floor, provisional)");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0));
            Assert.That(f.Entities.IsValid(UnitCommandStateView.ToEntityId(siteRaw)), Is.False, "the site entity despawns");
            Assert.That(f.Construction.ValidatePlacement(0, 7, 20, 20), Is.EqualTo(CommandResultCode.Applied),
                "the footprint is free again");
        }

        [Test]
        public void Sell_CompletedBuilding_Refunds50Percent_SiteIsNotSellable()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 7, 20, 20);
            uint raw = UnitCommandStateView.ToRawEntityId(barracks);

            Assert.That(f.Construction.SellBuilding(raw), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1250L),
                "1000 + 250 (50% floor, provisional)");
            Assert.That(f.Construction.BuildingCount, Is.EqualTo(1), "only the Barracks was sold");
            Assert.That(f.Construction.IsCellFree(20, 20), Is.True);

            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance (100 provided, 0 required)
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));
            Assert.That(f.Construction.ValidateSell(0, siteRaw), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "a site is cancelled, not sold");
        }

        [Test]
        public void Repair_BuilderRestoresHp_InReachOnly_AndResolvesAtFull()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True,
                "a completed power plant keeps the normal repair rate active");
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 7, 20, 20);
            uint raw = UnitCommandStateView.ToRawEntityId(barracks);
            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;

            EntityId farBuilder = f.SpawnBuilder(0, 60, 60);
            uint farRaw = UnitCommandStateView.ToRawEntityId(farBuilder);
            Assert.That(f.Construction.ValidateRepair(0, new[] { farRaw }, raw), Is.EqualTo(CommandResultCode.Applied),
                "validation checks role and damage, not reach");
            f.Construction.AssignRepairOrder(farRaw, raw);
            f.Step(10);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(100),
                "out of reach: the order is held, not dropped");

            f.Entities.GetUnitRef(farBuilder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
            f.Step(10);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(200),
                "10 HP per tick in reach (provisional rate)");
            f.Step(50);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(600),
                "repair caps at MaxHealth and the order resolves");
        }

        [Test]
        public void Repair_LowPower_ExactlyHalvesTheRate()
        {
            // 16.6 (C4, Economy.md repair rule): under LOW POWER the repair
            // rate halves exactly — 5 HP per tick, no rounding.
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 40, 40).IsValid, Is.True,
                "a completed Refinery (20 required, nothing provided) forces low power");
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 7, 20, 20);
            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;

            EntityId builder = f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance: refinery + barracks draw 35, nothing provided
            Assert.That(f.Economy.GetPlayerEconomy(0).IsLowPower, Is.True);

            f.Construction.AssignRepairOrder(UnitCommandStateView.ToRawEntityId(builder), UnitCommandStateView.ToRawEntityId(barracks));
            f.Step(10);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(150),
                "5 HP per tick under low power — exactly half the provisional rate");
        }

        [Test]
        public void Repair_Validation_RejectsNonBuilder_AndUndamagedTarget()
        {
            var f = new Fixture();
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 7, 20, 20);
            uint raw = UnitCommandStateView.ToRawEntityId(barracks);
            EntityId builder = f.SpawnBuilder(0, 19, 20);
            uint builderRaw = UnitCommandStateView.ToRawEntityId(builder);
            EntityId soldier = f.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(21)), SimFixed.FromInt(4), role: UnitRole.BasicInfantry);
            uint soldierRaw = UnitCommandStateView.ToRawEntityId(soldier);

            Assert.That(f.Construction.ValidateRepair(0, new[] { builderRaw }, raw),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "the target is undamaged");

            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;
            Assert.That(f.Construction.ValidateRepair(0, new[] { soldierRaw }, raw),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "only Builders repair");
            Assert.That(f.Construction.ValidateRepair(1, new[] { builderRaw }, raw),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "the builder belongs to slot 0");
        }

        [Test]
        public void DestroyedSite_AbortsWithoutRefund_AndFreesFootprint()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            Assert.That(f.Entities.DespawnUnit(UnitCommandStateView.ToEntityId(siteRaw)), Is.True,
                "combat-style kill of the site entity");
            f.Step(1);
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0), "the sweep aborts the dead site");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(500L), "no refund for a destroyed site");
            Assert.That(f.Construction.IsCellFree(20, 20), Is.True);
        }

        [Test]
        public void Snapshot_Roundtrip_IsByteIdentical_AndTamperingIsRejected()
        {
            var f = new Fixture();
            EntityId builder = f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 7, 30, 30);
            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;
            f.Construction.AssignRepairOrder(UnitCommandStateView.ToRawEntityId(builder), UnitCommandStateView.ToRawEntityId(barracks));
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 9, 40, 40).IsValid, Is.True); // T2 flag
            f.Step(10); // accumulate some site progress

            var writer = new SnapshotBlockWriter();
            f.Construction.WriteState(writer);
            byte[] bytes = writer.ToArray();

            var restored = new ConstructionSystem(new EntityManager(64), new EconomySystem(new EntityManager(64)));
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);

            var rewritten = new SnapshotBlockWriter();
            restored.WriteState(rewritten);
            Assert.That(rewritten.ToArray(), Is.EqualTo(bytes), "serialize -> restore -> serialize is byte-identical");

            // Tampering: unknown definition id.
            byte[] tampered = (byte[])bytes.Clone();
            tampered[2 + 2] = 200; // first site's defId low byte (version, t2, count16, then defId)
            Assert.That(restored.TryValidateState(tampered), Is.False);

            // Trailing bytes are a parse failure.
            var longer = new byte[bytes.Length + 1];
            System.Array.Copy(bytes, longer, bytes.Length);
            Assert.That(restored.TryValidateState(longer), Is.False);
        }

        [Test]
        public void Snapshot_AssignedBuilderRoleViolation_IsRejectedWithoutMutation()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            EntityId soldier = f.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(50), SimFixed.FromInt(50)), SimFixed.FromInt(4),
                role: UnitRole.BasicInfantry);
            f.Step(1);
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True);

            var writer = new SnapshotBlockWriter();
            f.Construction.WriteState(writer);
            byte[] bytes = writer.ToArray();
            Assert.That(f.Construction.TryValidateState(bytes), Is.True, "the untampered block validates");

            // Tamper: replace the site's assigned builder with the combat
            // unit (offset: version 1 + t2 1 + siteCount 2 + defId 2 +
            // originX 2 + originY 2 + siteEntity 4 = 14, LE uint32).
            byte[] tampered = (byte[])bytes.Clone();
            uint soldierRaw = UnitCommandStateView.ToRawEntityId(soldier);
            tampered[14] = (byte)(soldierRaw & 0xFF);
            tampered[15] = (byte)((soldierRaw >> 8) & 0xFF);
            tampered[16] = (byte)((soldierRaw >> 16) & 0xFF);
            tampered[17] = (byte)((soldierRaw >> 24) & 0xFF);

            Assert.That(f.Construction.TryValidateState(tampered), Is.False,
                "P2-2: a combat unit as assigned builder rejects the block");
            Assert.That(f.Construction.TryRestoreState(tampered), Is.False,
                "restore refuses the tampered block");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(1), "the host is unchanged");
            Assert.That(f.Construction.TryGetSite(
                UnitCommandStateView.ToRawEntityId(SiteEntity(f)), out _, out _, out uint assigned), Is.True);
            Assert.That(assigned, Is.Not.EqualTo(soldierRaw), "the live assignment is unchanged");
        }

        [Test]
        public void ProgressSites_ReassignsNonBuilderAssignment_DefenseInDepth()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True, "power provider");
            EntityId builder = f.SpawnBuilder(0, 19, 20);
            f.Step(1);
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            f.Step(10);
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out int progressRaw, out uint assigned), Is.True);
            Assert.That(progressRaw, Is.EqualTo(10 * SimFixed.OneRaw));
            Assert.That(assigned, Is.EqualTo(UnitCommandStateView.ToRawEntityId(builder)));

            // Defense-in-depth (P2-2): an assignment that no longer names a
            // Builder (here: direct role mutation, standing in for a tampered
            // or stale reference) is dropped and re-resolved like a dead
            // builder — the site pauses instead of letting a combat unit build.
            f.Entities.GetUnitRef(builder).Role = UnitRole.BasicInfantry;
            f.Step(5);
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out int pausedProgress, out assigned), Is.True);
            Assert.That(assigned, Is.EqualTo(0u), "no own Builder exists to re-assign");
            Assert.That(pausedProgress, Is.EqualTo(10 * SimFixed.OneRaw),
                "the site pauses — the non-builder never progressed it");
        }

        /// <summary>Returns the single active site entity of the fixture.</summary>
        private static EntityId SiteEntity(Fixture f)
        {
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == UnitRole.Unit)
                {
                    return units[i].Id;
                }
            }
            throw new System.InvalidOperationException("no site entity found");
        }
    }
}
