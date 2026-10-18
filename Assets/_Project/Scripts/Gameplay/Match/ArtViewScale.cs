using UnityEngine;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// The one rule that turns an art model's authored size into its size on
    /// the map (D-113). Art is authored in metres with one grid cell = 3 m
    /// (ArtAssetStandard.md §1), so the ratios BETWEEN models are already
    /// right; the view only has to pick one factor per class:
    /// <list type="bullet">
    /// <item><b>Buildings</b> shrink to their footprint width
    /// (<see cref="SimDefinitions.BuildingFootprintCells"/>); smaller art keeps
    /// its authored size.</item>
    /// <item><b>Units</b> share one metric factor,
    /// <see cref="UnitCellsPerMetre"/>: a 7.5 m battle tank becomes 1.65
    /// cells, a 4.5 m scout 1.0. The former rule squeezed every unit to its
    /// graybox primitive, which made a harvester shorter than a light tank, a
    /// builder smaller than a soldier, and a battle tank twice a soldier's
    /// height and three quarters of a barracks wide.</item>
    /// <item><b>Small units</b> get a minimum view HEIGHT
    /// (<see cref="MinViewHeight"/>): at true scale a soldier would be half a
    /// cell tall and unreadable from the RTS camera. Height, not width — a
    /// launcher held sideways must not shrink its soldier.</item>
    /// </list>
    /// </summary>
    public static class ArtViewScale
    {
        /// <summary>
        /// Cells per metre for unit art. Buildings sit at 1/3 (3 m per cell);
        /// vehicles run a third smaller so a tank reads at about a soldier and
        /// a half (real: 1.3x) and half a building's width, and neighbours at
        /// a collision radius of half a cell overlap less.
        /// </summary>
        public const float UnitCellsPerMetre = 0.22f;

        /// <summary>Uniform scale for a model of <paramref name="authoredSize"/> (world bounds at scale 1).</summary>
        public static float Factor(UnitRole role, Vector3 authoredSize)
        {
            if (SimDefinitions.IsBuildingRole(role))
            {
                float target = SimDefinitions.BuildingFootprintCells;
                float current = Mathf.Max(authoredSize.x, authoredSize.z);
                return current > target && current > 1e-4f ? target / current : 1f;
            }

            float factor = UnitCellsPerMetre;
            float minHeight = MinViewHeight(role);
            if (authoredSize.y > 1e-4f && authoredSize.y * factor < minHeight)
            {
                factor = minHeight / authoredSize.y;
            }
            return factor;
        }

        /// <summary>
        /// <see cref="Factor"/> by role NAME (the suffix of a PF_ prefab), for
        /// editor tooling that does not reference the simulation assembly.
        /// False for a name that is no <see cref="UnitRole"/>.
        /// </summary>
        public static bool TryFactor(string roleName, Vector3 authoredSize, out float factor)
        {
            factor = 1f;
            if (!System.Enum.TryParse(roleName, out UnitRole role)) return false;
            factor = Factor(role, authoredSize);
            return true;
        }

        /// <summary>Readability floor for small units, in cells of view height (0 = none).</summary>
        public static float MinViewHeight(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.BasicInfantry: return 0.85f;
                case UnitRole.AntiArmorInfantry: return 0.9f;
                case UnitRole.Builder: return 1.1f;
                default: return 0f;
            }
        }
    }
}
