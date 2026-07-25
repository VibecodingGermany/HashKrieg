using System;

namespace Nova.Core
{
    /// <summary>
    /// Represents a deterministic, versioned handle to a simulation entity.
    /// Index refers to the entity array slot; Version prevents stale handle dereferencing.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        public static readonly EntityId Invalid = new EntityId(-1, 0);

        public int Index { get; }
        public ushort Version { get; }

        public bool IsValid => Index >= 0;

        public EntityId(int index, ushort version)
        {
            Index = index;
            Version = version;
        }

        public bool Equals(EntityId other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Version.GetHashCode();
            }
        }

        public int CompareTo(EntityId other)
        {
            int indexComparison = Index.CompareTo(other.Index);
            if (indexComparison != 0) return indexComparison;
            return Version.CompareTo(other.Version);
        }

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

        public override string ToString()
        {
            return IsValid ? $"Entity({Index}:{Version})" : "Entity(Invalid)";
        }
    }
}
