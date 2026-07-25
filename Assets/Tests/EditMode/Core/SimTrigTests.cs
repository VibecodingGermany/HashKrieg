using Nova.Core;
using NUnit.Framework;

namespace Nova.Core.Tests
{
    /// <summary>
    /// G1 numerics suite for the purely integer <see cref="SimTrig"/>
    /// (EditMode lane, docs/tech/Testing.md section 3): exhaustive accuracy
    /// bounds over all 65536 angle units (integer-only reference checks —
    /// no float/double reference is needed or used), Atan2 quadrant and
    /// special cases, Sqrt exactness/monotonicity and determinism.
    /// Mirror of the .NET lane SimTrigTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class SimTrigTests
    {
        private const int OneRaw = SimFixed.OneRaw;

        [Test]
        public void SinCos_CardinalAngles_AreExact()
        {
            Assert.AreEqual(0, SimTrig.Sin(SimAngle.Zero).RawValue);
            Assert.AreEqual(OneRaw, SimTrig.Sin(SimAngle.FromRaw(16384)).RawValue, "sin(90 deg) = 1");
            Assert.AreEqual(0, SimTrig.Sin(SimAngle.FromRaw(32768)).RawValue);
            Assert.AreEqual(-OneRaw, SimTrig.Sin(SimAngle.FromRaw(49152)).RawValue, "sin(270 deg) = -1");

            Assert.AreEqual(OneRaw, SimTrig.Cos(SimAngle.Zero).RawValue, "cos(0) = 1");
            Assert.AreEqual(0, SimTrig.Cos(SimAngle.FromRaw(16384)).RawValue);
            Assert.AreEqual(-OneRaw, SimTrig.Cos(SimAngle.FromRaw(32768)).RawValue);
            Assert.AreEqual(0, SimTrig.Cos(SimAngle.FromRaw(49152)).RawValue);
        }

        [Test]
        public void SinCos_KnownValues_MatchIntegerReference()
        {
            // Integer reference literals (round(true * 65536) at the exact
            // representable angles): sin(45 deg) = 46341 at 8192 units;
            // 30 deg rounds to 5461 units (29.9945 deg) whose sine is 32766,
            // 60 deg rounds to 10923 units (60.0018 deg) whose cosine is 32766.
            Assert.AreEqual(46341, SimTrig.Sin(SimAngle.FromRaw(8192)).RawValue, "sin(45 deg)");
            Assert.AreEqual(32766, SimTrig.Sin(SimAngle.FromDegrees(SimFixed.FromInt(30))).RawValue);
            Assert.AreEqual(32766, SimTrig.Cos(SimAngle.FromDegrees(SimFixed.FromInt(60))).RawValue);
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
                Assert.LessOrEqual(deviation, 2L, $"identity deviation at angle {a}");
                Assert.GreaterOrEqual(deviation, -2L, $"identity deviation at angle {a}");
            }
        }

        [Test]
        public void SinCos_Symmetries_AreExact_ForAllAngles()
        {
            for (int a = 0; a < SimAngle.UnitsPerRevolution; a++)
            {
                var angle = SimAngle.FromRaw((ushort)a);
                Assert.AreEqual(SimTrig.Sin(angle + SimAngle.FromRaw(16384)), SimTrig.Cos(angle),
                    $"cos == sin(+90 deg) at {a}");
                Assert.AreEqual(-SimTrig.Sin(angle), SimTrig.Sin(angle + SimAngle.FromRaw(32768)),
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
                Assert.GreaterOrEqual(current, previous, $"monotonicity broken at angle {a}");
                previous = current;
            }
        }

        [Test]
        public void Atan2_AxesAndOrigin_AreExactAndDocumented()
        {
            Assert.AreEqual(0, SimTrig.Atan2(SimFixed.Zero, SimFixed.FromInt(3)).RawValue);
            Assert.AreEqual(32768, SimTrig.Atan2(SimFixed.Zero, SimFixed.FromInt(-3)).RawValue);
            Assert.AreEqual(16384, SimTrig.Atan2(SimFixed.FromInt(3), SimFixed.Zero).RawValue);
            Assert.AreEqual(49152, SimTrig.Atan2(SimFixed.FromInt(-3), SimFixed.Zero).RawValue);
            Assert.AreEqual(SimAngle.Zero, SimTrig.Atan2(SimFixed.Zero, SimFixed.Zero),
                "the documented degenerate (0, 0) case returns SimAngle.Zero");
        }

        [Test]
        public void Atan2_Quadrants_MapCorrectly()
        {
            SimFixed one = SimFixed.One;
            SimFixed minusOne = -SimFixed.One;
            Assert.AreEqual(8192, SimTrig.Atan2(one, one).RawValue, "45 deg");
            Assert.AreEqual(24576, SimTrig.Atan2(one, minusOne).RawValue, "135 deg");
            Assert.AreEqual(40960, SimTrig.Atan2(minusOne, minusOne).RawValue, "225 deg");
            Assert.AreEqual(57344, SimTrig.Atan2(minusOne, one).RawValue, "315 deg");

            // Reference literal: atan2(1, 2) = 26.565 deg = 4836.02 units.
            Assert.AreEqual(4836, SimTrig.Atan2(SimFixed.One, SimFixed.FromInt(2)).RawValue);
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
                Assert.AreEqual(a, roundtrip.RawValue, $"roundtrip broken at angle {a}");
            }
        }

        [Test]
        public void Sqrt_PerfectSquares_AreExact()
        {
            for (int k = 0; k <= 181; k++)
            {
                SimFixed value = SimFixed.FromInt(k) * SimFixed.FromInt(k);
                Assert.AreEqual(SimFixed.FromInt(k), SimTrig.Sqrt(value), $"sqrt({k}^2)");
            }
            Assert.AreEqual(SimFixed.Zero, SimTrig.Sqrt(SimFixed.Zero));
            Assert.AreEqual(256, SimTrig.Sqrt(SimFixed.FromRaw(1)).RawValue, "sqrt(1 raw) = 1/256");
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
                Assert.GreaterOrEqual(root, previous, $"monotonicity broken at raw {raw}");
                previous = root;

                long radicand = raw << 16;
                long diff = root * root - radicand;
                if (diff < 0) diff = -diff;
                Assert.LessOrEqual(diff, root, $"nearest-root property broken at raw {raw}");
            }
            Assert.AreEqual(11863283, SimTrig.Sqrt(SimFixed.MaxValue).RawValue,
                "sqrt(int32 max raw) stays in int32 range");
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
                Assert.AreEqual(SimTrig.Sin(angle), SimTrig.Sin(angle));
                Assert.AreEqual(SimTrig.Cos(angle), SimTrig.Cos(angle));
                Assert.AreEqual(SimTrig.Atan2(SimTrig.Sin(angle), SimTrig.Cos(angle)),
                    SimTrig.Atan2(SimTrig.Sin(angle), SimTrig.Cos(angle)));
            }
            Assert.AreEqual(SimTrig.Sqrt(SimFixed.FromInt(12345)), SimTrig.Sqrt(SimFixed.FromInt(12345)));
        }
    }
}
