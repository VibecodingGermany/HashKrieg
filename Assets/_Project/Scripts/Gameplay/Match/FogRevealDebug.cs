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
    /// <para>
    /// TWO LOCKS, and both are load-bearing. <b>Release builds:</b> the whole
    /// switch compiles out — <see cref="RevealAll"/> is a constant false, so a
    /// shipped client cannot reveal anything no matter what it presses.
    /// <b>Relay matches:</b> the reveal is refused even in a development build.
    /// It is worth being precise about why, because the class remarks above are
    /// only true for someone watching with their hands off the mouse:
    /// <c>RtsDeviceInput.TryPickUnit</c> has no fog filter and the command
    /// validation checks no visibility, so a revealed map converts directly
    /// into a legal AttackTarget order on a hidden unit — which goes over the
    /// relay and into the state hash. Against a human opponent this is a
    /// maphack, not a diagnostic.
    /// </para>
    /// <para>
    /// BEFORE THE FIRST PUBLIC BUILD: delete this class, <see cref="MatchSpeedDebug"/>,
    /// their F4/F5 keys and panel buttons in <c>DebugHud</c>, and the four
    /// presentation branches that read <see cref="RevealAll"/>. The build gate
    /// below makes them inert, not absent, and inert debug code in a shipped
    /// binary is a standing invitation. The gate buys time; it is not the fix.
    /// </para>
    /// </summary>
    public static class FogRevealDebug
    {
        /// <summary>
        /// True while a relay match is running. Set once per match by
        /// <c>MatchRunner.InitializeMatch</c> — the switch is process-wide,
        /// a match is not.
        /// </summary>
        public static bool RelayMatch { get; private set; }

        private static bool _revealAll;

        /// <summary>True while the presentation draws every entity and clear ground.</summary>
        public static bool RevealAll
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            get => _revealAll && !RelayMatch;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Flips the reveal. Refused in a relay match, and compiled to a
        /// no-op in a release build.
        /// </summary>
        public static void Toggle()
        {
            if (RelayMatch)
            {
                _revealAll = false;
                return;
            }
            _revealAll = !_revealAll;
        }

        /// <summary>
        /// Clears the reveal and records the match kind. Called at match start
        /// so the flag cannot ride along from a previous skirmish into a relay
        /// match the player never pressed the key in.
        /// </summary>
        public static void ResetForMatch(bool relayMatch)
        {
            _revealAll = false;
            RelayMatch = relayMatch;
        }

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
