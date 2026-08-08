using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Nova.Networking
{
    /// <summary>
    /// Client-side TCP socket of the relay protocol: non-blocking connect and
    /// read pump, direct writes. Frames go out via
    /// <see cref="SendFrame"/>; complete incoming frames surface through the
    /// handler installed with <see cref="SetFrameHandler"/>. Single-threaded
    /// and poll-driven. The platform socket performs the connect operation;
    /// <see cref="Poll"/> observes its completion or timeout without ever
    /// blocking Unity's main thread.
    /// <para>
    /// TCP, deliberately not UDP (strand A3 of the sprint doc): lockstep
    /// needs reliable, ordered delivery — exactly what TCP gives and what
    /// UDP would have to rebuild by hand. At two players, 10 Hz and records
    /// of 20–60 bytes, head-of-line blocking is not a real problem, and an
    /// entire error class disappears. UDP/RUDP is the later optimization
    /// when player count or latency demands it — not today.
    /// </para>
    /// </summary>
    public sealed class TcpRelayConnection
    {
        /// <summary>Frame handler installed by the protocol layer above.</summary>
        public delegate void FrameHandler(RelayFrameType type, byte[] payload);

        private readonly RelayProtocol.FrameCutter _cutter = new RelayProtocol.FrameCutter();
        private readonly byte[] _readBuffer = new byte[64 * 1024];
        private TcpClient _client;
        private NetworkStream _stream;
        private IAsyncResult _connectResult;
        private readonly Func<uint> _clockMilliseconds;
        private uint _connectStartedAtMilliseconds;
        private uint _connectTimeoutMilliseconds;
        private string _connectEndpoint;
        private FrameHandler _onFrame;

        public RelayConnectionState State { get; private set; } = RelayConnectionState.Disconnected;
        public string LastError { get; private set; }

        public TcpRelayConnection()
            : this(() => unchecked((uint)Environment.TickCount))
        {
        }

        internal TcpRelayConnection(Func<uint> clockMilliseconds)
        {
            _clockMilliseconds = clockMilliseconds
                ?? throw new ArgumentNullException(nameof(clockMilliseconds));
        }

        public void SetFrameHandler(FrameHandler handler)
        {
            _onFrame = handler;
        }

        /// <summary>
        /// Begins a connection attempt and returns immediately. <see cref="Poll"/>
        /// completes it or fails it after the supplied timeout.
        /// </summary>
        public bool Connect(string host, int port, int timeoutMilliseconds = 5000)
        {
            if (State != RelayConnectionState.Disconnected)
            {
                LastError = "relay connection reuse refused";
                State = RelayConnectionState.Failed;
                CloseSocket();
                return false;
            }
            if (string.IsNullOrWhiteSpace(host) || port < 1 || port > 65535
                || timeoutMilliseconds <= 0)
            {
                LastError = "invalid relay endpoint or connect timeout";
                State = RelayConnectionState.Failed;
                return false;
            }
            State = RelayConnectionState.Connecting;
            LastError = null;
            try
            {
                _client = new TcpClient { NoDelay = true };
                _connectEndpoint = $"{host}:{port}";
                _connectStartedAtMilliseconds = _clockMilliseconds();
                _connectTimeoutMilliseconds = unchecked((uint)timeoutMilliseconds);
                _connectResult = _client.BeginConnect(host, port, null, null);
                return true;
            }
            catch (Exception exception)
            {
                LastError = $"connect to {host}:{port} failed: {exception.Message}";
                State = RelayConnectionState.Failed;
                CloseSocket();
                return false;
            }
        }

        public void Disconnect()
        {
            CloseSocket();
            LastError = null;
            State = RelayConnectionState.Disconnected;
        }

        /// <summary>Sends one complete frame. Returns false (and records the error) when the connection is down.</summary>
        public bool SendFrame(RelayFrameType type, byte[] payload)
        {
            if (State != RelayConnectionState.Connected || _stream == null)
            {
                LastError = "send on a closed relay connection";
                return false;
            }
            try
            {
                byte[] frame = RelayProtocol.CreateFrame(type, payload);
                _stream.Write(frame, 0, frame.Length);
                return true;
            }
            catch (Exception exception)
            {
                LastError = $"relay send failed: {exception.Message}";
                State = RelayConnectionState.Failed;
                CloseSocket();
                return false;
            }
        }

        /// <summary>Pumps arrived bytes and dispatches every complete frame to the installed handler.</summary>
        public void Poll()
        {
            if (State == RelayConnectionState.Connecting)
            {
                PollConnect();
            }
            if (State != RelayConnectionState.Connected || _stream == null) return;
            try
            {
                if (_client.Client.Poll(0, SelectMode.SelectRead) && _client.Available == 0)
                {
                    LastError = "relay closed the connection";
                    State = RelayConnectionState.Failed;
                    CloseSocket();
                    return;
                }
                while (_client.Available > 0)
                {
                    int readCapacity = Math.Min(
                        _readBuffer.Length, _cutter.RemainingCapacity);
                    if (readCapacity <= 0)
                    {
                        throw new RelayFrameFormatException(
                            "Relay frame carry could not be drained.");
                    }
                    int read = _stream.Read(_readBuffer, 0, readCapacity);
                    if (read <= 0)
                    {
                        LastError = "relay closed the connection";
                        State = RelayConnectionState.Failed;
                        CloseSocket();
                        return;
                    }
                    _cutter.Feed(_readBuffer.AsSpan(0, read));
                    while (_cutter.TryTakeFrame(
                        out RelayFrameType type, out byte[] payload))
                    {
                        _onFrame?.Invoke(type, payload);
                        if (State != RelayConnectionState.Connected
                            || _client == null || _stream == null)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LastError = $"relay receive failed: {exception.Message}";
                State = RelayConnectionState.Failed;
                CloseSocket();
            }
        }

        private void PollConnect()
        {
            if (_client == null || _connectResult == null)
            {
                FailConnect("relay connect state was incomplete");
                return;
            }

            if (!_connectResult.IsCompleted)
            {
                uint elapsed = unchecked(_clockMilliseconds() - _connectStartedAtMilliseconds);
                if (elapsed < _connectTimeoutMilliseconds) return;

                FailConnect(
                    $"Relay endpoint {_connectEndpoint} did not answer within {_connectTimeoutMilliseconds} ms.");
                return;
            }

            try
            {
                _client.EndConnect(_connectResult);
                _connectResult = null;
                _stream = _client.GetStream();
                State = RelayConnectionState.Connected;
            }
            catch (Exception exception)
            {
                FailConnect($"connect to {_connectEndpoint} failed: {exception.Message}");
            }
        }

        private void FailConnect(string error)
        {
            LastError = error;
            State = RelayConnectionState.Failed;
            CloseSocket();
        }

        private void CloseSocket()
        {
            try { _stream?.Dispose(); } catch { /* closing never throws */ }
            try { _client?.Dispose(); } catch { /* closing never throws */ }
            _stream = null;
            _client = null;
            _connectResult = null;
            _connectEndpoint = null;
            _connectStartedAtMilliseconds = 0;
            _connectTimeoutMilliseconds = 0;
        }
    }
}
