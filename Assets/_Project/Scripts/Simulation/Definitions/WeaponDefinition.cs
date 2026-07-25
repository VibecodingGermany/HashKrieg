using System;

namespace Nova.Simulation.Definitions
{
    /// <summary>
    /// Immutable simulation weapon definition struct.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public readonly struct WeaponDefinition : IEquatable<WeaponDefinition>
    {
        public string WeaponId { get; }
        public float Range { get; }
        public int Damage { get; }
        public int CooldownTicks { get; }

        public WeaponDefinition(string weaponId, float range, int damage, int cooldownTicks)
        {
            WeaponId = weaponId ?? string.Empty;
            Range = range;
            Damage = damage;
            CooldownTicks = cooldownTicks;
        }

        public bool Equals(WeaponDefinition other) => WeaponId == other.WeaponId;
        public override bool Equals(object obj) => obj is WeaponDefinition other && Equals(other);
        public override int GetHashCode() => WeaponId != null ? WeaponId.GetHashCode() : 0;

        public static bool operator ==(WeaponDefinition left, WeaponDefinition right) => left.Equals(right);
        public static bool operator !=(WeaponDefinition left, WeaponDefinition right) => !left.Equals(right);
    }
}
