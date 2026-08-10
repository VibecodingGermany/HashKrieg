using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Replays;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Match fingerprint unit tests (.NET lane): canonical serialization,
    /// equality, hash stability and per-field sensitivity of the
    /// SimulationCore.md section 6 fingerprint, plus parser hardening.
    /// Mirror of the EditMode lane MatchFingerprintV1Tests.
    /// </summary>
    [TestFixture]
    public sealed class MatchFingerprintTests
    {
        private static MatchFingerprint CreateStandard()
        {
            return MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeCurrentRulesHash64(),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                ReplayTestUtil.StandardSlots(),
                ReplayTestUtil.StandardFactions(),
                startSeed: 0x5EED42UL,
                initialStateHash: 0xDEADBEEFCAFEF00DUL,
                inputDelayTicks: 1);
        }

        [Test]
        public void FactionId_WireValues_AreTheManifestIndexes()
        {
            // The byte on the wire IS the manifest index: factions[0] is the
            // Alliance, factions[1] the Legion (quality/content/mvp-v1.json).
            Assert.That((byte)FactionId.Alliance, Is.EqualTo((byte)0));
            Assert.That((byte)FactionId.Legion, Is.EqualTo((byte)1));
        }

        [Test]
        public void Serialize_Parse_RoundtripsEqual_AndByteIdentical()
        {
            MatchFingerprint fingerprint = CreateStandard();
            byte[] bytes = fingerprint.Serialize();

            Assert.That(MatchFingerprint.TryParse(bytes, out MatchFingerprint parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(fingerprint));
            Assert.That(parsed.GetHashCode(), Is.EqualTo(fingerprint.GetHashCode()));
            Assert.That(parsed.Serialize(), Is.EqualTo(bytes), "reserialization must be byte-identical");

            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                PlayerSlotOccupancy expected = slot == 0 ? PlayerSlotOccupancy.Human
                    : slot == 1 ? PlayerSlotOccupancy.AI
                    : PlayerSlotOccupancy.Free;
                Assert.That(parsed.GetSlotOccupancy(slot), Is.EqualTo(expected));

                FactionId expectedFaction = slot == 1 ? FactionId.Legion : FactionId.Alliance;
                Assert.That(parsed.GetSlotFaction(slot), Is.EqualTo(expectedFaction));
            }
        }

        [Test]
        public void ComputeHash_IsStableAcrossInstances_AndStubHashesAreDistinct()
        {
            Assert.That(CreateStandard().ComputeHash(), Is.EqualTo(CreateStandard().ComputeHash()),
                "identical fingerprints must hash identically");

            ulong rules = MatchFingerprint.ComputeCurrentRulesHash64();
            ulong definitions = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions);
            ulong map = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map);
            Assert.That(definitions, Is.Not.EqualTo(rules));
            Assert.That(map, Is.Not.EqualTo(rules));
            Assert.That(map, Is.Not.EqualTo(definitions));
            Assert.That(MatchFingerprint.ComputeCurrentRulesHash64(), Is.EqualTo(rules),
                "the current rules hash must be deterministic");
            Assert.That(rules,
                Is.Not.EqualTo(MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules)),
                "D-106 rules must not match the legacy empty rules stub");
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
                Assert.That(variants[i], Is.Not.EqualTo(standard), $"variant {i} must differ");
                Assert.That(variants[i].ComputeHash(), Is.Not.EqualTo(standardHash), $"variant {i} hash must differ");
                Assert.That(standard.FindFirstDifference(variants[i]), Is.Not.Null, $"variant {i} difference");
            }
            Assert.That(standard.FindFirstDifference(CreateStandard()), Is.Null);
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

            Assert.That(foreign, Is.Not.EqualTo(standard));
            Assert.That(foreign.ComputeHash(), Is.Not.EqualTo(standard.ComputeHash()));
            Assert.That(standard.FindFirstDifference(foreign), Is.EqualTo("SlotFaction[0]"));
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
                    Assert.That(MatchFingerprint.TryParse(prefix, out _), Is.False, $"prefix {length}"));
            }

            // Trailing byte.
            var trailing = new byte[bytes.Length + 1];
            Array.Copy(bytes, trailing, bytes.Length);
            Assert.That(MatchFingerprint.TryParse(trailing, out _), Is.False);

            // Undefined slot occupancy value (first slot byte after the
            // fixed-size prefix: versions, identifiers, content hashes).
            int slotOffset = 5 * 2
                + 4 + MatchFingerprint.NumericModelIdV1.Length
                + 2
                + 4 + MatchFingerprint.PrngIdV1.Length
                + 3 * 8;
            var badSlot = (byte[])bytes.Clone();
            badSlot[slotOffset] = 3;
            Assert.That(MatchFingerprint.TryParse(badSlot, out _), Is.False);

            // Undefined slot faction value (the faction array follows the
            // eight occupancy bytes directly).
            var badFaction = (byte[])bytes.Clone();
            badFaction[slotOffset + CommandLimits.ReservedPlayerSlots] = 2;
            Assert.That(MatchFingerprint.TryParse(badFaction, out _), Is.False);

            // Non-printable-ASCII identifier byte (inside the numeric model id).
            var badIdentifier = (byte[])bytes.Clone();
            badIdentifier[5 * 2 + 4] = 0x07;
            Assert.That(MatchFingerprint.TryParse(badIdentifier, out _), Is.False);
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
