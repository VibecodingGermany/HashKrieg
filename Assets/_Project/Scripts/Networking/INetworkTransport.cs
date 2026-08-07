using System;
using Nova.Simulation.CommandsV1;

namespace Nova.Networking
{
    /// <summary>Connection lifecycle of a network transport (<see cref="INetworkTransport"/>).</summary>
    public enum RelayConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Failed = 3,
    }

    /// <summary>
    /// A command transport with a receive path and a connection lifecycle —
    /// the network sibling of <see cref="LocalLoopbackTransport"/>
    /// (docs/production/hashkrieg/12_Sprint_Zu_Zweit.md, strand A1).
    /// <see cref="ICommandTransport"/> itself stays untouched: it is the
    /// proven seam into the ingress. Incoming record bytes are delivered to
    /// the LOCAL ingress's validating intake
    /// (<see cref="CommandIngress.TryAcceptRecordBytes"/>) — byte-for-byte
    /// the path <see cref="Nova.AI.AiPeerCommandTransport"/> already
    /// exercises, so foreign records pass the identical structural
    /// validation as local ones.
    /// <para>
    /// No UnityEngine types: Nova.Networking keeps
    /// <c>noEngineReferences: true</c> so the relay server process and the
    /// headless test lane compile the identical sources.
    /// </para>
    /// </summary>
    public interface INetworkTransport : ICommandTransport
    {
        /// <summary>Opens the connection to the relay endpoint. Non-blocking; completion surfaces via <see cref="State"/>.</summary>
        void Connect(string host, int port);

        /// <summary>Closes the connection (idempotent).</summary>
        void Disconnect();

        /// <summary>Pumps the socket: reads arrived bytes, cuts frames, dispatches them to the bound handlers.</summary>
        void Poll();

        /// <summary>Current lifecycle state.</summary>
        RelayConnectionState State { get; }

        /// <summary>Last measured round trip in milliseconds, or null before the first pong.</summary>
        uint? RoundTripMilliseconds { get; }

        /// <summary>Human-readable last transport error, or null.</summary>
        string LastError { get; }
    }
}
