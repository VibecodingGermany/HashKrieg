using System;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Represents a 2D integer grid position in the simulation map.
    /// Memory footprint: 4 bytes (ushort X, ushort Y).
    /// </summary>
    public readonly struct GridPos2D : IEquatable<GridPos2D>
    {
        public static readonly GridPos2D Invalid = new GridPos2D(ushort.MaxValue, ushort.MaxValue);

        public ushort X { get; }
        public ushort Y { get; }

        public bool IsValid => X != ushort.MaxValue && Y != ushort.MaxValue;

        public GridPos2D(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public GridPos2D(int x, int y)
        {
            X = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, x));
            Y = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, y));
        }

        public bool Equals(GridPos2D other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPos2D other && Equals(other);
        public override int GetHashCode() => (X << 16) | Y;

        public static bool operator ==(GridPos2D left, GridPos2D right) => left.Equals(right);
        public static bool operator !=(GridPos2D left, GridPos2D right) => !left.Equals(right);

        public int ManhattanDistance(GridPos2D other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public override string ToString() => IsValid ? $"({X}, {Y})" : "(Invalid)";
    }
}
