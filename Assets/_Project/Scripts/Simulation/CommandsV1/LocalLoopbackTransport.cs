using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Transport abstraction between the record-producing side of the ingress
    /// and its validating intake (docs/tech/Commands.md section 1). Every
    /// record — local, looped back or later networked — re-enters as raw
    /// canonical bytes and passes the identical structural validation, so the
    /// local MS-1 path exercises the same trust boundary as a future network
    /// transport.
    /// </summary>
    public interface ICommandTransport
    {
        /// <summary>Hands one serialized record to the transport for delivery.</summary>
        void Send(byte[] recordBytes);
    }

    /// <summary>
    /// Local loopback (MS-1): delivers each record synchronously back to the
    /// intake of the same <see cref="CommandIngress"/>. The input delay comes
    /// from the record's TargetTick, not from transport latency; delivery
    /// itself is immediate and deterministic.
    /// </summary>
    public sealed class LocalLoopbackTransport : ICommandTransport
    {
        private readonly CommandIngress _ingress;

        /// <summary>Last structural rejection observed on the looped-back intake, for diagnostics.</summary>
        public CommandRejectReason LastRejectReason { get; private set; }

        public LocalLoopbackTransport(CommandIngress ingress)
        {
            _ingress = ingress ?? throw new ArgumentNullException(nameof(ingress));
            _ingress.BindTransport(this);
            LastRejectReason = CommandRejectReason.None;
        }

        /// <summary>
        /// Delivers the record bytes back into the ingress intake. Structural
        /// rejection of a locally produced record indicates an ingress bug and
        /// is surfaced via <see cref="LastRejectReason"/> and the return value.
        /// </summary>
        public void Send(byte[] recordBytes)
        {
            CommandIngressResult result = _ingress.TryAcceptRecordBytes(recordBytes, out CommandRejectReason reason);
            LastRejectReason = result == CommandIngressResult.Rejected ? reason : CommandRejectReason.None;
        }
    }
}
