using System;
using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Simulation.Economy
{
    /// <summary>
    /// Canonical per-slot economy state of the harvest slice
    /// (docs/tech/SimulationCore.md sections 2 and 3): Aetherium credits and
    /// the power grid balance of one player slot. Pure integer / fixed-point;
    /// zero engine dependencies (no UnityEngine types, no floats).
    /// <para>
    /// Credits are int64 and never negative: the only income of this slice is
    /// the harvest deposit; spending is the concern of the command /
    /// construction / production slices, which must refuse a spend the
    /// balance cannot cover (<see cref="TrySpendCredits"/> is the enforcing
    /// primitive). The canonical match start balance is 3.000 AE
    /// (<see cref="EconomySystem.CanonicalMatchStartingCreditsAE"/>, D-077 —
    /// quality/content/mvp-v1.json startStatePerPlayer.aetheriumAE); the
    /// constructor default below is a plain library default for tests and
    /// fixtures, not the match rule.
    /// </para>
    /// <para>
    /// Power: <see cref="PowerProvided"/> and <see cref="PowerRequired"/> are
    /// recomputed by the <see cref="EconomySystem"/> on every tick from the
    /// living building-role entities (SimulationCore.md section 2, phase 2),
    /// so a destroyed power building drops the grid into
    /// <see cref="IsLowPower"/> deterministically. The low-power production
    /// factor is the exact Q16.16 value 0.5 (raw 32768) —
    /// <see cref="ProductionSpeedMultiplierQ16"/> replaces the retired
    /// prototype's IEEE-754 float multiplier.
    /// </para>
    /// </summary>
    public struct PlayerEconomyState : IEquatable<PlayerEconomyState>
    {
        /// <summary>Exact 0.5 in Q16.16 (raw 32768): the low-power production factor.</summary>
        public static readonly SimFixed LowPowerMultiplier = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        public byte PlayerId { get; }

        /// <summary>
        /// The faction this slot plays (stable wire value, economy block v2).
        /// Defaults to <see cref="FactionId.Alliance"/>; match setup assigns
        /// the real factions before the first tick, so the value is part of
        /// the hashed initial state.
        /// </summary>
        public FactionId Faction;

        /// <summary>Aetherium balance in AE; never negative (invariant, enforced by <see cref="TrySpendCredits"/>).</summary>
        public long AetheriumCredits;

        /// <summary>Power supplied by the slot's living power-providing buildings.</summary>
        public int PowerProvided;

        /// <summary>Power drawn by the slot's living power-consuming buildings.</summary>
        public int PowerRequired;

        /// <summary>True when demand exceeds supply; halves production speed.</summary>
        public bool IsLowPower => PowerRequired > PowerProvided;

        /// <summary>Production speed factor as exact Q16.16: 0.5 (raw 32768) under low power, 1.0 otherwise.</summary>
        public SimFixed ProductionSpeedMultiplierQ16 => IsLowPower ? LowPowerMultiplier : SimFixed.One;

        public PlayerEconomyState(byte playerId, long startingCredits = 1000, FactionId faction = FactionId.Alliance)
        {
            if (startingCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCredits), "Starting credits must not be negative.");
            }
            PlayerId = playerId;
            Faction = faction;
            AetheriumCredits = startingCredits;
            PowerProvided = 0;
            PowerRequired = 0;
        }

        /// <summary>Adds a non-negative amount of credits (harvest deposits of this slice).</summary>
        public void AddCredits(long amount)
        {
            if (amount > 0)
            {
                AetheriumCredits += amount;
            }
        }

        /// <summary>
        /// Attempts to spend credits; refuses (and mutates nothing) when the
        /// balance cannot cover the amount, so credits can never go negative.
        /// </summary>
        public bool TrySpendCredits(long amount)
        {
            if (amount <= 0 || AetheriumCredits < amount) return false;
            AetheriumCredits -= amount;
            return true;
        }

        public bool Equals(PlayerEconomyState other) =>
            PlayerId == other.PlayerId
            && Faction == other.Faction
            && AetheriumCredits == other.AetheriumCredits
            && PowerProvided == other.PowerProvided
            && PowerRequired == other.PowerRequired;

        public override bool Equals(object obj) => obj is PlayerEconomyState other && Equals(other);

        public override int GetHashCode() =>
            (PlayerId << 24) ^ ((int)Faction << 16) ^ AetheriumCredits.GetHashCode() ^ (PowerProvided << 8) ^ PowerRequired;

        public static bool operator ==(PlayerEconomyState left, PlayerEconomyState right) => left.Equals(right);
        public static bool operator !=(PlayerEconomyState left, PlayerEconomyState right) => !left.Equals(right);
    }
}
