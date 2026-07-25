using System;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Replays
{
    /// <summary>
    /// One parsed tick frame of a replay: the tick, the accepted records in
    /// canonical order with their recorded deterministic results and the
    /// stored chain value after this tick. The source offsets are a
    /// diagnostic surface for desync forensics and tooling (they locate the
    /// frame inside the source buffer); they are not part of the canonical
    /// contract.
    /// </summary>
    public sealed class ReplayTickFrame
    {
        public uint Tick { get; }
        public byte[][] RecordBytes { get; }
        public CommandRecord[] Records { get; }
        public CommandResultCode[] ResultCodes { get; }
        public ulong ChainHash { get; }

        /// <summary>Offset of the frame start (tick field) inside the source buffer.</summary>
        public int SourceOffset { get; }

        /// <summary>Whole frame size including the trailing chain hash.</summary>
        public int SourceLength { get; }

        /// <summary>Offset of each record's outer u16 length prefix inside the source buffer.</summary>
        public int[] RecordSourceOffsets { get; }

        internal ReplayTickFrame(
            uint tick, byte[][] recordBytes, CommandRecord[] records,
            CommandResultCode[] resultCodes, ulong chainHash,
            int sourceOffset, int sourceLength, int[] recordSourceOffsets)
        {
            Tick = tick;
            RecordBytes = recordBytes;
            Records = records;
            ResultCodes = resultCodes;
            ChainHash = chainHash;
            SourceOffset = sourceOffset;
            SourceLength = sourceLength;
            RecordSourceOffsets = recordSourceOffsets;
        }

        /// <summary>Number of records in this frame.</summary>
        public int RecordCount => Records.Length;
    }

    /// <summary>
    /// A fully parsed and verified canonical replay (docs/tech/SimulationCore.md
    /// section 8, layout in <see cref="ReplayFormat"/>): fingerprint, embedded
    /// initial snapshot, every tick frame and the trailer. Parsing performs
    /// the complete structural validation up front — lengths before
    /// allocation, structurally revalidated records (a structurally invalid
    /// command never reached the canonical stream and therefore rejects the
    /// replay), canonical record order, consecutive ticks, and the
    /// incremental hash chain verified frame by frame, so the first
    /// mismatching frame is the first tampered tick.
    /// </summary>
    public sealed class ReplayFile
    {
        public MatchFingerprint Fingerprint { get; }
        public byte[] InitialSnapshotBytes { get; }
        public ReplayTickFrame[] Frames { get; }
        public ulong FinalStateHash { get; }
        public ulong FinalChainHash { get; }

        private ReplayFile(
            MatchFingerprint fingerprint, byte[] initialSnapshotBytes,
            ReplayTickFrame[] frames, ulong finalStateHash, ulong finalChainHash)
        {
            Fingerprint = fingerprint;
            InitialSnapshotBytes = initialSnapshotBytes;
            Frames = frames;
            FinalStateHash = finalStateHash;
            FinalChainHash = finalChainHash;
        }

        /// <summary>The tick one past the last recorded tick, or null for an empty replay.</summary>
        public uint? LastRecordedTick => Frames.Length == 0 ? (uint?)null : Frames[Frames.Length - 1].Tick;

        /// <summary>
        /// Parses and fully verifies a replay. Never throws on malformed
        /// input; every rejection maps to one <see cref="ReplayReadError"/>.
        /// </summary>
        public static bool TryParse(byte[] bytes, out ReplayFile file, out ReplayReadError error)
        {
            file = null;
            error = ReplayReadError.None;
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length > ReplayFormat.MaxFileBytes)
            {
                error = ReplayReadError.FileTooLarge;
                return false;
            }
            if (bytes.Length < ReplayFormat.HeaderFixedBytes)
            {
                error = ReplayReadError.TruncatedHeader;
                return false;
            }
            for (int i = 0; i < ReplayFormat.Magic.Length; i++)
            {
                if (bytes[i] != ReplayFormat.Magic[i])
                {
                    error = ReplayReadError.BadMagic;
                    return false;
                }
            }

            var reader = new SnapshotBlockReader(bytes);
            reader.TryReadBytes(ReplayFormat.Magic.Length, out _);
            reader.TryReadUInt16(out ushort formatVersion);
            if (formatVersion != ReplayFormat.FormatVersion)
            {
                error = ReplayReadError.UnsupportedFormatVersion;
                return false;
            }

            // ---- Fingerprint (length checked before allocation). ----
            if (!reader.TryReadUInt32(out uint fingerprintLength)
                || fingerprintLength == 0
                || fingerprintLength > ReplayFormat.MaxFingerprintBytes
                || fingerprintLength > (uint)reader.Remaining)
            {
                error = ReplayReadError.FingerprintLengthInvalid;
                return false;
            }
            reader.TryReadBytes((int)fingerprintLength, out ReadOnlySpan<byte> fingerprintSpan);
            byte[] fingerprintBytes = fingerprintSpan.ToArray();
            if (!MatchFingerprint.TryParse(fingerprintBytes, out MatchFingerprint fingerprint))
            {
                error = ReplayReadError.FingerprintMalformed;
                return false;
            }

            // ---- Embedded initial snapshot (length checked before allocation). ----
            if (!reader.TryReadUInt32(out uint snapshotLength)
                || snapshotLength == 0
                || snapshotLength > SnapshotFormat.MaxFileBytes
                || snapshotLength > (uint)reader.Remaining)
            {
                error = ReplayReadError.SnapshotLengthInvalid;
                return false;
            }
            reader.TryReadBytes((int)snapshotLength, out ReadOnlySpan<byte> snapshotSpan);
            byte[] snapshotBytes = snapshotSpan.ToArray();
            if (!SnapshotReader.TryRead(snapshotBytes, out SnapshotFile snapshot, out _))
            {
                error = ReplayReadError.SnapshotMalformed;
                return false;
            }
            if (snapshot.StateHash != fingerprint.InitialStateHash)
            {
                error = ReplayReadError.FingerprintSnapshotMismatch;
                return false;
            }

            // ---- Tick frames (count bounded arithmetically before allocation). ----
            if (!reader.TryReadUInt32(out uint tickCount)) { error = ReplayReadError.FrameTruncated; return false; }
            long maxFrames = (reader.Remaining - (long)ReplayFormat.TrailerBytes) / ReplayFormat.FrameFixedBytes;
            if (maxFrames < 0 || tickCount > (ulong)maxFrames)
            {
                error = ReplayReadError.TickCountInvalid;
                return false;
            }

            var frames = new ReplayTickFrame[tickCount];
            ulong chain = ReplayFormat.ComputeGenesisChainHash(fingerprintBytes);
            uint previousTick = 0;
            for (long f = 0; f < tickCount; f++)
            {
                if (!TryParseFrame(bytes, ref reader, ref chain,
                        f > 0, previousTick, out ReplayTickFrame frame, out error))
                {
                    return false;
                }
                previousTick = frame.Tick;
                frames[f] = frame;
            }

            // ---- Trailer. ----
            if (reader.Remaining < ReplayFormat.TrailerBytes)
            {
                error = ReplayReadError.TruncatedTrailer;
                return false;
            }
            reader.TryReadUInt64(out ulong finalStateHash);
            reader.TryReadUInt64(out ulong finalChainHash);
            if (ReplayFormat.ComputeFinalChainHash(chain, finalStateHash) != finalChainHash)
            {
                error = ReplayReadError.FinalChainMismatch;
                return false;
            }
            if (reader.Remaining != 0)
            {
                error = ReplayReadError.TrailingBytes;
                return false;
            }

            file = new ReplayFile(fingerprint, snapshotBytes, frames, finalStateHash, finalChainHash);
            return true;
        }

        /// <summary>
        /// Parses and chain-verifies one tick frame; updates the running
        /// chain value. Ticks must be strictly consecutive across frames.
        /// </summary>
        private static bool TryParseFrame(
            byte[] source, ref SnapshotBlockReader reader, ref ulong chain,
            bool requireSuccessor, uint previousTick,
            out ReplayTickFrame frame, out ReplayReadError error)
        {
            frame = null;
            error = ReplayReadError.None;

            int frameOffset = source.Length - reader.Remaining;
            if (!reader.TryReadUInt32(out uint tick)) { error = ReplayReadError.FrameTruncated; return false; }
            if (requireSuccessor && (previousTick == uint.MaxValue || tick != previousTick + 1))
            {
                error = ReplayReadError.NonConsecutiveTicks;
                return false;
            }
            if (!reader.TryReadUInt16(out ushort recordCount)) { error = ReplayReadError.FrameTruncated; return false; }
            if (recordCount > CommandLimits.MaxBatchRecordsPerTick)
            {
                error = ReplayReadError.RecordCountExceeded;
                return false;
            }

            var recordBytes = new byte[recordCount][];
            var records = new CommandRecord[recordCount];
            var resultCodes = new CommandResultCode[recordCount];
            var chainCodes = new ushort[recordCount];
            var offsets = new int[recordCount];
            for (int r = 0; r < recordCount; r++)
            {
                offsets[r] = source.Length - reader.Remaining;
                if (!reader.TryReadUInt16(out ushort recordLength)) { error = ReplayReadError.FrameTruncated; return false; }
                if (recordLength < CommandLimits.HeaderBytes
                    || recordLength > CommandLimits.MaxRecordBytes
                    || recordLength > reader.Remaining)
                {
                    error = ReplayReadError.RecordLengthInvalid;
                    return false;
                }
                reader.TryReadBytes(recordLength, out ReadOnlySpan<byte> recordSpan);
                if (!CommandRecord.TryDeserialize(recordSpan, out CommandRecord record, out int consumed)
                    || consumed != recordLength)
                {
                    error = ReplayReadError.RecordMalformed;
                    return false;
                }
                // Structurally invalid commands never entered the canonical
                // stream (Commands.md section 4), so they reject the replay.
                if (record.Sequence == 0
                    || !CommandPayloadValidation.TryValidateStreamPayload(
                        record.Kind, record.PayloadVersion, record.Payload.Span, out _))
                {
                    error = ReplayReadError.StructurallyInvalidRecord;
                    return false;
                }
                if (record.TargetTick != tick)
                {
                    error = ReplayReadError.RecordTargetTickMismatch;
                    return false;
                }
                if (r > 0 && CommandBatch.CompareRecords(records[r - 1], record) >= 0)
                {
                    error = ReplayReadError.NonCanonicalRecordOrder;
                    return false;
                }

                if (!reader.TryReadUInt16(out ushort resultCode)) { error = ReplayReadError.FrameTruncated; return false; }
                if (resultCode < (ushort)CommandResultCode.Applied
                    || resultCode > (ushort)CommandResultCode.RejectedOnCooldown)
                {
                    error = ReplayReadError.UnknownResultCode;
                    return false;
                }

                recordBytes[r] = recordSpan.ToArray();
                records[r] = record;
                resultCodes[r] = (CommandResultCode)resultCode;
                chainCodes[r] = resultCode;
            }

            chain = ReplayFormat.ComputeTickChainHash(chain, tick, recordBytes, chainCodes);
            if (!reader.TryReadUInt64(out ulong storedChain)) { error = ReplayReadError.FrameTruncated; return false; }
            if (storedChain != chain)
            {
                error = ReplayReadError.ChainMismatch;
                return false;
            }

            int frameLength = (source.Length - reader.Remaining) - frameOffset;
            frame = new ReplayTickFrame(
                tick, recordBytes, records, resultCodes, storedChain,
                frameOffset, frameLength, offsets);
            return true;
        }
    }
}
