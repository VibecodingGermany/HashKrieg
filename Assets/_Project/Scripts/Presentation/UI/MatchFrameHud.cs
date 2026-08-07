// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Victory;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The match frame (sprint 09 §6): the two modal panels that close a
    /// round — the RESULT screen once <see cref="VictorySystem"/> latches an
    /// outcome ("Sieg" / "Niederlage" / "Unentschieden" with the decided-tick
    /// timestamp), and the PAUSE overlay while the kernel clock is stopped
    /// (a paused match and a broken one used to look identical). Both offer
    /// "Neue Runde" and/or "Hauptmenü", so quitting the application is no
    /// longer the only way out of a finished round.
    /// <para>
    /// "Neue Runde" runs <see cref="MatchBootstrap.RestartMatch"/> (the sim
    /// side is rebuilt wholesale) and then resets the presentation side:
    /// views via <see cref="UnitViewManager.ResetViews"/>, the camera through
    /// <see cref="MinimapCameraLink.RequestStartFocusReset"/> — the rig owns
    /// the reset itself because Nova.Presentation.UI may not reference
    /// Nova.Presentation (same rank). The selection clears itself on the
    /// ingress rebind (RtsDeviceInput.EnsureDispatcher), and the fog overlay
    /// plus minimap self-heal through their fog-instance guards.
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

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_bootstrap == null) _bootstrap = FindAnyObjectByType<MatchBootstrap>();
            if (_menu == null) _menu = FindAnyObjectByType<MainMenuController>();
            if (_views == null) _views = FindAnyObjectByType<UnitViewManager>();
        }

        private void OnGUI()
        {
            if (_runner == null || _bootstrap == null || !_bootstrap.IsMatchReady) return;
            if (_menu != null && _menu.IsMenuVisible) return;

            VictorySystem victory = _runner.Victory;
            if (victory == null) return;

            EnsureStyles();

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (victory.IsDecided)
            {
                DrawResultPanel(victory);
            }
            else if (!_runner.IsRunning)
            {
                DrawPausePanel();
            }

            GUI.matrix = previousMatrix;
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
                RestartRound();
            }
            if (GUILayout.Button("Hauptmenü", _buttonStyle, GUILayout.Height(30f)))
            {
                ReturnToMenu();
            }
            GUILayout.EndArea();
        }

        private void DrawPausePanel()
        {
            Rect rect = CenteredRect(150f);
            GUI.Box(rect, GUIContent.none, HudChrome.OpaquePanelStyle);
            GUILayout.BeginArea(rect);
            GUILayout.Space(HudChrome.OpaquePanelStyle.padding.top);
            GUILayout.Label("PAUSE", _headlineStyle);
            GUILayout.Label("Die Simulation steht — P oder der Knopf setzt fort.", _bodyStyle);
            GUILayout.Space(10f);
            if (GUILayout.Button("Fortsetzen (P)", _buttonStyle, GUILayout.Height(30f)))
            {
                _runner.StartMatch();
            }
            if (GUILayout.Button("Hauptmenü", _buttonStyle, GUILayout.Height(26f)))
            {
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
