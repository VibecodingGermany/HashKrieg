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
    }
}
