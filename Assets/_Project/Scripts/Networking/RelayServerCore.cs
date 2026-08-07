using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;

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
    /// The entire authority claim of this stage, stated honestly:
    /// (1) every record is revalidated structurally
    /// (<see cref="CommandPayloadValidation"/>) and (2) every record whose
    /// <c>PlayerSlot</c> is not the sender's assigned slot is discarded.
    /// That kills the cheapest form of cheating and nothing more — the relay
    /// does not execute the simulation and therefore cannot judge
    /// state-dependent legality; both clients still do that identically.
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
    /// the sprint letter ("the format exists") is logged as a D-088-followup
    /// decision note in the sprint result block.
    /// </para>
    /// </summary>
    public sealed class RelayServerCore
    {
        /// <summary>MS-1: exactly two players per match.</summary>
        public const int MaxPeers = 2;

        private sealed class Peer
        {
            public TcpClient Client;
            public NetworkStream Stream;
            public RelayProtocol.FrameCutter Cutter = new RelayProtocol.FrameCutter();
            public byte[] ReadBuffer = new byte[64 * 1024];
            public byte Slot;
            public bool HelloOk;
            public byte[] FingerprintBytes;
            public Nova.Simulation.Replays.MatchFingerprint Fingerprint;
            public byte[] InitialSnapshot;
            public readonly Dictionary<uint, ulong> StateHashes = new Dictionary<uint, ulong>();
        }

        private enum ServerPhase { Listening, Running, Ended }

        private readonly ulong _matchToken;
        private readonly uint _inputDelayTicks;
        private readonly string _recordDirectory;
        private readonly Action<string> _log;
        private readonly List<Peer> _peers = new List<Peer>();
        private readonly byte[] _activeSlots = { 0, 1 };

        private TcpListener _listener;
        private ServerPhase _phase = ServerPhase.Listening;
        private ulong _seed;
        private Stream _recordStream;

        /// <summary>Seed of the current match (generated at listener start when 0 was configured).</summary>
        public ulong Seed => _seed;

        public RelayServerCore(ulong matchToken, ulong seed, uint inputDelayTicks, string recordDirectory, Action<string> log)
        {
            _matchToken = matchToken;
            _seed = seed;
            _inputDelayTicks = inputDelayTicks >= 1 ? inputDelayTicks : 3;
            _recordDirectory = recordDirectory;
            _log = log ?? (_ => { });
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
                var rng = new Random(Environment.TickCount);
                _seed = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            }
            _log($"relay listening on {bindAddress}:{Port}, seed 0x{_seed:X16}, input delay {_inputDelayTicks}");
        }

        /// <summary>The effective bound port.</summary>
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public void Stop()
        {
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
                if (_peers.Count >= MaxPeers || _phase == ServerPhase.Ended)
                {
                    _log("connection refused: match full or ended");
                    try { accepted.Dispose(); } catch { /* ignore */ }
                    continue;
                }
                var peer = new Peer { Client = accepted, Stream = accepted.GetStream(), Slot = _activeSlots[_peers.Count] };
                _peers.Add(peer);
                _log($"peer connected as slot {peer.Slot} ({_peers.Count}/{MaxPeers})");
            }

            for (int i = _peers.Count - 1; i >= 0; i--)
            {
                if (!PumpPeer(_peers[i]))
                {
                    Peer lost = _peers[i];
                    _peers.RemoveAt(i);
                    if (_phase == ServerPhase.Running)
                    {
                        Broadcast(RelayFrameType.PeerLost, RelayProtocol.CreateSlotTickPayload(RelayFrameType.PeerLost, lost.Slot, 0), exceptSlot: -1);
                        EndMatch($"peer slot {lost.Slot} disconnected");
                    }
                    else
                    {
                        _log($"peer slot {lost.Slot} disconnected during handshake");
                        ResetMatch("handshake peer lost");
                    }
                }
            }

            TryStartMatch();
        }

        // ------------------------------------------------------------------
        // Peer pump + frame handling
        // ------------------------------------------------------------------

        private bool PumpPeer(Peer peer)
        {
            try
            {
                while (peer.Client.Available > 0)
                {
                    int read = peer.Stream.Read(peer.ReadBuffer, 0, peer.ReadBuffer.Length);
                    if (read <= 0) return false;
                    peer.Cutter.Feed(peer.ReadBuffer.AsSpan(0, read));
                }
                while (peer.Cutter.TryTakeFrame(out RelayFrameType type, out byte[] payload))
                {
                    if (!HandleFrame(peer, type, payload)) return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                _log($"peer slot {peer.Slot} socket error: {exception.Message}");
                return false;
            }
        }

        /// <summary>False return: the peer is broken and must be dropped.</summary>
        private bool HandleFrame(Peer peer, RelayFrameType type, byte[] payload)
        {
            switch (type)
            {
                case RelayFrameType.Hello:
                {
                    if (!RelayProtocol.TryParseHello(payload, out byte version, out ulong token)
                        || version != RelayProtocol.ProtocolVersion || token != _matchToken)
                    {
                        Send(peer, RelayFrameType.Reject,
                            RelayProtocol.CreateReasonPayload(RelayFrameType.Reject,
                                version != RelayProtocol.ProtocolVersion
                                    ? $"protocol version mismatch (server v{RelayProtocol.ProtocolVersion})"
                                    : "wrong match code"));
                        return false;
                    }
                    peer.HelloOk = true;
                    Send(peer, RelayFrameType.Offer, RelayProtocol.CreateOfferPayload(
                        peer.Slot, _activeSlots, _seed, _inputDelayTicks, SimDefinitions.ComputeDefinitionsHash64()));
                    return true;
                }

                case RelayFrameType.Fingerprint:
                {
                    if (!peer.HelloOk) return false;
                    if (!Nova.Simulation.Replays.MatchFingerprint.TryParse(payload, out Nova.Simulation.Replays.MatchFingerprint fingerprint))
                    {
                        Send(peer, RelayFrameType.Reject,
                            RelayProtocol.CreateReasonPayload(RelayFrameType.Reject, "malformed match fingerprint"));
                        return false;
                    }
                    peer.FingerprintBytes = payload;
                    peer.Fingerprint = fingerprint;
                    return true;
                }

                case RelayFrameType.InitialSnapshot:
                {
                    if (!peer.HelloOk) return false;
                    peer.InitialSnapshot = payload;
                    return true;
                }

                case RelayFrameType.CommandRecord:
                {
                    if (_phase != ServerPhase.Running) return true; // before Start: ignore silently
                    if (!CommandRecord.TryDeserialize(payload, out CommandRecord record, out int consumed)
                        || consumed != payload.Length)
                    {
                        _log($"slot {peer.Slot}: dropped malformed record frame");
                        return true;
                    }
                    // THE authority check of this stage: the slot on the wire
                    // must be the slot of the connection it arrived on.
                    if (record.PlayerSlot != peer.Slot)
                    {
                        _log($"slot {peer.Slot}: dropped record claiming slot {record.PlayerSlot}");
                        return true;
                    }
                    if (!CommandPayloadValidation.TryValidateStreamPayload(
                            record.Kind, record.PayloadVersion, record.Payload.Span, out CommandRejectReason reason))
                    {
                        _log($"slot {peer.Slot}: dropped structurally invalid record ({reason})");
                        return true;
                    }
                    Forward(RelayFrameType.CommandRecord, payload, exceptSlot: peer.Slot);
                    AppendRecordToStream(record);
                    return true;
                }

                case RelayFrameType.TickComplete:
                {
                    if (_phase != ServerPhase.Running) return true;
                    if (!RelayProtocol.TryParseTickComplete(payload, out byte slot, out _, out _) || slot != peer.Slot)
                    {
                        _log($"slot {peer.Slot}: dropped tick-complete claiming slot {slot}");
                        return true;
                    }
                    Forward(RelayFrameType.TickComplete, payload, exceptSlot: peer.Slot);
                    return true;
                }

                case RelayFrameType.StateHash:
                {
                    if (_phase != ServerPhase.Running) return true;
                    if (!RelayProtocol.TryParseStateHash(payload, out byte slot, out uint tick, out ulong hash)
                        || slot != peer.Slot)
                    {
                        return true;
                    }
                    peer.StateHashes[tick] = hash;
                    CheckDesync(tick);
                    return true;
                }

                case RelayFrameType.Ping:
                    Send(peer, RelayFrameType.Pong, payload);
                    return true;

                default:
                    _log($"slot {peer.Slot}: ignoring unexpected frame type {type}");
                    return true;
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

            Peer a = _peers[0];
            Peer b = _peers[1];

            // Byte equality: identical builds + same seed + identical
            // bootstrap produce byte-identical fingerprints; anything less is
            // a mismatch worth a readable message.
            string mismatch = CompareFingerprints(a.Fingerprint, b.Fingerprint);
            if (mismatch == null && a.Fingerprint.DefinitionsHash64 != SimDefinitions.ComputeDefinitionsHash64())
            {
                mismatch = $"DefinitionsHash64 (clients 0x{a.Fingerprint.DefinitionsHash64:X16} != relay build 0x{SimDefinitions.ComputeDefinitionsHash64():X16})";
            }
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
                EndMatch(reason);
                return;
            }

            _phase = ServerPhase.Running;
            OpenRecordStream(a.FingerprintBytes, a.InitialSnapshot);
            for (int i = 0; i < _peers.Count; i++)
            {
                Send(_peers[i], RelayFrameType.Start, Array.Empty<byte>());
            }
            _log($"match started: seed 0x{_seed:X16}, delay {_inputDelayTicks}, fingerprint hash 0x{a.Fingerprint.ComputeHash():X16}");
        }

        /// <summary>Names the first differing fingerprint field, or null on equality.</summary>
        private static string CompareFingerprints(Nova.Simulation.Replays.MatchFingerprint a, Nova.Simulation.Replays.MatchFingerprint b)
        {
            if (a.StateSchemaVersion != b.StateSchemaVersion) return $"StateSchemaVersion ({a.StateSchemaVersion} != {b.StateSchemaVersion})";
            if (a.CommandSchemaVersion != b.CommandSchemaVersion) return $"CommandSchemaVersion ({a.CommandSchemaVersion} != {b.CommandSchemaVersion})";
            if (a.PayloadSchemaVersion != b.PayloadSchemaVersion) return $"PayloadSchemaVersion ({a.PayloadSchemaVersion} != {b.PayloadSchemaVersion})";
            if (a.SnapshotSchemaVersion != b.SnapshotSchemaVersion) return $"SnapshotSchemaVersion ({a.SnapshotSchemaVersion} != {b.SnapshotSchemaVersion})";
            if (a.NumericModelId != b.NumericModelId) return $"NumericModelId ({a.NumericModelId} != {b.NumericModelId})";
            if (a.TicksPerSecond != b.TicksPerSecond) return $"TicksPerSecond ({a.TicksPerSecond} != {b.TicksPerSecond})";
            if (a.PrngId != b.PrngId) return $"PrngId ({a.PrngId} != {b.PrngId})";
            if (a.RulesHash64 != b.RulesHash64) return $"RulesHash64 (0x{a.RulesHash64:X16} != 0x{b.RulesHash64:X16})";
            if (a.DefinitionsHash64 != b.DefinitionsHash64) return $"DefinitionsHash64 (0x{a.DefinitionsHash64:X16} != 0x{b.DefinitionsHash64:X16})";
            if (a.MapHash64 != b.MapHash64) return $"MapHash64 (0x{a.MapHash64:X16} != 0x{b.MapHash64:X16})";
            if (!a.GetSlotOccupancyCopy().AsSpan().SequenceEqual(b.GetSlotOccupancyCopy())) return "slot occupancy";
            if (!a.GetSlotFactionCopy().AsSpan().SequenceEqual(b.GetSlotFactionCopy())) return "slot factions";
            if (a.StartSeed != b.StartSeed) return $"StartSeed (0x{a.StartSeed:X16} != 0x{b.StartSeed:X16})";
            if (a.InitialStateHash != b.InitialStateHash) return $"InitialStateHash (0x{a.InitialStateHash:X16} != 0x{b.InitialStateHash:X16})";
            if (a.InputDelayTicks != b.InputDelayTicks) return $"InputDelayTicks ({a.InputDelayTicks} != {b.InputDelayTicks})";
            return null;
        }

        // ------------------------------------------------------------------
        // Desync detection (A5)
        // ------------------------------------------------------------------

        private void CheckDesync(uint tick)
        {
            ulong? first = null;
            for (int i = 0; i < _peers.Count; i++)
            {
                if (!_peers[i].StateHashes.TryGetValue(tick, out ulong hash)) return;
                if (first == null) first = hash;
                else if (first.Value != hash)
                {
                    string reason = $"DESYNC at tick {tick}: slot hashes 0x{_peers[0].StateHashes[tick]:X16} / 0x{_peers[1].StateHashes[tick]:X16}";
                    _log(reason);
                    Broadcast(RelayFrameType.Desync, RelayProtocol.CreateSlotTickPayload(RelayFrameType.Desync, 255, tick), exceptSlot: -1);
                    EndMatch(reason);
                    return;
                }
            }
        }

        // ------------------------------------------------------------------
        // Command-stream recording (*.novarec)
        // ------------------------------------------------------------------

        private void OpenRecordStream(byte[] fingerprintBytes, byte[] initialSnapshot)
        {
            if (string.IsNullOrEmpty(_recordDirectory)) return;
            try
            {
                Directory.CreateDirectory(_recordDirectory);
                string path = Path.Combine(_recordDirectory,
                    $"match-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{_seed:X16}.novarec");
                _recordStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                WriteRecordBytes(System.Text.Encoding.ASCII.GetBytes("NOVAREC1"));
                WriteLengthPrefixed(fingerprintBytes);
                WriteLengthPrefixed(initialSnapshot);
                _log($"recording command stream to {path}");
            }
            catch (Exception exception)
            {
                _log($"recording disabled: {exception.Message}");
                _recordStream = null;
            }
        }

        private readonly List<CommandRecord> _tickRecords = new List<CommandRecord>();

        private void AppendRecordToStream(CommandRecord record)
        {
            if (_recordStream == null) return;
            _tickRecords.Add(record);
            FlushCompletedTicks();
        }

        private void FlushCompletedTicks()
        {
            if (_tickRecords.Count == 0) return;
            _tickRecords.Sort(CommandBatch.CompareRecords);
            int index = 0;
            while (index < _tickRecords.Count)
            {
                uint tick = _tickRecords[index].TargetTick;
                int end = index;
                while (end < _tickRecords.Count && _tickRecords[end].TargetTick == tick) end++;
                bool isLastGroup = end == _tickRecords.Count;
                if (isLastGroup) break; // the latest tick may still receive records
                WriteTickRecords(tick, index, end);
                index = end;
            }
            if (index > 0)
            {
                _tickRecords.RemoveRange(0, index);
            }
        }

        private void WriteTickRecords(uint tick, int from, int to)
        {
            byte[] header = new byte[6];
            RelayProtocol.WriteUInt32(header, 0, tick);
            RelayProtocol.WriteUInt16(header, 4, unchecked((ushort)(to - from)));
            WriteRecordBytes(header);
            for (int i = from; i < to; i++)
            {
                WriteLengthPrefixed(_tickRecords[i].Serialize());
            }
        }

        private void CloseRecordStream()
        {
            if (_recordStream == null) return;
            try
            {
                // Flush the tail: everything still pending is complete now.
                _tickRecords.Sort(CommandBatch.CompareRecords);
                int index = 0;
                while (index < _tickRecords.Count)
                {
                    uint tick = _tickRecords[index].TargetTick;
                    int end = index;
                    while (end < _tickRecords.Count && _tickRecords[end].TargetTick == tick) end++;
                    WriteTickRecords(tick, index, end);
                    index = end;
                }
                _tickRecords.Clear();
                _recordStream.Flush();
                _recordStream.Dispose();
            }
            catch { /* closing must not throw */ }
            _recordStream = null;
        }

        private void WriteLengthPrefixed(byte[] bytes)
        {
            var length = new byte[4];
            RelayProtocol.WriteUInt32(length, 0, unchecked((uint)bytes.Length));
            WriteRecordBytes(length);
            WriteRecordBytes(bytes);
        }

        private void WriteRecordBytes(byte[] bytes)
        {
            try { _recordStream?.Write(bytes, 0, bytes.Length); }
            catch (Exception exception) { _log($"record write failed: {exception.Message}"); }
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

        private void EndMatch(string reason)
        {
            _log($"match ended: {reason}");
            CloseRecordStream();
            _phase = ServerPhase.Ended;
            ResetMatch("match closed");
        }

        private void ResetMatch(string reason)
        {
            for (int i = 0; i < _peers.Count; i++)
            {
                try { _peers[i].Client.Dispose(); } catch { /* ignore */ }
            }
            _peers.Clear();
            _seed = 0;
            if (_listener != null)
            {
                var rng = new Random(Environment.TickCount);
                _seed = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
            }
            _phase = ServerPhase.Listening;
            if (reason != null) _log($"relay reset ({reason}); listening for the next match");
        }
    }
}
