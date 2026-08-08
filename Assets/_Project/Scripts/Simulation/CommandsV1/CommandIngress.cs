using System;
using System.Collections.Generic;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>A validated session action pending execution at the next completed tick boundary.</summary>
    public readonly struct SessionActionRequest
    {
        public CommandKind Kind { get; }
        public uint EnqueueTick { get; }

        public SessionActionRequest(CommandKind kind, uint enqueueTick)
        {
            Kind = kind;
            EnqueueTick = enqueueTick;
        }
    }

    /// <summary>
    /// Ingress authority (docs/tech/Commands.md section 1): turns
    /// <see cref="CommandIntent"/> values into sealed records, validates every
    /// incoming record structurally, owns the authoritative
    /// <see cref="CommandDedupeState"/>, enforces the backpressure limits
    /// before sealing and seals per-tick <see cref="CommandBatch"/> objects —
    /// the only command input the kernel accepts.
    /// <para>
    /// Every record — locally produced via the loopback or later received from
    /// a network transport — enters through <see cref="TryAcceptRecordBytes"/>
    /// as raw canonical bytes and passes the identical structural validation
    /// (Commands.md sections 2 and 4). Structural failures are rejected and
    /// never recorded.
    /// </para>
    /// <para>
    /// Trust boundary by construction: the <see cref="CommandRecord"/>
    /// constructor is internal, so only this assembly can mint records; UI and
    /// AI assemblies can only build intents.
    /// </para>
    /// </summary>
    public sealed class CommandIngress
    {
        /// <summary>
        /// Provisional cap for pending session actions (pause/save/load are
        /// user-paced; the spec fixes no number). Q-040 candidate.
        /// </summary>
        public const int MaxPendingSessionActions = 64;

        private const byte StateVersion = 1;

        private readonly MatchSession _session;
        private ICommandTransport _transport;
        private CommandDedupeState _dedupe;
        private readonly List<SessionActionRequest> _pendingSessionActions = new List<SessionActionRequest>();

        private CommandIngressResult _lastIntakeResult = CommandIngressResult.Accepted;
        private CommandRejectReason _lastIntakeReason = CommandRejectReason.None;

        public CommandIngress(MatchSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _dedupe = new CommandDedupeState();
        }

        /// <summary>
        /// Binds the transport exactly once (the loopback binds itself in its
        /// constructor). Intent submission before a transport is bound is a
        /// programming error.
        /// </summary>
        public void BindTransport(ICommandTransport transport)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (_transport != null) throw new InvalidOperationException("Transport already bound.");
            _transport = transport;
        }

        /// <summary>The session this ingress is bound to.</summary>
        public MatchSession Session => _session;

        /// <summary>The authoritative sequence/dedupe state (serialized into snapshots).</summary>
        public CommandDedupeState DedupeState => _dedupe;

        /// <summary>Number of pending (accepted, not yet sealed) records.</summary>
        public int PendingCount => _dedupe.PendingCount;

        /// <summary>Number of pending session actions awaiting a completed tick boundary.</summary>
        public int PendingSessionActionCount => _pendingSessionActions.Count;

        /// <summary>
        /// Submits a UI/AI intent. The ingress assigns the session-bound slot,
        /// the next monotone sequence and the target tick
        /// (EnqueueTick + InputDelayTicks), then routes the serialized record
        /// through the transport back into its own validating intake.
        /// <para>
        /// With the synchronous <see cref="LocalLoopbackTransport"/> the
        /// returned value is the intake verdict; a rejection burns the assigned
        /// sequence (sequences are never reused, Commands.md section 1).
        /// Session actions never become records: they are validated and queued
        /// for execution at the next completed tick boundary.
        /// </para>
        /// </summary>
        public CommandIngressResult TrySubmitIntent(in CommandIntent intent, out CommandRejectReason reason)
        {
            if (_transport is ICommandSubmissionReadiness readiness
                && !readiness.IsReadyForCommandSubmission)
            {
                reason = CommandRejectReason.TransportNotReady;
                return CommandIngressResult.Rejected;
            }

            if (CommandKindInfo.IsSessionAction(intent.Kind))
            {
                return TryQueueSessionAction(intent, out reason);
            }

            // Fail fast with the precise reason; the looped-back intake repeats
            // the identical validation so the transport path can never smuggle
            // an invalid record past this boundary.
            if (!CommandPayloadValidation.TryValidateStreamPayload(
                    intent.Kind, intent.PayloadVersion, intent.PayloadBytes.Span, out reason))
            {
                return CommandIngressResult.Rejected;
            }

            if (!_dedupe.TryAssignLocalSequence(_session.LocalSlot, out uint sequence))
            {
                reason = CommandRejectReason.SequenceOverflow;
                return CommandIngressResult.Rejected;
            }

            uint enqueueTick = _session.CurrentTick;
            uint targetTick = enqueueTick + _session.InputDelayTicks;
            if (targetTick < enqueueTick)
            {
                reason = CommandRejectReason.TickWindowViolation;
                return CommandIngressResult.Rejected;
            }

            var payload = new byte[intent.PayloadBytes.Length];
            intent.PayloadBytes.CopyTo(payload);
            var record = new CommandRecord(
                enqueueTick, targetTick, _session.LocalSlot, sequence,
                intent.Kind, intent.PayloadVersion, payload);

            if (_transport == null)
            {
                throw new InvalidOperationException("No transport bound to this ingress.");
            }
            _lastIntakeResult = CommandIngressResult.Accepted;
            _lastIntakeReason = CommandRejectReason.None;
            _transport.Send(record.Serialize());
            reason = _lastIntakeReason;
            return _lastIntakeResult;
        }

        /// <summary>
        /// Validating intake for canonical record bytes from any transport.
        /// Performs the full structural validation of Commands.md section 4:
        /// parser limits, active session slot, known kind/version, canonical
        /// payload, tick window, sequence/dedupe rules and queue/batch
        /// capacity (backpressure, checked before sealing).
        /// </summary>
        public CommandIngressResult TryAcceptRecordBytes(
            ReadOnlySpan<byte> bytes, out CommandRejectReason reason)
        {
            CommandIngressResult result = TryAcceptRecordBytes(bytes, checkLiveTickWindow: true, out reason);
            _lastIntakeResult = result;
            _lastIntakeReason = reason;
            return result;
        }

        /// <summary>
        /// Intake for replay import. Historical target ticks are accepted only
        /// from an already fingerprint-checked stream (Commands.md section 2);
        /// the caller guarantees that property — every other structural rule is
        /// still enforced here.
        /// </summary>
        public CommandIngressResult TryAcceptHistoricalRecordBytes(
            ReadOnlySpan<byte> bytes, out CommandRejectReason reason)
        {
            return TryAcceptRecordBytes(bytes, checkLiveTickWindow: false, out reason);
        }

        /// <summary>
        /// Seals all pending records for <paramref name="targetTick"/> into an
        /// immutable batch sorted by (TargetTick, PlayerSlot, Sequence). The
        /// batch capacity was already enforced at intake, so a sealed batch
        /// always satisfies MaxBatchRecordsPerTick.
        /// </summary>
        public CommandBatch SealTickBatch(uint targetTick)
        {
            List<CommandRecord> drained = _dedupe.DrainPendingForTick(targetTick);
            drained.Sort(CommandBatch.CompareRecords);
            return new CommandBatch(targetTick, drained.ToArray());
        }

        /// <summary>
        /// Returns the pending session actions in submission order and clears
        /// the queue. The host executes them at a completed tick boundary only;
        /// pause blocks further kernel/AI ticks, unpause stays receivable, save
        /// and load use the completed snapshot and never fabricate a tick
        /// (Commands.md section 5).
        /// </summary>
        public SessionActionRequest[] TakePendingSessionActions()
        {
            var actions = _pendingSessionActions.ToArray();
            _pendingSessionActions.Clear();
            return actions;
        }

        /// <summary>
        /// Serializes the authoritative ingress state (dedupe/sequence state
        /// including pending records, plus pending session actions) for
        /// snapshots (Commands.md section 3, SimulationCore.md section 3).
        /// </summary>
        public byte[] SerializeState()
        {
            byte[] dedupeBytes = _dedupe.Serialize();
            var bytes = new List<byte>(dedupeBytes.Length + 8);
            bytes.Add(StateVersion);
            WriteUInt32(bytes, unchecked((uint)dedupeBytes.Length));
            bytes.AddRange(dedupeBytes);
            WriteUInt16(bytes, unchecked((ushort)_pendingSessionActions.Count));
            for (int i = 0; i < _pendingSessionActions.Count; i++)
            {
                WriteUInt32(bytes, _pendingSessionActions[i].EnqueueTick);
                WriteUInt16(bytes, (ushort)_pendingSessionActions[i].Kind);
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// Fully parses and validates snapshot state bytes produced by
        /// <see cref="SerializeState"/> without mutating this ingress
        /// (two-phase kernel restore, docs/tech/Serialization.md section 5).
        /// </summary>
        public bool TryValidateState(ReadOnlySpan<byte> bytes)
        {
            return TryReadState(bytes, out _, out _);
        }

        /// <summary>
        /// Restores the ingress state from snapshot bytes. Lengths are checked
        /// before allocation; every pending record is revalidated structurally
        /// (docs/tech/Commands.md section 4), including that its player slot is
        /// an active slot of this session. Malformed input returns false
        /// without mutation. Pending commands survive the restore with
        /// identical acceptance and sealing behaviour.
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> bytes)
        {
            if (!TryReadState(bytes, out CommandDedupeState restored,
                    out List<SessionActionRequest> actions))
            {
                return false;
            }

            _dedupe = restored;
            _pendingSessionActions.Clear();
            _pendingSessionActions.AddRange(actions);
            return true;
        }

        /// <summary>
        /// Parses and fully validates snapshot state bytes into committed-ready
        /// locals. Never mutates this ingress.
        /// </summary>
        private bool TryReadState(
            ReadOnlySpan<byte> bytes, out CommandDedupeState restored,
            out List<SessionActionRequest> actions)
        {
            restored = null;
            actions = null;

            var reader = new CommandPayloadReader(bytes);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt32(out uint dedupeLength)) return false;
            if (dedupeLength > int.MaxValue) return false;
            if (!reader.TryReadBytes((int)dedupeLength, out ReadOnlySpan<byte> dedupeBytes)) return false;
            if (!CommandDedupeState.TryDeserialize(dedupeBytes, out CommandDedupeState parsedDedupe)) return false;

            // Slot activity is session state: a snapshot carrying pending
            // records for a slot this session does not run is rejected whole.
            if (!parsedDedupe.AllPendingSlotsAre(_session.IsActiveSlot)) return false;

            if (!reader.TryReadUInt16(out ushort actionCount)) return false;
            var parsedActions = new List<SessionActionRequest>(actionCount);
            for (int i = 0; i < actionCount; i++)
            {
                if (!reader.TryReadUInt32(out uint enqueueTick)) return false;
                if (!reader.TryReadUInt16(out ushort kindValue)) return false;
                var kind = (CommandKind)kindValue;
                if (!CommandKindInfo.IsSessionAction(kind)) return false;
                parsedActions.Add(new SessionActionRequest(kind, enqueueTick));
            }
            if (reader.Remaining != 0) return false;

            restored = parsedDedupe;
            actions = parsedActions;
            return true;
        }

        private CommandIngressResult TryQueueSessionAction(
            in CommandIntent intent, out CommandRejectReason reason)
        {
            reason = CommandRejectReason.None;
            if (intent.PayloadVersion != CommandLimits.PayloadVersionV1)
            {
                reason = CommandRejectReason.UnknownPayloadVersion;
                return CommandIngressResult.Rejected;
            }
            if (intent.PayloadBytes.Length != 0)
            {
                // Schema v1 session actions carry an empty payload.
                reason = CommandRejectReason.PayloadMalformed;
                return CommandIngressResult.Rejected;
            }
            if (_pendingSessionActions.Count >= MaxPendingSessionActions)
            {
                reason = CommandRejectReason.PendingQueueFull;
                return CommandIngressResult.Rejected;
            }
            _pendingSessionActions.Add(new SessionActionRequest(intent.Kind, _session.CurrentTick));
            return CommandIngressResult.Accepted;
        }

        private CommandIngressResult TryAcceptRecordBytes(
            ReadOnlySpan<byte> bytes, bool checkLiveTickWindow, out CommandRejectReason reason)
        {
            reason = CommandRejectReason.None;

            if (!CommandRecord.TryDeserialize(bytes, out CommandRecord record, out int consumed))
            {
                reason = bytes.Length >= 2
                    ? CommandRejectReason.RecordLengthInvalid
                    : CommandRejectReason.RecordTruncated;
                return CommandIngressResult.Rejected;
            }
            if (consumed != bytes.Length)
            {
                // The intake accepts exactly one record per call; trailing
                // bytes are a structural framing error, never silently ignored.
                reason = CommandRejectReason.TrailingBytes;
                return CommandIngressResult.Rejected;
            }

            if (!_session.IsActiveSlot(record.PlayerSlot))
            {
                reason = CommandRejectReason.InactiveSlot;
                return CommandIngressResult.Rejected;
            }

            if (!CommandPayloadValidation.TryValidateStreamPayload(
                    record.Kind, record.PayloadVersion, record.Payload.Span, out reason))
            {
                return CommandIngressResult.Rejected;
            }

            if (checkLiveTickWindow)
            {
                uint expectedTarget = record.EnqueueTick + _session.InputDelayTicks;
                bool targetOverflowed = expectedTarget < record.EnqueueTick;
                if (targetOverflowed || record.TargetTick != expectedTarget)
                {
                    reason = CommandRejectReason.TickWindowViolation;
                    return CommandIngressResult.Rejected;
                }
                if (record.TargetTick <= _session.CurrentTick)
                {
                    reason = CommandRejectReason.TickWindowViolation;
                    return CommandIngressResult.Rejected;
                }
            }

            if (record.Sequence == 0)
            {
                reason = CommandRejectReason.SequenceZero;
                return CommandIngressResult.Rejected;
            }

            // Dedupe on (PlayerSlot, Sequence): pending entries are compared
            // byte-exactly; a same-key different-bytes record is a
            // deterministic conflict and is rejected (Commands.md section 3).
            if (_dedupe.TryGetPending(record.PlayerSlot, record.Sequence, out CommandRecord pending))
            {
                if (pending == record)
                {
                    return CommandIngressResult.DuplicateIgnored;
                }
                reason = CommandRejectReason.DedupeConflict;
                return CommandIngressResult.Rejected;
            }

            // A completed sequence never bypasses dedupe: it is dropped as an
            // idempotent re-delivery and is never re-applied.
            if (_dedupe.IsCompleted(record.PlayerSlot, record.Sequence))
            {
                return CommandIngressResult.DuplicateIgnored;
            }

            // Backpressure limits, enforced before the record may be sealed.
            if (_dedupe.PendingCount >= CommandLimits.MaxPendingRecords)
            {
                reason = CommandRejectReason.PendingQueueFull;
                return CommandIngressResult.Rejected;
            }
            if (_dedupe.PendingCountForTick(record.TargetTick) >= CommandLimits.MaxBatchRecordsPerTick)
            {
                reason = CommandRejectReason.BatchCapacityExceeded;
                return CommandIngressResult.Rejected;
            }

            _dedupe.AddPending(record);
            // The sequence floor tracks the accepted stream on every node
            // identically (stream-derived authoritative sequence state); for
            // replay import this is what reconstructs the recording host's
            // floor from the recorded records alone.
            _dedupe.RaiseSequenceFloor(record.PlayerSlot, record.Sequence);
            return CommandIngressResult.Accepted;
        }

        private static void WriteUInt16(List<byte> bytes, ushort value)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
        }

        private static void WriteUInt32(List<byte> bytes, uint value)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 24));
        }
    }
}
