using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Replays;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 replay suite (.NET lane): the canonical replay contract of
    /// docs/tech/SimulationCore.md section 8 — golden playback over the
    /// standard 50-tick match, hash-chain tamper detection, fingerprint
    /// refusal (section 6), state-dependent rejections staying in the stream
    /// and playback without re-instantiating the AI (section 4).
    /// Mirror of the EditMode lane ReplayV1Tests.
    /// </summary>
    [TestFixture]
    public sealed class ReplayTests
    {
        [Test]
        public void GoldenReplay_PlaybackReproducesEndHashAndRecordedResults()
        {
            // Section 8 golden case: the standard match (human slot 0,
            // recorded slot-1 AI records, 50 ticks, Moves plus a
            // state-dependently rejected command) plays back through the
            // same kernel and sources and reproduces the recording exactly —
            // the player compares every re-executed CommandResult against the
            // recording and the end state hash against the trailer.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();

            Assert.That(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out ReplayReadError readError),
                Is.True, () => $"parse failed: {readError}");
            Assert.That(replay.Frames.Length, Is.EqualTo(ReplayTestUtil.MatchTicks),
                "every tick is recorded, empty ones included");
            Assert.That(replay.Fingerprint, Is.EqualTo(live.Fingerprint));

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, live.Fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.True, () => $"playback failed: {error} ({detail})");

            Assert.That(playback.Kernel.CurrentTick.Value, Is.EqualTo((uint)ReplayTestUtil.MatchTicks));
            Assert.That(playback.Kernel.CalculateStateHash(), Is.EqualTo(live.EndStateHash),
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
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            Assert.That(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _), Is.True);

            ReplayTickFrame frame10 = replay.Frames[9];
            Assert.That(frame10.Tick, Is.EqualTo(10u));
            Assert.That(frame10.RecordCount, Is.EqualTo(1));
            Assert.That(frame10.Records[0].PlayerSlot, Is.EqualTo(ReplayTestUtil.HumanSlot));
            Assert.That(frame10.Records[0].Kind, Is.EqualTo(CommandKind.Move));
            Assert.That(frame10.ResultCodes[0], Is.EqualTo(CommandResultCode.RejectedNotOwned),
                "the state-dependent rejection must be recorded, not dropped");

            // A forged recording (the same tick-10 rejection recorded as
            // Applied, chain-consistent) must fail playback at exactly the
            // result comparison: re-execution yields RejectedNotOwned.
            ReplayTestUtil.LiveMatch forged = ReplayTestUtil.RunLiveMatch(
                forgeResultAtTick: 10, forgedCode: CommandResultCode.Applied);
            Assert.That(ReplayFile.TryParse(forged.ReplayBytes, out _, out ReplayReadError forgeParse),
                Is.True, () => $"forged replay must stay parseable: {forgeParse}");

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    forged.ReplayBytes, forged.Fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.ResultMismatch), detail);
            Assert.That(detail, Does.Contain("tick 10"));
        }

        [Test]
        public void TamperedRecordPayload_InMiddleFrame_FailsChainAtThatTick()
        {
            // Section 8 hash chain: flipping one payload byte of the tick-20
            // record is detected by the incremental chain verification at that
            // frame (ChainMismatch), not later at the trailer.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            Assert.That(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _), Is.True);

            ReplayTickFrame frame20 = replay.Frames[19];
            Assert.That(frame20.Tick, Is.EqualTo(20u));
            Assert.That(frame20.RecordCount, Is.EqualTo(1));

            var tampered = (byte[])live.ReplayBytes.Clone();
            int lastPayloadByte = frame20.RecordSourceOffsets[0] + 2 + frame20.RecordBytes[0].Length - 1;
            tampered[lastPayloadByte] ^= 0xFF;

            Assert.That(ReplayFile.TryParse(tampered, out _, out ReplayReadError error), Is.False);
            Assert.That(error, Is.EqualTo(ReplayReadError.ChainMismatch),
                "the tampered frame must fail the chain at its own position");
        }

        [Test]
        public void TamperedResultCode_InMiddleFrame_FailsChainAtThatTick()
        {
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            Assert.That(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _), Is.True);

            ReplayTickFrame frame5 = replay.Frames[4];
            Assert.That(frame5.Tick, Is.EqualTo(5u));
            Assert.That(frame5.ResultCodes[0], Is.EqualTo(CommandResultCode.Applied));

            var tampered = (byte[])live.ReplayBytes.Clone();
            int resultCodeOffset = frame5.RecordSourceOffsets[0] + 2 + frame5.RecordBytes[0].Length;
            tampered[resultCodeOffset] ^= 0x03; // Applied (1) -> RejectedNotVisible (3), a defined code

            Assert.That(ReplayFile.TryParse(tampered, out _, out ReplayReadError error), Is.False);
            Assert.That(error, Is.EqualTo(ReplayReadError.ChainMismatch));
        }

        [Test]
        public void TamperedTickNumber_InMiddleFrame_IsDetectedAtThatPosition()
        {
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            Assert.That(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _), Is.True);

            var tampered = (byte[])live.ReplayBytes.Clone();
            int tickOffset = replay.Frames[19].SourceOffset;
            tampered[tickOffset] += 5; // tick 20 -> 25: consecutiveness breaks at frame 20

            Assert.That(ReplayFile.TryParse(tampered, out _, out ReplayReadError error), Is.False);
            Assert.That(error, Is.EqualTo(ReplayReadError.NonConsecutiveTicks));
        }

        [Test]
        public void TamperedFingerprintInitialStateHash_RejectsReplayAsInconsistent()
        {
            // The embedded snapshot's state hash must equal the fingerprint's
            // InitialStateHash; a tampered fingerprint field is caught before
            // any frame is read.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            var tampered = (byte[])live.ReplayBytes.Clone();
            int offset = ReplayFormat.HeaderFixedBytes
                + ReplayTestUtil.InitialStateHashOffsetInFingerprint(live.Fingerprint);
            tampered[offset] ^= 0xFF;

            Assert.That(ReplayFile.TryParse(tampered, out _, out ReplayReadError error), Is.False);
            Assert.That(error, Is.EqualTo(ReplayReadError.FingerprintSnapshotMismatch));
        }

        [Test]
        public void FingerprintMismatch_DifferentStartSeed_RefusesPlayback()
        {
            // Section 6: any divergence refuses the start, naming the field.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed + 1,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            Assert.That(detail, Does.Contain("StartSeed"));
            Assert.That(playback.Kernel.CurrentTick.Value, Is.EqualTo(0u),
                "a refused start must not touch the kernel");
        }

        [Test]
        public void FingerprintMismatch_DifferentSlotOccupancy_RefusesPlayback()
        {
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            byte[] slots = live.Fingerprint.GetSlotOccupancyCopy();
            slots[ReplayTestUtil.AiSlot] = (byte)PlayerSlotOccupancy.Human; // AI slot relabeled
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                slots, live.Fingerprint.GetSlotFactionCopy(), live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            Assert.That(detail, Does.Contain("SlotOccupancy"));
        }

        [Test]
        public void FingerprintMismatch_DifferentSlotFaction_RefusesPlayback()
        {
            // The faction assignment is bound into the fingerprint: a replay
            // recorded Alliance-vs-Legion must refuse to start against a
            // fingerprint that plays the AI slot as Alliance.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            byte[] factions = live.Fingerprint.GetSlotFactionCopy();
            factions[ReplayTestUtil.AiSlot] = (byte)FactionId.Alliance;
            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), factions, live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            Assert.That(detail, Does.Contain("SlotFaction"));
        }

        [Test]
        public void FingerprintMismatch_MutatedDefinitionsTable_RefusesPlayback()
        {
            // The definitions content hash is a REAL table hash now: a replay
            // must refuse to start against a fingerprint whose table differs
            // by a single weapon value — a changed Legion rifle damage is a
            // different game.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
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
            Assert.That(mutatedHash, Is.Not.EqualTo(SimDefinitions.ComputeDefinitionsHash64()));

            MatchFingerprint foreign = MatchFingerprint.CreateCurrent(
                live.Fingerprint.RulesHash64, mutatedHash, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(),
                live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            Assert.That(detail, Does.Contain("DefinitionsHash64"));
        }

        [Test]
        public void FingerprintMismatch_DifferentSchemaVersion_RefusesPlayback()
        {
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            var foreign = new MatchFingerprint(
                stateSchemaVersion: 2, // not the schema of this stream
                live.Fingerprint.CommandSchemaVersion, live.Fingerprint.PayloadSchemaVersion,
                live.Fingerprint.SnapshotSchemaVersion, live.Fingerprint.SidecarSchemaVersion,
                live.Fingerprint.NumericModelId, live.Fingerprint.TicksPerSecond, live.Fingerprint.PrngId,
                live.Fingerprint.RulesHash64, live.Fingerprint.DefinitionsHash64, live.Fingerprint.MapHash64,
                live.Fingerprint.GetSlotOccupancyCopy(), live.Fingerprint.GetSlotFactionCopy(), live.Fingerprint.StartSeed,
                live.Fingerprint.InitialStateHash, live.Fingerprint.InputDelayTicks);

            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, foreign, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.FingerprintMismatch));
            Assert.That(detail, Does.Contain("StateSchemaVersion"));
        }

        [Test]
        public void Playback_DoesNotReapplyAi_RecordedStreamCarriesAiCommands()
        {
            // Section 4: playback never instantiates or applies the AI again.
            // The recorded stream carries the slot-1 records; a shadow AI
            // (the same deterministic generator, diagnostic only) confirms it
            // would have produced exactly those commands at those ticks —
            // and the playback end state equals the recording without any AI.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            Assert.That(ReplayFile.TryParse(live.ReplayBytes, out ReplayFile replay, out _), Is.True);

            int aiRecords = 0;
            for (int f = 0; f < replay.Frames.Length; f++)
            {
                ReplayTickFrame frame = replay.Frames[f];
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    if (frame.Records[r].PlayerSlot != ReplayTestUtil.AiSlot) continue;
                    aiRecords++;
                    Assert.That(ReplayTestUtil.ShadowAiWantsMove((int)frame.Tick, out int shadowX, out int shadowY),
                        Is.True, $"shadow AI produced no command at recorded AI tick {frame.Tick}");
                    byte[] shadowPayload = CommandTestUtil.PayloadBytes(
                        new MovePayload(live.AiUnits, Core.SimFixed.FromInt(shadowX), Core.SimFixed.FromInt(shadowY)));
                    Assert.That(frame.Records[r].Payload.ToArray(), Is.EqualTo(shadowPayload),
                        $"recorded AI command at tick {frame.Tick} must match the shadow AI's intent");
                }
            }
            Assert.That(aiRecords, Is.EqualTo(2), "both shadow-AI commands must be in the stream");

            // Playback runs with the AI switched off; the state still matches.
            ReplayTestUtil.TestHost playback = ReplayTestUtil.CreatePlaybackHost();
            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, live.Fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.True, () => $"playback failed: {error} ({detail})");
            Assert.That(playback.Kernel.CalculateStateHash(), Is.EqualTo(live.EndStateHash));
        }

        [Test]
        public void HistoricalIntake_ReconstructsStreamDerivedSequenceFloor()
        {
            // The authoritative sequence floor is a deterministic function of
            // the accepted stream: the historical intake raises it past every
            // accepted sequence, which is what lets playback reproduce the
            // recording host's state hash exactly.
            var session = new MatchSession(ReplayTestUtil.HumanSlot, new byte[] { 0, 1 }, 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);

            for (uint sequence = 1; sequence <= 3; sequence++)
            {
                byte[] recordBytes = CommandTestUtil.CraftRecord(
                    enqueueTick: sequence - 1, targetTick: sequence, playerSlot: 0, sequence: sequence,
                    kind: (ushort)CommandKind.Stop, payloadVersion: CommandLimits.PayloadVersionV1,
                    payload: CommandTestUtil.PayloadBytes(new StopPayload(new uint[] { CommandTestUtil.EntityId(0, 1) })));
                Assert.That(ingress.TryAcceptHistoricalRecordBytes(recordBytes, out _),
                    Is.EqualTo(CommandIngressResult.Accepted));
            }
            Assert.That(ingress.DedupeState.NextLocalSequence(0), Is.EqualTo(4u),
                "the floor must track the accepted stream");

            byte[] foreignRecord = CommandTestUtil.CraftRecord(
                enqueueTick: 0, targetTick: 1, playerSlot: 1, sequence: 7,
                kind: (ushort)CommandKind.Stop, payloadVersion: CommandLimits.PayloadVersionV1,
                payload: CommandTestUtil.PayloadBytes(new StopPayload(new uint[] { CommandTestUtil.EntityId(1, 1) })));
            Assert.That(ingress.TryAcceptHistoricalRecordBytes(foreignRecord, out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            Assert.That(ingress.DedupeState.NextLocalSequence(1), Is.EqualTo(8u),
                "the floor of a foreign slot tracks the stream as well");

            // A lower historical sequence never lowers the floor.
            byte[] oldRecord = CommandTestUtil.CraftRecord(
                enqueueTick: 0, targetTick: 2, playerSlot: 1, sequence: 5,
                kind: (ushort)CommandKind.Stop, payloadVersion: CommandLimits.PayloadVersionV1,
                payload: CommandTestUtil.PayloadBytes(new StopPayload(new uint[] { CommandTestUtil.EntityId(1, 1) })));
            Assert.That(ingress.TryAcceptHistoricalRecordBytes(oldRecord, out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            Assert.That(ingress.DedupeState.NextLocalSequence(1), Is.EqualTo(8u));
        }

        [Test]
        public void Playback_IntoHostWithDifferentSources_FailsRestore()
        {
            // Section 8: playback uses the same kernel and the same sources.
            // A host with a different entity capacity cannot absorb the
            // initial snapshot and refuses.
            ReplayTestUtil.LiveMatch live = ReplayTestUtil.RunLiveMatch();
            ReplayTestUtil.TestHost foreign = ReplayTestUtil.TestHost.Create(ReplayTestUtil.Seed, capacity: 128);

            Assert.That(
                ReplayPlayer.TryPlay(
                    live.ReplayBytes, live.Fingerprint, foreign.Kernel, foreign.Ingress,
                    out ReplayPlaybackError error, out _),
                Is.False);
            Assert.That(error, Is.EqualTo(ReplayPlaybackError.RestoreFailed));
        }

        [Test]
        public void Recorder_RejectsMisuse()
        {
            ReplayTestUtil.TestHost host = ReplayTestUtil.TestHost.Create(ReplayTestUtil.Seed);
            host.SpawnUnits(ReplayTestUtil.HumanSlot, 2, 10.5f, 10.5f);
            MatchFingerprint fingerprint = ReplayTestUtil.CreateFingerprint(host, ReplayTestUtil.Seed);
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
