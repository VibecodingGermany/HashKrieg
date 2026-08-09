using System;

namespace Nova.AI.Data
{
    /// <summary>
    /// Every number the skirmish AI tunes on, in one place
    /// (docs/tech/AIArchitecture.md section 3: profiles change definitions and
    /// priorities, never rules or sight).
    /// <para>
    /// Before this type the values were split across two homes — constructor
    /// defaults on <c>AiFactionProfile</c> and <c>const</c> fields inside
    /// <c>SkirmishAiSystem</c> — so tuning meant editing behaviour code. One
    /// place to change is the point; the behaviour lives in C#, the numbers
    /// live here.
    /// </para>
    /// <para>
    /// TWO BINDING RULES.
    /// </para>
    /// <list type="number">
    /// <item><b>Whole numbers only.</b> A float in a profile is a float in the
    /// simulation, and <c>NoFloatInSimulationTests</c> checks. Every field
    /// here is an integer, and any weighting introduced later has to be too.</item>
    /// <item><b>The shipped profile keeps today's values exactly.</b> Moving
    /// the numbers out of the code therefore changes NO behaviour. What proves
    /// it is <c>AiProfileTests</c> (every field asserted value for value) and
    /// the unchanged end-state pin in <c>SkirmishAiTests</c> — NOT the four
    /// determinism baselines, which never run the skirmish AI.</item>
    /// </list>
    /// <para>
    /// ENGINE-FREE AND SIMULATION-FREE. This assembly references Nova.Core and
    /// nothing else — deliberately not Nova.Simulation, so a profile cannot
    /// name a <c>FactionId</c> or a <c>UnitRole</c> and quietly become a second
    /// definition table. Profiles are identified by <see cref="ProfileId"/>,
    /// a plain string, exactly as the plan's JSON sketch has it.
    /// </para>
    /// </summary>
    public readonly struct AiProfile : IEquatable<AiProfile>
    {
        /// <summary>
        /// Bumped when a field is removed or its meaning changes. Adding a
        /// field with a value that reproduces today's behaviour does not need
        /// a bump — the goal system of E7 will add several.
        /// </summary>
        public const int SchemaVersion = 1;

        /// <summary>Identity of this profile. Two profiles with different numbers are different profiles.</summary>
        public string ProfileId { get; }

        // ---- cadence ----

        /// <summary>Decision cadence in ticks: 20 ticks = 2.0 s on the canonical 10 Hz clock.</summary>
        public ushort DecisionTickInterval { get; }

        /// <summary>Largest Chebyshev ring around the placement anchor the spot search tries.</summary>
        public int PlacementSearchRadius { get; }

        // ---- economy ----

        /// <summary>
        /// Free power kept in reserve: a power-drawing building is placed only
        /// while the committed margin covers its draw plus this reserve. 0
        /// means "place a Power plant when the margin would go negative" — the
        /// D-077 opening rule the game ships with.
        /// </summary>
        public int PowerReserve { get; }

        /// <summary>Harvesters (alive plus queued) the completed Refinery is kept producing up to.</summary>
        public int TargetHarvesters { get; }

        /// <summary>Harvesters queued per decision tick while below the target.</summary>
        public int HarvesterQueueBatch { get; }

        // ---- army ----

        /// <summary>Infantry cap (alive plus queued) the Barracks is kept producing up to.</summary>
        public int TargetArmySize { get; }

        /// <summary>Living combat units at which the army is sent toward the enemy start area.</summary>
        public int AttackSquadThreshold { get; }

        /// <summary>Infantry queued per decision tick while below the army cap.</summary>
        public int InfantryQueueBatch { get; }

        // ---- waves ----
        //
        // EVERY NEW BEHAVIOUR CARRIES AN OFF SETTING, and these three are the
        // first to do it (methodology finding M001). A rule that lives only in
        // C# reaches BOTH sides of a self-play match, so "later decided, more
        // losses" cannot be told from "two stronger armies". With an off value
        // the same binary plays with against without, one-sided, in a single
        // comparison run. The off value here is WaveSize 1.

        /// <summary>
        /// Combat units that have to be waiting at the staging cell before the
        /// wave marches. <b>1 means off</b>: every unit is its own wave, which
        /// is the shipped behaviour of "march the moment you exist".
        /// </summary>
        public int WaveSize { get; }

        /// <summary>
        /// Cells from the own HQ toward the march destination at which the
        /// staging cell sits. Read only while <see cref="WaveSize"/> &gt; 1.
        /// </summary>
        public int StagingDistanceCells { get; }

        /// <summary>
        /// Chebyshev slack around the staging cell that still counts as
        /// "waiting here". Without it the formation spread would push arriving
        /// units past the staging point and they would count as already
        /// committed to the attack.
        /// </summary>
        public int StagingToleranceCells { get; }

        // ---- target scoring ----
        //
        // Four weights over ONE integer score, no scalar quality function:
        // this decides which visible enemy the army shoots at, it does not
        // rate the AI. The enemy HQ is not in here on purpose — losing it
        // decides the match (D-077), so it is the win condition and not a
        // preference a weight could outvote.

        /// <summary>
        /// Weight on the damage the army actually lands: the counter table
        /// resolved against the target's armor class, in integer percent
        /// (100 == 1.00), averaged over the living combat units.
        /// </summary>
        public int TargetDamageWeight { get; }

        /// <summary>Weight on how hard the target hits back — its own weapon damage, 0 for anything unarmed.</summary>
        public int TargetThreatWeight { get; }

        /// <summary>Weight on finishing wounded targets: percent of health already missing.</summary>
        public int TargetFinishWeight { get; }

        /// <summary>Penalty per cell of average Chebyshev distance between the army and the target.</summary>
        public int TargetDistanceWeight { get; }

        public AiProfile(
            string profileId,
            ushort decisionTickInterval,
            int placementSearchRadius,
            int powerReserve,
            int targetHarvesters,
            int harvesterQueueBatch,
            int targetArmySize,
            int attackSquadThreshold,
            int infantryQueueBatch,
            int targetDamageWeight,
            int targetThreatWeight,
            int targetFinishWeight,
            int targetDistanceWeight,
            int waveSize,
            int stagingDistanceCells,
            int stagingToleranceCells)
        {
            ProfileId = profileId ?? string.Empty;
            DecisionTickInterval = decisionTickInterval;
            PlacementSearchRadius = placementSearchRadius;
            PowerReserve = powerReserve;
            TargetHarvesters = targetHarvesters;
            HarvesterQueueBatch = harvesterQueueBatch;
            TargetArmySize = targetArmySize;
            AttackSquadThreshold = attackSquadThreshold;
            InfantryQueueBatch = infantryQueueBatch;
            TargetDamageWeight = targetDamageWeight;
            TargetThreatWeight = targetThreatWeight;
            TargetFinishWeight = targetFinishWeight;
            TargetDistanceWeight = targetDistanceWeight;
            WaveSize = waveSize;
            StagingDistanceCells = stagingDistanceCells;
            StagingToleranceCells = stagingToleranceCells;
        }

        /// <summary>
        /// Equality over EVERY value, not over the id.
        /// <para>
        /// This is the correction the migration had to make explicitly rather
        /// than inherit: <c>AiFactionProfile</c> compared only its faction
        /// name, so two profiles with the same name and different numbers
        /// counted as equal. Under tuning — where the whole point is two
        /// profiles that differ only in numbers — that comparison silently
        /// reports "no change".
        /// </para>
        /// </summary>
        public bool Equals(AiProfile other) =>
            ProfileId == other.ProfileId
            && DecisionTickInterval == other.DecisionTickInterval
            && PlacementSearchRadius == other.PlacementSearchRadius
            && PowerReserve == other.PowerReserve
            && TargetHarvesters == other.TargetHarvesters
            && HarvesterQueueBatch == other.HarvesterQueueBatch
            && TargetArmySize == other.TargetArmySize
            && AttackSquadThreshold == other.AttackSquadThreshold
            && InfantryQueueBatch == other.InfantryQueueBatch
            && TargetDamageWeight == other.TargetDamageWeight
            && TargetThreatWeight == other.TargetThreatWeight
            && TargetFinishWeight == other.TargetFinishWeight
            && TargetDistanceWeight == other.TargetDistanceWeight
            && WaveSize == other.WaveSize
            && StagingDistanceCells == other.StagingDistanceCells
            && StagingToleranceCells == other.StagingToleranceCells;

        public override bool Equals(object obj) => obj is AiProfile other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ProfileId != null ? ProfileId.GetHashCode() : 0;
                hash = (hash * 397) ^ DecisionTickInterval;
                hash = (hash * 397) ^ PlacementSearchRadius;
                hash = (hash * 397) ^ PowerReserve;
                hash = (hash * 397) ^ TargetHarvesters;
                hash = (hash * 397) ^ HarvesterQueueBatch;
                hash = (hash * 397) ^ TargetArmySize;
                hash = (hash * 397) ^ AttackSquadThreshold;
                hash = (hash * 397) ^ InfantryQueueBatch;
                hash = (hash * 397) ^ TargetDamageWeight;
                hash = (hash * 397) ^ TargetThreatWeight;
                hash = (hash * 397) ^ TargetFinishWeight;
                hash = (hash * 397) ^ TargetDistanceWeight;
                hash = (hash * 397) ^ WaveSize;
                hash = (hash * 397) ^ StagingDistanceCells;
                hash = (hash * 397) ^ StagingToleranceCells;
                return hash;
            }
        }

        public static bool operator ==(AiProfile left, AiProfile right) => left.Equals(right);
        public static bool operator !=(AiProfile left, AiProfile right) => !left.Equals(right);

        public override string ToString() => $"AiProfile({ProfileId}, schema {SchemaVersion})";
    }
}
