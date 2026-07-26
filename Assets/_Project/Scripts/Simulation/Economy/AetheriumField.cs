using Nova.Simulation.Pathfinding;

namespace Nova.Simulation.Economy
{
    /// <summary>
    /// A canonical Aetherium field: a fixed grid position with a finite
    /// remaining reserve in AE. Fields are referenced by a stable numeric id
    /// (uint16, 0 invalid — the identity model the Harvest payload already
    /// prescribes; the resolution of content field ids to these runtime ids
    /// is host setup and a Q-040 candidate).
    /// <para>
    /// G2 reservation (D-010): this slice models ONLY the finite reserve —
    /// no mother node, no regrowth, no spread, no overharvest damage and no
    /// depletion warning. A field at <see cref="RemainingAE"/> 0 is simply
    /// exhausted forever within this slice.
    /// </para>
    /// </summary>
    public struct AetheriumField
    {
        /// <summary>Stable field id (uint16, 0 invalid).</summary>
        public ushort FieldId;

        /// <summary>Fixed grid cell of the field; never moves within this slice.</summary>
        public GridPos2D GridPos;

        /// <summary>Remaining reserve in AE; monotonically decreasing, never negative.</summary>
        public long RemainingAE;

        /// <summary>True when the reserve is fully harvested (no regrowth in this slice — D-010 is G2).</summary>
        public bool IsExhausted => RemainingAE == 0;

        public AetheriumField(ushort fieldId, GridPos2D gridPos, long remainingAE)
        {
            FieldId = fieldId;
            GridPos = gridPos;
            RemainingAE = remainingAE;
        }
    }
}
