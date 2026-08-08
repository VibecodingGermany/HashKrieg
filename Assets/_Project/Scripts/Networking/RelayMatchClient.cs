using System;
using System.Collections.Generic;
using System.IO;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;

namespace Nova.Networking
{
    /// <summary>Lifecycle of <see cref="RelayMatchClient"/>.</summary>
    public enum RelayClientPhase
    {
        Disconnected = 0,
        /// <summary>Hello sent, waiting for the server's slot offer.</summary>
        WaitingOffer = 1,
        /// <summary>Local match setup done, fingerprint + initial snapshot sent, waiting for Start.</summary>
        WaitingStart = 2,
        /// <summary>Handshake complete, lockstep running.</summary>
        Running = 3,
        /// <summary>Match ended ordered (desync, peer lost, stall timeout or reject).</summary>
        Ended = 4,
    }

    /// <summary>Stable public lifecycle used by presentation and host code.</summary>
    public enum RelayMatchLifecycle
    {
        Disconnected = 0,
        Connecting = 1,
        WaitingStart = 2,
        Running = 3,
        Stalled = 4,
        Ended = 5,
    }

    /// <summary>
    /// The lockstep client engine (strand A2/A4/A5 of the sprint doc):
    /// drives the relay handshake, binds the network path to a local
    /// session/ingress pair as their <see cref="ICommandTransport"/>, owns
    /// the <see cref="LockstepBarrier"/> and steps the kernel only when the
    /// barrier releases the tick.
    /// <para>
    /// Own-record path, mirroring <see cref="LocalLoopbackTransport"/>:
    /// <see cref="Send"/> wraps the record in a CommandRecord frame to the
    /// relay (which forwards it to the peers) AND loops it back into the
    /// local validating intake in the same call — the relay never echoes a
    /// sender's own records, so there is exactly one delivery per record.
    /// </para>
    /// <para>
    /// Stall, never divergence: <see cref="TryStepTick"/> returns false
    /// while any active slot's input for the next tick is incomplete; the
    /// host shows "waiting for player N" (<see cref="StalledOnSlot"/>) and
    /// after <see cref="StallTimeoutSeconds"/> the peer counts as lost and
    /// the match ends ordered. Nothing is estimated, anticipated or
    /// discarded.
    /// </para>
    /// <para>
    /// Desync handling: every <see cref="StateHashIntervalTicks"/> ticks the
    /// canonical state hash goes to the relay; a reported mismatch flips the
    /// client to <see cref="RelayClientPhase.Ended"/> with
    /// <see cref="DesyncTick"/> set — the host writes its diagnosis dump
    /// (snapshot + record stream) at that point.
    /// </para>
    /// </summary>
    public sealed class RelayMatchClient : INetworkTransport, ICommandSubmissionReadiness
    {
        /// <summary>Interval of the state-hash reports the relay compares (5 s at 10 Hz).</summary>
        public const int StateHashIntervalTicks = 50;

        /// <summary>Wall-clock budget of a stall before the peer counts as lost (sprint A2c: 30 s).</summary>
        public const double StallTimeoutSeconds = 30.0;

        private readonly TcpRelayConnection _connection;
        private readonly Func<uint> _clockMilliseconds;
        private ulong _pendingMatchToken;
        private bool _helloPending;
        private bool _connectAttempted;
        private LockstepBarrier _barrier;
        private CommandIngress _ingress;
        private MatchSession _session;
        private uint _announcedThrough;
        private readonly Dictionary<uint, int> _localRecordsByTick = new Dictionary<uint, int>();
        private uint _stalledSinceMs = uint.MaxValue;
        private bool _stallActive;
        private bool _localInputClosed;
        private uint _pingCounter;
        private uint _pingSentMs;
        private bool _pingOutstanding;
        private DiagnosticRecordSpool _diagnosticRecordSpool;
        private string _diagnosticCaptureError = string.Empty;
        private readonly Dictionary<uint, CheckpointEvidence> _checkpointEvidence =
            new Dictionary<uint, CheckpointEvidence>();
        private SimulationKernel _lastKernel;
        private bool _diagnosticWritten;

