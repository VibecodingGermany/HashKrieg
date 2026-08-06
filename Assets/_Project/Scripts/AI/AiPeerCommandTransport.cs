using System;
using Nova.Simulation.CommandsV1;

namespace Nova.AI
{
    /// <summary>
    /// The AI peer's command transport (docs/tech/AIArchitecture.md section 1
    /// — "Der Session-Host ... gibt ihre Intents an denselben MatchSession-/
    /// CommandIngress wie Human-Intents"). The MS-1 skirmish AI is a session
    /// PEER, not a local intent source of the host session: it owns a
    /// slot-bound <see cref="MatchSession"/> and <see cref="CommandIngress"/>
    /// of its own, so its slot, sequences and target ticks are assigned by the
    /// same session authority as a human's (Commands.md section 1 — the AI
    /// never chooses them itself). This transport delivers the peer ingress's
    /// serialized records into the HOST ingress's validating intake
    /// (<see cref="CommandIngress.TryAcceptRecordBytes"/>) — byte-for-byte the
    /// path a network peer's records will take, and the path
    /// Determinism10000Scenario already exercises for slot 1 with its
    /// stand-in "AI transport".
    /// <para>
    /// Delivery is synchronous and deterministic, exactly like
    /// <see cref="LocalLoopbackTransport"/>; the input delay comes from the
    /// record's TargetTick, not from transport latency. The verdict surface
    /// of the peer ingress (<c>TrySubmitIntent</c>) reports SUBMISSION, not
    /// the host intake verdict — the host's verdict is mirrored here as
    /// <see cref="LastResult"/>/<see cref="LastRejectReason"/> for
    /// diagnostics. The AI itself tolerates host rejections by construction:
    /// every decision is re-derived from state on the next decision tick, so
    /// a rejected intent is simply retried on the cadence instead of spammed.
    /// </para>
    /// <para>
    /// Zero engine dependencies (no UnityEngine types).
    /// </para>
    /// </summary>
    public sealed class AiPeerCommandTransport : ICommandTransport
    {
        private readonly CommandIngress _hostIngress;

        /// <summary>Last host-intake verdict observed on the forwarded path, for diagnostics.</summary>
        public CommandIngressResult LastResult { get; private set; } = CommandIngressResult.Accepted;

        /// <summary>Last structural rejection observed on the forwarded path, for diagnostics.</summary>
        public CommandRejectReason LastRejectReason { get; private set; }

        /// <summary>
        /// Binds this transport to the AI peer's <paramref name="peerIngress"/>
        /// (exactly once, same contract as <see cref="LocalLoopbackTransport"/>);
        /// records it emits are forwarded into <paramref name="hostIngress"/>.
        /// </summary>
        public AiPeerCommandTransport(CommandIngress peerIngress, CommandIngress hostIngress)
        {
            if (peerIngress == null) throw new ArgumentNullException(nameof(peerIngress));
            _hostIngress = hostIngress ?? throw new ArgumentNullException(nameof(hostIngress));
            peerIngress.BindTransport(this);
        }

        /// <summary>
        /// Delivers the AI peer's record bytes into the host ingress's
        /// validating intake — the identical structural validation every local
        /// record passes (Commands.md section 4).
        /// </summary>
        public void Send(byte[] recordBytes)
        {
            LastResult = _hostIngress.TryAcceptRecordBytes(recordBytes, out CommandRejectReason reason);
            LastRejectReason = LastResult == CommandIngressResult.Rejected ? reason : CommandRejectReason.None;
        }
    }
}
