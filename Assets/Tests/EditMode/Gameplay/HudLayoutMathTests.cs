using NUnit.Framework;
using Nova.Gameplay;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for <see cref="HudLayoutMath"/> (the D-085 zone model):
    /// the docking relations between the five HUD zones and the no-overlap
    /// invariant at representative window sizes. The panels draw exactly
    /// these rects (via HudLayout, Nova.Presentation.UI), so a regression
    /// here is an overlap on screen.
    /// </summary>
    [TestFixture]
    public class HudLayoutMathTests
    {
        // Canonical graybox metrics (the panels' serialized defaults).
        private const float Margin = 8f;
        private const float StripHeight = 19f;    // DebugHud font 13 + 6
        private const float MapSize = 168f;       // MinimapHud
        private const float BarReserve = 100f;    // BuildMenuHud: status 26 + gap 4 + buttons 62 + margin 8
        private const float CardWidth = 236f;     // CommandCardHud

        [Test]
        public void StatusStrip_SpansTheTopEdgeMinusMargins()
        {
            HudRect strip = HudLayoutMath.StatusStrip(1280f, Margin, StripHeight);

            Assert.AreEqual(Margin, strip.X);
            Assert.AreEqual(Margin, strip.Y);
            Assert.AreEqual(1280f - 2f * Margin, strip.Width);
            Assert.AreEqual(StripHeight, strip.Height);
        }

        [Test]
        public void BottomCenterZone_IsCenteredAndHugsTheBottomMargin()
        {
            HudRect bar = HudLayoutMath.BottomCenterZone(1280f, 720f, 1004f, 92f, Margin);

            Assert.AreEqual((1280f - 1004f) * 0.5f, bar.X, 1e-4f);
            Assert.AreEqual(720f - Margin, bar.Bottom, 1e-4f);
            Assert.AreEqual(92f, bar.Height);
        }

        [Test]
        public void BottomLeftZone_DocksOneMarginAboveTheBarReserve()
        {
            HudRect bar = HudLayoutMath.BottomCenterZone(1280f, 720f, 1004f, 92f, Margin);
            HudRect map = HudLayoutMath.BottomLeftZoneAbove(720f, MapSize, Margin, BarReserve);

            Assert.AreEqual(Margin, map.X);
            Assert.AreEqual(bar.Y - Margin, map.Bottom, 1e-4f); // exactly one margin of air over the bar zone
        }

        [Test]
        public void BottomRightZone_SharesTheRightMarginAndTheBarDocking()
        {
            HudRect bar = HudLayoutMath.BottomCenterZone(1280f, 720f, 1004f, 92f, Margin);
            HudRect card = HudLayoutMath.BottomRightZoneAbove(1280f, 720f, CardWidth, 300f, Margin, BarReserve);

            Assert.AreEqual(1280f - Margin, card.Right, 1e-4f);
            Assert.AreEqual(bar.Y - Margin, card.Bottom, 1e-4f);
        }

        [Test]
        public void FreeFieldZone_IsBoundedByStripBottomLeftZoneAndCardColumn()
        {
            HudRect map = HudLayoutMath.BottomLeftZoneAbove(720f, MapSize, Margin, BarReserve);
            HudRect card = HudLayoutMath.BottomRightZoneAbove(1280f, 720f, CardWidth, 300f, Margin, BarReserve);
            float top = Margin + StripHeight + 4f;
            HudRect free = HudLayoutMath.FreeFieldZone(1280f, 640f, Margin, top, map.Y, card.X);

            Assert.AreEqual(Margin, free.X);
            Assert.AreEqual(top, free.Y);
            Assert.AreEqual(640f, free.Width); // the cap wins over the card column at this width
            Assert.AreEqual(map.Y - Margin, free.Bottom, 1e-4f);
        }

        [Test]
        public void FreeFieldZone_NarrowWindow_ClearsTheCardColumn()
        {
            HudRect map = HudLayoutMath.BottomLeftZoneAbove(512f, MapSize, Margin, BarReserve);
            HudRect card = HudLayoutMath.BottomRightZoneAbove(683f, 512f, CardWidth, 300f, Margin, BarReserve);
            HudRect free = HudLayoutMath.FreeFieldZone(683f, 640f, Margin, 31f, map.Y, card.X);

            Assert.AreEqual(card.X - Margin, free.Right, 1e-4f); // never enters the card's column
            Assert.IsFalse(free.Overlaps(card));
        }

        [Test]
        public void AllFiveZones_NeverOverlap_AtRepresentativeWindowSizes()
        {
            foreach ((float w, float h) in new[] { (1280f, 720f), (960f, 600f), (1707f, 960f), (683f, 512f) })
            {
                HudRect strip = HudLayoutMath.StatusStrip(w, Margin, StripHeight);
                HudRect bar = HudLayoutMath.BottomCenterZone(w, h, 1004f, 92f, Margin);
                HudRect map = HudLayoutMath.BottomLeftZoneAbove(h, MapSize, Margin, BarReserve);
                HudRect card = HudLayoutMath.BottomRightZoneAbove(w, h, CardWidth, 300f, Margin, BarReserve);
                HudRect free = HudLayoutMath.FreeFieldZone(w, 640f, Margin, strip.Bottom + 4f, map.Y, card.X);

                HudRect[] zones = { strip, bar, map, card, free };
                for (int a = 0; a < zones.Length; a++)
                {
                    for (int b = a + 1; b < zones.Length; b++)
                    {
                        Assert.IsFalse(
                            zones[a].Overlaps(zones[b]),
                            $"{w}x{h}: zone {a} ({zones[a]}) overlaps zone {b} ({zones[b]})");
                    }
                }
            }
        }

        [Test]
        public void TinyWindow_ClampsDockedZonesOntoTheScreen()
        {
            HudRect map = HudLayoutMath.BottomLeftZoneAbove(120f, MapSize, Margin, BarReserve);
            HudRect card = HudLayoutMath.BottomRightZoneAbove(300f, 120f, CardWidth, 300f, Margin, BarReserve);
            HudRect free = HudLayoutMath.FreeFieldZone(300f, 640f, Margin, 31f, map.Y, card.X);

            Assert.GreaterOrEqual(map.Y, Margin);
            Assert.GreaterOrEqual(card.Y, Margin);
            Assert.AreEqual(0f, free.Height); // degenerate: the panel skips drawing instead of overlapping
        }
    }
}
