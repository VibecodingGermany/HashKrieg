using Nova.Core;
using NUnit.Framework;

namespace Nova.Core.Tests
{
    /// <summary>
    /// G1 numerics suite for the uint16 <see cref="SimAngle"/> (docs/tech/Testing.md
    /// section 3): defined wraparound and the documented degrees mapping.
    /// Mirror of the .NET lane SimAngleTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class SimAngleTests
    {
        [Test]
        public void Addition_WrapsAroundFullRevolution()
        {
            Assert.AreEqual(0, (SimAngle.FromRaw(65535) + SimAngle.FromRaw(1)).RawValue);
            Assert.AreEqual(1, (SimAngle.FromRaw(65535) + SimAngle.FromRaw(2)).RawValue);
        }

        [Test]
        public void Subtraction_WrapsBelowZero()
        {
            Assert.AreEqual(65535, (SimAngle.Zero - SimAngle.FromRaw(1)).RawValue);
            Assert.AreEqual(65535, (SimAngle.FromRaw(1) - SimAngle.FromRaw(2)).RawValue);
            Assert.AreEqual(65535, (-SimAngle.FromRaw(1)).RawValue);
        }

        [Test]
        public void FromDegrees_MapsCardinalAndWrappedAngles()
        {
            Assert.AreEqual(0, SimAngle.FromDegrees(SimFixed.Zero).RawValue);
            Assert.AreEqual(16384, SimAngle.FromDegrees(SimFixed.FromInt(90)).RawValue);
            Assert.AreEqual(32768, SimAngle.FromDegrees(SimFixed.FromInt(180)).RawValue);
            Assert.AreEqual(0, SimAngle.FromDegrees(SimFixed.FromInt(360)).RawValue, "360 deg wraps to 0");
            Assert.AreEqual(49152, SimAngle.FromDegrees(SimFixed.FromInt(-90)).RawValue, "-90 deg wraps to 270");
        }

        [Test]
        public void ToDegrees_IsExact()
        {
            Assert.AreEqual(360, SimAngle.FromRaw(1).ToDegrees().RawValue);
            Assert.AreEqual(SimFixed.FromInt(180), SimAngle.FromRaw(32768).ToDegrees());
        }

        [Test]
        public void DegreesRoundTrip_IsStableForAllUnits()
        {
            for (int units = 0; units < SimAngle.UnitsPerRevolution; units += 257)
            {
                var angle = SimAngle.FromRaw((ushort)units);
                Assert.AreEqual(angle, SimAngle.FromDegrees(angle.ToDegrees()), $"units {units}");
            }
        }
    }
}