        public RelayMatchClient()
            : this(() => unchecked((uint)Environment.TickCount))
        {
        }

        internal RelayMatchClient(Func<uint> clockMilliseconds)
        {
            _clockMilliseconds = clockMilliseconds
                ?? throw new ArgumentNullException(nameof(clockMilliseconds));
            _connection = new TcpRelayConnection(_clockMilliseconds);
            _connection.SetFrameHandler(OnFrame);
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(root)) root = Path.GetTempPath();
            DiagnosticDirectory = Path.Combine(root, "ProjectNova", "NetworkDiagnostics");
        }

        // ------------------------------------------------------------------
        // Handshake surface
        // ------------------------------------------------------------------

        public RelayClientPhase Phase { get; private set; } = RelayClientPhase.Disconnected;

        /// <summary>Presentation-safe lifecycle that keeps terminal and stall states explicit.</summary>
        public RelayMatchLifecycle Lifecycle
        {
            get
            {
                if (Phase == RelayClientPhase.Ended) return RelayMatchLifecycle.Ended;
                if (Phase == RelayClientPhase.Disconnected) return RelayMatchLifecycle.Disconnected;
                if (Phase == RelayClientPhase.WaitingOffer) return RelayMatchLifecycle.Connecting;
                if (Phase == RelayClientPhase.WaitingStart) return RelayMatchLifecycle.WaitingStart;
                return _stallActive ? RelayMatchLifecycle.Stalled : RelayMatchLifecycle.Running;
            }
        }

        /// <summary>Terminal/rejection reason, retained after the socket closes.</summary>
        public string StatusReason => Phase == RelayClientPhase.Ended
            ? EndReason
            : RejectReason;

        /// <summary>Offer payload of the server; valid once <see cref="Phase"/> reached WaitingStart inputs.</summary>
        public bool HasOffer { get; private set; }
        public byte AssignedSlot { get; private set; }
        public byte[] ActiveSlots { get; private set; }
        public ulong Seed { get; private set; }
        public uint InputDelayTicks { get; private set; }
        public ulong ServerDefinitionsHash64 { get; private set; }

        /// <summary>Server rejection reason when the handshake failed (empty otherwise).</summary>
        public string RejectReason { get; private set; } = string.Empty;

        /// <summary>Terminal cause when <see cref="Phase"/> is Ended (human-readable).</summary>
        public string EndReason { get; private set; } = string.Empty;

        /// <summary>True once the relay reported diverging state hashes; <see cref="DesyncTick"/> names the tick.</summary>
        public bool Desynced { get; private set; }
        public uint DesyncTick { get; private set; }

        /// <summary>Directory for per-client <c>*.novadiag</c> files; configurable for tests and hosts.</summary>
        public string DiagnosticDirectory { get; set; }

        /// <summary>Path written for the last desync, or null when no diagnosis was written.</summary>
        public string LastDiagnosticPath { get; private set; }

        /// <summary>Diagnostic write failure, or empty after a successful/no write.</summary>
        public string LastDiagnosticError { get; private set; } = string.Empty;

        // ------------------------------------------------------------------
        // Stall surface
        // ------------------------------------------------------------------

        /// <summary>True while the next tick waits on another slot's input.</summary>
        public bool IsStalled => _stallActive;

        /// <summary>The slot the next tick waits on, or -1 when not stalled.</summary>
        public int StalledOnSlot { get; private set; } = -1;

        /// <summary>Seconds the current stall has lasted (0 when not stalled).</summary>
        public double StallSeconds =>
            _stallActive
                ? ElapsedMilliseconds(_clockMilliseconds(), _stalledSinceMs) / 1000.0
                : 0.0;

        internal static uint ElapsedMilliseconds(uint now, uint startedAt)
        {
            return unchecked(now - startedAt);
        }

