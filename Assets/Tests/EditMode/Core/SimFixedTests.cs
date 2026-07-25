using System;
using Nova.Core;
using NUnit.Framework;

namespace Nova.Core.Tests
{
    /// <summary>
    /// G1 numerics suite for the Q16.16 <see cref="SimFixed"/> scalar
    /// (docs/tech/Testing.md section 3, docs/tech/SimulationCore.md section 1).
    /// Mirror of the .NET lane SimFixedTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class SimFixedTests
    {
        [Test]
        public void RangeLimits_MatchSpec()
        {
            Assert.AreEqual(65536, SimFixed.OneRaw);
            Assert.AreEqual(int.MaxValue, SimFixed.MaxValue.RawValue);
            Assert.AreEqual(int.MinValue, SimFixed.MinValue.RawValue);
            Assert.AreEqual(-32768, SimFixed.MinValue.Floor());
            Assert.AreEqual(32767, SimFixed.MaxValue.Floor());
        }

        [Test]
        public void FromInt_ValidRange_ConvertsExactly()
        {
            Assert.AreEqual(0, SimFixed.FromInt(0).RawValue);
            Assert.AreEqual(65536, SimFixed.FromInt(1).RawValue);
            Assert.AreEqual(32767 * 65536, SimFixed.FromInt(32767).RawValue);
            Assert.AreEqual(int.MinValue, SimFixed.FromInt(-32768).RawValue);
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
        public void Division_ByZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() =>
            {
                var _ = SimFixed.One / SimFixed.Zero;
            });
        }

        [Test]
        public void Division_ResultOverflow_Throws()
        {
            Assert.Throws<OverflowException>(() =>
            {
                var _ = SimFixed.MinValue / SimFixed.FromRaw(-1);
            });
        }

        [Test]
        public void Multiplication_TiesToEven_OnDroppedFractionBits()
        {
            var half = SimFixed.FromRaw(32768);
            Assert.AreEqual(0, (SimFixed.FromRaw(1) * half).RawValue, "0.5 -> 0 (even)");
            Assert.AreEqual(2, (SimFixed.FromRaw(3) * half).RawValue, "1.5 -> 2 (even)");
            Assert.AreEqual(2, (SimFixed.FromRaw(5) * half).RawValue, "2.5 -> 2 (even)");
            Assert.AreEqual(0, (SimFixed.FromRaw(-1) * half).RawValue, "-0.5 -> 0 (even)");
            Assert.AreEqual(-2, (SimFixed.FromRaw(-3) * half).RawValue, "-1.5 -> -2 (even)");
            Assert.AreEqual(-2, (SimFixed.FromRaw(-5) * half).RawValue, "-2.5 -> -2 (even)");
            Assert.AreEqual(1, (SimFixed.FromRaw(3) * SimFixed.FromRaw(16384)).RawValue, "0.75 -> 1");
            Assert.AreEqual(-1, (SimFixed.FromRaw(-3) * SimFixed.FromRaw(16384)).RawValue, "-0.75 -> -1");
        }

        [Test]
        public void Division_TiesToEven_OnQuotient()
        {
            var two = SimFixed.FromInt(2);
            Assert.AreEqual(0, (SimFixed.FromRaw(1) / two).RawValue, "0.5 raw -> 0 (even)");
            Assert.AreEqual(2, (SimFixed.FromRaw(3) / two).RawValue, "1.5 raw -> 2 (even)");
            Assert.AreEqual(2, (SimFixed.FromRaw(5) / two).RawValue, "2.5 raw -> 2 (even)");
            Assert.AreEqual(-2, (SimFixed.FromRaw(-3) / two).RawValue, "-1.5 raw -> -2 (even)");
            Assert.AreEqual(32768, (SimFixed.One / two).RawValue, "1 / 2 = 0.5 exact");
        }

        [Test]
        public void Round_TiesToEven_ToWholeNumber()
        {
            Assert.AreEqual(0, SimFixed.FromRaw(32768).Round(), "0.5 -> 0");
            Assert.AreEqual(2, SimFixed.FromRaw(98304).Round(), "1.5 -> 2");
            Assert.AreEqual(2, SimFixed.FromRaw(163840).Round(), "2.5 -> 2");
            Assert.AreEqual(0, SimFixed.FromRaw(-32768).Round(), "-0.5 -> 0");
            Assert.AreEqual(-2, SimFixed.FromRaw(-98304).Round(), "-1.5 -> -2");
        }

        [Test]
        public void WorldToGrid_Floors_AlsoForNegativeValues()
        {
            Assert.AreEqual(-1, SimFixed.WorldToGrid(SimFixed.FromRaw(-32768)), "-0.5 -> -1");
            Assert.AreEqual(-1, SimFixed.WorldToGrid(SimFixed.FromInt(-1)), "-1.0 -> -1");
            Assert.AreEqual(-2, SimFixed.WorldToGrid(SimFixed.FromRaw(-98304)), "-1.5 -> -2");
            Assert.AreEqual(-1, SimFixed.WorldToGrid(SimFixed.FromRaw(-1)), "-epsilon -> -1");
            Assert.AreEqual(1, SimFixed.WorldToGrid(SimFixed.FromRaw(98304)), "1.5 -> 1");
        }

        [Test]
        public void ToInt_TruncatesTowardZero()
        {
            Assert.AreEqual(1, SimFixed.FromRaw(98304).ToInt());
            Assert.AreEqual(-1, SimFixed.FromRaw(-98304).ToInt());
        }

        [Test]
        public void Equality_AndComparison_UseRawValue()
        {
            var a = SimFixed.FromInt(2);
            var b = SimFixed.FromRaw(2 * 65536);
            var c = SimFixed.FromInt(3);
            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a < c);
            Assert.IsTrue(c > a);
            Assert.AreEqual(b.GetHashCode(), a.GetHashCode());
        }
    }
}
