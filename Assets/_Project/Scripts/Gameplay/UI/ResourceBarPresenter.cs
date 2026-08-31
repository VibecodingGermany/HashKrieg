using System;
using System.Text;

namespace Nova.Gameplay
{
    /// <summary>
    /// Where the Aetherium balance stands against the slot's DERIVED storage
    /// ceiling (16.4, #53, D-024): below it (normal), exactly at it (fresh
    /// income forfeits — the player must build storage) or above it (the
    /// excess decays once per second — the #131 opening situation and the
    /// D-106 destruction case). The three-way split exists because the two
    /// warning situations carry DIFFERENT truths: "at" loses future income,
    /// "above" actively burns the existing balance.
    /// </summary>
    public enum StorageCeilingState
    {
        BelowCeiling = 0,
        AtCeiling = 1,
        AboveCeiling = 2,
    }

    /// <summary>
    /// The one-line resource bar's view model for a single frame: the two
    /// value segments (always present), the optional warning segment and the
    /// state flags the IMGUI component one layer up turns into colours. Built
    /// once per frame by <see cref="ResourceBarPresenter.BuildModel"/>; the
    /// component renders it without any further decisions.
    /// </summary>
    public struct ResourceBarModel
    {
        /// <summary>"Aetherium 2.318 / 3.000" — balance AND ceiling, German thousands grouping.</summary>
        public string AetheriumText;

        /// <summary>"Strom 130/80" — provided against required, the DebugHud/FormatPowerBalance convention.</summary>
        public string PowerText;

        /// <summary>The warning segment, or null when nothing needs acting on. Both warnings join with <see cref="ResourceBarPresenter.SegmentSeparator"/>, storage first.</summary>
        public string WarningText;

        /// <summary>The storage situation behind <see cref="WarningText"/>.</summary>
        public StorageCeilingState StorageState;

        /// <summary>Mirror of the sim's IsLowPower (required &gt; provided); colours the power segment.</summary>
        public bool IsLowPower;

        /// <summary>
        /// Severity of <see cref="WarningText"/>: false renders it amber (a
        /// full store — income stops, nothing burns yet), true renders it red
        /// (an overflow decaying per second or a power deficit already taxing
        /// production, repair and radar).
        /// </summary>
        public bool IsCritical;
    }

    /// <summary>
    /// The testable brain of the resource bar (issue #137): maps the local
    /// slot's economy readout — credits, the DERIVED storage ceiling and the
    /// power balance — to the one line the bar shows, so the IMGUI component
    /// one layer up (<c>ResourceBarHud</c>, Nova.Presentation.UI) only renders
    /// and never decides. Plain Unity-free class on purpose, the same
    /// precedent as <see cref="CommandCardPresenter"/>: no MonoBehaviour, no
    /// UnityEngine types, so EditMode tests cover the whole mapping.
    /// <para>
    /// THE CEILING IS ALWAYS AN INPUT, NEVER A CONSTANT. The bar's whole
    /// point is the pair "balance / ceiling", and the ceiling is
    /// <c>EconomySystem.CapacityFor(slot)</c> — derived from the living
    /// building stock on every read (a completed HQ provides the base, every
    /// completed Storage adds its bonus, a destroyed one drops it). The
    /// presenter takes it as a parameter so a rule change (e.g. the HQ base
    /// capacity rising to 3.000) cannot strand a stale number in the UI.
    /// </para>
    /// <para>
    /// WHAT THE PLAYER NEEDS, NOT EVERY INTERNAL NUMBER: the power segment
    /// shows provided against required — the planning pair — and reserves the
    /// deficit's CONSEQUENCES for the warning segment, where they name what
    /// the sim actually does under low power: production runs at
    /// <c>PlayerEconomyState.ProductionSpeedMultiplierQ16</c> (exact ½),
    /// repair at <c>ConstructionSystem.LowPowerRepairRateHpPerTick</c>
    /// (5 instead of 10 HP/tick) and the radar/minimap goes dark
    /// (<c>FogOfWarSystem</c>'s low-power early-out, 16.6 C4). Free headroom
    /// is the pair's subtraction and stays implicit — the build bar's hover
    /// already names it where a building is actually placed.
    /// </para>
    /// <para>
    /// Culture independence: the German grouping dots are assembled digit by
    /// digit (same algorithm as <see cref="CommandCardPresenter"/>'s field
    /// reserve line), so the output is byte-identical under ANY ambient
    /// culture — a build on an en-US host must not render "2,318".
    /// </para>
    /// </summary>
    public static class ResourceBarPresenter
    {
        /// <summary>The separator between the bar's segments (and between two simultaneous warnings) — the DebugHud status strip's convention.</summary>
        public const string SegmentSeparator = "   |   ";

        /// <summary>Warning while the balance sits exactly ON the ceiling: fresh income forfeits until storage is built (amber — nothing burns yet).</summary>
        public const string StorageFullWarning = "Lager voll — Einnahmen verfallen";

        /// <summary>Warning while the balance sits ABOVE the ceiling: the excess decays 25%/s (D-024/D-106), so the warning names the action (red).</summary>
        public const string StorageOverflowWarning = "Überschuss verfällt — Lager bauen!";

