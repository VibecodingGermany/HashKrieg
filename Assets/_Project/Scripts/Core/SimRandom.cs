using System;

namespace Nova.Core
{
    /// <summary>
    /// Fast, seedable, bit-exact XorShift128+ implementation for deterministic simulation.
    /// Provides identical random sequences across platforms (ARM / x86).
    /// <para>
    /// Implements <c>XorShift128PlusV1</c> per docs/tech/SimulationCore.md section 1:
    /// two uint64 state words, canonical xorshift128+ transitions with the
    /// (23, 17, 26) shift triple. The spec fixes neither seeding nor the 64-to-32
    /// bit output reduction; both are implementation details pinned by the
    /// golden-vector tests (SimRandomGoldenTests). Seeding uses canonical
    /// SplitMix64 with a running state advanced per word (seed 0 included,
    /// non-degenerate); <see cref="NextUInt"/> emits the high 32 bits of the
    /// xorshift128+ sum output.
    /// </para>
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
            // Canonical SplitMix64 with a single running state: each state word
            // comes from a consecutive SplitMix64 draw (state advances by the
            // golden gamma per word), so s0 and s1 are statistically
            // independent. Seed 0 yields a well-defined non-degenerate state
            // (xorshift128+ only requires not both words to be zero).
            ulong state = seed;
            state += 0x9E3779B97F4A7C15UL;
            _s0 = SplitMix64Mix(state);
            state += 0x9E3779B97F4A7C15UL;
            _s1 = SplitMix64Mix(state);
        }

        private static ulong SplitMix64Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
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

        /// <summary>
        /// Reads the two authoritative PRNG state words without advancing the
        /// sequence (docs/tech/SimulationCore.md sections 1 and 3: the PRNG
        /// words are part of the canonical state and of snapshots). Not part
        /// of <see cref="ISimRandom"/> on purpose — the interface stays
        /// unchanged; state access lives on the concrete class (Q-040(d)).
        /// </summary>
        public void GetState(out ulong s0, out ulong s1)
        {
            s0 = _s0;
            s1 = _s1;
        }

        /// <summary>
        /// Restores previously read state words so the sequence continues
        /// exactly where it left off. Both words zero is the degenerate
        /// xorshift128+ state (it would emit zeros forever) and is rejected as
        /// a deterministic, checked error (SimulationCore.md section 1).
        /// </summary>
        public void SetState(ulong s0, ulong s1)
        {
            if (s0 == 0UL && s1 == 0UL)
            {
                throw new ArgumentException("Degenerate xorshift128+ state (both words zero).");
            }
            _s0 = s0;
            _s1 = s1;
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
