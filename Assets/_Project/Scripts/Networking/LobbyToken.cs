using System;
using System.Security.Cryptography;
using System.Text;

namespace Nova.Networking
{
    /// <summary>
    /// Short-lived, lobby-minted match tokens (D-093, sprint 14.5): an
    /// external lobby (Supabase edge function) mints one 64-bit token per
    /// match, both players present it as the Hello match token, and the
    /// relay validates it locally. Lobby and relay share exactly one static
    /// HMAC secret through configuration — there is deliberately no new
    /// channel between them, and the wire protocol
    /// (<see cref="RelayProtocol"/>, version 1) is untouched because a
    /// lobby token is just another u64 in the Hello token field.
    /// <para>
    /// Token bit layout (64 bits):
    /// <code>
    /// bits 63..44  expiry bucket (20 bits, unsigned)
    /// bits 43..32  match id      (12 bits, unsigned, lobby-chosen random)
    /// bits 31.. 0  tag           (32 bits)
    /// </code>
    /// The bucket indexes five-minute wall-clock slices since
    /// <see cref="BucketEpochMilliseconds"/>:
    /// <c>bucket = floor((unixMs - epoch) / 300000)</c>. A token validates
    /// while its bucket lies in <c>[current - 5, current]</c> — a
    /// 30-minute window. The 20-bit bucket wraps after 2^20 buckets
    /// (~9.97 years, early 2036); minting beyond the wrap is out of scope.
    /// </para>
    /// <para>
    /// Tag serialization — the lobby edge function MUST mirror this byte
    /// for byte: the HMAC input is the ASCII string
    /// <c>"NOVA-LOBBY-TOKEN-V1"</c> (19 bytes, no terminator), then the
    /// bucket as a 4-byte big-endian u32, then the match id as a 2-byte
    /// big-endian u16 (25 input bytes total). The tag is the first 4 bytes
    /// of the HMAC-SHA256 digest, read big-endian.
    /// </para>
    /// <para>
    /// Seed derivation (<see cref="DeriveSeed"/>) uses its own domain
    /// separation label <c>"NOVA-LOBBY-SEED-V1"</c> over the full 64-bit
    /// token so a tag collision can never be recycled into a seed, and the
    /// result is ORed with 1: 0 is the relay's "generate a random seed"
    /// sentinel (<see cref="RelayServerCore"/>), a lobby-derived seed must
    /// never collide with it.
    /// </para>
    /// </summary>
    public static class LobbyToken
    {
        /// <summary>Bucket epoch: 2026-01-01T00:00:00Z in unix milliseconds.</summary>
        public const long BucketEpochMilliseconds = 1_767_225_600_000L;

        /// <summary>Bucket width: five minutes in milliseconds.</summary>
        public const long BucketDurationMilliseconds = 300_000L;

        /// <summary>Validity window: the current bucket plus this many buckets back (30 minutes).</summary>
        public const int ExpiryWindowBuckets = 5;

        /// <summary>HMAC domain-separation label for the token tag (ASCII; mirrored by the lobby).</summary>
        public const string TagHmacContext = "NOVA-LOBBY-TOKEN-V1";

        /// <summary>HMAC domain-separation label for seed derivation (ASCII; mirrored by the lobby).</summary>
        public const string SeedHmacContext = "NOVA-LOBBY-SEED-V1";

        /// <summary>Largest encodable bucket (20 bits).</summary>
        public const uint MaxBucket = (1u << 20) - 1;

        /// <summary>Largest encodable match id (12 bits).</summary>
        public const ushort MaxMatchId = (1 << 12) - 1;

        private static readonly byte[] TagContextBytes = Encoding.ASCII.GetBytes(TagHmacContext);
        private static readonly byte[] SeedContextBytes = Encoding.ASCII.GetBytes(SeedHmacContext);

        /// <summary>
        /// Floor-indexed bucket of a wall-clock instant. Instants before the
        /// epoch map to negative buckets; no token can validate against them
        /// because the encoded bucket is unsigned.
        /// </summary>
        public static long BucketFromUnixMs(long unixMs)
        {
            long offset = unixMs - BucketEpochMilliseconds;
            long quotient = offset / BucketDurationMilliseconds;
            return offset >= 0 || offset % BucketDurationMilliseconds == 0
                ? quotient
                : quotient - 1;
        }

        /// <summary>Mints a token. The match id must fit the 12-bit field, the bucket the 20-bit field.</summary>
        public static ulong Mint(byte[] secret, uint bucket, ushort matchId)
        {
            if (secret == null) throw new ArgumentNullException(nameof(secret));
            if (bucket > MaxBucket) throw new ArgumentOutOfRangeException(nameof(bucket));
            if (matchId > MaxMatchId) throw new ArgumentOutOfRangeException(nameof(matchId));
            uint tag = ComputeTag(secret, bucket, matchId);
            return ((ulong)bucket << 44) | ((ulong)matchId << 32) | tag;
        }

