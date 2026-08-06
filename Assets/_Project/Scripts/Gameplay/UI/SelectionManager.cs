using System;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Gameplay
{
    /// <summary>
    /// Gameplay-side tracker for RTS unit selection state (single click & drag box bounds).
    /// UI-only state, no simulation mutation. Placement in Nova.Gameplay is the conscious
    /// G0-B interim until Nova.UI exists (see Architecture.md section 2).
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

                // Presentation-side boundary conversion (selection is UI, not sim).
                float px = u.Transform.PositionX.ToFloat();
                float py = u.Transform.PositionY.ToFloat();

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

        /// <summary>
        /// Copies the selected ids of MOBILE entities — everything that is not
        /// a building role — into <paramref name="destination"/> and returns
        /// the count written (capped at the destination length). Buildings are
        /// immobile, so unit orders (Move, Stop, Attack, Harvest, ReturnCargo)
        /// addressed to them are meaningless; the device input filters every
        /// such dispatch through this method. Stale ids (dead or despawned
        /// entities) are dropped as well, mirroring what the dispatcher's
        /// state view would do one layer later.
        /// </summary>
        public int CopyMobileSelection(EntityManager entityManager, EntityId[] destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (entityManager == null || _selectedCount == 0) return 0;

            int written = 0;
            for (int i = 0; i < _selectedCount && written < destination.Length; i++)
            {
                if (!entityManager.TryGetUnit(_selectedIds[i], out UnitState unit)) continue;
                if (SimDefinitions.IsBuildingRole(unit.Role)) continue;
                destination[written++] = _selectedIds[i];
            }
            return written;
        }
    }
}
