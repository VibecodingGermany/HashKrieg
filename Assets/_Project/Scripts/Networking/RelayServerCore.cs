using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Snapshots;

namespace Nova.Networking
{
    /// <summary>
    /// The authoritative input relay (docs/research/Multiplayer_Simulation.md
    /// §5/§6; strand A3–A5 of the sprint doc): accepts exactly two clients,
    /// assigns their slots, runs the fingerprint/snapshot handshake, validates
    /// and forwards every record, compares the periodic state hashes and
    /// writes the canonical command stream of the match for desync
    /// reproduction.
    /// <para>
    /// The relay revalidates record structure and sender slot, enforces
    /// monotone sequence/enqueue/target order with the offered delay, bounds
    /// global and per-tick pending input, requires gapless exact completion
    /// counts and accepts state hashes only at the canonical cadence. It does
    /// not execute the simulation and therefore still cannot judge
    /// state-dependent legality; both clients do that identically.
    /// </para>
    /// <para>
    /// Poll-driven and single-threaded like the client connection: the
    /// service host (<c>tools/Nova.RelayServer</c>) and the headless test
    /// lane pump the identical code.
    /// </para>
    /// <para>
    /// Replay format honesty: the relay records the command STREAM
    /// (<c>*.novarec</c>: fingerprint + initial snapshot + per-tick
    /// canonical record bytes), not a ReplayFile. ReplayFile binds
    /// per-record result codes of an EXECUTING host — the relay deliberately
    /// does not execute, so any code it wrote would be fabricated. The dump
    /// carries everything a desync reproduction needs; the deviation from
    /// the sprint letter ("the format exists") is logged as a D-089
    /// decision note in the sprint result block.
    /// </para>
    /// </summary>
    public sealed class RelayServerCore
    {
        /// <summary>MS-1: exactly two players per match.</summary>
        public const int MaxPeers = 2;

        public const uint HelloTimeoutMilliseconds = 5_000;
        public const uint ProofTimeoutMilliseconds = 10_000;
        public const uint OpponentWaitTimeoutMilliseconds = 120_000;

        private sealed class Peer
        {
            public TcpClient Client;
            public NetworkStream Stream;
            public RelayProtocol.FrameCutter Cutter = new RelayProtocol.FrameCutter();
            public byte[] ReadBuffer = new byte[64 * 1024];
            public byte Slot;
            public bool HelloOk;
            public uint ConnectedAtMilliseconds;
            public uint HelloAtMilliseconds;
            public bool ProofComplete;
            public uint ProofCompletedAtMilliseconds;
            public byte[] FingerprintBytes;
            public Nova.Simulation.Replays.MatchFingerprint Fingerprint;
            public byte[] InitialSnapshot;
            public readonly Dictionary<uint, ulong> StateHashes = new Dictionary<uint, ulong>();
        }

        private sealed class TickState
        {
            public readonly List<CommandRecord> Records = new List<CommandRecord>();
            public readonly int[] AcceptedBySlot = new int[MaxPeers];
            public readonly int[] AnnouncedBySlot = { -1, -1 };
            public readonly bool[] CompletedBySlot = new bool[MaxPeers];
        }

        private enum ServerPhase { Listening, Running, Ended }

        private enum PeerPumpOutcome
        {
            Active,
            Disconnected,
            ProtocolRejected,
        }

        private readonly ulong _matchToken;
        private readonly ulong _configuredSeed;
        private readonly uint _inputDelayTicks;
        private readonly string _recordDirectory;
        private readonly Action<string> _log;
        private readonly List<Peer> _peers = new List<Peer>();
        private readonly byte[] _activeSlots = { 0, 1 };
        private readonly Func<uint> _clockMilliseconds;
        private readonly Dictionary<uint, TickState> _pendingTicks = new Dictionary<uint, TickState>();
        private readonly Dictionary<uint, CommandRecord[]> _confirmedFrames =
            new Dictionary<uint, CommandRecord[]>();
        private readonly uint[] _lastSequenceBySlot = new uint[MaxPeers];
        private readonly uint[] _lastEnqueueTickBySlot = new uint[MaxPeers];
        private readonly uint[] _lastTargetTickBySlot = new uint[MaxPeers];
        private readonly uint[] _lastCompletedTickBySlot = new uint[MaxPeers];
        private readonly int[] _lastCompletedCountBySlot = new int[MaxPeers];
        private readonly uint[] _lastStateHashTickBySlot = new uint[MaxPeers];
        private int _pendingRecordCount;

        private TcpListener _listener;
        private ServerPhase _phase = ServerPhase.Listening;
        private ulong _seed;
        private Stream _recordStream;
        private bool _recordFooterWritten;
        private bool _recordingRequested;
        private bool _recordingFailed;
        private string _recordPartialPath;
        private string _recordFinalPath;
        private uint _initialRecordTick;
        private uint _lastConfirmedTick;
        private uint _lastHashDecisionTick;
        private bool _resetRequested;
        private string _resetReason;

        /// <summary>Seed of the current match (generated at listener start when 0 was configured).</summary>
        public ulong Seed => _seed;