        /// <summary>
        /// Validates a token: the tag must match (fixed-time comparison) and
        /// the bucket must lie in <c>[current - 5, current]</c>. The window
        /// check runs first — it is data-owned, not secret-dependent.
        /// </summary>
        public static bool TryValidate(ulong token, byte[] secret, long currentUnixMs, out ushort matchId)
        {
            matchId = 0;
            if (secret == null) throw new ArgumentNullException(nameof(secret));
            long bucket = (long)(token >> 44);
            long currentBucket = BucketFromUnixMs(currentUnixMs);
            if (bucket > currentBucket || currentBucket - bucket > ExpiryWindowBuckets)
            {
                return false;
            }
            ushort id = (ushort)((token >> 32) & MaxMatchId);
            uint expectedTag = ComputeTag(secret, (uint)bucket, id);
            var actualTag = new byte[4];
            WriteBigEndian32(actualTag, 0, (uint)(token & 0xFFFFFFFFu));
            var expectedTagBytes = new byte[4];
            WriteBigEndian32(expectedTagBytes, 0, expectedTag);
            if (!CryptographicOperations.FixedTimeEquals(expectedTagBytes, actualTag))
            {
                return false;
            }
            matchId = id;
            return true;
        }

        /// <summary>
        /// True once the token's bucket has fallen out of the validity
        /// window and the token can never validate again. Future-bucket
        /// tokens are NOT expired — they have not been valid yet.
        /// </summary>
        public static bool IsExpired(ulong token, long currentUnixMs)
        {
            long bucket = (long)(token >> 44);
            return bucket < BucketFromUnixMs(currentUnixMs) - ExpiryWindowBuckets;
        }

        /// <summary>
        /// Deterministic match seed for a validated token:
        /// <c>HMAC-SHA256(secret, ASCII("NOVA-LOBBY-SEED-V1") || token as 8-byte
        /// big-endian u64)</c>, first 8 digest bytes read big-endian, ORed
        /// with 1 so the result is never the relay's random-seed sentinel 0.
        /// Both match clients derive identical seeds from identical tokens.
        /// </summary>
        public static ulong DeriveSeed(byte[] secret, ulong token)
        {
            if (secret == null) throw new ArgumentNullException(nameof(secret));
            var input = new byte[SeedContextBytes.Length + 8];
            Array.Copy(SeedContextBytes, 0, input, 0, SeedContextBytes.Length);
            WriteBigEndian64(input, SeedContextBytes.Length, token);
            byte[] digest = ComputeHmacSha256(secret, input);
            return ReadBigEndian64(digest, 0) | 1UL;
        }

        /// <summary>HMAC input: ASCII context || bucket (u32 BE) || match id (u16 BE) — see class docs.</summary>
        private static uint ComputeTag(byte[] secret, uint bucket, ushort matchId)
        {
            var input = new byte[TagContextBytes.Length + 4 + 2];
            Array.Copy(TagContextBytes, 0, input, 0, TagContextBytes.Length);
            WriteBigEndian32(input, TagContextBytes.Length, bucket);
            WriteBigEndian16(input, TagContextBytes.Length + 4, matchId);
            byte[] digest = ComputeHmacSha256(secret, input);
            return ReadBigEndian32(digest, 0);
        }

        private static byte[] ComputeHmacSha256(byte[] secret, byte[] input)
        {
            using (var hmac = new HMACSHA256(secret))
            {
                return hmac.ComputeHash(input);
            }
        }

        // Big-endian primitives: the HMAC contracts are specified big-endian
        // so the Deno lobby function mirrors them with plain DataView writes.
        private static void WriteBigEndian16(byte[] dst, int offset, ushort value)
        {
            dst[offset] = (byte)(value >> 8);
            dst[offset + 1] = (byte)value;
        }

        private static void WriteBigEndian32(byte[] dst, int offset, uint value)
        {
            dst[offset] = (byte)(value >> 24);
            dst[offset + 1] = (byte)(value >> 16);
            dst[offset + 2] = (byte)(value >> 8);
            dst[offset + 3] = (byte)value;
        }

        private static void WriteBigEndian64(byte[] dst, int offset, ulong value)
        {
            WriteBigEndian32(dst, offset, (uint)(value >> 32));
            WriteBigEndian32(dst, offset + 4, (uint)value);
        }

        private static uint ReadBigEndian32(byte[] src, int offset)
        {
            return ((uint)src[offset] << 24) | ((uint)src[offset + 1] << 16)
                | ((uint)src[offset + 2] << 8) | src[offset + 3];
        }

        private static ulong ReadBigEndian64(byte[] src, int offset)
        {
            return ((ulong)ReadBigEndian32(src, offset) << 32) | ReadBigEndian32(src, offset + 4);
        }
    }
}
