namespace Nova.AI.Data
{
    /// <summary>
    /// What one unit is trying to do in THIS decision — a name for a condition
    /// and its effect, and nothing else.
    /// <para>
    /// A GOAL IS NOT STATE. It is worked out fresh on every decision cadence
    /// from the committed world and thrown away again; nothing stores it, and
    /// therefore nothing has to serialize it. That is what keeps the skirmish AI
    /// a pure function of the tick and the committed state after the goals were
    /// named — the names describe the decision, they do not survive it.
    /// </para>
    /// <para>
    /// It lives in this assembly rather than beside the rules because THREE
    /// SURFACES HAVE TO AGREE ON THE WORDS: the simulation that picks a goal,
    /// the lab that records which one was picked, and the panel that draws it.
    /// Nova.AI.Data references Nova.Core and nothing else, so every one of them
    /// can name a goal without pulling the simulation in behind it.
    /// </para>
    /// <para>
    /// THE PRIORITY IS FIXED AND IT IS NOT THE NUMBER. When more than one
    /// condition holds, the winner is decided by the order the tests are
    /// written in <c>SkirmishAiSystem.ResolveGoal</c>, which is:
    /// </para>
    /// <list type="number">
    /// <item><see cref="Retreat"/> — a wounded unit outranks everything, or the
    /// pull-back could never reach the one unit it exists for</item>
    /// <item><see cref="DefendHome"/> — the base burning outranks gathering for
    /// a wave that will leave it burning</item>
    /// <item><see cref="Attack"/></item>
    /// <item><see cref="Hold"/></item>
    /// <item><see cref="Advance"/></item>
    /// </list>
    /// <para>
    /// THE NUMBERS ARE APPENDED, NEVER RENUMBERED, and that is why the two came
    /// apart. A value is written into <c>goals.ndjson</c> as a bare integer;
    /// renumbering to keep value order equal to priority order would silently
    /// re-label every recorded run — the panel would print <c>attack</c> where
    /// the file means <c>hold</c>, which is precisely the display error a
    /// diagnostic tool must not have. Columns are appended in the artifacts for
    /// the same reason, and a goal is a column.
    /// </para>
    /// <para>
    /// The priority is fixed in code and not a profile value: priorities in the
    /// profile would have moved <c>AiProfile.ProfileHash</c> while the first
    /// step of this strand still had to be provably behaviour-neutral
    /// (ROADMAP.md point 2). A module that needs an off setting brings one with
    /// it, in the pull request that gives it a rule worth switching off —
    /// <see cref="DefendHome"/> is the first that did.
    /// </para>
    /// </summary>
    public enum GoalKind : byte
    {
        /// <summary>
        /// No goal — the unit was not judged at all this decision, and for the
        /// goal mask (<see cref="IAiGoalOverride"/>) it means "leave this one to
        /// the AI". Never the answer of the resolver: every combat unit the army
        /// step looks at gets one of the four below.
        /// </summary>
        None = 0,

        /// <summary>
        /// Wounded, in danger, and pulling out: walk to the staging cell and
        /// shoot at whoever is chasing. Outranks everything, because a rule that
        /// cannot beat "you are out with the wave, keep going" can never pull
        /// anybody back.
        /// </summary>
        Retreat = 1,

        /// <summary>
        /// Marching on the army's target: the shared attack target and the
        /// shared destination. The wave is out, or this unit already is.
        /// </summary>
        Attack = 2,

        /// <summary>
        /// Standing at the staging cell with nothing to say. THE EFFECT IS
        /// SILENCE, and that is the goal's whole content: a unit that is where
        /// it belongs and gets told so again every cadence turns 23 actions per
        /// minute into 40 without changing anything (behaviour journal V002).
        /// </summary>
        Hold = 3,

        /// <summary>
        /// Reinforcement on its way to the staging cell. No attack target while
        /// it walks — an explicit order is released only by its target's death,
        /// so aiming while not closing the distance silences the unit instead of
        /// arming it (finding F001, journal V003).
        /// </summary>
        Advance = 4,

        /// <summary>
        /// The base is under attack and this unit was waiting to leave it:
        /// break off, walk to the own headquarters, shoot at whoever is nearest.
        /// Outranks <see cref="Attack"/> — gathering for a wave that marches
        /// away from a burning base is the defect this goal exists for — and
        /// gives way to <see cref="Retreat"/>, because a unit too wounded to
        /// fight is no defender.
        /// <para>
        /// ONLY UNITS STILL INSIDE THE STAGING RING. A wave that is already out
        /// keeps marching: "units that are out are never called back" is the r3
        /// rule that made a wave a wave, and recalling them is the V002 failure
        /// mode with a new name.
        /// </para>
        /// <para>
        /// THE DESTINATION IS A STATIC CELL — the headquarters, which does not
        /// move all match — and not the enemy. That is the whole difference to
        /// the discarded <c>DefendBase</c>: a moving destination meant a fresh
        /// order every cadence for every unit, 23 % more intents and a worse
        /// match (journal V002).
        /// </para>
        /// </summary>
        DefendHome = 5,
    }
}
