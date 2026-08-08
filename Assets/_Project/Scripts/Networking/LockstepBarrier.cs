using System;
using System.Collections.Generic;

namespace Nova.Networking
{
    /// <summary>Verdict for one explicit mutation of the lockstep barrier.</summary>
    public enum LockstepBarrierVerdict
    {
        Accepted = 0,
        InvalidSlot = 1,
        InvalidOrPrunedTick = 2,
        InvalidRecordCount = 3,
        DuplicateCompletion = 4,
        RecordAfterCompletion = 5,
        RecordCountMismatch = 6,
    }

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
    /// Local completeness is announced ahead: Start safely prefills through
    /// CurrentTick + D - 1; each later step attempt first closes the current
    /// input window and announces CurrentTick + D before querying the barrier.
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
        private readonly Dictionary<uint, SlotTickState>[] _perSlot;
        private uint _prunedThrough;

        public LockstepBarrier(byte localSlot, byte[] activeSlots)
        {
            if (activeSlots == null) throw new ArgumentNullException(nameof(activeSlots));
            if (activeSlots.Length == 0) throw new ArgumentException("At least one active slot is required.", nameof(activeSlots));
            _activeSlots = (byte[])activeSlots.Clone();
            _localSlot = localSlot;
            _perSlot = new Dictionary<uint, SlotTickState>[Nova.Simulation.CommandsV1.CommandLimits.ReservedPlayerSlots];
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                _perSlot[_activeSlots[i]] = new Dictionary<uint, SlotTickState>();
            }
        }

        /// <summary>The local slot's announcement for <paramref name="targetTick"/> with <paramref name="recordCount"/> records.</summary>
        public LockstepBarrierVerdict NoteLocalTickComplete(
            uint targetTick, int recordCount)
        {
            if (!TryGetOpenTicks(_localSlot, targetTick, out Dictionary<uint, SlotTickState> ticks))
            {
                return targetTick == 0 || targetTick <= _prunedThrough
                    ? LockstepBarrierVerdict.InvalidOrPrunedTick
                    : LockstepBarrierVerdict.InvalidSlot;
            }
            if (recordCount < 0
                || recordCount > Nova.Simulation.CommandsV1.CommandLimits.MaxBatchRecordsPerTick)
            {
                return LockstepBarrierVerdict.InvalidRecordCount;
            }
            ticks.TryGetValue(targetTick, out SlotTickState state);
            if (state.Announced) return LockstepBarrierVerdict.DuplicateCompletion;
            state.Announced = true;
            state.AnnouncedRecords = recordCount;
            ticks[targetTick] = state;
            return LockstepBarrierVerdict.Accepted;
        }

        /// <summary>A remote TickComplete frame whose count must exactly match records already received over TCP.</summary>
        public LockstepBarrierVerdict NoteRemoteTickComplete(
            byte slot, uint targetTick, int recordCount)
        {
            if (slot == _localSlot)
            {
                return LockstepBarrierVerdict.InvalidSlot;
            }
            if (!TryGetOpenTicks(slot, targetTick, out Dictionary<uint, SlotTickState> ticks))
            {
                return targetTick == 0 || targetTick <= _prunedThrough
                    ? LockstepBarrierVerdict.InvalidOrPrunedTick
                    : LockstepBarrierVerdict.InvalidSlot;
            }
            if (recordCount < 0
                || recordCount > Nova.Simulation.CommandsV1.CommandLimits.MaxBatchRecordsPerTick)
            {
                return LockstepBarrierVerdict.InvalidRecordCount;
            }
            ticks.TryGetValue(targetTick, out SlotTickState state);
            if (state.Announced) return LockstepBarrierVerdict.DuplicateCompletion;
            if (state.ArrivedRecords != recordCount)
            {
                return LockstepBarrierVerdict.RecordCountMismatch;
            }
            state.Announced = true;
            state.AnnouncedRecords = recordCount;
            ticks[targetTick] = state;
            return LockstepBarrierVerdict.Accepted;
        }

        /// <summary>One arrived record frame of a remote slot; counted per its target tick.</summary>
        public LockstepBarrierVerdict NoteRemoteRecord(byte slot, uint targetTick)
        {
            if (slot == _localSlot)
            {
                return LockstepBarrierVerdict.InvalidSlot;
            }
            if (!TryGetOpenTicks(slot, targetTick, out Dictionary<uint, SlotTickState> ticks))
            {
                return targetTick == 0 || targetTick <= _prunedThrough
                    ? LockstepBarrierVerdict.InvalidOrPrunedTick
                    : LockstepBarrierVerdict.InvalidSlot;
            }
            ticks.TryGetValue(targetTick, out SlotTickState state);
            if (state.Announced)
            {
                return LockstepBarrierVerdict.RecordAfterCompletion;
            }
            state.ArrivedRecords++;
            ticks[targetTick] = state;
            return LockstepBarrierVerdict.Accepted;
        }

        /// <summary>True when every active slot announced <paramref name="tick"/> complete and all announced records arrived.</summary>
        public bool IsTickReady(uint tick)
        {
            return WaitingOnSlot(tick) < 0;
        }

        /// <summary>The first active slot blocking <paramref name="tick"/>, or -1 when the tick is ready (stall display).</summary>
        public int WaitingOnSlot(uint tick)
        {
            if (tick == 0 || tick <= _prunedThrough) return _localSlot;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                byte slot = _activeSlots[i];
                Dictionary<uint, SlotTickState> ticks = _perSlot[slot];
                if (!ticks.TryGetValue(tick, out SlotTickState state))
                {
                    return slot;
                }
                if (!state.Announced) return slot;
                // Local records live in the ingress pending pool at announce
                // time; only remote arrivals need counting.
                if (slot != _localSlot && state.ArrivedRecords != state.AnnouncedRecords)
                {
                    return slot;
                }
            }
            return -1;
        }

        /// <summary>Drops state through <paramref name="tick"/> (executed ticks never become relevant again).</summary>
        public void PruneThrough(uint tick)
        {
            if (tick <= _prunedThrough) return;
            _prunedThrough = tick;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                Dictionary<uint, SlotTickState> ticks = _perSlot[_activeSlots[i]];
                if (ticks.Count == 0) continue;
                var stale = new List<uint>(ticks.Count);
                foreach (uint key in ticks.Keys)
                {
                    if (key <= tick) stale.Add(key);
                }
                for (int s = 0; s < stale.Count; s++) ticks.Remove(stale[s]);
            }
        }

        private bool TryGetOpenTicks(
            byte slot, uint targetTick,
            out Dictionary<uint, SlotTickState> ticks)
        {
            ticks = null;
            if (targetTick == 0 || targetTick <= _prunedThrough
                || slot >= _perSlot.Length)
            {
                return false;
            }
            ticks = _perSlot[slot];
            return ticks != null;
        }
    }
}
