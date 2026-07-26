using System;
using Nova.Core;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation;

namespace Nova.AI
{
    /// <summary>
    /// Deterministic utility-based Skirmish AI system for Alliance and Legion factions.
    /// Evaluates base building, economy expansion, unit production, and army squad attacks.
    /// Zero engine dependencies (no UnityEngine types).
    /// <para>
    /// Prototype scaffolding (not part of the canonical G3 AI contract): the
    /// decision loop now targets the CANONICAL construction and production
    /// domains — placement through
    /// <see cref="ConstructionSystem.TryPlaceBuilding"/> and enqueues through
    /// <see cref="ProductionSystem.TryQueueUnit"/> with the provisional
    /// MS-1 definition ids of <see cref="SimDefinitions"/> (Q-040). The
    /// retired research-tree dependency is gone: MS-1 has no research tree
    /// (mvp-v1.json technology model).
    /// </para>
    /// </summary>
    public sealed class SkirmishAiSystem : ISimSystem
    {
        public const ushort DecisionTickInterval = 20; // 1.0 second decision loop

        /// <summary>Provisional placement anchor of the AI's power plant (Q-040 candidate).</summary>
        private const int PowerPlantOriginX = 40;
        private const int PowerPlantOriginY = 40;

        private readonly byte _aiPlayerId;
        private readonly AiFactionProfile _profile;
        private readonly EntityManager _entityManager;
        private readonly EconomySystem _economy;
        private readonly ConstructionSystem _construction;
        private readonly ProductionSystem _production;

        public string Name => $"SkirmishAi_{_profile.FactionName}_P{_aiPlayerId}";
        public byte AiPlayerId => _aiPlayerId;

        public SkirmishAiSystem(
            byte aiPlayerId,
            AiFactionProfile profile,
            EntityManager entityManager,
            EconomySystem economy,
            ConstructionSystem construction,
            ProductionSystem production)
        {
            _aiPlayerId = aiPlayerId;
            _profile = profile;
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
            _production = production ?? throw new ArgumentNullException(nameof(production));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized Skirmish AI for player {_aiPlayerId}.");
        }

        public void ExecuteTick(Tick tick)
        {
            if (tick.Value % DecisionTickInterval != 0) return;

            ref PlayerEconomyState eco = ref _economy.GetPlayerEconomy(_aiPlayerId);

            // 1. Evaluate Energy Power Grid: build a Power plant
            //    (definition id 2, provisional Q-040 values) at a fixed anchor.
            int powerMargin = eco.PowerProvided - eco.PowerRequired;
            if (powerMargin < _profile.TargetPowerMargin)
            {
                _construction.TryPlaceBuilding(_aiPlayerId, 2, PowerPlantOriginX, PowerPlantOriginY);
                return;
            }

            // 2. Evaluate Unit Production: queue a Builder (definition id 1)
            //    at the first own completed HQ while nothing is queued.
            if (_production.TotalQueuedUnits == 0)
            {
                uint hqRaw = FindFirstOwnedCompletedHq();
                if (hqRaw != 0)
                {
                    _production.TryQueueUnit(_aiPlayerId, hqRaw, 1, 1);
                }
            }
        }

        /// <summary>Lowest-index own completed HQ placement, or 0 (ascending-index scan, deterministic).</summary>
        private uint FindFirstOwnedCompletedHq()
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.Role != UnitRole.HQ || unit.PlayerId != _aiPlayerId) continue;
                uint raw = UnitCommandStateView.ToRawEntityId(unit.Id);
                if (_construction.IsCompletedPlacement(raw))
                {
                    return raw;
                }
            }
            return 0;
        }

        public void Shutdown()
        {
        }
    }
}
