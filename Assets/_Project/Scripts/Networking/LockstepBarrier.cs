using System;
using System.Collections.Generic;

namespace Nova.Networking
{
    /// <summary>
    /// The lockstep barrier (docs/production/hashkrieg/12_Sprint_Zu_Zweit.md,
    /// strand A2): a client may execute tick X only when EVERY active slot
    /// has announced its input for X complete (a TickComplete transport
    /// frame) and the announced number of records has actually arrived.
    /// <para>
    /// Nothing is estimated, anticipated or discarded: a missing
    /// announcement stalls the simulation — stall is correct, running on is
    /// the bug (a lockstep client that prefers to keep going is a broken
    /// lockstep client).
    /// </para>
    /// <para>
    /// Local completeness is announced ahead: with input delay D, no new
    /// local record for tick X can be minted once the session tick passed
    /// X - D, so the announcement for X goes out at session tick
    /// X - D + 1 — one tick of network slack before X must execute.
    /// Local records need no arrival counting: they sit in the ingress
    /// pending pool until the seal drains them.
    /// </para>
    /// </summary>
    public sealed class LockstepBarrier
    {
        private struct SlotTickState
        {
            public bool Announced;
            public int AnnouncedRecords;
            public int ArrivedRecords;
        }

        private readonly byte[] _activeSlots;
        private readonly byte _localSlot;
        private readonly Dictionary<int, SlotTickState>[] _perSlot;
        private uint _pruneBelow = 1;

        public LockstepBarrier(byte localSlot, byte[] activeSlots)
        {
            if (activeSlots == null) throw new ArgumentNullException(nameof(activeSlots));
            if (activeSlots.Length == 0) throw new ArgumentException("At least one active slot is required.", nameof(activeSlots));
            _activeSlots = (byte[])activeSlots.Clone();
            _localSlot = localSlot;
            _perSlot = new Dictionary<int, SlotTickState>[Nova.Simulation.CommandsV1.CommandLimits.ReservedPlayerSlots];
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                _perSlot[_activeSlots[i]] = new Dictionary<int, SlotTickState>();
            }
        }

        /// <summary>The local slot's announcement for <paramref name="targetTick"/> with <paramref name="recordCount"/> records.</summary>
        public void NoteLocalTickComplete(uint targetTick, int recordCount)
        {
            NoteTickComplete(_localSlot, targetTick, recordCount);
        }

        /// <summary>A TickComplete frame (local or remote) for one slot and tick.</summary>
        public void NoteTickComplete(byte slot, uint targetTick, int recordCount)
        {
            Dictionary<int, SlotTickState> ticks = TicksOf(slot);
            ticks.TryGetValue((int)targetTick, out SlotTickState state);
            state.Announced = true;
            state.AnnouncedRecords = recordCount;
            ticks[(int)targetTick] = state;
        }

        /// <summary>One arrived record frame of a remote slot; counted per its target tick.</summary>
        public void NoteRemoteRecord(byte slot, uint targetTick)
        {
            Dictionary<int, SlotTickState> ticks = TicksOf(slot);
            ticks.TryGetValue((int)targetTick, out SlotTickState state);
            state.ArrivedRecords++;
            ticks[(int)targetTick] = state;
        }

        /// <summary>True when every active slot announced <paramref name="tick"/> complete and all announced records arrived.</summary>
        public bool IsTickReady(uint tick)
        {
            return WaitingOnSlot(tick) < 0;
        }

        /// <summary>The first active slot blocking <paramref name="tick"/>, or -1 when the tick is ready (stall display).</summary>
        public int WaitingOnSlot(uint tick)
        {
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                byte slot = _activeSlots[i];
                Dictionary<int, SlotTickState> ticks = TicksOf(slot);
                if (!ticks.TryGetValue((int)tick, out SlotTickState state))
                {
                    return slot;
                }
                if (!state.Announced) return slot;
                // Local records live in the ingress pending pool at announce
                // time; only remote arrivals need counting.
                if (slot != _localSlot && state.ArrivedRecords < state.AnnouncedRecords)
                {
                    return slot;
                }
            }
            return -1;
        }

        /// <summary>Drops state below <paramref name="tick"/> (executed ticks never become relevant again).</summary>
        public void PruneThrough(uint tick)
        {
            if (tick <= _pruneBelow) return;
            _pruneBelow = tick;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                Dictionary<int, SlotTickState> ticks = _perSlot[_activeSlots[i]];
                if (ticks.Count == 0) continue;
                var stale = new List<int>(ticks.Count);
                foreach (int key in ticks.Keys)
                {
                    if (key < (int)tick) stale.Add(key);
                }
                for (int s = 0; s < stale.Count; s++) ticks.Remove(stale[s]);
            }
        }

        private Dictionary<int, SlotTickState> TicksOf(byte slot)
        {
            Dictionary<int, SlotTickState> ticks = _perSlot[slot];
            if (ticks == null)
            {
                throw new ArgumentException($"Slot {slot} is not an active slot of this barrier.", nameof(slot));
            }
            return ticks;
        }
    }
}
