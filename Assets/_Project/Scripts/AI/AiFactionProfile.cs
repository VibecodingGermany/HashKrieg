using System;

namespace Nova.AI
{
    /// <summary>
    /// Configuration profile for Skirmish AI faction behaviors (Alliance vs. Legion).
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public readonly struct AiFactionProfile : IEquatable<AiFactionProfile>
    {
        public string FactionName { get; }
        public int TargetPowerMargin { get; }
        public int TargetArmySize { get; }
        public int AttackSquadThreshold { get; }

        public AiFactionProfile(string factionName, int targetPowerMargin = 30, int targetArmySize = 15, int attackSquadThreshold = 8)
        {
            FactionName = factionName ?? string.Empty;
            TargetPowerMargin = targetPowerMargin;
            TargetArmySize = targetArmySize;
            AttackSquadThreshold = attackSquadThreshold;
        }

        public bool Equals(AiFactionProfile other) => FactionName == other.FactionName;
        public override bool Equals(object obj) => obj is AiFactionProfile other && Equals(other);
        public override int GetHashCode() => FactionName != null ? FactionName.GetHashCode() : 0;

        public static bool operator ==(AiFactionProfile left, AiFactionProfile right) => left.Equals(right);
        public static bool operator !=(AiFactionProfile left, AiFactionProfile right) => !left.Equals(right);
    }
}
