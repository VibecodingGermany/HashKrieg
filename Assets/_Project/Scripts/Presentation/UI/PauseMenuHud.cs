// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Audio;
using Nova.Gameplay.Match;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The in-match pause menu (#105): ESC or P opens it OVER the live world
    /// — the HUD root stays on, so the frozen match remains visible behind
    /// the panel — and stops the kernel clock via
    /// <see cref="MatchRunner.PauseMatch"/>. "Fortsetzen" (or ESC/P again)
    /// resumes what the menu itself paused, "Zum Hauptmenü" leaves through
    /// <see cref="MainMenuController.ReturnToMenu"/>, "Spiel beenden" through
    /// <see cref="MainMenuController.Quit"/>. Relay matches cannot pause
    /// (the runner refuses it), so there the menu opens without stopping the
    /// clock and says so.
    /// <para>
    /// ESC PEELS ONE LAYER PER PRESS: an armed placement ghost or order pick
    /// owns the key first (RtsDeviceInput runs at -200 and stamps
    /// <see cref="RtsDeviceInput.LastGestureCancelFrame"/> when it cancels
    /// one); the menu opens only on the next press. P is not a cancel key and
    /// opens the menu directly — the input component disarms any armed
    /// gesture the moment the modal gate engages.
    /// </para>
    /// <para>
    /// SINGLE WRITER of <see cref="ModalSurfaceLink"/>: every frame this
    /// component publishes whether ANY modal surface owns the input — this
    /// menu or <see cref="MatchFrameHud"/>'s terminal panels (result /
    /// network), which outrank the menu: the menu refuses to open over them
    /// and closes itself if one appears underneath. RtsDeviceInput reads the
    /// verdict and suspends world gestures while it is up. The publish is
    /// per-frame, never latched, and <see cref="OnDisable"/> resets the
    /// channel — the UI root switch-off on the way to the main menu must
    /// never leave a stale "open" behind, or the next match would deadlock
    /// every click.
    /// </para>
    /// <para>
    /// READ-ONLY toward the simulation: the only state this component
    /// touches is the kernel clock (pause/resume); it never submits a
    /// command. Settings are deliberately NOT a second mask here — they live
    /// in the main menu, and the panel says so instead of drifting a copy.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuHud : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private MainMenuController _menu;
        [Tooltip("Its result/network panels are the terminal modals: the pause menu never stacks on top of them.")]
        [SerializeField] private MatchFrameHud _matchFrame;
        [Tooltip("Read for the ESC layer-peel: an armed gesture cancels first, the menu opens on the next press.")]
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private float _panelWidth = 340f;

        private bool _menuOpen;
        private bool _pausedByMenu;

        private GUIStyle _headlineStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        /// <summary>True while the pause menu panel is up (read-only, for observers and tests).</summary>
        public bool IsPauseMenuOpen => _menuOpen;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_menu == null) _menu = FindAnyObjectByType<MainMenuController>();
            if (_matchFrame == null) _matchFrame = FindAnyObjectByType<MatchFrameHud>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void OnDisable()
        {
            // The channel must never outlive its writer: this component sits
            // on the UI root, and the root is switched OFF on the way to the
            // main menu — exactly the transition where a latched "open" would
            // deadlock the next match's input. Drop the local state with it,
            // so a re-enabled component (next match start) starts clean.
            _menuOpen = false;
            _pausedByMenu = false;
            ModalSurfaceLink.Reset();
        }

        private void Update()
        {
            if (_menu != null && _menu.IsMenuVisible)
            {
                // The main menu owns the screen and the UI root should
                // already be off (this Update would not run). Hand-built
                // scenes without the root switch still get a clean state:
                // ReturnToMenu keeps the match paused, so nothing resumes here.
                _menuOpen = false;
                _pausedByMenu = false;
            }
            else
            {
                HandleToggleInput();
                if (_menuOpen && _matchFrame != null && _matchFrame.ModalOpen)
                {
                    // A terminal panel (result / network) appeared under the
                    // open menu — it outranks the pause menu and takes over.
                    CloseMenu(playClick: false);
                }
            }

            ModalSurfaceLink.Publish(_menuOpen || (_matchFrame != null && _matchFrame.ModalOpen));
        }

        private void HandleToggleInput()
        {
            bool escape = Input.GetKeyDown(KeyCode.Escape);
            bool pause = Input.GetKeyDown(KeyCode.P);
            if (!escape && !pause) return;

            if (_menuOpen)
            {
                CloseMenu(playClick: true);
                return;
            }

            // ESC peels one layer at a time: RtsDeviceInput ran earlier this
            // frame (execution order -200) and stamped the press if it
            // cancelled an armed placement ghost or order pick — that press
            // belongs to the gesture, not to the menu. P never cancels a
            // gesture, so it needs no such check.
            if (escape && !pause
                && _input != null && _input.LastGestureCancelFrame == Time.frameCount)
            {
                return;
            }

            // The terminal modals (result / network panels) own the screen;
            // the pause menu does not stack on top of them.
            if (_matchFrame != null && _matchFrame.ModalOpen) return;

            OpenMenu();
        }

        private void OpenMenu()
        {
            _menuOpen = true;
            _pausedByMenu = false;
            // Only a running LOCAL match is paused (a relay kernel cannot
            // pause independently of its peer — PauseMatch would log an
            // error, so it is not even called). The menu itself stays fully
            // usable in a relay match; it just does not stop the clock.
            if (_runner != null && !_runner.IsRelayMatch && _runner.IsRunning)
            {
                _pausedByMenu = _runner.PauseMatch();
            }
            AudioServiceLocator.Play2D(SoundEventId.UI_Click);
        }

        /// <summary>Closes the menu and resumes the clock if — and only if — this menu paused it.</summary>
        private void CloseMenu(bool playClick)
        {
            _menuOpen = false;
            if (_pausedByMenu)
            {
                _pausedByMenu = false;
                if (_runner != null && !_runner.IsRunning)
                {
                    _runner.StartMatch();
                }
            }
            if (playClick) AudioServiceLocator.Play2D(SoundEventId.UI_Click);
        }

        private void OnGUI()
        {
            if (!_menuOpen) return;

            EnsureStyles();

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Rect rect = CenteredRect(250f);
            GUI.Box(rect, GUIContent.none, HudChrome.OpaquePanelStyle);
            GUILayout.BeginArea(rect);
            GUILayout.Space(HudChrome.OpaquePanelStyle.padding.top);
            GUILayout.Label("PAUSE", _headlineStyle);
            GUILayout.Label(
                _runner != null && _runner.IsRelayMatch
                    ? "Netzpartie — die Simulation läuft im Hintergrund weiter."
                    : "Die Simulation steht — ESC setzt fort.",
                _bodyStyle);
            GUILayout.Space(10f);
            if (GUILayout.Button("Fortsetzen (ESC)", _buttonStyle, GUILayout.Height(34f)))
            {
                CloseMenu(playClick: true);
            }
            if (GUILayout.Button("Zum Hauptmenü", _buttonStyle, GUILayout.Height(30f)))
            {
                AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                // No resume: the menu leaves the match for good, and
                // ReturnToMenu keeps it paused for the next "Neues Spiel".
                _menuOpen = false;
                _pausedByMenu = false;
                if (_menu != null) _menu.ReturnToMenu();
            }
            if (GUILayout.Button("Spiel beenden", _buttonStyle, GUILayout.Height(30f)))
            {
                AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                if (_menu != null) _menu.Quit();
            }
            GUILayout.Space(6f);
            GUILayout.Label("Einstellungen (Ton, Bild, Auflösung) gibt es im Hauptmenü.", _bodyStyle);
            GUILayout.EndArea();

            GUI.matrix = previousMatrix;
        }

        private Rect CenteredRect(float height)
        {
            float scale = Mathf.Max(1f, _uiScale);
            float x = (Screen.width / scale - _panelWidth) * 0.5f;
            float y = (Screen.height / scale - height) * 0.4f;
            return new Rect(x, y, _panelWidth, height);
        }

        private void EnsureStyles()
        {
            if (_headlineStyle == null)
            {
                _headlineStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }
            if (_bodyStyle == null)
            {
                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            }
        }
    }
}
