using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Replays;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// G1 replay suite (EditMode lane): the canonical replay contract of
    /// docs/tech/SimulationCore.md section 8 — golden playback over the
    /// standard 50-tick match, hash-chain tamper detection, fingerprint
    /// refusal (section 6), state-dependent rejections staying in the stream
    /// and playback without re-instantiating the AI (section 4).
    /// Mirror of the .NET lane ReplayTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class ReplayV1Tests
    {
        [Test]
        public void GoldenReplay_PlaybackReproducesEndHashAndRecordedResults()
        {
            // Section 8 golden case: the standard match (human slot 0,
            // recorded slot-1 AI records, 50 ticks, Moves plus a
            // state-dependently rejected command) plays back through the
            // same kernel and sources and reproduces the recording exactly.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();

            Assert.IsTrue(
                ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out ReplayReadError readError),
                $"parse failed: {readError}");
            Assert.AreEqual(ReplayV1TestUtil.MatchTicks, replay.Frames.Length,
                "every tick is recorded, empty ones included");
            Assert.AreEqual(live.Fingerprint, replay.Fingerprint);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsTrue(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, live.Fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                $"playback failed: {error} ({detail})");

            Assert.AreEqual((uint)ReplayV1TestUtil.MatchTicks, playback.Kernel.CurrentTick.Value);
            Assert.AreEqual(live.EndStateHash, playback.Kernel.CalculateStateHash(),
                "playback end state hash must equal the recorded one");
        }

        [Test]
        public void StateDependentRejection_StaysInStream_AndIsReproducedExactly()
        {
            // Commands.md section 4 / SimulationCore.md section 8: the tick-10
            // command (human orders an AI-owned unit) is structurally valid,
            // fails only state-dependently and therefore stays in the stream
            // with its deterministic RejectedNotOwned result; playback
            // re-executes it to the identical result.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));

            ReplayTickFrame frame10 = replay.Frames[9];
            Assert.AreEqual(10u, frame10.Tick);
            Assert.AreEqual(1, frame10.RecordCount);
            Assert.AreEqual(ReplayV1TestUtil.HumanSlot, frame10.Records[0].PlayerSlot);
            Assert.AreEqual(CommandKind.Move, frame10.Records[0].Kind);
            Assert.AreEqual(CommandResultCode.RejectedNotOwned, frame10.ResultCodes[0],
                "the state-dependent rejection must be recorded, not dropped");

            // A forged recording (the same tick-10 rejection recorded as
            // Applied, chain-consistent) must fail playback at exactly the
            // result comparison: re-execution yields RejectedNotOwned.
            ReplayV1TestUtil.LiveMatch forged = ReplayV1TestUtil.RunLiveMatch(
                forgeResultAtTick: 10, forgedCode: CommandResultCode.Applied);
            Assert.IsTrue(
                ReplayFile.TryParse(forged.ReplayBytes, out _, out ReplayReadError forgeParse),
                $"forged replay must stay parseable: {forgeParse}");

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    forged.ReplayBytes, forged.Fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.ResultMismatch, error, detail);
            StringAssert.Contains("tick 10", detail);
        }

        [Test]
        public void TamperedRecordPayload_InMiddleFrame_FailsChainAtThatTick()
        {
            // Section 8 hash chain: flipping one payload byte of the tick-20
            // record is detected by the incremental chain verification at that
            // frame (ChainMismatch), not later at the trailer.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));

            ReplayTickFrame frame20 = replay.Frames[19];
            Assert.AreEqual(20u, frame20.Tick);
            Assert.AreEqual(1, frame20.RecordCount);

            var tampered = (byte[])live.ReplayBytes.Clone();
            int lastPayloadByte = frame20.RecordSourceOffsets[0] + 2 + frame20.RecordBytes[0].Length - 1;
            tampered[lastPayloadByte] ^= 0xFF;

            Assert.IsFalse(ReplayFile.TryParse(tampered, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.ChainMismatch, error,
                "the tampered frame must fail the chain at its own position");
        }

        [Test]
        public void TamperedResultCode_InMiddleFrame_FailsChainAtThatTick()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));

            ReplayTickFrame frame5 = replay.Frames[4];
            Assert.AreEqual(5u, frame5.Tick);
            Assert.AreEqual(CommandResultCode.Applied, frame5.ResultCodes[0]);

            var tampered = (byte[])live.ReplayBytes.Clone();
            int resultCodeOffset = frame5.RecordSourceOffsets[0] + 2 + frame5.RecordBytes[0].Length;
            tampered[resultCodeOffset] ^= 0x03; // Applied (1) -> RejectedNotVisible (3), a defined code

            Assert.IsFalse(ReplayFile.TryParse(tampered, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.ChainMismatch, error);
        }

        [Test]
        public void TamperedTickNumber_InMiddleFrame_IsDetectedAtThatPosition()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));

            var tampered = (byte[])live.ReplayBytes.Clone();
            int tickOffset = replay.Frames[19].SourceOffset;
            tampered[tickOffset] += 5; // tick 20 -> 25: consecutiveness breaks at frame 20

            Assert.IsFalse(ReplayFile.TryParse(tampered, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.NonConsecutiveTicks, error);
        }

        [Test]
        public void TamperedFingerprintInitialStateHash_RejectsReplayAsInconsistent()
        {
            // The embedded snapshot's state hash must equal the fingerprint's
            // InitialStateHash; a tampered fingerprint field is caught before
            // any frame is read.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            var tampered = (byte[])live.ReplayBytes.Clone();
            int offset = ReplayFormat.HeaderFixedBytes
                + ReplayV1TestUtil.InitialStateHashOffsetInFingerprint(live.Fingerprint);
            tampered[offset] ^= 0xFF;

            Assert.IsFalse(ReplayFile.TryParse(tampered, out _, out ReplayReadError error));
            Assert.AreEqual(ReplayReadError.FingerprintSnapshotMismatch, error);
        }

        [Test]
        public void FingerprintMismatch_DifferentStartSeed_RefusesPlayback()
        {
            // Section 6: any divergence refuses the start, naming the field.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed + 1,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.FingerprintMismatch, error);
            StringAssert.Contains("StartSeed", detail);
            Assert.AreEqual(0u, playback.Kernel.CurrentTick.Value,
                "a refused start must not touch the kernel");
        }

        [Test]
        public void FingerprintMismatch_LegacyEmptyRules_RefusesPlaybackBeforeTickOne()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            MatchFingerprint legacyRules = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed, live.Fingerprint.InitialStateHash,
                live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, legacyRules, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.FingerprintMismatch, error);
            StringAssert.Contains("RulesHash64", detail);
            Assert.AreEqual(0u, playback.Kernel.CurrentTick.Value,
                "an old/new rules mismatch must be refused before execution");
        }

        [Test]
        public void FingerprintMismatch_RevisionOneRules_RefusesPlaybackBeforeTickOne()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            MatchFingerprint revisionOne = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV1),
                live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed, live.Fingerprint.InitialStateHash,
                live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, revisionOne, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            StringAssert.Contains("RulesHash64", detail);
            Assert.That(playback.Kernel.CurrentTick.Value, Is.EqualTo(0u),
                "revision-1 rules must be refused before execution");
        }

        [Test]
        public void FingerprintMismatch_RevisionTwoRules_RefusesPlaybackBeforeTickOne()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            MatchFingerprint revisionTwo = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeRulesHash64(MatchFingerprint.RulesRevisionV2),
                live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed, live.Fingerprint.InitialStateHash,
                live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, revisionTwo, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            StringAssert.Contains("RulesHash64", detail);
            Assert.That(playback.Kernel.CurrentTick.Value, Is.EqualTo(0u),
                "revision-2 rules must be refused before execution");
        }

        [Test]
        public void FingerprintMismatch_DifferentSlotOccupancy_RefusesPlayback()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            byte[] slots = live.Fingerprint.GetSlotOccupancyCopy();
            slots[ReplayV1TestUtil.AiSlot] = (byte)PlayerSlotOccupancy.Human; // AI slot relabeled
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                slots, live.Fingerprint.GetSlotFactionCopy(), live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.FingerprintMismatch, error);
            StringAssert.Contains("SlotOccupancy", detail);
        }

        [Test]
        public void FingerprintMismatch_DifferentSlotFaction_RefusesPlayback()
        {
            // The faction assignment is bound into the fingerprint: a replay
            // recorded Alliance-vs-Legion must refuse to start against a
            // fingerprint that plays the AI slot as Alliance.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            byte[] factions = live.Fingerprint.GetSlotFactionCopy();
            factions[ReplayV1TestUtil.AiSlot] = (byte)FactionId.Alliance;
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), factions, live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.FingerprintMismatch, error);
            StringAssert.Contains("SlotFaction", detail);
        }

        [Test]
        public void FingerprintMismatch_MutatedDefinitionsTable_RefusesPlayback()
        {
            // The definitions content hash is a REAL table hash now: a replay
            // must refuse to start against a fingerprint whose table differs
            // by a single weapon value — a changed Legion rifle damage is a
            // different game.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            var units = SimDefinitions.AllUnits.ToArray();
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i].Faction == FactionId.Legion && units[i].Role == UnitRole.BasicInfantry)
                {
                    units[i] = new SimUnitDefinition(
                        units[i].DefinitionId, units[i].Faction, units[i].Role, units[i].CostAE, units[i].BuildTicks,
                        units[i].Tier, units[i].ProducerRole, units[i].MaxHealth, units[i].MoveSpeed,
                        units[i].ArmorClass, units[i].DamageType,
                        attackDamage: units[i].AttackDamage + 1, units[i].AttackRangeTiles, units[i].AttackCooldownTicks);
                }
            }
            ulong mutatedHash = SimDefinitions.ComputeDefinitionsHash64(SimDefinitions.AllBuildings, units);
            Assert.AreNotEqual(SimDefinitions.ComputeDefinitionsHash64(), mutatedHash);

            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, mutatedHash, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.FingerprintMismatch, error);
            StringAssert.Contains("DefinitionsHash64", detail);
        }

        [Test]
        public void FingerprintMismatch_DifferentSchemaVersion_RefusesPlayback()
        {
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            var foreign = new MatchFingerprint(
                stateSchemaVersion: 2, // not the schema of this stream
                live.Fingerprint.CommandSchemaVersion, live.Fingerprint.PayloadSchemaVersion,
                live.Fingerprint.SnapshotSchemaVersion, live.Fingerprint.SidecarSchemaVersion,
                live.Fingerprint.NumericModelId, live.Fingerprint.TicksPerSecond, live.Fingerprint.PrngId,
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(), live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail));
            Assert.AreEqual(ReplayPlaybackError.FingerprintMismatch, error);
            StringAssert.Contains("StateSchemaVersion", detail);
        }

        [Test]
        public void Playback_DoesNotReapplyAi_RecordedStreamCarriesAiCommands()
        {
            // Section 4: playback never instantiates or applies the AI again.
            // The recorded stream carries the slot-1 records; a shadow AI
            // (the same deterministic generator, diagnostic only) confirms it
            // would have produced exactly those commands at those ticks —
            // and the playback end state equals the recording without any AI.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            Assert.IsTrue(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _));

            int aiRecords = 0;
            for (int f = 0; f < replay.Frames.Length; f++)
            {
                ReplayTickFrame frame = replay.Frames[f];
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    if (frame.Records[r].PlayerSlot != ReplayV1TestUtil.AiSlot) continue;
                    aiRecords++;
                    Assert.IsTrue(
                        ReplayV1TestUtil.ShadowAiWantsMove((int)frame.Tick, out int shadowX, out int shadowY),
                        $"shadow AI produced no command at recorded AI tick {frame.Tick}");
                    byte[] shadowPayload = ReplayV1TestUtil.PayloadBytes(
                        new MovePayload(live.AiUnits, Core.SimFixed.FromInt(shadowX), Core.SimFixed.FromInt(shadowY)));
                    Assert.AreEqual(shadowPayload, frame.Records[r].Payload.ToArray(),
                        $"recorded AI command at tick {frame.Tick} must match the shadow AI's intent");
                }
            }
            Assert.AreEqual(2, aiRecords, "both shadow-AI commands must be in the stream");

            // Playback runs with the AI switched off; the state still matches.
            ReplayV1TestUtil.TestHost playback = ReplayV1TestUtil.CreatePlaybackHost();
            Assert.IsTrue(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, live.Fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                $"playback failed: {error} ({detail})");
            Assert.AreEqual(live.EndStateHash, playback.Kernel.CalculateStateHash());
        }

        [Test]
        public void HistoricalIntake_ReconstructsStreamDerivedSequenceFloor()
        {
            // The authoritative sequence floor is a deterministic function of
            // the accepted stream: the historical intake raises it past every
            // accepted sequence, which is what lets playback reproduce the
            // recording host's state hash exactly.
            var session = new MatchSession(ReplayV1TestUtil.HumanSlot, new byte[] { 0, 1 }, 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);

            for (uint sequence = 1; sequence <= 3; sequence++)
            {
                byte[] recordBytes = ReplayV1TestUtil.CraftRecord(
                    enqueueTick: sequence - 1, targetTick: sequence, playerSlot: 0, sequence: sequence,
                    kind: (ushort)CommandKind.Stop, payloadVersion: CommandLimits.PayloadVersionV1,
                    payload: ReplayV1TestUtil.PayloadBytes(new StopPayload(new uint[] { ReplayV1TestUtil.EntityId(0, 1) })));
                Assert.AreEqual(
                    CommandIngressResult.Accepted,
                    ingress.TryAcceptHistoricalRecordBytes(recordBytes, out _));
            }
            Assert.AreEqual(4u, ingress.DedupeState.NextLocalSequence(0),
                "the floor must track the accepted stream");

            byte[] foreignRecord = ReplayV1TestUtil.CraftRecord(
                enqueueTick: 0, targetTick: 1, playerSlot: 1, sequence: 7,
                kind: (ushort)CommandKind.Stop, payloadVersion: CommandLimits.PayloadVersionV1,
                payload: ReplayV1TestUtil.PayloadBytes(new StopPayload(new uint[] { ReplayV1TestUtil.EntityId(1, 1) })));
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TryAcceptHistoricalRecordBytes(foreignRecord, out _));
            Assert.AreEqual(8u, ingress.DedupeState.NextLocalSequence(1),
                "the floor of a foreign slot tracks the stream as well");

            // A lower historical sequence never lowers the floor.
            byte[] oldRecord = ReplayV1TestUtil.CraftRecord(
                enqueueTick: 0, targetTick: 2, playerSlot: 1, sequence: 5,
                kind: (ushort)CommandKind.Stop, payloadVersion: CommandLimits.PayloadVersionV1,
                payload: ReplayV1TestUtil.PayloadBytes(new StopPayload(new uint[] { ReplayV1TestUtil.EntityId(1, 1) })));
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TryAcceptHistoricalRecordBytes(oldRecord, out _));
            Assert.AreEqual(8u, ingress.DedupeState.NextLocalSequence(1));
        }

        [Test]
        public void Playback_IntoHostWithDifferentSources_FailsRestore()
        {
            // Section 8: playback uses the same kernel and the same sources.
            // A host with a different entity capacity cannot absorb the
            // initial snapshot and refuses.
            ReplayV1TestUtil.LiveMatch live = ReplayV1TestUtil.RunLiveMatch();
            ReplayV1TestUtil.TestHost foreign = ReplayV1TestUtil.TestHost.Create(ReplayV1TestUtil.Seed, capacity: 128);

            Assert.IsFalse(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, live.Fingerprint, foreign.Kernel, foreign.Ingress,
                    out ReplayPlaybackError error, out _));
            Assert.AreEqual(ReplayPlaybackError.RestoreFailed, error);
        }

        [Test]
        public void Recorder_RejectsMisuse()
        {
            ReplayV1TestUtil.TestHost host = ReplayV1TestUtil.TestHost.Create(ReplayV1TestUtil.Seed);
            host.SpawnUnits(ReplayV1TestUtil.HumanSlot, 2, 10.5f, 10.5f);
            MatchFingerprint fingerprint = ReplayV1TestUtil.CreateFingerprint(host, ReplayV1TestUtil.Seed);
            byte[] snapshot = host.Kernel.SaveSnapshot();
            var recorder = new ReplayRecorder(fingerprint, snapshot);

            // Gapless ticks are enforced.
            CommandBatch empty = host.Ingress.SealTickBatch(1);
            recorder.RecordTick(1, empty, host.Kernel.LastTickResults);
            Assert.Throws<InvalidOperationException>(
                () => recorder.RecordTick(3, host.Ingress.SealTickBatch(3), host.Kernel.LastTickResults));

            // A result/record count mismatch is a host programming error.
            var recorder2 = new ReplayRecorder(fingerprint, snapshot);
            Assert.Throws<InvalidOperationException>(
                () => recorder2.RecordTick(1, empty, new CommandResult[1]));

            // A batch that does not belong to the tick is rejected.
            var recorder3 = new ReplayRecorder(fingerprint, snapshot);
            Assert.Throws<InvalidOperationException>(
                () => recorder3.RecordTick(2, empty, host.Kernel.LastTickResults));

            // A fingerprint/snapshot inconsistency can never be recorded.
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                fingerprint.RulesHash64, fingerprint.DefinitionsHash64, fingerprint.MapHash64,
                fingerprint.GetSlotOccupancyCopy(), fingerprint.GetSlotFactionCopy(), fingerprint.StartSeed,
                fingerprint.InitialStateHash ^ 1, fingerprint.InputDelayTicks);
            Assert.Throws<ArgumentException>(() => new ReplayRecorder(foreign, snapshot));

            // Finalize is single-shot.
            recorder2.RecordTick(1, empty, host.Kernel.LastTickResults);
            recorder2.Finalize(0UL);
            Assert.Throws<InvalidOperationException>(() => recorder2.Finalize(0UL));
        }
    }
}
