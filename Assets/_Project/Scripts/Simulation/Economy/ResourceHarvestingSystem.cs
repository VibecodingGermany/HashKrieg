using System;
using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Simulation.Economy
{
    /// <summary>
    /// Deterministic simulation system handling harvester unit unloading at Aetherium Refineries.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class ResourceHarvestingSystem : ISimSystem
    {
        public const int DefaultHarvestDepositAmount = 50;

        private readonly EntityManager _entityManager;
        private readonly EnergyGridSystem _energyGrid;

        public string Name => "ResourceHarvestingSystem";

        public ResourceHarvestingSystem(EntityManager entityManager, EnergyGridSystem energyGrid)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _energyGrid = energyGrid ?? throw new ArgumentNullException(nameof(energyGrid));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized resource harvesting system.");
        }

        public bool DepositResource(byte playerId, int amount = DefaultHarvestDepositAmount)
        {
            if (amount <= 0) return false;
            ref PlayerEconomyState eco = ref _energyGrid.GetPlayerEconomy(playerId);
            eco.AddCredits(amount);
            return true;
        }

        public void ExecuteTick(Tick tick)
        {
            // Deterministic tick updates for harvester unloading states
        }

        public void Shutdown()
        {
        }
    }
}