        /// <summary>Warning under low power, naming the three live consequences so the rule stops being invisible (16.6 C4, Sprint-16 production factor).</summary>
        public const string LowPowerWarning = "Strommangel — Produktion ½ · Reparatur ½ · Radar aus";

        /// <summary>
        /// The storage situation, with the two edges the account can sit on:
        /// <see cref="StorageCeilingState.AtCeiling"/> only when a ceiling
        /// actually exists (capacity &gt; 0) — "0 / 0" is a slot without any
        /// account building, not a full store, and must not warn. A balance
        /// above the ceiling is always <see cref="StorageCeilingState.AboveCeiling"/>,
        /// including capacity 0 (no completed HQ: everything is excess).
        /// Negative inputs are clamped to zero — the sim never produces them,
        /// but a presentation function defends its own contract.
        /// </summary>
        public static StorageCeilingState EvaluateStorageState(long credits, long capacity)
        {
            if (credits < 0) credits = 0;
            if (capacity < 0) capacity = 0;
            if (credits > capacity) return StorageCeilingState.AboveCeiling;
            if (credits == capacity && capacity > 0) return StorageCeilingState.AtCeiling;
            return StorageCeilingState.BelowCeiling;
        }

        /// <summary>
        /// The whole line's model from the slot's raw economy readout. The
        /// low-power flag mirrors the sim's own rule EXACTLY
        /// (<c>PlayerEconomyState.IsLowPower</c>: required &gt; provided —
        /// an exactly-balanced grid is NOT a deficit). The warning joins both
        /// active warnings storage-first: the overflow is the one bleeding
        /// resources per second, so it leads.
        /// </summary>
        public static ResourceBarModel BuildModel(
            long credits, long capacity, int powerProvided, int powerRequired)
        {
            StorageCeilingState storageState = EvaluateStorageState(credits, capacity);
            bool isLowPower = powerRequired > powerProvided;

            string warning = null;
            if (storageState == StorageCeilingState.AtCeiling)
            {
                warning = StorageFullWarning;
            }
            else if (storageState == StorageCeilingState.AboveCeiling)
            {
                warning = StorageOverflowWarning;
            }
            if (isLowPower)
            {
                warning = warning == null
                    ? LowPowerWarning
                    : warning + SegmentSeparator + LowPowerWarning;
            }

            return new ResourceBarModel
            {
                AetheriumText = FormatAetherium(credits, capacity),
                PowerText = FormatPower(powerProvided, powerRequired),
                WarningText = warning,
                StorageState = storageState,
                IsLowPower = isLowPower,
                // Amber is reserved for the single "full store" state; any
                // active penalty (decay or deficit) escalates to red.
                IsCritical = storageState == StorageCeilingState.AboveCeiling || isLowPower,
            };
        }

        /// <summary>
        /// The Aetherium segment: "Aetherium 2.318 / 3.000" — balance AND
        /// ceiling, because a bare "2.318" says nothing (issue #137) and the
        /// "2.318 / 3.000" pair reads at a glance. German thousands grouping,
        /// assembled digit by digit (culture-independent, see class remarks).
        /// </summary>
        public static string FormatAetherium(long credits, long capacity)
        {
            var builder = new StringBuilder(32);
            builder.Append("Aetherium ");
            AppendGroupedDe(builder, credits);
            builder.Append(" / ");
            AppendGroupedDe(builder, capacity);
            return builder.ToString();
        }

        /// <summary>
        /// The power segment: "Strom 130/80" — provided against required, the
        /// convention of <see cref="CommandCardPresenter.FormatPowerBalance"/>
        /// and the DebugHud status strip, so the same grid reads identically
        /// on every surface.
        /// </summary>
        public static string FormatPower(int powerProvided, int powerRequired)
        {
            return $"Strom {powerProvided}/{powerRequired}";
        }

        /// <summary>
        /// The bar's zone: right-docked in the top screen area, below the
        /// status strip. Content-sized, clamped to the width between the side
        /// margins — on a window too narrow for the content the bar grows
        /// leftward from the right margin instead of running off the edge.
        /// Pure math in the <see cref="HudRect"/> currency, converted to
        /// <c>UnityEngine.Rect</c> at the IMGUI boundary (the HudLayoutMath
        /// precedent).
        /// </summary>
        public static HudRect TopRightZone(
            float screenWidth, float top, float contentWidth, float height, float margin)
        {
            if (margin < 0f) margin = 0f;
            float available = Math.Max(0f, screenWidth - 2f * margin);
            float width = Math.Min(Math.Max(0f, contentWidth), available);
            return new HudRect(
                margin + Math.Max(0f, available - width),
                Math.Max(0f, top),
                width,
                Math.Max(0f, height));
        }

        /// <summary>Decimal digits with the German '.' group separator; balances are never negative, so a non-positive input renders as "0".</summary>
        private static void AppendGroupedDe(StringBuilder builder, long value)
        {
            if (value <= 0)
            {
                builder.Append('0');
                return;
            }

            int digitCount = 1;
            for (long rest = value; rest >= 10; rest /= 10) digitCount++;
            for (int i = 0; i < digitCount; i++)
            {
                if (i > 0 && (digitCount - i) % 3 == 0) builder.Append('.');
                long divisor = 1;
                for (int d = 1; d < digitCount - i; d++) divisor *= 10;
                builder.Append((char)('0' + (int)(value / divisor % 10)));
            }
        }
    }
}
