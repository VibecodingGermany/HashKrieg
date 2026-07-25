using NUnit.Framework;
using UnityEngine;
using Nova.Data;

namespace Nova.Data.Tests
{
    [TestFixture]
    public class AssetIntegrationTests
    {
        [Test]
        public void AssetMappingRegistry_RegistersAndRetrievesUnitAndBuildingPrefabs()
        {
            var registry = ScriptableObject.CreateInstance<AssetMappingRegistrySO>();
            var dummyUnitPrefab = new GameObject("DummyUnitPrefab");
            var dummyBuildingPrefab = new GameObject("DummyBuildingPrefab");

            registry.RegisterUnitMapping(10, dummyUnitPrefab);
            registry.RegisterBuildingMapping(2, dummyBuildingPrefab);

            Assert.AreEqual(dummyUnitPrefab, registry.GetUnitPrefab(10));
            Assert.AreEqual(dummyBuildingPrefab, registry.GetBuildingPrefab(2));
            Assert.IsNull(registry.GetUnitPrefab(99)); // Unknown ID test

            Object.DestroyImmediate(dummyUnitPrefab);
            Object.DestroyImmediate(dummyBuildingPrefab);
            Object.DestroyImmediate(registry);
        }
    }
}
