using System;
using System.Globalization;

namespace Nova.Core
{
    /// <summary>
    /// Deterministic signed Q16.16 fixed-point scalar backed by <see cref="int"/>,
    /// per docs/tech/SimulationCore.md section 1 (NumericModelId <c>Q16_16_V1</c>).
    /// <para>
    /// Intermediate products are computed in <see cref="long"/>. Rounding is
    /// nearest, ties-to-even. Overflow, division by zero and out-of-range
    /// conversions are deterministic checked faults (<see cref="OverflowException"/>,
    /// <see cref="DivideByZeroException"/>). Saturation and silent wraparound are
    /// forbidden by the spec and are never applied here.
    /// </para>
    /// <para>
    /// Spec-silent points fixed by this implementation (documented, not
    /// spec truth): <see cref="ToInt"/> truncates toward zero; the ties-to-even
    /// rule also applies to the fractional bits dropped by multiplication and
    /// division.
    /// </para>
    /// </summary>
    public readonly struct SimFixed : IEquatable<SimFixed>, IComparable<SimFixed>
    {
        public const int FractionalBits = 16;
        public const int OneRaw = 1 << FractionalBits;

        public static readonly SimFixed Zero = new SimFixed(0);
        public static readonly SimFixed One = new SimFixed(OneRaw);
        public static readonly SimFixed MaxValue = new SimFixed(int.MaxValue);
        public static readonly SimFixed MinValue = new SimFixed(int.MinValue);

        /// <summary>Raw Q16.16 bits; <c>OneRaw</c> equals 1.0.</summary>
        public int RawValue { get; }

        private SimFixed(int rawValue)
        {
            RawValue = rawValue;
        }

        /// <summary>Wraps an already-scaled raw value. Every int32 raw value is valid.</summary>
        public static SimFixed FromRaw(int rawValue) => new SimFixed(rawValue);

        /// <summary>
        /// Converts a whole number. Values outside [-32768, 32767] are an
        /// out-of-range conversion and throw <see cref="OverflowException"/>.
        /// </summary>
        public static SimFixed FromInt(int value)
        {
            return new SimFixed(CheckedRaw((long)value * OneRaw));
        }

        /// <summary>Truncates toward zero (spec-silent; floor is available via <see cref="Floor"/>).</summary>
        public int ToInt() => RawValue / OneRaw;

        /// <summary>
        /// Largest whole number not greater than this value. Uses an arithmetic
        /// shift, so negative values floor away from zero (e.g. -0.5 to -1).
        /// </summary>
        public int Floor() => RawValue >> FractionalBits;

        /// <summary>Nearest whole number, ties-to-even.</summary>
        public int Round() => (int)RoundShiftRightHalfEven(RawValue, FractionalBits);

        /// <summary>
        /// Canonical world-to-grid mapping per SimulationCore.md section 1:
        /// floor, also for negative values.
        /// </summary>
        public static int WorldToGrid(SimFixed worldCoordinate) => worldCoordinate.Floor();

        public static SimFixed operator +(SimFixed left, SimFixed right)
            => new SimFixed(CheckedRaw((long)left.RawValue + right.RawValue));

        public static SimFixed operator -(SimFixed left, SimFixed right)
            => new SimFixed(CheckedRaw((long)left.RawValue - right.RawValue));

        public static SimFixed operator -(SimFixed value)
            => new SimFixed(CheckedRaw(-(long)value.RawValue));

        public static SimFixed operator *(SimFixed left, SimFixed right)
        {
            // int64 intermediate product per spec; |product| &lt;= 2^62, no int64 overflow.
            long product = (long)left.RawValue * right.RawValue;
            return new SimFixed(CheckedRaw(RoundShiftRightHalfEven(product, FractionalBits)));
        }

        public static SimFixed operator /(SimFixed left, SimFixed right)
        {
            if (right.RawValue == 0)
            {
                throw new DivideByZeroException(
                    "SimFixed division by zero is a deterministic checked fault (SimulationCore.md section 1).");
            }

            long numerator = (long)left.RawValue << FractionalBits;
            long denominator = right.RawValue;
            // C# division truncates toward zero; correct to nearest, ties-to-even.
            long quotient = numerator / denominator;
            long remainder = numerator % denominator;
            long twiceRemainder = Math.Abs(remainder) * 2;
            long absDenominator = Math.Abs(denominator);
            if (twiceRemainder > absDenominator ||
                (twiceRemainder == absDenominator && (quotient & 1L) != 0L))
            {
                quotient += (numerator < 0) == (denominator < 0) ? 1 : -1;
            }
            return new SimFixed(CheckedRaw(quotient));
        }

        public static bool operator ==(SimFixed left, SimFixed right) => left.RawValue == right.RawValue;
        public static bool operator !=(SimFixed left, SimFixed right) => left.RawValue != right.RawValue;
        public static bool operator <(SimFixed left, SimFixed right) => left.RawValue < right.RawValue;
        public static bool operator >(SimFixed left, SimFixed right) => left.RawValue > right.RawValue;
        public static bool operator <=(SimFixed left, SimFixed right) => left.RawValue <= right.RawValue;
        public static bool operator >=(SimFixed left, SimFixed right) => left.RawValue >= right.RawValue;

        public bool Equals(SimFixed other) => RawValue == other.RawValue;
        public override bool Equals(object obj) => obj is SimFixed other && Equals(other);
        public override int GetHashCode() => RawValue;
        public int CompareTo(SimFixed other) => RawValue.CompareTo(other.RawValue);

        /// <summary>Integer-only, culture-invariant formatting for diagnostics.</summary>
        public override string ToString()
        {
            long magnitude = Math.Abs((long)RawValue);
            string sign = RawValue < 0 ? "-" : string.Empty;
            long integerPart = magnitude >> FractionalBits;
            long fractionDigits = ((magnitude & (OneRaw - 1)) * 1000000) >> FractionalBits;
            return string.Concat(
                sign,
                integerPart.ToString(CultureInfo.InvariantCulture),
                ".",
                fractionDigits.ToString("D6", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Right shift with round-to-nearest, ties-to-even. The arithmetic shift
        /// floors and the masked remainder is always in [0, 2^shift), which keeps
        /// negative halves correct (e.g. -1.5 rounds to -2).
        /// </summary>
        private static long RoundShiftRightHalfEven(long value, int shift)
        {
            long truncated = value >> shift;
            long remainder = value & ((1L << shift) - 1);
            long half = 1L << (shift - 1);
            if (remainder > half || (remainder == half && (truncated & 1L) != 0L))
            {
                truncated++;
            }
            return truncated;
        }

        /// <summary>Range-checks an int64 intermediate against the int32 raw domain.</summary>
        private static int CheckedRaw(long raw)
        {
            if (raw < int.MinValue || raw > int.MaxValue)
            {
                throw new OverflowException(
                    "SimFixed result leaves the Q16.16 range [-32768, 32767.9999847412109375]; " +
                    "saturation and wraparound are forbidden (SimulationCore.md section 1).");
            }
            return (int)raw;
        }
    }
}
