namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Canonical wire-level id validation for command payloads
    /// (docs/tech/SimulationCore.md section 1, docs/tech/Commands.md section 4).
    /// Payloads carry the packed uint32 entity layout of the spec: bits 0–9
    /// index, bits 10–31 generation, raw value 0 invalid, generation 0 invalid.
    /// The prototype <see cref="Core.EntityId"/> struct still stores index and
    /// version separately; migrating it is tracked as Q-040(e) and does not
    /// change this wire format.
    /// </summary>
    public static class CommandIds
    {
        /// <summary>True when the raw packed entity id is canonical (non-zero, generation ≥ 1).</summary>
        public static bool IsCanonicalEntityId(uint rawEntityId)
        {
            return rawEntityId != 0 && (rawEntityId >> 10) != 0;
        }

        /// <summary>True when the raw definition id is valid (raw value 0 is invalid per spec).</summary>
        public static bool IsValidDefinitionId(ushort rawDefinitionId)
        {
            return rawDefinitionId != 0;
        }

        /// <summary>
        /// True when the list is non-empty, within
        /// <see cref="CommandLimits.MaxEntityIdsPerCommand"/>, strictly ascending
        /// (duplicates are a structural error, not silently deduplicated) and
        /// every entry is a canonical entity id.
        /// </summary>
        public static bool IsCanonicalEntityList(uint[] entityIds)
        {
            if (entityIds == null || entityIds.Length == 0) return false;
            if (entityIds.Length > CommandLimits.MaxEntityIdsPerCommand) return false;
            uint previous = 0;
            for (int i = 0; i < entityIds.Length; i++)
            {
                if (!IsCanonicalEntityId(entityIds[i])) return false;
                if (i > 0 && entityIds[i] <= previous) return false;
                previous = entityIds[i];
            }
            return true;
        }
    }
}
