namespace Nova.AI.Data
{
    /// <summary>
    /// A goal forced onto single units from outside — the "override" half of the
    /// lab's admin panel.
    /// <para>
    /// THE MASK IS AN INPUT, NOT A STATE, and the distinction is the whole
    /// reason this is an interface and not a field on the AI. The host answers
    /// "for this entity, goal X" before each decision; the AI reads the answer
    /// exactly the way it reads its profile, picks, and forgets. Nothing is
    /// remembered between cadences, no block is added beside the world, and the
    /// system stays a pure function of the tick, the committed state and its
    /// inputs. A sidecar would have been an owner decision (D-ID); this is not
    /// one.
    /// </para>
    /// <para>
    /// THE SHIPPED GAME NEVER PASSES ONE. <c>MatchRunner</c> constructs the AI
    /// without it, the reference is null, and the null check is the only cost —
    /// which is what makes "with the mask compiled in" byte-identical to
    /// "before", and therefore measurable at all.
    /// </para>
    /// <para>
    /// A run in which somebody intervened is NOT A MEASUREMENT. It says what the
    /// AI could have done, never what it does; the lab marks such a run and
    /// keeps it out of the archive.
    /// </para>
    /// </summary>
    public interface IAiGoalOverride
    {
        /// <summary>
        /// The goal forced on this entity, or <see cref="GoalKind.None"/> to
        /// leave the decision to the AI.
        /// <para>
        /// Called once per combat unit per decision cadence, in the ascending
        /// entity scan. It must be a pure function of the caller's own state for
        /// the run to stay reproducible: same tick, same entity, same answer.
        /// </para>
        /// </summary>
        GoalKind ResolveGoal(uint entityRaw);
    }
}
