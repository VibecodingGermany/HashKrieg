using System;
using Nova.Core;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 numerics suite for the Q16.16 <see cref="SimFixed"/> scalar
    /// (docs/tech/Testing.md section 3, docs/tech/SimulationCore.md section 1):
    /// range limits, ties-to-even rounding, negative world-to-grid floor and
    /// overflow/division-by-zero as deterministic checked faults.
    /// </summary>
    [TestFixture]
    public sealed class SimFixedTests
    {
        [Test]
        public void RangeLimits_MatchSpec()
        {
            Assert.That(SimFixed.OneRaw, Is.EqualTo(65536));
            Assert.That(SimFixed.MaxValue.RawValue, Is.EqualTo(int.MaxValue));
            Assert.That(SimFixed.MinValue.RawValue, Is.EqualTo(int.MinValue));
            Assert.That(SimFixed.MinValue.Floor(), Is.EqualTo(-32768));
            // 32767.9999847412109375 floors to 32767.
            Assert.That(SimFixed.MaxValue.Floor(), Is.EqualTo(32767));
        }

        [Test]
        public void FromInt_ValidRange_ConvertsExactly()
        {
            Assert.That(SimFixed.FromInt(0).RawValue, Is.EqualTo(0));
            Assert.That(SimFixed.FromInt(1).RawValue, Is.EqualTo(65536));
            Assert.That(SimFixed.FromInt(32767).RawValue, Is.EqualTo(32767 * 65536));
            Assert.That(SimFixed.FromInt(-32768).RawValue, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void FromInt_OutOfRange_ThrowsOverflow()
        {
            Assert.Throws<OverflowException>(() => SimFixed.FromInt(32768));
            Assert.Throws<OverflowException>(() => SimFixed.FromInt(-32769));
            Assert.Throws<OverflowException>(() => SimFixed.FromInt(int.MaxValue));
        }

        [Test]
        public void Addition_Overflow_ThrowsInsteadOfWrapping()
        {
            // MaxValue + one raw LSB already leaves the int32 raw domain.
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MaxValue + SimFixed.FromRaw(1);
            });
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MinValue - SimFixed.FromRaw(1);
            });
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MaxValue + SimFixed.One;
            });
            Assert.Throws<OverflowException>(() =>
            {
                var _ = -SimFixed.MinValue;
            });
        }

        [Test]
        public void Multiplication_Overflow_ThrowsInsteadOfSaturating()
        {
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MaxValue * SimFixed.FromInt(2);
            });
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MinValue * SimFixed.FromInt(2);
            });
        }

        [Test]
        public void Multiplication_MaxValueTimesMaxValue_Throws()
        {
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MaxValue * SimFixed.MaxValue;
            });
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MinValue * SimFixed.MinValue;
            });
        }

        [Test]
        public void Division_ByZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() =>
            {
                var _ = SimFixed.One / SimFixed.Zero;
            });
            Assert.Throws<DivideByZeroException>(() =>
            {
                var _ = SimFixed.MinValue / SimFixed.Zero;
            });
        }

        [Test]
        public void Division_ResultOverflow_Throws()
        {
            // (int.MinValue << 16) / -1 = 2^47, far beyond the raw domain.
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MinValue / SimFixed.FromRaw(-1);
            });
        }

        [Test]
        public void Multiplication_TiesToEven_OnDroppedFractionBits()
        {
            // FromRaw(n) * 0.5 produces exact raw halves: 0.5, 1.5, 2.5 raw units.
            var half = SimFixed.FromRaw(32768);
            Assert.That((SimFixed.FromRaw(1) * half).RawValue, Is.EqualTo(0), "0.5 -> 0 (even)");
            Assert.That((SimFixed.FromRaw(3) * half).RawValue, Is.EqualTo(2), "1.5 -> 2 (even)");
            Assert.That((SimFixed.FromRaw(5) * half).RawValue, Is.EqualTo(2), "2.5 -> 2 (even)");
            Assert.That((SimFixed.FromRaw(-1) * half).RawValue, Is.EqualTo(0), "-0.5 -> 0 (even)");
            Assert.That((SimFixed.FromRaw(-3) * half).RawValue, Is.EqualTo(-2), "-1.5 -> -2 (even)");
            Assert.That((SimFixed.FromRaw(-5) * half).RawValue, Is.EqualTo(-2), "-2.5 -> -2 (even)");
            // Non-tie fractions round to nearest.
            Assert.That((SimFixed.FromRaw(3) * SimFixed.FromRaw(16384)).RawValue, Is.EqualTo(1), "0.75 -> 1");
            Assert.That((SimFixed.FromRaw(-3) * SimFixed.FromRaw(16384)).RawValue, Is.EqualTo(-1), "-0.75 -> -1");
        }

        [Test]
        public void Division_TiesToEven_OnQuotient()
        {
            var two = SimFixed.FromInt(2);
            Assert.That((SimFixed.FromRaw(1) / two).RawValue, Is.EqualTo(0), "0.5 raw -> 0 (even)");
            Assert.That((SimFixed.FromRaw(3) / two).RawValue, Is.EqualTo(2), "1.5 raw -> 2 (even)");
            Assert.That((SimFixed.FromRaw(5) / two).RawValue, Is.EqualTo(2), "2.5 raw -> 2 (even)");
            Assert.That((SimFixed.FromRaw(-3) / two).RawValue, Is.EqualTo(-2), "-1.5 raw -> -2 (even)");
            // Exact results are unaffected by rounding.
            Assert.That((SimFixed.One / two).RawValue, Is.EqualTo(32768), "1 / 2 = 0.5 exact");
            Assert.That((SimFixed.FromInt(7) / two).RawValue, Is.EqualTo(7 * 32768), "7 / 2 = 3.5 exact");
        }

        [Test]
        public void Division_NegativeDivisorAndDividend_RoundTiesToEven()
        {
            // Exact sign combinations.
            Assert.That((SimFixed.FromRaw(98304) / SimFixed.FromRaw(-131072)).RawValue, Is.EqualTo(-49152), "1.5 / -2 = -0.75");
            Assert.That((SimFixed.FromRaw(-98304) / SimFixed.FromRaw(-131072)).RawValue, Is.EqualTo(49152), "-1.5 / -2 = 0.75");
            // Ties with a negative dividend.
            var two = SimFixed.FromInt(2);
            Assert.That((SimFixed.FromRaw(-1) / two).RawValue, Is.EqualTo(0), "-0.5 raw -> 0 (even)");
            Assert.That((SimFixed.FromRaw(-5) / two).RawValue, Is.EqualTo(-2), "-2.5 raw -> -2 (even)");
            // Tie with a negative divisor.
            Assert.That((SimFixed.FromRaw(3) / SimFixed.FromInt(-2)).RawValue, Is.EqualTo(-2), "1.5 raw / -2 -> -2 (even)");
            Assert.That((SimFixed.FromRaw(-3) / SimFixed.FromInt(-2)).RawValue, Is.EqualTo(2), "-1.5 raw / -2 -> 2 (even)");
        }

        [Test]
        public void Round_TiesToEven_ToWholeNumber()
        {
            Assert.That(SimFixed.FromRaw(32768).Round(), Is.EqualTo(0), "0.5 -> 0");
            Assert.That(SimFixed.FromRaw(98304).Round(), Is.EqualTo(2), "1.5 -> 2");
            Assert.That(SimFixed.FromRaw(163840).Round(), Is.EqualTo(2), "2.5 -> 2");
            Assert.That(SimFixed.FromRaw(-32768).Round(), Is.EqualTo(0), "-0.5 -> 0");
            Assert.That(SimFixed.FromRaw(-98304).Round(), Is.EqualTo(-2), "-1.5 -> -2");
            Assert.That(SimFixed.FromRaw(49152).Round(), Is.EqualTo(1), "0.75 -> 1");
        }

        [Test]
        public void WorldToGrid_Floors_AlsoForNegativeValues()
        {
            Assert.That(SimFixed.WorldToGrid(SimFixed.FromRaw(-32768)), Is.EqualTo(-1), "-0.5 -> -1");
            Assert.That(SimFixed.WorldToGrid(SimFixed.FromInt(-1)), Is.EqualTo(-1), "-1.0 -> -1");
            Assert.That(SimFixed.WorldToGrid(SimFixed.FromRaw(-98304)), Is.EqualTo(-2), "-1.5 -> -2");
            Assert.That(SimFixed.WorldToGrid(SimFixed.FromRaw(-1)), Is.EqualTo(-1), "-epsilon -> -1");
            Assert.That(SimFixed.WorldToGrid(SimFixed.FromRaw(98304)), Is.EqualTo(1), "1.5 -> 1");
            Assert.That(SimFixed.WorldToGrid(SimFixed.Zero), Is.EqualTo(0), "0 -> 0");
        }

        [Test]
        public void ToInt_TruncatesTowardZero()
        {
            Assert.That(SimFixed.FromRaw(98304).ToInt(), Is.EqualTo(1));
            Assert.That(SimFixed.FromRaw(-98304).ToInt(), Is.EqualTo(-1));
        }

        [Test]
        public void Arithmetic_ExactCases_MatchInt64Intermediates()
        {
            Assert.That((SimFixed.One + SimFixed.One).RawValue, Is.EqualTo(2 * 65536));
            Assert.That((SimFixed.FromInt(3) * SimFixed.FromInt(4)).RawValue, Is.EqualTo(12 * 65536));
            Assert.That((SimFixed.FromInt(7) - SimFixed.FromInt(10)).RawValue, Is.EqualTo(-3 * 65536));
        }

        [Test]
        public void Equality_AndComparison_UseRawValue()
        {
            var a = SimFixed.FromInt(2);
            var b = SimFixed.FromRaw(2 * 65536);
            var c = SimFixed.FromInt(3);
            Assert.That(a == b, Is.True);
            Assert.That(a != c, Is.True);
            Assert.That(a < c, Is.True);
            Assert.That(c > a, Is.True);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.CompareTo(c), Is.LessThan(0));
        }
    }
}
