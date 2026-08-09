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
    /// the numbers out of the code therefore changes NO behaviour, the four
    /// baseline files stay green, and that is precisely the proof the move was
    /// clean.</item>
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

        public AiProfile(
            string profileId,
            ushort decisionTickInterval,
            int placementSearchRadius,
            int powerReserve,
            int targetHarvesters,
            int harvesterQueueBatch,
            int targetArmySize,
            int attackSquadThreshold,
            int infantryQueueBatch)
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
            && InfantryQueueBatch == other.InfantryQueueBatch;

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
                return hash;
            }
        }

        public static bool operator ==(AiProfile left, AiProfile right) => left.Equals(right);
        public static bool operator !=(AiProfile left, AiProfile right) => !left.Equals(right);

        public override string ToString() => $"AiProfile({ProfileId}, schema {SchemaVersion})";
    }
}
