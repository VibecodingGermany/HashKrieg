using UnityEngine;
using Nova.Simulation.State;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Graybox faction tinting: FACTION owns the colour channel of a unit
    /// view (the <see cref="UnitRole"/> keeps the shape channel), taken from
    /// the ratified MS-1 palettes of DecisionLog D-072 — Alliance base tone
    /// <c>#8A9199</c> (Stahlgrau), Legion base tone <c>#7A3524</c> (Rostrot).
    /// The secondary and accent tones of D-072 (Allianz #2C6E9E/#4FD8FF,
    /// Legion #B08430/#2B2018) stay art-side content; the graybox needs
    /// exactly one unambiguous tone per faction.
    /// <para>
    /// No ScriptableObjects, no .asset files, no materials: two constants
    /// and a <see cref="MaterialPropertyBlock"/> writer, so the whole thing
    /// deletes in one commit once real art lands. The block writer sets BOTH
    /// colour properties — built-in RP reads <c>_Color</c>, URP reads
    /// <c>_BaseColor</c> — so the tint survives the pipeline migration.
    /// </para>
    /// </summary>
    public static class FactionTint
    {
        /// <summary>Alliance base tone of the D-072 palette (#8A9199).</summary>
        public static readonly Color AllianceBase = ParseHex("#8A9199");

        /// <summary>Legion base tone of the D-072 palette (#7A3524).</summary>
        public static readonly Color LegionBase = ParseHex("#7A3524");

        /// <summary>
        /// High-albedo tint for textured 3D Alliance prefabs: keeps the
        /// authored BaseColor map bright and legible while adding a subtle
        /// cool steel-blue grade.
        /// </summary>
        public static readonly Color AlliancePrefabTint = new Color(0.94f, 0.97f, 1.00f, 1f);

        /// <summary>
        /// High-albedo tint for textured 3D Legion prefabs: keeps the
        /// authored BaseColor map bright and legible while adding a subtle
        /// warm industrial-amber grade instead of crushing green/blue channels.
        /// </summary>
        public static readonly Color LegionPrefabTint = new Color(1.00f, 0.93f, 0.87f, 1f);

        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        /// <summary>
        /// The graybox tint of a faction: its D-072 base tone. Any undeclared
        /// faction value reads as Alliance rather than throwing — a view must
        /// never take down the frame over a cosmetic colour.
        /// </summary>
        public static Color BaseColor(FactionId faction)
        {
            return faction == FactionId.Legion ? LegionBase : AllianceBase;
        }

        /// <summary>
        /// High-albedo tint for a 3D prefab view whose material already carries
        /// a faction-authored BaseColor texture.
        /// </summary>
        public static Color PrefabBaseColor(FactionId faction)
        {
            return faction == FactionId.Legion ? LegionPrefabTint : AlliancePrefabTint;
        }

        /// <summary>
        /// Writes the tint into the property block under BOTH shader colour
        /// properties (<c>_BaseColor</c> and <c>_Color</c>). The block is
        /// cleared by the caller when it batches several writes.
        /// </summary>
        public static void ApplyToPropertyBlock(MaterialPropertyBlock block, Color color)
        {
            block.SetColor(BaseColorPropertyId, color);
            block.SetColor(ColorPropertyId, color);
        }

        private static Color ParseHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
        }
    }
}
