using System;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Bounded, fully deterministic cache of generated <see cref="FlowField"/>
    /// instances keyed by their destination cell.
    /// <para>
    /// Why it exists: with a single global flow field every Move command
    /// retargeted every already-moving unit — ordering group B somewhere made
    /// group A turn around and follow. Units read the field belonging to
    /// their own <c>TargetGridPos</c>, so the cache must be able to hold one
    /// field per concurrently-active destination.
    /// </para>
    /// <para>
    /// Sizing follows the manifest caps (quality/content/mvp-v1.json,
    /// <c>capacity.flowFieldCacheEntryCap</c> = 32 and
    /// <c>capacity.flowFieldCacheMiBCap</c> = 8): the effective capacity is
    /// the entry cap, lowered on very large maps so the resident field bytes
    /// stay inside the MiB cap. Both caps are integer constants and the
    /// derivation is pure integer arithmetic on the map dimensions, so every
    /// host derives the identical capacity.
    /// </para>
    /// <para>
    /// Determinism contract: eviction picks the entry with the lowest
    /// <c>LastUsedTick</c>, ties broken by the lowest linear grid index of the
    /// destination. Destinations are unique inside the cache, so that pair is
    /// a total order and the victim never depends on slot layout, insertion
    /// history, hashing, wall-clock time or floating point. The entry serving
    /// the most recent request is never evicted, which keeps
    /// <see cref="MruField"/> stable for the caller that just requested it.
    /// </para>
    /// </summary>
    public sealed class FlowFieldCache
    {
        /// <summary>Manifest cap <c>capacity.flowFieldCacheEntryCap</c>.</summary>
        public const int MaxEntries = 32;

        /// <summary>Manifest cap <c>capacity.flowFieldCacheMiBCap</c>, in bytes.</summary>
        public const int MaxResidentBytes = 8 * 1024 * 1024;

        /// <summary><see cref="Direction2D"/> is a byte enum: one byte per cell.</summary>
        private const int BytesPerCell = 1;

        private struct Slot
        {
            public bool InUse;
            public GridPos2D Destination;
            public uint LastUsedTick;
            public FlowField Field;
        }

        private readonly ushort _width;
        private readonly ushort _height;
        private readonly Slot[] _slots;
        private int _count;
        private int _mruSlot;

        public FlowFieldCache(ushort width, ushort height)
        {
            if (width == 0 || height == 0)
            {
                throw new ArgumentException("FlowFieldCache dimensions must be greater than zero.");
            }

            _width = width;
            _height = height;
            _slots = new Slot[DeriveCapacity(width, height)];
            _count = 0;
            _mruSlot = -1;
        }

        /// <summary>
        /// Effective entry capacity: the manifest entry cap, reduced so the
        /// resident flow-field bytes never exceed the manifest MiB cap. Pure
        /// integer arithmetic; at least one entry is always available.
        /// </summary>
        public static int DeriveCapacity(ushort width, ushort height)
        {
            long bytesPerField = (long)width * height * BytesPerCell;
            long byBudget = MaxResidentBytes / Math.Max(1L, bytesPerField);
            if (byBudget < 1L) byBudget = 1L;
            return (int)Math.Min(MaxEntries, byBudget);
        }

        /// <summary>Number of destinations this cache can hold simultaneously.</summary>
        public int Capacity => _slots.Length;

        /// <summary>Number of currently cached destinations.</summary>
        public int Count => _count;

        /// <summary>True when at least one request was served since the last <see cref="Clear"/>.</summary>
        public bool HasMru => _mruSlot >= 0;

        /// <summary>Destination of the most recently requested field, or <see cref="GridPos2D.Invalid"/>.</summary>
        public GridPos2D MruDestination => _mruSlot >= 0 ? _slots[_mruSlot].Destination : GridPos2D.Invalid;

        /// <summary>Most recently requested field, or null when nothing was requested yet.</summary>
        public FlowField MruField => _mruSlot >= 0 ? _slots[_mruSlot].Field : null;

        /// <summary>Linear grid index used as the deterministic eviction tie-break.</summary>
        public int GridIndex(GridPos2D destination) => destination.Y * _width + destination.X;

        /// <summary>
        /// Pure lookup: returns the slot holding <paramref name="destination"/>
        /// or -1. Destinations are unique, so the result never depends on the
        /// scan order.
        /// </summary>
        public int FindSlot(GridPos2D destination)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].InUse && _slots[i].Destination == destination)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Pure lookup by destination; null on a miss. Mutates nothing.</summary>
        public FlowField TryGetField(GridPos2D destination)
        {
            int slot = FindSlot(destination);
            return slot >= 0 ? _slots[slot].Field : null;
        }

        /// <summary>Field of a slot returned by <see cref="FindSlot"/> or <see cref="Acquire"/>.</summary>
        public FlowField FieldAt(int slot) => _slots[slot].Field;

        /// <summary>Last-used tick of a slot (serialization and tests).</summary>
        public uint LastUsedTickAt(int slot) => _slots[slot].LastUsedTick;

        /// <summary>Destination of a slot (serialization and tests).</summary>
        public GridPos2D DestinationAt(int slot) => _slots[slot].Destination;

        /// <summary>True when the slot currently holds a cached field.</summary>
        public bool IsSlotInUse(int slot) => _slots[slot].InUse;

        /// <summary>
        /// Refreshes recency of an existing slot and makes it the MRU entry.
        /// </summary>
        public void Touch(int slot, uint tick)
        {
            _slots[slot].LastUsedTick = tick;
            _mruSlot = slot;
        }

        /// <summary>
        /// Returns the slot that shall hold <paramref name="destination"/>,
        /// evicting deterministically when the cache is full, and makes it the
        /// MRU entry. The returned slot always owns an allocated
        /// <see cref="FlowField"/>; its CONTENT is undefined until the caller
        /// generates into it. Generation deliberately stays with the caller
        /// (<see cref="PathfindingSystem.RequestFlowField"/>) so the headless
        /// performance harness keeps attributing pathfinding time correctly.
        /// </summary>
        public int Acquire(GridPos2D destination, uint tick)
        {
            int existing = FindSlot(destination);
            if (existing >= 0)
            {
                Touch(existing, tick);
                return existing;
            }

            int slot = FindFreeSlot();
            if (slot < 0)
            {
                slot = SelectEvictionVictim();
            }

            if (_slots[slot].Field == null)
            {
                _slots[slot].Field = new FlowField(_width, _height);
            }
            if (!_slots[slot].InUse)
            {
                _count++;
            }

            _slots[slot].InUse = true;
            _slots[slot].Destination = destination;
            _slots[slot].LastUsedTick = tick;
            _mruSlot = slot;
            return slot;
        }

        /// <summary>Marks the slot holding <paramref name="destination"/> as MRU (restore path).</summary>
        public bool TrySetMru(GridPos2D destination)
        {
            int slot = FindSlot(destination);
            if (slot < 0) return false;
            _mruSlot = slot;
            return true;
        }

        /// <summary>
        /// Drops every entry. The <see cref="FlowField"/> buffers stay
        /// allocated for reuse — they carry no state once the slot is free.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].InUse = false;
                _slots[i].Destination = GridPos2D.Invalid;
                _slots[i].LastUsedTick = 0;
            }
            _count = 0;
            _mruSlot = -1;
        }

        /// <summary>
        /// Copies the cached entries into the caller's buffers in canonical
        /// order (ascending linear grid index) and returns the entry count.
        /// The canonical order makes the serialized block independent of slot
        /// layout, so two hosts that reached the same cache contents by
        /// different insertion histories write byte-identical blocks.
        /// </summary>
        public int CopyCanonicalEntries(GridPos2D[] destinations, uint[] lastUsedTicks)
        {
            if (destinations == null) throw new ArgumentNullException(nameof(destinations));
            if (lastUsedTicks == null) throw new ArgumentNullException(nameof(lastUsedTicks));
            if (destinations.Length < _count || lastUsedTicks.Length < _count)
            {
                throw new ArgumentException("Destination buffers are too small for the cached entry count.");
            }

            int written = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].InUse) continue;

                // Insertion sort by grid index: bounded by 32 entries and
                // deterministic (grid indices are unique inside the cache).
                GridPos2D destination = _slots[i].Destination;
                uint tick = _slots[i].LastUsedTick;
                int index = GridIndex(destination);

                int insertAt = written;
                while (insertAt > 0 && GridIndex(destinations[insertAt - 1]) > index)
                {
                    destinations[insertAt] = destinations[insertAt - 1];
                    lastUsedTicks[insertAt] = lastUsedTicks[insertAt - 1];
                    insertAt--;
                }
                destinations[insertAt] = destination;
                lastUsedTicks[insertAt] = tick;
                written++;
            }
            return written;
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].InUse) return i;
            }
            return -1;
        }

        /// <summary>
        /// Lowest last-used tick wins, ties broken by the lowest linear grid
        /// index. The MRU entry is protected while another candidate exists,
        /// so the field a caller just requested cannot vanish underneath it.
        /// </summary>
        private int SelectEvictionVictim()
        {
            int victim = -1;
            uint bestTick = uint.MaxValue;
            int bestIndex = int.MaxValue;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].InUse || i == _mruSlot) continue;

                uint tick = _slots[i].LastUsedTick;
                int index = GridIndex(_slots[i].Destination);
                if (tick < bestTick || (tick == bestTick && index < bestIndex))
                {
                    bestTick = tick;
                    bestIndex = index;
                    victim = i;
                }
            }

            // Only reachable at capacity 1, where the MRU entry is the sole
            // candidate and must give way.
            return victim >= 0 ? victim : _mruSlot;
        }
    }
}
