using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Gameplay;
using Nova.Gameplay.Match;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Regression/diagnosis for the 21.2 play-observation finding "fields are
    /// not clickable" (#86): drives the real click path of RtsDeviceInput
    /// (private SelectSingle/TryPickUnit/TryPickField via reflection — test
    /// assemblies may not reference Nova.Presentation.UI, the MainMenuTests
    /// pattern) with the field centre projected through the real main camera,
    /// and logs every stage's verdict so a failure names its stage.
    /// Run headless-with-graphics and NEVER with -quit (see
    /// GrayboxDemoProofTests' header for the invocation).
    /// </summary>
    public sealed class FieldReservePickTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [UnityTest]
        public IEnumerator ClickOnStartField_SelectsTheField()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            bootstrap.StartGrayboxMatch();
            Assert.IsTrue(bootstrap.IsMatchReady, "match did not start");

            // Two frames: let every Awake/Start and the first model rebuilds
            // settle. MainMenuController.Start is among them, and it is the
            // one that matters below.
            yield return null;
            yield return null;

            MonoBehaviour input = FindByTypeName("RtsDeviceInput");
            Assert.NotNull(input, "scene contains no RtsDeviceInput");

            // THE COCKPIT HAS TO BE SWITCHED ON, and this test has to do it
            // itself. Since package 21.8 the whole HUD root is inactive while
            // the main menu owns the screen (#102), and the thing that turns
            // it back on is MainMenuController.StartMatch — which this test
            // deliberately bypasses by driving MatchBootstrap directly. Left
            // off, RtsDeviceInput never runs a single Update, never binds its
            // dispatcher, and every pick path below dereferences null.
            //
            // The OFF assertion keeps this line honest: it must run AFTER the
            // two frames above, because the switch-off happens in
            // MainMenuController.Start, not during scene activation — asserted
            // one frame earlier it reads the scene file's default and passes
            // for the wrong reason.
            Assert.IsFalse(input.gameObject.activeInHierarchy,
                "the HUD root is expected to be off while the main menu owns the screen — if this " +
                "fails, the menu/match switch moved and the SetActive below is papering over it");
            input.gameObject.SetActive(true);

            // One more frame so the freshly enabled input binds its dispatcher.
            yield return null;

            // The serialized scene predates the 21.2 field: a missing YAML
            // entry must materialise the C# default 2f — pin that assumption,
            // it is exactly the kind of silent zero a scene upgrade swallows.
            FieldInfo radiusField = input.GetType().GetField(
                "_fieldPickRadiusWorld", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(radiusField, "RtsDeviceInput._fieldPickRadiusWorld missing");
            float radius = (float)radiusField.GetValue(input);
            Debug.Log($"[FieldPick] _fieldPickRadiusWorld = {radius}");
            Assert.Greater(radius, 0.5f, "field pick radius deserialised as ~0 — old scene asset ate the default");

            Camera camera = Camera.main;
            Assert.NotNull(camera, "no main camera");

            // Stage 1: the raw field probe at the field centre of the
            // canonical start field (7,7) -> centre (7.5, 0, 7.5).
            var world = new Vector3(7.5f, 0f, 7.5f);
            object[] pickArgs = { world, (ushort)0 };
            bool fieldHit = (bool)InvokePrivate(input, "TryPickField", pickArgs);
            Debug.Log($"[FieldPick] TryPickField({world}) = {fieldHit}, id = {pickArgs[1]}");

            // Stage 2: does a UNIT claim the same point first (the intended
            // priority — but then the click reads as unit selection)?
            object[] unitArgs = { world, true, Nova.Core.EntityId.Invalid };
            bool unitHit = (bool)InvokePrivate(input, "TryPickUnit", unitArgs);
            Debug.Log($"[FieldPick] TryPickUnit({world}, own) = {unitHit}, id = {unitArgs[2]}");

            // Stage 3: the real click path with the camera-projected point.
            Vector3 screen = camera.WorldToScreenPoint(world);
            Debug.Log($"[FieldPick] field centre on screen = {screen} (screen {Screen.width}x{Screen.height})");
            InvokePrivate(input, "SelectSingle", new object[] { new Vector2(screen.x, screen.y), false });

            var selection = (SelectionManager)input.GetType().GetProperty("Selection").GetValue(input);
            Debug.Log($"[FieldPick] after SelectSingle: SelectedFieldId = {selection.SelectedFieldId}, " +
                      $"SelectedCount = {selection.SelectedCount}");

            Assert.IsTrue(fieldHit, "TryPickField rejected the field centre itself");
            Assert.AreEqual((ushort)1, selection.SelectedFieldId,
                "a click on the start field must select field #1 (21.2, #86)");
            Assert.AreEqual(0, selection.SelectedCount, "a field selection owns no entities");

            // The real play-observation failure (T-02): on a trackpad a
            // "click" is a MICRO-DRAG past the 8 px threshold, which becomes
            // a box — and the box held no entities, so the gesture read as
            // "clear selection". A unit forgives the same gesture (the box
            // catches it), a field did not. Reproduce that exact gesture:
            // a small, unit-empty drag across the field must select it too.
            selection.ClearSelection();
            // A tight quadrant of +10 px around the field-centre pixel:
            // provably unit-empty here (stage 2 found no own unit within
            // the wider 1.5-cell pick radius), so the box exercises the
            // empty-gesture path and nothing else.
            InvokePrivate(input, "SelectBox", new object[]
            {
                new Vector2(screen.x, screen.y),
                new Vector2(screen.x + 10f, screen.y + 10f),
                false,
            });
            Debug.Log($"[FieldPick] after micro-drag SelectBox: SelectedFieldId = {selection.SelectedFieldId}, " +
                      $"SelectedCount = {selection.SelectedCount}");
            Assert.AreEqual((ushort)1, selection.SelectedFieldId,
                "a micro-drag over the start field (no units inside the box) must select field #1, not clear into nothing (21.2 play finding)");
        }

        private static object InvokePrivate(MonoBehaviour target, string method, object[] args)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(info, $"{target.GetType().Name}.{method} not found");
            object result = info.Invoke(target, args);
            return result;
        }

        private static MonoBehaviour FindByTypeName(string typeName)
        {
            MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetType().Name == typeName) return all[i];
            }
            return null;
        }
    }
}
