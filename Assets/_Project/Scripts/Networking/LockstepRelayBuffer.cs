using System;
using System.Collections.Generic;

namespace Nova.Networking
{
    /// <summary>
    /// Frame synchronization relay buffer storing incoming network command packets per tick.
    /// Performs lockstep readiness checks and StateHash desync verification.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class LockstepRelayBuffer
    {
        public const int MaxBufferedTicks = 256;

        private readonly Dictionary<uint, List<CommandEnvelopeNetPacket>> _tickBuffers;

        public LockstepRelayBuffer()
        {
            _tickBuffers = new Dictionary<uint, List<CommandEnvelopeNetPacket>>(MaxBufferedTicks);
        }

        public void PushPacket(in CommandEnvelopeNetPacket packet)
        {
            if (!_tickBuffers.TryGetValue(packet.TargetTick, out var list))
            {
                list = new List<CommandEnvelopeNetPacket>(8);
                _tickBuffers[packet.TargetTick] = list;
            }

            list.Add(packet);
        }

        public bool IsTickReady(uint targetTick, int expectedPlayerCount)
        {
            if (!_tickBuffers.TryGetValue(targetTick, out var list)) return false;
            return list.Count >= expectedPlayerCount;
        }

        public bool VerifyDesyncHashes(uint targetTick, out ulong mismatchHash)
        {
            mismatchHash = 0;
            if (!_tickBuffers.TryGetValue(targetTick, out var list) || list.Count == 0) return true;

            ulong firstHash = list[0].StateHash;
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].StateHash != firstHash)
                {
                    mismatchHash = list[i].StateHash;
                    return false; // Desync detected!
                }
            }

            return true;
        }

        public ReadOnlySpan<CommandEnvelopeNetPacket> GetPacketsForTick(uint targetTick)
        {
            if (_tickBuffers.TryGetValue(targetTick, out var list))
            {
                return list.ToArray();
            }

            return ReadOnlySpan<CommandEnvelopeNetPacket>.Empty;
        }
    }
}
