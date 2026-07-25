using System;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Commands
{
    /// <summary>
    /// Deterministic simulation system processing unboxed CommandEnvelope orders.
    /// Updates entity targets, triggers pathfinding flow-field calculations, and applies unit orders.
    /// </summary>
    public sealed class CommandProcessorSystem : ISimSystem, ICommandSink
    {
        private readonly EntityManager _entityManager;
        private readonly PathfindingSystem _pathfindingSystem;
        private readonly CommandEnvelope[] _commandBuffer;
        private int _commandCount;

        public string Name => "CommandProcessorSystem";
        public int MaxCapacity => _commandBuffer.Length;

        public CommandProcessorSystem(EntityManager entityManager, PathfindingSystem pathfindingSystem, int commandCapacity = 256)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _pathfindingSystem = pathfindingSystem ?? throw new ArgumentNullException(nameof(pathfindingSystem));
            _commandBuffer = new CommandEnvelope[commandCapacity];
            _commandCount = 0;
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized command processor with capacity {MaxCapacity}.");
        }

        public bool SubmitCommand(in CommandEnvelope command)
        {
            if (_commandCount >= MaxCapacity) return false;
            _commandBuffer[_commandCount++] = command;
            return true;
        }

        public void ExecuteTick(Tick tick)
        {
            for (int i = 0; i < _commandCount; i++)
            {
                ref readonly CommandEnvelope cmd = ref _commandBuffer[i];
                ProcessCommand(in cmd);
            }

            _commandCount = 0; // Clear queue for next tick
        }

        private void ProcessCommand(in CommandEnvelope cmd)
        {
            switch (cmd.Type)
            {
                case CommandType.Move:
                    ushort targetX = (ushort)SimMath.Clamp((int)cmd.TargetPositionX, 0, _pathfindingSystem.CostField.Width - 1);
                    ushort targetY = (ushort)SimMath.Clamp((int)cmd.TargetPositionY, 0, _pathfindingSystem.CostField.Height - 1);
                    var targetPos = new GridPos2D(targetX, targetY);

                    _pathfindingSystem.RequestFlowField(targetPos);

                    if (_entityManager.IsValid(cmd.EntityId))
                    {
                        ref UnitState unit = ref _entityManager.GetUnitRef(cmd.EntityId);
                        unit.SetTarget(targetPos);
                    }
                    break;

                case CommandType.Stop:
                    if (_entityManager.IsValid(cmd.EntityId))
                    {
                        ref UnitState unit = ref _entityManager.GetUnitRef(cmd.EntityId);
                        unit.Stop();
                    }
                    break;

                case CommandType.AttackTarget:
                    if (_entityManager.IsValid(cmd.EntityId))
                    {
                        ref UnitState unit = ref _entityManager.GetUnitRef(cmd.EntityId);
                        unit.AttackTarget = cmd.TargetEntityId;
                    }
                    break;
            }
        }

        public void Shutdown()
        {
            _commandCount = 0;
        }
    }
}
