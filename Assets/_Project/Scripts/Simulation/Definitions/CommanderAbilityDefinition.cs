using System;

namespace Nova.Simulation.Definitions
{
    /// <summary>
    /// Immutable simulation definition for Commander & Doctrine abilities.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public readonly struct CommanderAbilityDefinition : IEquatable<CommanderAbilityDefinition>
    {
        public int AbilityId { get; }
        public string StringId { get; }
        public int EnergyCost { get; }
        public int CooldownTicks { get; }
        public float Radius { get; }
        public int EffectValue { get; }

        public CommanderAbilityDefinition(
            int abilityId,
            string stringId,
            int energyCost,
            int cooldownTicks,
            float radius,
            int effectValue)
        {
            AbilityId = abilityId;
            StringId = stringId ?? string.Empty;
            EnergyCost = energyCost;
            CooldownTicks = cooldownTicks;
            Radius = radius;
            EffectValue = effectValue;
        }

        public bool Equals(CommanderAbilityDefinition other) => AbilityId == other.AbilityId && StringId == other.StringId;
        public override bool Equals(object obj) => obj is CommanderAbilityDefinition other && Equals(other);
        public override int GetHashCode() => AbilityId;

        public static bool operator ==(CommanderAbilityDefinition left, CommanderAbilityDefinition right) => left.Equals(right);
        public static bool operator !=(CommanderAbilityDefinition left, CommanderAbilityDefinition right) => !left.Equals(right);
    }
}
