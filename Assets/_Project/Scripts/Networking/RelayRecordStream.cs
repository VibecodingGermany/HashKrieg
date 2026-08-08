using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Replays;
using Nova.Simulation.Snapshots;

namespace Nova.Networking
{
    /// <summary>One gapless, globally confirmed relay tick, including empty ticks.</summary>
    public sealed class RelayRecordTickFrame
    {
        public uint Tick { get; }
        public IReadOnlyList<CommandRecord> Records { get; }

        internal RelayRecordTickFrame(uint tick, CommandRecord[] records)
        {
            Tick = tick;
            Records = records;
        }
    }

    /// <summary>One state hash submitted identically by both peers.</summary>
    public readonly struct RelayRecordCheckpoint
    {
        public uint Tick { get; }
        public ulong StateHash { get; }

        internal RelayRecordCheckpoint(uint tick, ulong stateHash)
        {
            Tick = tick;
            StateHash = stateHash;
        }
    }

    /// <summary>The two unequal state hashes that terminated a recorded match.</summary>
    public sealed class RelayRecordDesync
    {
        public uint Tick { get; }
        public ulong Slot0Hash { get; }
        public ulong Slot1Hash { get; }

        internal RelayRecordDesync(uint tick, ulong slot0Hash, ulong slot1Hash)
        {
            Tick = tick;
            Slot0Hash = slot0Hash;
            Slot1Hash = slot1Hash;
        }
    }

    /// <summary>Why the relay sealed a recording.</summary>
    public enum RelayRecordTerminalReason : byte
    {
        ServerStopped = 1,
        PeerLost = 2,
        ProtocolViolation = 3,
        Desync = 4,
        RecordingLimitExceeded = 5,
    }

    /// <summary>
    /// A structurally verified <c>*.novarec</c> v2 recording. A sealed
    /// budget-exhausted prefix parses with <see cref="IsComplete"/> false so
    /// tooling can report its exact watermarks; full playback rejects it.
    /// </summary>
    public sealed class RelayRecordStreamFile
    {
        public MatchFingerprint Fingerprint { get; }
        public byte[] InitialSnapshotBytes { get; }
        public IReadOnlyList<RelayRecordTickFrame> Frames { get; }
        public IReadOnlyList<RelayRecordCheckpoint> Checkpoints { get; }
        public RelayRecordDesync Desync { get; }
        public uint InitialSnapshotTick { get; }
        public RelayRecordTerminalReason TerminalReason { get; }
        public uint TerminalTick { get; }
        public uint LastRecordedTick { get; }
        public uint LastCheckpointTick { get; }
        public bool IsComplete =>
            TerminalReason != RelayRecordTerminalReason.RecordingLimitExceeded
            && TerminalTick == LastRecordedTick;

        internal RelayRecordStreamFile(
            MatchFingerprint fingerprint, byte[] initialSnapshotBytes,
            RelayRecordTickFrame[] frames, RelayRecordCheckpoint[] checkpoints,
            RelayRecordDesync desync, uint initialSnapshotTick,
            RelayRecordTerminalReason terminalReason, uint terminalTick,
            uint lastRecordedTick, uint lastCheckpointTick)
        {
            Fingerprint = fingerprint;
            InitialSnapshotBytes = initialSnapshotBytes;
            Frames = frames;
            Checkpoints = checkpoints;
            Desync = desync;
            InitialSnapshotTick = initialSnapshotTick;
            TerminalReason = terminalReason;
            TerminalTick = terminalTick;
            LastRecordedTick = lastRecordedTick;
            LastCheckpointTick = lastCheckpointTick;
        }

        public bool TryGetCheckpointHash(uint tick, out ulong stateHash)
        {
            for (int i = 0; i < Checkpoints.Count; i++)
            {
                if (Checkpoints[i].Tick == tick)
                {
                    stateHash = Checkpoints[i].StateHash;
                    return true;
                }
            }
            stateHash = 0;
            return false;
        }
    }

    /// <summary>
    /// Hardened reader/writer primitives for the relay-owned NOVAREC2
    /// format. It is deliberately not <see cref="ReplayFile"/>: the relay
    /// does not execute commands and therefore cannot truthfully invent the
    /// per-record result codes required by NOVAPLAY. Instead, NOVAREC2 binds
    /// the canonical initial snapshot to gapless confirmed tick frames and
    /// the equal state-hash checkpoints actually submitted by both peers.
    /// </summary>
    public static class RelayRecordStream
    {
        internal static readonly byte[] Magic = Encoding.ASCII.GetBytes("NOVAREC2");
        internal const byte TickFrameEntry = 1;
        internal const byte CheckpointEntry = 2;
        internal const byte DesyncEntry = 3;
        internal const byte EndEntry = 255;
        public const int MaxRecordingBytes = 64 * 1024 * 1024;
        internal const int EndEntryBytes = 1 + 1 + 4 + 4 + 4;

