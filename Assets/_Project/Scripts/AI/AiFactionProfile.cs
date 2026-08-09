using System;
using Nova.AI.Data;

namespace Nova.AI
{
    /// <summary>
    /// Configuration profile for Skirmish AI faction behaviors (Alliance vs. Legion).
    /// Zero engine dependencies (no UnityEngine types).
    /// <para>
    /// SINCE THE DATA MIGRATION this type is a thin, faction-named handle over
    /// an <see cref="AiProfile"/> in <c>Nova.AI.Data</c> — that is where the
    /// numbers live now, all of them, including the cadence values that used
    /// to be <c>const</c> fields inside <see cref="SkirmishAiSystem"/>.
    /// </para>
    /// <para>
    /// The constructor signature is UNCHANGED on purpose. <c>MatchRunner</c>
    /// constructs it, and MatchRunner belongs to the network strand — a
    /// data-layer migration must not reach into someone else's file. Callers
    /// that pass the four historical numbers keep working unchanged and get
    /// the shipped cadence; callers that want to tune everything pass an
    /// <see cref="AiProfile"/> instead.
    /// </para>
    /// <para>
    /// Field semantics of the MS-1 skirmish loop (docs/tech/AIArchitecture.md
    /// section 3: profiles change definitions and priorities, never rules or
    /// sight):
    /// <list type="bullet">
    /// <item><see cref="TargetPowerMargin"/> — free power the AI keeps in
    /// reserve: a power-drawing building is only placed while the committed
    /// margin covers its draw plus this reserve (0 = "place a Power plant
    /// when the margin would go negative", the D-077 opening rule);</item>
    /// <item><see cref="TargetArmySize"/> — infantry cap (alive plus queued)
    /// the Barracks is kept producing up to;</item>
    /// <item><see cref="AttackSquadThreshold"/> — living combat units at
    /// which the army is sent toward the enemy start area;</item>
    /// <item><see cref="TargetHarvesterCount"/> — harvesters (alive plus
    /// queued) the completed Refinery is kept producing up to.</item>
    /// </list>
    /// </para>
    /// </summary>
    public readonly struct AiFactionProfile : IEquatable<AiFactionProfile>
    {
        public string FactionName { get; }

        /// <summary>Every tunable number of this profile (Nova.AI.Data).</summary>
        public AiProfile Profile { get; }

        public int TargetPowerMargin => Profile.PowerReserve;
        public int TargetArmySize => Profile.TargetArmySize;
        public int AttackSquadThreshold => Profile.AttackSquadThreshold;
        public int TargetHarvesterCount => Profile.TargetHarvesters;

        /// <summary>
        /// The historical four-number constructor. The cadence values it
        /// cannot express come from <see cref="AiProfiles.Ms1Canonical"/> —
        /// the values that were <c>const</c> in the system before, so this
        /// path produces exactly the behaviour it always did.
        /// </summary>
        public AiFactionProfile(string factionName, int targetPowerMargin = 30, int targetArmySize = 15,
            int attackSquadThreshold = 8, int targetHarvesterCount = 2)
        {
            FactionName = factionName ?? string.Empty;
            AiProfile shipped = AiProfiles.Ms1Canonical;
            Profile = new AiProfile(
                profileId: shipped.ProfileId,
                decisionTickInterval: shipped.DecisionTickInterval,
                placementSearchRadius: shipped.PlacementSearchRadius,
                powerReserve: targetPowerMargin,
                targetHarvesters: targetHarvesterCount,
                harvesterQueueBatch: shipped.HarvesterQueueBatch,
                targetArmySize: targetArmySize,
                attackSquadThreshold: attackSquadThreshold,
                infantryQueueBatch: shipped.InfantryQueueBatch);
        }

        /// <summary>Binds a faction name to a fully specified profile — the tuning path.</summary>
        public AiFactionProfile(string factionName, AiProfile profile)
        {
            FactionName = factionName ?? string.Empty;
            Profile = profile;
        }

        /// <summary>
        /// Equality over the faction name AND every number.
        /// <para>
        /// The previous version compared the faction name alone, so two
        /// profiles named "Legion" with different numbers were equal. That is
        /// harmless while only one profile ships and actively wrong the moment
        /// tuning begins, because the whole point of a tuning run is two
        /// profiles that differ only in numbers.
        /// </para>
        /// </summary>
        public bool Equals(AiFactionProfile other) =>
            FactionName == other.FactionName && Profile.Equals(other.Profile);

        public override bool Equals(object obj) => obj is AiFactionProfile other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = FactionName != null ? FactionName.GetHashCode() : 0;
                return (hash * 397) ^ Profile.GetHashCode();
            }
        }

        public static bool operator ==(AiFactionProfile left, AiFactionProfile right) => left.Equals(right);
        public static bool operator !=(AiFactionProfile left, AiFactionProfile right) => !left.Equals(right);
    }
}
