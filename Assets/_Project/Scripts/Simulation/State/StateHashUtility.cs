using System;
using Nova.Core;

namespace Nova.Simulation.State
{
    /// <summary>
    /// Computes bit-exact 64-bit state hashes across active entities for lockstep multiplayer desync detection.
    /// Uses FNV-1a 64-bit hashing algorithm.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public static class StateHashUtility
    {
        private const ulong FNVOffsetBasis = 14695981039346656037UL;
        private const ulong FNVPrime = 1099511628211UL;

        public static ulong CalculateStateHash(EntityManager entities, ulong currentTick)
        {
            if (entities == null) return 0UL;

            ulong hash = FNVOffsetBasis;
            hash = CombineHash(hash, currentTick);

            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive) continue;

                hash = CombineHash(hash, (ulong)u.Id.Index);
                hash = CombineHash(hash, (ulong)u.Id.Version);
                hash = CombineHash(hash, (ulong)u.PlayerId);

                // Convert float bits to ulong for deterministic hashing
                uint xBits = SimMath.SingleToUInt32Bits(u.Transform.PositionX);
                uint yBits = SimMath.SingleToUInt32Bits(u.Transform.PositionY);
                uint rotBits = SimMath.SingleToUInt32Bits(u.Transform.Rotation);

                hash = CombineHash(hash, xBits);
                hash = CombineHash(hash, yBits);
                hash = CombineHash(hash, rotBits);
                hash = CombineHash(hash, (ulong)u.CurrentHealth);
            }

            return hash;
        }

        private static ulong CombineHash(ulong currentHash, ulong value)
        {
            unchecked
            {
                currentHash ^= value;
                currentHash *= FNVPrime;
                return currentHash;
            }
        }
    }
}