        public static bool TryRead(byte[] bytes, out RelayRecordStreamFile file, out string error)
        {
            file = null;
            error = string.Empty;
            if (bytes == null)
            {
                error = "recording is null";
                return false;
            }
            if (bytes.Length > MaxRecordingBytes)
            {
                error = "recording exceeds the hard size cap";
                return false;
            }
            if (bytes.Length < Magic.Length + 8 + EndEntryBytes)
            {
                error = "recording is truncated";
                return false;
            }

            int offset = 0;
            for (int i = 0; i < Magic.Length; i++)
            {
                if (bytes[offset + i] != Magic[i])
                {
                    error = "bad NOVAREC2 magic";
                    return false;
                }
            }
            offset += Magic.Length;

            if (!TryReadBlob(bytes, ref offset, 4096, out byte[] fingerprintBytes)
                || !MatchFingerprint.TryParse(fingerprintBytes, out MatchFingerprint fingerprint))
            {
                error = "malformed fingerprint";
                return false;
            }
            if (!RelayProtocol.IsSupportedInputDelay(fingerprint.InputDelayTicks))
            {
                error = "fingerprint input delay is outside the relay contract";
                return false;
            }
            if (!TryReadBlob(bytes, ref offset, SnapshotFormat.MaxFileBytes, out byte[] snapshotBytes))
            {
                error = "malformed initial snapshot length";
                return false;
            }
            if (!DesyncDiagnostic.TryReadSnapshotIdentity(
                    snapshotBytes, out uint initialTick, out ulong initialHash, out string snapshotError))
            {
                error = $"malformed initial snapshot ({snapshotError})";
                return false;
            }
            if (initialHash != fingerprint.InitialStateHash)
            {
                error = "initial snapshot hash does not match the fingerprint";
                return false;
            }

            var frames = new List<RelayRecordTickFrame>();
            var checkpoints = new List<RelayRecordCheckpoint>();
            var lastSequence = new uint[CommandLimits.ReservedPlayerSlots];
            RelayRecordDesync desync = null;
            uint previousTick = initialTick;
            uint previousCheckpointTick = 0;
            bool ended = false;

            while (offset < bytes.Length)
            {
                byte entry = bytes[offset++];
                if (desync != null && entry != EndEntry)
                {
                    error = "desync evidence must be followed by the end marker";
                    return false;
                }

                if (entry == TickFrameEntry)
                {
                    if (!TryReadUInt32(bytes, ref offset, out uint tick)
                        || !TryReadUInt16(bytes, ref offset, out ushort recordCount))
                    {
                        error = "truncated tick-frame header";
                        return false;
                    }
                    if (previousTick == uint.MaxValue || tick != previousTick + 1)
                    {
                        error = $"tick frame {tick} is not the gapless successor of {previousTick}";
                        return false;
                    }
                    if (recordCount > CommandLimits.MaxBatchRecordsPerTick)
                    {
                        error = $"invalid record count {recordCount} at tick {tick}";
                        return false;
                    }

                    var records = new CommandRecord[recordCount];
                    for (int i = 0; i < records.Length; i++)
                    {
                        if (!TryReadBlob(bytes, ref offset, CommandLimits.MaxRecordBytes, out byte[] recordBytes)
                            || !CommandRecord.TryDeserialize(recordBytes, out CommandRecord record, out int consumed)
                            || consumed != recordBytes.Length)
                        {
                            error = $"malformed record {i} at tick {tick}";
                            return false;
                        }
                        if (record.TargetTick != tick
                            || record.Sequence == 0
                            || record.PlayerSlot >= CommandLimits.ReservedPlayerSlots
                            || fingerprint.GetSlotOccupancy(record.PlayerSlot) == PlayerSlotOccupancy.Free)
                        {
                            error = $"record {i} at tick {tick} violates slot/tick/sequence binding";
                            return false;
                        }
                        uint expectedTarget = record.EnqueueTick + fingerprint.InputDelayTicks;
                        if (expectedTarget < record.EnqueueTick || expectedTarget != record.TargetTick)
                        {
                            error = $"record {i} at tick {tick} violates the input-delay window";
                            return false;
                        }
                        if (record.Sequence <= lastSequence[record.PlayerSlot])
                        {
                            error = $"record {i} at tick {tick} is a duplicate or non-monotone sequence";
                            return false;
                        }
                        if (!CommandPayloadValidation.TryValidateStreamPayload(
                                record.Kind, record.PayloadVersion, record.Payload.Span, out CommandRejectReason reason))
                        {
                            error = $"record {i} at tick {tick} has invalid payload ({reason})";
                            return false;
                        }
                        if (i > 0 && CommandBatch.CompareRecords(records[i - 1], record) >= 0)
                        {
                            error = $"records at tick {tick} are not in strict canonical order";
                            return false;
                        }
                        lastSequence[record.PlayerSlot] = record.Sequence;
                        records[i] = record;
                    }

                    frames.Add(new RelayRecordTickFrame(tick, records));
                    previousTick = tick;
                    continue;
                }

                if (entry == CheckpointEntry)
                {
                    if (!TryReadUInt32(bytes, ref offset, out uint tick)
                        || !TryReadUInt64(bytes, ref offset, out ulong stateHash))
                    {
                        error = "truncated checkpoint entry";
                        return false;
                    }
                    uint expectedCheckpoint = previousCheckpointTick == 0
                        ? initialTick - initialTick % RelayMatchClient.StateHashIntervalTicks
                            + RelayMatchClient.StateHashIntervalTicks
                        : previousCheckpointTick + RelayMatchClient.StateHashIntervalTicks;
                    if (tick == 0 || tick % RelayMatchClient.StateHashIntervalTicks != 0
                        || tick > previousTick || tick != expectedCheckpoint)
                    {
                        error = "checkpoint tick is duplicate, out of cadence or outside the recorded frames";
                        return false;
                    }
                    checkpoints.Add(new RelayRecordCheckpoint(tick, stateHash));
                    previousCheckpointTick = tick;
                    continue;
                }

                if (entry == DesyncEntry)
                {
                    if (!TryReadUInt32(bytes, ref offset, out uint tick)
                        || !TryReadUInt64(bytes, ref offset, out ulong slot0Hash)
                        || !TryReadUInt64(bytes, ref offset, out ulong slot1Hash))
                    {
                        error = "truncated desync entry";
                        return false;
                    }
                    uint expectedDesyncTick = previousCheckpointTick == 0
                        ? initialTick - initialTick % RelayMatchClient.StateHashIntervalTicks
                            + RelayMatchClient.StateHashIntervalTicks
                        : previousCheckpointTick + RelayMatchClient.StateHashIntervalTicks;
                    if (tick == 0 || tick % RelayMatchClient.StateHashIntervalTicks != 0
                        || tick > previousTick || tick != expectedDesyncTick
                        || slot0Hash == slot1Hash)
                    {
                        error = "invalid desync evidence";
                        return false;
                    }
                    desync = new RelayRecordDesync(tick, slot0Hash, slot1Hash);
                    continue;
                }

                if (entry == EndEntry)
                {
                    if (offset >= bytes.Length)
                    {
                        error = "truncated end marker";
                        return false;
                    }
                    var terminalReason = (RelayRecordTerminalReason)bytes[offset++];
                    if (!IsKnownTerminalReason(terminalReason)
                        || !TryReadUInt32(bytes, ref offset, out uint terminalTick)
                        || !TryReadUInt32(bytes, ref offset, out uint lastRecordedTick)
                        || !TryReadUInt32(bytes, ref offset, out uint lastCheckpointTick)
                        || lastRecordedTick != previousTick
                        || lastCheckpointTick != previousCheckpointTick
                        || terminalTick < lastRecordedTick
                        || lastCheckpointTick > lastRecordedTick
                        || (terminalReason != RelayRecordTerminalReason.RecordingLimitExceeded
                            && terminalTick != lastRecordedTick)
                        || (desync == null
                            ? terminalReason == RelayRecordTerminalReason.Desync
                            : terminalReason != RelayRecordTerminalReason.Desync
                                || terminalTick != desync.Tick
                                || lastRecordedTick != desync.Tick)
                        || offset != bytes.Length)
                    {
                        error = "invalid or non-terminal end marker";
                        return false;
                    }
                    ended = true;
                    file = new RelayRecordStreamFile(
                        fingerprint, snapshotBytes, frames.ToArray(), checkpoints.ToArray(),
                        desync, initialTick, terminalReason, terminalTick,
                        lastRecordedTick, lastCheckpointTick);
                    break;
                }

                error = $"unknown NOVAREC2 entry type {entry}";
                return false;
            }

            if (!ended)
            {
                error = "recording has no completed end marker";
                return false;
            }

            return true;
        }

