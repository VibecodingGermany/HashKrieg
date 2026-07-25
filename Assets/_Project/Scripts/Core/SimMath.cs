using System;

namespace Nova.Core
{
    /// <summary>
    /// Deterministic math abstraction wrapper for simulation calculations.
    /// Encapsulates all mathematical operations (Sqrt, Atan2, Sin, Cos, Floor, Clamp) in a single place.
    /// Per D-033 & CodingGuidelines §2.3, this wrapper uses IEEE-754 floats for the MVP phase,
    /// providing the single point of conversion for the Beta Fixed-Point (q31.32) transition.
    /// <para>
    /// Status after the Q-040(i) SimFixed migration: nothing authoritative
    /// uses the float paths anymore — the canonical movement path computes
    /// in <see cref="SimFixed"/>/<see cref="SimAngle"/> via the purely
    /// integer <see cref="SimTrig"/>. Only the integer <see cref="Clamp(int, int, int)"/>
    /// overload still has callers (prototype scaffolding and the command
    /// adapter); the float transcendentals, the float Clamp overload, Floor
    /// helpers and the float bit converters are caller-less and stay until
    /// the remaining domain slices migrate.
    /// </para>
    /// </summary>
    public static class SimMath
    {
        public const float PI = 3.14159265358979323846f;
        public const float Deg2Rad = PI / 180.0f;
        public const float Rad2Deg = 180.0f / PI;

        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sin(float radians) => (float)Math.Sin(radians);
        public static float Cos(float radians) => (float)Math.Cos(radians);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static ushort FloorToUShort(float value) => (ushort)Math.Max(0, Math.Min(ushort.MaxValue, (int)Math.Floor(value)));
        public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));

        public static uint SingleToUInt32Bits(float value)
        {
            return (uint)BitConverter.SingleToInt32Bits(value);
        }

        public static float UInt32BitsToSingle(uint value)
        {
            return BitConverter.Int32BitsToSingle(unchecked((int)value));
        }
    }
}
