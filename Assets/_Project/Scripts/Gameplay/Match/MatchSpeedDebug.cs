// Diagnostic aid, not gameplay. It changes what the screen shows and what the
// clock hands to the tick loop, never what the simulation computes — see the
// class remarks for the exact boundary and how it is kept.
namespace Nova.Gameplay.Match
{
    /// <summary>
    /// A wall-clock fast-forward for the local match, toggled from the F3
    /// debug panel. It exists for one purpose: judging AI behaviour without
    /// watching it in real time. A skirmish decides around tick 6.000, which
    /// is ten minutes of sitting still — and the parts worth watching (the
    /// wave gathering, the march, the moment a wounded unit turns around) are
    /// minutes apart.
    /// <para>
    /// WHAT THIS DOES NOT TOUCH: the simulation runs at the canonical 10 Hz,
    /// tick for tick, exactly as it always does. The only thing that changes
    /// is how much wall-clock time <c>MatchRunner</c> hands to its fixed-tick
    /// accumulator per frame, so at 4x it consumes four ticks in the time it
    /// used to consume one. No <c>deltaTime</c> reaches the simulation, no
    /// tick is skipped, no tick is executed twice; a match watched at 10x ends
    /// on the same tick with the same state hash as one watched at 1x. That is
    /// the only reason a debug view is allowed to touch the clock at all: an
    /// observation is worthless if observing it changed the thing observed.
    /// </para>
    /// <para>
    /// IGNORED IN A RELAY MATCH, and not as a courtesy. Two peers stepping at
    /// different wall-clock rates would sit in the lockstep barrier waiting
    /// for each other; the fast one gains nothing and the slow one is stalled
    /// by a debug key. The refusal lives in <see cref="Multiplier"/> itself
    /// rather than at the one call site in <c>MatchRunner</c>: with the guard
    /// outside, the key handler and the status line kept cycling and kept
    /// claiming "4x SPEED" in a relay match that ran in real time — a label
    /// that lies is worse than no label, and any future second tick source
    /// would have silently lost the protection.
    /// </para>
    /// <para>
    /// BEFORE THE FIRST PUBLIC BUILD: goes out together with
    /// <see cref="FogRevealDebug"/> — see the removal note there. The build
    /// gate below makes this inert in a release build, not absent.
    /// </para>
    /// <para>
    /// A static like <see cref="FogRevealDebug"/> next door, for the same
    /// reason: a lab-only switch read by two components is not worth a wiring
    /// change in the scene generator.
    /// </para>
    /// </summary>
    public static class MatchSpeedDebug
    {
        /// <summary>
        /// The speeds the panel cycles through. Ten is the top on purpose:
        /// above that a 10 Hz match outruns what an eye can follow, and the
        /// point of watching is to see something.
        /// </summary>
        public static readonly int[] Steps = { 1, 2, 4, 10 };

        /// <summary>
        /// True while a relay match is running. Set once per match by
        /// <c>MatchRunner.InitializeMatch</c> — the switch is process-wide,
        /// a match is not.
        /// </summary>
        public static bool RelayMatch { get; private set; }

        private static int _index;

        /// <summary>Wall-clock ticks per canonical tick. 1 is the shipped speed.</summary>
        public static int Multiplier
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            get => RelayMatch ? 1 : Steps[_index];
#else
            get => 1;
#endif
        }

        /// <summary>True while the match is not running at the shipped speed — the screen has to say so.</summary>
        public static bool IsFastForwarding => Multiplier != 1;

        /// <summary>
        /// Next speed in the ring, wrapping back to 1x. Refused in a relay
        /// match, so the panel cannot show a speed the clock never ran at.
        /// </summary>
        public static void Cycle()
        {
            if (RelayMatch)
            {
                _index = 0;
                return;
            }
            _index = (_index + 1) % Steps.Length;
        }

        /// <summary>
        /// Back to 1x and records the match kind. Called at match start so a
        /// speed set in a skirmish cannot ride along into a relay match.
        /// </summary>
        public static void ResetForMatch(bool relayMatch)
        {
            _index = 0;
            RelayMatch = relayMatch;
        }
    }
}
