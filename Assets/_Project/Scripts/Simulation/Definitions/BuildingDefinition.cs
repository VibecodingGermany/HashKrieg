using System;

namespace Nova.Simulation.Definitions
{
    /// <summary>
    /// Immutable simulation building definition struct.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public readonly struct BuildingDefinition : IEquatable<BuildingDefinition>
    {
        public int DefinitionId { get; }
        public string StringId { get; }
        public ushort SizeX { get; }
        public ushort SizeY { get; }
        public int MaxHealth { get; }
        public int PowerProduced { get; }
        public int PowerConsumed { get; }
        public int AetheriumCost { get; }
        public int BuildTimeTicks { get; }

        public BuildingDefinition(
            int definitionId,
            string stringId,
            ushort sizeX,
            ushort sizeY,
            int maxHealth,
            int powerProduced,
            int powerConsumed,
            int aetheriumCost,
            int buildTimeTicks)
        {
            DefinitionId = definitionId;
            StringId = stringId ?? string.Empty;
            SizeX = sizeX;
            SizeY = sizeY;
            MaxHealth = maxHealth;
            PowerProduced = powerProduced;
            PowerConsumed = powerConsumed;
            AetheriumCost = aetheriumCost;
            BuildTimeTicks = buildTimeTicks;
        }

        public bool Equals(BuildingDefinition other) => DefinitionId == other.DefinitionId && StringId == other.StringId;
        public override bool Equals(object obj) => obj is BuildingDefinition other && Equals(other);
        public override int GetHashCode() => DefinitionId;

        public static bool operator ==(BuildingDefinition left, BuildingDefinition right) => left.Equals(right);
        public static bool operator !=(BuildingDefinition left, BuildingDefinition right) => !left.Equals(right);
    }
}
