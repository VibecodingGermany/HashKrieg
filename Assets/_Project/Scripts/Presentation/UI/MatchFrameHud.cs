// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Audio;
using Nova.Gameplay.Match;
using Nova.Simulation.Victory;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The match frame (sprint 09 §6): the modal panels that close a round —
    /// the RESULT screen once <see cref="VictorySystem"/> latches an outcome
    /// ("Sieg" / "Niederlage" / "Unentschieden" with the decided-tick
    /// timestamp), and the NETWORK status panels for a relay match that
    /// ended or stalls. Both offer a way out ("Neue Runde" and/or
    /// "Hauptmenü"), so quitting the application is no longer the only exit
    /// from a finished round. The PAUSE surface no longer lives here: since
    /// sprint 21.8 it is <see cref="PauseMenuHud"/> (ESC/P), which also owns
    /// the kernel clock.
    /// <para>
    /// "Neue Runde" runs <see cref="MatchBootstrap.RestartMatch"/> (the sim
    /// side is rebuilt wholesale) and then resets the presentation side:
    /// views via <see cref="UnitViewManager.ResetViews"/>, the camera through
    /// <see cref="MinimapCameraLink.RequestStartFocusReset"/> — the rig owns
    /// the reset itself because Nova.Presentation.UI may not reference
    /// Nova.Presentation (same rank). The selection and any armed gesture
    /// clear themselves on the ingress rebind (RtsDeviceInput.EnsureDispatcher),
    /// and the fog overlay plus minimap self-heal through their fog-instance
    /// guards.
    /// </para>
    /// <para>
    /// <see cref="ModalOpen"/> reports once per frame whether one of these
    /// panels is up; PauseMenuHud aggregates it into
    /// <see cref="ModalSurfaceLink"/>, the channel that tells the input
    /// component "a modal owns every click" — a click on "Hauptmenü" must
    /// never fall through the panel and select a unit behind it.
    /// </para>
    /// <para>
    /// READ-ONLY toward the simulation: this panel polls the victory system's
    /// public surface and never submits a command.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchFrameHud : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private MatchBootstrap _bootstrap;
        [SerializeField] private MainMenuController _menu;
        [SerializeField] private UnitViewManager _views;

        [Header("Presentation")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private float _panelWidth = 340f;

        private GUIStyle _headlineStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        // The panel state, derived once per frame in Update: OnGUI draws from
        // it (possibly several passes per frame) and PauseMenuHud reads
        // ModalOpen for the ModalSurfaceLink publish — one derivation, no
        // drift between "is a panel up" and "which panel is drawn".
        private VictorySystem _panelVictory;
        private string _panelNetworkReason;
        private int _panelStalledSlot;

        /// <summary>
        /// True while one of this component's modal panels (result or
        /// network status) is up. Computed once per frame in Update;
        /// PauseMenuHud aggregates it into <see cref="ModalSurfaceLink"/>.
        /// </summary>
        public bool ModalOpen { get; private set; }

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_bootstrap == null) _bootstrap = FindAnyObjectByType<MatchBootstrap>();
            if (_menu == null) _menu = FindAnyObjectByType<MainMenuController>();
            if (_views == null) _views = FindAnyObjectByType<UnitViewManager>();
        }

        private void Update()
        {
            ModalOpen = ComputeModalState();
        }

        /// <summary>
        /// The modal verdict and the panel payload behind it. "Modal" means
        /// the decided-result screen, a relay end reason or a stalled relay
        /// handshake — the paused-with-victory-pending state is deliberately
        /// absent: it belongs to the pause menu now.
        /// </summary>
        private bool ComputeModalState()
        {
            _panelVictory = null;
            _panelNetworkReason = null;
            _panelStalledSlot = -1;
            if (_runner == null || _bootstrap == null) return false;
            if (_menu != null && _menu.IsMenuVisible) return false;

            bool isMatchReady = _bootstrap.IsMatchReady;
            _panelVictory = isMatchReady ? _runner.Victory : null;
            _panelNetworkReason = isMatchReady
                ? _runner.RelayEndReason
                : _bootstrap.NetworkStatusReason;
            _panelStalledSlot = isMatchReady && _runner.IsRelayMatch && _runner.RelayCommandsAllowed
                ? _bootstrap.NetworkStalledOnSlot
                : -1;

            return (_panelVictory != null && _panelVictory.IsDecided)
                || !string.IsNullOrEmpty(_panelNetworkReason)
                || _panelStalledSlot >= 0;
        }

        private void OnGUI()
        {
            if (!ModalOpen) return;

            EnsureStyles();

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (_panelVictory != null && _panelVictory.IsDecided)
            {
                DrawResultPanel(_panelVictory);
            }
            else if (!string.IsNullOrEmpty(_panelNetworkReason))
            {
                DrawNetworkStatusPanel("NETZWERKFEHLER", _panelNetworkReason);
            }
            else if (_panelStalledSlot >= 0)
            {
                DrawNetworkStatusPanel(
                    "VERBINDUNG",
                    $"Warte auf Spieler {_panelStalledSlot + 1} … {_bootstrap.NetworkStallSeconds:0.0}s");
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawNetworkStatusPanel(string headline, string detail)
        {
            Rect rect = CenteredRect(110f);
            GUI.Box(rect, GUIContent.none, HudChrome.OpaquePanelStyle);
            GUILayout.BeginArea(rect);
            GUILayout.Space(HudChrome.OpaquePanelStyle.padding.top);
            GUILayout.Label(headline, _headlineStyle);
            GUILayout.Label(detail, _bodyStyle);
            GUILayout.EndArea();
        }

        private void DrawResultPanel(VictorySystem victory)
        {
            byte localSlot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;

            string headline;
            switch (victory.Outcome)
            {
                case MatchOutcome.VictoryElimination:
                    headline = victory.WinnerSlot == localSlot ? "SIEG" : "NIEDERLAGE";
                    break;
                case MatchOutcome.DrawMutualAnnihilation:
                    headline = "UNENTSCHIEDEN — gegenseitige Vernichtung";
                    break;
                case MatchOutcome.DrawTimeLimit:
                    headline = "UNENTSCHIEDEN — Zeitlimit";
                    break;
                default:
                    headline = victory.Outcome.ToString();
                    break;
            }

            float minutes = victory.DecidedTick * MatchRunner.TickDeltaTime / 60f;
            string detail = $"Runde entschieden bei Tick {victory.DecidedTick} (~{minutes:0.0} min)";

            Rect rect = CenteredRect(190f);
            GUI.Box(rect, GUIContent.none, HudChrome.OpaquePanelStyle);
            GUILayout.BeginArea(rect);
            GUILayout.Space(HudChrome.OpaquePanelStyle.padding.top);
            GUILayout.Label(headline, _headlineStyle);
            GUILayout.Label(detail, _bodyStyle);
            GUILayout.Space(14f);
            if (GUILayout.Button("Neue Runde", _buttonStyle, GUILayout.Height(34f)))
            {
                AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                RestartRound();
            }
            if (GUILayout.Button("Hauptmenü", _buttonStyle, GUILayout.Height(30f)))
            {
                AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                ReturnToMenu();
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// The full round reset: sim side via the bootstrap, view side via
        /// the view manager, camera via the link channel (see the class
        /// remarks for why the rig is not called directly).
        /// </summary>
        private void RestartRound()
        {
            _bootstrap.RestartMatch();
            if (_views != null) _views.ResetViews();
            MinimapCameraLink.RequestStartFocusReset();
        }

        private void ReturnToMenu()
        {
            if (_menu != null)
            {
                _menu.ReturnToMenu();
            }
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