        public int PeerCount => _peers.Count;
        public uint LastRecordedTick { get; private set; }
        public uint LastCheckpointTick { get; private set; }
        public uint LastTerminalTick { get; private set; }
        public string LastRecordPath { get; private set; }
        public string LastPartialRecordPath { get; private set; }
        public string LastRecordingError { get; private set; } = string.Empty;
        public uint LastDesyncTick { get; private set; }
        public ulong LastDesyncSlot0Hash { get; private set; }
        public ulong LastDesyncSlot1Hash { get; private set; }
        internal int PendingRecordCount => _pendingRecordCount;
        internal int PendingTickCount => _pendingTicks.Count;
#if NOVA_RELAY_TESTS
        internal int CompleteProofPeerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _peers.Count; i++)
                {
                    if (_peers[i].ProofComplete) count++;
                }
                return count;
            }
        }
#endif

        public RelayServerCore(
            ulong matchToken, ulong seed, uint inputDelayTicks,
            string recordDirectory, Action<string> log,
            Func<uint> clockMilliseconds = null)
        {
            if (matchToken == 0) throw new ArgumentOutOfRangeException(nameof(matchToken));
            if (!RelayProtocol.IsSupportedInputDelay(inputDelayTicks))
            {
                throw new ArgumentOutOfRangeException(nameof(inputDelayTicks));
            }
            _matchToken = matchToken;
            _configuredSeed = seed;
            _seed = seed;
            _inputDelayTicks = inputDelayTicks;
            _recordDirectory = recordDirectory;
            _log = log ?? (_ => { });
            _clockMilliseconds = clockMilliseconds ?? (() => unchecked((uint)Environment.TickCount));
        }

        /// <summary>Starts the listener on 0.0.0.0:<paramref name="port"/>; port 0 picks a free one (test lane).</summary>
        public void Start(int port)
        {
            Start(port, IPAddress.Any);
        }

        /// <summary>Starts the listener on <paramref name="bindAddress"/>:<paramref name="port"/>; port 0 picks a free one (test lane).</summary>
        public void Start(int port, IPAddress bindAddress)
        {
            _listener = new TcpListener(bindAddress, port);
            _listener.Start();
            if (_seed == 0)
            {
                _seed = GenerateRandomSeed();
            }
            _log($"relay listening on {bindAddress}:{Port}, seed 0x{_seed:X16}, input delay {_inputDelayTicks}");
        }

        /// <summary>The effective bound port.</summary>
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public void Stop()
        {
            FinalizeRecordStream(
                RelayRecordTerminalReason.ServerStopped,
                GetOrderedTerminalTick(), drainConfirmedTail: true);
            ResetMatch("server stopping");
            _listener?.Stop();
            _listener = null;
        }

        /// <summary>Pumps accepts and every peer socket once. Call in a loop (service) or per test step.</summary>
        public void Poll()
        {
            if (_listener == null) return;

            while (_listener.Pending())
            {
                TcpClient accepted = _listener.AcceptTcpClient();
                accepted.NoDelay = true;
                if (_peers.Count >= MaxPeers || _phase != ServerPhase.Listening
                    || !TryFindFreeSlot(out byte freeSlot))
                {
                    _log("connection refused: match full or ended");
                    try { accepted.Dispose(); } catch { /* ignore */ }
                    continue;
                }
                var peer = new Peer
                {
                    Client = accepted,
                    Stream = accepted.GetStream(),
                    Slot = freeSlot,
                    ConnectedAtMilliseconds = _clockMilliseconds(),
                };
                _peers.Add(peer);
                _log($"peer connected as slot {peer.Slot} ({_peers.Count}/{MaxPeers})");
            }

            for (int i = _peers.Count - 1; i >= 0; i--)
            {
                if (_resetRequested) break;
                PeerPumpOutcome outcome = PumpPeer(_peers[i]);
                if (outcome != PeerPumpOutcome.Active)
                {
                    Peer lost = _peers[i];
                    RemovePeerAt(i);
                    if (_phase == ServerPhase.Running)
                    {
                        Broadcast(RelayFrameType.PeerLost, RelayProtocol.CreateSlotTickPayload(RelayFrameType.PeerLost, lost.Slot, 0), exceptSlot: -1);
                        bool protocolRejected = outcome == PeerPumpOutcome.ProtocolRejected;
                        EndMatch(
                            protocolRejected
                                ? $"peer slot {lost.Slot} violated the relay protocol"
                                : $"peer slot {lost.Slot} disconnected",
                            protocolRejected
                                ? RelayRecordTerminalReason.ProtocolViolation
                                : RelayRecordTerminalReason.PeerLost);
                    }
                    else
                    {
                        _log(outcome == PeerPumpOutcome.ProtocolRejected
                            ? $"peer slot {lost.Slot} rejected during handshake"
                            : $"peer slot {lost.Slot} disconnected during handshake");
                    }
                }
            }

            if (_resetRequested)
            {
                ApplyRequestedReset();
                return;
            }

            ExpireHandshakePeers();
            TryStartMatch();
            if (_resetRequested) ApplyRequestedReset();
        }

        // ------------------------------------------------------------------
        // Peer pump + frame handling
        // ------------------------------------------------------------------

        private PeerPumpOutcome PumpPeer(Peer peer)
        {
            try
            {
                if (peer.Client.Client.Poll(0, SelectMode.SelectRead) && peer.Client.Available == 0)
                {
                    return PeerPumpOutcome.Disconnected;
                }
                while (peer.Client.Available > 0)
                {
                    int readCapacity = Math.Min(
                        peer.ReadBuffer.Length, peer.Cutter.RemainingCapacity);
                    if (readCapacity <= 0)
                    {
                        throw new RelayFrameFormatException(
                            "Relay frame carry could not be drained.");
                    }
                    int read = peer.Stream.Read(
                        peer.ReadBuffer, 0, readCapacity);
                    if (read <= 0) return PeerPumpOutcome.Disconnected;
                    peer.Cutter.Feed(peer.ReadBuffer.AsSpan(0, read));
                    while (peer.Cutter.TryTakeFrame(
                        out RelayFrameType type, out byte[] payload))
                    {
                        if (!HandleFrame(peer, type, payload))
                        {
                            return PeerPumpOutcome.ProtocolRejected;
                        }
                        if (_resetRequested) return PeerPumpOutcome.Active;
                    }
                }
                return PeerPumpOutcome.Active;
            }
            catch (RelayFrameFormatException exception)
            {
                _log($"peer slot {peer.Slot} protocol framing error: {exception.Message}");
                return PeerPumpOutcome.ProtocolRejected;
            }
            catch (SocketException exception)
            {
                _log($"peer slot {peer.Slot} socket error: {exception.Message}");
                return PeerPumpOutcome.Disconnected;
            }
            catch (IOException exception)
            {
                _log($"peer slot {peer.Slot} socket I/O error: {exception.Message}");
                return PeerPumpOutcome.Disconnected;
            }
            catch (ObjectDisposedException exception)
            {
                _log($"peer slot {peer.Slot} socket error: {exception.Message}");
                return PeerPumpOutcome.Disconnected;
            }
        }

        /// <summary>False return: the peer is broken and must be dropped.</summary>
        private bool HandleFrame(Peer peer, RelayFrameType type, byte[] payload)
        {
            switch (type)
            {
                case RelayFrameType.Hello:
                {
                    bool helloParsed = RelayProtocol.TryParseHello(
                        payload, out byte version, out ulong token);
                    if (peer.HelloOk || !helloParsed
                        || version != RelayProtocol.ProtocolVersion || token != _matchToken)
                    {
                        Send(peer, RelayFrameType.Reject,
                            RelayProtocol.CreateReasonPayload(RelayFrameType.Reject,
                                helloParsed && version != RelayProtocol.ProtocolVersion
                                    ? $"protocol version mismatch (server v{RelayProtocol.ProtocolVersion})"
                                    : "wrong match code"));
                        return false;
                    }
                    peer.HelloOk = true;
                    peer.HelloAtMilliseconds = _clockMilliseconds();
                    Send(peer, RelayFrameType.Offer, RelayProtocol.CreateOfferPayload(
                        peer.Slot, _activeSlots, _seed, _inputDelayTicks, SimDefinitions.ComputeDefinitionsHash64()));
                    return true;
                }

                case RelayFrameType.Fingerprint:
                {
                    if (!peer.HelloOk || peer.Fingerprint != null) return false;
                    if (!Nova.Simulation.Replays.MatchFingerprint.TryParse(payload, out Nova.Simulation.Replays.MatchFingerprint fingerprint))
                    {
                        Send(peer, RelayFrameType.Reject,
                            RelayProtocol.CreateReasonPayload(RelayFrameType.Reject, "malformed match fingerprint"));
                        return false;
                    }
                    peer.FingerprintBytes = payload;
                    peer.Fingerprint = fingerprint;
                    MarkProofComplete(peer);
                    return true;
                }

                case RelayFrameType.InitialSnapshot:
                {
                    if (!peer.HelloOk || peer.InitialSnapshot != null) return false;
                    peer.InitialSnapshot = payload;
                    MarkProofComplete(peer);
                    return true;
                }

                case RelayFrameType.CommandRecord:
                {
                    if (_phase != ServerPhase.Running) return false;
                    if (!CommandRecord.TryDeserialize(payload, out CommandRecord record, out int consumed)
                        || consumed != payload.Length)
                    {
                        _log($"slot {peer.Slot}: malformed record frame");
                        return false;
                    }
                    // THE authority check of this stage: the slot on the wire
                    // must be the slot of the connection it arrived on.
                    if (record.PlayerSlot != peer.Slot)
                    {
                        _log($"slot {peer.Slot}: record claimed slot {record.PlayerSlot}");
                        return false;
                    }
                    if (!CommandPayloadValidation.TryValidateStreamPayload(
                            record.Kind, record.PayloadVersion, record.Payload.Span, out CommandRejectReason reason))
                    {
                        _log($"slot {peer.Slot}: structurally invalid record ({reason})");
                        return false;
                    }
                    int slotIndex = peer.Slot;
                    uint expectedTarget = record.EnqueueTick + _inputDelayTicks;
                    if (record.Sequence == 0
                        || record.Sequence <= _lastSequenceBySlot[slotIndex]
                        || record.EnqueueTick < _lastEnqueueTickBySlot[slotIndex]
                        || record.TargetTick < _lastTargetTickBySlot[slotIndex]
                        || expectedTarget < record.EnqueueTick
                        || expectedTarget != record.TargetTick
                        || record.TargetTick <= _lastConfirmedTick)
                    {
                        _log($"slot {peer.Slot}: non-monotone sequence/enqueue/target tick");
                        return false;
                    }
                    if (_pendingRecordCount >= CommandLimits.MaxPendingRecords)
                    {
                        _log($"slot {peer.Slot}: exceeded the global pending record capacity");
                        return false;
                    }
                    TickState tickState = GetOrCreateTickState(record.TargetTick);
                    if (tickState == null || tickState.CompletedBySlot[slotIndex]
                        || tickState.Records.Count >= CommandLimits.MaxBatchRecordsPerTick)
                    {
                        _log($"slot {peer.Slot}: record arrived after completion or exceeded the tick capacity");
                        return false;
                    }

                    _lastSequenceBySlot[slotIndex] = record.Sequence;
                    _lastEnqueueTickBySlot[slotIndex] = record.EnqueueTick;
                    _lastTargetTickBySlot[slotIndex] = record.TargetTick;
                    tickState.Records.Add(record);
                    tickState.AcceptedBySlot[slotIndex]++;
                    _pendingRecordCount++;
                    Forward(RelayFrameType.CommandRecord, payload, exceptSlot: peer.Slot);
                    return true;
                }

                case RelayFrameType.TickComplete:
                {
                    if (_phase != ServerPhase.Running) return false;
                    if (!RelayProtocol.TryParseTickComplete(
                            payload, out byte slot, out uint tick, out int recordCount)
                        || slot != peer.Slot)
                    {
                        _log($"slot {peer.Slot}: invalid tick-complete claiming slot {slot}");
                        return false;
                    }
                    int slotIndex = peer.Slot;
                    uint lastComplete = _lastCompletedTickBySlot[slotIndex];
                    if (tick == lastComplete)
                    {
                        if (recordCount == _lastCompletedCountBySlot[slotIndex]) return true;
                        _log($"slot {peer.Slot}: conflicting duplicate tick-complete for tick {tick}");
                        return false;
                    }
                    if (lastComplete == uint.MaxValue || tick != lastComplete + 1)
                    {
                        _log($"slot {peer.Slot}: non-gapless tick-complete {tick} after {lastComplete}");
                        return false;
                    }
                    TickState tickState = GetOrCreateTickState(tick);
                    if (tickState == null || tickState.CompletedBySlot[slotIndex]
                        || tickState.AcceptedBySlot[slotIndex] != recordCount)
                    {
                        _log($"slot {peer.Slot}: announced {recordCount} records for tick {tick}, accepted {tickState?.AcceptedBySlot[slotIndex] ?? -1}");
                        return false;
                    }
                    tickState.CompletedBySlot[slotIndex] = true;
                    tickState.AnnouncedBySlot[slotIndex] = recordCount;
                    _lastCompletedTickBySlot[slotIndex] = tick;
                    _lastCompletedCountBySlot[slotIndex] = recordCount;
                    Forward(RelayFrameType.TickComplete, payload, exceptSlot: peer.Slot);
                    if (!FlushConfirmedTicks()) return false;
                    return true;
                }

                case RelayFrameType.StateHash:
                {
                    if (_phase != ServerPhase.Running) return false;
                    if (!RelayProtocol.TryParseStateHash(payload, out byte slot, out uint tick, out ulong hash)
                        || slot != peer.Slot
                        || tick == 0
                        || tick % RelayMatchClient.StateHashIntervalTicks != 0
                        || tick > _lastConfirmedTick
                        || tick != _lastStateHashTickBySlot[peer.Slot] + RelayMatchClient.StateHashIntervalTicks
                        || peer.StateHashes.ContainsKey(tick))
                    {
                        _log($"slot {peer.Slot}: invalid, duplicate or out-of-cadence state hash at tick {tick}");
                        return false;
                    }
                    _lastStateHashTickBySlot[peer.Slot] = tick;
                    peer.StateHashes.Add(tick, hash);
                    CommitAvailableStateHashes();
                    return true;
                }

                case RelayFrameType.Ping:
                    if (_phase != ServerPhase.Running
                        || !peer.HelloOk
                        || !RelayProtocol.TryParsePing(payload, out _))
                    {
                        _log($"slot {peer.Slot}: invalid ping");
                        return false;
                    }
                    Send(peer, RelayFrameType.Pong, payload);
                    return true;

                default:
                    _log($"slot {peer.Slot}: unexpected client frame type {type}");
                    return false;
            }
        }

        // ------------------------------------------------------------------
        // Handshake completion (A4: the fingerprint lock)
        // ------------------------------------------------------------------

        private void TryStartMatch()
        {
            if (_phase != ServerPhase.Listening || _peers.Count != MaxPeers) return;
            for (int i = 0; i < _peers.Count; i++)
            {
                if (_peers[i].Fingerprint == null || _peers[i].InitialSnapshot == null) return;
            }

            Peer a = FindPeer(0);
            Peer b = FindPeer(1);
            if (a == null || b == null) return;

            // First bind EACH proof to the offer. Comparing only peer A to
            // peer B is insufficient: two clients can agree on the same old
            // seed, delay or definitions and would otherwise start a match
            // that is different from the one the relay offered.
            string mismatch = ValidateProofAgainstOffer(a);
            if (mismatch == null) mismatch = ValidateProofAgainstOffer(b);

            // Byte equality: identical builds + same offered parameters +
            // identical bootstrap produce byte-identical fingerprints;
            // anything less is a mismatch worth a readable message.
            if (mismatch == null) mismatch = a.Fingerprint.FindFirstDifference(b.Fingerprint);
            if (mismatch == null && !a.InitialSnapshot.AsSpan().SequenceEqual(b.InitialSnapshot))
            {
                mismatch = "InitialSnapshot (byte-different tick-0 state)";
            }

            if (mismatch != null)
            {
                string reason = $"match start refused — fingerprint mismatch: {mismatch}";
                _log(reason);
                for (int i = 0; i < _peers.Count; i++)
                {
                    Send(_peers[i], RelayFrameType.Reject, RelayProtocol.CreateReasonPayload(RelayFrameType.Reject, reason));
                }
                EndMatch(reason, RelayRecordTerminalReason.ProtocolViolation);
                return;
            }

            if (!OpenRecordStream(a.FingerprintBytes, a.InitialSnapshot))
            {
                string reason = string.IsNullOrEmpty(LastRecordingError)
                    ? "match start refused — relay recording could not be initialized"
                    : $"match start refused — {LastRecordingError}";
                for (int i = 0; i < _peers.Count; i++)
                {
                    Send(_peers[i], RelayFrameType.Reject,
                        RelayProtocol.CreateReasonPayload(RelayFrameType.Reject, reason));
                }
                EndMatch(reason, RelayRecordTerminalReason.ProtocolViolation);
                return;
            }
            _phase = ServerPhase.Running;
            for (int i = 0; i < _peers.Count; i++)
            {
                Send(_peers[i], RelayFrameType.Start, Array.Empty<byte>());
            }
            _log($"match started: seed 0x{_seed:X16}, delay {_inputDelayTicks}, fingerprint hash 0x{a.Fingerprint.ComputeHash():X16}");
        }

        /// <summary>
        /// Validates one client's proof against the match parameters sent in
        /// its Offer and binds the canonical snapshot back to the
        /// fingerprint's initial-state hash.
        /// </summary>
        private string ValidateProofAgainstOffer(Peer peer)
        {
            Nova.Simulation.Replays.MatchFingerprint fingerprint = peer.Fingerprint;
            if (fingerprint.StartSeed != _seed)
            {
                return $"StartSeed (slot {peer.Slot} 0x{fingerprint.StartSeed:X16} != offered 0x{_seed:X16})";
            }
            if (fingerprint.InputDelayTicks != _inputDelayTicks)
            {
                return $"InputDelayTicks (slot {peer.Slot} {fingerprint.InputDelayTicks} != offered {_inputDelayTicks})";
            }

            ulong relayDefinitionsHash = SimDefinitions.ComputeDefinitionsHash64();
            if (fingerprint.DefinitionsHash64 != relayDefinitionsHash)
            {
                return $"DefinitionsHash64 (slot {peer.Slot} 0x{fingerprint.DefinitionsHash64:X16} != offered 0x{relayDefinitionsHash:X16})";
            }

            if (!DesyncDiagnostic.TryReadSnapshotIdentity(
                    peer.InitialSnapshot, out uint snapshotTick, out ulong snapshotHash,
                    out string snapshotError))
            {
                return $"InitialSnapshot (slot {peer.Slot} has no canonical kernel identity: {snapshotError})";
            }
            if (snapshotTick != 0)
            {
                return $"InitialSnapshot (slot {peer.Slot} starts at tick {snapshotTick}, expected tick 0)";
            }
            if (snapshotHash != fingerprint.InitialStateHash)
            {
                return $"InitialSnapshot (slot {peer.Slot} state hash 0x{snapshotHash:X16} != fingerprint InitialStateHash 0x{fingerprint.InitialStateHash:X16})";
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Confirmed tick stream + state-hash evidence (NOVAREC2)
        // ------------------------------------------------------------------

        private TickState GetOrCreateTickState(uint tick)
        {
            if (_pendingTicks.TryGetValue(tick, out TickState existing)) return existing;
            if (_pendingTicks.Count >= CommandLimits.MaxPendingRecords) return null;
            var created = new TickState();
            _pendingTicks.Add(tick, created);
            return created;
        }

        private bool FlushConfirmedTicks()
        {
            while (_lastConfirmedTick != uint.MaxValue
                && _pendingTicks.TryGetValue(_lastConfirmedTick + 1, out TickState state)
                && state.CompletedBySlot[0] && state.CompletedBySlot[1])
            {
                int announced = state.AnnouncedBySlot[0] + state.AnnouncedBySlot[1];
                if (announced != state.Records.Count
                    || announced < 0
                    || announced > CommandLimits.MaxBatchRecordsPerTick)
                {
                    _log($"tick {_lastConfirmedTick + 1}: announced/accepted record totals disagree");
                    return false;
                }

                state.Records.Sort(CommandBatch.CompareRecords);
                for (int i = 1; i < state.Records.Count; i++)
                {
                    if (CommandBatch.CompareRecords(state.Records[i - 1], state.Records[i]) >= 0)
                    {
                        _log($"tick {_lastConfirmedTick + 1}: records are not strict canonical identities");
                        return false;
                    }
                }

                if (state.Records.Count > _pendingRecordCount)
                {
                    _log("pending record accounting underflow");
                    return false;
                }

                uint tick = _lastConfirmedTick + 1;
                _confirmedFrames.Add(tick, state.Records.ToArray());
                _lastConfirmedTick = tick;
                _pendingTicks.Remove(tick);
                _pendingRecordCount -= state.Records.Count;
                if (_confirmedFrames.Count
                    > RelayMatchClient.StateHashIntervalTicks + RelayProtocol.MaxInputDelayTicks)
                {
                    _log("confirmed ticks outran the state-hash checkpoint window");
                    return false;
                }
                CommitAvailableStateHashes();
                if (_resetRequested) return true;
            }
            return true;
        }

        private void CommitAvailableStateHashes()
        {
            if (_lastHashDecisionTick > uint.MaxValue - RelayMatchClient.StateHashIntervalTicks) return;
            uint tick = _lastHashDecisionTick + RelayMatchClient.StateHashIntervalTicks;
            if (tick > _lastConfirmedTick) return;

            Peer slot0 = FindPeer(0);
            Peer slot1 = FindPeer(1);
            if (slot0 == null || slot1 == null
                || !slot0.StateHashes.TryGetValue(tick, out ulong hash0)
                || !slot1.StateHashes.TryGetValue(tick, out ulong hash1))
            {
                return;
            }

            slot0.StateHashes.Remove(tick);
            slot1.StateHashes.Remove(tick);
            _lastHashDecisionTick = tick;
            if (hash0 == hash1)
            {
                if (!PersistConfirmedFramesThrough(tick))
                {
                    EndMatch(
                        $"recording failed before checkpoint {tick}: {LastRecordingError}",
                        RelayRecordTerminalReason.ProtocolViolation);
                    return;
                }
                if (!WriteCheckpoint(tick, hash0))
                {
                    EndMatch(
                        $"recording failed at checkpoint {tick}: {LastRecordingError}",
                        RelayRecordTerminalReason.ProtocolViolation);
                    return;
                }
                LastCheckpointTick = tick;
                return;
            }

            if (!PersistConfirmedFramesThrough(tick))
            {
                EndMatch(
                    $"recording failed before desync checkpoint {tick}: {LastRecordingError}",
                    RelayRecordTerminalReason.ProtocolViolation);
                return;
            }
            LastDesyncTick = tick;
            LastDesyncSlot0Hash = hash0;
            LastDesyncSlot1Hash = hash1;
            bool desyncEvidenceWritten = WriteDesyncEvidence(tick, hash0, hash1);
            string reason = $"DESYNC at tick {tick}: slot hashes 0x{hash0:X16} / 0x{hash1:X16}";
            if (!desyncEvidenceWritten)
            {
                reason += $"; recording failed: {LastRecordingError}";
            }
            _log(reason);
            Broadcast(RelayFrameType.Desync,
                RelayProtocol.CreateSlotTickPayload(RelayFrameType.Desync, 255, tick), exceptSlot: -1);
            EndMatch(reason, RelayRecordTerminalReason.Desync, tick);
        }

        private bool OpenRecordStream(byte[] fingerprintBytes, byte[] initialSnapshot)
        {
            if (!DesyncDiagnostic.TryReadSnapshotIdentity(
                    initialSnapshot, out uint initialTick, out _, out string identityError))
            {
                LastRecordingError =
                    $"validated initial snapshot lost its identity: {identityError}";
                _log(LastRecordingError);
                return false;
            }

            _pendingTicks.Clear();
            _confirmedFrames.Clear();
            _pendingRecordCount = 0;
            Array.Clear(_lastSequenceBySlot, 0, _lastSequenceBySlot.Length);
            Array.Clear(_lastEnqueueTickBySlot, 0, _lastEnqueueTickBySlot.Length);
            Array.Clear(_lastTargetTickBySlot, 0, _lastTargetTickBySlot.Length);
            Array.Clear(_lastStateHashTickBySlot, 0, _lastStateHashTickBySlot.Length);
            for (int i = 0; i < MaxPeers; i++)
            {
                _lastCompletedTickBySlot[i] = initialTick;
                _lastCompletedCountBySlot[i] = 0;
            }
            _initialRecordTick = initialTick;
            _lastConfirmedTick = initialTick;
            _lastHashDecisionTick = initialTick - initialTick % RelayMatchClient.StateHashIntervalTicks;
            LastRecordedTick = 0;
            LastCheckpointTick = 0;
            LastTerminalTick = initialTick;
            LastRecordPath = null;
            LastPartialRecordPath = null;
            LastRecordingError = string.Empty;
            LastDesyncTick = 0;
            LastDesyncSlot0Hash = 0;
            LastDesyncSlot1Hash = 0;
            _recordFooterWritten = false;
            _recordingFailed = false;
            _recordingRequested = !string.IsNullOrEmpty(_recordDirectory);
            _recordPartialPath = null;
            _recordFinalPath = null;

            if (!_recordingRequested)
            {
                LastRecordedTick = initialTick;
                return true;
            }
            try
            {
                Directory.CreateDirectory(_recordDirectory);
                _recordFinalPath = Path.Combine(_recordDirectory,
                    $"match-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{_seed:X16}-{Guid.NewGuid():N}.novarec");
                _recordPartialPath = _recordFinalPath + ".partial";
                _recordStream = new FileStream(
                    _recordPartialPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                RelayRecordStream.WriteHeader(_recordStream, fingerprintBytes, initialSnapshot);
                _recordStream.Flush();
                LastRecordedTick = initialTick;
                _log($"recording confirmed tick stream to {_recordPartialPath}");
                return true;
            }
            catch (Exception exception)
            {
                AbortRecordStream($"recording initialization failed: {exception.Message}");
                return false;
            }
        }

        private bool WriteConfirmedTick(
            uint tick, IReadOnlyList<CommandRecord> records, uint terminalTick)
        {
            if (!_recordingRequested) return true;
            if (_recordStream == null || _recordingFailed) return false;
            try
            {
                RelayRecordStream.WriteTickFrame(_recordStream, tick, records);
                _recordStream.Flush();
                return true;
            }
            catch (Exception exception)
            {
                return HandleRecordWriteFailure(
                    $"record tick {tick} write failed", exception, terminalTick);
            }
        }

        private bool WriteCheckpoint(uint tick, ulong stateHash)
        {
            if (!_recordingRequested) return true;
            if (_recordStream == null || _recordingFailed) return false;
            try
            {
                RelayRecordStream.WriteCheckpoint(_recordStream, tick, stateHash);
                _recordStream.Flush();
                return true;
            }
            catch (Exception exception)
            {
                return HandleRecordWriteFailure(
                    $"record checkpoint {tick} write failed", exception, tick);
            }
        }

        private bool WriteDesyncEvidence(uint tick, ulong slot0Hash, ulong slot1Hash)
        {
            if (!_recordingRequested) return true;
            if (_recordStream == null || _recordingFailed) return false;
            try
            {
                RelayRecordStream.WriteDesync(_recordStream, tick, slot0Hash, slot1Hash);
                _recordStream.Flush();
                return true;
            }
            catch (Exception exception)
            {
                return HandleRecordWriteFailure(
                    $"record desync {tick} write failed", exception, tick);
            }
        }

        private bool FinalizeRecordStream(
            RelayRecordTerminalReason terminalReason,
            uint terminalTick, bool drainConfirmedTail)
        {
            LastTerminalTick = terminalTick;
            if (_recordFooterWritten) return !_recordingFailed;
            if (_recordingFailed) return false;

            if (drainConfirmedTail && !PersistConfirmedFramesThrough(terminalTick))
            {
                return false;
            }
            if (LastRecordedTick != terminalTick)
            {
                LastRecordingError =
                    $"recorded tick {LastRecordedTick} does not reach terminal tick {terminalTick}";
                _log(LastRecordingError);
                return false;
            }
            if (!_recordingRequested)
            {
                _recordFooterWritten = true;
                return true;
            }
            if (_recordStream == null) return false;

            try
            {
                RelayRecordStream.WriteEnd(
                    _recordStream, terminalReason,
                    terminalTick, LastRecordedTick, LastCheckpointTick);
                _recordStream.Flush();
                _recordStream.Dispose();
                _recordStream = null;
                File.Move(_recordPartialPath, _recordFinalPath);
                _recordFooterWritten = true;
                LastRecordPath = _recordFinalPath;
                _log($"recording sealed to {_recordFinalPath}");
                return true;
            }
            catch (Exception exception)
            {
                AbortRecordStream($"record footer/publish failed: {exception.Message}");
                return false;
            }
        }

        private bool PersistConfirmedFramesThrough(uint terminalTick)
        {
            if (terminalTick > _lastConfirmedTick || terminalTick < LastRecordedTick) return false;
            while (LastRecordedTick < terminalTick)
            {
                if (LastRecordedTick == uint.MaxValue
                    || !_confirmedFrames.TryGetValue(LastRecordedTick + 1, out CommandRecord[] records))
                {
                    return false;
                }
                uint tick = LastRecordedTick + 1;
                if (!WriteConfirmedTick(tick, records, terminalTick)) return false;
                LastRecordedTick = tick;
                _confirmedFrames.Remove(tick);
            }
            return true;
        }

        private bool HandleRecordWriteFailure(
            string context, Exception exception, uint terminalTick)
        {
            string message = $"{context}: {exception.Message}";
            if (exception is RelayRecordBudgetExceededException)
            {
                LastRecordingError = message;
                _recordingFailed = true;
                _log(message);
                TrySealBudgetPartial(terminalTick);
                return false;
            }
            AbortRecordStream(message);
            return false;
        }

        private void TrySealBudgetPartial(uint terminalTick)
        {
            LastTerminalTick = terminalTick;
            if (_recordStream == null) return;
            try
            {
                RelayRecordStream.WriteEnd(
                    _recordStream, RelayRecordTerminalReason.RecordingLimitExceeded,
                    terminalTick, LastRecordedTick, LastCheckpointTick);
                _recordStream.Flush();
                _recordStream.Dispose();
                _recordStream = null;
                _recordFooterWritten = true;
                LastPartialRecordPath = _recordPartialPath;
                _log($"recording sealed as partial evidence at {_recordPartialPath}");
            }
            catch (Exception footerException)
            {
                LastRecordingError += $"; partial footer failed: {footerException.Message}";
                try { _recordStream?.Dispose(); } catch { /* ignore */ }
                _recordStream = null;
                LastPartialRecordPath = _recordPartialPath;
            }
        }

        private void AbortRecordStream(string message)
        {
            LastRecordingError = message;
            _recordingFailed = true;
            _log(message);
            try { _recordStream?.Dispose(); } catch { /* ignore */ }
            _recordStream = null;
            if (!string.IsNullOrEmpty(_recordPartialPath))
            {
                LastPartialRecordPath = _recordPartialPath;
            }
        }

        private uint GetOrderedTerminalTick()
        {
            uint completed = Math.Min(_lastCompletedTickBySlot[0], _lastCompletedTickBySlot[1]);
            uint lookAhead = _inputDelayTicks - 1;
            uint completedSinceInitial = completed >= _initialRecordTick
                ? completed - _initialRecordTick
                : 0;
            uint inferred = completedSinceInitial >= lookAhead
                ? completed - lookAhead
                : _initialRecordTick;
            if (inferred > _lastConfirmedTick) inferred = _lastConfirmedTick;
            if (inferred < LastRecordedTick) inferred = LastRecordedTick;
            return inferred;
        }

        // ------------------------------------------------------------------
        // Send helpers + lifecycle
        // ------------------------------------------------------------------

        private void Send(Peer peer, RelayFrameType type, byte[] payload)
        {
            try
            {
                byte[] frame = RelayProtocol.CreateFrame(type, payload);
                peer.Stream.Write(frame, 0, frame.Length);
            }
            catch (Exception exception)
            {
                _log($"send to slot {peer.Slot} failed: {exception.Message}");
            }
        }

        private void Forward(RelayFrameType type, byte[] payload, int exceptSlot)
        {
            for (int i = 0; i < _peers.Count; i++)
            {
                if (_peers[i].Slot != exceptSlot) Send(_peers[i], type, payload);
            }
        }

        private void Broadcast(RelayFrameType type, byte[] payload, int exceptSlot)
        {
            for (int i = 0; i < _peers.Count; i++)
            {
                if (_peers[i].Slot != exceptSlot) Send(_peers[i], type, payload);
            }
        }

        private void EndMatch(
            string reason, RelayRecordTerminalReason terminalReason,
            uint? terminalTick = null)
        {
            if (_resetRequested) return;
            _log($"match ended: {reason}");
            if (_phase == ServerPhase.Running)
            {
                uint finalTick = terminalTick ?? GetOrderedTerminalTick();
                FinalizeRecordStream(
                    terminalReason, finalTick, drainConfirmedTail: true);
            }
            _phase = ServerPhase.Ended;
            _resetRequested = true;
            _resetReason = "match closed";
        }

        private void ApplyRequestedReset()
        {
            string reason = _resetReason;
            _resetRequested = false;
            _resetReason = null;
            ResetMatch(reason);
        }

        private void ResetMatch(string reason)
        {
            if (_recordStream != null)
            {
                try { _recordStream.Dispose(); } catch { /* ignore */ }
                _recordStream = null;
                if (!string.IsNullOrEmpty(_recordPartialPath))
                {
                    LastPartialRecordPath = _recordPartialPath;
                }
            }
            _pendingTicks.Clear();
            _confirmedFrames.Clear();
            _pendingRecordCount = 0;
            Array.Clear(_lastSequenceBySlot, 0, _lastSequenceBySlot.Length);
            Array.Clear(_lastEnqueueTickBySlot, 0, _lastEnqueueTickBySlot.Length);
            Array.Clear(_lastTargetTickBySlot, 0, _lastTargetTickBySlot.Length);
            for (int i = 0; i < _peers.Count; i++)
            {
                try { _peers[i].Client.Dispose(); } catch { /* ignore */ }
            }
            _peers.Clear();
            _seed = _configuredSeed;
            if (_listener != null)
            {
                if (_seed == 0)
                {
                    _seed = GenerateRandomSeed();
                }
            }
            _phase = ServerPhase.Listening;
            if (reason != null) _log($"relay reset ({reason}); listening for the next match");
        }

        internal static ulong FirstNonZeroGeneratedSeed(Func<ulong> nextCandidate)
        {
            if (nextCandidate == null)
            {
                throw new ArgumentNullException(nameof(nextCandidate));
            }
            ulong seed;
            do
            {
                seed = nextCandidate();
            }
            while (seed == 0);
            return seed;
        }

        private static ulong GenerateRandomSeed()
        {
            var rng = new Random(Environment.TickCount);
            return FirstNonZeroGeneratedSeed(
                () => ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next());
        }

        private bool TryFindFreeSlot(out byte slot)
        {
            for (int s = 0; s < _activeSlots.Length; s++)
            {
                bool occupied = false;
                for (int i = 0; i < _peers.Count; i++) occupied |= _peers[i].Slot == _activeSlots[s];
                if (!occupied)
                {
                    slot = _activeSlots[s];
                    return true;
                }
            }
            slot = byte.MaxValue;
            return false;
        }

        private Peer FindPeer(byte slot)
        {
            for (int i = 0; i < _peers.Count; i++) if (_peers[i].Slot == slot) return _peers[i];
            return null;
        }

        private void RemovePeerAt(int index)
        {
            Peer peer = _peers[index];
            _peers.RemoveAt(index);
            try { peer.Client.Dispose(); } catch { /* ignore */ }
        }

        private void ExpireHandshakePeers()
        {
            if (_phase != ServerPhase.Listening) return;
            uint now = _clockMilliseconds();
            for (int i = _peers.Count - 1; i >= 0; i--)
            {
                Peer peer = _peers[i];
                uint started = !peer.HelloOk
                    ? peer.ConnectedAtMilliseconds
                    : peer.ProofComplete
                        ? peer.ProofCompletedAtMilliseconds
                        : peer.HelloAtMilliseconds;
                uint timeout = !peer.HelloOk
                    ? HelloTimeoutMilliseconds
                    : peer.ProofComplete
                        ? OpponentWaitTimeoutMilliseconds
                        : ProofTimeoutMilliseconds;
                if (unchecked(now - started) < timeout) continue;

                string reason = !peer.HelloOk
                    ? "relay hello timed out"
                    : peer.ProofComplete
                        ? "waiting for opponent timed out"
                        : "handshake proof timed out";
                Send(peer, RelayFrameType.Reject,
                    RelayProtocol.CreateReasonPayload(RelayFrameType.Reject, reason));
                _log($"peer slot {peer.Slot}: {reason}");
                RemovePeerAt(i);
            }
        }

        private void MarkProofComplete(Peer peer)
        {
            if (peer.ProofComplete || peer.Fingerprint == null || peer.InitialSnapshot == null)
            {
                return;
            }
            peer.ProofComplete = true;
            peer.ProofCompletedAtMilliseconds = _clockMilliseconds();
        }
    }
}
