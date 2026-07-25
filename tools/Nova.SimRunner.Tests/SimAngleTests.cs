using Nova.Core;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 numerics suite for the uint16 <see cref="SimAngle"/> (docs/tech/Testing.md
    /// section 3): defined wraparound and the documented degrees mapping
    /// (360 deg = 65536 units; spec-silent unit fixed by the implementation).
    /// </summary>
    [TestFixture]
    public sealed class SimAngleTests
    {
        [Test]
        public void Addition_WrapsAroundFullRevolution()
        {
            Assert.That((SimAngle.FromRaw(65535) + SimAngle.FromRaw(1)).RawValue, Is.EqualTo(0));
            Assert.That((SimAngle.FromRaw(65535) + SimAngle.FromRaw(2)).RawValue, Is.EqualTo(1));
            Assert.That((SimAngle.FromRaw(40000) + SimAngle.FromRaw(40000)).RawValue, Is.EqualTo(14464));
        }

        [Test]
        public void Subtraction_WrapsBelowZero()
        {
            Assert.That((SimAngle.Zero - SimAngle.FromRaw(1)).RawValue, Is.EqualTo(65535));
            Assert.That((SimAngle.FromRaw(1) - SimAngle.FromRaw(2)).RawValue, Is.EqualTo(65535));
            Assert.That((-SimAngle.FromRaw(1)).RawValue, Is.EqualTo(65535));
        }

        [Test]
        public void FromDegrees_MapsCardinalAndWrappedAngles()
        {
            Assert.That(SimAngle.FromDegrees(SimFixed.Zero).RawValue, Is.EqualTo(0));
            Assert.That(SimAngle.FromDegrees(SimFixed.FromInt(90)).RawValue, Is.EqualTo(16384));
            Assert.That(SimAngle.FromDegrees(SimFixed.FromInt(180)).RawValue, Is.EqualTo(32768));
            Assert.That(SimAngle.FromDegrees(SimFixed.FromInt(270)).RawValue, Is.EqualTo(49152));
            Assert.That(SimAngle.FromDegrees(SimFixed.FromInt(360)).RawValue, Is.EqualTo(0), "360 deg wraps to 0");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromInt(450)).RawValue, Is.EqualTo(16384), "450 deg wraps to 90");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromInt(-90)).RawValue, Is.EqualTo(49152), "-90 deg wraps to 270");
        }

        [Test]
        public void FromDegrees_FractionalDegrees_RoundTiesToEven()
        {
            // units = degreesRaw / 360; raw ≡ 180 (mod 360) is an exact half-unit tie.
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(180)).RawValue, Is.EqualTo(0), "0.5 units -> 0 (even)");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(540)).RawValue, Is.EqualTo(2), "1.5 units -> 2 (even)");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(900)).RawValue, Is.EqualTo(2), "2.5 units -> 2 (even)");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(-180)).RawValue, Is.EqualTo(0), "-0.5 units -> 0 (even)");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(-540)).RawValue, Is.EqualTo(65534), "-1.5 units -> -2 wraps to 65534");
            // Non-tie fractions round to nearest.
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(100)).RawValue, Is.EqualTo(0), "0.278 units -> 0");
            Assert.That(SimAngle.FromDegrees(SimFixed.FromRaw(260)).RawValue, Is.EqualTo(1), "0.722 units -> 1");
            Assert.That(SimAngle.FromDegrees(SimFixed.One).RawValue, Is.EqualTo(182), "1 deg -> 182.04 units -> 182");
        }

        [Test]
        public void ToDegrees_IsExact()
        {
            Assert.That(SimAngle.FromRaw(1).ToDegrees().RawValue, Is.EqualTo(360));
            Assert.That(SimAngle.FromRaw(32768).ToDegrees(), Is.EqualTo(SimFixed.FromInt(180)));
            Assert.That(SimAngle.FromRaw(65535).ToDegrees().RawValue, Is.EqualTo(65535 * 360));
        }

        [Test]
        public void DegreesRoundTrip_IsStableForAllUnits()
        {
            // ToDegrees is exact and FromDegrees re-rounds to the same unit.
            for (int units = 0; units < SimAngle.UnitsPerRevolution; units += 257)
            {
                var angle = SimAngle.FromRaw((ushort)units);
                Assert.That(SimAngle.FromDegrees(angle.ToDegrees()), Is.EqualTo(angle), $"units {units}");
            }
        }

        [Test]
        public void Equality_UsesRawValue()
        {
            Assert.That(SimAngle.FromRaw(42) == SimAngle.FromRaw(42), Is.True);
            Assert.That(SimAngle.FromRaw(42) != SimAngle.FromRaw(43), Is.True);
            Assert.That(SimAngle.FromRaw(42).GetHashCode(), Is.EqualTo(SimAngle.FromRaw(42).GetHashCode()));
        }
    }
}
