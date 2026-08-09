using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Nova.Gameplay.Match;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Proof that the game opens in the main menu instead of dropping the
    /// player into a running match, and that the menu is a working way in and
    /// out (sprint docs/production/hashkrieg/08_Sprint_Hauptmenue.md, D-083).
    /// <para>
    /// The menu is an OVERLAY in the one scene this project has: there is no
    /// second scene and no SceneManager call in production code. So everything
    /// below happens inside a single load of Bootstrap.unity — the idle host,
    /// the overlay, the start, and the cockpit waking up behind it.
    /// </para>
    /// <para>
    /// WHY THIS FILE NAMES TYPES AS STRINGS. MainMenuController, DebugHud
    /// (Nova.Presentation.UI) and RtsCameraController (Nova.Presentation) are
    /// rank-4 assemblies, and quality/scripts/run_gate_check.py:183-188 forbids
    /// any test assembly from referencing one — Nova.Presentation.Tests was
    /// dissolved for exactly that reason in 5cdb0ce. The menu's contract with
    /// those three components (it switches two of them off while it is up and
    /// back on when the match starts) is still worth a test, so they are found
    /// by type name. A name that no longer resolves fails loudly here; the
    /// alternative was not testing the behaviour at all.
    /// </para>
    /// <para>
    /// Run headless-with-graphics, never with -quit (same lane as
    /// GrayboxDemoProofTests):
    ///   Unity -batchmode -projectPath &lt;repo&gt; -runTests -testPlatform PlayMode \
    ///     -testResults &lt;abs&gt;/output/playmode-results.xml -logFile output/playmode-tests.log
    /// </para>
    /// </summary>
    public sealed class MainMenuTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string StoreTypeName = "Nova.Presentation.UI.GameSettingsStore";
        private const string MenuControllerTypeName = "MainMenuController";
        private const string CameraRigTypeName = "RtsCameraController";
        private const string DebugHudTypeName = "DebugHud";

        /// <summary>Deliberately not 0.4 (the default) and not 0 or 1 (the clamp edges).</summary>
        private const float TestVolume = 0.13f;

        private PropertyInfo _filePathProperty;
        private string _settingsDirectory;
        private string _settingsPath;

        /// <summary>
        /// Sends every settings write this fixture provokes to a throwaway
        /// file. The settings panel persists on every change, so without this
        /// redirect a test run would overwrite the settings.json of whoever
        /// runs it — with a music volume of 0.13, on their real machine.
        /// GameSettingsStore.FilePath exists as exactly this seam.
        /// </summary>
        [SetUp]
        public void RedirectTheSettingsFile()
        {
            _filePathProperty = ResolveFilePathProperty();
            _settingsDirectory = Path.Combine(
                Application.temporaryCachePath, "MainMenuTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_settingsDirectory);
            _settingsPath = Path.Combine(_settingsDirectory, "settings.json");
            _filePathProperty.SetValue(null, _settingsPath);
        }

        [TearDown]
        public void RestoreTheSettingsFile()
        {
            // null clears the override; the property then resolves to
            // persistentDataPath again, which is where the player's file lives.
            _filePathProperty?.SetValue(null, null);
            if (!string.IsNullOrEmpty(_settingsDirectory) && Directory.Exists(_settingsDirectory))
            {
                Directory.Delete(_settingsDirectory, recursive: true);
            }
        }

        // ------------------------------------------------------------------
        // (a) The scene opens into the menu, not into a match
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator BootstrapScene_OpensInTheMainMenuWithNoMatchRunning()
        {
            yield return LoadBootstrapScene();

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            Assert.IsFalse(bootstrap.IsMatchReady,
                "the scene must load into an IDLE host: MatchBootstrap.AutoStart is off " +
                "(BootstrapSceneGenerator.CreateMatchObject writes it) so the main menu owns the " +
                "start. A ready match here means Bootstrap.unity predates the menu sprint — the " +
                "scene is machine output, run Tools/Project Nova/Create Bootstrap Scene.");

            var runner = Object.FindAnyObjectByType<MatchRunner>();
            Assert.NotNull(runner, "Bootstrap scene contains no MatchRunner");
            Assert.IsFalse(runner.IsRunning,
                "no kernel may tick behind the menu — the simulation starts with the player's " +
                "first click, not with the scene load");

            VisualElement root = MenuRoot();
            VisualElement screen = root.Q("menu-screen");
            Assert.NotNull(screen,
                "MainMenuController built no tree: 'menu-screen' is its root element. Either the " +
                "component is missing from the MainMenu object or it bailed out in Start().");
            Assert.AreNotEqual(DisplayStyle.None, screen.style.display.value,
                "the menu overlay must be visible the moment the scene is up");

            Assert.NotNull(FindButton(root, "Neues Spiel"), "the menu needs a way into a match");
            Assert.NotNull(FindButton(root, "Netzpartie"), "the menu needs a two-player join path");
            Assert.NotNull(FindButton(root, "Einstellungen"), "the menu needs a way into the settings");
            Assert.NotNull(FindButton(root, "Beenden"),
                "the menu needs a way out of the game. No test ever presses it: in the Editor it " +
                "sets EditorApplication.isPlaying = false, which would kill the test run with it.");

            Button load = FindButton(root, "Laden");
            Assert.NotNull(load,
                "'Laden' must be SHOWN (sprint §6): hiding it makes the player wonder whether the " +
                "game can save at all");
            Assert.IsFalse(load.enabledSelf,
                "'Laden' must be DISABLED (sprint §6): the snapshot layer can restore a full match " +
                "tick-for-tick, but nothing in the game ever writes to disk — there is no save " +
                "format and there are no slots, so an enabled button would be a lie");

            Assert.IsFalse(CameraRig().enabled,
                "the RTS camera rig must rest while the menu is up. It is the one component in " +
                "this scene with no 'no match yet' guard: its LateUpdate reads scroll wheel, MMB, " +
                "Z/X and the screen-edge pan every frame, so a pointer resting near an edge would " +
                "pan the camera away from the HQ before the match even starts.");
            Assert.IsFalse(DebugHudBehaviour().enabled,
                "the debug HUD must be silent while the menu is up: its always-on status bar draws " +
                "BEFORE its own F3 visibility check, so it would sit on top of the key art");
        }

        // ------------------------------------------------------------------
        // (b) Sound exists at all
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator MainMenu_PlaysTheLoopedTrackAndTheSceneHasAListener()
        {
            yield return LoadBootstrapScene();

            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            Assert.AreEqual(1, listeners.Length,
                "the scene must carry exactly one AudioListener. Before this sprint it had none, " +
                "and without one EVERY sound in the game is silent no matter how the source is " +
                "configured — a failure with no error message. Two listeners make Unity warn and " +
                "pick one arbitrarily.");
            Assert.AreEqual("Main Camera", listeners[0].gameObject.name,
                "the listener rides on the main camera, where Unity's own camera template puts it " +
                "(BootstrapSceneGenerator.CreateCamera)");

            AudioSource source = MenuAudioSource();
            Assert.NotNull(source.clip,
                "no clip on the menu source — expected Assets/_Project/Audio/Music/" +
                "MUS_MainMenu_Hashkrieg.ogg, wired by BootstrapSceneGenerator");
            Assert.AreEqual("MUS_MainMenu_Hashkrieg", source.clip.name,
                "the menu must play the looped menu track, not some other clip");
            Assert.IsTrue(source.loop,
                "the track loops seamlessly in the file itself (the last six seconds are blended " +
                "over its head, sprint §4), so AudioSource.loop is the entire looping mechanism — " +
                "there is deliberately no crossfade in code");
            Assert.IsFalse(source.playOnAwake,
                "playOnAwake would start the clip before the stored volume is applied, so a player " +
                "who muted the music would hear a burst of it at the 0.4 default on every single " +
                "start (MenuMusicPlayer.OnEnable applies the volume, then plays)");
            Assert.AreEqual(0f, source.spatialBlend, 0.0001f,
                "menu music has no position in the world; a 3D source would pan with the camera");
            Assert.IsTrue(source.isPlaying,
                "the menu must actually be audible — 'I start the game and hear music' is the " +
                "first half of the sprint's acceptance test");
        }

        // ------------------------------------------------------------------
        // (c) The way in
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NewGame_StartsTheMatchHidesTheOverlayAndWakesTheCockpit()
        {
            yield return LoadBootstrapScene();

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            var runner = Object.FindAnyObjectByType<MatchRunner>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            Assert.NotNull(runner, "Bootstrap scene contains no MatchRunner");

            VisualElement root = MenuRoot();
            AudioSource music = MenuAudioSource();

            Submit(FindButton(root, "Neues Spiel"));

            // No yield: StartGrayboxMatch() builds the D-077 opening position
            // synchronously, so the button's whole effect has landed by now.
            Assert.IsTrue(bootstrap.IsMatchReady,
                "'Neues Spiel' must call the idempotent MatchBootstrap.StartGrayboxMatch(). If the " +
                "match is not ready here, either the bootstrap reference on MainMenuController is " +
                "unwired or the button never reached its click handler.");
            Assert.IsTrue(runner.IsRunning, "the kernel must be running after the menu start");

            Assert.AreEqual(DisplayStyle.None, root.Q("menu-screen").style.display.value,
                "the overlay must disappear when the match starts — otherwise the player is left " +
                "playing behind the key art");
            Assert.IsTrue(CameraRig().enabled,
                "the RTS camera must take over as the match starts; it was switched off only for " +
                "as long as the menu was up");
            Assert.IsTrue(DebugHudBehaviour().enabled,
                "the status bar belongs to the cockpit and must come back with the match");
            Assert.IsFalse(MenuController().enabled,
                "the menu switches itself off after starting: this sprint has no pause menu and no " +
                "restart, so there is deliberately no way back into it");

            uint tickAtStart = runner.Session.CurrentTick;
            yield return new WaitForSeconds(2f);

            Assert.Greater(runner.Session.CurrentTick, tickAtStart,
                "the kernel must keep ticking after a menu start, exactly as it did under AutoStart");
            Assert.IsFalse(music.isPlaying,
                "the menu track fades out and stops when the match starts (MenuMusicPlayer, 1.25 s) " +
                "— it must not keep playing over the match");
        }

        [UnityTest]
        public IEnumerator NetworkPanel_ValidatesMasksAndCancelsWithoutStartingGameplay()
        {
            yield return LoadBootstrapScene();

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            var runner = Object.FindAnyObjectByType<MatchRunner>();
            VisualElement root = MenuRoot();

            Submit(FindButton(root, "Netzpartie"));
            yield return null;

            Assert.AreEqual(DisplayStyle.None, root.Q("menu-main").style.display.value);
            Assert.AreNotEqual(DisplayStyle.None, root.Q("menu-network").style.display.value);
            TextField host = FindTextField(root, "Serveradresse");
            TextField port = FindTextField(root, "Port");
            TextField code = FindTextField(root, "Match-Code");
            Assert.IsTrue(code.isPasswordField, "the match code must be masked while typed");
            Assert.AreEqual(16, code.maxLength);
            Assert.NotNull(FindDropdown(root, "Rolle"));

            const string secret = "0123456789ABCDEF";
            host.value = string.Empty;
            port.value = "47777";
            code.value = secret;
            LogAssert.Expect(LogType.Error,
                "[MatchBootstrap] Network join failed: Die Serveradresse darf nicht leer sein.");
            Submit(FindButton(root, "Verbinden"));
            yield return null;

            Label status = root.Q<Label>("network-status");
            Assert.NotNull(status);
            StringAssert.Contains("Serveradresse", status.text);
            StringAssert.DoesNotContain(secret, status.text);
            Assert.AreEqual(string.Empty, code.value,
                "the UI must drop the code immediately after the join click");
            Assert.AreEqual(NetworkJoinFailure.InvalidHost, bootstrap.JoinStatus.Failure);
            Assert.IsFalse(bootstrap.IsMatchReady);
            Assert.IsFalse(runner.IsRunning);
            Assert.IsFalse(CameraRig().enabled);
            Assert.IsTrue(MenuAudioSource().isPlaying);

            Submit(FindButton(root, "Abbrechen"));
            yield return null;
            Assert.AreNotEqual(DisplayStyle.None, root.Q("menu-main").style.display.value);
            Assert.AreEqual(DisplayStyle.None, root.Q("menu-network").style.display.value);
            Assert.AreEqual(NetworkJoinPhase.Idle, bootstrap.JoinStatus.Phase);
        }

        // ------------------------------------------------------------------
        // (d) Settings: the volume is audible at once and survives a restart
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SettingsPanel_MusicVolumeReachesTheSourceAtOnceAndIsPersisted()
        {
            yield return LoadBootstrapScene();

            VisualElement root = MenuRoot();
            AudioSource music = MenuAudioSource();

            Submit(FindButton(root, "Einstellungen"));
            yield return null;

            Assert.AreEqual(DisplayStyle.None, root.Q("menu-main").style.display.value,
                "the four main entries give way to the settings panel");
            Assert.AreNotEqual(DisplayStyle.None, root.Q("menu-settings").style.display.value,
                "'Einstellungen' must show the settings panel");

            // Every control the sprint promises, in one place: a silently
            // dropped row (BuildQualityField and BuildResolutionField both
            // return early when the engine offers no choices) is otherwise
            // invisible until a player goes looking for it.
            Assert.NotNull(FindToggle(root, "Musik"), "music on/off is in scope");
            Assert.NotNull(FindSlider(root, "Lautstärke"), "music volume is in scope");
            Assert.NotNull(FindToggle(root, "Soundeffekte"), "SFX on/off is in scope");
            Assert.NotNull(FindSlider(root, "SFX-Lautstärke"),
                "the SFX volume is in scope and is SHOWN even though the game has no sound effects " +
                "yet — the panel labels it as having no effect rather than hiding it (sprint §5)");
            Assert.NotNull(FindDropdown(root, "Render-Detail"),
                "render detail is in scope: the six quality levels differ in 19 real fields");
            Assert.NotNull(FindToggle(root, "vSync"), "vSync is in scope");
            Assert.NotNull(FindDropdown(root, "Auflösung"), "resolution is in scope");
            Assert.NotNull(FindToggle(root, "Vollbild"), "fullscreen is in scope");

            // Pinned so the assertion below does not depend on the settings
            // file of whoever runs this: a muted player would get volume 0.
            FindToggle(root, "Musik").value = true;

            Slider volume = FindSlider(root, "Lautstärke");
            volume.value = TestVolume;

            // NOTHING between the change and the assertion — no yield, no
            // wait. "Ich stelle die Lautstärke leiser" is an acceptance
            // criterion about the same frame: the volume goes onto the source
            // while the slider is being dragged, not when the panel closes.
            Assert.AreEqual(TestVolume, music.volume, 0.0001f,
                "moving the music slider must reach AudioSource.volume in the same frame " +
                "(MainMenuController.CommitSettings pushes it before the coalesced file write)");

            // The file write IS coalesced: a dragged slider raises one event
            // per frame, and each save also runs the engine apply.
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsTrue(File.Exists(_settingsPath),
                $"no settings file at {_settingsPath} — a changed volume that is never written " +
                "does not survive the restart the sprint asks for");
            var stored = JsonUtility.FromJson<SettingsProbe>(File.ReadAllText(_settingsPath));
            Assert.NotNull(stored, "settings.json did not parse as JSON");
            Assert.AreEqual(TestVolume, stored.musicVolume, 0.0001f,
                "the volume the player set must be in the file — this is the 'und beim nächsten " +
                "Start ist die Lautstärke noch so' half of the acceptance test");
            Assert.IsTrue(stored.musicEnabled, "the music toggle must persist with the volume");
        }

        // ------------------------------------------------------------------
        // Fixtures
        // ------------------------------------------------------------------

        private static IEnumerator LoadBootstrapScene()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            // Awake/OnEnable run during activation, Start one frame later —
            // and MainMenuController builds its tree in Start. Nothing above
            // may query the panel before this.
            yield return null;
            yield return null;
        }

        private static UIDocument MenuDocument()
        {
            var document = Object.FindAnyObjectByType<UIDocument>();
            Assert.NotNull(document,
                "no UIDocument in the Bootstrap scene: the MainMenu object is missing. The scene " +
                "is machine output — run Tools/Project Nova/Create Bootstrap Scene " +
                "(BootstrapSceneGenerator.CreateMainMenuObject).");
            Assert.NotNull(document.panelSettings,
                "the menu UIDocument has no PanelSettings, so it has no panel and draws nothing. " +
                "MenuAssetSetup.LoadOrCreatePanelSettings creates " +
                "Assets/_Project/UI/HashkriegPanelSettings.asset when the generator runs.");
            return document;
        }

        private static VisualElement MenuRoot()
        {
            VisualElement root = MenuDocument().rootVisualElement;
            Assert.NotNull(root, "the menu UIDocument has no root visual element");
            return root;
        }

        private static AudioSource MenuAudioSource()
        {
            AudioSource source = MenuDocument().GetComponent<AudioSource>();
            Assert.NotNull(source,
                "the MainMenu object carries no AudioSource — MenuMusicPlayer declares " +
                "[RequireComponent(typeof(AudioSource))] and the generator adds one explicitly");
            return source;
        }

        /// <summary>
        /// Presses a menu button the way UI Toolkit's keyboard and gamepad
        /// submit does: <c>Button</c> turns a NavigationSubmitEvent into the
        /// same <c>clicked</c> callback a mouse click raises, so this exercises
        /// the real handler — not a private method reached by reflection.
        /// A synthesized pointer click would additionally depend on resolved
        /// layout geometry and on the panel's element-under-pointer
        /// bookkeeping, neither of which is meaningful in a batchmode run.
        /// </summary>
        private static void Submit(Button button)
        {
            Assert.NotNull(button, "cannot press a button that does not exist");
            Assert.IsTrue(button.enabledSelf, $"'{button.text}' is disabled and cannot be pressed");

            using (var submit = new NavigationSubmitEvent { target = button })
            {
                button.SendEvent(submit);
            }
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

        private static Slider FindSlider(VisualElement root, string label)
        {
            foreach (Slider slider in root.Query<Slider>().ToList())
            {
                if (slider.label == label) return slider;
            }
            Assert.Fail($"the settings panel has no slider labelled '{label}'");
            return null;
        }

        private static Toggle FindToggle(VisualElement root, string label)
        {
            foreach (Toggle toggle in root.Query<Toggle>().ToList())
            {
                if (toggle.label == label) return toggle;
            }
            Assert.Fail($"the settings panel has no toggle labelled '{label}'");
            return null;
        }

        private static DropdownField FindDropdown(VisualElement root, string label)
        {
            foreach (DropdownField dropdown in root.Query<DropdownField>().ToList())
            {
                if (dropdown.label == label) return dropdown;
            }
            Assert.Fail($"the settings panel has no dropdown labelled '{label}'");
            return null;
        }

        private static TextField FindTextField(VisualElement root, string label)
        {
            foreach (TextField field in root.Query<TextField>().ToList())
            {
                if (field.label == label) return field;
            }
            Assert.Fail($"the network panel has no text field labelled '{label}'");
            return null;
        }

        private static Behaviour MenuController() =>
            RequireBehaviour(MenuControllerTypeName, "the MainMenu object carries no menu controller");

        private static Behaviour CameraRig() =>
            RequireBehaviour(CameraRigTypeName, "the main camera carries no RTS camera rig");

        private static Behaviour DebugHudBehaviour() =>
            RequireBehaviour(DebugHudTypeName, "the UI object carries no debug HUD");

        private static Behaviour RequireBehaviour(string typeName, string message)
        {
            // FindObjectsInactive.Include is load-bearing: the menu switches
            // the rig and the HUD OFF, and a disabled component counts as
            // inactive for this API.
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
        /// Resolves <c>GameSettingsStore.FilePath</c> by name for the same
        /// assembly-rank reason as the rest of this file. This is the only
        /// member of that class the fixture touches, and it is the seam that
        /// keeps a test run out of the player's real settings file.
        /// </summary>
        private static PropertyInfo ResolveFilePathProperty()
        {
            System.Type store = System.Type.GetType($"{StoreTypeName}, Nova.Presentation.UI");
            if (store == null)
            {
                foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    store = assembly.GetType(StoreTypeName);
                    if (store != null) break;
                }
            }

            Assert.NotNull(store,
                $"{StoreTypeName} not found. If the settings moved out of Nova.Presentation.UI " +
                "(the recommended fix — see the sprint report), this fixture can reference the " +
                "type directly and drop the reflection.");

            PropertyInfo property = store.GetProperty("FilePath", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(property,
                "GameSettingsStore.FilePath is gone. It is the seam that keeps a test run away " +
                "from the player's real settings file; without it this fixture must not run.");
            Assert.IsTrue(property.CanWrite, "GameSettingsStore.FilePath must stay writable for tests");
            return property;
        }

        /// <summary>
        /// The two fields this fixture reads back out of settings.json,
        /// declared locally because the real type sits behind the assembly
        /// wall. It doubles as a pin on the JSON field NAMES: renaming a field
        /// silently orphans every player's stored settings, and nothing else
        /// in the project would notice.
        /// </summary>
#pragma warning disable 0649 // JsonUtility fills these by reflection
        [System.Serializable]
        private sealed class SettingsProbe
        {
            public bool musicEnabled;
            public float musicVolume;
        }
#pragma warning restore 0649
    }
}
