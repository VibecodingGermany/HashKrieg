using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Nova.Gameplay.Audio;
using Nova.Gameplay.Match;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The main menu: key art, title, four entries (Neues Spiel / Laden /
    /// Einstellungen / Beenden) and a settings panel, drawn as a UI Toolkit
    /// overlay over the ONE scene this project has.
    /// <para>
    /// OVERLAY, NOT A SECOND SCENE. There is no SceneManager call anywhere in
    /// production code, and a menu scene would be the project's first
    /// scene-flow layer — bought for nothing.
    /// <see cref="MatchBootstrap.AutoStart"/> is off (the scene generator
    /// writes it), so the scene loads into an idle host: the kernel is not
    /// running, every HUD component checks for a running match and draws
    /// nothing, and "Neues Spiel" calls the idempotent
    /// <see cref="MatchBootstrap.StartGrayboxMatch"/> and hides this overlay.
    /// </para>
    /// <para>
    /// UI TOOLKIT, NOT uGUI, and built in C# rather than from UXML. uGUI would
    /// mean real assembly references (UnityEngine.UI, TextMeshPro) in an
    /// asmdef that deliberately references only Nova.* assemblies, plus an
    /// EventSystem in the scene; UI Toolkit's uielements module is an engine
    /// module and needs neither. The tree is built here instead of in a .uxml
    /// because the scene itself is machine output — a hand-authored asset the
    /// generator cannot reproduce would be the one part of the menu nobody
    /// could regenerate. This is the project's first real UI and sets the
    /// standard; everything else is still throwaway OnGUI.
    /// </para>
    /// <para>
    /// HONESTY RULES (sprint §5/§6): "Laden" is shown, disabled and labelled —
    /// the snapshot layer can restore a full match, but nothing ever writes to
    /// disk, so offering it would be a lie and hiding it would raise the wrong
    /// question. The SFX controls apply immediately through the D-090 mixer
    /// bridge; the render-detail dropdown still names what it does not change.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string LoadHintText = "kommt später — es gibt noch kein Speicherformat";

        private const string SfxHintText =
            "Wirkt sofort auf UI-, Waffen-, Treffer- und Einheitensounds.";

        private const string QualityHintText =
            "Wirkt auf LOD-Abstand, Anisotropie, Texturauflösung und Partikelbudget. Renderskalierung, " +
            "Schatten und Kantenglättung bleiben gleich — alle sechs Stufen teilen sich ein URP-Asset.";

        private const string DisplayHintText =
            "Auflösung und Vollbild wirken nur im gebauten Spiel; im Editor bestimmt das Game-Fenster die Größe.";

        [Header("Wiring (scene generator)")]
        [SerializeField] private UIDocument _document;
        [SerializeField] private MatchBootstrap _bootstrap;
        [SerializeField] private MenuMusicPlayer _music;

        [Tooltip("RTS camera rig. Declared as Behaviour, not RtsCameraController: Nova.Presentation.UI and " +
                 "Nova.Presentation are both rank 4 and the architecture gate rejects same-layer references. " +
                 "The scene generator wires the concrete component.")]
        [SerializeField] private Behaviour _cameraRig;

        [Tooltip("Debug HUD. Its status bar draws before its own visibility check, so it would sit on the key art.")]
        [SerializeField] private DebugHud _debugHud;

        [Header("Content (scene generator)")]
        [SerializeField] private Texture2D _keyArt;
        [Tooltip("Rajdhani-Bold — the title.")]
        [SerializeField] private Font _titleFont;
        [Tooltip("Rajdhani-Regular — buttons, labels, settings.")]
        [SerializeField] private Font _bodyFont;

        [Header("Layout")]
        [Tooltip("Brand title. E-3: everything the player sees is called Hashkrieg; the code identity (Nova.*) stays.")]
        [SerializeField] private string _title = "HASHKRIEG";
        [SerializeField] private int _titleFontSize = 92;
        [SerializeField] private float _titleLetterSpacing = 14f;
        [SerializeField] private float _titleRuleWidth = 420f;
        [SerializeField] private float _contentPadding = 120f;
        [SerializeField] private int _buttonFontSize = 30;
        [SerializeField] private float _buttonWidth = 340f;
        [SerializeField] private float _buttonHeight = 56f;
        [SerializeField] private int _fieldFontSize = 17;
        [SerializeField] private float _fieldLabelWidth = 190f;
        [SerializeField] private float _settingsWidth = 680f;
        [Tooltip("Scroll area cap so the panel still fits a short window at the 1920x1080 reference resolution.")]
        [SerializeField] private float _settingsScrollHeight = 480f;

        [Header("Colours (mirrors HudChrome so the menu and the cockpit share one language)")]
        [SerializeField] private Color _titleColor = new Color(0.92f, 0.96f, 1f, 1f);
        [SerializeField] private Color _accentColor = new Color(0.35f, 0.78f, 0.92f, 1f);
        [SerializeField] private Color _bodyColor = new Color(0.88f, 0.91f, 0.95f, 1f);
        [SerializeField] private Color _panelFill = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        [SerializeField] private Color _panelEdge = new Color(0.55f, 0.62f, 0.72f, 0.50f);
        [SerializeField] private Color _buttonFill = new Color(0.08f, 0.10f, 0.13f, 0.85f);
        [SerializeField] private Color _buttonHoverFill = new Color(0.16f, 0.26f, 0.32f, 0.92f);
        [Tooltip("Darkening veil over the key art so white text stays readable on the bright parts of the image.")]
        [SerializeField] private Color _scrimColor = new Color(0.02f, 0.03f, 0.05f, 0.42f);
        [Tooltip("\"Laden\": greyed out, still legible. Opaque on purpose — see BuildMainButtons.")]
        [SerializeField] private Color _disabledTextColor = new Color(0.52f, 0.55f, 0.60f, 1f);
        [SerializeField] private Color _disabledEdgeColor = new Color(0.34f, 0.38f, 0.44f, 0.45f);

        [Header("Persistence")]
        [Tooltip("Seconds a dragged slider may stay unsaved. Discrete controls (toggles, dropdowns) always save at once.")]
        [SerializeField] private float _saveIntervalSeconds = 0.35f;

        private VisualElement _screen;
        private VisualElement _mainPanel;
        private VisualElement _settingsPanel;
        private VisualElement _networkPanel;
        private TextField _networkHost;
        private TextField _networkPort;
        private TextField _networkCode;
        private DropdownField _networkRole;
        private Button _networkConnect;
        private Button _networkCancel;
        private Label _networkStatus;
        private UnitViewManager _views;

        /// <summary>Deduplicated width x height modes, biggest first; index-parallel to the resolution dropdown.</summary>
        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>(16);

        private bool _settingsDirty;
        private float _dirtySince;
        private bool _networkTransitionCommitted;

        /// <summary>
        /// True while the menu overlay owns the screen (initial state, and
        /// after MatchFrameHud's "Hauptmenü"). Read by the input component to
        /// suspend world gestures while the menu is up — a click on a menu
        /// button must never also select or order in the world behind it.
        /// </summary>
        public bool IsMenuVisible { get; private set; }

        private void Awake()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_music == null) _music = GetComponent<MenuMusicPlayer>();
            if (_bootstrap == null) _bootstrap = FindAnyObjectByType<MatchBootstrap>();
            if (_views == null) _views = FindAnyObjectByType<UnitViewManager>();
        }

        private void Start()
        {
            // Before anything is drawn, not on the first button click: the
            // camera rig has no match guard, so a pointer resting near a
            // screen edge would edge-pan the whole time the menu is up and
            // the match would open on a drifted camera. Start() runs before
            // the first LateUpdate and before the first OnGUI of the frame.
            SetGameplayLayerActive(false);
            IsMenuVisible = true;

            LogMissingWiring();

            if (_document == null || _document.rootVisualElement == null)
            {
                Debug.LogError(
                    "[MainMenuController] No UIDocument (or no panel) — there is no menu to click, so the " +
                    "match is started directly to keep the build playable. Re-run " +
                    "Tools/Project Nova/Create Bootstrap Scene.");
                StartMatch();
                return;
            }

            BuildTree();
        }

        /// <summary>
        /// Names every reference the scene generator failed to write, once, at
        /// startup. Nothing here is fatal — a menu without fonts, art or music
        /// still starts a match — but every one of these failures is silent by
        /// nature (a black panel, a mute menu, a drifting camera), and a
        /// silent failure is exactly what this sprint set out to remove.
        /// </summary>
        private void LogMissingWiring()
        {
            if (_bootstrap == null)
            {
                Debug.LogError("[MainMenuController] No MatchBootstrap wired — 'Neues Spiel' will do nothing.");
            }
            if (_music == null)
            {
                Debug.LogError("[MainMenuController] No MenuMusicPlayer wired — the menu stays silent.");
            }
            if (_titleFont == null || _bodyFont == null)
            {
                Debug.LogError(
                    "[MainMenuController] Rajdhani not wired (expected Assets/_Project/UI/Fonts/" +
                    "Rajdhani-Bold.ttf and Rajdhani-Regular.ttf) — the menu falls back to the engine font.");
            }
            if (_cameraRig == null)
            {
                Debug.LogError(
                    "[MainMenuController] No camera rig wired — the RTS camera keeps reading edge pan and " +
                    "scroll wheel while the menu is up, so the match will open on a drifted camera.");
            }
            if (_debugHud == null)
            {
                Debug.LogError(
                    "[MainMenuController] No DebugHud wired — its always-on status bar will draw on top of " +
                    "the main menu.");
            }
        }

        private void OnDisable()
        {
            // Covers the two exits that do not go through a button: leaving
            // Play mode and the player closing the window.
            FlushSettings();
        }

        private void Update()
        {
            if (_settingsDirty && Time.unscaledTime - _dirtySince >= _saveIntervalSeconds)
            {
                FlushSettings();
            }

            UpdateNetworkJoin();
        }

        // --- tree ---------------------------------------------------------

        private void BuildTree()
        {
            VisualElement root = _document.rootVisualElement;
            root.Clear(); // idempotent: a rebuild must not stack two menus

            // The UIDocument root is PickingMode.Ignore and takes its
            // full-screen box from the theme class .unity-ui-document__root,
            // so our own container carries its geometry itself. Position (not
            // Ignore) so it swallows stray clicks: a click that misses a
            // button must not fall through to the scene behind the menu.
            _screen = new VisualElement { name = "menu-screen", pickingMode = PickingMode.Position };
            StretchToParent(_screen);

            if (_keyArt != null)
            {
                _screen.style.backgroundImage = new StyleBackground(_keyArt);
                // Cover, not stretch: the art is 16:9 and must not distort on
                // a 21:9 monitor. Overflow is cropped, which the composition
                // (subject left of centre) tolerates.
                _screen.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            }
            else
            {
                _screen.style.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
                Debug.LogError(
                    "[MainMenuController] No key art wired (expected Assets/_Project/UI/UI_KeyArt_MainMenu.jpg) " +
                    "— the menu falls back to a flat dark background.");
            }
            root.Add(_screen);

            var scrim = new VisualElement { name = "menu-scrim", pickingMode = PickingMode.Ignore };
            StretchToParent(scrim);
            scrim.style.backgroundColor = _scrimColor;
            _screen.Add(scrim);

            // The padding lives on the content column, not on _screen: an
            // absolutely positioned child is laid out against the padding
            // box, so padding up there would inset the scrim as well.
            var content = new VisualElement { name = "menu-content", pickingMode = PickingMode.Ignore };
            content.style.flexGrow = 1f;
            content.style.flexDirection = FlexDirection.Column;
            content.style.justifyContent = Justify.Center;
            content.style.alignItems = Align.FlexStart;
            content.style.paddingLeft = _contentPadding;
            content.style.paddingRight = _contentPadding;
            _screen.Add(content);

            BuildTitle(content);

            _mainPanel = MakePanel("menu-main");
            content.Add(_mainPanel);
            BuildMainButtons(_mainPanel);

            _settingsPanel = MakePanel("menu-settings");
            _settingsPanel.style.display = DisplayStyle.None;
            _settingsPanel.style.maxWidth = _settingsWidth;
            content.Add(_settingsPanel);
            BuildSettings(_settingsPanel);

            _networkPanel = MakePanel("menu-network");
            _networkPanel.style.display = DisplayStyle.None;
            _networkPanel.style.width = _settingsWidth;
            content.Add(_networkPanel);
            BuildNetworkJoin(_networkPanel);
        }

        private void BuildTitle(VisualElement parent)
        {
            var title = new Label(_title) { name = "menu-title", pickingMode = PickingMode.Ignore };
            ApplyFont(title, _titleFont);
            title.style.fontSize = _titleFontSize;
            title.style.letterSpacing = _titleLetterSpacing;
            title.style.color = _titleColor;
            title.style.marginBottom = 2f;
            parent.Add(title);

            var rule = new VisualElement { name = "menu-title-rule", pickingMode = PickingMode.Ignore };
            rule.style.width = _titleRuleWidth;
            rule.style.height = 3f;
            rule.style.backgroundColor = _accentColor;
            rule.style.marginBottom = 26f;
            parent.Add(rule);
        }

        private void BuildMainButtons(VisualElement parent)
        {
            parent.Add(MakeButton("Neues Spiel", StartMatch));
            parent.Add(MakeButton("Netzpartie", () => ShowNetworkPanel(true)));

            Button load = MakeButton("Laden", null);
            load.SetEnabled(false);
            // The tooltip is set because the sprint asks for it and because it
            // documents the button in the Inspector — but it can never reach
            // the player: TooltipEvent is Editor-only, and SetEnabled(false)
            // suppresses pointer events anyway. The hint label below is what
            // is actually read, and it is a sibling at full strength, so the
            // theme's own disabled styling cannot dim the explanation along
            // with the button.
            load.tooltip = "kommt später";
            // Greyed out in solid colours rather than opacity: the runtime
            // theme may add its own :disabled rule, and two stacked opacities
            // would make this unreadable.
            load.style.color = _disabledTextColor;
            load.style.marginBottom = 2f;
            SetBorder(load, 1f, _disabledEdgeColor, 3f);
            parent.Add(load);
            parent.Add(MakeHint(LoadHintText));

            parent.Add(MakeButton("Einstellungen", () => ShowSettings(true)));
            parent.Add(MakeButton("Beenden", Quit));
        }

        private void BuildNetworkJoin(VisualElement parent)
        {
            var header = new Label("Netzpartie");
            ApplyFont(header, _titleFont);
            header.style.fontSize = 34;
            header.style.letterSpacing = 4f;
            header.style.color = _titleColor;
            header.style.marginBottom = 14f;
            parent.Add(header);

            _networkHost = new TextField("Serveradresse")
            {
                name = "network-host",
                value = "127.0.0.1",
            };
            StyleField(_networkHost);
            parent.Add(_networkHost);

            _networkPort = new TextField("Port")
            {
                name = "network-port",
                value = "47777",
                maxLength = 5,
            };
            StyleField(_networkPort);
            parent.Add(_networkPort);

            _networkCode = new TextField("Match-Code")
            {
                name = "network-code",
                isPasswordField = true,
                maxLength = 16,
            };
            StyleField(_networkCode);
            parent.Add(_networkCode);
            parent.Add(MakeHint("Genau 16 Hexzeichen. Der Code wird weder angezeigt noch gespeichert."));

            _networkRole = new DropdownField(
                "Rolle", new List<string> { "Host", "Gast" }, 0)
            {
                name = "network-role",
            };
            StyleField(_networkRole);
            parent.Add(_networkRole);
            parent.Add(MakeHint("Der Host verbindet zuerst; der Gast danach."));

            _networkStatus = new Label("Bereit für eine Verbindung.")
            {
                name = "network-status",
            };
            ApplyFont(_networkStatus, _bodyFont);
            _networkStatus.style.fontSize = _fieldFontSize;
            _networkStatus.style.color = _bodyColor;
            _networkStatus.style.whiteSpace = WhiteSpace.Normal;
            _networkStatus.style.minHeight = 48f;
            _networkStatus.style.marginTop = 8f;
            _networkStatus.style.marginBottom = 12f;
            _networkStatus.style.paddingLeft = 10f;
            _networkStatus.style.paddingRight = 10f;
            _networkStatus.style.paddingTop = 8f;
            _networkStatus.style.paddingBottom = 8f;
            _networkStatus.style.backgroundColor = _buttonFill;
            SetBorder(_networkStatus, 1f, _panelEdge, 3f);
            parent.Add(_networkStatus);

            _networkConnect = MakeButton("Verbinden", StartNetworkJoin);
            _networkConnect.name = "network-connect";
            parent.Add(_networkConnect);

            _networkCancel = MakeButton("Abbrechen", CancelNetworkJoin);
            _networkCancel.name = "network-cancel";
            _networkCancel.style.marginBottom = 0f;
            parent.Add(_networkCancel);
        }

        private void BuildSettings(VisualElement parent)
        {
            var header = new Label("Einstellungen");
            ApplyFont(header, _titleFont);
            header.style.fontSize = 34;
            header.style.letterSpacing = 4f;
            header.style.color = _titleColor;
            header.style.marginBottom = 14f;
            parent.Add(header);

            // The scroll view keeps the panel usable in a short window; the
            // "Zurück" button sits outside it and therefore never scrolls away.
            var scroll = new ScrollView();
            scroll.style.maxHeight = _settingsScrollHeight;
            scroll.style.flexGrow = 0f;
            parent.Add(scroll);

            GameSettings settings = GameSettingsStore.Current;

            scroll.Add(MakeSectionHeader("Ton"));

            var musicToggle = new Toggle("Musik") { value = settings.musicEnabled };
            StyleField(musicToggle);
            musicToggle.RegisterValueChangedCallback(evt =>
            {
                GameSettingsStore.Current.musicEnabled = evt.newValue;
                CommitSettings(immediate: true);
            });
            scroll.Add(musicToggle);

            scroll.Add(MakeVolumeRow(
                "Lautstärke",
                settings.musicVolume,
                value => GameSettingsStore.Current.musicVolume = value));

            var sfxToggle = new Toggle("Soundeffekte") { value = settings.sfxEnabled };
            StyleField(sfxToggle);
            sfxToggle.RegisterValueChangedCallback(evt =>
            {
                GameSettingsStore.Current.sfxEnabled = evt.newValue;
                CommitSettings(immediate: true);
            });
            scroll.Add(sfxToggle);

            scroll.Add(MakeVolumeRow(
                "SFX-Lautstärke",
                settings.sfxVolume,
                value => GameSettingsStore.Current.sfxVolume = value));
            scroll.Add(MakeHint(SfxHintText));

            scroll.Add(MakeSectionHeader("Bild"));
            BuildQualityField(scroll, settings);

            var vsyncToggle = new Toggle("vSync") { value = settings.vsync };
            StyleField(vsyncToggle);
            vsyncToggle.RegisterValueChangedCallback(evt =>
            {
                GameSettingsStore.Current.vsync = evt.newValue;
                CommitSettings(immediate: true);
            });
            scroll.Add(vsyncToggle);

            BuildResolutionField(scroll, settings);

            var fullScreenToggle = new Toggle("Vollbild") { value = settings.fullScreen };
            StyleField(fullScreenToggle);
            fullScreenToggle.RegisterValueChangedCallback(evt =>
            {
                GameSettingsStore.Current.fullScreen = evt.newValue;
                CommitSettings(immediate: true);
            });
            scroll.Add(fullScreenToggle);
            scroll.Add(MakeHint(DisplayHintText));

            Button back = MakeButton("Zurück", () => ShowSettings(false));
            back.style.marginTop = 16f;
            back.style.marginBottom = 0f;
            parent.Add(back);
        }

        private void BuildQualityField(VisualElement parent, GameSettings settings)
        {
            var names = new List<string>(QualitySettings.names);
            if (names.Count == 0) return; // no quality levels: nothing honest to offer

            var quality = new DropdownField(
                "Render-Detail", names, Mathf.Clamp(settings.qualityLevel, 0, names.Count - 1));
            StyleField(quality);
            quality.RegisterValueChangedCallback(_ =>
            {
                GameSettingsStore.Current.qualityLevel = quality.index;
                CommitSettings(immediate: true);
            });
            parent.Add(quality);
            parent.Add(MakeHint(QualityHintText));
        }

        private void BuildResolutionField(VisualElement parent, GameSettings settings)
        {
            CollectResolutions();
            if (_resolutions.Count == 0) return;

            var choices = new List<string>(_resolutions.Count);
            int selected = 0;
            for (int i = 0; i < _resolutions.Count; i++)
            {
                Vector2Int size = _resolutions[i];
                choices.Add($"{size.x} × {size.y}");
                if (size.x == settings.resolutionWidth && size.y == settings.resolutionHeight)
                {
                    selected = i;
                }
            }

            // 0/0 means "never chosen" (GameSettings' documented default), so
            // preselect what the window is actually running instead of
            // claiming the largest mode is active.
            if (settings.resolutionWidth <= 0 || settings.resolutionHeight <= 0)
            {
                selected = Mathf.Max(0, _resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height)));
            }

            var resolution = new DropdownField("Auflösung", choices, selected);
            StyleField(resolution);
            resolution.RegisterValueChangedCallback(_ =>
            {
                int index = Mathf.Clamp(resolution.index, 0, _resolutions.Count - 1);
                GameSettingsStore.Current.resolutionWidth = _resolutions[index].x;
                GameSettingsStore.Current.resolutionHeight = _resolutions[index].y;
                CommitSettings(immediate: true);
            });
            parent.Add(resolution);
        }

        /// <summary>
        /// Deduplicates <see cref="Screen.resolutions"/> down to width x
        /// height — the list carries one entry per refresh rate, and a
        /// dropdown that shows 1920x1080 six times is noise, not information.
        /// Biggest first, because that is what a player looks for.
        /// </summary>
        private void CollectResolutions()
        {
            _resolutions.Clear();

            Resolution[] available = Screen.resolutions;
            for (int i = 0; i < available.Length; i++)
            {
                var size = new Vector2Int(available[i].width, available[i].height);
                if (!_resolutions.Contains(size)) _resolutions.Add(size);
            }

            // The mode the game currently runs in is not necessarily a listed
            // fullscreen mode (a windowed size, or batch mode where the list
            // is empty). Keeping it in means the dropdown never shows a
            // resolution that is not the one on screen.
            var current = new Vector2Int(Screen.width, Screen.height);
            if (current.x > 0 && current.y > 0 && !_resolutions.Contains(current))
            {
                _resolutions.Add(current);
            }

            _resolutions.Sort((a, b) => b.x != a.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
        }

        // --- actions ------------------------------------------------------

        private void ShowSettings(bool show)
        {
            _mainPanel.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            _settingsPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_networkPanel != null) _networkPanel.style.display = DisplayStyle.None;
            if (!show) FlushSettings(); // leaving the panel persists whatever a slider left dirty
        }

        private void ShowNetworkPanel(bool show)
        {
            _mainPanel.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            _settingsPanel.style.display = DisplayStyle.None;
            _networkPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show && _bootstrap != null)
            {
                NetworkJoinStatus status = _bootstrap.JoinStatus;
                _networkStatus.text = status.Phase == NetworkJoinPhase.Idle
                    ? "Bereit für eine Verbindung."
                    : status.Message;
            }
        }

        private void StartNetworkJoin()
        {
            if (_bootstrap == null)
            {
                _networkStatus.text = "Match-Start ist in dieser Szene nicht verdrahtet.";
                return;
            }

            NetworkJoinRole role = _networkRole.index == 0
                ? NetworkJoinRole.Host
                : NetworkJoinRole.Guest;
            _networkTransitionCommitted = false;
            _bootstrap.TryStartNetworkJoin(
                _networkHost.value,
                _networkPort.value,
                _networkCode.value,
                role);
            // The match code is never kept in UI state beyond the click.
            _networkCode.SetValueWithoutNotify(string.Empty);
            UpdateNetworkJoin();
        }

        private void CancelNetworkJoin()
        {
            _bootstrap?.CancelNetworkJoin();
            _bootstrap?.ResetNetworkJoin();
            _views?.ResetViews();
            _networkCode?.SetValueWithoutNotify(string.Empty);
            _networkTransitionCommitted = false;
            ShowNetworkPanel(false);
        }

        private void UpdateNetworkJoin()
        {
            if (_bootstrap == null || _networkStatus == null) return;

            NetworkJoinStatus status = _bootstrap.JoinStatus;
            if (status.Phase != NetworkJoinPhase.Idle)
            {
                _networkStatus.text = status.Message;
            }

            bool active = status.IsActive;
            _networkHost?.SetEnabled(!active);
            _networkPort?.SetEnabled(!active);
            _networkCode?.SetEnabled(!active);
            _networkRole?.SetEnabled(!active);
            _networkConnect?.SetEnabled(!active);
            _networkCancel?.SetEnabled(true);

            if (!_networkTransitionCommitted
                && status.Phase == NetworkJoinPhase.Ready
                && _bootstrap.IsMatchReady
                && _bootstrap.Runner != null
                && _bootstrap.Runner.IsRunning)
            {
                _networkTransitionCommitted = true;
                IsMenuVisible = false;
                SetGameplayLayerActive(true);
                if (_music != null) _music.FadeOutAndStop();
                if (_screen != null) _screen.style.display = DisplayStyle.None;
                enabled = false;
            }
        }

        private void StartMatch()
        {
            FlushSettings();

            if (_bootstrap == null)
            {
                Debug.LogError(
                    "[MainMenuController] No MatchBootstrap wired — 'Neues Spiel' cannot start a match. " +
                    "Re-run Tools/Project Nova/Create Bootstrap Scene.");
                return;
            }

            IsMenuVisible = false;
            // Order matters: match first, rig second. RtsCameraController
            // resets the minimap channel in its OnEnable, and that reset
            // belongs to the fresh match rather than to the empty scene.
            // A match that already ran is restarted cleanly instead of being
            // stacked (RestartMatch rebuilds the kernel and every system).
            if (_bootstrap.IsMatchReady)
            {
                _bootstrap.RestartMatch();
            }
            else
            {
                _bootstrap.StartGrayboxMatch();
            }
            SetGameplayLayerActive(true);

            if (_music != null) _music.FadeOutAndStop();
            if (_screen != null) _screen.style.display = DisplayStyle.None;

            // Nothing left to drive — until a match ends: MatchFrameHud's
            // "Hauptmenü" button re-enters through ReturnToMenu below.
            enabled = false;
        }

        /// <summary>
        /// The way back from a finished match (MatchFrameHud's "Hauptmenü"
        /// button): pause the host, hide the cockpit layer, bring the menu
        /// screen and its music back. The match state itself is left for the
        /// NEXT "Neues Spiel", which restarts it via
        /// <see cref="MatchBootstrap.RestartMatch"/> — the menu does not
        /// demolish what it may never need again.
        /// </summary>
        public void ReturnToMenu()
        {
            if (_bootstrap != null && _bootstrap.Runner != null && _bootstrap.Runner.IsRunning)
            {
                _bootstrap.Runner.PauseMatch();
            }
            SetGameplayLayerActive(false);

            // The menu music component switched itself off when its fade
            // landed on match start; re-enabling re-enters OnEnable, which
            // applies the stored volume and replays the loop.
            if (_music != null && !_music.enabled)
            {
                _music.enabled = true;
            }
            if (_screen != null)
            {
                _screen.style.display = DisplayStyle.Flex;
            }
            IsMenuVisible = true;
            _networkTransitionCommitted = false;
            enabled = true;
        }

        private void Quit()
        {
            FlushSettings();
#if UNITY_EDITOR
            // Application.Quit is a no-op in the Editor; leaving Play mode is
            // the equivalent gesture and makes the button testable at all.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Silences (or restores) the two scene components that do NOT guard
        /// themselves against "no match yet". The list is short on purpose:
        /// every other HUD component already returns early without a running
        /// match, and a catalogue of every HUD in the scene would rot with
        /// the next one added.
        /// <list type="bullet">
        /// <item>The camera rig has no guard at all — its LateUpdate reads
        /// scroll wheel, MMB, Z/X, arrow keys and the screen-edge pan every
        /// frame. Awake has already applied the start framing, so a disabled
        /// rig sits exactly where the match wants it.</item>
        /// <item>The debug HUD draws its always-on status bar BEFORE its own
        /// visibility check, so its text would sit on top of the key art.</item>
        /// </list>
        /// </summary>
        private void SetGameplayLayerActive(bool active)
        {
            if (_cameraRig != null) _cameraRig.enabled = active;
            if (_debugHud != null) _debugHud.enabled = active;
        }

        // --- settings persistence ----------------------------------------

        /// <summary>
        /// Records a settings change. Discrete controls (toggles, dropdowns)
        /// commit at once; a dragged slider raises one change event per frame,
        /// and each commit runs <see cref="GameSettingsStore.ApplyToEngine"/>
        /// (which may call <c>Screen.SetResolution</c>) plus a file write, so
        /// slider changes are coalesced into at most one commit per
        /// <see cref="_saveIntervalSeconds"/>. The audible part is not
        /// delayed: the music volume goes onto the source in the same frame,
        /// which is what the player is listening for while dragging.
        /// </summary>
        private void CommitSettings(bool immediate)
        {
            GameSettingsStore.Current.Sanitize();
            if (_music != null) _music.ApplyVolume(GameSettingsStore.Current);
            AudioServiceLocator.Current?.SetBusVolume(
                AudioBus.Sfx,
                GameSettingsStore.Current.EffectiveSfxVolume);

            if (!_settingsDirty)
            {
                _settingsDirty = true;
                _dirtySince = Time.unscaledTime;
            }
            if (immediate) FlushSettings();
        }

        private void FlushSettings()
        {
            if (!_settingsDirty) return;
            _settingsDirty = false;
            GameSettingsStore.ApplyAndSave();
        }

        // --- element factories --------------------------------------------

        /// <summary>
        /// A menu panel in the same restrained chrome the in-game HUD uses
        /// (<see cref="HudChrome"/>'s dark translucent fill and light border
        /// ring, mirrored here because that class is IMGUI-only): the menu
        /// leads into the cockpit, so it should not speak a second visual
        /// language.
        /// </summary>
        private VisualElement MakePanel(string elementName)
        {
            var panel = new VisualElement { name = elementName };
            panel.style.backgroundColor = _panelFill;
            panel.style.paddingLeft = 26f;
            panel.style.paddingRight = 26f;
            panel.style.paddingTop = 22f;
            panel.style.paddingBottom = 22f;
            panel.style.alignItems = Align.Stretch;
            SetBorder(panel, 1f, _panelEdge, 4f);
            return panel;
        }

        private Button MakeButton(string text, Action onClick)
        {
            var button = new Button { text = text };
            if (onClick != null)
            {
                button.clicked += () =>
                {
                    AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                    onClick();
                };
            }

            ApplyFont(button, _bodyFont);
            button.style.fontSize = _buttonFontSize;
            button.style.height = _buttonHeight;
            button.style.minWidth = _buttonWidth;
            button.style.letterSpacing = 2f;
            button.style.color = _bodyColor;
            button.style.backgroundColor = _buttonFill;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.paddingLeft = 20f;
            button.style.paddingRight = 20f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 10f;
            SetBorder(button, 1f, _panelEdge, 3f);

            // Inline styles cannot express :hover and a runtime panel built in
            // C# has no stylesheet to put one in, so the hover tint rides on
            // the two pointer events. A disabled button never receives them —
            // which is exactly what "Laden" needs.
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.backgroundColor = _buttonHoverFill);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.backgroundColor = _buttonFill);
            return button;
        }

        private Label MakeSectionHeader(string text)
        {
            var label = new Label(text);
            ApplyFont(label, _titleFont);
            label.style.fontSize = 20;
            label.style.letterSpacing = 3f;
            label.style.color = _accentColor;
            label.style.marginTop = 14f;
            label.style.marginBottom = 8f;
            return label;
        }

        /// <summary>Small muted note under a control — this menu explains its own limits instead of hiding them.</summary>
        private Label MakeHint(string text)
        {
            var label = new Label(text);
            ApplyFont(label, _bodyFont);
            label.style.fontSize = 14;
            label.style.color = _bodyColor;
            label.style.opacity = 0.68f;
            label.style.marginBottom = 10f;
            label.style.maxWidth = _settingsWidth - 80f;
            // Labels do not wrap by default; a two-line hint would otherwise
            // run off the panel.
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        /// <summary>
        /// A 0..1 slider with a live percentage readout next to it. The
        /// readout exists because a bare slider gives no way to tell 40 % from
        /// 45 % — and the music default is deliberately low (0.4), which looks
        /// like a bug without a number beside it.
        /// </summary>
        private VisualElement MakeVolumeRow(string label, float initialValue, Action<float> apply)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var slider = new Slider(label, 0f, 1f) { value = initialValue };
            slider.style.flexGrow = 1f;
            StyleField(slider);

            var readout = new Label(FormatPercent(initialValue));
            ApplyFont(readout, _bodyFont);
            readout.style.width = 62f;
            readout.style.fontSize = _fieldFontSize;
            readout.style.color = _bodyColor;
            readout.style.unityTextAlign = TextAnchor.MiddleRight;

            // Registered AFTER the initial value is set, so building the tree
            // does not look like a player change and does not write the file.
            slider.RegisterValueChangedCallback(evt =>
            {
                apply(evt.newValue);
                readout.text = FormatPercent(evt.newValue);
                CommitSettings(immediate: false);
            });

            row.Add(slider);
            row.Add(readout);
            return row;
        }

        private void StyleField<TValue>(BaseField<TValue> field)
        {
            // color and unityFontDefinition are inherited properties, so one
            // assignment on the field covers its label and its inner text.
            ApplyFont(field, _bodyFont);
            field.style.fontSize = _fieldFontSize;
            field.style.color = _bodyColor;
            field.style.marginBottom = 6f;
            field.labelElement.style.minWidth = _fieldLabelWidth;
        }

        // --- style helpers -------------------------------------------------

        private static void ApplyFont(VisualElement element, Font font)
        {
            if (font == null) return; // the theme's default font is the fallback; the missing file is logged once
            element.style.unityFontDefinition = FontDefinition.FromFont(font);
        }

        private static void StretchToParent(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
        }

        private static void SetBorder(VisualElement element, float width, Color color, float radius)
        {
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;

            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;

            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private static string FormatPercent(float value) => $"{Mathf.RoundToInt(value * 100f)} %";
    }
}
