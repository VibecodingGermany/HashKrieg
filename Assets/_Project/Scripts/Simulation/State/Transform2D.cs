using System;
using Nova.Core;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Represents a 2D position and orientation in the simulation world.
    /// Memory footprint: 10 bytes (Q16.16 <see cref="SimFixed"/> X/Y,
    /// <see cref="SimAngle"/> Rotation).
    /// <para>
    /// Q-040(i) resolution (implemented, ratification pending): position and
    /// rotation are canonical fixed-point values — no IEEE-754 floats remain
    /// in the hash-relevant movement state (docs/tech/SimulationCore.md
    /// sections 1 and 9). Rotation 0 points along +x and increases toward +y,
    /// matching <see cref="SimTrig.Atan2"/>.
    /// </para>
    /// <para>
    /// Value range: positions are bounded by the Q16.16 domain
    /// [-32768, 32768). <see cref="DistanceToSquared"/> is exact checked
    /// fixed-point arithmetic; it covers squared distances up to 32767.99
    /// (e.g. deltas up to 128 m per axis on the 128x128 map, 2 * 128^2 =
    /// 32768 would exceed the domain by a hair, so the exact full-map
    /// diagonal is a checked overflow, not a silent wraparound). All current
    /// call sites (separation radii ~1 m) are far inside the safe range.
    /// </para>
    /// </summary>
    public readonly struct Transform2D : IEquatable<Transform2D>
    {
        public static readonly Transform2D Zero = new Transform2D(SimFixed.Zero, SimFixed.Zero, SimAngle.Zero);

        public SimFixed PositionX { get; }
        public SimFixed PositionY { get; }

        /// <summary>Heading as <see cref="SimAngle"/>; 0 = +x axis, increasing toward +y.</summary>
        public SimAngle Rotation { get; }

        public Transform2D(SimFixed positionX, SimFixed positionY, SimAngle rotation = default)
        {
            PositionX = positionX;
            PositionY = positionY;
            Rotation = rotation;
        }

        /// <summary>
        /// Squared distance in Q16.16 (checked — see class remarks for the
        /// value range). Prefer this over <see cref="DistanceTo"/> whenever a
        /// comparison against a squared threshold suffices.
        /// </summary>
        public SimFixed DistanceToSquared(in Transform2D other)
        {
            SimFixed dx = PositionX - other.PositionX;
            SimFixed dy = PositionY - other.PositionY;
            return dx * dx + dy * dy;
        }

        /// <summary>Exact fixed-point distance via <see cref="SimTrig.Sqrt"/>.</summary>
        public SimFixed DistanceTo(in Transform2D other)
        {
            return SimTrig.Sqrt(DistanceToSquared(in other));
        }

        public bool Equals(Transform2D other)
        {
            return PositionX == other.PositionX &&
                   PositionY == other.PositionY &&
                   Rotation == other.Rotation;
        }

        public override bool Equals(object obj) => obj is Transform2D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PositionX.RawValue;
                hash = (hash * 397) ^ PositionY.RawValue;
                hash = (hash * 397) ^ Rotation.RawValue;
                return hash;
            }
        }

        public static bool operator ==(Transform2D left, Transform2D right) => left.Equals(right);
        public static bool operator !=(Transform2D left, Transform2D right) => !left.Equals(right);

        public override string ToString() => $"Pos({PositionX}, {PositionY}), Rot({Rotation.RawValue}units)";
    }
}
