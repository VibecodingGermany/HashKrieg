using System;
using System.Text;
using Nova.Core;

namespace Nova.AI.Data
{
    /// <summary>
    /// Which build order the AI opens with. The values are stable: they are
    /// part of what <see cref="AiPersonality.Derive"/> maps a seed onto.
    /// </summary>
    public enum AiOpening : byte
    {
        /// <summary>Refinery, Barracks — the order the AI always played before r9.</summary>
        Balanced = 0,

        /// <summary>Barracks before Refinery, a smaller army and an early march.</summary>
        Rush = 1,

        /// <summary>A third harvester and a larger army, paid for by a slower start.</summary>
        Economy = 2,

        /// <summary>Defence platforms toward the front, a wider home guard, a bigger army.</summary>
        Turtle = 3,
    }

    /// <summary>
    /// The per-match flavour of the skirmish AI: a small bundle of choices that
    /// makes two matches against the same profile play differently, derived
    /// purely from the match seed and the AI's slot.
    /// <para>
    /// WHY THIS IS NOT RANDOMNESS INSIDE THE AI. <c>SkirmishAiSystem</c> is
    /// stateless by design — every decision is a pure function of the tick and
    /// the committed state, and that is what lets a save/restore reproduce the
    /// same later intents with no sidecar. A PRNG would be state. The personality
    /// is therefore an INPUT fixed at construction, exactly like the profile:
    /// the seed is consumed once, here, by a hash, and the AI only ever reads
    /// the result. Both peers of a networked match derive the same value from
    /// the same (seed, slot), so nothing has to be transmitted.
    /// </para>
    /// <para>
    /// <c>default(AiPersonality)</c> IS the neutral personality — Balanced, every
    /// delta zero — and reproduces the AI exactly as it played before it
    /// existed (behaviour r8). Tests, the lab and any caller that does not pass
    /// a personality therefore keep the behaviour they were measured against.
    /// </para>
    /// <para>
    /// Deltas are relative to the profile <see cref="Apply"/> is given. A
    /// personality never switches on a rule the base profile has off (retreat 0,
    /// defend-home 0): a profile that turned a rule off did so to measure
    /// without it, and a flavour must not undo that.
    /// </para>
    /// </summary>
    public readonly struct AiPersonality : IEquatable<AiPersonality>
    {
        /// <summary>Domain tag of the derivation hash; bump the suffix if the mapping ever changes.</summary>
        private const string DomainTag = "NOVA-AI-PERSONALITY-V1";

        /// <summary>Which build order the AI opens with.</summary>
        public AiOpening Opening { get; }

        /// <summary>Sideways shift of the staging cell, perpendicular to the line toward the enemy: -10, 0 or +10 cells.</summary>
        public int FlankOffsetCells { get; }

        /// <summary>Orientation of the placement scan inside each ring, 0..7 (bit0 x descending, bit1 y descending, bit2 x outer).</summary>
        public byte LayoutVariant { get; }

        /// <summary>Defence platforms the AI builds after its other buildings (Turtle: 1 or 2).</summary>
        public int DefensePlatforms { get; }

        public int ArmySizeDelta { get; }
        public int SquadThresholdDelta { get; }
        public int HarvesterDelta { get; }
        public int RetreatPercentDelta { get; }
        public int DefendHomeCellsDelta { get; }

        private AiPersonality(
            AiOpening opening, int flankOffsetCells, byte layoutVariant, int defensePlatforms,
            int armySizeDelta, int squadThresholdDelta, int harvesterDelta,
            int retreatPercentDelta, int defendHomeCellsDelta)
        {
            Opening = opening;
            FlankOffsetCells = flankOffsetCells;
            LayoutVariant = layoutVariant;
            DefensePlatforms = defensePlatforms;
            ArmySizeDelta = armySizeDelta;
            SquadThresholdDelta = squadThresholdDelta;
            HarvesterDelta = harvesterDelta;
            RetreatPercentDelta = retreatPercentDelta;
            DefendHomeCellsDelta = defendHomeCellsDelta;
        }

        /// <summary>The personality that changes nothing.</summary>
        public static AiPersonality Neutral => default;

        /// <summary>True for <c>default</c>: Balanced, every delta zero, no flank, layout 0.</summary>
        public bool IsNeutral =>
            Opening == AiOpening.Balanced && FlankOffsetCells == 0 && LayoutVariant == 0
            && DefensePlatforms == 0 && ArmySizeDelta == 0 && SquadThresholdDelta == 0
            && HarvesterDelta == 0 && RetreatPercentDelta == 0 && DefendHomeCellsDelta == 0;

        /// <summary>
        /// Builds a personality from its already-banded choices — the single
        /// place the per-opening deltas are defined, shared by
        /// <see cref="Derive"/> and by tests that need a specific combination.
        /// </summary>
        /// <param name="armyJitter">-1, 0 or +1: army size nudge on top of the opening's own delta.</param>
        /// <param name="retreatJitter">-1, 0 or +1: retreat percentage nudge in steps of 5.</param>
        /// <param name="extraDefensePlatform">Turtle only: 1 builds a second platform.</param>
        public static AiPersonality Create(
            AiOpening opening, int flankOffsetCells, byte layoutVariant,
            int armyJitter = 0, int retreatJitter = 0, int extraDefensePlatform = 0)
        {
            int army = armyJitter, squad = 0, harvesters = 0, retreat = 5 * retreatJitter, home = 0, platforms = 0;
            switch (opening)
            {
                case AiOpening.Rush:
                    army += -4; squad = -2; retreat += -15;
                    break;
                case AiOpening.Economy:
                    army += 4; harvesters = 1; retreat += 5;
                    break;
                case AiOpening.Turtle:
                    army += 2; retreat += 5; home = 4; platforms = 1 + (extraDefensePlatform & 1);
                    break;
            }
            return new AiPersonality(opening, flankOffsetCells, (byte)(layoutVariant & 7), platforms,
                army, squad, harvesters, retreat, home);
        }

        /// <summary>
        /// The personality of the AI in <paramref name="slot"/> of the match with
        /// <paramref name="matchSeed"/>. PURE: the same pair gives the same value
        /// on every machine, which is what lets both peers of a networked match
        /// agree without a message.
        /// <para>
        /// One hash, independent bit ranges: opening from the low bits, flank
        /// from the next byte, and so on, so that no field is a function of
        /// another and 512 seeds cover every combination of the small ones.
        /// </para>
        /// </summary>
        public static AiPersonality Derive(ulong matchSeed, byte slot)
        {
            byte[] tag = Encoding.ASCII.GetBytes(DomainTag);
            byte[] bytes = new byte[tag.Length + 9];
            Array.Copy(tag, bytes, tag.Length);
            for (int i = 0; i < 8; i++) bytes[tag.Length + i] = (byte)(matchSeed >> (8 * i));
            bytes[tag.Length + 8] = slot;
            ulong h = XxHash64.ComputeHash(bytes);

            AiOpening opening = (AiOpening)(h % 4);
            int flank = (int)(((h >> 8) % 3) - 1) * 10;
            byte layout = (byte)((h >> 16) % 8);
            int j = (int)((h >> 24) % 3) - 1;
            int k = (int)((h >> 32) % 3) - 1;
            int extraDefense = (int)((h >> 40) & 1);
            return Create(opening, flank, layout, j, k, extraDefense);
        }

        /// <summary>
        /// Returns <paramref name="baseProfile"/> with this personality's deltas
        /// applied. The neutral personality returns the profile untouched — same
        /// id, same hash — so the canonical profile stays the canonical profile.
        /// </summary>
        public AiProfile Apply(AiProfile baseProfile)
        {
            if (IsNeutral) return baseProfile;

            int squad = Math.Max(1, baseProfile.AttackSquadThreshold + SquadThresholdDelta);
            int army = Math.Max(Math.Max(1, squad), baseProfile.TargetArmySize + ArmySizeDelta);
            int harvesters = Math.Max(1, baseProfile.TargetHarvesters + HarvesterDelta);

            // A rule the base profile has off stays off.
            int retreat = baseProfile.RetreatHealthPercent;
            if (retreat > 0) retreat = Math.Min(95, Math.Max(0, retreat + RetreatPercentDelta));
            int home = baseProfile.DefendHomeCells;
            if (home > 0) home = Math.Max(0, home + DefendHomeCellsDelta);

            return new AiProfile(
                profileId: baseProfile.ProfileId + "+" + Opening.ToString().ToLowerInvariant(),
                decisionTickInterval: baseProfile.DecisionTickInterval,
                placementSearchRadius: baseProfile.PlacementSearchRadius,
                powerReserve: baseProfile.PowerReserve,
                targetHarvesters: harvesters,
                harvesterQueueBatch: baseProfile.HarvesterQueueBatch,
                targetArmySize: army,
                attackSquadThreshold: squad,
                infantryQueueBatch: baseProfile.InfantryQueueBatch,
                targetDamageWeight: baseProfile.TargetDamageWeight,
                targetThreatWeight: baseProfile.TargetThreatWeight,
                targetFinishWeight: baseProfile.TargetFinishWeight,
                targetDistanceWeight: baseProfile.TargetDistanceWeight,
                waveSize: baseProfile.WaveSize,
                stagingDistanceCells: baseProfile.StagingDistanceCells,
                stagingToleranceCells: baseProfile.StagingToleranceCells,
                retreatHealthPercent: retreat,
                retreatDangerCells: baseProfile.RetreatDangerCells,
                waveStrengthPoints: baseProfile.WaveStrengthPoints,
                defendHomeCells: home);
        }

        /// <summary>Compact description for the debug HUD and logs, e.g. <c>Rush, flank +10, layout 5, army 8</c>.</summary>
        public override string ToString()
        {
            string flank = FlankOffsetCells > 0 ? "+" + FlankOffsetCells : FlankOffsetCells.ToString();
            string army = ArmySizeDelta > 0 ? "+" + ArmySizeDelta : ArmySizeDelta.ToString();
            return IsNeutral
                ? "Neutral"
                : $"{Opening}, flank {flank}, layout {LayoutVariant}, army delta {army}";
        }

        public bool Equals(AiPersonality other) =>
            Opening == other.Opening && FlankOffsetCells == other.FlankOffsetCells
            && LayoutVariant == other.LayoutVariant && DefensePlatforms == other.DefensePlatforms
            && ArmySizeDelta == other.ArmySizeDelta && SquadThresholdDelta == other.SquadThresholdDelta
            && HarvesterDelta == other.HarvesterDelta && RetreatPercentDelta == other.RetreatPercentDelta
            && DefendHomeCellsDelta == other.DefendHomeCellsDelta;

        public override bool Equals(object obj) => obj is AiPersonality other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Opening;
                hash = (hash * 397) ^ FlankOffsetCells;
                hash = (hash * 397) ^ LayoutVariant;
                hash = (hash * 397) ^ DefensePlatforms;
                hash = (hash * 397) ^ ArmySizeDelta;
                hash = (hash * 397) ^ SquadThresholdDelta;
                hash = (hash * 397) ^ HarvesterDelta;
                hash = (hash * 397) ^ RetreatPercentDelta;
                hash = (hash * 397) ^ DefendHomeCellsDelta;
                return hash;
            }
        }

        public static bool operator ==(AiPersonality left, AiPersonality right) => left.Equals(right);
        public static bool operator !=(AiPersonality left, AiPersonality right) => !left.Equals(right);
    }
}
