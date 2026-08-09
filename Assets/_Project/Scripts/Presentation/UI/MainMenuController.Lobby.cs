using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.State;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The lobby half of the main menu (sprint 14, D-092): one panel with
    /// three views — entry (create / join / direct connection), create and
    /// join — drawn in the same C#-only UI Toolkit style as the rest of the
    /// menu. All lobby logic lives in <see cref="LobbySession"/>
    /// (Nova.Gameplay); this file only builds elements, forwards clicks and
    /// renders the session's immutable <see cref="LobbyStatus"/>. Like the
    /// rest of Presentation.UI it references Nova.Networking nowhere.
    /// <para>
    /// The sprint-13 direct connection stays fully intact: it is reachable
    /// from the lobby entry view and opens the unchanged network panel. Once
    /// a session reaches <see cref="LobbyPhase.HandedOff"/>, the bootstrap's
    /// JoinStatus drives the status band and the same transition guard as
    /// the direct path hides the menu when the match runs.
    /// </para>
    /// </summary>
    public sealed partial class MainMenuController
    {
        private enum LobbyView
        {
            Entry,
            Create,
            Join,
        }

        private VisualElement _lobbyPanel;

        private VisualElement _lobbyEntryView;
        private Button _lobbyCreateEntry;
        private Button _lobbyJoinEntry;
        private Label _lobbyUnavailableHint;

        private VisualElement _lobbyCreateView;
        private VisualElement _lobbyCreateForm;
        private VisualElement _lobbyCreateRoom;
        private DropdownField _lobbyCreateFaction;
        private Button _lobbyCreateButton;
        private Label _lobbyCreateCode;
        private Label _lobbyCreateInfo;
        private Label _lobbyCreateStatus;
        private Button _lobbyCreateReady;

        private VisualElement _lobbyJoinView;
        private VisualElement _lobbyJoinForm;
        private VisualElement _lobbyJoinRoom;
        private TextField _lobbyJoinCode;
        private DropdownField _lobbyJoinFaction;
        private Button _lobbyJoinButton;
        private Label _lobbyJoinRoomCode;
        private Label _lobbyJoinInfo;
        private Label _lobbyJoinStatus;
        private Button _lobbyJoinReady;

        private LobbySession _lobbySession;
        private bool _lobbyTransitionCommitted;

        // --- tree ---------------------------------------------------------

        private void BuildLobby(VisualElement parent)
        {
            var header = new Label("Netzpartie");
            ApplyFont(header, _titleFont);
            header.style.fontSize = 34;
            header.style.letterSpacing = 4f;
            header.style.color = _titleColor;
            header.style.marginBottom = 14f;
            parent.Add(header);

            _lobbyEntryView = new VisualElement { name = "lobby-entry" };
            parent.Add(_lobbyEntryView);
            BuildLobbyEntry(_lobbyEntryView);

            _lobbyCreateView = new VisualElement { name = "lobby-create" };
            _lobbyCreateView.style.display = DisplayStyle.None;
            parent.Add(_lobbyCreateView);
            BuildLobbyCreate(_lobbyCreateView);

            _lobbyJoinView = new VisualElement { name = "lobby-join" };
            _lobbyJoinView.style.display = DisplayStyle.None;
            parent.Add(_lobbyJoinView);
            BuildLobbyJoin(_lobbyJoinView);
        }

        private void BuildLobbyEntry(VisualElement parent)
        {
            _lobbyCreateEntry = MakeButton("Match anlegen", () => ShowLobbyView(LobbyView.Create));
            _lobbyCreateEntry.name = "lobby-entry-create";
            parent.Add(_lobbyCreateEntry);

            _lobbyJoinEntry = MakeButton("Match beitreten", () => ShowLobbyView(LobbyView.Join));
            _lobbyJoinEntry.name = "lobby-entry-join";
            parent.Add(_lobbyJoinEntry);

            // Shown only when no lobby endpoint is configured; the two
            // buttons above are disabled then. Honesty rule, same as "Laden".
            _lobbyUnavailableHint = MakeHint(
                "Lobby nicht konfiguriert — Direktverbindung funktioniert weiterhin.");
            _lobbyUnavailableHint.name = "lobby-unavailable";
            parent.Add(_lobbyUnavailableHint);

            Button direct = MakeButton("Direktverbindung …", () =>
            {
                _lobbySession?.Cancel();
                ShowNetworkPanel(true);
            });
            direct.name = "lobby-entry-direct";
            parent.Add(direct);
            parent.Add(MakeHint("Verbindet ohne Lobby direkt mit einem Relay-Server."));

            Button back = MakeButton("Zurück", () => ShowLobbyPanel(false));
            back.name = "lobby-entry-back";
            back.style.marginBottom = 0f;
            parent.Add(back);
        }

        private void BuildLobbyCreate(VisualElement parent)
        {
            parent.Add(MakeSectionHeader("Match anlegen"));

            _lobbyCreateForm = new VisualElement { name = "lobby-create-form" };
            parent.Add(_lobbyCreateForm);

            _lobbyCreateFaction = new DropdownField(
                "Fraktion", new List<string> { "Allianz", "Legion" }, 0)
            {
                name = "lobby-create-faction",
            };
            StyleField(_lobbyCreateFaction);
            _lobbyCreateForm.Add(_lobbyCreateFaction);
            _lobbyCreateForm.Add(MakeHint("Dein Gegenspieler wählt seine Fraktion beim Beitreten."));

            _lobbyCreateButton = MakeButton("Match anlegen", StartLobbyCreate);
            _lobbyCreateButton.name = "lobby-create-start";
            _lobbyCreateForm.Add(_lobbyCreateButton);

            _lobbyCreateRoom = new VisualElement { name = "lobby-create-room" };
            _lobbyCreateRoom.style.display = DisplayStyle.None;
            parent.Add(_lobbyCreateRoom);

            _lobbyCreateCode = MakeCodeLabel("lobby-create-code");
            _lobbyCreateRoom.Add(_lobbyCreateCode);
            _lobbyCreateRoom.Add(MakeHint("Gib diesen Code deinem Mitspieler."));

            _lobbyCreateInfo = MakeInfoLabel("lobby-create-info");
            _lobbyCreateRoom.Add(_lobbyCreateInfo);

            _lobbyCreateReady = MakeButton("Bereit", ToggleLobbyReady);
            _lobbyCreateReady.name = "lobby-create-ready";
            _lobbyCreateRoom.Add(_lobbyCreateReady);

            _lobbyCreateStatus = MakeStatusBand("lobby-create-status");
            parent.Add(_lobbyCreateStatus);

            Button cancel = MakeButton("Abbrechen", CancelLobby);
            cancel.name = "lobby-create-cancel";
            cancel.style.marginBottom = 0f;
            parent.Add(cancel);
        }

        private void BuildLobbyJoin(VisualElement parent)
        {
            parent.Add(MakeSectionHeader("Match beitreten"));

            _lobbyJoinForm = new VisualElement { name = "lobby-join-form" };
            parent.Add(_lobbyJoinForm);

            _lobbyJoinCode = new TextField("Match-Code")
            {
                name = "lobby-join-code",
                // The code is no secret (unlike the direct path's relay
                // token): six alphabet characters plus the display dash.
                maxLength = 7,
            };
            StyleField(_lobbyJoinCode);
            _lobbyJoinForm.Add(_lobbyJoinCode);
            _lobbyJoinForm.Add(MakeHint("Sechs Zeichen, zum Beispiel K7F-2Q9."));

            _lobbyJoinFaction = new DropdownField(
                "Fraktion", new List<string> { "Allianz", "Legion" }, 0)
            {
                name = "lobby-join-faction",
            };
            StyleField(_lobbyJoinFaction);
            _lobbyJoinForm.Add(_lobbyJoinFaction);

            _lobbyJoinButton = MakeButton("Beitreten", StartLobbyJoin);
            _lobbyJoinButton.name = "lobby-join-start";
            _lobbyJoinForm.Add(_lobbyJoinButton);

            _lobbyJoinRoom = new VisualElement { name = "lobby-join-room" };
            _lobbyJoinRoom.style.display = DisplayStyle.None;
            parent.Add(_lobbyJoinRoom);

            _lobbyJoinRoomCode = MakeCodeLabel("lobby-join-room-code");
            _lobbyJoinRoom.Add(_lobbyJoinRoomCode);

            _lobbyJoinInfo = MakeInfoLabel("lobby-join-info");
            _lobbyJoinRoom.Add(_lobbyJoinInfo);

            _lobbyJoinReady = MakeButton("Bereit", ToggleLobbyReady);
            _lobbyJoinReady.name = "lobby-join-ready";
            _lobbyJoinRoom.Add(_lobbyJoinReady);

            _lobbyJoinStatus = MakeStatusBand("lobby-join-status");
            parent.Add(_lobbyJoinStatus);

            Button cancel = MakeButton("Abbrechen", CancelLobby);
            cancel.name = "lobby-join-cancel";
            cancel.style.marginBottom = 0f;
            parent.Add(cancel);
        }

        // --- actions --------------------------------------------------------

        private void ShowLobbyPanel(bool show)
        {
            _mainPanel.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            _settingsPanel.style.display = DisplayStyle.None;
            if (_networkPanel != null) _networkPanel.style.display = DisplayStyle.None;
            if (_lobbyPanel != null) _lobbyPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                EnsureLobbySession();
                ShowLobbyView(LobbyView.Entry);
            }
            else
            {
                // Leaving the panel lets go of any held lobby slot.
                _lobbySession?.Cancel();
            }
        }

        private void ShowLobbyView(LobbyView view)
        {
            _lobbyEntryView.style.display = view == LobbyView.Entry ? DisplayStyle.Flex : DisplayStyle.None;
            _lobbyCreateView.style.display = view == LobbyView.Create ? DisplayStyle.Flex : DisplayStyle.None;
            _lobbyJoinView.style.display = view == LobbyView.Join ? DisplayStyle.Flex : DisplayStyle.None;
            if (view == LobbyView.Entry)
            {
                RefreshLobbyAvailability();
            }
        }

        private void RefreshLobbyAvailability()
        {
            bool configured = LobbySession.IsConfigured;
            _lobbyCreateEntry.SetEnabled(configured);
            _lobbyJoinEntry.SetEnabled(configured);
            _lobbyUnavailableHint.style.display = configured ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private LobbySession EnsureLobbySession()
        {
            if (_lobbySession == null)
            {
                // A null bootstrap is tolerated: the session fails cleanly at
                // handoff with a plain-text message instead of starting.
                _lobbySession = new LobbySession(_bootstrap);
            }
            return _lobbySession;
        }

        private void StartLobbyCreate()
        {
            LobbySession session = EnsureLobbySession();
            int index = Mathf.Clamp(_lobbyCreateFaction.index, 0, 1);
            session.CreateMatch((FactionId)index);
            UpdateLobby();
        }

        private void StartLobbyJoin()
        {
            LobbySession session = EnsureLobbySession();
            int index = Mathf.Clamp(_lobbyJoinFaction.index, 0, 1);
            // The code field keeps its value: a typo or an expired match is
            // retried with one edit, not with retyping everything.
            session.JoinMatch(_lobbyJoinCode.value, (FactionId)index);
            UpdateLobby();
        }

        private void ToggleLobbyReady()
        {
            LobbySession session = EnsureLobbySession();
            LobbyStatus status = session.Status;
            bool ownReady = status.LocalSlot == 1 ? status.Slot1Ready : status.Slot0Ready;
            session.SetReady(!ownReady);
            UpdateLobby();
        }

        private void CancelLobby()
        {
            if (_lobbySession != null
                && _lobbySession.Status.Phase == LobbyPhase.HandedOff
                && _bootstrap != null)
            {
                // The relay handshake already owns the match — cancel it the
                // same way the direct path's Abbrechen does.
                _bootstrap.CancelNetworkJoin();
                _bootstrap.ResetNetworkJoin();
                _views?.ResetViews();
            }
            _lobbySession?.Cancel();
            _lobbyTransitionCommitted = false;
            ShowLobbyView(LobbyView.Entry);
        }

        private void UpdateLobby()
        {
            if (_lobbySession == null || _lobbyPanel == null) return;

            _lobbySession.Update(Time.unscaledDeltaTime);

            // Same transition contract as UpdateNetworkJoin: the menu stays
            // until the relay handshake finished and the kernel runs.
            if (!_lobbyTransitionCommitted
                && IsMenuVisible
                && _lobbySession.Status.Phase == LobbyPhase.HandedOff
                && _bootstrap != null
                && _bootstrap.JoinStatus.Phase == NetworkJoinPhase.Ready
                && _bootstrap.IsMatchReady
                && _bootstrap.Runner != null
                && _bootstrap.Runner.IsRunning)
            {
                _lobbyTransitionCommitted = true;
                IsMenuVisible = false;
                SetGameplayLayerActive(true);
                if (_music != null) _music.FadeOutAndStop();
                if (_screen != null) _screen.style.display = DisplayStyle.None;
                enabled = false;
                return;
            }

            if (_lobbyPanel.style.display == DisplayStyle.None) return;
            RefreshLobbyUi(_lobbySession);
        }

        // --- status rendering ----------------------------------------------

        private void RefreshLobbyUi(LobbySession session)
        {
            LobbyStatus status = session.Status;
            bool inRoom = status.Phase == LobbyPhase.WaitingForOpponent
                || status.Phase == LobbyPhase.ReadyExchange
                || status.Phase == LobbyPhase.Starting
                || status.Phase == LobbyPhase.HandedOff;
            bool busy = status.Phase == LobbyPhase.Creating || status.Phase == LobbyPhase.Joining;

            _lobbyCreateForm.style.display = inRoom ? DisplayStyle.None : DisplayStyle.Flex;
            _lobbyCreateRoom.style.display = inRoom ? DisplayStyle.Flex : DisplayStyle.None;
            _lobbyJoinForm.style.display = inRoom ? DisplayStyle.None : DisplayStyle.Flex;
            _lobbyJoinRoom.style.display = inRoom ? DisplayStyle.Flex : DisplayStyle.None;

            _lobbyCreateButton.SetEnabled(!busy);
            _lobbyCreateFaction.SetEnabled(!busy);
            _lobbyJoinButton.SetEnabled(!busy);
            _lobbyJoinCode.SetEnabled(!busy);
            _lobbyJoinFaction.SetEnabled(!busy);

            string code = status.Code ?? "···";
            _lobbyCreateCode.text = code;
            _lobbyJoinRoomCode.text = code;

            string info = LobbyInfoLine(status);
            _lobbyCreateInfo.text = info;
            _lobbyJoinInfo.text = info;

            bool canReady = status.Phase == LobbyPhase.WaitingForOpponent
                || status.Phase == LobbyPhase.ReadyExchange;
            bool ownReady = status.LocalSlot == 1 ? status.Slot1Ready : status.Slot0Ready;
            string readyText = ownReady ? "Bereitschaft zurücknehmen" : "Bereit";
            _lobbyCreateReady.SetEnabled(canReady && !session.ReadyRequestInFlight);
            _lobbyCreateReady.text = readyText;
            _lobbyJoinReady.SetEnabled(canReady && !session.ReadyRequestInFlight);
            _lobbyJoinReady.text = readyText;

            string message = LobbyStatusMessage(status);
            _lobbyCreateStatus.text = !string.IsNullOrEmpty(message)
                ? message
                : "Wähle deine Fraktion und lege das Match an.";
            _lobbyJoinStatus.text = !string.IsNullOrEmpty(message)
                ? message
                : "Frag deinen Mitspieler nach dem Code.";
        }

        /// <summary>
        /// After HandedOff the relay handshake owns the progress, so its
        /// JoinStatus text (including its failure classification) wins.
        /// </summary>
        private string LobbyStatusMessage(LobbyStatus status)
        {
            if (status.Phase == LobbyPhase.HandedOff && _bootstrap != null)
            {
                NetworkJoinStatus join = _bootstrap.JoinStatus;
                if (join.Phase != NetworkJoinPhase.Idle)
                {
                    return join.Message;
                }
            }
            return status.Message;
        }

        private static string LobbyInfoLine(LobbyStatus status)
        {
            FactionId? own = status.LocalSlot == 1 ? status.Slot1Faction : status.Slot0Faction;
            FactionId? opponent = status.LocalSlot == 1 ? status.Slot0Faction : status.Slot1Faction;
            bool ownReady = status.LocalSlot == 1 ? status.Slot1Ready : status.Slot0Ready;
            bool opponentReady = status.LocalSlot == 1 ? status.Slot0Ready : status.Slot1Ready;

            string line = $"Du: {FactionDisplayName(own)} ({(ownReady ? "bereit" : "nicht bereit")})";
            line += $"  ·  Gegenspieler: {FactionDisplayName(opponent)}";
            if (opponent != null)
            {
                line += opponentReady ? " (bereit)" : " (nicht bereit)";
            }
            return line;
        }

        private static string FactionDisplayName(FactionId? faction)
        {
            if (faction == FactionId.Alliance) return "Allianz";
            if (faction == FactionId.Legion) return "Legion";
            return "—";
        }

        // --- element factories ----------------------------------------------

        /// <summary>The match code, big enough to be read out loud over voice chat.</summary>
        private Label MakeCodeLabel(string elementName)
        {
            var label = new Label("···") { name = elementName };
            ApplyFont(label, _titleFont);
            label.style.fontSize = 44;
            label.style.letterSpacing = 6f;
            label.style.color = _accentColor;
            label.style.marginTop = 4f;
            label.style.marginBottom = 2f;
            return label;
        }

        /// <summary>The faction/ready summary line inside a lobby room.</summary>
        private Label MakeInfoLabel(string elementName)
        {
            var label = new Label(string.Empty) { name = elementName };
            ApplyFont(label, _bodyFont);
            label.style.fontSize = _fieldFontSize;
            label.style.color = _bodyColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 6f;
            label.style.marginBottom = 10f;
            return label;
        }

        /// <summary>Same status band chrome as the network panel's.</summary>
        private Label MakeStatusBand(string elementName)
        {
            var label = new Label(string.Empty) { name = elementName };
            ApplyFont(label, _bodyFont);
            label.style.fontSize = _fieldFontSize;
            label.style.color = _bodyColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.minHeight = 48f;
            label.style.marginTop = 8f;
            label.style.marginBottom = 12f;
            label.style.paddingLeft = 10f;
            label.style.paddingRight = 10f;
            label.style.paddingTop = 8f;
            label.style.paddingBottom = 8f;
            label.style.backgroundColor = _buttonFill;
            SetBorder(label, 1f, _panelEdge, 3f);
            return label;
        }
    }
}
