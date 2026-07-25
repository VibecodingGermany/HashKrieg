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
    }
}
