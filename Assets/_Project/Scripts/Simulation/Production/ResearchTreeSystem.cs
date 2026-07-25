using System;
using Nova.Core;

namespace Nova.Simulation.Production
{
    /// <summary>
    /// Deterministic simulation system managing tech tier unlocks and research prerequisites per player.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class ResearchTreeSystem : ISimSystem
    {
        public const int MaxPlayers = 8;
        private readonly byte[] _techTiers;

        public string Name => "ResearchTreeSystem";

        public ResearchTreeSystem()
        {
            _techTiers = new byte[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                _techTiers[i] = 1; // All players start at Tier 1
            }
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized research tree system.");
        }

        public byte GetTechTier(byte playerId)
        {
            if (playerId >= MaxPlayers) return 1;
            return _techTiers[playerId];
        }

        public bool IsTechUnlocked(byte playerId, byte requiredTier)
        {
            return GetTechTier(playerId) >= requiredTier;
        }

        public bool UnlockTechTier(byte playerId, byte newTier)
        {
            if (playerId >= MaxPlayers || newTier <= _techTiers[playerId]) return false;
            _techTiers[playerId] = newTier;
            return true;
        }

        public void ExecuteTick(Tick tick)
        {
            // Deterministic tick processing for active research projects
        }

        public void Shutdown()
        {
        }
    }
}
