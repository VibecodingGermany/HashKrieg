using System;
using Nova.Core;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 hash-domain suite for the canonical XXH64 hasher
    /// (docs/tech/SimulationCore.md section 5). Correctness is anchored on the
    /// official xxHash sanity test vectors from the xxHash reference
    /// repository (tests/sanity_test_vectors.h, dev branch) using the official
    /// sanity buffer generator (XSUM_fillTestBuffer in
    /// cli/xsum_sanity_check.c: byteGen starts at PRIME32 2654435761, each byte
    /// is byteGen &gt;&gt; 56, byteGen multiplied by PRIME64
    /// 11400714785074694797 per step). Self-generated values are used only for
    /// streaming parity checks, never as correctness proof.
    /// </summary>
    [TestFixture]
    public sealed class XxHash64Tests
    {
        private const ulong SanityPrime32 = 2654435761UL;
        private const ulong SanityPrime64 = 11400714785074694797UL;

        /// <summary>Official xxHash sanity buffer generator (must not be changed).</summary>
        private static byte[] CreateSanityBuffer(int length)
        {
            var buffer = new byte[length];
            ulong byteGen = SanityPrime32;
            for (int i = 0; i < length; i++)
            {
                buffer[i] = (byte)(byteGen >> 56);
                unchecked
                {
                    byteGen *= SanityPrime64;
                }
            }
            return buffer;
        }

        // Official XSUM_XXH64_testdata entries (xxHash repo,
        // tests/sanity_test_vectors.h): { len, seed, Nresult }.
        [TestCase(0, 0x0UL, 0xEF46DB3751D8E999UL)]         // testdata[0]
        [TestCase(0, 0x9E3779B1UL, 0xAC75FDA2929B17EFUL)]  // testdata[1]
        [TestCase(1, 0x0UL, 0xE934A84ADB052768UL)]         // testdata[2]
        [TestCase(1, 0x9E3779B1UL, 0x5014607643A9B4C3UL)]  // testdata[3]
        [TestCase(4, 0x0UL, 0x9136A0DCA57457EEUL)]         // testdata[8]
        [TestCase(14, 0x0UL, 0x8282DCC4994E35C8UL)]        // testdata[28]
        [TestCase(16, 0x9E3779B1UL, 0xC900AD2D536B607EUL)] // testdata[33]
        [TestCase(32, 0x0UL, 0x18B216492BB44B70UL)]        // testdata[64]  one full stripe
        [TestCase(33, 0x0UL, 0x55C8DC3E578F5B59UL)]        // testdata[66]  stripe + 1 remainder byte
        [TestCase(64, 0x0UL, 0xEF558F8ACAC2B5CDUL)]        // testdata[128] two stripes
        [TestCase(222, 0x0UL, 0xB641AE8CB691C174UL)]       // testdata[444]
        [TestCase(222, 0x9E3779B1UL, 0x20CB8AB7AE10C14AUL)]// testdata[445]
        [TestCase(2367, 0x0UL, 0xA82418DDEC0EA581UL)]      // testdata[4734] full sanity buffer
        [TestCase(2367, 0x9E3779B1UL, 0xA36A93C18052673AUL)]// testdata[4735]
        public void OfficialSanityVectors_MatchReference(int length, ulong seed, ulong expected)
        {
            byte[] buffer = CreateSanityBuffer(length);
            Assert.That(XxHash64.ComputeHash(buffer, seed), Is.EqualTo(expected));
        }

        [Test]
        public void ComputeHash_DefaultSeed_IsCanonicalSeed0()
        {
            byte[] buffer = CreateSanityBuffer(64);
            Assert.That(
                XxHash64.ComputeHash(buffer),
                Is.EqualTo(XxHash64.ComputeHash(buffer, XxHash64.CanonicalSeed)));
            Assert.That(
                XxHash64.ComputeHash(ReadOnlySpan<byte>.Empty),
                Is.EqualTo(0xEF46DB3751D8E999UL));
        }

        [Test]
        public void Streaming_MatchesOneShot_AllLengths0To200_AllChunkings()
        {
            byte[] data = CreateSanityBuffer(200);
            int[] chunkSizes = { 1, 3, 7, 13, 31, 32, 33, 64, 200 };
            for (int length = 0; length <= 200; length++)
            {
                ulong expected = XxHash64.ComputeHash(new ReadOnlySpan<byte>(data, 0, length));
                foreach (int chunkSize in chunkSizes)
                {
                    var state = new XxHash64State();
                    for (int offset = 0; offset < length; offset += chunkSize)
                    {
                        int count = Math.Min(chunkSize, length - offset);
                        state.Update(new ReadOnlySpan<byte>(data, offset, count));
                    }
                    Assert.That(
                        state.Digest(),
                        Is.EqualTo(expected),
                        $"streaming diverged at length {length}, chunk size {chunkSize}");
                }
            }
        }

        // Odd stripe boundaries: 31/32/33 (first stripe), 64/65 (second stripe).
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(64)]
        [TestCase(65)]
        public void Streaming_StripeBoundaries_MatchOneShot(int length)
        {
            byte[] data = CreateSanityBuffer(length);
            ulong expected = XxHash64.ComputeHash(data);

            // Split exactly at the boundary plus an unaligned split at byte 9.
            foreach (int split in new[] { 9, length / 2 })
            {
                var state = new XxHash64State();
                state.Update(new ReadOnlySpan<byte>(data, 0, split));
                state.Update(new ReadOnlySpan<byte>(data, split, length - split));
                Assert.That(state.Digest(), Is.EqualTo(expected), $"split {split}, length {length}");
            }
        }

        [Test]
        public void Streaming_OfficialVector_LargeBuffer_MatchesReference()
        {
            // Anchors the streaming path itself on the official 2367-byte
            // vector (testdata[4734]), fed in awkward 100-byte chunks.
            byte[] data = CreateSanityBuffer(2367);
            var state = new XxHash64State();
            for (int offset = 0; offset < data.Length; offset += 100)
            {
                int count = Math.Min(100, data.Length - offset);
                state.Update(new ReadOnlySpan<byte>(data, offset, count));
            }
            Assert.That(state.Digest(), Is.EqualTo(0xA82418DDEC0EA581UL));
        }

        [Test]
        public void Streaming_WithSeed_MatchesSeededOneShot()
        {
            byte[] data = CreateSanityBuffer(97);
            ulong expected = XxHash64.ComputeHash(data, 0x9E3779B1UL);
            var state = new XxHash64State(0x9E3779B1UL);
            state.Update(new ReadOnlySpan<byte>(data, 0, 17));
            state.Update(new ReadOnlySpan<byte>(data, 17, data.Length - 17));
            Assert.That(state.Digest(), Is.EqualTo(expected));
        }
    }
}
