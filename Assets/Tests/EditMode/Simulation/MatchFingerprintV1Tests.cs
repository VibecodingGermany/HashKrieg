using System;
using System.IO;
using System.Text.RegularExpressions;
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
                MatchFingerprint.ComputeCurrentRulesHash64(),
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

            ulong rules = MatchFingerprint.ComputeCurrentRulesHash64();
            ulong definitions = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions);
            ulong map = MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map);
            Assert.AreNotEqual(rules, definitions);
            Assert.AreNotEqual(rules, map);
            Assert.AreNotEqual(definitions, map);
            Assert.AreEqual(rules, MatchFingerprint.ComputeCurrentRulesHash64(),
                "the current rules hash must be deterministic");
            Assert.AreNotEqual(rules, MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                "D-106 rules must not match the legacy empty rules stub");
        }

        /// <summary>
        /// The failure text of every rules-revision tripwire (#138, D-120).
        /// </summary>
        private const string AppendRevisionHint =
            "A hashed rules constant changed under an existing rules revision. Append the next " +
            "RulesRevision with its own frozen literal row (MatchFingerprint, D-120) — do not " +
            "re-pin a golden hash and do not edit a frozen row.";

        [Test]
        public void RulesRevisionOneAndTwo_GoldenHashesRemainByteStable()
        {
            ulong revisionOne = MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV1);
            ulong revisionTwo = MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV2);

            // Original pins (#138, D-120): revision 1 as introduced in e1970a9
            // (HQ 2,000 / storage 2,000 / decay 25 % every 10 ticks), revision 2
            // as introduced in 8512e9f (plus repair 10 / low-power repair 5).
            // Both rows are literals now, so no later constant edit can move
            // them; a red assertion here means a frozen row was edited.
            Assert.That(revisionOne, Is.EqualTo(0x531CE8F614A16CB5UL), "revision 1 canonical stream is frozen");
            Assert.That(revisionTwo, Is.EqualTo(0x07725EA26668C9F8UL), "revision 2 canonical stream is frozen");
        }

        [Test]
        public void RulesRevisionThree_FrozenLiteral_IsTheCurrentStream()
        {
            // Revision 3 as of e4faa75 (#131, HQ 3,000). Its pre-#131 identity
            // 0x05CCA8475789AD4A is the accepted D-120 gap.
            Assert.That(MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV3),
                Is.EqualTo(0xD1B683837D12FCEFUL), "revision 3 canonical stream is frozen");
            Assert.Throws<ArgumentOutOfRangeException>(() => MatchFingerprint.ComputeRulesHash64(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MatchFingerprint.ComputeRulesHash64((ushort)(MatchFingerprint.CurrentRulesRevision + 1)));
        }

        [Test]
        public void CurrentRulesHash_MovesPastRevisionTwo_ForD104PlacementAndRepair()
        {
            ulong revisionTwo = MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV2);
            ulong current = MatchFingerprint.ComputeCurrentRulesHash64();

            Assert.That(MatchFingerprint.CurrentRulesRevision, Is.EqualTo(MatchFingerprint.RulesRevisionV3));
            Assert.That(current, Is.EqualTo(MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV3)),
                AppendRevisionHint);
            Assert.That(current, Is.EqualTo(0xD1B683837D12FCEFUL), "revision 3 canonical stream is frozen");
            Assert.That(current, Is.Not.EqualTo(revisionTwo),
                "D-104 placement and repair behavior must not share revision 2's rules identity");
        }

        [Test]
        public void CurrentLiveRules_MatchTheFrozenCurrentRevision()
        {
            // The tripwire (#138, D-120): the live constants must equal the
            // frozen literal row of the current revision, field by field. The
            // message names the moved constant instead of two opaque hashes.
            string[] drift = MatchFingerprint.DescribeCurrentRulesDrift();

            Assert.That(drift, Is.Empty, AppendRevisionHint + "\n" + string.Join("\n", drift));
            Assert.That(MatchFingerprint.ComputeCurrentRulesHash64(),
                Is.EqualTo(MatchFingerprint.ComputeRulesHash64(MatchFingerprint.CurrentRulesRevision)),
                AppendRevisionHint);
        }

        [Test]
        public void FrozenRevisions_ReadNoLiveConstants()
        {
            // Source-text guard (the NoFloatInSimulationTests pattern): the
            // frozen-row region of MatchFingerprint.cs may hold literal values
            // only. A row that reads EconomySystem/ConstructionSystem/... would
            // compile, hash identically today and silently unfreeze again with
            // the next balance change — exactly the #138 defect.
            string source = File.ReadAllText(ResolveMatchFingerprintSource());
            const string OpenMarker = "// <frozen-rules-rows>";
            const string CloseMarker = "// </frozen-rules-rows>";
            int open = source.IndexOf(OpenMarker, StringComparison.Ordinal);
            int close = source.IndexOf(CloseMarker, StringComparison.Ordinal);
            Assert.That(open, Is.GreaterThanOrEqualTo(0), "frozen-row region start marker missing");
            Assert.That(close, Is.GreaterThan(open), "frozen-row region end marker missing");
            Assert.That(source.IndexOf(OpenMarker, open + 1, StringComparison.Ordinal), Is.EqualTo(-1),
                "exactly one frozen-row region expected");

            // Comments go first: they may name live constants freely.
            string region = Regex.Replace(source.Substring(open, close - open), @"//[^\n]*", string.Empty);

            // Every row entry is RulesField.IntXX(<tag>, "<name>", <integer literal>);
            // a SimFixed field is frozen as its raw value (decimal or hex).
            int entries = Regex.Matches(region, @"RulesField\s*\.\s*Int(32|64)\s*\(").Count;
            int literalEntries = Regex.Matches(region,
                @"RulesField\s*\.\s*Int(32|64)\s*\(\s*\d+u?\s*,\s*""[^""]*""\s*,\s*-?(0[xX][0-9A-Fa-f]+|\d+)[Ll]?\s*\)").Count;
            Assert.That(entries, Is.GreaterThanOrEqualTo(4 + 6 + 13), "expected the rows of revisions 1-3");
            Assert.That(literalEntries, Is.EqualTo(entries),
                "every frozen row value must be an integer literal, never a live constant");

            // Strings blanked: no member access other than the RulesField factories.
            string code = Regex.Replace(region, @"""[^""]*""", "\"\"");
            MatchCollection access = Regex.Matches(code, @"\b(?!RulesField\b)[A-Za-z_]\w*\s*\.\s*[A-Za-z_]\w*");
            var offenders = new string[access.Count];
            for (int i = 0; i < access.Count; i++) offenders[i] = access[i].Value;
            Assert.That(offenders, Is.Empty,
                "the frozen-row region must not reference live constants: " + string.Join(", ", offenders));
        }

        /// <summary>
        /// THE ONE LANE-SPECIFIC METHOD. The .NET mirror walks up from the test
        /// binary; here the anchor is Application.dataPath (the
        /// NoFloatInSimulationTests pattern).
        /// </summary>
        private static string ResolveMatchFingerprintSource()
        {
            string scripts = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Scripts");
            Assert.That(Directory.Exists(scripts), Is.True,
                $"could not locate {scripts} — Application.dataPath is {UnityEngine.Application.dataPath}");
            return Path.Combine(scripts, "Simulation", "Replays", "MatchFingerprint.cs");
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
