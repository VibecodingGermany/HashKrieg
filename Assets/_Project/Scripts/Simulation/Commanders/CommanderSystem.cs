using System;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Simulation.Commanders
{
    public struct PlayerCommanderState
    {
        public int CurrentEnergy;
        public int CooldownRemainingTicks;

        public const int MaxEnergy = 100;
    }

    /// <summary>
    /// Deterministic simulation system managing Commander Energy accumulation, ability activation, and cooldowns.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class CommanderSystem : ISimSystem
    {
        public const int MaxPlayers = 8;
        public const int EnergyGenerationIntervalTicks = 20; // +1 Energy per 1.0s

        private readonly EntityManager _entityManager;
        private readonly PlayerCommanderState[] _playerStates;

        public string Name => "CommanderSystem";

        public CommanderSystem(EntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _playerStates = new PlayerCommanderState[MaxPlayers];
            for (int p = 0; p < MaxPlayers; p++)
            {
                _playerStates[p].CurrentEnergy = 50; // Starting energy
            }
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized Commander system.");
        }

        public ref PlayerCommanderState GetPlayerState(byte playerId)
        {
            if (playerId >= MaxPlayers) return ref _playerStates[0];
            return ref _playerStates[playerId];
        }

        public bool TryActivateAbility(byte playerId, in CommanderAbilityDefinition def, float targetX, float targetY)
        {
            if (playerId >= MaxPlayers) return false;
            ref PlayerCommanderState state = ref _playerStates[playerId];

            if (state.CooldownRemainingTicks > 0) return false;
            if (state.CurrentEnergy < def.EnergyCost) return false;

            // Spend energy & set cooldown
            state.CurrentEnergy -= def.EnergyCost;
            state.CooldownRemainingTicks = def.CooldownTicks;

            // Apply area effect (e.g., Orbital Strike damage to enemy units)
            if (def.EffectValue > 0 && def.Radius > 0f)
            {
                UnitState[] rawUnits = _entityManager.RawUnits;
                int capacity = _entityManager.Capacity;
                float rSq = def.Radius * def.Radius;

                for (int i = 0; i < capacity; i++)
                {
                    ref UnitState u = ref rawUnits[i];
                    if (!u.IsActive || u.PlayerId == playerId) continue;

                    float dx = u.Transform.PositionX - targetX;
                    float dy = u.Transform.PositionY - targetY;

                    if (dx * dx + dy * dy <= rSq)
                    {
                        u.CurrentHealth = SimMath.Clamp(u.CurrentHealth - def.EffectValue, 0, u.MaxHealth);
                    }
                }
            }

            return true;
        }

        public void ExecuteTick(Tick tick)
        {
            for (byte p = 0; p < MaxPlayers; p++)
            {
                ref PlayerCommanderState state = ref _playerStates[p];

                // Decrement cooldown timer
                if (state.CooldownRemainingTicks > 0)
                {
                    state.CooldownRemainingTicks--;
                }

                // Passive energy generation
                if (tick.Value % EnergyGenerationIntervalTicks == 0 && state.CurrentEnergy < PlayerCommanderState.MaxEnergy)
                {
                    state.CurrentEnergy++;
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
