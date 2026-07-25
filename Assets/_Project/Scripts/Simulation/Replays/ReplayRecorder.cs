using System;
using System.Collections.Generic;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Replays
{
    /// <summary>
    /// Records a live match into the canonical replay container v1
    /// (docs/tech/SimulationCore.md section 8, layout in
    /// <see cref="ReplayFormat"/>). The host calls
    /// <see cref="RecordTick"/> once per tick, after
    /// <c>SimulationKernel.StepTick</c>, with the sealed batch that was
    /// applied at that tick and the kernel's deterministic results of that
    /// tick. Only accepted records of sealed batches are recorded —
    /// structurally rejected bytes never reached a batch and never enter the
    /// replay, while state-dependently failed commands stay in the stream
    /// with their deterministic rejection result (Commands.md section 4).
    /// <para>
    /// Every tick is recorded, including empty ones, so the recorded tick
    /// range is gapless: the playback reproduces the recorded end state hash
    /// exactly. Recording must start at the tick immediately after the
    /// initial snapshot; the recorder enforces gapless +1 ticks from the
    /// first recorded tick on, and the player verifies the alignment of the
    /// first frame against the restored snapshot tick.
    /// </para>
    /// <para>
    /// Misuse (skipped ticks, a batch that does not belong to the tick, a
    /// result count/identity mismatch against the batch) is a host
    /// programming error and throws; malformed world input cannot reach this
    /// class because its input is the already-sealed batch.
    /// </para>
    /// </summary>
    public sealed class ReplayRecorder
    {
        private sealed class FrameData
        {
            public uint Tick;
            public byte[][] RecordBytes;
            public ushort[] ResultCodes;
        }

        private readonly MatchFingerprint _fingerprint;
        private readonly byte[] _fingerprintBytes;
        private readonly byte[] _initialSnapshotBytes;
        private readonly List<FrameData> _frames = new List<FrameData>();

        private uint _lastTick;
        private bool _finalized;

        /// <summary>
        /// Creates a recorder. The initial snapshot must be the canonical
        /// snapshot of the state the fingerprint's InitialStateHash was
        /// computed over; a mismatch is a host programming error and throws,
        /// because such a replay could never pass its own consistency check.
        /// </summary>
        public ReplayRecorder(MatchFingerprint fingerprint, byte[] initialSnapshotBytes)
        {
            _fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
            if (initialSnapshotBytes == null) throw new ArgumentNullException(nameof(initialSnapshotBytes));
            if (!SnapshotReader.TryRead(initialSnapshotBytes, out SnapshotFile snapshot, out _))
            {
                throw new ArgumentException("Initial snapshot is not a valid canonical snapshot.", nameof(initialSnapshotBytes));
            }
            if (snapshot.StateHash != fingerprint.InitialStateHash)
            {
                throw new ArgumentException(
                    "Initial snapshot state hash does not match the fingerprint's InitialStateHash.",
                    nameof(initialSnapshotBytes));
            }
            _initialSnapshotBytes = (byte[])initialSnapshotBytes.Clone();
            _fingerprintBytes = fingerprint.Serialize();
        }

        /// <summary>The fingerprint this replay is bound to.</summary>
        public MatchFingerprint Fingerprint => _fingerprint;

        /// <summary>Number of ticks recorded so far.</summary>
        public int RecordedTickCount => _frames.Count;

        /// <summary>
        /// Records one tick. <paramref name="tick"/> must be exactly one
        /// above the previously recorded tick; <paramref name="appliedBatch"/>
        /// must be the sealed batch of this tick (empty batches included);
        /// <paramref name="results"/> must be the kernel's results of this
        /// tick, one per record in batch order with matching stream identity.
        /// </summary>
        public void RecordTick(uint tick, CommandBatch appliedBatch, IReadOnlyList<CommandResult> results)
        {
            if (appliedBatch == null) throw new ArgumentNullException(nameof(appliedBatch));
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (_finalized) throw new InvalidOperationException("Replay already finalized.");
            if (_frames.Count > 0 && tick != _lastTick + 1)
            {
                throw new InvalidOperationException(
                    $"Recorded ticks must be gapless (+1); got {_lastTick} then {tick}.");
            }
            if (appliedBatch.TargetTick != tick)
            {
                throw new InvalidOperationException(
                    $"Batch target tick {appliedBatch.TargetTick} does not match recorded tick {tick}.");
            }
            if (results.Count != appliedBatch.Count)
            {
                throw new InvalidOperationException(
                    $"Result count {results.Count} does not match batch record count {appliedBatch.Count}.");
            }

            var frame = new FrameData
            {
                Tick = tick,
                RecordBytes = new byte[appliedBatch.Count][],
                ResultCodes = new ushort[appliedBatch.Count],
            };
            for (int i = 0; i < appliedBatch.Count; i++)
            {
                CommandRecord record = appliedBatch.Records[i];
                CommandResult result = results[i];
                if (record.TargetTick != tick
                    || result.PlayerSlot != record.PlayerSlot
                    || result.Sequence != record.Sequence
                    || result.TargetTick != record.TargetTick
                    || result.Kind != record.Kind)
                {
                    throw new InvalidOperationException(
                        $"Result {i} does not identify its record; the kernel result contract is broken.");
                }
                frame.RecordBytes[i] = record.Serialize();
                frame.ResultCodes[i] = (ushort)result.Code;
            }

            _frames.Add(frame);
            _lastTick = tick;
        }

        /// <summary>
        /// Seals the replay and serializes the canonical container, computing
        /// the running hash chain and binding
        /// <paramref name="finalStateHash"/> — the kernel's state hash after
        /// the last recorded tick — into the final chain step. Callable once.
        /// </summary>
        public byte[] Finalize(ulong finalStateHash)
        {
            if (_finalized) throw new InvalidOperationException("Replay already finalized.");
            _finalized = true;

            var writer = new SnapshotBlockWriter(1024);
            writer.WriteBytes(ReplayFormat.Magic);
            writer.WriteUInt16(ReplayFormat.FormatVersion);
            writer.WriteLengthPrefixed(_fingerprintBytes);
            writer.WriteLengthPrefixed(_initialSnapshotBytes);
            writer.WriteUInt32(unchecked((uint)_frames.Count));

            ulong chain = ReplayFormat.ComputeGenesisChainHash(_fingerprintBytes);
            for (int f = 0; f < _frames.Count; f++)
            {
                FrameData frame = _frames[f];
                writer.WriteUInt32(frame.Tick);
                writer.WriteUInt16(unchecked((ushort)frame.RecordBytes.Length));
                for (int r = 0; r < frame.RecordBytes.Length; r++)
                {
                    byte[] recordBytes = frame.RecordBytes[r];
                    writer.WriteUInt16(unchecked((ushort)recordBytes.Length));
                    writer.WriteBytes(recordBytes);
                    writer.WriteUInt16(frame.ResultCodes[r]);
                }
                chain = ReplayFormat.ComputeTickChainHash(chain, frame.Tick, frame.RecordBytes, frame.ResultCodes);
                writer.WriteUInt64(chain);
            }

            writer.WriteUInt64(finalStateHash);
            writer.WriteUInt64(ReplayFormat.ComputeFinalChainHash(chain, finalStateHash));
            return writer.ToArray();
        }
    }
}
