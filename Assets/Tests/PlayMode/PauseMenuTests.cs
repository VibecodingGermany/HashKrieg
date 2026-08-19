using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Gameplay;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// The two traps of package 21.8, pinned where they actually bite.
    /// <para>
    /// WHY THIS FILE NAMES THE TYPE AS A STRING: Nova.PlayMode.Tests
    /// references Nova.Gameplay but NOT Nova.Presentation.UI, so
    /// <c>PauseMenuHud</c> cannot be named as a type here — the same reason
    /// <c>MainMenuTests</c> reaches for <c>DebugHud</c> that way.
    /// <c>ModalSurfaceLink</c> IS reachable: it lives in Nova.Gameplay
    /// precisely so both Presentation assemblies and this one can see it.
    /// </para>
    /// </summary>
    public sealed class PauseMenuTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string PauseMenuTypeName = "PauseMenuHud";
        private const string DebugHudTypeName = "DebugHud";

        /// <summary>
        /// The pause menu is IN THE SCENE, on the switched root, and the modal
        /// channel is clean while the main menu owns the screen.
        /// <para>
        /// THE SCENE IS THE POINT OF THIS TEST. Bootstrap.unity is machine
        /// output that is committed, and it went stale once already: it sat
        /// unchanged from 2026-08-08 while the generator moved three times, so
        /// components wired only in the generator were simply absent from the
        /// running game — code merged, CI green, nothing on screen. Without
        /// this assertion the whole of 21.8 can ship and do nothing.
        /// </para>
        /// <para>
        /// THE CLEAN CHANNEL IS THE SECOND POINT. <c>ModalSurfaceLink</c> is a
        /// per-frame verdict published by the pause menu, and the way to the
        /// main menu switches the HUD root — writer included — OFF. A writer
        /// that stopped publishing while its last word was <c>true</c> would
        /// leave every world gesture suspended for the rest of the session:
        /// no selection, no orders, no camera edge-pan, and no component still
        /// running that could ever clear it. That is why the writer resets in
        /// OnDisable, and this is the assertion that keeps it there.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator PauseMenu_IsWiredOntoTheHudRootAndLeavesNoStaleModalFlag()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            yield return null;

            Behaviour pauseMenu = RequireBehaviour(PauseMenuTypeName,
                "the UI object carries no pause menu, so ESC does nothing and a player cannot leave " +
                "a running match (#105). The scene is machine output — run " +
                "Tools/Project Nova/Create Bootstrap Scene after any change to " +
                "BootstrapSceneGenerator.CreateUiObject.");

            Behaviour debugHud = RequireBehaviour(DebugHudTypeName,
                "the UI object carries no debug HUD");

            Assert.AreSame(debugHud.gameObject, pauseMenu.gameObject,
                "the pause menu must live on the same GameObject as the rest of the cockpit: that " +
                "root IS the menu/match switch (MainMenuController.SetGameplayLayerActive). A pause " +
                "menu beside it would keep drawing over the main menu — which is exactly the defect " +
                "(#102) this package exists to end.");

            Assert.IsFalse(pauseMenu.gameObject.activeInHierarchy,
                "the HUD root must be OFF while the main menu owns the screen");

            Assert.IsFalse(ModalSurfaceLink.Open,
                "no modal may be claimed while the main menu is up. This is the deadlock guard: the " +
                "channel is a per-frame verdict whose only writer sits on the root that was just " +
                "switched off, so a last word of 'true' would suspend every world gesture for the " +
                "rest of the session with nothing left running to clear it.");
        }

        private static Behaviour RequireBehaviour(string typeName, string message)
        {
            // FindObjectsInactive.Include is load-bearing: the menu switches
            // the HUD root off, and everything on it counts as inactive.
            Behaviour[] all = Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].GetType().Name == typeName) return all[i];
            }

            Assert.Fail($"{message} (looked for a component named '{typeName}'). If it was renamed, " +
                        "this file has to follow — it cannot reference the type.");
            return null;
        }

        /// <summary>
        /// The play-observation deadlock (T-03): pause menu → "Zum Hauptmenü"
        /// → "Neues Spiel" left the match RUNNING (ticks advance) but every
        /// world gesture dead. The round trip is driven through the real
        /// entry points (MainMenuController.StartMatch / ReturnToMenu,
        /// PauseMenuHud open/close via reflection — the assembly may not be
        /// referenced, so the button layer is not under test here), and after
        /// EVERY leg the input path must still answer: a click on the start
        /// field must select it (the FieldReservePickTests probe). Each leg
        /// logs the three gate states, so a failure names the stuck one:
        /// ModalSurfaceLink, menu visibility, HUD-root activity.
        /// </summary>
        [UnityTest]
        public IEnumerator PauseRoundTrip_ThroughMainMenu_KeepsInputAlive()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            yield return null;

            Behaviour menu = RequireBehaviour("MainMenuController", "scene contains no main menu controller");
            Behaviour input = RequireBehaviour("RtsDeviceInput", "scene contains no device input");
            Behaviour pause = RequireBehaviour(PauseMenuTypeName, "scene contains no pause menu");

            // Leg 1: menu → match.
            Invoke(menu, "StartMatch");
            yield return new WaitForSeconds(0.5f);
            LogGates("after StartMatch", menu, input);
            AssertFieldPickWorks(input, "input dead right after StartMatch");

            // Leg 2: open and close the pause menu.
            Invoke(pause, "OpenMenu");
            yield return null;
            yield return null;
            Assert.IsTrue(ModalSurfaceLink.Open, "an open pause menu must claim the modal channel");
            Invoke(pause, "CloseMenu", false);
            yield return null;
            yield return null;
            LogGates("after pause close", menu, input);
            Assert.IsFalse(ModalSurfaceLink.Open, "closing the pause menu must release the modal channel");
            AssertFieldPickWorks(input, "input dead after closing the pause menu");

            // Leg 3: pause → "Zum Hauptmenü" → "Neues Spiel" (the T-03 path).
            // The button's exact semantics: drop the menu state WITHOUT
            // resuming the clock, then ReturnToMenu.
            Invoke(pause, "OpenMenu");
            yield return null;
            SetPrivateField(pause, "_menuOpen", false);
            SetPrivateField(pause, "_pausedByMenu", false);
            Invoke(menu, "ReturnToMenu");
            yield return null;
            yield return null;
            LogGates("after ReturnToMenu", menu, input);
            Invoke(menu, "StartMatch");
            yield return new WaitForSeconds(0.5f);
            LogGates("after second StartMatch", menu, input);
            AssertFieldPickWorks(input, "input dead after menu → new-match round trip (T-03)");
        }

        private static void SetPrivateField(object target, string field, object value)
        {
            System.Reflection.FieldInfo info = target.GetType().GetField(
                field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(info, $"{target.GetType().Name}.{field} not found");
            info.SetValue(target, value);
        }

        private static void LogGates(string where, Behaviour menu, Behaviour input)
        {
            bool menuVisible = (bool)menu.GetType().GetProperty("IsMenuVisible").GetValue(menu);
            Debug.Log($"[PauseRoundTrip] {where}: ModalSurfaceLink.Open={ModalSurfaceLink.Open}, " +
                      $"IsMenuVisible={menuVisible}, input GO active={input.gameObject.activeInHierarchy}, " +
                      $"input enabled={input.enabled}");
        }

        private static void AssertFieldPickWorks(Behaviour input, string message)
        {
            if (!input.gameObject.activeInHierarchy || !input.enabled)
            {
                Assert.Fail($"{message} — the input component itself is off (HUD root switch)");
            }
            Camera camera = Camera.main;
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(7.5f, 0f, 7.5f));
            input.GetType()
                .GetMethod("SelectSingle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(input, new object[] { new Vector2(screen.x, screen.y), false });
            var selection = (SelectionManager)input.GetType().GetProperty("Selection").GetValue(input);
            Assert.AreEqual((ushort)1, selection.SelectedFieldId, message);
        }

        private static void Invoke(Behaviour target, string method, params object[] args)
        {
            System.Reflection.MethodInfo info = target.GetType().GetMethod(
                method,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(info, $"{target.GetType().Name}.{method} not found");
            info.Invoke(target, args);
        }

        /// <summary>
        /// The actual T-03 defect ("after pause, units no longer move, but
        /// buildings still complete"): MatchRunner.StartMatch is the resume
        /// path, and Kernel.Start() with its default argument resets
        /// CurrentTick to 0 while the session and all systems keep their
        /// state — player commands, targeted at session ticks, then land
        /// minutes late or never. The pin: a move order must move the unit
        /// BEFORE and AFTER a pause/resume, and the kernel tick must never
        /// jump backwards across it.
        /// </summary>
        [UnityTest]
        public IEnumerator PauseResume_KeepsTheTickAndCommandsFlowing()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            yield return null;

            Behaviour menu = RequireBehaviour("MainMenuController", "scene contains no main menu controller");
            Invoke(menu, "StartMatch");
            yield return new WaitForSeconds(0.5f);

            var bootstrap = Object.FindAnyObjectByType<Nova.Gameplay.Match.MatchBootstrap>();
            Assert.NotNull(bootstrap, "no MatchBootstrap");
            Nova.Gameplay.Match.MatchRunner runner = bootstrap.Runner;
            Assert.IsTrue(runner.IsRunning, "match not running after StartMatch");

            // The local Builder of the D-077 opening.
            Nova.Core.EntityId builder = Nova.Core.EntityId.Invalid;
            Nova.Simulation.State.UnitState[] units = runner.Entities.RawUnits;
            for (int i = 0; i < runner.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == 0 && units[i].Role == Nova.Simulation.State.UnitRole.Builder)
                {
                    builder = units[i].Id;
                    break;
                }
            }
            Assert.IsTrue(builder.IsValid, "no local Builder in the opening");

            float before = units[builder.Index].Transform.PositionX.ToFloat();
            SubmitMove(runner, builder, 6f);
            yield return new WaitForSeconds(1.5f);
            float afterFirst = units[builder.Index].Transform.PositionX.ToFloat();
            Assert.Greater(afterFirst - before, 0.5f, "the move order before the pause must move the Builder");

            Assert.IsTrue(runner.PauseMatch(), "local pause refused");
            uint tickAtPause = runner.Kernel.CurrentTick.Value;
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(runner.StartMatch(), "resume refused");
            yield return null;
            Assert.GreaterOrEqual(runner.Kernel.CurrentTick.Value, tickAtPause,
                "the kernel tick jumped backwards across pause/resume — the T-03 defect");

            SubmitMove(runner, builder, 3f);
            yield return new WaitForSeconds(1.5f);
            float afterResume = units[builder.Index].Transform.PositionX.ToFloat();
            Assert.Greater(Mathf.Abs(afterResume - afterFirst), 0.5f,
                "the move order after pause/resume must still reach the sim (T-03)");
        }

        /// <summary>Submits a move order for the unit through the sealed intake, target = current X + delta.</summary>
        private static void SubmitMove(Nova.Gameplay.Match.MatchRunner runner, Nova.Core.EntityId unit, float deltaX)
        {
            Nova.Simulation.State.UnitState[] units = runner.Entities.RawUnits;
            float x = units[unit.Index].Transform.PositionX.ToFloat() + deltaX;
            float y = units[unit.Index].Transform.PositionY.ToFloat();
            var payload = new Nova.Simulation.CommandsV1.MovePayload(
                new[] { Nova.Simulation.State.UnitCommandStateView.ToRawEntityId(unit) },
                Nova.Core.SimFixed.FromFloat(x), Nova.Core.SimFixed.FromFloat(y));
            Assert.AreEqual(Nova.Simulation.CommandsV1.CommandIngressResult.Accepted,
                runner.Ingress.TrySubmitIntent(Nova.Simulation.CommandsV1.CommandIntent.Create(payload), out _),
                "the move intent must enter the sealed intake");
        }
    }
}
