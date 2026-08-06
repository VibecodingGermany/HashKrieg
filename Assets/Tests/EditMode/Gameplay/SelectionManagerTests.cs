using NUnit.Framework;
using Nova.Core;
using Nova.Gameplay;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    [TestFixture]
    public class SelectionManagerTests
    {
        [Test]
        public void SelectionManager_SelectBox_SelectsUnitsInBounds()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();

            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(15), SimFixed.FromInt(15)), SimFixed.FromInt(5));
            EntityId u3 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(50), SimFixed.FromInt(50)), SimFixed.FromInt(5)); // Outside bounds

            int count = selection.SelectBox(entities, playerId: 0, minX: 5f, minY: 5f, maxX: 20f, maxY: 20f);
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, selection.SelectedCount);
        }

        [Test]
        public void SelectionManager_CopyMobileSelection_ExcludesBuildingsAndStaleIds()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();

            EntityId builder = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(3), maxHealth: 350, role: UnitRole.Builder);
            EntityId harvester = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(2), maxHealth: 800, role: UnitRole.Harvester);
            EntityId hq = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(14), SimFixed.FromInt(14)), SimFixed.Zero, maxHealth: 2000, role: UnitRole.HQ);
            EntityId refinery = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(16), SimFixed.FromInt(16)), SimFixed.Zero, maxHealth: 800, role: UnitRole.Refinery);

            // Select everything (buildings included — box select does not
            // filter), then let one mobile unit die so its id goes stale.
            selection.SelectBox(entities, playerId: 0, minX: 0f, minY: 0f, maxX: 20f, maxY: 20f);
            Assert.AreEqual(4, selection.SelectedCount);
            entities.DespawnUnit(harvester);

            var buffer = new EntityId[SelectionManager.MaxSelectedEntities];
            int count = selection.CopyMobileSelection(entities, buffer);

            Assert.AreEqual(1, count);
            Assert.AreEqual(builder, buffer[0]);
            Assert.AreNotEqual(hq, buffer[0]);
            Assert.AreNotEqual(refinery, buffer[0]);
        }

        [Test]
        public void ProducerBuildingRoles_IsProducerRole_MatchesMs1ProducerAssignment()
        {
            // The D-077 producer assignment: HQ (Builder), Refinery
            // (Harvester), Barracks (infantry), VehicleFactory (vehicles).
            Assert.IsTrue(ProducerBuildingRoles.IsProducerRole(UnitRole.HQ));
            Assert.IsTrue(ProducerBuildingRoles.IsProducerRole(UnitRole.Refinery));
            Assert.IsTrue(ProducerBuildingRoles.IsProducerRole(UnitRole.Barracks));
            Assert.IsTrue(ProducerBuildingRoles.IsProducerRole(UnitRole.VehicleFactory));

            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.Power));
            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.Storage));
            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.ResearchLab));
            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.Radar));
            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.DefensePlatform));
            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.Builder));
            Assert.IsFalse(ProducerBuildingRoles.IsProducerRole(UnitRole.Unit));
        }

        [Test]
        public void CommandCardPresenter_GetAvailableCommands_ReturnsExpectedFlags()
        {
            var presenter = new CommandCardPresenter();

            Assert.AreEqual(CommandButtonType.None, presenter.GetAvailableCommands(0));

            CommandButtonType flags = presenter.GetAvailableCommands(3);
            Assert.IsTrue(flags.HasFlag(CommandButtonType.Move));
            Assert.IsTrue(flags.HasFlag(CommandButtonType.Stop));
            Assert.IsTrue(flags.HasFlag(CommandButtonType.Attack));
        }

        [Test]
        public void MinimapRenderer_WorldToMinimapCoordinates_ScalesCorrectly()
        {
            var (uiX, uiY) = MinimapRenderer.WorldToMinimapCoordinates(worldX: 64f, worldY: 64f, mapWidth: 128f, mapHeight: 128f, minimapWidth: 256f, minimapHeight: 256f);

            Assert.AreEqual(128f, uiX);
            Assert.AreEqual(128f, uiY);
        }
    }
}
