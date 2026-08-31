using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Economy;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The always-on resource bar (issue #137): one line, top-right below the
    /// status strip, answering three things the running match previously kept
    /// invisible outside the F3 debug panel — the Aetherium balance AGAINST
    /// its storage ceiling ("Aetherium 2.318 / 3.000", D-024: what lies above
    /// the ceiling is forfeit), the power balance with its deficit state
    /// (16.6 C4: production and repair halve, the radar goes dark) and a
    /// warning segment for the moments a number alone is not enough (store
    /// full, overflow decaying, power deficit). All formatting and state
    /// decisions live in the Unity-free <see cref="ResourceBarPresenter"/>
    /// (Nova.Gameplay, EditMode-tested); this component only reads the sim,
    /// caches one model per frame and paints it.
    /// <para>
    /// STRICTLY READ-ONLY, like the DebugHud: every value is copied out of
    /// <see cref="MatchRunner.Economy"/> per frame. The ceiling is read
    /// through <see cref="EconomySystem.CapacityFor"/> on every model rebuild
    /// — never a cached or hard-coded number, because the ceiling moves with
    /// the living building stock (and with economy rule changes).
    /// </para>
    /// <para>
    /// CLICK BEHAVIOUR — A DELIBERATE DECISION: the bar is a pure readout
    /// with NO interactive element, so it registers no hit test with
    /// RtsDeviceInput.IsPointerOverHud and swallows no click. It draws only
    /// GUI.Box/GUI.Label, which never claim the mouse in IMGUI (only
    /// interactive controls do), so a click on the bar reaches the world
    /// exactly like a click on the DebugHud status strip above it — the
    /// established behaviour of every non-interactive HUD readout in this
    /// cockpit. (Registering a hit test would require editing RtsDeviceInput,
    /// which this slice must not touch — and there is nothing here a click
    /// could meaningfully press.)
    /// </para>
    /// <para>
    /// LAYOUT: explicit content-width rects, no GUILayout stacking — the
    /// "EstimateHeight must mirror OnGUI row for row" trap documented in
    /// CommandCardHud cannot snap here because there is no hidden height
    /// mirror: the bar is exactly one row of a fixed serialized height, its
    /// width is measured from the real styles (GUIStyle.CalcSize) and docked
    /// right via <see cref="ResourceBarPresenter.TopRightZone"/>. The vertical
    /// dock sits below the DebugHud status strip; <see cref="_topOffset"/>
    /// mirrors that strip's serialized defaults (8 px margin + 13+6 px height
    /// + 4 px gap = 31) and is inspector-adjustable if the strip ever moves.
    /// The model is rebuilt at most once per frame — OnGUI runs twice
    /// (Layout + Repaint) and CapacityFor re-scans the entity store on every
    /// call (the DebugHud census sets the caching precedent).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceBarHud : MonoBehaviour
    {
        /// <summary>Horizontal inset between the chrome frame and the first/last segment.</summary>
        private const float ContentInset = 8f;

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;

        [Header("Presentation")]
        [Tooltip("Whole bar is scaled by this factor, matching the DebugHud/BuildMenuHud convention for Retina displays.")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private int _fontSize = 13;
        [SerializeField] private float _margin = 8f;
        [Tooltip("Top edge in scaled GUI space. Docks below the DebugHud status strip (its defaults: 8 margin + 19 height + 4 gap = 31); adjust here if the strip moves.")]
        [SerializeField] private float _topOffset = 31f;
        [Tooltip("One row of text plus air — fixed, so no height estimate ever has to mirror the drawing code.")]
        [SerializeField] private float _barHeight = 22f;

        [Header("Warning colours")]
        [Tooltip("Amber: the store is full — income stops, but nothing burns yet.")]
        [SerializeField] private Color _warningColor = new Color(1f, 0.72f, 0.25f);
        [Tooltip("Red: an active penalty — the overflow decays per second, or the grid is in deficit.")]
        [SerializeField] private Color _criticalColor = new Color(1f, 0.38f, 0.32f);

        private GUIStyle _valueStyle;
        private GUIStyle _alertValueStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _criticalStyle;

        // Reused content shells for the width measurement — no per-frame
        // GUIContent allocations (the styles are measured with the very
        // content instance that is drawn).
        private readonly GUIContent _aetheriumContent = new GUIContent();
        private readonly GUIContent _powerContent = new GUIContent();
        private readonly GUIContent _warningContent = new GUIContent();

        // One model per frame: OnGUI fires Layout AND Repaint, and
        // CapacityFor re-sweeps the entity store on every call.
        private ResourceBarModel _model;
        private int _modelFrame = -1;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
        }

        private void OnGUI()
        {
            if (_runner == null) return;
            EconomySystem economy = _runner.Economy;
            if (economy == null) return; // menu, or the match is not initialized yet

            EnsureStyles();

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawBar(ModelForCurrentFrame(economy), scale);

            GUI.matrix = previousMatrix;
        }

        /// <summary>
        /// The frame's model: the local slot's credits, ceiling and power
        /// balance, copied out of the sim (the HUD reads, never writes) and
        /// mapped by the pure presenter. Rebuilt at most once per frame.
        /// </summary>
        private ResourceBarModel ModelForCurrentFrame(EconomySystem economy)
        {
            if (_modelFrame == Time.frameCount) return _model;
            _modelFrame = Time.frameCount;

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            PlayerEconomyState playerEconomy = economy.GetPlayerEconomy(slot);
            _model = ResourceBarPresenter.BuildModel(
                playerEconomy.AetheriumCredits,
                economy.CapacityFor(slot),
                playerEconomy.PowerProvided,
                playerEconomy.PowerRequired);
            return _model;
        }

        /// <summary>
        /// Chrome frame plus the two (three under warning) text segments,
        /// right-docked at the measured content width. Segments carry the
        /// separator as a prefix, so the separator takes its segment's
        /// colour and never needs a fifth label.
        /// </summary>
        private void DrawBar(in ResourceBarModel model, float scale)
        {
            _aetheriumContent.text = model.AetheriumText;
            _powerContent.text = ResourceBarPresenter.SegmentSeparator + model.PowerText;
            GUIStyle powerStyle = model.IsLowPower ? _alertValueStyle : _valueStyle;
            _warningContent.text = model.WarningText != null
                ? ResourceBarPresenter.SegmentSeparator + model.WarningText
                : null;

            float aetheriumWidth = _valueStyle.CalcSize(_aetheriumContent).x;
            float powerWidth = powerStyle.CalcSize(_powerContent).x;
            float warningWidth = model.WarningText != null ? WarningStyle(model).CalcSize(_warningContent).x : 0f;
            float contentWidth = ContentInset + aetheriumWidth + powerWidth + warningWidth + ContentInset;

            Rect zone = ToRect(ResourceBarPresenter.TopRightZone(
                HudLayout.GuiWidth(scale), _topOffset, contentWidth, _barHeight, _margin));
            GUI.Box(zone, GUIContent.none, HudChrome.PanelStyle);

            float x = zone.x + ContentInset;
            GUI.Label(new Rect(x, zone.y, aetheriumWidth, zone.height), _aetheriumContent, _valueStyle);
            x += aetheriumWidth;
            GUI.Label(new Rect(x, zone.y, powerWidth, zone.height), _powerContent, powerStyle);
            if (model.WarningText != null)
            {
                x += powerWidth;
                GUI.Label(new Rect(x, zone.y, warningWidth, zone.height), _warningContent, WarningStyle(model));
            }
        }

        /// <summary>Amber for "store full", red for an active penalty (overflow decay or power deficit) — the model's IsCritical flag decides.</summary>
        private GUIStyle WarningStyle(in ResourceBarModel model)
        {
            return model.IsCritical ? _criticalStyle : _warningStyle;
        }

        private void EnsureStyles()
        {
            if (_valueStyle == null)
            {
                _valueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _fontSize,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    padding = new RectOffset(0, 0, 0, 0),
                };
            }
            if (_alertValueStyle == null)
            {
                // The power pair itself goes red in a deficit: the deficit is
                // the number's meaning, not an annotation beside it.
                _alertValueStyle = new GUIStyle(_valueStyle);
                _alertValueStyle.normal.textColor = _criticalColor;
            }
            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(_valueStyle) { fontStyle = FontStyle.Bold };
                _warningStyle.normal.textColor = _warningColor;
            }
            if (_criticalStyle == null)
            {
                _criticalStyle = new GUIStyle(_valueStyle) { fontStyle = FontStyle.Bold };
                _criticalStyle.normal.textColor = _criticalColor;
            }
        }

        private static Rect ToRect(HudRect zone) => new Rect(zone.X, zone.Y, zone.Width, zone.Height);

        private void OnDestroy()
        {
            HudChrome.DestroyShared();
        }
    }
}
