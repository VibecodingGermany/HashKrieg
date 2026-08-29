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

        /// <summary>Control-group slots: digits 1..9 are valid; index 0 stays unused so the key IS the index.</summary>
        public const int ControlGroupCount = 10;

        private readonly EntityId[] _selectedIds;
        private int _selectedCount;

        // Control groups 1-9 (sprint 09 §7): index 0 stays unused so the
        // digit key IS the index. Pure UI state — no sim contact.
        private readonly EntityId[][] _groups = new EntityId[ControlGroupCount][];
        private readonly int[] _groupCounts = new int[ControlGroupCount];

        public int SelectedCount => _selectedCount;
        public ReadOnlySpan<EntityId> SelectedEntities => _selectedIds.AsSpan(0, _selectedCount);

        /// <summary>
        /// The selected Aetherium field (21.2, #86), 0 = none. Fields are not
        /// entities, so their id lives BESIDE the entity list, never inside
        /// it — same UI-only character, no simulation contact.
        /// </summary>
        public ushort SelectedFieldId { get; private set; }

        public SelectionManager()
        {
            _selectedIds = new EntityId[MaxSelectedEntities];
            for (int i = 0; i < ControlGroupCount; i++)
            {
                _groups[i] = new EntityId[MaxSelectedEntities];
            }
        }

        public void ClearSelection()
        {
            _selectedCount = 0;
            SelectedFieldId = 0;
        }

        /// <summary>Selects a field for its reserve readout; clears the entity selection (a field takes no entity orders).</summary>
        public void SelectField(ushort fieldId)
        {
            ClearSelection();
            SelectedFieldId = fieldId;
        }

        public bool SelectSingle(EntityId id)
        {
            ClearSelection();
            if (!id.IsValid) return false;

            _selectedIds[0] = id;
            _selectedCount = 1;
            return true;
        }

        /// <summary>Shift-click: adds the entity to the selection (no-op when already selected or full).</summary>
        public bool AddSingle(EntityId id)
        {
            if (!id.IsValid || _selectedCount >= MaxSelectedEntities) return false;
            for (int i = 0; i < _selectedCount; i++)
            {
                if (_selectedIds[i] == id) return false;
            }
            // An entity joining the selection ends any field selection.
            SelectedFieldId = 0;
            _selectedIds[_selectedCount++] = id;
            return true;
        }

        public int SelectBox(EntityManager entityManager, byte playerId, float minX, float minY, float maxX, float maxY)
        {
            ClearSelection();
            return SelectBoxAdditive(entityManager, playerId, minX, minY, maxX, maxY);
        }

        /// <summary>Shift-drag: the union of the current selection and the box contents (deduped, capped).</summary>
        public int SelectBoxAdditive(EntityManager entityManager, byte playerId, float minX, float minY, float maxX, float maxY)
        {
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
                    AddSingle(u.Id);
                }
            }

            return _selectedCount;
        }

        /// <summary>
        /// The command card's breakdown-row click (21.5 rows, made clickable
        /// in sprint 22 for #50): reduces the selection to its entities of
        /// <paramref name="role"/>, preserving the selection's own order (it
        /// is stable, so the lead of the reduced selection is the first
        /// selected entity of that role — the same first-occurrence rule the
        /// rows are drawn in). Stale handles are dropped against the live
        /// store, the same courtesy <see cref="RecallControlGroup"/> applies
        /// at recall. Returns the surviving count; 0 means the selection is
        /// empty (the row's units died between model build and click).
        /// </summary>
        public int RetainRole(EntityManager entityManager, UnitRole role)
        {
            if (entityManager == null || _selectedCount == 0) return _selectedCount;

            int kept = 0;
            for (int i = 0; i < _selectedCount; i++)
            {
                EntityId id = _selectedIds[i];
                if (!entityManager.TryGetUnit(id, out UnitState unit) || unit.Role != role) continue;
                _selectedIds[kept++] = id;
            }
            _selectedCount = kept;
            return _selectedCount;
        }

        /// <summary>
        /// Replaces the selection with <paramref name="ids"/> (deduped and
        /// capped through <see cref="AddSingle"/>, field selection cleared).
        /// The double-click role select's replace half (sprint 22, #50);
        /// store-less like <see cref="SelectSingle"/> — the caller vouches
        /// for the ids, staleness is filtered on use. Returns the new count.
        /// </summary>
        public int ReplaceSelection(ReadOnlySpan<EntityId> ids)
        {
            ClearSelection();
            return AddRange(ids);
        }

        /// <summary>
        /// Adds every id of <paramref name="ids"/> to the selection (deduped,
        /// capped, field selection cleared) — the double-click role select's
        /// Shift half, the same additive discipline Shift-click and
        /// Shift-drag already follow (sprint 09 §7). Returns the new count.
        /// </summary>
        public int AddRange(ReadOnlySpan<EntityId> ids)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                AddSingle(ids[i]);
            }
            return _selectedCount;
        }

        // ------------------------------------------------------------------
        // Control groups (sprint 09 §7)
        // ------------------------------------------------------------------

        /// <summary>Stores the current selection under group <paramref name="slot"/> (1..9); an empty selection clears the group.</summary>
        public void SaveControlGroup(int slot)
        {
            if (slot < 1 || slot >= ControlGroupCount) return;
            for (int i = 0; i < _selectedCount; i++)
            {
                _groups[slot][i] = _selectedIds[i];
            }
            _groupCounts[slot] = _selectedCount;
        }

        public bool HasControlGroup(int slot)
        {
            return slot >= 1 && slot < ControlGroupCount && _groupCounts[slot] > 0;
        }

        /// <summary>
        /// Replaces the selection with the group's survivors: dead or foreign
        /// ids are dropped here (staleness is checked against the live entity
        /// store at recall time), and the group itself is compacted in place.
        /// Returns the recalled count.
        /// </summary>
        public int RecallControlGroup(int slot, EntityManager entityManager, byte playerId)
        {
            ClearSelection();
            if (!HasControlGroup(slot) || entityManager == null) return 0;

            int stored = _groupCounts[slot];
            int alive = 0;
            for (int i = 0; i < stored; i++)
            {
                EntityId id = _groups[slot][i];
                if (!entityManager.TryGetUnit(id, out UnitState unit) || unit.PlayerId != playerId) continue;
                _groups[slot][alive++] = id;
                AddSingle(id);
            }
            _groupCounts[slot] = alive;
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
