using System;

namespace Nova.Core
{
    /// <summary>
    /// Exact managed XXH64 implementation per the xxHash specification v0.2.0
    /// (Yann Collet, https://github.com/Cyan4973/xxHash/blob/dev/doc/xxhash_spec.md),
    /// "XXH64 Algorithm Description". This is the canonical 64-bit hash for all
    /// Project Nova hash domains (docs/tech/SimulationCore.md section 5).
    /// <para>
    /// Pure managed code: no unsafe blocks, no engine references, no float/double
    /// paths. All input lanes are assembled with explicit little-endian byte
    /// shifts, so results are identical on little- and big-endian hosts. Modular
    /// 64-bit arithmetic overflow is part of the algorithm and wrapped in
    /// <c>unchecked</c> blocks.
    /// </para>
    /// <para>
    /// The one-shot <see cref="ComputeHash(ReadOnlySpan{byte}, ulong)"/> and the
    /// streaming <see cref="XxHash64State"/> are required to produce identical
    /// digests for identical byte sequences; the streaming test suites verify
    /// this across stripe boundaries. Correctness is anchored on the official
    /// xxHash sanity test vectors (tests/sanity_test_vectors.h of the xxHash
    /// repository), not on self-generated golden values.
    /// </para>
    /// </summary>
    public static class XxHash64
    {
        /// <summary>XXH64 consumes input in stripes of 32 bytes (4 lanes of 8 bytes).</summary>
        public const int StripeSize = 32;

        internal const ulong Prime64_1 = 0x9E3779B185EBCA87UL;
        internal const ulong Prime64_2 = 0xC2B2AE3D27D4EB4FUL;
        internal const ulong Prime64_3 = 0x165667B19E3779F9UL;
        internal const ulong Prime64_4 = 0x85EBCA77C2B2AE63UL;
        internal const ulong Prime64_5 = 0x27D4EB2F165667C5UL;

        /// <summary>Canonical seed for all Project Nova hash domains (SimulationCore.md section 5).</summary>
        public const ulong CanonicalSeed = 0UL;

        /// <summary>
        /// One-shot XXH64 digest of <paramref name="data"/>. The default seed 0
        /// is the canonical Project Nova seed.
        /// </summary>
        public static ulong ComputeHash(ReadOnlySpan<byte> data, ulong seed = CanonicalSeed)
        {
            unchecked
            {
                int index = 0;
                ulong acc;

                if (data.Length >= StripeSize)
                {
                    ulong acc1 = seed + Prime64_1 + Prime64_2;
                    ulong acc2 = seed + Prime64_2;
                    ulong acc3 = seed;
                    ulong acc4 = seed - Prime64_1;

                    int limit = data.Length - StripeSize;
                    do
                    {
                        acc1 = Round(acc1, ReadUInt64LittleEndian(data, index));
                        acc2 = Round(acc2, ReadUInt64LittleEndian(data, index + 8));
                        acc3 = Round(acc3, ReadUInt64LittleEndian(data, index + 16));
                        acc4 = Round(acc4, ReadUInt64LittleEndian(data, index + 24));
                        index += StripeSize;
                    }
                    while (index <= limit);

                    acc = RotateLeft(acc1, 1) + RotateLeft(acc2, 7)
                        + RotateLeft(acc3, 12) + RotateLeft(acc4, 18);
                    acc = MergeAccumulator(acc, acc1);
                    acc = MergeAccumulator(acc, acc2);
                    acc = MergeAccumulator(acc, acc3);
                    acc = MergeAccumulator(acc, acc4);
                }
                else
                {
                    // Special case: fewer than 32 bytes never touch the stripes.
                    acc = seed + Prime64_5;
                }

                acc += (ulong)data.Length;
                acc = ConsumeRemainder(data, index, acc);
                return Avalanche(acc);
            }
        }

        /// <summary>XXH64 round: acc = rotl(acc + lane * PRIME64_2, 31) * PRIME64_1.</summary>
        internal static ulong Round(ulong acc, ulong lane)
        {
            unchecked
            {
                acc += lane * Prime64_2;
                acc = RotateLeft(acc, 31);
                return acc * Prime64_1;
            }
        }

        /// <summary>XXH64 accumulator convergence merge (spec step 3).</summary>
        internal static ulong MergeAccumulator(ulong acc, ulong accN)
        {
            unchecked
            {
                acc ^= Round(0UL, accN);
                acc = acc * Prime64_1;
                return acc + Prime64_4;
            }
        }

        /// <summary>
        /// Digests up to 31 trailing bytes starting at <paramref name="offset"/>:
        /// 8-byte lanes, then a 4-byte lane, then single bytes (spec step 5).
        /// </summary>
        internal static ulong ConsumeRemainder(ReadOnlySpan<byte> data, int offset, ulong acc)
        {
            unchecked
            {
                int remaining = data.Length - offset;
                while (remaining >= 8)
                {
                    acc ^= Round(0UL, ReadUInt64LittleEndian(data, offset));
                    acc = RotateLeft(acc, 27) * Prime64_1;
                    acc += Prime64_4;
                    offset += 8;
                    remaining -= 8;
                }

                if (remaining >= 4)
                {
                    acc ^= ReadUInt32LittleEndian(data, offset) * Prime64_1;
                    acc = RotateLeft(acc, 23) * Prime64_2;
                    acc += Prime64_3;
                    offset += 4;
                    remaining -= 4;
                }

                while (remaining >= 1)
                {
                    acc ^= data[offset] * Prime64_5;
                    acc = RotateLeft(acc, 11) * Prime64_1;
                    offset += 1;
                    remaining -= 1;
                }

                return acc;
            }
        }

