// Diagnostic aid, not gameplay. It changes what the screen shows and what the
// clock hands to the tick loop, never what the simulation computes — see the
// class remarks for the exact boundary and how it is kept.
using System.Collections.Generic;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// A presentation-only reveal of the whole map, toggled from the F3 debug
    /// panel. It exists for one purpose: watching what the skirmish AI does
    /// while it does it. Through the fog one only ever judges the few seconds
    /// of the opponent that happen inside one's own sight radius — the march
    /// route, the staging, the moment a damaged unit turns around all happen
    /// where nobody can see them.
    /// <para>
    /// WHAT THIS DOES NOT TOUCH: <see cref="Nova.Simulation.Vision.FogOfWarSystem"/>
    /// keeps computing and committing exactly the same team views, and the AI
    /// keeps reading its own through <c>GetVisibleEntities</c>. Only the four
    /// presentation consumers of that feed (unit views, fog overlay, minimap,
    /// health bars) are told to draw from the entity store instead. Switching
    /// this on therefore changes what the screen shows and nothing about what
    /// the simulation computes — the run stays bit-identical. That is the only
    /// reason a debug view is allowed to see through the fog at all: an
    /// observation is worthless if observing it changed the thing observed.
    /// </para>
    /// <para>
    /// It is a static flag because five components across two assemblies read
    /// it and a lab-only switch is not worth a wiring change in the scene
    /// generator. The reveal survives closing the panel (looking at an
    /// uncluttered map is the point) but not a restart of the player.
    /// </para>
    /// </summary>
    public static class FogRevealDebug
    {
        /// <summary>True while the presentation draws every entity and clear ground.</summary>
        public static bool RevealAll { get; private set; }

        public static void Toggle() => RevealAll = !RevealAll;

        /// <summary>
        /// Every active entity in ascending entity-index order, as a drop-in
        /// for <see cref="Nova.Simulation.Vision.FogOfWarSystem.GetVisibleEntities"/>
        /// — same order, same append-without-clearing contract, minus the
        /// visibility filter. Read-only over the entity store.
        /// </summary>
        public static void CollectAllActive(EntityManager entities, List<EntityId> results)
        {
            if (entities == null || results == null) return;

            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive) continue;
                results.Add(u.Id);
            }
        }
    }
}
