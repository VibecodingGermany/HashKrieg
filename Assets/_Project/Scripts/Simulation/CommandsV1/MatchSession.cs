using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Session authority (docs/tech/Commands.md section 1): binds the local
    /// player slot, owns the match tick and the input delay. The session,
    /// together with <see cref="CommandIngress"/>, is the only place where a
    /// <see cref="CommandIntent"/> becomes a record — UI and AI can neither
    /// choose a slot, a sequence nor a target tick.
    /// <para>
    /// <see cref="InputDelayTicks"/> is part of the match fingerprint; the
    /// MS-1 value is exactly 1.
    /// </para>
    /// </summary>
    public sealed class MatchSession
    {
        private readonly bool[] _activeSlots = new bool[CommandLimits.ReservedPlayerSlots];

        /// <summary>
        /// Creates a session. <paramref name="localSlot"/> must be one of the
        /// <paramref name="activeSlots"/> (MS-1: exactly two active of eight
        /// reserved slots, SimulationCore.md section 10). The input delay must
        /// be at least 1 so a live target tick always lies in the future.
        /// </summary>
        public MatchSession(byte localSlot, byte[] activeSlots, uint inputDelayTicks)
        {
            if (activeSlots == null) throw new ArgumentNullException(nameof(activeSlots));
            if (activeSlots.Length == 0 || activeSlots.Length > CommandLimits.ReservedPlayerSlots)
            {
                throw new ArgumentException("Active slot count outside the reserved range.", nameof(activeSlots));
            }
            if (inputDelayTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(inputDelayTicks), "Input delay must be >= 1.");
            }
            for (int i = 0; i < activeSlots.Length; i++)
            {
                if (activeSlots[i] >= CommandLimits.ReservedPlayerSlots)
                {
                    throw new ArgumentOutOfRangeException(nameof(activeSlots), "Slot outside the reserved range.");
                }
                _activeSlots[activeSlots[i]] = true;
            }
            if (localSlot >= CommandLimits.ReservedPlayerSlots || !_activeSlots[localSlot])
            {
                throw new ArgumentException("Local slot must be an active session slot.", nameof(localSlot));
            }
            LocalSlot = localSlot;
            InputDelayTicks = inputDelayTicks;
            CurrentTick = 0;
        }

        /// <summary>The session-bound local player slot; the only slot intents are sealed for.</summary>
        public byte LocalSlot { get; }

        /// <summary>Input delay in ticks; part of the match fingerprint (MS-1: exactly 1).</summary>
        public uint InputDelayTicks { get; }

        /// <summary>Current authoritative session tick.</summary>
        public uint CurrentTick { get; private set; }

        /// <summary>True when the slot is an active slot of this session.</summary>
        public bool IsActiveSlot(byte playerSlot)
        {
            return playerSlot < CommandLimits.ReservedPlayerSlots && _activeSlots[playerSlot];
        }

        /// <summary>Advances the session tick by exactly one (10 Hz lockstep).</summary>
        public void AdvanceTick()
        {
            CurrentTick++;
        }
    }
}