        /// <summary>XXH64 final mix (spec step 6).</summary>
        internal static ulong Avalanche(ulong acc)
        {
            unchecked
            {
                acc ^= acc >> 33;
                acc *= Prime64_2;
                acc ^= acc >> 29;
                acc *= Prime64_3;
                acc ^= acc >> 32;
                return acc;
            }
        }

        /// <summary>
        /// Reads 8 bytes as a little-endian uint64 with explicit shifts; the
        /// result is host-endianness independent, as the spec requires.
        /// </summary>
        internal static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> data, int offset)
        {
            return (ulong)data[offset]
                | ((ulong)data[offset + 1] << 8)
                | ((ulong)data[offset + 2] << 16)
                | ((ulong)data[offset + 3] << 24)
                | ((ulong)data[offset + 4] << 32)
                | ((ulong)data[offset + 5] << 40)
                | ((ulong)data[offset + 6] << 48)
                | ((ulong)data[offset + 7] << 56);
        }

        /// <summary>Reads 4 bytes as a little-endian uint32 with explicit shifts.</summary>
        internal static ulong ReadUInt32LittleEndian(ReadOnlySpan<byte> data, int offset)
        {
            return (ulong)data[offset]
                | ((ulong)data[offset + 1] << 8)
                | ((ulong)data[offset + 2] << 16)
                | ((ulong)data[offset + 3] << 24);
        }

        internal static ulong RotateLeft(ulong value, int bits)
            => (value << bits) | (value >> (64 - bits));
    }

    /// <summary>
    /// Incremental XXH64 state for large snapshots. Buffers up to 31 bytes so
    /// stripes are always processed whole; <see cref="Digest"/> must return
    /// exactly the same value as a one-shot
    /// <see cref="XxHash64.ComputeHash(ReadOnlySpan{byte}, ulong)"/> over the
    /// concatenation of all <see cref="Update"/> inputs.
    /// </summary>
    public sealed class XxHash64State
    {
        private readonly ulong _seed;
        private readonly byte[] _buffer = new byte[XxHash64.StripeSize];
        private ulong _acc1;
        private ulong _acc2;
        private ulong _acc3;
        private ulong _acc4;
        private long _totalLength;
        private int _bufferLength;

        public XxHash64State(ulong seed = XxHash64.CanonicalSeed)
        {
            _seed = seed;
            unchecked
            {
                _acc1 = seed + XxHash64.Prime64_1 + XxHash64.Prime64_2;
                _acc2 = seed + XxHash64.Prime64_2;
                _acc3 = seed;
                _acc4 = seed - XxHash64.Prime64_1;
            }
        }

        /// <summary>Feeds the next chunk. Chunks may have any size and alignment.</summary>
        public void Update(ReadOnlySpan<byte> data)
        {
            _totalLength += data.Length;

            if (_bufferLength + data.Length < XxHash64.StripeSize)
            {
                // Not enough bytes for a stripe yet; keep buffering.
                data.CopyTo(new Span<byte>(_buffer, _bufferLength, data.Length));
                _bufferLength += data.Length;
                return;
            }

            int index = 0;
            if (_bufferLength > 0)
            {
                // Complete the partial stripe held in the buffer first.
                int needed = XxHash64.StripeSize - _bufferLength;
                data.Slice(0, needed).CopyTo(new Span<byte>(_buffer, _bufferLength, needed));
                ProcessStripe(_buffer, 0);
                index += needed;
                _bufferLength = 0;
            }

            int limit = data.Length - XxHash64.StripeSize;
            while (index <= limit)
            {
                ProcessStripe(data, index);
                index += XxHash64.StripeSize;
            }

            if (index < data.Length)
            {
                int tail = data.Length - index;
                data.Slice(index, tail).CopyTo(new Span<byte>(_buffer, 0, tail));
                _bufferLength = tail;
            }
        }

        /// <summary>
        /// Finalizes and returns the digest. Identical to the one-shot result
        /// over the same byte sequence. The state must not be reused afterwards.
        /// </summary>
        public ulong Digest()
        {
            unchecked
            {
                ulong acc;
                if (_totalLength >= XxHash64.StripeSize)
                {
                    acc = XxHash64.RotateLeft(_acc1, 1) + XxHash64.RotateLeft(_acc2, 7)
                        + XxHash64.RotateLeft(_acc3, 12) + XxHash64.RotateLeft(_acc4, 18);
                    acc = XxHash64.MergeAccumulator(acc, _acc1);
                    acc = XxHash64.MergeAccumulator(acc, _acc2);
                    acc = XxHash64.MergeAccumulator(acc, _acc3);
                    acc = XxHash64.MergeAccumulator(acc, _acc4);
                }
                else
                {
                    // Fewer than 32 bytes total: stripes never ran (spec step 1).
                    acc = _seed + XxHash64.Prime64_5;
                }

                acc += (ulong)_totalLength;
                acc = XxHash64.ConsumeRemainder(
                    new ReadOnlySpan<byte>(_buffer, 0, _bufferLength), 0, acc);
                return XxHash64.Avalanche(acc);
            }
        }

        private void ProcessStripe(ReadOnlySpan<byte> data, int offset)
        {
            _acc1 = XxHash64.Round(_acc1, XxHash64.ReadUInt64LittleEndian(data, offset));
            _acc2 = XxHash64.Round(_acc2, XxHash64.ReadUInt64LittleEndian(data, offset + 8));
            _acc3 = XxHash64.Round(_acc3, XxHash64.ReadUInt64LittleEndian(data, offset + 16));
            _acc4 = XxHash64.Round(_acc4, XxHash64.ReadUInt64LittleEndian(data, offset + 24));
        }
    }
}
