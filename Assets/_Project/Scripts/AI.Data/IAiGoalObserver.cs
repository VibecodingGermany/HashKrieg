namespace Nova.AI.Data
{
    /// <summary>Which rule the wave gate answered with this decision.</summary>
    public enum WaveGateMode : byte
    {
        /// <summary>Waves are off (<c>waveSize</c> 1): every unit is its own wave.</summary>
        Off = 0,

        /// <summary>The threshold is a number of gathered units.</summary>
        Count = 1,

        /// <summary>The threshold is a sum of combat points (r6).</summary>
        Strength = 2,
    }

    /// <summary>
    /// What the army as a whole decided this cadence, with the numbers the
    /// decision was made from.
    /// <para>
    /// The numbers are the point. "The wave waits" explains nothing; "the ring
    /// holds 1.060 of the 1.200 points it needs" says how far off it is and in
    /// which direction the next unit moves it.
    /// </para>
    /// </summary>
    public readonly struct AiArmyGoal
    {
        /// <summary>False when the army does not act at all — below the squad threshold, or no committed team view.</summary>
        public readonly bool Engages;

        /// <summary>The scored target every marching unit shoots at; 0 when nothing enemy is visible.</summary>
        public readonly uint TargetRaw;

        /// <summary>Where the army walks; -1 while it does not act.</summary>
        public readonly int MoveCellX;

        /// <summary>See <see cref="MoveCellX"/>.</summary>
        public readonly int MoveCellY;

        /// <summary>Where reinforcements gather; -1 when waves are off or the army does not act.</summary>
        public readonly int StagingCellX;

        /// <summary>See <see cref="StagingCellX"/>.</summary>
        public readonly int StagingCellY;

        /// <summary>Whether what waits in the staging ring is enough for the wave to march.</summary>
        public readonly bool WaveReady;

        /// <summary>Which rule answered — the unit of measure of <see cref="WaveThreshold"/>.</summary>
        public readonly WaveGateMode WaveMode;

        /// <summary>Living combat units inside the staging ring.</summary>
        public readonly int Gathered;

        /// <summary>Living combat units outside it — an earlier wave, never called back.</summary>
        public readonly int Committed;

        /// <summary>Summed combat points of the gathered units.</summary>
        public readonly long GatheredStrength;

        /// <summary>
        /// What the ring has to hold before the wave marches — points under
        /// <see cref="WaveGateMode.Strength"/>, heads under
        /// <see cref="WaveGateMode.Count"/>, 0 while waves are off. Already
        /// capped by what production can still deliver, so the difference to
        /// what is gathered is the honest distance to the march.
        /// </summary>
        public readonly long WaveThreshold;

        /// <summary>
        /// A visible armed enemy stands within <c>AiProfile.DefendHomeCells</c>
        /// of the headquarters, so everyone still in the ring breaks off and
        /// defends (r8). Always false while the rule is off.
        /// <para>
        /// APPENDED, like every column before it — a reader of an older file
        /// keeps reading the columns it has.
        /// </para>
        /// </summary>
        public readonly bool HomeThreatened;

        public AiArmyGoal(
            bool engages, uint targetRaw, int moveCellX, int moveCellY,
            int stagingCellX, int stagingCellY, bool waveReady, WaveGateMode waveMode,
            int gathered, int committed, long gatheredStrength, long waveThreshold,
            bool homeThreatened)
        {
            HomeThreatened = homeThreatened;
            Engages = engages;
            TargetRaw = targetRaw;
            MoveCellX = moveCellX;
            MoveCellY = moveCellY;
            StagingCellX = stagingCellX;
            StagingCellY = stagingCellY;
            WaveReady = waveReady;
            WaveMode = waveMode;
            Gathered = gathered;
            Committed = committed;
            GatheredStrength = gatheredStrength;
            WaveThreshold = waveThreshold;
        }
    }

    /// <summary>
    /// What one unit was told to do this cadence, which goal said so, and the
    /// measured quantities every goal condition compared against a profile
    /// value.
    /// <para>
    /// EVERY CONDITION IN THE GOAL CATALOGUE IS AN INTEGER COMPARISON, so the
    /// distance to the next one is exact arithmetic rather than an estimate:
    /// <c>RetreatHealthPercent - HealthPercent</c> is how much life a unit has
    /// left before it turns, <c>StagingToleranceCells - StagingDistanceCells</c>
    /// how many cells before it counts as arrived. That is what a panel can show
    /// without repeating the rules in a second language and getting them subtly
    /// wrong.
    /// </para>
    /// </summary>
    public readonly struct AiUnitGoal
    {
        /// <summary>The unit this is about.</summary>
        public readonly uint EntityRaw;

        /// <summary>The goal that won.</summary>
        public readonly GoalKind Goal;

        /// <summary>True when a goal mask named this unit — the goal was not the AI's own pick.</summary>
        public readonly bool Forced;

        /// <summary>The attack order that goes out; 0 means no attack intent is submitted.</summary>
        public readonly uint AttackTargetRaw;

        /// <summary>The move order that goes out; -1 means the unit is left where it walks.</summary>
        public readonly int MoveCellX;

        /// <summary>See <see cref="MoveCellX"/>.</summary>
        public readonly int MoveCellY;

        /// <summary>Health in percent of maximum — the left-hand side of the retreat rule.</summary>
        public readonly int HealthPercent;

        /// <summary>Chebyshev cells to the nearest visible ARMED enemy, or -1 when none is visible or the retreat rule is off.</summary>
        public readonly int ThreatDistanceCells;

        /// <summary>Chebyshev cells to the staging cell, or -1 when no staging cell is resolved.</summary>
        public readonly int StagingDistanceCells;

        /// <summary>Chebyshev cells to the own headquarters — the left-hand side of the staging-ring test.</summary>
        public readonly int HomeDistanceCells;

        public AiUnitGoal(
            uint entityRaw, GoalKind goal, bool forced, uint attackTargetRaw,
            int moveCellX, int moveCellY, int healthPercent,
            int threatDistanceCells, int stagingDistanceCells, int homeDistanceCells)
        {
            EntityRaw = entityRaw;
            Goal = goal;
            Forced = forced;
            AttackTargetRaw = attackTargetRaw;
            MoveCellX = moveCellX;
            MoveCellY = moveCellY;
            HealthPercent = healthPercent;
            ThreatDistanceCells = threatDistanceCells;
            StagingDistanceCells = stagingDistanceCells;
            HomeDistanceCells = homeDistanceCells;
        }
    }

    /// <summary>
    /// Watches the skirmish AI decide, without being able to change what it
    /// decides.
    /// <para>
    /// WHY AN OBSERVER AND NOT A RECORD ON THE SYSTEM. The AI is a pure function
    /// of the tick and the committed state; a buffer of "what I decided last"
    /// hanging off it would be exactly the memory the whole design avoids, and
    /// the first thing a later rule would be tempted to read. A callback carries
    /// the same information out to whoever asked for it and leaves nothing
    /// behind.
    /// </para>
    /// <para>
    /// WHAT THIS IS FOR. Until now the only way to see WHY a unit did something
    /// was to re-implement the rules beside the recording and label the result
    /// derived — and a diagnostic tool that shows a second, slightly different
    /// set of rules is worse than one that shows none. The goal is now recorded
    /// where it is decided.
    /// </para>
    /// <para>
    /// THE SHIPPED GAME NEVER PASSES ONE, so the null check is the entire cost
    /// in the delivered path, and everything the observer wants computed is
    /// computed behind it.
    /// </para>
    /// </summary>
    public interface IAiGoalObserver
    {
        /// <summary>
        /// The army's posture for this decision — reported even when the army
        /// does not act, because "it does not act" is the answer one is looking
        /// for at least as often as the other one.
        /// </summary>
        void OnArmyGoal(byte slot, uint tick, in AiArmyGoal army);

        /// <summary>
        /// One unit's goal for this decision. Called in the ascending entity
        /// scan, only while the army acts — below the squad threshold no unit is
        /// judged at all, which the army report has already said.
        /// </summary>
        void OnUnitGoal(byte slot, uint tick, in AiUnitGoal goal);
    }
}
