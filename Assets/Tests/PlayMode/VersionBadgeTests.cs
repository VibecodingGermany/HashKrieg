using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Proof that the version badge (issue #103) is always on screen with the
    /// exact string the sprint pins down — in the Editor
    /// "v&lt;version&gt; · dev (Editor)" — that it never picks, and that it
    /// survives the transition the IMGUI cockpit does not (issue #102): the
    /// way from the menu into the match.
    /// <para>
    /// SAME ASSEMBLY-WALL PATTERN AS MainMenuTests: VersionBadge lives in
    /// Nova.Presentation.UI (rank 4), which no test assembly may reference
    /// (quality/scripts/run_gate_check.py:183-188). Everything below is found
    /// by GameObject name and element name; a rename fails loudly here.
    /// </para>
    /// </summary>
    public sealed class VersionBadgeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string MenuObjectName = "MainMenu";
        private const string BadgeObjectName = "VersionBadge";
        private const string BadgeElementName = "version-badge";

        [UnityTest]
        public IEnumerator VersionBadge_IsAlwaysThereNamesTheBuildAndNeverPicks()
        {
            yield return LoadBootstrapScene();

            UIDocument badgeDocument = DocumentOn(BadgeObjectName);
            UIDocument menuDocument = DocumentOn(MenuObjectName);
            Label badge = BadgeLabel(badgeDocument);

            Assert.AreEqual($"v{Application.version} · dev (Editor)", badge.text,
                "the badge must name the running version — Application.version, i.e. " +
                "ProjectSettings' bundleVersion, the ONE source — plus the build id. In the " +
                "Editor BuildInfo.Commit reports its editor sentinel (D-094), so the id is " +
                "'dev' with the '(Editor)' marker the sprint specifies.");
            Assert.AreEqual(PickingMode.Ignore, badge.pickingMode,
                "the badge must never pick: a click in the bottom-left corner belongs to the " +
                "minimap or the world behind it, not to a read-only label");
            Assert.AreNotSame(menuDocument, badgeDocument,
                "the badge needs its OWN UIDocument: the menu clears and rebuilds its root on " +
                "every return (MainMenuController.BuildTree), and the IMGUI cockpit is toggled " +
                "across the menu/match transition (#102) — a label in either layer would " +
                "vanish with it");
            Assert.Greater(badgeDocument.sortOrder, menuDocument.sortOrder,
                "the badge's document must sort above the menu's: the menu paints full-screen " +
                "key art plus scrim, and a badge under them is invisible exactly where the " +
                "sprint wants it seen");

            // Into the match: the menu overlay hides, the badge stays. This
            // is the regression the own-document rule exists for.
            Button newGame = FindButton(menuDocument.rootVisualElement, "Neues Spiel");
            using (var submit = new NavigationSubmitEvent { target = newGame })
            {
                newGame.SendEvent(submit);
            }
            yield return null;

            Assert.AreEqual(DisplayStyle.None,
                menuDocument.rootVisualElement.Q("menu-screen").style.display.value,
                "sanity check: starting a match must hide the menu overlay");
            Assert.NotNull(badgeDocument.rootVisualElement.Q(BadgeElementName),
                "the badge must still be there with the match running — it is not part of " +
                "the layer the menu switches off");
            Assert.AreNotEqual(DisplayStyle.None, badge.style.display.value,
                "nothing may hide the badge on the way into the match");
        }

        private static IEnumerator LoadBootstrapScene()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            // Awake/OnEnable run during activation, Start one frame later —
            // and VersionBadge builds its label in Start. Nothing above may
            // query the panel before this.
            yield return null;
            yield return null;
        }

        private static Label BadgeLabel(UIDocument badgeDocument)
        {
            Assert.NotNull(badgeDocument.rootVisualElement,
                "the badge UIDocument has no root visual element");
            Label badge = badgeDocument.rootVisualElement.Q<Label>(BadgeElementName);
            Assert.NotNull(badge,
                $"no Label named '{BadgeElementName}' in the badge document — VersionBadge " +
                "builds it in Start(). If the element was renamed, this test has to follow: " +
                "it cannot reference the type (rank-4 assembly wall).");
            return badge;
        }

        private static UIDocument DocumentOn(string gameObjectName)
        {
            // FindObjectsInactive.Include: this finder must not silently
            // change its answer the day someone deactivates a UI object.
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include);
            foreach (UIDocument document in documents)
            {
                if (document.gameObject.name != gameObjectName) continue;
                Assert.NotNull(document.panelSettings,
                    $"the '{gameObjectName}' UIDocument has no PanelSettings, so it has no panel " +
                    "and draws nothing. MenuAssetSetup.LoadOrCreatePanelSettings creates " +
                    "Assets/_Project/UI/HashkriegPanelSettings.asset when the generator runs.");
                return document;
            }
            Assert.Fail(
                $"no UIDocument on a '{gameObjectName}' object in the Bootstrap scene. The scene " +
                "is machine output — run Tools/Project Nova/Create Bootstrap Scene " +
                "(BootstrapSceneGenerator).");
            return null;
        }

        private static Button FindButton(VisualElement root, string text)
        {
            foreach (Button button in root.Query<Button>().ToList())
            {
                if (button.text == text) return button;
            }
            Assert.Fail($"the menu has no button labelled '{text}'");
            return null;
        }
    }
}
