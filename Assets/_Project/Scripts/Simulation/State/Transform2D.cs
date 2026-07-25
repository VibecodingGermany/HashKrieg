using System;
using Nova.Core;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Represents a 2D position and orientation in the simulation world.
    /// Memory footprint: 12 bytes (float X, float Y, float Rotation).
    /// </summary>
    public readonly struct Transform2D : IEquatable<Transform2D>
    {
        public static readonly Transform2D Zero = new Transform2D(0f, 0f, 0f);

        public float PositionX { get; }
        public float PositionY { get; }
        public float Rotation { get; } // Rotation in radians [-PI, PI]

        public Transform2D(float positionX, float positionY, float rotation = 0f)
        {
            PositionX = positionX;
            PositionY = positionY;
            Rotation = rotation;
        }

        public float DistanceToSquared(in Transform2D other)
        {
            float dx = PositionX - other.PositionX;
            float dy = PositionY - other.PositionY;
            return dx * dx + dy * dy;
        }

        public float DistanceTo(in Transform2D other)
        {
            return SimMath.Sqrt(DistanceToSquared(in other));
        }

        public bool Equals(Transform2D other)
        {
            return PositionX.Equals(other.PositionX) &&
                   PositionY.Equals(other.PositionY) &&
                   Rotation.Equals(other.Rotation);
        }

        public override bool Equals(object obj) => obj is Transform2D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PositionX.GetHashCode();
                hash = (hash * 397) ^ PositionY.GetHashCode();
                hash = (hash * 397) ^ Rotation.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Transform2D left, Transform2D right) => left.Equals(right);
        public static bool operator !=(Transform2D left, Transform2D right) => !left.Equals(right);

        public override string ToString() => $"Pos({PositionX:F2}, {PositionY:F2}), Rot({Rotation:F2}rad)";
    }
}