        internal static void WriteHeader(Stream stream, byte[] fingerprintBytes, byte[] snapshotBytes)
        {
            if (fingerprintBytes == null || fingerprintBytes.Length > 4096)
            {
                throw new ArgumentOutOfRangeException(nameof(fingerprintBytes));
            }
            if (snapshotBytes == null || snapshotBytes.Length > SnapshotFormat.MaxFileBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshotBytes));
            }
            int entryBytes = checked(Magic.Length + 4 + fingerprintBytes.Length + 4 + snapshotBytes.Length);
            EnsureEntryFits(stream, entryBytes, reserveEnd: true);
            stream.Write(Magic, 0, Magic.Length);
            WriteBlob(stream, fingerprintBytes);
            WriteBlob(stream, snapshotBytes);
        }

        internal static void WriteTickFrame(Stream stream, uint tick, IReadOnlyList<CommandRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (records.Count > CommandLimits.MaxBatchRecordsPerTick)
            {
                throw new ArgumentOutOfRangeException(nameof(records));
            }
            var serialized = new byte[records.Count][];
            int entryBytes = 1 + 4 + 2;
            for (int i = 0; i < records.Count; i++)
            {
                serialized[i] = records[i].Serialize();
                if (serialized[i].Length > CommandLimits.MaxRecordBytes)
                {
                    throw new ArgumentOutOfRangeException(nameof(records));
                }
                entryBytes = checked(entryBytes + 4 + serialized[i].Length);
            }
            EnsureEntryFits(stream, entryBytes, reserveEnd: true);
            stream.WriteByte(TickFrameEntry);
            WriteUInt32(stream, tick);
            WriteUInt16(stream, unchecked((ushort)records.Count));
            for (int i = 0; i < serialized.Length; i++) WriteBlob(stream, serialized[i]);
        }

        internal static void WriteCheckpoint(Stream stream, uint tick, ulong stateHash)
        {
            EnsureEntryFits(stream, 1 + 4 + 8, reserveEnd: true);
            stream.WriteByte(CheckpointEntry);
            WriteUInt32(stream, tick);
            WriteUInt64(stream, stateHash);
        }

        internal static void WriteDesync(Stream stream, uint tick, ulong slot0Hash, ulong slot1Hash)
        {
            EnsureEntryFits(stream, 1 + 4 + 8 + 8, reserveEnd: true);
            stream.WriteByte(DesyncEntry);
            WriteUInt32(stream, tick);
            WriteUInt64(stream, slot0Hash);
            WriteUInt64(stream, slot1Hash);
        }

        internal static void WriteEnd(
            Stream stream, RelayRecordTerminalReason terminalReason,
            uint terminalTick, uint lastRecordedTick, uint lastCheckpointTick)
        {
            if (!IsKnownTerminalReason(terminalReason)
                || terminalTick < lastRecordedTick
                || lastCheckpointTick > lastRecordedTick)
            {
                throw new ArgumentException("invalid relay recording terminal watermarks");
            }
            EnsureEntryFits(stream, EndEntryBytes, reserveEnd: false);
            stream.WriteByte(EndEntry);
            stream.WriteByte((byte)terminalReason);
            WriteUInt32(stream, terminalTick);
            WriteUInt32(stream, lastRecordedTick);
            WriteUInt32(stream, lastCheckpointTick);
        }

        private static void EnsureEntryFits(Stream stream, int entryBytes, bool reserveEnd)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanSeek) throw new ArgumentException("NOVAREC2 output must be seekable", nameof(stream));
            long required = checked(stream.Position + entryBytes + (reserveEnd ? EndEntryBytes : 0));
            if (required > MaxRecordingBytes)
            {
                throw new RelayRecordBudgetExceededException(
                    $"NOVAREC2 byte budget exceeded ({required} > {MaxRecordingBytes})");
            }
        }

        private static bool IsKnownTerminalReason(RelayRecordTerminalReason reason)
        {
            return reason >= RelayRecordTerminalReason.ServerStopped
                && reason <= RelayRecordTerminalReason.RecordingLimitExceeded;
        }

        private static void WriteBlob(Stream stream, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            WriteUInt32(stream, unchecked((uint)bytes.Length));
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            var bytes = new byte[2];
            RelayProtocol.WriteUInt16(bytes, 0, value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            var bytes = new byte[4];
            RelayProtocol.WriteUInt32(bytes, 0, value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUInt64(Stream stream, ulong value)
        {
            var bytes = new byte[8];
            RelayProtocol.WriteUInt64(bytes, 0, value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static bool TryReadBlob(byte[] source, ref int offset, int maximumLength, out byte[] blob)
        {
            blob = null;
            if (!TryReadUInt32(source, ref offset, out uint length)
                || length > (uint)maximumLength || length > (uint)(source.Length - offset))
            {
                return false;
            }
            blob = new byte[(int)length];
            Array.Copy(source, offset, blob, 0, (int)length);
            offset += (int)length;
            return true;
        }

        private static bool TryReadUInt16(byte[] source, ref int offset, out ushort value)
        {
            value = 0;
            if (source.Length - offset < 2) return false;
            value = RelayProtocol.ReadUInt16(source, offset);
            offset += 2;
            return true;
        }

        private static bool TryReadUInt32(byte[] source, ref int offset, out uint value)
        {
            value = 0;
            if (source.Length - offset < 4) return false;
            value = RelayProtocol.ReadUInt32(source, offset);
            offset += 4;
            return true;
        }

        private static bool TryReadUInt64(byte[] source, ref int offset, out ulong value)
        {
            value = 0;
            if (source.Length - offset < 8) return false;
            value = RelayProtocol.ReadUInt64(source, offset);
            offset += 8;
            return true;
        }
    }

    internal sealed class RelayRecordBudgetExceededException : InvalidOperationException
    {
        public RelayRecordBudgetExceededException(string message) : base(message) { }
    }

    public enum RelayRecordPlaybackError
    {
        None = 0,
        FingerprintMismatch,
        RestoreFailed,
        TickMismatch,
        RecordRejected,
        BatchSubmitFailed,
        CheckpointMismatch,
        IncompleteRecording,
        DesyncHashMismatch,
    }

    public sealed class RelayRecordPlaybackResult
    {
        public uint EndTick { get; }
        public ulong StateHash { get; }
        public int VerifiedCheckpoints { get; }

        internal RelayRecordPlaybackResult(uint endTick, ulong stateHash, int verifiedCheckpoints)
        {
            EndTick = endTick;
            StateHash = stateHash;
            VerifiedCheckpoints = verifiedCheckpoints;
        }
    }

    /// <summary>Engine-free NOVAREC2 playback through historical ingress and the canonical kernel.</summary>
    public static class RelayRecordPlayback
    {
        public static bool TryPlay(
            RelayRecordStreamFile recording, MatchFingerprint expectedFingerprint,
            SimulationKernel kernel, CommandIngress ingress,
            out RelayRecordPlaybackResult result,
            out RelayRecordPlaybackError error, out string detail)
        {
            if (recording == null) throw new ArgumentNullException(nameof(recording));
            if (!recording.IsComplete)
            {
                result = null;
                error = RelayRecordPlaybackError.IncompleteRecording;
                detail = $"recording stopped at persisted tick {recording.LastRecordedTick} before terminal tick {recording.TerminalTick}";
                return false;
            }
            return TryPlayThrough(
                recording, recording.LastRecordedTick,
                expectedFingerprint, kernel, ingress,
                out result, out error, out detail);
        }

        /// <summary>
        /// Plays through an explicit trusted end tick. A prefix may end only
        /// on a persisted equal-hash checkpoint; this is how verification can
        /// target tick 10,000 even if a future format later carries a longer
        /// completed stream.
        /// </summary>
        public static bool TryPlayThrough(
            RelayRecordStreamFile recording, uint endTick,
            MatchFingerprint expectedFingerprint,
            SimulationKernel kernel, CommandIngress ingress,
            out RelayRecordPlaybackResult result,
            out RelayRecordPlaybackError error, out string detail)
        {
            if (recording == null) throw new ArgumentNullException(nameof(recording));
            if (expectedFingerprint == null) throw new ArgumentNullException(nameof(expectedFingerprint));
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));
            if (ingress == null) throw new ArgumentNullException(nameof(ingress));
            result = null;

            if (endTick < recording.InitialSnapshotTick || endTick > recording.LastRecordedTick
                || (endTick != recording.LastRecordedTick
                    && !recording.TryGetCheckpointHash(endTick, out _)))
            {
                error = RelayRecordPlaybackError.TickMismatch;
                detail = $"requested end tick {endTick} is not a recorded end or checkpoint";
                return false;
            }
            if (!recording.IsComplete
                && endTick == recording.LastRecordedTick
                && !recording.TryGetCheckpointHash(endTick, out _))
            {
                error = RelayRecordPlaybackError.IncompleteRecording;
                detail = $"partial recording tick {endTick} has no equal-hash checkpoint";
                return false;
            }

            string difference = recording.Fingerprint.FindFirstDifference(expectedFingerprint);
            if (difference != null)
            {
                error = RelayRecordPlaybackError.FingerprintMismatch;
                detail = $"fingerprint mismatch in {difference}";
                return false;
            }
            if (!kernel.TryRestoreSnapshot(recording.InitialSnapshotBytes))
            {
                error = RelayRecordPlaybackError.RestoreFailed;
                detail = "the fresh kernel refused the initial snapshot";
                return false;
            }

            int checkpointIndex = 0;
            for (int f = 0; f < recording.Frames.Count; f++)
            {
                RelayRecordTickFrame frame = recording.Frames[f];
                if (frame.Tick > endTick) break;
                if (kernel.CurrentTick.Value == uint.MaxValue
                    || frame.Tick != kernel.CurrentTick.Value + 1)
                {
                    error = RelayRecordPlaybackError.TickMismatch;
                    detail = $"frame {frame.Tick} is not the successor of kernel tick {kernel.CurrentTick.Value}";
                    return false;
                }

                for (int r = 0; r < frame.Records.Count; r++)
                {
                    CommandIngressResult intake = ingress.TryAcceptHistoricalRecordBytes(
                        frame.Records[r].Serialize(), out CommandRejectReason reason);
                    if (intake != CommandIngressResult.Accepted)
                    {
                        error = RelayRecordPlaybackError.RecordRejected;
                        detail = $"tick {frame.Tick} record {r} rejected: {intake}/{reason}";
                        return false;
                    }
                }

                CommandBatch batch = ingress.SealTickBatch(frame.Tick);
                if (batch.Count != frame.Records.Count)
                {
                    error = RelayRecordPlaybackError.RecordRejected;
                    detail = $"tick {frame.Tick} sealed {batch.Count} records, expected {frame.Records.Count}";
                    return false;
                }
                if (batch.Count > 0 && !kernel.SubmitBatch(batch))
                {
                    error = RelayRecordPlaybackError.BatchSubmitFailed;
                    detail = $"kernel refused tick {frame.Tick}";
                    return false;
                }
                kernel.StepTick();

                while (checkpointIndex < recording.Checkpoints.Count
                    && recording.Checkpoints[checkpointIndex].Tick == frame.Tick)
                {
                    RelayRecordCheckpoint checkpoint = recording.Checkpoints[checkpointIndex];
                    ulong actual = kernel.CalculateStateHash();
                    if (actual != checkpoint.StateHash)
                    {
                        error = RelayRecordPlaybackError.CheckpointMismatch;
                        detail = $"tick {frame.Tick} hash 0x{actual:X16} differs from checkpoint 0x{checkpoint.StateHash:X16}";
                        return false;
                    }
                    checkpointIndex++;
                }
            }

            int checkpointsThroughEnd = 0;
            while (checkpointsThroughEnd < recording.Checkpoints.Count
                && recording.Checkpoints[checkpointsThroughEnd].Tick <= endTick)
            {
                checkpointsThroughEnd++;
            }
            if (checkpointIndex != checkpointsThroughEnd)
            {
                error = RelayRecordPlaybackError.CheckpointMismatch;
                detail = "not every recorded checkpoint belongs to a played frame";
                return false;
            }

            ulong stateHash = kernel.CalculateStateHash();
            if (recording.Desync != null && endTick == recording.Desync.Tick
                && stateHash != recording.Desync.Slot0Hash
                && stateHash != recording.Desync.Slot1Hash)
            {
                error = RelayRecordPlaybackError.DesyncHashMismatch;
                detail =
                    $"desync playback hash 0x{stateHash:X16} matches neither " +
                    $"slot hash 0x{recording.Desync.Slot0Hash:X16}/0x{recording.Desync.Slot1Hash:X16}";
                return false;
            }
            result = new RelayRecordPlaybackResult(
                kernel.CurrentTick.Value, stateHash, checkpointIndex);
            error = RelayRecordPlaybackError.None;
            detail = "playback verified";
            return true;
        }
    }
}
