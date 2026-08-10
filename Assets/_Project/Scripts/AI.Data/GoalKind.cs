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
    /// THE ORDER OF THE VALUES IS THE PRIORITY, highest first: when more than
    /// one condition holds, the lower number wins. It is a fixed order and not a
    /// profile value on purpose — priorities in the profile would move
    /// <c>AiProfile.ProfileHash</c> and with it the behaviour identifier printed
    /// into every lab artifact, and the first step of this strand has to be
    /// provably behaviour-neutral (ROADMAP.md point 2). A module that needs an
    /// off setting brings one with it, in the pull request that gives it a rule
    /// worth switching off.
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
    }
}
