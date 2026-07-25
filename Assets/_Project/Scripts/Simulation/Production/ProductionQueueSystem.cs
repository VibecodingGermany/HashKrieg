using System;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.State;

namespace Nova.Simulation.Production
{
    public struct ProductionItemState
    {
        public bool IsActive;
        public byte PlayerId;
        public UnitDefinition Definition;
        public Transform2D SpawnTransform;
        public int RemainingTicks;

        public bool IsComplete => RemainingTicks <= 0;
    }

    /// <summary>
    /// Deterministic simulation system managing unit production queues in production buildings.
    /// Deducts credits on queueing, applies Low-Power speed penalties, and spawns units into EntityManager upon completion.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class ProductionQueueSystem : ISimSystem
    {
        public const int MaxQueueSlots = 128;

        private readonly EntityManager _entityManager;
        private readonly EnergyGridSystem _energyGrid;
        private readonly ResearchTreeSystem _researchTree;
        private readonly ProductionItemState[] _queue;
        private int _activeQueueCount;

        public string Name => "ProductionQueueSystem";
        public int ActiveQueueCount => _activeQueueCount;

        public ProductionQueueSystem(EntityManager entityManager, EnergyGridSystem energyGrid, ResearchTreeSystem researchTree)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _energyGrid = energyGrid ?? throw new ArgumentNullException(nameof(energyGrid));
            _researchTree = researchTree ?? throw new ArgumentNullException(nameof(researchTree));
            _queue = new ProductionItemState[MaxQueueSlots];
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized production queue system.");
        }

        public bool EnqueueUnitProduction(byte playerId, in UnitDefinition def, Transform2D spawnTransform, int buildTimeTicks = 60, byte requiredTechTier = 1)
        {
            // 1. Check tech tier prerequisite
            if (!_researchTree.IsTechUnlocked(playerId, requiredTechTier)) return false;

            // 2. Check Aetherium Credit affordability
            ref PlayerEconomyState eco = ref _energyGrid.GetPlayerEconomy(playerId);
            if (!eco.TrySpendCredits(def.AetheriumCost)) return false;

            // 3. Find free queue slot
            for (int i = 0; i < MaxQueueSlots; i++)
            {
                if (!_queue[i].IsActive)
                {
                    _queue[i] = new ProductionItemState
                    {
                        IsActive = true,
                        PlayerId = playerId,
                        Definition = def,
                        SpawnTransform = spawnTransform,
                        RemainingTicks = buildTimeTicks
                    };

                    _activeQueueCount++;
                    return true;
                }
            }

            eco.AddCredits(def.AetheriumCost); // Refund if queue capacity exceeded
            return false;
        }

        public void ExecuteTick(Tick tick)
        {
            for (int i = 0; i < MaxQueueSlots; i++)
            {
                ref ProductionItemState item = ref _queue[i];
                if (!item.IsActive) continue;

                ref PlayerEconomyState eco = ref _energyGrid.GetPlayerEconomy(item.PlayerId);
                float speedMultiplier = eco.ProductionSpeedMultiplier;

                // Account for Low-Power -50% speed penalty
                if (speedMultiplier >= 1.0f || (tick.Value % 2 == 0))
                {
                    item.RemainingTicks--;
                }

                if (item.IsComplete)
                {
                    // Spawn unit into EntityManager upon queue completion
                    _entityManager.SpawnUnit(
                        playerId: item.PlayerId,
                        initialTransform: item.SpawnTransform,
                        moveSpeed: item.Definition.MoveSpeed,
                        radius: item.Definition.Radius,
                        maxHealth: item.Definition.MaxHealth
                    );

                    item.IsActive = false;
                    _activeQueueCount--;
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
