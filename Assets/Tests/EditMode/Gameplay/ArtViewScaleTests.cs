using NUnit.Framework;
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Map size of art models (EditMode lane, D-113): buildings fill their
    /// footprint, units keep their authored proportions under one metric
    /// factor, small units get a readable minimum height.
    /// </summary>
    [TestFixture]
    public class ArtViewScaleTests
    {
        [Test]
        public void Building_ShrinksToFootprintWidth()
        {
            float f = ArtViewScale.Factor(UnitRole.Barracks, new Vector3(9f, 8f, 6f));
            Assert.That(9f * f, Is.EqualTo(SimDefinitions.BuildingFootprintCells).Within(1e-4f));
        }

        [Test]
        public void Building_SmallerThanFootprint_KeepsAuthoredSize()
        {
            Assert.That(ArtViewScale.Factor(UnitRole.Storage, new Vector3(2f, 2f, 2f)), Is.EqualTo(1f));
        }

        [Test]
        public void Vehicles_ShareOneFactor_SoTheirRatiosSurvive()
        {
            float tank = ArtViewScale.Factor(UnitRole.BattleTank, new Vector3(7.5f, 6.5f, 7.5f));
            float scout = ArtViewScale.Factor(UnitRole.ScoutVehicle, new Vector3(2.8f, 4f, 4.5f));
            Assert.That(tank, Is.EqualTo(scout));
            Assert.That(7.5f * tank, Is.LessThan(SimDefinitions.BuildingFootprintCells * 0.65f),
                "a battle tank must read clearly smaller than a building");
        }

        [Test]
        public void Infantry_GetsTheMinimumHeight_IndependentOfWeaponWidth()
        {
            float rifle = ArtViewScale.Factor(UnitRole.BasicInfantry, new Vector3(0.8f, 1.8f, 0.9f));
            float wideLauncher = ArtViewScale.Factor(UnitRole.BasicInfantry, new Vector3(1.5f, 1.8f, 0.9f));
            Assert.That(1.8f * rifle, Is.EqualTo(ArtViewScale.MinViewHeight(UnitRole.BasicInfantry)).Within(1e-4f));
            Assert.That(wideLauncher, Is.EqualTo(rifle), "width must not shrink a soldier");
        }

        [Test]
        public void Infantry_StaysShorterThanABattleTank()
        {
            float tankHeight = 6.5f * ArtViewScale.Factor(UnitRole.BattleTank, new Vector3(7.5f, 6.5f, 7.5f));
            float soldier = 1.8f * ArtViewScale.Factor(UnitRole.BasicInfantry, new Vector3(0.8f, 1.8f, 0.9f));
            Assert.That(soldier, Is.LessThan(tankHeight * 0.7f));
        }

        [Test]
        public void TryFactor_ResolvesRoleNames_AndRejectsUnknownOnes()
        {
            Assert.That(ArtViewScale.TryFactor("LightTank", Vector3.one, out float f), Is.True);
            Assert.That(f, Is.EqualTo(ArtViewScale.UnitCellsPerMetre));
            Assert.That(ArtViewScale.TryFactor("Zeppelin", Vector3.one, out _), Is.False);
        }
    }
}