        // ------------------------------------------------------------------
        // INetworkTransport
        // ------------------------------------------------------------------

        public RelayConnectionState State
        {
            get
            {
                if (Phase == RelayClientPhase.Ended) return RelayConnectionState.Failed;
                if (Phase == RelayClientPhase.Disconnected) return RelayConnectionState.Disconnected;
                return _connection.State;
            }
        }

        /// <summary>Raw RTT measurement retained for diagnostics.</summary>
        public uint? RoundTripMilliseconds { get; private set; }

        /// <summary>RTT in canonical 100-ms simulation ticks, rounded up.</summary>
        public uint? RoundTripTicks => RoundTripMilliseconds.HasValue
            ? (RoundTripMilliseconds.Value + (1000u / Nova.Core.SimClock.TicksPerSecond) - 1)
                / (1000u / Nova.Core.SimClock.TicksPerSecond)
            : (uint?)null;

        public string LastError => !string.IsNullOrEmpty(EndReason) ? EndReason : _connection.LastError;

        /// <summary>
        /// The ingress may mint neither stream records nor session actions
        /// until the relay's authoritative Start frame has arrived.
        /// </summary>
        public bool IsReadyForCommandSubmission =>
            Phase == RelayClientPhase.Running && !_localInputClosed;

        /// <summary>Binds this client as the transport of the local session's ingress (exactly once).</summary>
        public void BindIngress(CommandIngress ingress)
        {
            if (ingress == null) throw new ArgumentNullException(nameof(ingress));
            if (_ingress != null) throw new InvalidOperationException("RelayMatchClient is already bound to an ingress.");
            _ingress = ingress;
            _session = ingress.Session;
            ingress.BindTransport(this);
        }

        /// <summary>Opens the relay connection and sends Hello. The match token never touches the repository or the log.</summary>
        public void Connect(string host, int port, ulong matchToken)
        {
            Connect(host, port, matchToken, 5000);
        }

        /// <summary>Connect overload with explicit timeout (INetworkTransport signature-free; the token rides in Hello).</summary>
        public void Connect(string host, int port, ulong matchToken, int timeoutMilliseconds)
        {
            // A match client is a single-session authority. Reusing it would
            // retain the old ingress/dedupe/barrier and is therefore a
            // fail-closed programming error: restart with a fresh client.
            if (_connectAttempted || Phase != RelayClientPhase.Disconnected
                || _ingress != null || HasOffer)
            {
                Phase = RelayClientPhase.Ended;
                EndReason = "relay client reuse refused — create a fresh client for a new match";
                return;
            }
            _connectAttempted = true;
            if (string.IsNullOrWhiteSpace(host) || port < 1 || port > 65535 || matchToken == 0)
            {
                Phase = RelayClientPhase.Ended;
                EndReason = "invalid relay endpoint or match token";
                return;
            }
            DisposeDiagnosticRecordSpool();
            _diagnosticCaptureError = string.Empty;
            _checkpointEvidence.Clear();
            _diagnosticWritten = false;
            LastDiagnosticPath = null;
            LastDiagnosticError = string.Empty;
            _pendingMatchToken = matchToken;
            _helloPending = true;
            if (!_connection.Connect(host, port, timeoutMilliseconds))
            {
                ClearPendingHello();
                Phase = RelayClientPhase.Ended;
                EndReason = _connection.LastError ?? "connect failed";
                return;
            }
            Phase = RelayClientPhase.WaitingOffer;
        }

        public void Disconnect()
        {
            ClearPendingHello();
            _connection.Disconnect();
            DisposeDiagnosticRecordSpool();
            if (Phase != RelayClientPhase.Ended)
            {
                Phase = RelayClientPhase.Disconnected;
            }
        }

