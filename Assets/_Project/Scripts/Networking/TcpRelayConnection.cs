using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Nova.Networking
{
    /// <summary>
    /// Client-side TCP socket of the relay protocol: synchronous connect,
    /// non-blocking read pump, direct writes. Frames go out via
    /// <see cref="SendFrame"/>; complete incoming frames surface through the
    /// handler installed with <see cref="SetFrameHandler"/>. Single-threaded
    /// and poll-driven — no async, no threads, no hidden work.
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
        private FrameHandler _onFrame;

        public RelayConnectionState State { get; private set; } = RelayConnectionState.Disconnected;
        public string LastError { get; private set; }

        public void SetFrameHandler(FrameHandler handler)
        {
            _onFrame = handler;
        }

        /// <summary>Opens the connection synchronously with a bounded wait; failure lands in <see cref="LastError"/> and <see cref="State"/>.</summary>
        public bool Connect(string host, int port, int timeoutMilliseconds = 5000)
        {
            if (State == RelayConnectionState.Connected || State == RelayConnectionState.Connecting)
            {
                return true;
            }
            Disconnect();
            State = RelayConnectionState.Connecting;
            LastError = null;
            try
            {
                _client = new TcpClient { NoDelay = true };
                IAsyncResult async = _client.BeginConnect(host, port, null, null);
                if (!async.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
                {
                    throw new TimeoutException($"Relay endpoint {host}:{port} did not answer within {timeoutMilliseconds} ms.");
                }
                _client.EndConnect(async);
                _stream = _client.GetStream();
                State = RelayConnectionState.Connected;
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
            if (State != RelayConnectionState.Connected || _stream == null) return;
            try
            {
                while (_client.Available > 0)
                {
                    int read = _stream.Read(_readBuffer, 0, _readBuffer.Length);
                    if (read <= 0)
                    {
                        LastError = "relay closed the connection";
                        State = RelayConnectionState.Failed;
                        CloseSocket();
                        return;
                    }
                    _cutter.Feed(_readBuffer.AsSpan(0, read));
                }
                while (_cutter.TryTakeFrame(out RelayFrameType type, out byte[] payload))
                {
                    _onFrame?.Invoke(type, payload);
                }
            }
            catch (Exception exception)
            {
                LastError = $"relay receive failed: {exception.Message}";
                State = RelayConnectionState.Failed;
                CloseSocket();
            }
        }

        private void CloseSocket()
        {
            try { _stream?.Dispose(); } catch { /* closing never throws */ }
            try { _client?.Dispose(); } catch { /* closing never throws */ }
            _stream = null;
            _client = null;
        }
    }
}
