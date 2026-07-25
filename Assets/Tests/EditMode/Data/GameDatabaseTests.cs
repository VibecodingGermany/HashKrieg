using NUnit.Framework;
using UnityEngine;
using Nova.Data.Registries;
using Nova.Data.Units;
using Nova.Simulation.Definitions;

namespace Nova.Data.Tests
{
    [TestFixture]
    public class GameDatabaseTests
    {
        [Test]
        public void UnitDefinitionSO_ToSimDefinition_ConvertsToStruct()
        {
            var so = ScriptableObject.CreateInstance<UnitDefinitionSO>();
            so.name = "UNIT_RiflemanTest";

            UnitDefinition simDef = so.ToSimDefinition(definitionId: 42);

            Assert.AreEqual(42, simDef.DefinitionId);
            Assert.AreEqual(so.Id, simDef.StringId);
            Assert.AreEqual(so.MoveSpeed, simDef.MoveSpeed);
            Assert.AreEqual(so.MaxHealth, simDef.MaxHealth);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void UnitRegistry_GetById_ReturnsCorrectDefinition()
        {
            var reg = ScriptableObject.CreateInstance<UnitRegistrySO>();
            var u1 = ScriptableObject.CreateInstance<UnitDefinitionSO>();

            reg.SetEntries(new[] { u1 });

            UnitDefinitionSO found = reg.GetById(u1.Id);
            Assert.IsNotNull(found);
            Assert.AreEqual(u1.Id, found.Id);

            Object.DestroyImmediate(u1);
            Object.DestroyImmediate(reg);
        }

        [Test]
        public void GameDatabaseMasterSO_Validate_DetectsNullEntries()
        {
            var master = ScriptableObject.CreateInstance<GameDatabaseMasterSO>();
            var reg = ScriptableObject.CreateInstance<UnitRegistrySO>();

            reg.SetEntries(new UnitDefinitionSO[] { null });
            master.SetSubRegistries(reg, null, null);

            bool isValid = master.ValidateMasterIndex(out string error);
            Assert.IsFalse(isValid);
            Assert.IsTrue(error.Contains("null reference"));

            Object.DestroyImmediate(reg);
            Object.DestroyImmediate(master);
        }
    }
}
