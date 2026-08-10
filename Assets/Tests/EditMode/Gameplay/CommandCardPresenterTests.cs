using NUnit.Framework;
using Nova.Core;
using Nova.Gameplay;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Production;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for the role-aware command-card mapping of
    /// <see cref="CommandCardPresenter"/>: which buttons a selection shows
    /// (presence), the blocker evaluation priorities that grey them
    /// (availability — documented separately for each surface),
    /// the producer/content listing and the building-side repair-actor
    /// convention. The legacy count-only overload is pinned by
    /// SelectionManagerTests and deliberately not duplicated here.
    /// </summary>
    [TestFixture]
    public class CommandCardPresenterTests
    {
        // ----------------------------------------------------------------
        // Unit card presence
        // ----------------------------------------------------------------

        [Test]
        public void GetUnitCommands_ArmedCombatUnit_OffersMoveStopAttackOnly()
        {
            var presenter = new CommandCardPresenter();

            CommandButtonType commands = presenter.GetUnitCommands(FactionId.Alliance, UnitRole.BasicInfantry);

            Assert.IsTrue(commands.HasFlag(CommandButtonType.Move));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Stop));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Attack));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.Harvest));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.ReturnCargo));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.Repair));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.Sell));
        }

        [Test]
        public void GetUnitCommands_Harvester_OffersHarvestAndReturnCargoButNoAttack()
        {
            var presenter = new CommandCardPresenter();

            // Both factions: the Harvester is unarmed (AttackDamage 0) in
            // both definition rows, so an Attack button would be a lie.
            foreach (FactionId faction in new[] { FactionId.Alliance, FactionId.Legion })
            {
                CommandButtonType commands = presenter.GetUnitCommands(faction, UnitRole.Harvester);
                Assert.IsTrue(commands.HasFlag(CommandButtonType.Move), faction.ToString());
                Assert.IsTrue(commands.HasFlag(CommandButtonType.Stop), faction.ToString());
                Assert.IsTrue(commands.HasFlag(CommandButtonType.Harvest), faction.ToString());
                Assert.IsTrue(commands.HasFlag(CommandButtonType.ReturnCargo), faction.ToString());
                Assert.IsFalse(commands.HasFlag(CommandButtonType.Attack), faction.ToString());
                Assert.IsFalse(commands.HasFlag(CommandButtonType.Repair), faction.ToString());
            }
        }

        [Test]
        public void GetUnitCommands_Builder_OffersRepairButNoAttack()
        {
            var presenter = new CommandCardPresenter();

            CommandButtonType commands = presenter.GetUnitCommands(FactionId.Legion, UnitRole.Builder);

            Assert.IsTrue(commands.HasFlag(CommandButtonType.Move));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Stop));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Repair));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.Attack));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.Harvest));
        }

        [Test]
        public void GetUnitCommands_BuildingRole_ReturnsNone()
        {
            var presenter = new CommandCardPresenter();

            Assert.AreEqual(CommandButtonType.None, presenter.GetUnitCommands(FactionId.Alliance, UnitRole.HQ));
            Assert.AreEqual(CommandButtonType.None, presenter.GetUnitCommands(FactionId.Alliance, UnitRole.Barracks));
        }

        [Test]
        public void GetUnitCommands_GenericUnitRole_TreatedAsUnarmed()
        {
            var presenter = new CommandCardPresenter();

            // UnitRole.Unit has no definition row, so there is nothing to
            // prove it armed with: Move/Stop only.
            CommandButtonType commands = presenter.GetUnitCommands(FactionId.Alliance, UnitRole.Unit);

            Assert.AreEqual(CommandButtonType.Move | CommandButtonType.Stop, commands);
        }

        // ----------------------------------------------------------------
        // Building and site card presence
        // ----------------------------------------------------------------

        [Test]
        public void GetSiteCommands_OffersCancelConstructionOnly()
        {
            var presenter = new CommandCardPresenter();

            Assert.AreEqual(CommandButtonType.CancelConstruction, presenter.GetSiteCommands());
        }

        [Test]
        public void GetBuildingCommands_AnyBuilding_OffersSellAndRepair()
        {
            var presenter = new CommandCardPresenter();

            CommandButtonType commands = presenter.GetBuildingCommands(UnitRole.Barracks);

            Assert.IsTrue(commands.HasFlag(CommandButtonType.Sell));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Repair));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.InstallDefenseModule));
            Assert.IsFalse(commands.HasFlag(CommandButtonType.Move), "buildings are immobile");
        }

        [Test]
        public void GetBuildingCommands_DefensePlatform_ShowsModuleButtonAsPresent()
        {
            var presenter = new CommandCardPresenter();

            // Presence only: the HUD renders this button DISABLED with the
            // G2/G4 reason — the sim rejects the kind in this slice, so it
            // must never be dispatched, but hiding it would pretend the
            // platform has no module slot at all.
            CommandButtonType commands = presenter.GetBuildingCommands(UnitRole.DefensePlatform);

            Assert.IsTrue(commands.HasFlag(CommandButtonType.InstallDefenseModule));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Sell));
            Assert.IsTrue(commands.HasFlag(CommandButtonType.Repair));
        }

        [Test]
        public void GetBuildingCommands_UnitRole_ReturnsNone()
        {
            var presenter = new CommandCardPresenter();

            Assert.AreEqual(CommandButtonType.None, presenter.GetBuildingCommands(UnitRole.Harvester));
        }

        // ----------------------------------------------------------------
        // Production listing
        // ----------------------------------------------------------------

        private static UnitRole[] ProducedRoles(FactionId faction, UnitRole producerRole)
        {
            var presenter = new CommandCardPresenter();
            var buffer = new SimUnitDefinition[SimDefinitions.UnitsPerFaction];
            int count = presenter.GetProducibleUnits(faction, producerRole, buffer);
            var roles = new UnitRole[count];
            for (int i = 0; i < count; i++)
            {
                roles[i] = buffer[i].Role;
            }
            return roles;
        }

        [Test]
        public void GetProducibleUnits_MatchesD077ProducerAssignment()
        {
            // D-077: the HQ produces the Builder, the REFINERY the Harvester,
            // the Barracks both infantry roles, the VehicleFactory all four
            // vehicle roles.
            CollectionAssert.AreEqual(new[] { UnitRole.Builder }, ProducedRoles(FactionId.Alliance, UnitRole.HQ));
            CollectionAssert.AreEqual(new[] { UnitRole.Harvester }, ProducedRoles(FactionId.Alliance, UnitRole.Refinery));
            CollectionAssert.AreEqual(
                new[] { UnitRole.BasicInfantry, UnitRole.AntiArmorInfantry },
                ProducedRoles(FactionId.Alliance, UnitRole.Barracks));
            CollectionAssert.AreEqual(
                new[] { UnitRole.ScoutVehicle, UnitRole.LightTank, UnitRole.BattleTank, UnitRole.Artillery },
                ProducedRoles(FactionId.Alliance, UnitRole.VehicleFactory));

            Assert.IsEmpty(ProducedRoles(FactionId.Alliance, UnitRole.Power), "a non-producer has no buttons");
        }

        [Test]
        public void GetProducibleUnits_FiltersByFaction()
        {
            var presenter = new CommandCardPresenter();
            var buffer = new SimUnitDefinition[SimDefinitions.UnitsPerFaction];

            int count = presenter.GetProducibleUnits(FactionId.Legion, UnitRole.HQ, buffer);

            Assert.AreEqual(1, count);
            Assert.AreEqual(FactionId.Legion, buffer[0].Faction);
            Assert.AreEqual(UnitRole.Builder, buffer[0].Role);
            Assert.AreEqual(18, buffer[0].DefinitionId, "the Legion Builder is the faction-offset row, not the Alliance one");
        }

        // ----------------------------------------------------------------
        // Availability evaluation (documented per-surface priorities)
        // ----------------------------------------------------------------

        [Test]
        public void EvaluateProductionBlocker_TierLockWinsOverQueueAndCredits()
        {
            Assert.IsTrue(SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.BattleTank, out SimUnitDefinition t2));

            // All three blockers apply at once; the executor checks the T2
            // gate first, so that is the reason the button must show.
            ProductionBlocker blocker = CommandCardPresenter.EvaluateProductionBlocker(
                in t2, credits: 0, t2Unlocked: false, queueEntryCount: ProductionSystem.MaxQueueEntries);

            Assert.AreEqual(ProductionBlocker.TierLocked, blocker);
        }

        [Test]
        public void EvaluateProductionBlocker_QueueFullWinsOverCredits()
        {
            Assert.IsTrue(SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.BasicInfantry, out SimUnitDefinition t1));

            ProductionBlocker blocker = CommandCardPresenter.EvaluateProductionBlocker(
                in t1, credits: 0, t2Unlocked: false, queueEntryCount: ProductionSystem.MaxQueueEntries);

            Assert.AreEqual(ProductionBlocker.QueueFull, blocker);
        }

        [Test]
        public void EvaluateProductionBlocker_InsufficientCreditsAndNone()
        {
            Assert.IsTrue(SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.BasicInfantry, out SimUnitDefinition t1));

            Assert.AreEqual(
                ProductionBlocker.InsufficientCredits,
                CommandCardPresenter.EvaluateProductionBlocker(in t1, credits: t1.CostAE - 1, t2Unlocked: true, queueEntryCount: 0));
            Assert.AreEqual(
                ProductionBlocker.None,
                CommandCardPresenter.EvaluateProductionBlocker(in t1, credits: t1.CostAE, t2Unlocked: false, queueEntryCount: 0),
                "a tier-1 unit needs no T2 unlock");

            Assert.IsTrue(SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.Artillery, out SimUnitDefinition t2));
            Assert.AreEqual(
                ProductionBlocker.None,
                CommandCardPresenter.EvaluateProductionBlocker(in t2, credits: t2.CostAE, t2Unlocked: true, queueEntryCount: 0),
                "unlocked, funded and queue space: the executor would apply");
        }

        [Test]
        public void EvaluateBuildingPlacementBlocker_FollowsBuildMenuPriority()
        {
            Assert.IsTrue(SimDefinitions.TryGetBuilding(
                FactionId.Alliance, UnitRole.VehicleFactory, out SimBuildingDefinition factory));

            Assert.AreEqual(
                BuildingPlacementBlocker.MissingPrerequisite,
                CommandCardPresenter.EvaluateBuildingPlacementBlocker(
                    in factory, prerequisiteMet: false, credits: 0,
                    powerProvided: 0, powerRequired: 0, activeSiteCount: ConstructionSystem.MaxSites),
                "the build menu prioritizes the actionable prerequisite chain when every blocker applies");
            Assert.AreEqual(
                BuildingPlacementBlocker.InsufficientCredits,
                CommandCardPresenter.EvaluateBuildingPlacementBlocker(
                    in factory, prerequisiteMet: true, credits: factory.CostAE - 1,
                    powerProvided: 0, powerRequired: 0, activeSiteCount: ConstructionSystem.MaxSites),
                "affordability wins over power and capacity");
            Assert.AreEqual(
                BuildingPlacementBlocker.InsufficientPower,
                CommandCardPresenter.EvaluateBuildingPlacementBlocker(
                    in factory, prerequisiteMet: true, credits: factory.CostAE,
                    powerProvided: factory.PowerRequired - 1, powerRequired: 0,
                    activeSiteCount: ConstructionSystem.MaxSites),
                "power wins over site capacity");
            Assert.AreEqual(
                BuildingPlacementBlocker.SiteCapacityReached,
                CommandCardPresenter.EvaluateBuildingPlacementBlocker(
                    in factory, prerequisiteMet: true, credits: factory.CostAE,
                    powerProvided: factory.PowerRequired, powerRequired: 0,
                    activeSiteCount: ConstructionSystem.MaxSites),
                "free power equal to the draw is sufficient, exposing the later capacity blocker");
            Assert.AreEqual(
                BuildingPlacementBlocker.None,
                CommandCardPresenter.EvaluateBuildingPlacementBlocker(
                    in factory, prerequisiteMet: true, credits: factory.CostAE,
                    powerProvided: factory.PowerRequired, powerRequired: 0,
                    activeSiteCount: ConstructionSystem.MaxSites - 1));
        }

        [Test]
        public void EvaluateBuildingPlacementBlocker_ZeroDrawNeverEnergyBlocks()
        {
            Assert.IsTrue(SimDefinitions.TryGetBuilding(
                FactionId.Alliance, UnitRole.Power, out SimBuildingDefinition powerPlant));

            Assert.AreEqual(
                BuildingPlacementBlocker.None,
                CommandCardPresenter.EvaluateBuildingPlacementBlocker(
                    in powerPlant, prerequisiteMet: true, credits: powerPlant.CostAE,
                    powerProvided: 0, powerRequired: 100, activeSiteCount: 0));
        }

        [Test]
        public void PowerFormatters_NameBalanceConsequenceAndBuildingDraw()
        {
            Assert.AreEqual("Strom 30/20", CommandCardPresenter.FormatPowerBalance(30, 20));
            Assert.AreEqual(
                "Strom 20/40 · LOW POWER: Produktion ½",
                CommandCardPresenter.FormatPowerBalance(20, 40));

            Assert.IsTrue(SimDefinitions.TryGetBuilding(
                FactionId.Alliance, UnitRole.Power, out SimBuildingDefinition powerPlant));
            Assert.IsTrue(SimDefinitions.TryGetBuilding(
                FactionId.Alliance, UnitRole.Refinery, out SimBuildingDefinition refinery));
            Assert.AreEqual("Erzeugt +100 Strom", CommandCardPresenter.FormatBuildingPower(in powerPlant));
            Assert.AreEqual("Benötigt 20 Strom", CommandCardPresenter.FormatBuildingPower(in refinery));
        }

        [Test]
        public void EvaluateBuildingRepairBlocker_FollowsTheSimRepairRule()
        {
            Assert.AreEqual(
                BuildingRepairBlocker.NotDamaged,
                CommandCardPresenter.EvaluateBuildingRepairBlocker(isDamaged: false, builderAvailable: true),
                "the executor rejects repair on a full-HP placement as an invalid target");
            Assert.AreEqual(
                BuildingRepairBlocker.NoBuilderAvailable,
                CommandCardPresenter.EvaluateBuildingRepairBlocker(isDamaged: true, builderAvailable: false));
            Assert.AreEqual(
                BuildingRepairBlocker.None,
                CommandCardPresenter.EvaluateBuildingRepairBlocker(isDamaged: true, builderAvailable: true));
        }

        // ----------------------------------------------------------------
        // Building-side repair actor convention
        // ----------------------------------------------------------------

        [Test]
        public void TryFindRepairBuilder_ReturnsLowestIndexOwnBuilder()
        {
            var entities = new EntityManager(8);

            // Spawn order pins the indices: 0 harvester, 1 enemy builder, 2
            // own builder, 3 own builder — the convention must skip the
            // non-builder and the foreign one and take index 2.
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(1), SimFixed.FromInt(1)), SimFixed.FromInt(3), role: UnitRole.Harvester);
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(2), SimFixed.FromInt(2)), SimFixed.FromInt(3), role: UnitRole.Builder);
            EntityId expected = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(3), SimFixed.FromInt(3)), SimFixed.FromInt(3), role: UnitRole.Builder);
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(4), SimFixed.FromInt(4)), SimFixed.FromInt(3), role: UnitRole.Builder);

            bool found = CommandCardPresenter.TryFindRepairBuilder(entities, playerSlot: 0, out EntityId builder);

            Assert.IsTrue(found);
            Assert.AreEqual(expected, builder);
        }

        [Test]
        public void TryFindRepairBuilder_NoOwnBuilder_ReturnsFalse()
        {
            var entities = new EntityManager(4);
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(1), SimFixed.FromInt(1)), SimFixed.FromInt(3), role: UnitRole.Harvester);

            Assert.IsFalse(CommandCardPresenter.TryFindRepairBuilder(entities, playerSlot: 0, out EntityId builder));
            Assert.IsFalse(builder.IsValid);
            Assert.IsFalse(CommandCardPresenter.TryFindRepairBuilder(null, playerSlot: 0, out _));
        }

        // ----------------------------------------------------------------
        // Display names (canonical GDD name tables)
        // ----------------------------------------------------------------

        [Test]
        public void DisplayNames_MatchTheMs1NameTables()
        {
            // docs/gamedesign/Vehicles.md and Infantry.md, MS-1 tables.
            Assert.AreEqual("Aegis", CommandCardPresenter.UnitDisplayName(FactionId.Alliance, UnitRole.BattleTank));
            Assert.AreEqual("Schürfer", CommandCardPresenter.UnitDisplayName(FactionId.Legion, UnitRole.Harvester));
            Assert.AreEqual("Rifleman", CommandCardPresenter.UnitDisplayName(FactionId.Alliance, UnitRole.BasicInfantry));
            Assert.AreEqual("Rekrut", CommandCardPresenter.UnitDisplayName(FactionId.Legion, UnitRole.BasicInfantry));
            Assert.AreEqual("Raketenschütze", CommandCardPresenter.UnitDisplayName(FactionId.Legion, UnitRole.AntiArmorInfantry));

            Assert.AreEqual("Raffinerie", CommandCardPresenter.BuildingDisplayName(UnitRole.Refinery));
            Assert.AreEqual("Verteidigungsplattform", CommandCardPresenter.BuildingDisplayName(UnitRole.DefensePlatform));

            // Fallback for roles outside the named roster stays the enum name.
            Assert.AreEqual(UnitRole.Unit.ToString(), CommandCardPresenter.UnitDisplayName(FactionId.Alliance, UnitRole.Unit));
        }
    }
}
