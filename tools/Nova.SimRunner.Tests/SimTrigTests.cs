using NUnit.Framework;
using Nova.Core;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 numerics suite for the purely integer <see cref="SimTrig"/>
    /// (.NET lane, docs/tech/Testing.md section 3): exhaustive accuracy
    /// bounds over all 65536 angle units (integer-only reference checks —
    /// no float/double reference is needed or used), Atan2 quadrant and
    /// special cases, Sqrt exactness/monotonicity and determinism.
    /// Mirror of the EditMode lane SimTrigTests.
    /// </summary>
    [TestFixture]
    public sealed class SimTrigTests
    {
        private const int OneRaw = SimFixed.OneRaw;

        [Test]
        public void SinCos_CardinalAngles_AreExact()
        {
            Assert.That(SimTrig.Sin(SimAngle.Zero).RawValue, Is.EqualTo(0));
            Assert.That(SimTrig.Sin(SimAngle.FromRaw(16384)).RawValue, Is.EqualTo(OneRaw), "sin(90 deg) = 1");
            Assert.That(SimTrig.Sin(SimAngle.FromRaw(32768)).RawValue, Is.EqualTo(0));
            Assert.That(SimTrig.Sin(SimAngle.FromRaw(49152)).RawValue, Is.EqualTo(-OneRaw), "sin(270 deg) = -1");

            Assert.That(SimTrig.Cos(SimAngle.Zero).RawValue, Is.EqualTo(OneRaw), "cos(0) = 1");
            Assert.That(SimTrig.Cos(SimAngle.FromRaw(16384)).RawValue, Is.EqualTo(0));
            Assert.That(SimTrig.Cos(SimAngle.FromRaw(32768)).RawValue, Is.EqualTo(-OneRaw));
            Assert.That(SimTrig.Cos(SimAngle.FromRaw(49152)).RawValue, Is.EqualTo(0));
        }

        [Test]
        public void SinCos_KnownValues_MatchIntegerReference()
        {
            // Integer reference literals (round(true * 65536) at the exact
            // representable angles): sin(45 deg) = 46341 at 8192 units;
            // 30 deg rounds to 5461 units (29.9945 deg) whose sine is 32766,
            // 60 deg rounds to 10923 units (60.0018 deg) whose cosine is 32766.
            Assert.That(SimTrig.Sin(SimAngle.FromRaw(8192)).RawValue, Is.EqualTo(46341), "sin(45 deg)");
            Assert.That(SimTrig.Sin(SimAngle.FromDegrees(SimFixed.FromInt(30))).RawValue, Is.EqualTo(32766));
            Assert.That(SimTrig.Cos(SimAngle.FromDegrees(SimFixed.FromInt(60))).RawValue, Is.EqualTo(32766));
        }

        [Test]
        public void SinCos_PythagoreanIdentity_HoldsWithinBound_ForAllAngles()
        {
            // Full sweep over all 65536 angles: with a per-entry error of at
            // most half a raw unit, |sin^2+cos^2-1| stays at or below 2 raw
            // units (measured maximum on the reference implementation: 1.4).
            for (int a = 0; a < SimAngle.UnitsPerRevolution; a++)
            {
                var angle = SimAngle.FromRaw((ushort)a);
                long sin = SimTrig.Sin(angle).RawValue;
                long cos = SimTrig.Cos(angle).RawValue;
                long identityRaw = (sin * sin + cos * cos) / OneRaw;
                long deviation = identityRaw - OneRaw;
                Assert.That(deviation, Is.InRange(-2L, 2L), $"identity deviation at angle {a}");
            }
        }

        [Test]
        public void SinCos_Symmetries_AreExact_ForAllAngles()
        {
            for (int a = 0; a < SimAngle.UnitsPerRevolution; a++)
            {
                var angle = SimAngle.FromRaw((ushort)a);
                Assert.That(SimTrig.Cos(angle), Is.EqualTo(SimTrig.Sin(angle + SimAngle.FromRaw(16384))),
                    $"cos == sin(+90 deg) at {a}");
                Assert.That(SimTrig.Sin(angle + SimAngle.FromRaw(32768)), Is.EqualTo(-SimTrig.Sin(angle)),
                    $"sin(+180 deg) == -sin at {a}");
            }
        }

        [Test]
        public void Sin_IsMonotonicAcross_FirstQuadrant()
        {
            int previous = SimTrig.Sin(SimAngle.Zero).RawValue;
            for (int a = 1; a <= 16384; a++)
            {
                int current = SimTrig.Sin(SimAngle.FromRaw((ushort)a)).RawValue;
                Assert.That(current, Is.GreaterThanOrEqualTo(previous), $"monotonicity broken at angle {a}");
                previous = current;
            }
        }

        [Test]
        public void Atan2_AxesAndOrigin_AreExactAndDocumented()
        {
            Assert.That(SimTrig.Atan2(SimFixed.Zero, SimFixed.FromInt(3)).RawValue, Is.EqualTo(0));
            Assert.That(SimTrig.Atan2(SimFixed.Zero, SimFixed.FromInt(-3)).RawValue, Is.EqualTo(32768));
            Assert.That(SimTrig.Atan2(SimFixed.FromInt(3), SimFixed.Zero).RawValue, Is.EqualTo(16384));
            Assert.That(SimTrig.Atan2(SimFixed.FromInt(-3), SimFixed.Zero).RawValue, Is.EqualTo(49152));
            Assert.That(SimTrig.Atan2(SimFixed.Zero, SimFixed.Zero), Is.EqualTo(SimAngle.Zero),
                "the documented degenerate (0, 0) case returns SimAngle.Zero");
        }

        [Test]
        public void Atan2_Quadrants_MapCorrectly()
        {
            SimFixed one = SimFixed.One;
            SimFixed minusOne = -SimFixed.One;
            Assert.That(SimTrig.Atan2(one, one).RawValue, Is.EqualTo(8192), "45 deg");
            Assert.That(SimTrig.Atan2(one, minusOne).RawValue, Is.EqualTo(24576), "135 deg");
            Assert.That(SimTrig.Atan2(minusOne, minusOne).RawValue, Is.EqualTo(40960), "225 deg");
            Assert.That(SimTrig.Atan2(minusOne, one).RawValue, Is.EqualTo(57344), "315 deg");

            // Reference literal: atan2(1, 2) = 26.565 deg = 4836.02 units.
            Assert.That(SimTrig.Atan2(SimFixed.One, SimFixed.FromInt(2)).RawValue, Is.EqualTo(4836));
        }

        [Test]
        public void Atan2_RoundtripThroughSinCos_IsExact_ForAllAngles()
        {
            // Exhaustive: Atan2(Sin(a), Cos(a)) reproduces every angle unit
            // exactly (sub-ULP errors of Sin/Cos and Atan2 cancel in the
            // quantization back to 16-bit angle space).
            for (int a = 0; a < SimAngle.UnitsPerRevolution; a++)
            {
                var angle = SimAngle.FromRaw((ushort)a);
                SimAngle roundtrip = SimTrig.Atan2(SimTrig.Sin(angle), SimTrig.Cos(angle));
                Assert.That(roundtrip.RawValue, Is.EqualTo(a), $"roundtrip broken at angle {a}");
            }
        }

        [Test]
        public void Sqrt_PerfectSquares_AreExact()
        {
            for (int k = 0; k <= 181; k++)
            {
                SimFixed value = SimFixed.FromInt(k) * SimFixed.FromInt(k);
                Assert.That(SimTrig.Sqrt(value), Is.EqualTo(SimFixed.FromInt(k)), $"sqrt({k}^2)");
            }
            Assert.That(SimTrig.Sqrt(SimFixed.Zero), Is.EqualTo(SimFixed.Zero));
            Assert.That(SimTrig.Sqrt(SimFixed.FromRaw(1)).RawValue, Is.EqualTo(256), "sqrt(1 raw) = 1/256");
        }

        [Test]
        public void Sqrt_IsMonotonic_AndWithinHalfRawUnit()
        {
            // Sweep a dense sample up to the Q16.16 domain edge: results are
            // non-decreasing and satisfy the nearest-root property
            // |root^2 - radicand| <= root (measured error stays below half a
            // raw unit).
            long previous = -1;
            for (long raw = 0; raw < int.MaxValue; raw += 9973)
            {
                long root = SimTrig.Sqrt(SimFixed.FromRaw((int)raw)).RawValue;
                Assert.That(root, Is.GreaterThanOrEqualTo(previous), $"monotonicity broken at raw {raw}");
                previous = root;

                long radicand = raw << 16;
                long diff = root * root - radicand;
                if (diff < 0) diff = -diff;
                Assert.That(diff, Is.LessThanOrEqualTo(root), $"nearest-root property broken at raw {raw}");
            }
            int maxRoot = SimTrig.Sqrt(SimFixed.MaxValue).RawValue;
            Assert.That(maxRoot, Is.EqualTo(11863283), "sqrt(int32 max raw) stays in int32 range");
        }

        [Test]
        public void Sqrt_NegativeInput_IsDeterministicCheckedFault()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SimTrig.Sqrt(SimFixed.FromInt(-1)));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SimTrig.Sqrt(SimFixed.FromRaw(-1)));
        }

        [Test]
        public void AllFunctions_TwoRuns_AreBitIdentical()
        {
            // Trivial for pure integer code, kept as a determinism regression.
            for (int a = 0; a < SimAngle.UnitsPerRevolution; a += 257)
            {
                var angle = SimAngle.FromRaw((ushort)a);
                Assert.That(SimTrig.Sin(angle), Is.EqualTo(SimTrig.Sin(angle)));
                Assert.That(SimTrig.Cos(angle), Is.EqualTo(SimTrig.Cos(angle)));
                Assert.That(SimTrig.Atan2(SimTrig.Sin(angle), SimTrig.Cos(angle)),
                    Is.EqualTo(SimTrig.Atan2(SimTrig.Sin(angle), SimTrig.Cos(angle))));
            }
            Assert.That(SimTrig.Sqrt(SimFixed.FromInt(12345)), Is.EqualTo(SimTrig.Sqrt(SimFixed.FromInt(12345))));
        }
    }
}
