// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using System.Text;
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.Economy;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Read-only OnGUI overlay over a running <see cref="MatchRunner"/>: tick,
    /// economy, selection, Aetherium reserves and the control legend. No
    /// Canvas, no uGUI, no prefabs, no materials.
    /// <para>
    /// STRICTLY READ-ONLY: this component never mutates simulation state and
    /// never submits a command. Every value is copied out of the sim per frame
    /// at the presentation boundary.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugHud : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [SerializeField] private bool _visible = true;
        [Tooltip("Whole GUI is scaled by this factor so it stays legible on a Retina display.")]
        [SerializeField] private float _uiScale = 2f;
        [SerializeField] private int _fontSize = 13;

        private readonly StringBuilder _builder = new StringBuilder(256);
        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void OnGUI()
        {
            if (!_visible) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, richText = false, wordWrap = true };
            }

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float width = Mathf.Min(560f, Screen.width / scale - 16f);
            GUILayout.BeginArea(new Rect(8f, 8f, width, Screen.height / scale - 16f));
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(false));

            if (_runner == null)
            {
                GUILayout.Label("Nova graybox HUD — no MatchRunner in the scene.", _labelStyle);
            }
            else
            {
                DrawMatchLine();
                DrawEconomyLine();
                DrawSelectionLine();
                DrawFieldLine();
                if (_input != null) GUILayout.Label($"Last command: {_input.LastCommandStatus}", _labelStyle);
                string legend = _input != null ? _input.ControlLegend : null;
                GUILayout.Label(string.IsNullOrEmpty(legend) ? "Controls: no RtsDeviceInput in the scene." : legend, _labelStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawMatchLine()
        {
            uint tick = _runner.Kernel != null ? _runner.Kernel.CurrentTick.Value : 0u;
            string state = _runner.IsRunning ? "running" : _runner.Kernel == null ? "not initialized" : "stopped";
            GUILayout.Label($"Nova graybox HUD — tick {tick} ({state}, 10 Hz lockstep)", _labelStyle);
        }

        private void DrawEconomyLine()
        {
            if (_runner.Economy == null)
            {
                GUILayout.Label("Economy: not initialized.", _labelStyle);
                return;
            }

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            // Copies the ref return into a local: the HUD reads, never writes.
            PlayerEconomyState economy = _runner.Economy.GetPlayerEconomy(slot);
            string power = economy.IsLowPower ? "LOW POWER" : "ok";
            GUILayout.Label(
                $"Slot {slot}: {economy.AetheriumCredits} AE | power {economy.PowerProvided}/{economy.PowerRequired} ({power})",
                _labelStyle);
        }

        private void DrawSelectionLine()
        {
            if (_input == null)
            {
                GUILayout.Label("Selection: unavailable (no RtsDeviceInput).", _labelStyle);
                return;
            }
            string wiring = _input.IsWired ? string.Empty : "  [input not wired — orders are dropped]";
            GUILayout.Label($"Selection: {_input.SelectionCount} unit(s){wiring}", _labelStyle);
        }

        /// <summary>
        /// Aetherium reserves. EconomySystem exposes only FieldCount and a
        /// by-id lookup, so the ids are probed ascending and the scan stops
        /// once FieldCount fields are found (canonical setup uses ids 1 and 2).
        /// </summary>
        private void DrawFieldLine()
        {
            if (_runner.Economy == null) return;

            _builder.Clear();
            _builder.Append("Aetherium: ");
            int found = 0;
            for (ushort fieldId = 1; fieldId <= EconomySystem.MaxFields && found < _runner.Economy.FieldCount; fieldId++)
            {
                if (!_runner.Economy.TryGetField(fieldId, out AetheriumField field)) continue;
                if (found > 0) _builder.Append("  |  ");
                _builder.Append('#').Append(field.FieldId)
                    .Append(" (").Append(field.GridPos.X).Append(',').Append(field.GridPos.Y).Append(") ")
                    .Append(field.RemainingAE).Append(" AE");
                if (field.IsExhausted) _builder.Append(" EXHAUSTED");
                found++;
            }
            if (found == 0) _builder.Append("no fields registered");
            GUILayout.Label(_builder.ToString(), _labelStyle);
        }
    }
}
