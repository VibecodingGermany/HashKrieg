using System;
using System.Collections.Generic;
using Nova.Core;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Contiguous, pre-allocated storage for simulation entities.
    /// Uses index-based slot recycling via a free-list to guarantee zero GC allocations during gameplay.
    /// </summary>
    public sealed class EntityManager
    {
        private readonly UnitState[] _units;
        private readonly ushort[] _versions;
        private readonly Stack<int> _freeSlots;

        public int Capacity { get; }
        public int ActiveCount { get; private set; }

        public UnitState[] RawUnits => _units;

        public EntityManager(int capacity = 2048)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
            }

            Capacity = capacity;
            _units = new UnitState[capacity];
            _versions = new ushort[capacity];
            _freeSlots = new Stack<int>(capacity);

            // Populate free slots in reverse order so slot 0 is assigned first
            for (int i = capacity - 1; i >= 0; i--)
            {
                _freeSlots.Push(i);
                _versions[i] = 1;
            }
        }

        public bool IsValid(EntityId id)
        {
            if (!id.IsValid || id.Index < 0 || id.Index >= Capacity) return false;
            return _versions[id.Index] == id.Version && _units[id.Index].IsActive;
        }

        public EntityId SpawnUnit(byte playerId, Transform2D initialTransform, float moveSpeed, float radius = 0.5f, int maxHealth = 100)
        {
            if (_freeSlots.Count == 0)
            {
                throw new InvalidOperationException($"EntityManager capacity exceeded ({Capacity} entities max).");
            }

            int index = _freeSlots.Pop();
            ushort version = _versions[index];
            var id = new EntityId(index, version);

            _units[index] = new UnitState(id, playerId, initialTransform, moveSpeed, radius, maxHealth);
            ActiveCount++;

            return id;
        }

        public bool DespawnUnit(EntityId id)
        {
            if (!IsValid(id)) return false;

            int index = id.Index;
            _units[index].IsActive = false;
            _units[index].Stop();

            // Increment version to invalidate old handle references
            _versions[index]++;
            _freeSlots.Push(index);
            ActiveCount--;

            return true;
        }

        public ref UnitState GetUnitRef(EntityId id)
        {
            if (!IsValid(id))
            {
                throw new ArgumentException($"Invalid EntityId {id}.", nameof(id));
            }

            return ref _units[id.Index];
        }

        public bool TryGetUnit(EntityId id, out UnitState unit)
        {
            if (IsValid(id))
            {
                unit = _units[id.Index];
                return true;
            }

            unit = default;
            return false;
        }

        public void Clear()
        {
            Array.Clear(_units, 0, Capacity);
            _freeSlots.Clear();
            ActiveCount = 0;

            for (int i = Capacity - 1; i >= 0; i--)
            {
                _versions[i]++;
                _freeSlots.Push(i);
            }
        }

        /// <summary>
        /// Serialization version of the entity store snapshot block.
        /// </summary>
        public const byte StateVersion = 1;

        /// <summary>
        /// Writes the complete authoritative entity store state as canonical
        /// little-endian block content (docs/tech/SimulationCore.md section 3:
        /// entity allocator, free list, generations and unit state). Slot
        /// order is ascending index; the free list is written in stack
        /// enumeration order (top first) so a restore reproduces the exact
        /// same future id assignment sequence.
        /// <para>
        /// Positions, speeds and radii are still IEEE-754 floats of the
        /// prototype movement scaffolding; they serialize as their exact
        /// little-endian bit patterns so hash and snapshot stay bit-exact
        /// until the fixed-point migration replaces the storage type.
        /// </para>
        /// </summary>
        public void WriteState(Snapshots.SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);
            writer.WriteUInt32(unchecked((uint)Capacity));
            writer.WriteUInt32(unchecked((uint)ActiveCount));

            for (int i = 0; i < Capacity; i++)
            {
                writer.WriteUInt16(_versions[i]);
            }

            writer.WriteUInt32(unchecked((uint)_freeSlots.Count));
            foreach (int slot in _freeSlots)
            {
                writer.WriteInt32(slot);
            }

            for (int i = 0; i < Capacity; i++)
            {
                ref readonly UnitState u = ref _units[i];
                writer.WriteUInt8(u.IsActive ? (byte)1 : (byte)0);
                if (!u.IsActive) continue;

                writer.WriteEntityId(u.Id);
                writer.WriteUInt8(u.PlayerId);
                writer.WriteUInt32(SimMath.SingleToUInt32Bits(u.Transform.PositionX));
                writer.WriteUInt32(SimMath.SingleToUInt32Bits(u.Transform.PositionY));
                writer.WriteUInt32(SimMath.SingleToUInt32Bits(u.Transform.Rotation));
                writer.WriteUInt32(SimMath.SingleToUInt32Bits(u.MoveSpeed));
                writer.WriteUInt32(SimMath.SingleToUInt32Bits(u.Radius));
                writer.WriteUInt16(u.TargetGridPos.X);
                writer.WriteUInt16(u.TargetGridPos.Y);
                writer.WriteInt32(u.CurrentHealth);
                writer.WriteInt32(u.MaxHealth);
                writer.WriteEntityId(u.AttackTarget);
                writer.WriteInt32(u.WeaponCooldownTicks);
                writer.WriteUInt8(u.IsMoving ? (byte)1 : (byte)0);
            }
        }

        /// <summary>
        /// Fully parses and validates entity store block content produced by
        /// <see cref="WriteState"/> — every length, index and store
        /// invariant — without mutating this manager in any way.
        /// </summary>
        public bool TryValidateState(ReadOnlySpan<byte> content)
        {
            return TryParseState(content, out _);
        }

        /// <summary>
        /// Restores the entity store from block content produced by
        /// <see cref="WriteState"/>. Every length and index is validated
        /// before anything is allocated or mutated; the store invariants
        /// (active count matches the active flags, the free list contains
        /// exactly the inactive slots without duplicates, unit handles match
        /// their slot and generation) are fully checked. Malformed input
        /// returns false and leaves this manager untouched.
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> content)
        {
            if (!TryParseState(content, out ParsedEntityStore parsed)) return false;

            // All checks passed; commit atomically.
            Array.Copy(parsed.Units, _units, Capacity);
            Array.Copy(parsed.Versions, _versions, Capacity);
            _freeSlots.Clear();
            // Serialized order is stack enumeration order (top first); pushing
            // in reverse restores an identical stack, so the next id
            // assignment sequence matches the serialized host exactly.
            for (int i = parsed.FreeSlots.Length - 1; i >= 0; i--)
            {
                _freeSlots.Push(parsed.FreeSlots[i]);
            }
            ActiveCount = parsed.ActiveCount;
            return true;
        }

        /// <summary>Validated intermediate of a parsed entity store block.</summary>
        private sealed class ParsedEntityStore
        {
            public ushort[] Versions;
            public int[] FreeSlots;
            public UnitState[] Units;
            public int ActiveCount;
        }

        /// <summary>
        /// Parses and fully validates block content into a committed-ready
        /// intermediate. Never mutates this manager.
        /// </summary>
        private bool TryParseState(ReadOnlySpan<byte> content, out ParsedEntityStore parsed)
        {
            parsed = null;
            var reader = new Snapshots.SnapshotBlockReader(content);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt32(out uint capacity)) return false;
            if (capacity != (uint)Capacity) return false;
            if (!reader.TryReadUInt32(out uint activeCount) || activeCount > capacity) return false;

            var versions = new ushort[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                if (!reader.TryReadUInt16(out versions[i])) return false;
            }

            if (!reader.TryReadUInt32(out uint freeCount) || freeCount > capacity) return false;
            var freeSlots = new int[freeCount];
            var seenFree = new bool[Capacity];
            for (int i = 0; i < freeSlots.Length; i++)
            {
                if (!reader.TryReadInt32(out freeSlots[i])) return false;
                if (freeSlots[i] < 0 || freeSlots[i] >= Capacity) return false;
                if (seenFree[freeSlots[i]]) return false; // duplicate free-list entry
                seenFree[freeSlots[i]] = true;
            }

            var units = new UnitState[Capacity];
            int observedActive = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (!reader.TryReadUInt8(out byte activeFlag) || activeFlag > 1) return false;
                bool isActive = activeFlag == 1;

                // Store invariant: the free list holds exactly the inactive slots.
                if (isActive == seenFree[i]) return false;

                if (!isActive) continue;
                observedActive++;

                if (!reader.TryReadEntityId(out EntityId id)) return false;
                if (!reader.TryReadUInt8(out byte playerId)) return false;
                if (!reader.TryReadUInt32(out uint posX)) return false;
                if (!reader.TryReadUInt32(out uint posY)) return false;
                if (!reader.TryReadUInt32(out uint rotation)) return false;
                if (!reader.TryReadUInt32(out uint moveSpeed)) return false;
                if (!reader.TryReadUInt32(out uint radius)) return false;
                if (!reader.TryReadUInt16(out ushort targetX)) return false;
                if (!reader.TryReadUInt16(out ushort targetY)) return false;
                if (!reader.TryReadInt32(out int currentHealth)) return false;
                if (!reader.TryReadInt32(out int maxHealth)) return false;
                if (!reader.TryReadEntityId(out EntityId attackTarget)) return false;
                if (!reader.TryReadInt32(out int weaponCooldown)) return false;
                if (!reader.TryReadUInt8(out byte movingFlag) || movingFlag > 1) return false;

                // The unit handle must match its slot and the slot generation.
                if (id.Index != i || id.Version != versions[i] || versions[i] == 0) return false;

                units[i] = new UnitState
                {
                    Id = id,
                    PlayerId = playerId,
                    Transform = new Transform2D(
                        SimMath.UInt32BitsToSingle(posX),
                        SimMath.UInt32BitsToSingle(posY),
                        SimMath.UInt32BitsToSingle(rotation)),
                    MoveSpeed = SimMath.UInt32BitsToSingle(moveSpeed),
                    Radius = SimMath.UInt32BitsToSingle(radius),
                    TargetGridPos = new Pathfinding.GridPos2D(targetX, targetY),
                    CurrentHealth = currentHealth,
                    MaxHealth = maxHealth,
                    AttackTarget = attackTarget,
                    WeaponCooldownTicks = weaponCooldown,
                    IsActive = true,
                    IsMoving = movingFlag == 1
                };
            }
            if (observedActive != (int)activeCount) return false;
            if (reader.Remaining != 0) return false;

            parsed = new ParsedEntityStore
            {
                Versions = versions,
                FreeSlots = freeSlots,
                Units = units,
                ActiveCount = observedActive
            };
            return true;
        }
    }
}
