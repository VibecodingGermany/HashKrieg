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

        // ------------------------------------------------------------------
        // Sprint 09 §7: additive selection + control groups
        // ------------------------------------------------------------------

        [Test]
        public void SelectionManager_AddSingle_AddsWithoutDuplicates()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(5));

            selection.SelectSingle(u1);
            Assert.IsTrue(selection.AddSingle(u2));
            Assert.AreEqual(2, selection.SelectedCount);
            Assert.IsFalse(selection.AddSingle(u2), "a duplicate add is a no-op");
            Assert.AreEqual(2, selection.SelectedCount);
        }

        [Test]
        public void SelectionManager_SelectBoxAdditive_UnionsWithExistingSelection()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(1), SimFixed.FromInt(1)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(15), SimFixed.FromInt(15)), SimFixed.FromInt(5));

            selection.SelectSingle(u1);
            int count = selection.SelectBoxAdditive(entities, playerId: 0, minX: 10f, minY: 10f, maxX: 20f, maxY: 20f);

            Assert.AreEqual(2, count, "the box content joins the previous selection instead of replacing it");
        }

        [Test]
        public void SelectionManager_ControlGroup_SaveAndRecall_DropsTheDead()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(5));

            selection.SelectBox(entities, playerId: 0, minX: 0f, minY: 0f, maxX: 20f, maxY: 20f);
            selection.SaveControlGroup(1);
            selection.ClearSelection();
            Assert.AreEqual(0, selection.SelectedCount);
            Assert.IsTrue(selection.HasControlGroup(1));

            entities.DespawnUnit(u1); // one member dies before the recall

            int recalled = selection.RecallControlGroup(1, entities, playerId: 0);
            Assert.AreEqual(1, recalled, "the dead member is dropped at recall");
            Assert.AreEqual(u2, selection.SelectedEntities[0]);
        }

        [Test]
        public void SelectionManager_RecallEmptyGroup_IsANoOp()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            Assert.IsFalse(selection.HasControlGroup(5));
            Assert.AreEqual(0, selection.RecallControlGroup(5, entities, playerId: 0));
        }

        // ------------------------------------------------------------------
        // Sprint 21.2 (#86): field selection — UI-only, coupled both ways
        // ------------------------------------------------------------------

        [Test]
        public void SelectionManager_SelectField_ClearsEntitySelection()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(5));
            selection.SelectSingle(u1);
            selection.AddSingle(u2);

            selection.SelectField(3);

            Assert.AreEqual(0, selection.SelectedCount, "a field takes no entity orders — the entity selection goes");
            Assert.AreEqual((ushort)3, selection.SelectedFieldId);
        }

        [Test]
        public void SelectionManager_EntitySelection_ClearsSelectedField()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(5));

            selection.SelectField(2);
            selection.SelectSingle(u1);
            Assert.AreEqual((ushort)0, selection.SelectedFieldId, "SelectSingle replaces the field");
            Assert.AreEqual(1, selection.SelectedCount);

            selection.SelectField(2);
            selection.AddSingle(u2);
            Assert.AreEqual((ushort)0, selection.SelectedFieldId, "an additive entity pick ends the field selection too");
            Assert.AreEqual(1, selection.SelectedCount);

            selection.SelectField(2);
            selection.SelectBox(entities, playerId: 0, minX: 0f, minY: 0f, maxX: 20f, maxY: 20f);
            Assert.AreEqual((ushort)0, selection.SelectedFieldId, "a box selection replaces the field");
            Assert.AreEqual(2, selection.SelectedCount);
        }

        [Test]
        public void SelectionManager_ClearSelection_ClearsFieldAndEntities()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            selection.SelectSingle(u1);

            selection.ClearSelection();
            Assert.AreEqual(0, selection.SelectedCount);
            Assert.AreEqual((ushort)0, selection.SelectedFieldId);

            selection.SelectField(5);
            selection.ClearSelection();
            Assert.AreEqual((ushort)0, selection.SelectedFieldId, "the ingress rebind relies on ClearSelection dropping the field too");
        }

        // ------------------------------------------------------------------
        // Sprint 22 (#50): type-row filter + double-click role select
        // ------------------------------------------------------------------

        [Test]
        public void SelectionManager_RetainRole_KeepsOnlyThatRoleInStableOrder()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId firstTank = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(3), role: UnitRole.LightTank);
            EntityId harvester = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(2), role: UnitRole.Harvester);
            EntityId secondTank = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(14), SimFixed.FromInt(14)), SimFixed.FromInt(3), role: UnitRole.LightTank);
            selection.SelectBox(entities, playerId: 0, minX: 0f, minY: 0f, maxX: 20f, maxY: 20f);
            Assert.AreEqual(3, selection.SelectedCount);

            int kept = selection.RetainRole(entities, UnitRole.LightTank);

            Assert.AreEqual(2, kept, "the row click reduces the selection to the row's type");
            Assert.AreEqual(firstTank, selection.SelectedEntities[0], "the selection order is stable, so the first tank leads");
            Assert.AreEqual(secondTank, selection.SelectedEntities[1]);
        }

        [Test]
        public void SelectionManager_RetainRole_DropsStaleHandlesAndAbsentRoles()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId dying = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(3), role: UnitRole.LightTank);
            EntityId living = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(3), role: UnitRole.LightTank);
            selection.SelectBox(entities, playerId: 0, minX: 0f, minY: 0f, maxX: 20f, maxY: 20f);
            entities.DespawnUnit(dying); // died between the card's model build and the row click

            Assert.AreEqual(1, selection.RetainRole(entities, UnitRole.LightTank), "the stale handle is dropped against the live store");
            Assert.AreEqual(living, selection.SelectedEntities[0]);

            Assert.AreEqual(0, selection.RetainRole(entities, UnitRole.Harvester), "a role nothing selected has leaves an empty selection");
            Assert.AreEqual(0, selection.SelectedCount);
        }

        [Test]
        public void SelectionManager_ReplaceSelection_ReplacesDedupesAndClearsField()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(5));
            EntityId u3 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(14), SimFixed.FromInt(14)), SimFixed.FromInt(5));
            selection.SelectField(3); // a field readout owns the card before the gesture

            int count = selection.ReplaceSelection(new[] { u2, u3, u2 });

            Assert.AreEqual(2, count, "duplicates collapse through AddSingle");
            Assert.AreEqual(u2, selection.SelectedEntities[0], "the new list leads, u1 never joined it");
            Assert.AreEqual((ushort)0, selection.SelectedFieldId, "an entity selection ends the field selection");
        }

        [Test]
        public void SelectionManager_AddRange_UnionsLikeShiftClick()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();
            EntityId u1 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(12), SimFixed.FromInt(12)), SimFixed.FromInt(5));
            EntityId u3 = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(14), SimFixed.FromInt(14)), SimFixed.FromInt(5));
            selection.SelectSingle(u1);

            int count = selection.AddRange(new[] { u2, u1, u3 });

            Assert.AreEqual(3, count, "the new ids join, the already-selected one is not duplicated");
            Assert.AreEqual(u1, selection.SelectedEntities[0], "the existing selection keeps its lead");
            Assert.AreEqual(u2, selection.SelectedEntities[1]);
            Assert.AreEqual(u3, selection.SelectedEntities[2]);
        }
    }
}
