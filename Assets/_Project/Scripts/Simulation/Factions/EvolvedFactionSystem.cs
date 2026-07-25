using System;
using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Simulation.Factions
{
    /// <summary>
    /// Deterministic simulation system handling organic faction mechanics for "Die Evolvierten".
    /// Provides passive health regeneration (+2 HP per 0.5s) for units positioned on Biomass cells.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class EvolvedFactionSystem : ISimSystem
    {
        public const int RegenerationIntervalTicks = 10; // Heal every 10 ticks (0.5s)
        public const int BiomassHealAmount = 2;

        private readonly EntityManager _entityManager;
        private readonly BiomassGrid _biomassGrid;

        public string Name => "EvolvedFactionSystem";
        public BiomassGrid BiomassGrid => _biomassGrid;

        public EvolvedFactionSystem(EntityManager entityManager, BiomassGrid biomassGrid)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _biomassGrid = biomassGrid ?? throw new ArgumentNullException(nameof(biomassGrid));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized Evolved Faction system.");
        }

        public void ExecuteTick(Tick tick)
        {
            if (tick.Value % RegenerationIntervalTicks != 0) return;

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref UnitState u = ref units[i];
                if (!u.IsActive || u.CurrentHealth >= u.MaxHealth) continue;

                if (_biomassGrid.IsOnBiomass(u.Transform.PositionX, u.Transform.PositionY))
                {
                    u.CurrentHealth = SimMath.Clamp(u.CurrentHealth + BiomassHealAmount, 0, u.MaxHealth);
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
