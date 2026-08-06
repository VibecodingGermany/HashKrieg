using System;

namespace Nova.Gameplay
{
    /// <summary>
    /// The build state one construction site can be in, in the exact order
    /// the simulation evaluates it every tick
    /// (<c>ConstructionSystem.ProgressSites</c>): no own Builder alive (the
    /// site pauses), the assigned Builder still outside the footprint's
    /// Chebyshev reach (the site pauses), or the Builder in reach (the site
    /// progresses).
    /// </summary>
    public enum SiteBuildState
    {
        NoBuilder = 0,
        BuilderEnRoute = 1,
        Building = 2,
    }

    /// <summary>
    /// Presentation mirror of the construction rules the HUD and the input
    /// layer must speak about but must never change. Pure C# (no UnityEngine),
    /// so EditMode tests pin every rule (ConstructionSiteStatusTests); the
    /// MonoBehaviours one layer up only render or dispatch what these
    /// functions return.
    /// <para>
    /// MIRRORED RULES (read from the sim, never re-interpreted):
    /// <list type="bullet">
    /// <item>Builder approach cell: the deterministic adjacent target cell
    /// the Skirmish AI walks its assigned Builder to (SkirmishAiSystem
    /// construction support) — west of the footprint
    /// (<c>originX - 1</c>), the east side
    /// (<c>originX + BuildingFootprintCells</c>) as the map-edge fallback,
    /// the footprint's middle row clamped into the grid. D-085 sends the
    /// player's Builder to the very same cell when a placement is
    /// dispatched.</item>
    /// <item>Footprint reach: the private Chebyshev rule of
    /// <c>ConstructionSystem.IsInReachOfFootprint</c> — the builder's cell is
    /// the 3x3 footprint rectangle or adjacent (&lt;= 1 per axis). Rebuilt
    /// here over plain cell ints because the sim's scan is private, the same
    /// precedent as <see cref="CommandCardPresenter.TryFindRepairBuilder"/>
    /// rebuilding the lowest-index-builder scan.</item>
    /// <item>Remaining build time: the site's <c>ProgressRaw</c> accumulates
    /// the owner's <c>ProductionSpeedMultiplierQ16</c> raw value per
    /// progressed tick against <c>BuildTicks &lt;&lt; 16</c> — the ETA the
    /// site card shows is computed from exactly those three numbers, not
    /// estimated.</item>
    /// </list>
    /// </para>
    /// <para>
    /// DETERMINISM: this class only READS and FORMATS. The auto-dispatch it
    /// supports travels over the normal Move command path — no new
    /// CommandKind, no state change, no baseline (D-085).
    /// </para>
    /// </summary>
    public static class ConstructionSiteStatus
    {
        /// <summary>The canonical lockstep rate: 10 simulation ticks per second.</summary>
        public const int TicksPerSecond = 10;

        /// <summary>German one-line warning for a site without any living own Builder (D-085; the cancel refund is 75%).</summary>
        public const string NoBuilderWarning = "Kein Builder — Bau pausiert. Builder im HQ bauen.";

        /// <summary>
        /// The deterministic adjacent cell a Builder is sent to so the site's
        /// reach rule is satisfied: west of the footprint, east as the
        /// map-edge fallback (both Chebyshev 1 of the rectangle), the
        /// footprint's middle row clamped into the grid. Mirrors the AI's
        /// construction support one to one, so the human auto-dispatch walks
        /// to the same cell the AI's would.
        /// </summary>
        public static void ApproachCell(
            int originX, int originY, int footprintCells, int gridSize,
            out int cellX, out int cellY)
        {
            cellX = originX - 1;
            if (cellX < 0) cellX = originX + footprintCells;
            cellY = Math.Max(0, Math.Min(gridSize - 1, originY + footprintCells / 2));
        }

        /// <summary>
        /// Chebyshev reach mirror: the unit's cell is the footprint rectangle
        /// or at most one cell away per axis — the same rule
        /// <c>ConstructionSystem.IsInReachOfFootprint</c> applies, restated
        /// over plain ints (the sim clamps negative unit cells to 0 before
        /// evaluating; callers do the same at the float boundary).
        /// </summary>
        public static bool IsInReachOfFootprint(
            int unitCellX, int unitCellY, int originX, int originY, int footprintCells)
        {
            int dx = Math.Max(0, Math.Max(originX - unitCellX, unitCellX - (originX + footprintCells - 1)));
            int dy = Math.Max(0, Math.Max(originY - unitCellY, unitCellY - (originY + footprintCells - 1)));
            return dx <= 1 && dy <= 1;
        }

        /// <summary>The site's build state in the sim's own evaluation order.</summary>
        public static SiteBuildState Evaluate(bool hasAssignedBuilder, bool builderInReach)
        {
            if (!hasAssignedBuilder) return SiteBuildState.NoBuilder;
            return builderInReach ? SiteBuildState.Building : SiteBuildState.BuilderEnRoute;
        }

        /// <summary>
        /// Progress as a 0..1 ratio: <c>ProgressRaw</c> is Q16.16 ticks
        /// against <c>BuildTicks &lt;&lt; 16</c>. Not clamped — callers that
        /// draw a bar clamp, callers that print a percentage floor.
        /// </summary>
        public static float Progress01(int progressRaw, int buildTicks)
        {
            if (buildTicks <= 0) return 1f;
            return progressRaw / (float)(buildTicks << 16);
        }

        /// <summary>
        /// Remaining build time in whole seconds, rounded up: the raw ticks
        /// left divided by the owner's production speed (Q16.16 raw per
        /// progressed tick — 1.0 at full power, exactly 0.5 under low power)
        /// at the canonical <see cref="TicksPerSecond"/>. Returns -1 when the
        /// speed is zero or negative, which the sim never produces (the
        /// multiplier is 1.0 or 0.5) but a formatter must never divide by.
        /// </summary>
        public static int RemainingSecondsCeil(int progressRaw, int buildTicks, int speedMultiplierRaw)
        {
            if (speedMultiplierRaw <= 0) return -1;
            long remainingRaw = ((long)buildTicks << 16) - progressRaw;
            if (remainingRaw <= 0) return 0;
            double remainingSeconds = remainingRaw / (double)speedMultiplierRaw / TicksPerSecond;
            return (int)Math.Ceiling(remainingSeconds);
        }

        /// <summary>
        /// The German status line of the site card, one per
        /// <see cref="SiteBuildState"/>: the pause reason spelled out while
        /// the site does not grow (a standing site must never look simply
        /// "broken"), progress plus ETA while it does.
        /// </summary>
        public static string StatusText(SiteBuildState state, float progress01, int remainingSeconds)
        {
            switch (state)
            {
                case SiteBuildState.NoBuilder:
                    return NoBuilderWarning;
                case SiteBuildState.BuilderEnRoute:
                    return "Builder unterwegs — Bau pausiert";
                default:
                    int percent = Math.Max(0, Math.Min(100, (int)(progress01 * 100f)));
                    return remainingSeconds >= 0
                        ? $"im Bau, {percent} % — fertig in ~{remainingSeconds} s"
                        : $"im Bau, {percent} %";
            }
        }
    }
}
