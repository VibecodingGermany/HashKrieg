using System;
using Nova.Simulation.Pathfinding;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// GLUTRINNE TERRAIN (21.7, #94, D-109) — the single authoritative source
    /// of the canonical map's natural terrain: a square rock ring around the
    /// centre zone with four diagonal corner gaps. BOTH sides of the map read
    /// this one table: the simulation is fed through
    /// <see cref="CostField.SetCost"/> (applied by <see cref="MatchBootstrap"/>
    /// when the opening is built), and the presentation layer
    /// (Presentation/Maps/GlutrinneBlockoutView — a cref would point against
    /// the assembly reference direction) renders exactly these cells as the
    /// rock ridge. Two separate sources would produce units walking through
    /// rocks and sticking on invisible walls — the failure mode this package
    /// exists to prevent.
    /// <para>
    /// Geometry. The wall is the Chebyshev ring
    /// <see cref="RingInnerRadius"/>..<see cref="RingOuterRadius"/> around the
    /// centre cell (62,62). TWO cells thick — as a deliberate choice, NOT as a
    /// pathfinding necessity. One shell would already be impassable: a king
    /// move changes each coordinate by at most 1, so it changes
    /// max(|dx|, |dy|) by at most 1 and can never step from inside (r-1) to
    /// outside (r+1) across a closed Chebyshev shell. Corner cutting does not
    /// help either, because the step still has to land ON a shell cell.
    /// (Checked by flooding the closed ring with king moves in both
    /// thicknesses: neither leaks.) The second shell buys two other things:
    /// it gives each corner gap an actual DEPTH, so the opening reads as a
    /// passage instead of a slit, and it gives the rock ridge enough visual
    /// mass that a player sees the wall before walking into it. Do not thin
    /// it to one shell to save cells — that would move
    /// <see cref="ImpassableCellCount"/> (168 -> 84) and every pinned
    /// checksum with it. The four corner gaps open
    /// exactly onto the diagonal approach lanes — the two start bases (SW/NE)
    /// and the two contested far flanks (NW/SE). At the throat (the inner
    /// shell) each gap is four cells wide: the MS-1 squad gate is six units
    /// at the 0.5-cell default radius (UnitState), and four cells pass them
    /// four abreast — a narrower cut would serialize the group into single
    /// file, which is a blockade, not a chokepoint. D-107 point symmetry holds by construction: the
    /// predicate reads only |x−62| and |y−62|, so every blocked cell's mirror
    /// (x, y) -&gt; (124 − x, 124 − y) is blocked too.
    /// </para>
    /// <para>
    /// Snapshot contract (the load-bearing part). The cost field is NOT a
    /// snapshot block; the restore proof is structural — footprint content is
    /// replayed by the construction block, and the serialized epoch is
    /// ADOPTED via <see cref="CostField.RestoreEpoch"/> because a mutation
    /// counter cannot be replayed. Static terrain therefore has to be written
    /// on EVERY host of the canonical match, identically, BEFORE the first
    /// snapshot: local and relay hosts through <see cref="MatchBootstrap"/>,
    /// the headless generator AND playback hosts through
    /// Determinism10000Scenario.BuildHost — the playback host never runs the
    /// scenario's SetupMatch, so terrain could not live in the setup pass.
    /// The write count is fixed (<see cref="ImpassableCellCount"/>) and the
    /// write order is fixed (y, then x, ascending), so the epoch lands on the
    /// same value everywhere and later construction mutations keep counting
    /// in lockstep after a restore.
    /// </para>
    /// <para>
    /// The headless lane cannot reference this assembly (tools/Nova.SimRunner
    /// compiles Core/Simulation/Networking/AI only — a frozen boundary), so
    /// Determinism10000Scenario carries a hand-mirrored copy of this
    /// predicate, exactly like the field layout mirror (R-1). The mirror is
    /// pinned cell-exact and by a shared FNV-1a content checksum in
    /// tools/Nova.SimRunner.Tests (GlutrinneTerrainTests) and in the EditMode
    /// CanonicalMatchSetupTests — an unpinned mirror here would be a defect,
    /// not a compromise.
    /// </para>
    /// </summary>
    public static class GlutrinneTerrainMap
    {
        /// <summary>Centre cell of the map and the ring (the D-107 self-mirror point).</summary>
        public const int CentreX = 62;
        public const int CentreY = 62;

        /// <summary>Chebyshev distance of the wall's inner face from the centre.</summary>
        public const int RingInnerRadius = 14;

        /// <summary>Chebyshev distance of the wall's outer face — the second shell is gap depth and visual mass, not tightness; see the class remarks.</summary>
        public const int RingOuterRadius = 15;

        /// <summary>
        /// Corner gaps: ring cells with min(|dx|, |dy|) at or above this stay
        /// open. 11 opens the inner shell on exactly four cells per corner
        /// (min in 11..14) — the pinned throat width.
        /// </summary>
        public const int CornerGapMinRadius = 11;

        /// <summary>
        /// Blocked cell count of the canonical 128x128 map, as arithmetic:
        /// the ring band holds (2*15+1)^2 - (2*13+1)^2 = 232 cells, the four
        /// corner gaps keep 16 open each (pairs (a, b) in 11..15 with
        /// max(a, b) &gt;= 14), so 232 - 64 = 168. Both terrain tests pin
        /// <see cref="Apply"/> against this number.
        /// </summary>
        public const int ImpassableCellCount = 168;

        /// <summary>
        /// The terrain predicate. Pure integer math — this feeds the
        /// simulation, so no float and no randomness of any kind.
        /// </summary>
        public static bool IsImpassable(int x, int y)
        {
            int dx = Math.Abs(x - CentreX);
            int dy = Math.Abs(y - CentreY);
            int ring = Math.Max(dx, dy);
            if (ring < RingInnerRadius || ring > RingOuterRadius)
            {
                return false;
            }
            return Math.Min(dx, dy) < CornerGapMinRadius;
        }

        /// <summary>
        /// Writes every blocked cell into the cost field in the canonical
        /// order (y, then x, ascending) and returns the write count. The loop
        /// is bounded by the field's own dimensions, so no write can fall out
        /// of bounds (an out-of-bounds SetCost would silently not count
        /// against the epoch — the count check at the call site exists to
        /// make that impossible to miss). Callers on the canonical 128x128
        /// map must see exactly <see cref="ImpassableCellCount"/>.
        /// </summary>
        public static int Apply(CostField costField)
        {
            if (costField == null) throw new ArgumentNullException(nameof(costField));
            int written = 0;
            for (int y = 0; y < costField.Height; y++)
            {
                for (int x = 0; x < costField.Width; x++)
                {
                    if (!IsImpassable(x, y)) continue;
                    costField.SetCost((ushort)x, (ushort)y, CostField.ImpassableCost);
                    written++;
                }
            }
            return written;
        }
    }
}
