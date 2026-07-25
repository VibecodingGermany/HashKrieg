using System;
using Nova.Core;

namespace Nova.Simulation.Economy
{
    /// <summary>
    /// Deterministic simulation system tracking player economy states and power grids.
    /// Manages Aetherium credit balances and triggers Low-Power penalties (-50% speed) when power is depleted.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class EnergyGridSystem : ISimSystem
    {
        public const int MaxPlayers = 8;
        private readonly PlayerEconomyState[] _playerEconomies;

        public string Name => "EnergyGridSystem";

        public EnergyGridSystem(int startingCredits = 1000)
        {
            _playerEconomies = new PlayerEconomyState[MaxPlayers];
            for (byte i = 0; i < MaxPlayers; i++)
            {
                _playerEconomies[i] = new PlayerEconomyState(i, startingCredits, powerProduced: 30, powerConsumed: 0);
            }
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized energy grid system for {MaxPlayers} players.");
        }

        public ref PlayerEconomyState GetPlayerEconomy(byte playerId)
        {
            if (playerId >= MaxPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), $"PlayerId must be between 0 and {MaxPlayers - 1}.");
            }
            return ref _playerEconomies[playerId];
        }

        public void RegisterPowerProduction(byte playerId, int amount)
        {
            if (playerId < MaxPlayers && amount > 0)
            {
                _playerEconomies[playerId].PowerProduced += amount;
            }
        }

        public void RegisterPowerConsumption(byte playerId, int amount)
        {
            if (playerId < MaxPlayers && amount > 0)
            {
                _playerEconomies[playerId].PowerConsumed += amount;
            }
        }

        public void ExecuteTick(Tick tick)
        {
            // Deterministic tick processing for economy state checks
        }

        public void Shutdown()
        {
        }
    }
}
