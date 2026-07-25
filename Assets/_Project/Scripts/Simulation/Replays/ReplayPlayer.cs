using System;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Replays
{
    /// <summary>Deterministic refusal/failure codes of replay playback.</summary>
    public enum ReplayPlaybackError
    {
        /// <summary>Playback completed and verified.</summary>
        None = 0,

        /// <summary>The replay bytes failed parsing or chain verification.</summary>
        ParseFailed,

        /// <summary>
        /// The replay fingerprint differs from the host's expected
        /// fingerprint; the start is refused (SimulationCore.md section 6).
        /// </summary>
        FingerprintMismatch,

        /// <summary>The fresh kernel refused the embedded initial snapshot.</summary>
        RestoreFailed,

        /// <summary>
        /// The first recorded tick is not the tick immediately after the
        /// restored snapshot tick; the replay does not belong to this
        /// snapshot position.
        /// </summary>
        TickMismatch,

        /// <summary>
        /// A recorded record was rejected by the validating historical
        /// intake although the parser accepted it; the ingress contract is
        /// broken (implementation bug, never bad input).
        /// </summary>
        RecordRejected,

        /// <summary>The kernel refused a sealed playback batch (implementation bug).</summary>
        BatchSubmitFailed,

        /// <summary>
        /// The re-executed deterministic results of a tick differ from the
        /// recorded results — the run desynchronized from the recording.
        /// </summary>
        ResultMismatch,

        /// <summary>
        /// The state hash after the last recorded tick differs from the
        /// recorded final state hash — the run desynchronized from the
        /// recording.
        /// </summary>
        FinalStateMismatch,
    }

    /// <summary>
    /// Replay playback (docs/tech/SimulationCore.md sections 6 and 8): uses
    /// the same kernel and the same sources as the live host. It restores the
    /// embedded initial snapshot into a fresh, already started kernel, then
    /// replays every recorded tick through the identical sealed path — the
    /// recorded bytes re-enter via
    /// <see cref="CommandIngress.TryAcceptHistoricalRecordBytes"/>, are
    /// sealed per tick, submitted and applied by the kernel. The AI is never
    /// instantiated or applied again: its accepted commands are records of
    /// the stream (SimulationCore.md section 4).
    /// <para>
    /// Verification per run: exact fingerprint equality before the start
    /// (any divergence refuses with the differing field named), one
    /// re-executed <see cref="CommandResult"/> per recorded record compared
    /// value-exactly against the recording (state-dependent rejections
    /// included), and the kernel state hash after the last recorded tick
    /// against the recorded final state hash. The hash chain itself is
    /// verified at parse time, frame by frame.
    /// </para>
    /// <para>
    /// The caller owns host construction: the kernel must be fresh and
    /// started with the same stateful systems and capacities as the
    /// recording host (same sources), and the ingress must be bound to the
    /// kernel and to a session whose active slots match the fingerprint
    /// match configuration. The player never mutates anything outside the
    /// provided kernel/ingress.
    /// </para>
    /// </summary>
    public static class ReplayPlayer
    {
        /// <summary>
        /// Parses, verifies and plays a replay. Returns true only when the
        /// complete playback reproduced the recording; otherwise returns
        /// false with a deterministic <paramref name="error"/> and a
        /// human-readable <paramref name="detail"/>.
        /// </summary>
        /// <remarks>
        /// Early refusals (parse, chain, fingerprint, restore) happen before
        /// any mutation. A failure mid-playback (for example a result
        /// mismatch) leaves the provided kernel/ingress advanced to the
        /// failure point — callers must treat playback as single-use on a
        /// freshly built host (the intended verifier flow, D-046).
        /// The replay chain is unsigned: it detects corruption, not a fully
        /// fabricated replay. The authenticity anchor is the caller-supplied
        /// <paramref name="expectedFingerprint"/>, which must come from a
        /// trusted source (registry/attestation, D-046).
        /// </remarks>
        public static bool TryPlay(
            byte[] replayBytes,
            MatchFingerprint expectedFingerprint,
            SimulationKernel kernel,
            CommandIngress ingress,
            out ReplayPlaybackError error,
            out string detail)
        {
            if (replayBytes == null) throw new ArgumentNullException(nameof(replayBytes));
            if (!ReplayFile.TryParse(replayBytes, out ReplayFile replay, out ReplayReadError readError))
            {
                error = ReplayPlaybackError.ParseFailed;
                detail = $"replay parse failed: {readError}";
                return false;
            }
            return TryPlay(replay, expectedFingerprint, kernel, ingress, out error, out detail);
        }

        /// <summary>
        /// Verifies and plays an already parsed replay (see
        /// <see cref="TryPlay(byte[], MatchFingerprint, SimulationKernel, CommandIngress, out ReplayPlaybackError, out string)"/>).
        /// </summary>
        public static bool TryPlay(
            ReplayFile replay,
            MatchFingerprint expectedFingerprint,
            SimulationKernel kernel,
            CommandIngress ingress,
            out ReplayPlaybackError error,
            out string detail)
        {
            if (replay == null) throw new ArgumentNullException(nameof(replay));
            if (expectedFingerprint == null) throw new ArgumentNullException(nameof(expectedFingerprint));
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));
            if (ingress == null) throw new ArgumentNullException(nameof(ingress));

            // Fingerprint gate (SimulationCore.md section 6): any divergence
            // refuses the start before any state is touched.
            string difference = replay.Fingerprint.FindFirstDifference(expectedFingerprint);
            if (difference != null)
            {
                error = ReplayPlaybackError.FingerprintMismatch;
                detail = $"fingerprint mismatch in {difference}";
                return false;
            }

            if (!kernel.TryRestoreSnapshot(replay.InitialSnapshotBytes))
            {
                error = ReplayPlaybackError.RestoreFailed;
                detail = "the fresh kernel refused the embedded initial snapshot";
                return false;
            }

            ReplayTickFrame[] frames = replay.Frames;
            if (frames.Length > 0 && frames[0].Tick != kernel.CurrentTick.Value + 1)
            {
                error = ReplayPlaybackError.TickMismatch;
                detail = $"first recorded tick {frames[0].Tick} is not snapshot tick {kernel.CurrentTick.Value} + 1";
                return false;
            }

            for (int f = 0; f < frames.Length; f++)
            {
                ReplayTickFrame frame = frames[f];

                // Re-enter the recorded bytes through the same validating
                // intake the live transport uses; historical target ticks are
                // legal here because the stream is fingerprint-checked
                // (Commands.md sections 1 and 2).
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    CommandIngressResult intake = ingress.TryAcceptHistoricalRecordBytes(
                        frame.RecordBytes[r], out CommandRejectReason reason);
                    if (intake != CommandIngressResult.Accepted)
                    {
                        error = ReplayPlaybackError.RecordRejected;
                        detail = $"record {r} of tick {frame.Tick} rejected at intake: {intake}/{reason}";
                        return false;
                    }
                }

                CommandBatch batch = ingress.SealTickBatch(frame.Tick);
                if (batch.Count != frame.RecordCount)
                {
                    error = ReplayPlaybackError.RecordRejected;
                    detail = $"sealed batch of tick {frame.Tick} holds {batch.Count} records, expected {frame.RecordCount}";
                    return false;
                }
                if (batch.Count > 0 && !kernel.SubmitBatch(batch))
                {
                    error = ReplayPlaybackError.BatchSubmitFailed;
                    detail = $"kernel refused the sealed batch of tick {frame.Tick}";
                    return false;
                }

                kernel.StepTick();

                // Deterministic result verification: one recorded result per
                // record, compared value-exactly (state-dependent rejections
                // included, Commands.md section 4).
                var results = kernel.LastTickResults;
                if (results.Count != frame.RecordCount)
                {
                    error = ReplayPlaybackError.ResultMismatch;
                    detail = $"tick {frame.Tick}: {results.Count} results, expected {frame.RecordCount}";
                    return false;
                }
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    var expected = new CommandResult(frame.Records[r], frame.ResultCodes[r]);
                    if (results[r] != expected)
                    {
                        error = ReplayPlaybackError.ResultMismatch;
                        detail = $"tick {frame.Tick} record {r}: reproduced {results[r]}, recorded {expected}";
                        return false;
                    }
                }
            }

            ulong endHash = kernel.CalculateStateHash();
            if (endHash != replay.FinalStateHash)
            {
                error = ReplayPlaybackError.FinalStateMismatch;
                detail = $"end state hash {endHash:X16} differs from recorded {replay.FinalStateHash:X16}";
                return false;
            }

            error = ReplayPlaybackError.None;
            detail = "playback verified";
            return true;
        }
    }
}
