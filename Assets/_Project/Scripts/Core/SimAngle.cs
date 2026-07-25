using System;

namespace Nova.Core
{
    /// <summary>
    /// Deterministic angle backed by <see cref="ushort"/>; one full revolution
    /// equals 65536 units. Defined wraparound on add/subtract is allowed
    /// exclusively for this type (docs/tech/SimulationCore.md section 1).
    /// <para>
    /// The spec is silent on the fixed-point conversion unit; this
    /// implementation fixes degrees: 360 degrees = 65536 units.
    /// <see cref="ToDegrees"/> is exact (raw degrees = units * 360);
    /// <see cref="FromDegrees"/> rounds nearest, ties-to-even and then wraps
    /// modulo 65536 (true mathematical modulo, so -90 deg maps to 49152).
    /// </para>
    /// </summary>
    public readonly struct SimAngle : IEquatable<SimAngle>
    {
        public const int UnitsPerRevolution = 1 << 16;

        public static readonly SimAngle Zero = new SimAngle(0);

        /// <summary>Raw angle units; one full revolution is 65536.</summary>
        public ushort RawValue { get; }

        private SimAngle(ushort rawValue)
        {
            RawValue = rawValue;
        }

        public static SimAngle FromRaw(ushort rawValue) => new SimAngle(rawValue);

        /// <summary>
        /// Converts fixed-point degrees to angle units: rounds nearest,
        /// ties-to-even, then wraps modulo 65536.
        /// </summary>
        public static SimAngle FromDegrees(SimFixed degrees)
        {
            long raw = degrees.RawValue;
            // units = degreesRaw * 65536 / (360 * 65536) = degreesRaw / 360.
            long quotient = raw / 360;
            long remainder = raw % 360;
            long twiceRemainder = Math.Abs(remainder) * 2;
            if (twiceRemainder > 360 || (twiceRemainder == 360 && (quotient & 1L) != 0L))
            {
                quotient += raw >= 0 ? 1 : -1;
            }
            // C# % keeps the sign of the dividend; normalize to [0, 65536).
            long wrapped = ((quotient % UnitsPerRevolution) + UnitsPerRevolution) % UnitsPerRevolution;
            return new SimAngle((ushort)wrapped);
        }

        /// <summary>Exact conversion: raw degrees = units * 360 (always fits int32).</summary>
        public SimFixed ToDegrees() => SimFixed.FromRaw(RawValue * 360);

        public static SimAngle operator +(SimAngle left, SimAngle right)
            => new SimAngle(unchecked((ushort)(left.RawValue + right.RawValue)));

        public static SimAngle operator -(SimAngle left, SimAngle right)
            => new SimAngle(unchecked((ushort)(left.RawValue - right.RawValue)));

        public static SimAngle operator -(SimAngle value)
            => new SimAngle(unchecked((ushort)-value.RawValue));

        public static bool operator ==(SimAngle left, SimAngle right) => left.RawValue == right.RawValue;
        public static bool operator !=(SimAngle left, SimAngle right) => left.RawValue != right.RawValue;

        public bool Equals(SimAngle other) => RawValue == other.RawValue;
        public override bool Equals(object obj) => obj is SimAngle other && Equals(other);
        public override int GetHashCode() => RawValue;

        public override string ToString() => $"SimAngle({RawValue})";
    }
}
