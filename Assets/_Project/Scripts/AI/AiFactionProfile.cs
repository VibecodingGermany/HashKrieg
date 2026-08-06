using System;

namespace Nova.AI
{
    /// <summary>
    /// Configuration profile for Skirmish AI faction behaviors (Alliance vs. Legion).
    /// Zero engine dependencies (no UnityEngine types).
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
        public int TargetPowerMargin { get; }
        public int TargetArmySize { get; }
        public int AttackSquadThreshold { get; }
        public int TargetHarvesterCount { get; }

        public AiFactionProfile(string factionName, int targetPowerMargin = 30, int targetArmySize = 15,
            int attackSquadThreshold = 8, int targetHarvesterCount = 2)
        {
            FactionName = factionName ?? string.Empty;
            TargetPowerMargin = targetPowerMargin;
            TargetArmySize = targetArmySize;
            AttackSquadThreshold = attackSquadThreshold;
            TargetHarvesterCount = targetHarvesterCount;
        }

        public bool Equals(AiFactionProfile other) => FactionName == other.FactionName;
        public override bool Equals(object obj) => obj is AiFactionProfile other && Equals(other);
        public override int GetHashCode() => FactionName != null ? FactionName.GetHashCode() : 0;

        public static bool operator ==(AiFactionProfile left, AiFactionProfile right) => left.Equals(right);
        public static bool operator !=(AiFactionProfile left, AiFactionProfile right) => !left.Equals(right);
    }
}
