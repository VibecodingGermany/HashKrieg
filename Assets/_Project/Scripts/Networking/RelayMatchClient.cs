using System;
using System.Collections.Generic;
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
    public sealed class RelayMatchClient : INetworkTransport
    {
        /// <summary>Interval of the state-hash reports the relay compares (5 s at 10 Hz).</summary>
        public const int StateHashIntervalTicks = 50;

        /// <summary>Wall-clock budget of a stall before the peer counts as lost (sprint A2c: 30 s).</summary>
        public const double StallTimeoutSeconds = 30.0;

        private readonly TcpRelayConnection _connection = new TcpRelayConnection();
        private LockstepBarrier _barrier;
        private CommandIngress _ingress;
        private MatchSession _session;
        private uint _announcedThrough;
        private readonly Dictionary<uint, int> _localRecordsByTick = new Dictionary<uint, int>();
        private uint _stalledSinceMs = uint.MaxValue;
        private bool _stallActive;
        private uint _pingCounter;
        private long _pingSentMs = -1;

        public RelayMatchClient()
        {
            _connection.SetFrameHandler(OnFrame);
        }

        // ------------------------------------------------------------------
        // Handshake surface
        // ------------------------------------------------------------------

        public RelayClientPhase Phase { get; private set; } = RelayClientPhase.Disconnected;

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

        // ------------------------------------------------------------------
        // Stall surface
        // ------------------------------------------------------------------

        /// <summary>True while the next tick waits on another slot's input.</summary>
        public bool IsStalled => _stallActive;

        /// <summary>The slot the next tick waits on, or -1 when not stalled.</summary>
        public int StalledOnSlot { get; private set; } = -1;

        /// <summary>Seconds the current stall has lasted (0 when not stalled).</summary>
        public double StallSeconds =>
            _stallActive ? (Environment.TickCount - _stalledSinceMs) / 1000.0 : 0.0;

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

        public uint? RoundTripMilliseconds { get; private set; }

        public string LastError => _connection.LastError;

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
            if (!_connection.Connect(host, port, timeoutMilliseconds))
            {
                Phase = RelayClientPhase.Ended;
                EndReason = _connection.LastError ?? "connect failed";
                return;
            }
            _connection.SendFrame(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(matchToken));
            Phase = RelayClientPhase.WaitingOffer;
        }

        /// <summary>INetworkTransport.Connect without a token is not meaningful for the match client; kept for interface completeness.</summary>
        void INetworkTransport.Connect(string host, int port)
        {
            throw new NotSupportedException("RelayMatchClient.Connect requires the match token: use Connect(host, port, matchToken).");
        }

        public void Disconnect()
        {
            _connection.Disconnect();
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
            _connection.SendFrame(RelayFrameType.Fingerprint, fingerprintBytes);
            _connection.SendFrame(RelayFrameType.InitialSnapshot, initialSnapshotBytes);
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
            CommandIngressResult result = _ingress.TryAcceptRecordBytes(recordBytes, out _);
            if (result == CommandIngressResult.Rejected)
            {
                return;
            }
            _connection.SendFrame(RelayFrameType.CommandRecord, recordBytes);
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

            if (Phase == RelayClientPhase.Running)
            {
                // RTT cadence: one ping every ~5 s of host frames (callers
                // pump at display rate; the counter keeps it cheap).
                _pingCounter++;
                if (_pingCounter >= 300)
                {
                    _pingCounter = 0;
                    var probe = new byte[4];
                    RelayProtocol.WriteUInt32(probe, 0, unchecked((uint)Environment.TickCount));
                    if (_connection.SendFrame(RelayFrameType.Ping, probe))
                    {
                        _pingSentMs = Environment.TickCount;
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
            if (Phase != RelayClientPhase.Running) return false;

            uint nextTick = kernel.CurrentTick.Value + 1;
            if (!_barrier.IsTickReady(nextTick))
            {
                if (!_stallActive)
                {
                    _stallActive = true;
                    _stalledSinceMs = unchecked((uint)Environment.TickCount);
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
            _barrier.PruneThrough(nextTick);
            _localRecordsByTick.Remove(nextTick);

            AnnounceLocalCompleteness();

            if (nextTick % StateHashIntervalTicks == 0)
            {
                _connection.SendFrame(RelayFrameType.StateHash,
                    RelayProtocol.CreateStateHashPayload(_session.LocalSlot, nextTick, kernel.CalculateStateHash()));
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
                    if (RelayProtocol.TryParseOffer(payload, out byte slot, out byte[] activeSlots,
                            out ulong seed, out uint delay, out ulong serverDefsHash))
                    {
                        AssignedSlot = slot;
                        ActiveSlots = activeSlots;
                        Seed = seed;
                        InputDelayTicks = delay;
                        ServerDefinitionsHash64 = serverDefsHash;
                        HasOffer = true;
                        _barrier = new LockstepBarrier(slot, activeSlots);
                        _announcedThrough = 0;
                    }
                    break;

                case RelayFrameType.Start:
                    if (Phase == RelayClientPhase.WaitingStart)
                    {
                        Phase = RelayClientPhase.Running;
                        AnnounceLocalCompleteness();
                    }
                    break;

                case RelayFrameType.Reject:
                    RejectReason = RelayProtocol.ParseReasonPayload(payload);
                    EndMatch($"rejected by relay: {RejectReason}");
                    break;

                case RelayFrameType.CommandRecord:
                    if (Phase == RelayClientPhase.Running
                        && Nova.Simulation.CommandsV1.CommandRecord.TryDeserialize(payload, out var record, out int consumed)
                        && consumed == payload.Length)
                    {
                        // The intake revalidates structurally; the barrier
                        // counts the arrival regardless of the intake verdict
                        // (a rejected foreign record cannot affect the seal).
                        if (record.PlayerSlot != _session.LocalSlot)
                        {
                            _barrier.NoteRemoteRecord(record.PlayerSlot, record.TargetTick);
                            Nova.Simulation.CommandsV1.CommandIngressResult intake = _ingress.TryAcceptRecordBytes(payload, out CommandRejectReason intakeReason);
                            DebugLog?.Invoke($"slot {AssignedSlot}: record(slot {record.PlayerSlot}, tick {record.TargetTick}, seq {record.Sequence}, {record.Kind}) intake={intake}/{intakeReason}");
                        }
                    }
                    break;

                case RelayFrameType.TickComplete:
                    if (RelayProtocol.TryParseTickComplete(payload, out byte completeSlot, out uint completeTick, out int recordCount)
                        && completeSlot != AssignedSlot)
                    {
                        DebugLog?.Invoke($"slot {AssignedSlot}: complete(slot {completeSlot}, tick {completeTick}, n={recordCount})");
                        _barrier?.NoteTickComplete(completeSlot, completeTick, recordCount);
                    }
                    break;

                case RelayFrameType.Desync:
                    if (RelayProtocol.TryParseSlotTick(payload, out _, out uint desyncTick))
                    {
                        Desynced = true;
                        DesyncTick = desyncTick;
                        EndMatch($"desync reported by the relay at tick {desyncTick}");
                    }
                    break;

                case RelayFrameType.PeerLost:
                    if (RelayProtocol.TryParseSlotTick(payload, out byte lostSlot, out _))
                    {
                        EndMatch($"peer slot {lostSlot} lost the relay connection");
                    }
                    break;

                case RelayFrameType.Pong:
                    if (_pingSentMs >= 0 && RelayProtocol.TryParsePing(payload, out _))
                    {
                        RoundTripMilliseconds = unchecked((uint)(Environment.TickCount - _pingSentMs));
                        _pingSentMs = -1;
                    }
                    break;
            }
        }

        /// <summary>
        /// Pipelined local announcement: with input delay D, no new local
        /// record for tick X can be minted once the session tick passed
        /// X - D — so every tick up to CurrentTick + D - 1 is announced
        /// complete now, one tick of network slack ahead of its execution.
        /// The announced count is the LOCAL slot's records only, tracked at
        /// Send time: the ingress pending pool mixes in the peers' records
        /// and must never leak into a slot's own completeness claim.
        /// </summary>
        private void AnnounceLocalCompleteness()
        {
            if (_barrier == null || _session == null || Phase != RelayClientPhase.Running) return;
            uint through = _session.CurrentTick + _session.InputDelayTicks - 1;
            for (uint tick = _announcedThrough + 1; tick <= through; tick++)
            {
                _localRecordsByTick.TryGetValue(tick, out int count);
                _barrier.NoteLocalTickComplete(tick, count);
                _connection.SendFrame(RelayFrameType.TickComplete,
                    RelayProtocol.CreateTickCompletePayload(_session.LocalSlot, tick, count));
            }
            if (through > _announcedThrough)
            {
                _announcedThrough = through;
            }
        }

        private void EndMatch(string reason)
        {
            EndReason = reason;
            Phase = RelayClientPhase.Ended;
            _stallActive = false;
            _connection.Disconnect();
        }
    }
}
