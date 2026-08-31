using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Nova.Gameplay;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for <see cref="ResourceBarPresenter"/>, the pure brain
    /// of the resource bar (issue #137): the three-way storage state against
    /// the derived ceiling, the composed one-line model (value pairs, warning
    /// text, severity), the culture-independent German number grouping and
    /// the right-docked zone math. The bar's whole contract is pinned here
    /// because the IMGUI component above it makes no decisions of its own.
    /// </summary>
    [TestFixture]
    public class ResourceBarPresenterTests
    {
        // ----------------------------------------------------------------
        // Storage state
        // ----------------------------------------------------------------

        [Test]
        public void EvaluateStorageState_BalanceBelowCeiling_IsNormal()
        {
            Assert.AreEqual(
                StorageCeilingState.BelowCeiling,
                ResourceBarPresenter.EvaluateStorageState(2318, 3000));
        }

        [Test]
        public void EvaluateStorageState_BalanceExactlyOnCeiling_IsAtCeiling()
        {
            // The canonical post-change opening: 3.000 start against a 3.000
            // HQ ceiling — fresh income forfeits from the first deposit.
            Assert.AreEqual(
                StorageCeilingState.AtCeiling,
                ResourceBarPresenter.EvaluateStorageState(3000, 3000));
        }

        [Test]
        public void EvaluateStorageState_BalanceAboveCeiling_IsAboveCeiling()
        {
            // The #131 opening: 3.000 start against the old 2.000 HQ ceiling —
            // the excess decays per second and the bar must say why.
            Assert.AreEqual(
                StorageCeilingState.AboveCeiling,
                ResourceBarPresenter.EvaluateStorageState(3000, 2000));
        }

        [Test]
        public void EvaluateStorageState_ZeroAgainstZero_IsNormalNotFull()
        {
            // A slot without any account building has no ceiling and no
            // balance — "0 / 0" must not read as a full store.
            Assert.AreEqual(
                StorageCeilingState.BelowCeiling,
                ResourceBarPresenter.EvaluateStorageState(0, 0));
        }

        [Test]
        public void EvaluateStorageState_BalanceWithoutAnyCeiling_IsAboveCeiling()
        {
            // No completed HQ: the derived ceiling is 0, so every credit is
            // excess and decays.
            Assert.AreEqual(
                StorageCeilingState.AboveCeiling,
                ResourceBarPresenter.EvaluateStorageState(500, 0));
        }

        [Test]
        public void EvaluateStorageState_NegativeInputs_AreClampedToZero()
        {
            Assert.AreEqual(
                StorageCeilingState.BelowCeiling,
                ResourceBarPresenter.EvaluateStorageState(-5, -10));
            Assert.AreEqual(
                StorageCeilingState.AboveCeiling,
                ResourceBarPresenter.EvaluateStorageState(5, -10));
        }

        // ----------------------------------------------------------------
        // Formatting
        // ----------------------------------------------------------------

        [Test]
        public void FormatAetherium_ShowsBalanceAndCeilingWithGermanGrouping()
        {
            Assert.AreEqual(
                "Aetherium 2.318 / 3.000",
                ResourceBarPresenter.FormatAetherium(2318, 3000));
        }

        [Test]
        public void FormatAetherium_GroupsEveryMagnitude()
        {
            Assert.AreEqual("Aetherium 0 / 0", ResourceBarPresenter.FormatAetherium(0, 0));
            Assert.AreEqual("Aetherium 5 / 12", ResourceBarPresenter.FormatAetherium(5, 12));
            Assert.AreEqual("Aetherium 999 / 1.000", ResourceBarPresenter.FormatAetherium(999, 1000));
            Assert.AreEqual(
                "Aetherium 1.234.567 / 9.000.000",
                ResourceBarPresenter.FormatAetherium(1234567, 9000000));
            Assert.AreEqual(
                "Aetherium 9.223.372.036.854.775.807 / 9.223.372.036.854.775.807",
                ResourceBarPresenter.FormatAetherium(long.MaxValue, long.MaxValue));
        }

        [Test]
        public void FormatAetherium_UnderForeignCulture_KeepsGermanDots()
        {
            // The digits are assembled one by one precisely so a host culture
            // cannot leak in — pin that with an en-US ambient culture.
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Assert.AreEqual(
                    "Aetherium 2.318 / 3.000",
                    ResourceBarPresenter.FormatAetherium(2318, 3000));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void FormatPower_ShowsProvidedAgainstRequired()
        {
            Assert.AreEqual("Strom 130/80", ResourceBarPresenter.FormatPower(130, 80));
            Assert.AreEqual("Strom 60/80", ResourceBarPresenter.FormatPower(60, 80));
        }

        // ----------------------------------------------------------------
        // The composed model
        // ----------------------------------------------------------------

        [Test]
        public void BuildModel_NormalState_HasNoWarning()
        {
            ResourceBarModel model = ResourceBarPresenter.BuildModel(2318, 3000, 130, 80);

            Assert.AreEqual("Aetherium 2.318 / 3.000", model.AetheriumText);
            Assert.AreEqual("Strom 130/80", model.PowerText);
            Assert.IsNull(model.WarningText);
            Assert.AreEqual(StorageCeilingState.BelowCeiling, model.StorageState);
            Assert.IsFalse(model.IsLowPower);
            Assert.IsFalse(model.IsCritical);
        }

        [Test]
        public void BuildModel_ExactlyBalancedGrid_IsNotADeficit()
        {
            // The sim's own rule (IsLowPower: required > provided) — an
            // exactly-balanced grid runs at full speed and must not warn.
            ResourceBarModel model = ResourceBarPresenter.BuildModel(2318, 3000, 80, 80);

            Assert.IsFalse(model.IsLowPower);
            Assert.IsNull(model.WarningText);
        }

        [Test]
        public void BuildModel_FullStore_WarnsAmber()
        {
            ResourceBarModel model = ResourceBarPresenter.BuildModel(3000, 3000, 130, 80);

            Assert.AreEqual(StorageCeilingState.AtCeiling, model.StorageState);
            Assert.AreEqual("Lager voll — Einnahmen verfallen", model.WarningText);
            Assert.IsFalse(model.IsCritical, "a full store stops income but burns nothing — amber, not red");
        }

        [Test]
        public void BuildModel_Overflow_WarnsCriticalAndNamesTheAction()
        {
            ResourceBarModel model = ResourceBarPresenter.BuildModel(3000, 2000, 130, 80);

            Assert.AreEqual(StorageCeilingState.AboveCeiling, model.StorageState);
            Assert.AreEqual("Überschuss verfällt — Lager bauen!", model.WarningText);
            Assert.IsTrue(model.IsCritical);
        }

        [Test]
        public void BuildModel_PowerDeficit_WarnsCriticalAndNamesTheConsequences()
        {
            ResourceBarModel model = ResourceBarPresenter.BuildModel(2318, 3000, 60, 80);

            Assert.IsTrue(model.IsLowPower);
            Assert.AreEqual("Strom 60/80", model.PowerText);
            Assert.AreEqual("Strommangel — Produktion ½ · Reparatur ½ · Radar aus", model.WarningText);
            Assert.IsTrue(model.IsCritical);
        }

        [Test]
        public void BuildModel_FullStoreAndDeficit_JoinsBothWarningsStorageFirst()
        {
            ResourceBarModel model = ResourceBarPresenter.BuildModel(3000, 3000, 60, 80);

            Assert.AreEqual(
                "Lager voll — Einnahmen verfallen   |   Strommangel — Produktion ½ · Reparatur ½ · Radar aus",
                model.WarningText);
            Assert.IsTrue(model.IsCritical, "the deficit is an active penalty — the join escalates to red");
        }

        // ----------------------------------------------------------------
        // Zone math
        // ----------------------------------------------------------------

        [Test]
        public void TopRightZone_DocksToTheRightMargin()
        {
            HudRect zone = ResourceBarPresenter.TopRightZone(
                screenWidth: 1280f, top: 31f, contentWidth: 400f, height: 22f, margin: 8f);

            Assert.AreEqual(1280f - 8f - 400f, zone.X);
            Assert.AreEqual(31f, zone.Y);
            Assert.AreEqual(400f, zone.Width);
            Assert.AreEqual(22f, zone.Height);
        }

        [Test]
        public void TopRightZone_ContentWiderThanScreen_ClampsToTheMargins()
        {
            HudRect zone = ResourceBarPresenter.TopRightZone(
                screenWidth: 300f, top: 31f, contentWidth: 900f, height: 22f, margin: 8f);

            Assert.AreEqual(8f, zone.X, "grows leftward from the margin instead of running off the right edge");
            Assert.AreEqual(300f - 16f, zone.Width);
        }

        [Test]
        public void TopRightZone_DegenerateInputs_NeverGoNegative()
        {
            HudRect zone = ResourceBarPresenter.TopRightZone(
                screenWidth: 0f, top: -5f, contentWidth: -10f, height: -1f, margin: 8f);

            Assert.GreaterOrEqual(zone.X, 0f);
            Assert.GreaterOrEqual(zone.Y, 0f);
            Assert.GreaterOrEqual(zone.Width, 0f);
            Assert.GreaterOrEqual(zone.Height, 0f);
        }
    }
}
