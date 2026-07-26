using NUnit.Framework;
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Faction tint suite (EditMode lane): the graybox colour channel belongs
    /// to the FACTION (D-072 palettes) — Alliance and Legion map to their
    /// ratified, distinct base tones, and the property-block writer sets BOTH
    /// shader colour properties so the tint survives the built-in/URP
    /// migration. Pure logic level: no scene, no renderer.
    /// </summary>
    [TestFixture]
    public class FactionTintTests
    {
        [Test]
        public void BaseColor_MapsEachFactionToItsD072BaseTone()
        {
            Assert.That(ColorUtility.TryParseHtmlString("#8A9199", out Color alliance), Is.True);
            Assert.That(ColorUtility.TryParseHtmlString("#7A3524", out Color legion), Is.True);

            Assert.That(FactionTint.BaseColor(FactionId.Alliance), Is.EqualTo(alliance),
                "Alliance tint is the D-072 Alliance base tone");
            Assert.That(FactionTint.BaseColor(FactionId.Legion), Is.EqualTo(legion),
                "Legion tint is the D-072 Legion base tone");
            Assert.That(FactionTint.BaseColor(FactionId.Alliance),
                Is.Not.EqualTo(FactionTint.BaseColor(FactionId.Legion)),
                "the factions must read apart at a glance");
        }

        [Test]
        public void BaseColor_UndeclaredFaction_FallsBackWithoutThrowing()
        {
            Assert.DoesNotThrow(() => FactionTint.BaseColor((FactionId)7),
                "a view must never take down the frame over a cosmetic colour");
        }

        [Test]
        public void ApplyToPropertyBlock_SetsBothShaderColourProperties()
        {
            var block = new MaterialPropertyBlock();
            Color tint = FactionTint.BaseColor(FactionId.Legion);
            FactionTint.ApplyToPropertyBlock(block, tint);

            Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(tint),
                "URP reads _BaseColor");
            Assert.That(block.GetColor("_Color"), Is.EqualTo(tint),
                "built-in RP reads _Color");
        }
    }
}
