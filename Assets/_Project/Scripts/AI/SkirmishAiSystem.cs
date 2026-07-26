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
    /// </summary>
    public sealed class SkirmishAiSystem : ISimSystem
    {
        public const ushort DecisionTickInterval = 20; // 1.0 second decision loop

        private readonly byte _aiPlayerId;
        private readonly AiFactionProfile _profile;
        private readonly EntityManager _entityManager;
        private readonly EconomySystem _economy;
        private readonly ConstructionSystem _construction;
        private readonly ProductionQueueSystem _production;

        public string Name => $"SkirmishAi_{_profile.FactionName}_P{_aiPlayerId}";
        public byte AiPlayerId => _aiPlayerId;

        public SkirmishAiSystem(
            byte aiPlayerId,
            AiFactionProfile profile,
            EntityManager entityManager,
            EconomySystem economy,
            ConstructionSystem construction,
            ProductionQueueSystem production)
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

            // 1. Evaluate Energy Power Grid
            int powerMargin = eco.PowerProvided - eco.PowerRequired;
            if (powerMargin < _profile.TargetPowerMargin && eco.AetheriumCredits >= 100)
            {
                var powerPlantDef = new BuildingDefinition(
                    definitionId: 2,
                    stringId: "BLD_PowerPlant",
                    sizeX: 2,
                    sizeY: 2,
                    maxHealth: 300,
                    powerProduced: 50,
                    powerConsumed: 0,
                    aetheriumCost: 100,
                    buildTimeTicks: 20
                );

                _construction.RequestConstruction(_aiPlayerId, in powerPlantDef, 40, 40);
                return;
            }

            // 2. Evaluate Unit Production
            if (eco.AetheriumCredits >= 100 && _production.ActiveQueueCount == 0)
            {
                var riflemanDef = new UnitDefinition(
                    definitionId: 10,
                    stringId: "UNIT_Rifleman",
                    moveSpeed: 5.0f,
                    radius: 0.5f,
                    maxHealth: 100,
                    aetheriumCost: 100
                );

                _production.EnqueueUnitProduction(
                    playerId: _aiPlayerId,
                    def: in riflemanDef,
                    spawnTransform: new Transform2D(SimFixed.FromInt(45), SimFixed.FromInt(45)),
                    buildTimeTicks: 20
                );
            }
        }

        public void Shutdown()
        {
        }
    }
}
