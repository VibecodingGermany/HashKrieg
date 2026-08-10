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
        /// D-077 margin rule the game ships with. Independently, D-103 forces
        /// a Power plant whenever the planned building names Power as a still
        /// missing prerequisite.
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
        // EVERY NEW BEHAVIOUR CARRIES AN OFF SETTING, and these were the
        // first to do it (methodology finding M001). A rule that lives only in
        // C# reaches BOTH sides of a self-play match, so "later decided, more
        // losses" cannot be told from "two stronger armies". With an off value
        // the same binary plays with against without, one-sided, in a single
        // comparison run.
        //
        // There are TWO off switches here now and they are not the same one:
        // WaveSize 1 turns waves off altogether, WaveStrengthPoints 0 keeps the
        // waves and returns their threshold to a head count. The second is the
        // one r6 is measured against.

        /// <summary>
        /// <b>1 means off</b> — every unit is its own wave, the pre-revision-3
        /// behaviour of "march the moment you exist" — and this switch also
        /// takes <see cref="WaveStrengthPoints"/> off with it.
        /// <para>
        /// ABOVE 1 IT IS NO LONGER THE WAVE'S THRESHOLD. Until r6 it was:
        /// so many combat units waiting at the staging cell and the wave
        /// marched. Since r6 the threshold is <see cref="WaveStrengthPoints"/>,
        /// and this value is read only on the off path of THAT field — the
        /// count rule still lives, but only while the strength gate is 0.
        /// </para>
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

        /// <summary>
        /// Combat strength that has to stand at the staging cell before the
        /// wave marches, in the points of <c>CombatStrength</c> (damage times
        /// health per firing interval — an Alliance rifleman at full health is
        /// 100). <b>0 means off</b> and the wave counts heads instead, exactly
        /// as it did before behaviour revision 6.
        /// <para>
        /// WHY A POINT VALUE AND NOT A LARGER <see cref="WaveSize"/>: a head
        /// count does not know what a head is worth. Twelve Legion recruits are
        /// 528 points against the twelve Alliance riflemen's 1.200, so the
        /// same number means two very different armies — and the Legion marches
        /// into the stronger one and calls it a full wave.
        /// </para>
        /// <para>
        /// ONE VALUE FOR BOTH FACTIONS ON PURPOSE. Per-faction thresholds would
        /// re-introduce exactly the asymmetry this replaces, one indirection
        /// further away from view.
        /// </para>
        /// </summary>
        public int WaveStrengthPoints { get; }

        // ---- retreat ----

        /// <summary>
        /// Health percentage below which a unit disengages and walks back to
        /// the staging cell. <b>0 means off.</b>
        /// <para>
        /// THERE IS NO EXIT THRESHOLD, and that is not an omission. The plan
        /// sketch asked for hysteresis between an entry and an exit percentage
        /// (25 in, 60 out), which presumes a unit can heal. In MS-1 it cannot:
        /// <c>Repair</c> validates its target as a completed BUILDING, and no
        /// other system raises a unit's health. An exit percentage would
        /// therefore never be reached, and a retreated unit would sit at home
        /// for the rest of the match while still counting against the army cap
        /// — the wave would never fill again and the AI would stop attacking
        /// altogether. The damping happens over DANGER and DISTANCE instead,
        /// see <see cref="RetreatDangerCells"/>.
        /// </para>
        /// </summary>
        public int RetreatHealthPercent { get; }

        /// <summary>
        /// How near a visible ARMED enemy has to be for a wounded unit to
        /// count as in danger. A unit retreats while it is wounded AND
        /// (an armed enemy is this close OR it is still walking home) — the
        /// second half is what stops it from turning around mid-field the
        /// moment it outruns the shooter. Once it is home it becomes an
        /// ordinary waiting unit and rejoins the next wave, wounded or not.
        /// </summary>
        public int RetreatDangerCells { get; }

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
            int stagingToleranceCells,
            int retreatHealthPercent,
            int retreatDangerCells,
            int waveStrengthPoints)
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
            RetreatHealthPercent = retreatHealthPercent;
            RetreatDangerCells = retreatDangerCells;
            WaveStrengthPoints = waveStrengthPoints;
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
            && StagingToleranceCells == other.StagingToleranceCells
            && RetreatHealthPercent == other.RetreatHealthPercent
            && RetreatDangerCells == other.RetreatDangerCells
            && WaveStrengthPoints == other.WaveStrengthPoints;

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
                hash = (hash * 397) ^ RetreatHealthPercent;
                hash = (hash * 397) ^ RetreatDangerCells;
                hash = (hash * 397) ^ WaveStrengthPoints;
                return hash;
            }
        }

        public static bool operator ==(AiProfile left, AiProfile right) => left.Equals(right);
        public static bool operator !=(AiProfile left, AiProfile right) => !left.Equals(right);

        public override string ToString() => $"AiProfile({ProfileId}, schema {SchemaVersion})";
    }
}
