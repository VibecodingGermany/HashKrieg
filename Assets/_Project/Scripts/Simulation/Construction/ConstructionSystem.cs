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
    /// <para>
    /// Prototype scaffolding (not part of the canonical kernel wiring): the
    /// canonical economy slice replaced the energy grid with
    /// <see cref="EconomySystem"/>. The low-power penalty now reads the exact
    /// Q16.16 multiplier via <see cref="PlayerEconomyState.IsLowPower"/>, and
    /// power registration on completion was dropped — the canonical power
    /// balance derives from building-role entities, and wiring construction
    /// output to role entities is the construction slice's job.
    /// </para>
    /// </summary>
    public sealed class ConstructionSystem : ISimSystem
    {
        public const int MaxBuildingSites = 128;

        private readonly ConstructionGrid _grid;
        private readonly EconomySystem _economy;
        private readonly BuildingSiteState[] _sites;
        private int _activeSiteCount;

        public string Name => "ConstructionSystem";
        public ConstructionGrid Grid => _grid;

        public ConstructionSystem(ConstructionGrid grid, EconomySystem economy)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _sites = new BuildingSiteState[MaxBuildingSites];
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized construction system.");
        }

        public bool RequestConstruction(byte playerId, in BuildingDefinition def, ushort originX, ushort originY)
        {
            ref PlayerEconomyState eco = ref _economy.GetPlayerEconomy(playerId);

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

                ref PlayerEconomyState eco = ref _economy.GetPlayerEconomy(site.PlayerId);

                // Progress construction timer (accounting for Low-Power -50% penalty)
                if (!eco.IsLowPower || (tick.Value % 2 == 0))
                {
                    site.RemainingTicks--;
                }

                if (site.IsComplete)
                {
                    // Power accounting derives from building-role entities in
                    // the canonical economy; the construction slice wires its
                    // output to role entities (no registration here anymore).
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
