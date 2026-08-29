using System;
using Nova.Core;
using Nova.Simulation.Construction;
using Nova.Simulation.State;

namespace Nova.Gameplay
{
    /// <summary>
    /// The "next idle Builder" query behind the I key (sprint 22, #50 — the
    /// beta report's build-flow break: the player lost the Builder in a clump
    /// and could not build). Pure, Unity-free and allocation-free on purpose,
    /// the same precedent as <see cref="CommandCardPresenter"/>: the whole
    /// predicate and the cycling order are EditMode-tested here, the device
    /// input only wires the key and the camera.
    /// <para>
    /// "IDLE" AS READ FROM THE CODE, not from intuition — a Builder is idle
    /// when he carries NONE of the standing-order markers the sim knows. The
    /// entity store carries four, and the Stop command's clearing list
    /// (<see cref="UnitCommandStateView"/>, Stop case) is the sim's own
    /// enumeration of them: <see cref="UnitState.IsMoving"/> (a movement
    /// order; set by SetTarget, cleared by arrival and by Stop),
    /// <see cref="UnitState.AttackTarget"/> (an attack order),
    /// <see cref="UnitState.HarvestFieldId"/> and
    /// <see cref="UnitState.IsReturningCargo"/> (the two economy orders — a
    /// Builder never legitimately holds these, but the predicate checks them
    /// anyway: the view's Apply does not role-filter, so a state that
    /// should not exist must still read as busy, never as free labour). The
    /// fifth marker lives construction-side: a site's
    /// <c>AssignedBuilderRaw</c> — the builder a site holds is building (or
    /// is expected to walk there), readable per site via
    /// <see cref="ConstructionSystem.TryGetSite"/> and collected here by
    /// <see cref="CollectAssignedBuilderRaws"/>.
    /// </para>
    /// <para>
    /// KNOWN BLIND SPOT, reported instead of approximated: the sixth marker,
    /// a standing REPAIR order, is not observable from this layer. It lives
    /// in ConstructionSystem's private repair table; the only public surface
    /// is write-side (AssignRepairOrder/ClearRepairOrder) plus a global
    /// capacity count, and the repair tick mutates the TARGET's health, never
    /// the Builder's UnitState. Closing this needs a one-line public reader
    /// inside Simulation/** — out of scope for #50, which is selection and
    /// presentation only. So a Builder mid-repair with his feet still reads
    /// as idle here. That is a deliberate, documented wrong answer in exactly
    /// one occasional state, not a guess: the predicate is shaped so the
    /// future repair read slots in as one more clause, and the report names
    /// the seam.
    /// </para>
    /// <para>
    /// CYCLING: <see cref="TryFindNextIdleBuilder"/> walks entity indices
    /// ascending — the entity store's own order, stable and reproducible —
    /// starting strictly after the previously returned index and wrapping
    /// once, so repeated presses tour every idle Builder in a deterministic
    /// round. A Builder who is the only idle one is returned every press.
    /// </para>
    /// </summary>
    public static class IdleBuilderQuery
    {
        /// <summary>
        /// The entity-store half of idle: no movement, attack or economy
        /// order on the unit itself. Construction-side markers (site
        /// assignment) are NOT visible here — they arrive as
        /// <paramref name="assignedBuilderRaws"/> in
        /// <see cref="IsIdleBuilder"/>; the repair blind spot is documented
        /// on the class.
        /// </summary>
        public static bool HasNoEntitySideOrder(in UnitState unit)
        {
            return !unit.IsMoving
                && !unit.AttackTarget.IsValid
                && unit.HarvestFieldId == 0
                && !unit.IsReturningCargo;
        }

        /// <summary>
        /// Collects the <c>AssignedBuilderRaw</c> of every active
        /// construction site into <paramref name="destination"/> and returns
        /// the count written (capped at the destination length; sized to
        /// <see cref="ConstructionSystem.MaxSites"/> nothing is ever dropped).
        /// Sites of ANY owner are collected: the membership test in
        /// <see cref="IsIdleBuilder"/> only ever runs against the local
        /// player's Builders, and a raw id (index + version) cannot alias a
        /// different living entity, so a foreign site's row can never mark an
        /// own Builder busy. One read pass per key press — the entity scan
        /// asks <see cref="ConstructionSystem.TryGetSite"/> of every active
        /// entity, and a non-site misses the site register immediately.
        /// </summary>
        public static int CollectAssignedBuilderRaws(
            EntityManager entities, ConstructionSystem construction, uint[] destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (entities == null || construction == null) return 0;

            int written = 0;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity && written < destination.Length; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive) continue;
                if (construction.TryGetSite(
                        UnitCommandStateView.ToRawEntityId(unit.Id),
                        out _, out _, out uint assignedBuilderRaw)
                    && assignedBuilderRaw != 0)
                {
                    destination[written++] = assignedBuilderRaw;
                }
            }
            return written;
        }

        /// <summary>
        /// The full idle predicate for one unit: an ACTIVE Builder of
        /// <paramref name="playerSlot"/> with no entity-side order
        /// (<see cref="HasNoEntitySideOrder"/>) and no site assignment in
        /// <paramref name="assignedBuilderRaws"/> (as collected by
        /// <see cref="CollectAssignedBuilderRaws"/>). The unobservable
        /// standing repair order is the documented exception (class remarks).
        /// </summary>
        public static bool IsIdleBuilder(
            in UnitState unit, byte playerSlot, ReadOnlySpan<uint> assignedBuilderRaws)
        {
            if (!unit.IsActive || unit.Role != UnitRole.Builder || unit.PlayerId != playerSlot)
            {
                return false;
            }
            if (!HasNoEntitySideOrder(in unit)) return false;

            uint raw = UnitCommandStateView.ToRawEntityId(unit.Id);
            for (int i = 0; i < assignedBuilderRaws.Length; i++)
            {
                if (assignedBuilderRaws[i] == raw) return false; // a site holds his Bauauftrag
            }
            return true;
        }

        /// <summary>
        /// The next idle Builder strictly after <paramref name="afterIndex"/>
        /// in ascending entity-index order, wrapping once to index 0 when the
        /// tail holds none — the deterministic round the I key tours. Pass -1
        /// to start at the lowest-index idle Builder.
        /// <paramref name="assignedScratch"/> is the caller's reusable buffer
        /// for <see cref="CollectAssignedBuilderRaws"/> (sized
        /// <see cref="ConstructionSystem.MaxSites"/>), so the per-press path
        /// stays allocation-free.
        /// </summary>
        public static bool TryFindNextIdleBuilder(
            EntityManager entities, ConstructionSystem construction, byte playerSlot,
            int afterIndex, uint[] assignedScratch, out EntityId builder)
        {
            builder = EntityId.Invalid;
            if (entities == null || construction == null || assignedScratch == null) return false;

            int assignedCount = CollectAssignedBuilderRaws(entities, construction, assignedScratch);
            ReadOnlySpan<uint> assigned = assignedScratch.AsSpan(0, assignedCount);

            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            int start = afterIndex < -1 ? -1 : afterIndex;

            // Two ascending passes — after start, then from 0 up to start —
            // so the tour is a strict index cycle with a single wrap.
            for (int i = start + 1; i < capacity; i++)
            {
                if (IsIdleBuilder(in units[i], playerSlot, assigned))
                {
                    builder = units[i].Id;
                    return true;
                }
            }
            for (int i = 0; i <= start && i < capacity; i++)
            {
                if (IsIdleBuilder(in units[i], playerSlot, assigned))
                {
                    builder = units[i].Id;
                    return true;
                }
            }
            return false;
        }
    }
}
