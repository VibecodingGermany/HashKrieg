using System;

namespace Nova.Simulation.Definitions
{
    /// <summary>
    /// Immutable simulation-side unit definition struct.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public readonly struct UnitDefinition : IEquatable<UnitDefinition>
    {
        public int DefinitionId { get; }
        public string StringId { get; }
        public float MoveSpeed { get; }
        public float Radius { get; }
        public int MaxHealth { get; }
        public int AetheriumCost { get; }

        public UnitDefinition(
            int definitionId,
            string stringId,
            float moveSpeed,
            float radius,
            int maxHealth,
            int aetheriumCost)
        {
            DefinitionId = definitionId;
            StringId = stringId ?? string.Empty;
            MoveSpeed = moveSpeed;
            Radius = radius;
            MaxHealth = maxHealth;
            AetheriumCost = aetheriumCost;
        }

        public bool Equals(UnitDefinition other)
        {
            return DefinitionId == other.DefinitionId && StringId == other.StringId;
        }

        public override bool Equals(object obj) => obj is UnitDefinition other && Equals(other);
        public override int GetHashCode() => DefinitionId;

        public static bool operator ==(UnitDefinition left, UnitDefinition right) => left.Equals(right);
        public static bool operator !=(UnitDefinition left, UnitDefinition right) => !left.Equals(right);

        public override string ToString() => $"UnitDef({DefinitionId}:{StringId}, Speed={MoveSpeed})";
    }
}
