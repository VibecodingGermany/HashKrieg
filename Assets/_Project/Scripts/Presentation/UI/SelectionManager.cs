using System;
using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Presentation manager handling RTS unit selection (single click & drag box bounds).
    /// </summary>
    public sealed class SelectionManager
    {
        public const int MaxSelectedEntities = 64;

        private readonly EntityId[] _selectedIds;
        private int _selectedCount;

        public int SelectedCount => _selectedCount;
        public ReadOnlySpan<EntityId> SelectedEntities => _selectedIds.AsSpan(0, _selectedCount);

        public SelectionManager()
        {
            _selectedIds = new EntityId[MaxSelectedEntities];
        }

        public void ClearSelection()
        {
            _selectedCount = 0;
        }

        public bool SelectSingle(EntityId id)
        {
            ClearSelection();
            if (!id.IsValid) return false;

            _selectedIds[0] = id;
            _selectedCount = 1;
            return true;
        }

        public int SelectBox(EntityManager entityManager, byte playerId, float minX, float minY, float maxX, float maxY)
        {
            ClearSelection();
            if (entityManager == null) return 0;

            UnitState[] rawUnits = entityManager.RawUnits;
            int capacity = entityManager.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref rawUnits[i];
                if (!u.IsActive || u.PlayerId != playerId) continue;

                float px = u.Transform.PositionX;
                float py = u.Transform.PositionY;

                if (px >= minX && px <= maxX && py >= minY && py <= maxY)
                {
                    if (_selectedCount < MaxSelectedEntities)
                    {
                        _selectedIds[_selectedCount++] = u.Id;
                    }
                }
            }

            return _selectedCount;
        }
    }
}
