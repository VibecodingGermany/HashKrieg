using NUnit.Framework;
using Nova.Data;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Data.Tests
{
    /// <summary>
    /// Pins the ArtAssetStandard.md section-2 prefab naming convention: the
    /// parser accepts exactly the canonical PF_UNIT_/PF_BLDG_ names and
    /// rejects everything else, so a dropped-in asset either registers
    /// deterministically or is reported as a convention violation.
    /// </summary>
    [TestFixture]
    public sealed class ArtAssetNamingTests
    {
        [Test]
        public void TryParsePrefabName_AllianceUnit_ResolvesFactionRoleAndClass()
        {
            bool ok = ArtAssetNaming.TryParsePrefabName(
                "PF_UNIT_Alliance_LightTank.prefab",
                out FactionId faction, out UnitRole role, out bool isBuilding);

            Assert.IsTrue(ok);
            Assert.AreEqual(FactionId.Alliance, faction);
            Assert.AreEqual(UnitRole.LightTank, role);
            Assert.IsFalse(isBuilding);
            Assert.AreEqual(15, SimDefinitions.ToDefinitionId(faction, role));
        }

        [Test]
        public void TryParsePrefabName_LegionBuilding_ResolvesLegionOffsetId()
        {
            bool ok = ArtAssetNaming.TryParsePrefabName(
                "PF_BLDG_Legion_HQ",
                out FactionId faction, out UnitRole role, out bool isBuilding);

            Assert.IsTrue(ok);
            Assert.AreEqual(FactionId.Legion, faction);
            Assert.AreEqual(UnitRole.HQ, role);
            Assert.IsTrue(isBuilding);
            Assert.AreEqual(3 + SimDefinitions.FactionDefinitionOffset,
                SimDefinitions.ToDefinitionId(faction, role));
        }

        [Test]
        public void TryParsePrefabName_AllMs1Roles_Parse()
        {
            string[] unitRoles =
            {
                "Builder", "Harvester", "BasicInfantry", "AntiArmorInfantry",
                "ScoutVehicle", "LightTank", "BattleTank", "Artillery",
            };
            string[] buildingRoles =
            {
                "HQ", "Power", "Refinery", "Storage", "Barracks",
                "VehicleFactory", "ResearchLab", "Radar", "DefensePlatform",
            };

            foreach (string role in unitRoles)
            {
                Assert.IsTrue(
                    ArtAssetNaming.TryParsePrefabName($"PF_UNIT_Legion_{role}", out _, out _, out bool isBuilding),
                    $"unit role {role}");
                Assert.IsFalse(isBuilding, $"unit role {role} classified as building");
            }

            foreach (string role in buildingRoles)
            {
                Assert.IsTrue(
                    ArtAssetNaming.TryParsePrefabName($"PF_BLDG_Alliance_{role}", out _, out _, out bool isBuilding),
                    $"building role {role}");
                Assert.IsTrue(isBuilding, $"building role {role} classified as unit");
            }
        }

        [Test]
        public void TryParsePrefabDefinitionId_ResolvesWireIdsAndRejectsInvalid()
        {
            Assert.IsTrue(ArtAssetNaming.TryParsePrefabDefinitionId("PF_UNIT_Alliance_Builder.prefab", out int builderId, out bool builderIsBuilding));
            Assert.AreEqual(1, builderId);
            Assert.IsFalse(builderIsBuilding);

            Assert.IsTrue(ArtAssetNaming.TryParsePrefabDefinitionId("PF_BLDG_Legion_HQ", out int hqId, out bool hqIsBuilding));
            Assert.AreEqual(3 + SimDefinitions.FactionDefinitionOffset, hqId);
            Assert.IsTrue(hqIsBuilding);

            Assert.IsTrue(ArtAssetNaming.TryParsePrefabDefinitionId("PF_UNIT_Legion_Artillery", out int artilleryId, out _));
            Assert.AreEqual(SimDefinitions.MaxDefinitionId, artilleryId);

            Assert.IsFalse(ArtAssetNaming.TryParsePrefabDefinitionId("SM_UNIT_Alliance_Builder", out int rejectedId, out _));
            Assert.AreEqual(0, rejectedId);
        }

        [Test]
        public void TryParsePrefabName_RejectsNonConventionNames()
        {
            // Meshes, textures and materials are valid assets but no prefabs.
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("SM_UNIT_Alliance_LightTank", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("T_BLDG_Legion_HQ_BC", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("M_UNIT_Legion_Harvester", out _, out _, out _));

            // Wrong faction token (data-layer token is NOT the art-layer token).
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_Allianz_Builder", out _, out _, out _));

            // Unit/building class swapped across the role sets.
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_Alliance_HQ", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_BLDG_Legion_LightTank", out _, out _, out _));

            // The generic construction-site role has no art asset.
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_Alliance_Unit", out _, out _, out _));

            // Case drift and malformed shapes.
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_alliance_Builder", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_Alliance_lighttank", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_Alliance", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("PF_UNIT_Alliance_Builder_Extra", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName("", out _, out _, out _));
            Assert.IsFalse(ArtAssetNaming.TryParsePrefabName(null, out _, out _, out _));
        }
    }
}
