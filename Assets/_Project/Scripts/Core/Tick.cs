using System;

namespace Nova.Core
{
    /// <summary>
    /// Represents a discrete simulation step / frame index in the deterministic Lockstep loop.
    /// </summary>
    public readonly struct Tick : IEquatable<Tick>, IComparable<Tick>
    {
        public static readonly Tick Zero = new Tick(0);

        public uint Value { get; }

        public Tick(uint value)
        {
            Value = value;
        }

        public bool Equals(Tick other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Tick other && Equals(other);
        public override int GetHashCode() => (int)Value;

        public int CompareTo(Tick other) => Value.CompareTo(other.Value);

        public static bool operator ==(Tick left, Tick right) => left.Value == right.Value;
        public static bool operator !=(Tick left, Tick right) => left.Value != right.Value;
        public static bool operator <(Tick left, Tick right) => left.Value < right.Value;
        public static bool operator >(Tick left, Tick right) => left.Value > right.Value;
        public static bool operator <=(Tick left, Tick right) => left.Value <= right.Value;
        public static bool operator >=(Tick left, Tick right) => left.Value >= right.Value;

        public static Tick operator +(Tick tick, uint count) => new Tick(tick.Value + count);
        public static Tick operator -(Tick tick, uint count) => new Tick(tick.Value >= count ? tick.Value - count : 0);
        public static Tick operator ++(Tick tick) => new Tick(tick.Value + 1);

        public override string ToString() => $"Tick({Value})";
    }
}
