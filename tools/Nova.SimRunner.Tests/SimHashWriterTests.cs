using System;
using System.Text;
using Nova.Core;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 hash-domain suite for the canonical <see cref="SimHashWriter"/>
    /// (docs/tech/SimulationCore.md section 5): exact ASCII domain prefixes
    /// with 0x00 terminator, canonical little-endian encoding verified byte by
    /// byte against the official-vector-verified one-shot hasher, field-order
    /// sensitivity and length-prefix collision resistance.
    /// </summary>
    [TestFixture]
    public sealed class SimHashWriterTests
    {
        /// <summary>
        /// Reference digest: domain ASCII bytes + 0x00 + payload, hashed with
        /// the official-vector-verified one-shot XXH64, seed 0.
        /// </summary>
        private static ulong ExpectedDigest(byte[] domain, params byte[] payload)
        {
            var bytes = new byte[domain.Length + 1 + payload.Length];
            Array.Copy(domain, bytes, domain.Length);
            bytes[domain.Length] = 0x00;
            Array.Copy(payload, 0, bytes, domain.Length + 1, payload.Length);
            return XxHash64.ComputeHash(bytes);
        }

        [Test]
        public void DomainPrefixes_AreExactAscii()
        {
            Assert.That(
                SimHashWriter.StateDomain,
                Is.EqualTo(Encoding.ASCII.GetBytes("NOVA_STATE_V1")));
            Assert.That(
                SimHashWriter.DefinitionsDomain,
                Is.EqualTo(Encoding.ASCII.GetBytes("NOVA_DEFINITIONS_V1")));
            Assert.That(
                SimHashWriter.FileDomain,
                Is.EqualTo(Encoding.ASCII.GetBytes("NOVA_FILE_V1")));
            Assert.That(
                SimHashWriter.ReplayChainDomain,
                Is.EqualTo(Encoding.ASCII.GetBytes("NOVA_REPLAY_CHAIN_V1")));
        }

        [Test]
        public void EmptyPayload_DigestIsDomainPrefixPlusNullTerminator()
        {
            Assert.That(
                SimHashWriter.ForState().Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain)));
            Assert.That(
                SimHashWriter.ForDefinitions().Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.DefinitionsDomain)));
            Assert.That(
                SimHashWriter.ForFile().Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.FileDomain)));
            Assert.That(
                SimHashWriter.ForReplayChain().Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.ReplayChainDomain)));
        }

        [Test]
        public void SamePayload_DifferentDomains_ProduceDifferentHashes()
        {
            var digests = new System.Collections.Generic.HashSet<ulong>();
            foreach (Func<SimHashWriter> factory in new Func<SimHashWriter>[]
            {
                SimHashWriter.ForState,
                SimHashWriter.ForDefinitions,
                SimHashWriter.ForFile,
                SimHashWriter.ForReplayChain,
            })
            {
                SimHashWriter writer = factory();
                writer.WriteFieldTag(1);
                writer.WriteUInt32(42);
                Assert.That(digests.Add(writer.Digest()), Is.True, "domain collision");
            }
        }

        [Test]
        public void FieldOrder_ChangesHash()
        {
            var ordered = SimHashWriter.ForState();
            ordered.WriteFieldTag(1);
            ordered.WriteUInt32(42);
            ordered.WriteFieldTag(2);
            ordered.WriteUInt32(7);

            var swapped = SimHashWriter.ForState();
            swapped.WriteFieldTag(2);
            swapped.WriteUInt32(7);
            swapped.WriteFieldTag(1);
            swapped.WriteUInt32(42);

            Assert.That(ordered.Digest(), Is.Not.EqualTo(swapped.Digest()));
        }

        [Test]
        public void LengthPrefix_PreventsPrefixCollisions()
        {
            var first = SimHashWriter.ForState();
            first.WriteLengthPrefixed(Encoding.ASCII.GetBytes("ab"));
            first.WriteLengthPrefixed(Encoding.ASCII.GetBytes("c"));

            var second = SimHashWriter.ForState();
            second.WriteLengthPrefixed(Encoding.ASCII.GetBytes("a"));
            second.WriteLengthPrefixed(Encoding.ASCII.GetBytes("bc"));

            Assert.That(first.Digest(), Is.Not.EqualTo(second.Digest()));
        }

        [Test]
        public void Deterministic_SameSequenceSameDigest()
        {
            ulong Digest()
            {
                var writer = SimHashWriter.ForState();
                writer.WriteFieldTag(7);
                writer.WriteTick(new Tick(123));
                writer.WriteSimFixed(SimFixed.FromRaw(-123456));
                writer.WriteLengthPrefixedString("nova");
                return writer.Digest();
            }
            Assert.That(Digest(), Is.EqualTo(Digest()));
        }

        [Test]
        public void WriteUInt8_IsLittleEndianExact()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteUInt8(0xAB);
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0xAB)));
        }

        [Test]
        public void WriteUInt16_IsLittleEndianExact()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteUInt16(0x0102);
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0x02, 0x01)));
        }

        [Test]
        public void WriteUInt32_IsLittleEndianExact()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteUInt32(0x01020304);
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0x04, 0x03, 0x02, 0x01)));
        }

        [Test]
        public void WriteInt32_Negative_IsTwosComplementLittleEndian()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteInt32(-2);
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0xFE, 0xFF, 0xFF, 0xFF)));
        }

        [Test]
        public void WriteUInt64_IsLittleEndianExact()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteUInt64(0x0102030405060708UL);
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(
                    SimHashWriter.StateDomain,
                    0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01)));
        }

        [Test]
        public void WriteInt64_Negative_IsTwosComplementLittleEndian()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteInt64(-1L);
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(
                    SimHashWriter.StateDomain,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF)));
        }

        [Test]
        public void WriteFieldTag_EncodesAsUInt32()
        {
            var tagged = SimHashWriter.ForState();
            tagged.WriteFieldTag(0xDEADBEEF);
            var raw = SimHashWriter.ForState();
            raw.WriteUInt32(0xDEADBEEF);
            Assert.That(tagged.Digest(), Is.EqualTo(raw.Digest()));
        }

        [Test]
        public void WriteSimFixed_WritesRawLittleEndian()
        {
            var positive = SimHashWriter.ForState();
            positive.WriteSimFixed(SimFixed.One);
            Assert.That(
                positive.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0x00, 0x00, 0x01, 0x00)));

            var negative = SimHashWriter.ForState();
            negative.WriteSimFixed(SimFixed.FromRaw(-65536)); // exactly -1.0
            Assert.That(
                negative.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0x00, 0x00, 0xFF, 0xFF)));
        }

        [Test]
        public void WriteTick_WritesUInt32LittleEndian()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteTick(new Tick(0x01020304));
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(SimHashWriter.StateDomain, 0x04, 0x03, 0x02, 0x01)));
        }

        [Test]
        public void WriteEntityId_WritesIndexThenVersionLittleEndian()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteEntityId(new EntityId(0x01020304, 0x0506));
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(
                    SimHashWriter.StateDomain,
                    0x04, 0x03, 0x02, 0x01, 0x06, 0x05)));
        }

        [Test]
        public void WriteLengthPrefixed_WritesUInt32LengthThenBytes()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteLengthPrefixed(new byte[] { 0x11, 0x22, 0x33 });
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(
                    SimHashWriter.StateDomain,
                    0x03, 0x00, 0x00, 0x00, 0x11, 0x22, 0x33)));
        }

        [Test]
        public void WriteLengthPrefixedString_UsesUtf8WithLengthPrefix()
        {
            var writer = SimHashWriter.ForState();
            writer.WriteLengthPrefixedString("ab");
            Assert.That(
                writer.Digest(),
                Is.EqualTo(ExpectedDigest(
                    SimHashWriter.StateDomain,
                    0x02, 0x00, 0x00, 0x00, (byte)'a', (byte)'b')));
        }
    }
}
