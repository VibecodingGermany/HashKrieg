using System;

namespace Nova.Core
{
    /// <summary>
    /// Fast, seedable, bit-exact XorShift128+ implementation for deterministic simulation.
    /// Provides identical random sequences across platforms (ARM / x86).
    /// </summary>
    public sealed class SimRandom : ISimRandom
    {
        private ulong _s0;
        private ulong _s1;

        public ulong Seed { get; }

        public SimRandom(ulong seed)
        {
            Seed = seed;
            SetSeed(seed);
        }

        private void SetSeed(ulong seed)
        {
            // SplitMix64 generator to initialize state from a single seed
            ulong state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
            
            state = (state ^ (state >> 30)) * 0xBF58476D1CE4E5B9UL;
            state = (state ^ (state >> 27)) * 0x94D049BB133111EBUL;
            _s0 = state ^ (state >> 31);

            state = seed + 0x9E3779B97F4A7C15UL;
            state = (state ^ (state >> 30)) * 0xBF58476D1CE4E5B9UL;
            state = (state ^ (state >> 27)) * 0x94D049BB133111EBUL;
            _s1 = state ^ (state >> 31);
        }

        public uint NextUInt()
        {
            ulong x = _s0;
            ulong y = _s1;
            _s0 = y;
            x ^= x << 23;
            _s1 = x ^ y ^ (x >> 17) ^ (y >> 26);
            return (uint)((_s1 + y) >> 32);
        }

        public int NextInt(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than maxValue.");
            }

            uint range = (uint)(maxValue - minValue);
            uint value = NextUInt();
            return minValue + (int)(value % range);
        }

        public float NextFloat()
        {
            // Uniform 32-bit float in [0.0, 1.0)
            uint value = NextUInt();
            return (value >> 8) * (1.0f / 16777216.0f);
        }

        public ISimRandom Clone()
        {
            var clone = new SimRandom(Seed)
            {
                _s0 = this._s0,
                _s1 = this._s1
            };
            return clone;
        }
    }
}
