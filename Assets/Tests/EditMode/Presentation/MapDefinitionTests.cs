using NUnit.Framework;
using UnityEngine;
using Nova.Presentation.Maps;

namespace Nova.Presentation.Tests
{
    [TestFixture]
    public class MapDefinitionTests
    {
        [Test]
        public void MapDefinition_ValidatesDimensionsAndSpawnPoints()
        {
            var map = ScriptableObject.CreateInstance<MapDefinitionSO>();

            map.Initialize(
                mapName: "Dust Bowl 1v1",
                biome: MapBiomeType.Desert,
                width: 128,
                height: 128,
                spawnPoints: new Vector2[] { new Vector2(20f, 20f), new Vector2(100f, 100f) },
                resourceNodes: new Vector2[] { new Vector2(64f, 64f) }
            );

            Assert.IsTrue(map.IsValid());
            Assert.AreEqual("Dust Bowl 1v1", map.MapName);
            Assert.AreEqual(MapBiomeType.Desert, map.Biome);
            Assert.AreEqual(2, map.SpawnPoints.Count);
            Assert.AreEqual(1, map.ResourceNodes.Count);

            Object.DestroyImmediate(map);
        }
    }
}
