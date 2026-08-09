using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nova.Networking;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Lobby-token codec (D-093, sprint 14.5): mint/validate roundtrips,
    /// window and tag rejection, bucket arithmetic, expiry and seed
    /// derivation. All instants are fixed — no wall-clock dependency.
    /// </summary>
    [TestFixture]
    public sealed class LobbyTokenTests
    {
        private static readonly byte[] Secret = CreateSecret(0x11);
        private static readonly byte[] OtherSecret = CreateSecret(0x77);

        // Fixed wall instant exactly on the boundary of bucket 500.
        private const long NowMs =
            LobbyToken.BucketEpochMilliseconds + 500 * LobbyToken.BucketDurationMilliseconds;
        private const uint CurrentBucket = 500;

        private static byte[] CreateSecret(byte first)
        {
            var secret = new byte[32];
            for (int i = 0; i < secret.Length; i++)
            {
                secret[i] = (byte)(first + i);
            }
            return secret;
        }

        [Test]
        public void Mint_ThenValidate_RoundTripsTheMatchId()
        {
            ulong token = LobbyToken.Mint(Secret, CurrentBucket, 0xABC);

            Assert.That(
                LobbyToken.TryValidate(token, Secret, NowMs, out ushort matchId), Is.True);
            Assert.That(matchId, Is.EqualTo(0xABC));
        }

        [Test]
        public void Mint_EncodesTheDocumentedBitLayout()
        {
            ulong token = LobbyToken.Mint(Secret, 0x12345, 0x123);

            Assert.That((uint)(token >> 44), Is.EqualTo(0x12345), "bucket field");
            Assert.That((uint)((token >> 32) & 0xFFF), Is.EqualTo(0x123), "match-id field");
        }

        [Test]
        public void Mint_RejectsOutOfRangeFieldsAndNullSecrets()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LobbyToken.Mint(Secret, LobbyToken.MaxBucket + 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LobbyToken.Mint(Secret, 0, (ushort)(LobbyToken.MaxMatchId + 1)));
            Assert.Throws<ArgumentNullException>(() => LobbyToken.Mint(null, 0, 0));
        }

        [Test]
        public void Validate_RejectsAWrongSecret()
        {
            ulong token = LobbyToken.Mint(Secret, CurrentBucket, 0xABC);

            Assert.That(
                LobbyToken.TryValidate(token, OtherSecret, NowMs, out ushort matchId), Is.False);
            Assert.That(matchId, Is.Zero);
        }

        [Test]
        public void Validate_AcceptsTheWholeLookbackWindow_AndNoOlder()
        {
            ulong edgeToken = LobbyToken.Mint(Secret, CurrentBucket - 5, 0x001);
            ulong oldToken = LobbyToken.Mint(Secret, CurrentBucket - 6, 0x002);

            Assert.That(LobbyToken.TryValidate(edgeToken, Secret, NowMs, out _), Is.True,
                "the bucket five slices back is still inside the 30-minute window");
            Assert.That(LobbyToken.TryValidate(oldToken, Secret, NowMs, out _), Is.False,
                "six buckets back lies outside the window");
        }

        [Test]
        public void Validate_RejectsFutureBuckets()
        {
            ulong futureToken = LobbyToken.Mint(Secret, CurrentBucket + 1, 0x003);

            Assert.That(LobbyToken.TryValidate(futureToken, Secret, NowMs, out _), Is.False);
        }

        [Test]
        public void Validate_TracksTheWindowAsWallTimeAdvances()
        {
            ulong token = LobbyToken.Mint(Secret, CurrentBucket, 0x007);

            Assert.That(
                LobbyToken.TryValidate(token, Secret,
                    NowMs + LobbyToken.BucketDurationMilliseconds - 1, out _),
                Is.True, "same bucket, late instant");
            Assert.That(
                LobbyToken.TryValidate(token, Secret,
                    NowMs + 5 * LobbyToken.BucketDurationMilliseconds, out _),
                Is.True, "exactly at the window edge");
            Assert.That(
                LobbyToken.TryValidate(token, Secret,
                    NowMs + 6 * LobbyToken.BucketDurationMilliseconds, out _),
                Is.False, "one bucket past the window");
        }

        [TestCase(0)]   // lowest tag bit
        [TestCase(31)]  // highest tag bit
        [TestCase(33)]  // a match-id bit — the tag no longer matches the id
        public void Validate_RejectsSingleBitFlips(int bit)
        {
            ulong token = LobbyToken.Mint(Secret, CurrentBucket, 0x555);
            ulong flipped = token ^ (1UL << bit);

            Assert.That(LobbyToken.TryValidate(flipped, Secret, NowMs, out _), Is.False);
        }

        [Test]
        public void Validate_RejectsTheZeroToken()
        {
            Assert.That(LobbyToken.TryValidate(0UL, Secret, NowMs, out _), Is.False);
        }

        [Test]
        public void BucketFromUnixMs_FloorIndexesFiveMinuteSlices()
        {
            Assert.That(LobbyToken.BucketFromUnixMs(LobbyToken.BucketEpochMilliseconds),
                Is.Zero);
            Assert.That(LobbyToken.BucketFromUnixMs(
                    LobbyToken.BucketEpochMilliseconds + LobbyToken.BucketDurationMilliseconds - 1),
                Is.Zero);
            Assert.That(LobbyToken.BucketFromUnixMs(
                    LobbyToken.BucketEpochMilliseconds + LobbyToken.BucketDurationMilliseconds),
                Is.EqualTo(1));
            Assert.That(LobbyToken.BucketFromUnixMs(LobbyToken.BucketEpochMilliseconds - 1),
                Is.EqualTo(-1), "instants before the epoch floor to negative buckets");
        }

        [Test]
        public void IsExpired_TracksTheWindowEdge_AndIgnoresFutureBuckets()
        {
            ulong current = LobbyToken.Mint(Secret, CurrentBucket, 0x010);
            ulong windowEdge = LobbyToken.Mint(Secret, CurrentBucket - 5, 0x011);
            ulong old = LobbyToken.Mint(Secret, CurrentBucket - 6, 0x012);
            ulong future = LobbyToken.Mint(Secret, CurrentBucket + 3, 0x013);

            Assert.That(LobbyToken.IsExpired(current, NowMs), Is.False);
            Assert.That(LobbyToken.IsExpired(windowEdge, NowMs), Is.False);
            Assert.That(LobbyToken.IsExpired(old, NowMs), Is.True);
            Assert.That(LobbyToken.IsExpired(future, NowMs), Is.False,
                "a future-bucket token has not been valid yet — it is not expired");
            Assert.That(
                LobbyToken.IsExpired(current, NowMs + 5 * LobbyToken.BucketDurationMilliseconds),
                Is.False);
            Assert.That(
                LobbyToken.IsExpired(current, NowMs + 6 * LobbyToken.BucketDurationMilliseconds),
                Is.True);
        }

        [Test]
        public void DeriveSeed_IsDeterministic_NonZero_AndTokenSpecific()
        {
            var seeds = new HashSet<ulong>();
            for (ushort id = 0; id < 1000; id++)
            {
                ulong token = LobbyToken.Mint(Secret, CurrentBucket, id);
                ulong seed = LobbyToken.DeriveSeed(Secret, token);

                Assert.That(seed, Is.Not.Zero);
                Assert.That(seed & 1UL, Is.EqualTo(1UL), "the seed is pinned odd, never the 0 sentinel");
                Assert.That(LobbyToken.DeriveSeed(Secret, token), Is.EqualTo(seed),
                    "same input, same seed");
                Assert.That(seeds.Add(seed), Is.True, $"seed collision at match id {id}");
            }
        }

        [Test]
        public void DeriveSeed_DependsOnTheSecret_AndRejectsNullSecrets()
        {
            ulong token = LobbyToken.Mint(Secret, CurrentBucket, 0x0F0);

            Assert.That(LobbyToken.DeriveSeed(OtherSecret, token),
                Is.Not.EqualTo(LobbyToken.DeriveSeed(Secret, token)));
            Assert.Throws<ArgumentNullException>(() => LobbyToken.DeriveSeed(null, token));
        }
    }
}
