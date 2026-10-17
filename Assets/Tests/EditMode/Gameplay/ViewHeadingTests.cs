using NUnit.Framework;
using UnityEngine;
using Nova.Core;
using Nova.Gameplay.Match;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Sim heading -> Unity yaw (EditMode lane). The simulation measures
    /// counter-clockwise from sim +X (<see cref="SimTrig.Atan2"/>), Unity
    /// clockwise from world +Z; a model rotated by the view yaw must look
    /// along the direction the simulation actually moves it. The regression
    /// these pin down: the raw sim angle as yaw mirrored every heading
    /// across the north-east diagonal.
    /// </summary>
    [TestFixture]
    public class ViewHeadingTests
    {
        private static SimAngle HeadingOf(int dx, int dy)
            => SimTrig.Atan2(SimFixed.FromInt(dy), SimFixed.FromInt(dx));

        private static Vector3 WorldMove(int dx, int dy) => new Vector3(dx, 0f, dy).normalized;

        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(1, 1)]
        [TestCase(-1, 1)]
        [TestCase(-1, -1)]
        [TestCase(1, -1)]
        [TestCase(3, -7)]
        public void ModelForward_PointsAlongTheSimulatedMove(int dx, int dy)
        {
            Vector3 forward = ViewHeading.Rotation(HeadingOf(dx, dy)) * Vector3.forward;

            Assert.That(Vector3.Angle(forward, WorldMove(dx, dy)), Is.LessThan(0.5f),
                $"a unit moving ({dx},{dy}) in sim space must face world ({dx},0,{dy})");
        }

        [Test]
        public void YawDegrees_EastIsNinety_NorthIsZero()
        {
            Assert.That(ViewHeading.YawDegrees(HeadingOf(1, 0)), Is.EqualTo(90f).Within(0.01f));
            Assert.That(ViewHeading.YawDegrees(HeadingOf(0, 1)), Is.EqualTo(0f).Within(0.01f));
            Assert.That(ViewHeading.YawDegrees(HeadingOf(0, -1)), Is.EqualTo(180f).Within(0.01f));
            Assert.That(ViewHeading.YawDegrees(HeadingOf(-1, 0)), Is.EqualTo(270f).Within(0.01f));
        }

        [Test]
        public void YawToward_MatchesTheSimHeadingOfTheSameOffset()
        {
            float toward = ViewHeading.YawToward(new Vector3(2f, 0f, 5f), new Vector3(6f, 3f, 2f), 0f);

            Assert.That(Mathf.DeltaAngle(toward, ViewHeading.YawDegrees(HeadingOf(4, -3))),
                Is.EqualTo(0f).Within(0.05f));
        }

        [Test]
        public void YawToward_CoincidentPoints_KeepsTheFallback()
        {
            Assert.That(ViewHeading.YawToward(Vector3.one, Vector3.one, 123f), Is.EqualTo(123f));
        }
    }
}