        /// <summary>
        /// Sends the local proofs after the offer: the serialized
        /// MatchFingerprint (built with the server's seed and delay) and the
        /// canonical initial snapshot. The server compares both against its
        /// own build and the peer's, then starts or rejects the match.
        /// </summary>
        public void SubmitLocalProof(byte[] fingerprintBytes, byte[] initialSnapshotBytes)
        {
            if (Phase != RelayClientPhase.WaitingOffer || !HasOffer)
            {
                throw new InvalidOperationException("SubmitLocalProof requires a received server offer.");
            }
            if (!_connection.SendFrame(RelayFrameType.Fingerprint, fingerprintBytes)
                || !_connection.SendFrame(RelayFrameType.InitialSnapshot, initialSnapshotBytes))
            {
                EndMatch(_connection.LastError ?? "relay proof submission failed");
                return;
            }
            Phase = RelayClientPhase.WaitingStart;
        }

        // ------------------------------------------------------------------
        // Own-record transport path (ICommandTransport)
        // ------------------------------------------------------------------

        /// <summary>
        /// Sends one locally minted record to the relay for the peers AND
        /// loops it back into the local validating intake in the same call —
        /// exactly one delivery per record, identical validation for both
        /// directions (synchronous verdict like the loopback transport).
        /// The local intake verdict is authoritative: a locally REJECTED
        /// record never leaves the building — forwarding it anyway would
        /// let the peer accept and execute a record the local kernel never
        /// sees, which is the definition of a desync.
        /// </summary>
        public void Send(byte[] recordBytes)
        {
            if (_ingress == null) throw new InvalidOperationException("BindIngress must precede Send.");
            // ICommandTransport.Send cannot return a verdict. The hard
            // fail-closed guarantee here is therefore state-based: before
            // Start, no bytes reach either the local ingress or the socket,
            // and no barrier/accounting state changes.
            if (Phase != RelayClientPhase.Running || _localInputClosed) return;

            CommandIngressResult result = _ingress.TryAcceptRecordBytes(recordBytes, out _);
            if (result != CommandIngressResult.Accepted)
            {
                return;
            }
            if (!_connection.SendFrame(RelayFrameType.CommandRecord, recordBytes))
            {
                EndMatch(_connection.LastError ?? "relay command send failed");
                return;
            }
            if (Nova.Simulation.CommandsV1.CommandRecord.TryDeserialize(recordBytes, out var sentRecord, out int consumed)
                && consumed == recordBytes.Length)
            {
                _localRecordsByTick.TryGetValue(sentRecord.TargetTick, out int count);
                _localRecordsByTick[sentRecord.TargetTick] = count + 1;
            }
        }

        // ------------------------------------------------------------------
        // Per-frame pump and lockstep stepping
        // ------------------------------------------------------------------

        /// <summary>Pumps the socket, dispatches frames and checks the stall timeout. Call every host frame.</summary>
        public void Poll()
        {
            _connection.Poll();

            if (_helloPending && _connection.State == RelayConnectionState.Connected)
            {
                ulong matchToken = _pendingMatchToken;
                ClearPendingHello();
                if (!_connection.SendFrame(
                        RelayFrameType.Hello,
                        RelayProtocol.CreateHelloPayload(matchToken)))
                {
                    EndMatch(_connection.LastError ?? "relay hello failed");
                    return;
                }
            }

            if (_connection.State == RelayConnectionState.Failed
                && Phase != RelayClientPhase.Ended)
            {
                EndMatch(_connection.LastError ?? "relay connection failed");
                return;
            }

            if (Phase == RelayClientPhase.Running)
            {
                // RTT cadence: one ping every ~5 s of host frames (callers
                // pump at display rate; the counter keeps it cheap).
                _pingCounter++;
                if (_pingCounter >= 300)
                {
                    _pingCounter = 0;
                    if (!_pingOutstanding)
                    {
                        uint sentAt = _clockMilliseconds();
                        var probe = new byte[4];
                        RelayProtocol.WriteUInt32(probe, 0, sentAt);
                        if (!_connection.SendFrame(RelayFrameType.Ping, probe))
                        {
                            EndMatch(_connection.LastError ?? "relay ping send failed");
                            return;
                        }
                        _pingSentMs = sentAt;
                        _pingOutstanding = true;
                    }
                }

                if (_stallActive && StallSeconds > StallTimeoutSeconds)
                {
                    EndMatch($"peer slot {StalledOnSlot} delivered nothing for {StallTimeoutSeconds:0}s — counted as lost");
                }
            }
        }

