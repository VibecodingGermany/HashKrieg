using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Replays;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Match fingerprint unit tests (EditMode lane): canonical serialization,
    /// equality, hash stability and per-field sensitivity of the
    /// SimulationCore.md section 6 fingerprint, plus parser hardening.
    /// Mirror of the .NET lane MatchFingerprintTests.
    /// </summary>
    [TestFixture]
    public class MatchFingerprintV1Tests
    {
        private static MatchFingerprint CreateStandard()
        {
            return MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                ReplayV1TestUtil.StandardSlots(),
                ReplayV1TestUtil.StandardFactions(),
                startSeed: 0x5EED42UL,
                initialStateHash: 0xDEADBEEFCAFEF00DUL,
                inputDelayTicks: 1);
        }

        [Test]
        public void FactionId_WireValues_AreTheManifestIndexes()
        {
            // The byte on the wire IS the manifest index: factions[0] is the
            // Alliance, factions[1] the Legion (quality/content/mvp-v1.json).
            Assert.AreEqual((byte)0, (byte)FactionId.Alliance);
            Assert.AreEqual((byte)1, (byte)FactionId.Legion);
        }

        [Test]
        public void Serialize_Parse_RoundtripsEqual_AndByteIdentical()
        {
            MatchFingerprint fingerprint = CreateStandard();
            byte[] bytes = fingerprint.Serialize();

            Assert.IsTrue(MatchFingerprint.TryParse(bytes, out MatchFingerprint parsed));
            Assert.AreEqual(fingerprint, parsed);
            Assert.AreEqual(fingerprint.GetHashCode(), parsed.GetHashCode());
            Assert.AreEqual(bytes, parsed.Serialize(), "reserialization must be byte-identical");

            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                PlayerSlotOccupancy expected = slot == 0 ? PlayerSlotOccupancy.Human
                    : slot == 1 ? PlayerSlotOccupancy.AI
                    : PlayerSlotOccupancy.Free;
                Assert.AreEqual(expected, parsed.GetSlotOccupancy(slot));

                FactionId expectedFaction = slot == 1 ? FactionId.Legion : FactionId.Alliance;
                Assert.AreEqual(expectedFaction, parsed.GetSlotFaction(slot));
            }
        }

        [Test]
        public void ComputeHash_IsStableAcrossInstances_AndStubHashesAreDistinct()
        {
            Assert.AreEqual(CreateStandard().ComputeHash(), CreateStandard().ComputeHash(),
                "identical fingerprints must hash identically");

            ulong rules = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules);
            ulong definitions = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions);
            ulong map = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map);
            Assert.AreNotEqual(rules, definitions);
            Assert.AreNotEqual(rules, map);
            Assert.AreNotEqual(definitions, map);
            Assert.AreEqual(rules, MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                "stub hashes must be deterministic");
        }

        [Test]
        public void ComputeHash_AndEquality_AreSensitiveToEveryField()
        {
            MatchFingerprint standard = CreateStandard();
            ulong standardHash = standard.ComputeHash();

            MatchFingerprint[] variants =
            {
                new MatchFingerprint(
                    2, standard.CommandSchemaVersion, standard.PayloadSchemaVersion,
                    standard.SnapshotSchemaVersion, standard.SidecarSchemaVersion,
                    standard.NumericModelId, standard.TicksPerSecond, standard.PrngId,
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64 ^ 1, standard.DefinitionsHash64, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64 ^ 1, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64 ^ 1,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                    new byte[] { 1, 1, 0, 0, 0, 0, 0, 0 }, standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed + 1,
                    standard.InitialStateHash, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash ^ 1, standard.InputDelayTicks),
                MatchFingerprint.CreateCurrent(
                    standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                    standard.GetSlotOccupancyCopy(), standard.GetSlotFactionCopy(), standard.StartSeed,
                    standard.InitialStateHash, standard.InputDelayTicks + 1),
            };

            for (int i = 0; i < variants.Length; i++)
            {
                Assert.AreNotEqual(standard, variants[i], $"variant {i} must differ");
                Assert.AreNotEqual(standardHash, variants[i].ComputeHash(), $"variant {i} hash must differ");
                Assert.NotNull(standard.FindFirstDifference(variants[i]), $"variant {i} difference");
            }
            Assert.IsNull(standard.FindFirstDifference(CreateStandard()));
        }

        [Test]
        public void ComputeHash_NamesTheFactionField_WhenOnlyTheFactionDiffers()
        {
            // The faction assignment is match identity: swapping the two
            // active slots' factions must change the fingerprint hash AND be
            // named as the first difference — a Legion-vs-Legion replay must
            // never start against an Alliance-vs-Legion fingerprint.
            MatchFingerprint standard = CreateStandard();
            byte[] swapped = standard.GetSlotFactionCopy();
            swapped[0] = (byte)FactionId.Legion;
            swapped[1] = (byte)FactionId.Alliance;

            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                standard.RulesHash64, standard.DefinitionsHash64, standard.MapHash64,
                standard.GetSlotOccupancyCopy(), swapped, standard.StartSeed,
                standard.InitialStateHash, standard.InputDelayTicks);

            Assert.AreNotEqual(standard, foreign);
            Assert.AreNotEqual(standard.ComputeHash(), foreign.ComputeHash());
            Assert.AreEqual("SlotFaction[0]", standard.FindFirstDifference(foreign));
        }

        [Test]
        public void TryParse_RejectsTruncationTrailingBytesAndBadFields()
        {
            byte[] bytes = CreateStandard().Serialize();

            // Truncation loop: every strict prefix is rejected without throwing.
            for (int length = 0; length < bytes.Length; length++)
            {
                var prefix = new byte[length];
                Array.Copy(bytes, prefix, length);
                Assert.DoesNotThrow(() =>
                    Assert.IsFalse(MatchFingerprint.TryParse(prefix, out _), $"prefix {length}"));
            }

            // Trailing byte.
            var trailing = new byte[bytes.Length + 1];
            Array.Copy(bytes, trailing, bytes.Length);
            Assert.IsFalse(MatchFingerprint.TryParse(trailing, out _));

            // Undefined slot occupancy value (first slot byte after the
            // fixed-size prefix: versions, identifiers, content hashes).
            int slotOffset = 5 * 2
                + 4 + MatchFingerprint.NumericModelIdV1.Length
                + 2
                + 4 + MatchFingerprint.PrngIdV1.Length
                + 3 * 8;
            var badSlot = (byte[])bytes.Clone();
            badSlot[slotOffset] = 3;
            Assert.IsFalse(MatchFingerprint.TryParse(badSlot, out _));

            // Undefined slot faction value (the faction array follows the
            // eight occupancy bytes directly).
            var badFaction = (byte[])bytes.Clone();
            badFaction[slotOffset + CommandLimits.ReservedPlayerSlots] = 2;
            Assert.IsFalse(MatchFingerprint.TryParse(badFaction, out _));

            // Non-printable-ASCII identifier byte (inside the numeric model id).
            var badIdentifier = (byte[])bytes.Clone();
            badIdentifier[5 * 2 + 4] = 0x07;
            Assert.IsFalse(MatchFingerprint.TryParse(badIdentifier, out _));
        }

        [Test]
        public void Constructor_AndAccess_EnforceBounds()
        {
            MatchFingerprint fingerprint = CreateStandard();
            Assert.Throws<ArgumentOutOfRangeException>(() => fingerprint.GetSlotOccupancy(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => fingerprint.GetSlotOccupancy(8));
            Assert.Throws<ArgumentOutOfRangeException>(() => fingerprint.GetSlotFaction(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => fingerprint.GetSlotFaction(8));
            Assert.Throws<ArgumentException>(() => MatchFingerprint.CreateCurrent(
                0, 0, 0, new byte[7], new byte[8], 0, 0, 1));
            Assert.Throws<ArgumentException>(() => MatchFingerprint.CreateCurrent(
                0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 9 }, new byte[8], 0, 0, 1));
            Assert.Throws<ArgumentException>(() => MatchFingerprint.CreateCurrent(
                0, 0, 0, new byte[8], new byte[7], 0, 0, 1));
            Assert.Throws<ArgumentException>(() => MatchFingerprint.CreateCurrent(
                0, 0, 0, new byte[8], new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 }, 0, 0, 1));
            Assert.Throws<ArgumentNullException>(() => MatchFingerprint.CreateCurrent(
                0, 0, 0, null, new byte[8], 0, 0, 1));
            Assert.Throws<ArgumentNullException>(() => MatchFingerprint.CreateCurrent(
                0, 0, 0, new byte[8], null, 0, 0, 1));
        }
    }
}
