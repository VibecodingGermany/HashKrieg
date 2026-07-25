using System;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;

namespace Nova.Simulation.Construction
{
    public struct BuildingSiteState
    {
        public bool IsActive;
        public byte PlayerId;
        public BuildingDefinition Definition;
        public ushort OriginX;
        public ushort OriginY;
        public int RemainingTicks;

        public bool IsComplete => RemainingTicks <= 0;
    }

    /// <summary>
    /// Deterministic simulation system handling building placement, construction progress timers, and power grid integration.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class ConstructionSystem : ISimSystem
    {
        public const int MaxBuildingSites = 128;

        private readonly ConstructionGrid _grid;
        private readonly EnergyGridSystem _energyGrid;
        private readonly BuildingSiteState[] _sites;
        private int _activeSiteCount;

        public string Name => "ConstructionSystem";
        public ConstructionGrid Grid => _grid;

        public ConstructionSystem(ConstructionGrid grid, EnergyGridSystem energyGrid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _energyGrid = energyGrid ?? throw new ArgumentNullException(nameof(energyGrid));
            _sites = new BuildingSiteState[MaxBuildingSites];
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized construction system.");
        }

        public bool RequestConstruction(byte playerId, in BuildingDefinition def, ushort originX, ushort originY)
        {
            ref PlayerEconomyState eco = ref _energyGrid.GetPlayerEconomy(playerId);

            // 1. Check Aetherium Credit affordability
            if (!eco.TrySpendCredits(def.AetheriumCost)) return false;

            // 2. Check grid cell availability
            if (!_grid.CanPlaceBuilding(originX, originY, def.SizeX, def.SizeY))
            {
                eco.AddCredits(def.AetheriumCost); // Refund credits on failed placement
                return false;
            }

            // 3. Find free site slot
            for (int i = 0; i < MaxBuildingSites; i++)
            {
                if (!_sites[i].IsActive)
                {
                    _grid.OccupyCells(originX, originY, def.SizeX, def.SizeY, playerId);

                    _sites[i] = new BuildingSiteState
                    {
                        IsActive = true,
                        PlayerId = playerId,
                        Definition = def,
                        OriginX = originX,
                        OriginY = originY,
                        RemainingTicks = def.BuildTimeTicks
                    };

                    _activeSiteCount++;
                    return true;
                }
            }

            eco.AddCredits(def.AetheriumCost); // Refund if queue full
            return false;
        }

        public void ExecuteTick(Tick tick)
        {
            for (int i = 0; i < MaxBuildingSites; i++)
            {
                ref BuildingSiteState site = ref _sites[i];
                if (!site.IsActive) continue;

                ref PlayerEconomyState eco = ref _energyGrid.GetPlayerEconomy(site.PlayerId);
                float speedMultiplier = eco.ProductionSpeedMultiplier;

                // Progress construction timer (accounting for Low-Power -50% penalty)
                if (speedMultiplier >= 1.0f || (tick.Value % 2 == 0))
                {
                    site.RemainingTicks--;
                }

                if (site.IsComplete)
                {
                    // Register Power Produced/Consumed upon building completion
                    _energyGrid.RegisterPowerProduction(site.PlayerId, site.Definition.PowerProduced);
                    _energyGrid.RegisterPowerConsumption(site.PlayerId, site.Definition.PowerConsumed);

                    site.IsActive = false;
                    _activeSiteCount--;
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
