using System;
using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Simulation.Vision
{
    /// <summary>
    /// Deterministic simulation system managing Fog of War line-of-sight calculations.
    /// Refreshes sight circles around active units and buildings on the VisionGrid.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class VisionSystem : ISimSystem
    {
        public const ushort DefaultSightRadius = 10;
        public const ushort TickInterval = 4; // Refresh vision every 4 ticks (0.2s)

        private readonly EntityManager _entityManager;
        private readonly VisionGrid _visionGrid;

        public string Name => "VisionSystem";
        public VisionGrid VisionGrid => _visionGrid;

        public VisionSystem(EntityManager entityManager, VisionGrid visionGrid)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _visionGrid = visionGrid ?? throw new ArgumentNullException(nameof(visionGrid));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized vision system.");
        }

        public void ExecuteTick(Tick tick)
        {
            if (tick.Value % TickInterval != 0) return;

            // Step 1: Demote current Visible cells to Explored across all players
            for (byte p = 0; p < VisionGrid.MaxPlayers; p++)
            {
                _visionGrid.DemoteVisibleToExplored(p);
            }

            // Step 2: Reveal sight circles for all active units
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive) continue;

                ushort cx = (ushort)SimMath.Clamp((int)Math.Floor(u.Transform.PositionX), 0, _visionGrid.Width - 1);
                ushort cy = (ushort)SimMath.Clamp((int)Math.Floor(u.Transform.PositionY), 0, _visionGrid.Height - 1);

                _visionGrid.RevealCircle(u.PlayerId, cx, cy, DefaultSightRadius);
            }
        }

        public void Shutdown()
        {
        }
    }
}
