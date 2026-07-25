using NUnit.Framework;
using Nova.Core;
using Nova.Presentation.UI;
using Nova.Simulation.State;

namespace Nova.Presentation.Tests
{
    [TestFixture]
    public class SelectionManagerTests
    {
        [Test]
        public void SelectionManager_SelectBox_SelectsUnitsInBounds()
        {
            var entities = new EntityManager(10);
            var selection = new SelectionManager();

            EntityId u1 = entities.SpawnUnit(0, new Transform2D(10f, 10f), 5f);
            EntityId u2 = entities.SpawnUnit(0, new Transform2D(15f, 15f), 5f);
            EntityId u3 = entities.SpawnUnit(0, new Transform2D(50f, 50f), 5f); // Outside bounds

            int count = selection.SelectBox(entities, playerId: 0, minX: 5f, minY: 5f, maxX: 20f, maxY: 20f);
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, selection.SelectedCount);
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