        /// <summary>
        /// One lockstep iteration: seals and executes the next tick ONLY when
        /// the barrier released it — every active slot announced its input
        /// complete and the announced records arrived. Returns false while
        /// stalled; the simulation then simply does not advance.
        /// </summary>
        public bool TryStepTick(SimulationKernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));
            _lastKernel = kernel;
            if (Phase != RelayClientPhase.Running) return false;

            if (!_localInputClosed)
            {
                _localInputClosed = true;
                AnnounceLocalCompleteness(includeCurrentInputTick: true);
                if (Phase != RelayClientPhase.Running) return false;
            }

            uint nextTick = kernel.CurrentTick.Value + 1;
            if (!_barrier.IsTickReady(nextTick))
            {
                if (!_stallActive)
                {
                    _stallActive = true;
                    _stalledSinceMs = _clockMilliseconds();
                    DebugLog?.Invoke($"slot {AssignedSlot} stalls at tick {nextTick} waiting on slot {_barrier.WaitingOnSlot(nextTick)}");
                }
                StalledOnSlot = _barrier.WaitingOnSlot(nextTick);
                return false;
            }

            _stallActive = false;
            StalledOnSlot = -1;

            CommandBatch batch = _ingress.SealTickBatch(nextTick);
            if (batch.Count > 0)
            {
                if (!kernel.SubmitBatch(batch))
                {
                    EndMatch($"kernel refused the sealed batch of tick {nextTick} — the intake contract is broken");
                    return false;
                }
            }
            kernel.StepTick();
            _session.AdvanceTick();
            _localInputClosed = false;

            // Diagnostics are derived from what the kernel actually
            // applied, never from attempted sends or merely received bytes.
            for (int i = 0; i < batch.Count; i++)
            {
                if (_diagnosticRecordSpool == null)
                {
                    _diagnosticRecordSpool = new DiagnosticRecordSpool();
                }
                if (!_diagnosticRecordSpool.TryAppend(
                        batch.Records[i].Serialize(), out string captureError))
                {
                    _diagnosticCaptureError = captureError;
                    LastDiagnosticError = captureError;
                    EndMatch(captureError);
                    return true;
                }
            }
            _barrier.PruneThrough(nextTick);
            _localRecordsByTick.Remove(nextTick);

            if (nextTick % StateHashIntervalTicks == 0)
            {
                ulong stateHash = kernel.CalculateStateHash();
                byte[] snapshot = kernel.SaveSnapshot();
                if (!DesyncDiagnostic.TryReadSnapshotIdentity(
                        snapshot, out uint snapshotTick, out ulong snapshotHash, out string snapshotError)
                    || snapshotTick != nextTick || snapshotHash != stateHash)
                {
                    EndMatch(
                        $"checkpoint capture failed at tick {nextTick}: " +
                        (string.IsNullOrEmpty(snapshotError)
                            ? $"snapshot identity was tick {snapshotTick}, hash 0x{snapshotHash:X16}"
                            : snapshotError));
                    return true;
                }
                long recordBytes = _diagnosticRecordSpool?.ByteLength ?? 0;
                if (!DesyncDiagnostic.CanFit(
                        snapshot.Length, recordBytes, out string budgetError))
                {
                    _diagnosticCaptureError = budgetError;
                    LastDiagnosticError = budgetError;
                    EndMatch(budgetError);
                    return true;
                }
                _checkpointEvidence[nextTick] = new CheckpointEvidence(stateHash, snapshot);
                while (_checkpointEvidence.Count > 2)
                {
                    uint oldest = uint.MaxValue;
                    foreach (uint bufferedTick in _checkpointEvidence.Keys)
                    {
                        if (bufferedTick < oldest) oldest = bufferedTick;
                    }
                    _checkpointEvidence.Remove(oldest);
                }
                if (!_connection.SendFrame(RelayFrameType.StateHash,
                        RelayProtocol.CreateStateHashPayload(_session.LocalSlot, nextTick, stateHash)))
                {
                    EndMatch(_connection.LastError ?? "relay state-hash send failed");
                    return true;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------
        // Frame dispatch
        // ------------------------------------------------------------------

        /// <summary>Optional diagnostic sink for every received frame (desync/stall analysis; null in production).</summary>
        public Action<string> DebugLog;

        private void OnFrame(RelayFrameType type, byte[] payload)
        {
            DebugLog?.Invoke($"slot {AssignedSlot} <- {type} ({payload.Length} B)");
            switch (type)
            {
                case RelayFrameType.Offer:
                    if (Phase != RelayClientPhase.WaitingOffer
                        || !RelayProtocol.TryParseOffer(payload, out byte slot, out byte[] activeSlots,
                            out ulong seed, out uint delay, out ulong serverDefsHash)
                        || !HasValidOfferRoles(slot, activeSlots, seed))
                    {
                        ProtocolViolation("invalid or unexpected Offer");
                        return;
                    }
                    AssignedSlot = slot;
                    ActiveSlots = activeSlots;
                    Seed = seed;
                    InputDelayTicks = delay;
                    ServerDefinitionsHash64 = serverDefsHash;
                    HasOffer = true;
                    _barrier = new LockstepBarrier(slot, activeSlots);
                    _announcedThrough = 0;
                    break;

                case RelayFrameType.Start:
                    if (Phase != RelayClientPhase.WaitingStart
                        || payload.Length != 0
                        || _session == null || _ingress == null || _barrier == null)
                    {
                        ProtocolViolation("invalid or unexpected Start");
                        return;
                    }
                    Phase = RelayClientPhase.Running;
                    _localInputClosed = false;
                    AnnounceLocalCompleteness(includeCurrentInputTick: false);
                    break;

                case RelayFrameType.Reject:
                    string rejectReason = RelayProtocol.ParseReasonPayload(payload);
                    if (Phase == RelayClientPhase.Disconnected
                        || Phase == RelayClientPhase.Ended
                        || string.IsNullOrEmpty(rejectReason))
                    {
                        ProtocolViolation("invalid or unexpected Reject");
                        return;
                    }
                    RejectReason = rejectReason;
                    EndMatch($"rejected by relay: {RejectReason}");
                    break;

                case RelayFrameType.CommandRecord:
                    if (Phase != RelayClientPhase.Running
                        || _session == null || _ingress == null || _barrier == null
                        || !Nova.Simulation.CommandsV1.CommandRecord.TryDeserialize(
                            payload, out var record, out int consumed)
                        || consumed != payload.Length
                        || !IsActiveRemoteSlot(record.PlayerSlot))
                    {
                        ProtocolViolation("invalid or unexpected CommandRecord");
                        return;
                    }
                    Nova.Simulation.CommandsV1.CommandIngressResult intake =
                        _ingress.TryAcceptRecordBytes(payload, out CommandRejectReason intakeReason);
                    if (intake != CommandIngressResult.Accepted)
                    {
                        ProtocolViolation($"CommandRecord intake rejected ({intakeReason})");
                        return;
                    }
                    LockstepBarrierVerdict recordVerdict =
                        _barrier.NoteRemoteRecord(record.PlayerSlot, record.TargetTick);
                    if (recordVerdict != LockstepBarrierVerdict.Accepted)
                    {
                        ProtocolViolation(
                            $"CommandRecord barrier rejected ({recordVerdict})");
                        return;
                    }
                    DebugLog?.Invoke($"slot {AssignedSlot}: record(slot {record.PlayerSlot}, tick {record.TargetTick}, seq {record.Sequence}, {record.Kind}) intake={intake}/{intakeReason}");
                    break;

                case RelayFrameType.TickComplete:
                    if (Phase != RelayClientPhase.Running
                        || _barrier == null
                        || !RelayProtocol.TryParseTickComplete(
                            payload, out byte completeSlot, out uint completeTick, out int recordCount)
                        || !IsActiveRemoteSlot(completeSlot))
                    {
                        ProtocolViolation("invalid or unexpected TickComplete");
                        return;
                    }
                    DebugLog?.Invoke($"slot {AssignedSlot}: complete(slot {completeSlot}, tick {completeTick}, n={recordCount})");
                    LockstepBarrierVerdict completeVerdict =
                        _barrier.NoteRemoteTickComplete(
                            completeSlot, completeTick, recordCount);
                    if (completeVerdict != LockstepBarrierVerdict.Accepted)
                    {
                        ProtocolViolation(
                            $"TickComplete barrier rejected ({completeVerdict})");
                        return;
                    }
                    break;

                case RelayFrameType.Desync:
                    if (Phase != RelayClientPhase.Running
                        || !RelayProtocol.TryParseSlotTick(
                            payload, out byte desyncSlot, out uint desyncTick)
                        || desyncSlot != byte.MaxValue
                        || desyncTick == 0
                        || desyncTick % StateHashIntervalTicks != 0
                        || !_checkpointEvidence.ContainsKey(desyncTick))
                    {
                        ProtocolViolation("invalid or unexpected Desync");
                        return;
                    }
                    Desynced = true;
                    DesyncTick = desyncTick;
                    WriteDesyncDiagnostic(desyncTick);
                    EndMatch($"desync reported by the relay at tick {desyncTick}");
                    break;

                case RelayFrameType.PeerLost:
                    if (Phase != RelayClientPhase.Running
                        || !RelayProtocol.TryParseSlotTick(
                            payload, out byte lostSlot, out uint lostTick)
                        || !IsActiveRemoteSlot(lostSlot)
                        || lostTick != 0)
                    {
                        ProtocolViolation("invalid or unexpected PeerLost");
                        return;
                    }
                    EndMatch($"peer slot {lostSlot} lost the relay connection");
                    break;

                case RelayFrameType.Pong:
                    if (Phase != RelayClientPhase.Running
                        || !_pingOutstanding
                        || !RelayProtocol.TryParsePing(payload, out uint probe)
                        || probe != _pingSentMs)
                    {
                        ProtocolViolation("invalid or unexpected Pong");
                        return;
                    }
                    RoundTripMilliseconds = ElapsedMilliseconds(
                        _clockMilliseconds(), _pingSentMs);
                    _pingOutstanding = false;
                    break;

                default:
                    ProtocolViolation($"unexpected frame type {type}");
                    break;
            }
        }

        private static bool HasValidOfferRoles(
            byte assignedSlot, byte[] activeSlots, ulong seed)
        {
            if (seed == 0 || activeSlots == null
                || activeSlots.Length != RelayServerCore.MaxPeers)
            {
                return false;
            }
            var seen = new bool[CommandLimits.ReservedPlayerSlots];
            bool assignedIsActive = false;
            for (int i = 0; i < activeSlots.Length; i++)
            {
                byte activeSlot = activeSlots[i];
                if (activeSlot >= seen.Length || seen[activeSlot]) return false;
                seen[activeSlot] = true;
                assignedIsActive |= activeSlot == assignedSlot;
            }
            return assignedIsActive;
        }

        private bool IsActiveRemoteSlot(byte slot)
        {
            return _session != null
                && slot != _session.LocalSlot
                && _session.IsActiveSlot(slot);
        }

        private void ProtocolViolation(string detail)
        {
            EndMatch($"relay protocol violation: {detail}");
        }

        /// <summary>
        /// Pipelined local announcement: Start may safely prefill through
        /// CurrentTick + D - 1. At the beginning of a step attempt, local
        /// input is closed first, which makes CurrentTick + D final too;
        /// that current window is announced before the barrier is queried.
        /// The announced count is the LOCAL slot's records only, tracked at
        /// Send time: the ingress pending pool mixes in the peers' records
        /// and must never leak into a slot's own completeness claim.
        /// </summary>
        private void AnnounceLocalCompleteness(bool includeCurrentInputTick)
        {
            if (_barrier == null || _session == null || Phase != RelayClientPhase.Running) return;
            uint offset = includeCurrentInputTick
                ? _session.InputDelayTicks
                : _session.InputDelayTicks - 1;
            if (_session.CurrentTick > uint.MaxValue - offset)
            {
                EndMatch("local completeness window overflowed");
                return;
            }
            uint through = _session.CurrentTick + offset;
            if (through == uint.MaxValue)
            {
                EndMatch("local completeness window reached the final representable tick");
                return;
            }
            if (through <= _announcedThrough) return;

            uint tick = _announcedThrough + 1;
            while (true)
            {
                _localRecordsByTick.TryGetValue(tick, out int count);
                if (!_connection.SendFrame(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(_session.LocalSlot, tick, count)))
                {
                    EndMatch(_connection.LastError ?? "relay tick-complete send failed");
                    return;
                }
                LockstepBarrierVerdict localVerdict =
                    _barrier.NoteLocalTickComplete(tick, count);
                if (localVerdict != LockstepBarrierVerdict.Accepted)
                {
                    ProtocolViolation(
                        $"local TickComplete barrier rejected ({localVerdict})");
                    return;
                }
                _announcedThrough = tick;
                if (tick == through) break;
                tick++;
            }
        }

        private void EndMatch(string reason)
        {
            ClearPendingHello();
            EndReason = reason;
            Phase = RelayClientPhase.Ended;
            _stallActive = false;
            _connection.Disconnect();
            DisposeDiagnosticRecordSpool();
        }

        private void ClearPendingHello()
        {
            _pendingMatchToken = 0;
            _helloPending = false;
        }

        private void WriteDesyncDiagnostic(uint tick)
        {
            if (_diagnosticWritten) return;
            _diagnosticWritten = true;
            if (!_checkpointEvidence.TryGetValue(tick, out CheckpointEvidence evidence))
            {
                LastDiagnosticError = $"no verified checkpoint evidence was buffered for tick {tick}";
                return;
            }

            if (!string.IsNullOrEmpty(_diagnosticCaptureError))
            {
                LastDiagnosticError = _diagnosticCaptureError;
                return;
            }
            if (_diagnosticRecordSpool == null)
            {
                _diagnosticRecordSpool = new DiagnosticRecordSpool();
            }

            if (DesyncDiagnostic.TryWrite(
                    DiagnosticDirectory, AssignedSlot, tick, evidence.StateHash, evidence.SnapshotBytes,
                    _diagnosticRecordSpool, out string path, out string error))
            {
                LastDiagnosticPath = path;
                LastDiagnosticError = string.Empty;
                DebugLog?.Invoke($"slot {AssignedSlot}: wrote desync diagnosis to {path}");
            }
            else
            {
                LastDiagnosticError = error;
                DebugLog?.Invoke($"slot {AssignedSlot}: desync diagnosis failed: {error}");
            }
            DisposeDiagnosticRecordSpool();
        }

        private void DisposeDiagnosticRecordSpool()
        {
            _diagnosticRecordSpool?.Dispose();
            _diagnosticRecordSpool = null;
        }

        private readonly struct CheckpointEvidence
        {
            public ulong StateHash { get; }
            public byte[] SnapshotBytes { get; }

            public CheckpointEvidence(ulong stateHash, byte[] snapshotBytes)
            {
                StateHash = stateHash;
                SnapshotBytes = snapshotBytes;
            }
        }
    }
}
