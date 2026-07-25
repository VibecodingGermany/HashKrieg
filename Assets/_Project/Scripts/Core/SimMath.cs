using System;

namespace Nova.Core
{
    /// <summary>
    /// Deterministic math abstraction wrapper for simulation calculations.
    /// Encapsulates all mathematical operations (Sqrt, Atan2, Sin, Cos, Floor, Clamp) in a single place.
    /// Per D-033 & CodingGuidelines §2.3, this wrapper uses IEEE-754 floats for the MVP phase,
    /// providing the single point of conversion for the Beta Fixed-Point (q31.32) transition.
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
    }
}
