using System;

namespace Nova.Simulation.Economy
{
    /// <summary>
    /// Represents the deterministic economy and energy grid state of a player.
    /// Memory footprint: 16 bytes.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public struct PlayerEconomyState : IEquatable<PlayerEconomyState>
    {
        public byte PlayerId { get; }
        public int AetheriumCredits;
        public int PowerProduced;
        public int PowerConsumed;

        public bool IsLowPower => PowerConsumed > PowerProduced;
        public float ProductionSpeedMultiplier => IsLowPower ? 0.5f : 1.0f;

        public PlayerEconomyState(byte playerId, int startingCredits = 1000, int powerProduced = 30, int powerConsumed = 0)
        {
            PlayerId = playerId;
            AetheriumCredits = startingCredits;
            PowerProduced = powerProduced;
            PowerConsumed = powerConsumed;
        }

        public void AddCredits(int amount)
        {
            if (amount > 0)
            {
                AetheriumCredits += amount;
            }
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount <= 0 || AetheriumCredits < amount) return false;
            AetheriumCredits -= amount;
            return true;
        }

        public bool Equals(PlayerEconomyState other) => PlayerId == other.PlayerId && AetheriumCredits == other.AetheriumCredits;
        public override bool Equals(object obj) => obj is PlayerEconomyState other && Equals(other);
        public override int GetHashCode() => (PlayerId << 24) ^ AetheriumCredits;

        public static bool operator ==(PlayerEconomyState left, PlayerEconomyState right) => left.Equals(right);
        public static bool operator !=(PlayerEconomyState left, PlayerEconomyState right) => !left.Equals(right);
    }
}
