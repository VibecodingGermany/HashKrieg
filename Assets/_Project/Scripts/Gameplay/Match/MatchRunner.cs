using System;
using UnityEngine;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Gameplay.Match
{
    public sealed class UnityNovaLogger : INovaLogger
    {
        public bool IsEnabled(Core.LogLevel level) => true;

        public void Log(Core.LogLevel level, string message)
        {
            switch (level)
            {
                case Core.LogLevel.Warn:
                    Debug.LogWarning($"[Nova.Sim] {message}");
                    break;
                case Core.LogLevel.Error:
                    Debug.LogError($"[Nova.Sim] {message}");
                    break;
                default:
                    Debug.Log($"[Nova.Sim] {message}");
                    break;
            }
        }

        public void LogTrace(string message) => Log(Core.LogLevel.Trace, message);
        public void LogDebug(string message) => Log(Core.LogLevel.Debug, message);
        public void LogInfo(string message) => Log(Core.LogLevel.Info, message);
        public void LogWarn(string message) => Log(Core.LogLevel.Warn, message);
        public void LogError(string message) => Log(Core.LogLevel.Error, message);
    }

    /// <summary>
    /// MonoBehaviour driving the deterministic C# SimulationKernel inside Unity.
    /// Accumulates Time.deltaTime and steps the kernel at fixed 20 Ticks/sec (0.05s per tick).
    /// </summary>
    [DisallowMultipleComponent]
    public class MatchRunner : MonoBehaviour
    {
        public const float TickDeltaTime = 0.05f; // 20 Ticks / sec

        [Header("Match Settings")]
        [SerializeField] private ulong _seed = 12345UL;
        [SerializeField] private ushort _mapWidth = 128;
        [SerializeField] private ushort _mapHeight = 128;
        [SerializeField] private int _maxUnits = 2048;

        private float _timeAccumulator;

        public SimulationKernel Kernel { get; private set; }
        public EntityManager Entities { get; private set; }
        public PathfindingSystem Pathfinding { get; private set; }
        public MovementSystem Movement { get; private set; }

        public bool IsRunning => Kernel != null && Kernel.IsRunning;

        public void InitializeMatch(ulong seed, ushort width = 128, ushort height = 128, int maxUnits = 2048)
        {
            _seed = seed;
            _mapWidth = width;
            _mapHeight = height;
            _maxUnits = maxUnits;

            var random = new SimRandom(_seed);
            var logger = new UnityNovaLogger();
            Kernel = new SimulationKernel(random, logger);

            Entities = new EntityManager(_maxUnits);
            Pathfinding = new PathfindingSystem(_mapWidth, _mapHeight);
            Movement = new MovementSystem(Entities, Pathfinding);

            Kernel.RegisterSystem(Pathfinding);
            Kernel.RegisterSystem(Movement);
        }

        public void StartMatch()
        {
            if (Kernel == null)
            {
                InitializeMatch(_seed, _mapWidth, _mapHeight, _maxUnits);
            }

            _timeAccumulator = 0f;
            Kernel.Start();
        }

        public void PauseMatch()
        {
            if (IsRunning)
            {
                Kernel.Stop();
            }
        }

        private void Update()
        {
            if (!IsRunning) return;

            _timeAccumulator += Time.deltaTime;

            // Step fixed 20-Hz simulation ticks
            while (_timeAccumulator >= TickDeltaTime)
            {
                Kernel.StepTick();
                _timeAccumulator -= TickDeltaTime;
            }
        }

        private void OnDestroy()
        {
            if (IsRunning)
            {
                Kernel.Stop();
            }
        }
    }
}
