using System;
using System.Threading;
using System.Threading.Tasks;
using Nova.Gameplay.Match;
using Nova.Networking;
using Nova.Networking.Lobby;
using Nova.Simulation.State;

namespace Nova.Gameplay
{
    /// <summary>Lifecycle phases of the menu-driven lobby flow (sprint 14, D-092).</summary>
    public enum LobbyPhase
    {
        /// <summary>No lobby activity.</summary>
        Idle = 0,
        /// <summary>create-match request in flight.</summary>
        Creating = 1,
        /// <summary>Match created, code known; polling until the opponent appears. Ready may already be set.</summary>
        WaitingForOpponent = 2,
        /// <summary>join-match request in flight.</summary>
        Joining = 3,
        /// <summary>Both players are in; both factions are known; ready flags are exchanged via polling.</summary>
        ReadyExchange = 4,
        /// <summary>Server reported "starting"; the handoff to MatchBootstrap is happening.</summary>
        Starting = 5,
        /// <summary>MatchBootstrap owns the relay handshake now; its JoinStatus drives the progress.</summary>
        HandedOff = 6,
        /// <summary>Terminal failure with a German plain-text Message; Cancel/Reset or a fresh action clears it.</summary>
        Failed = 7,
    }

    /// <summary>
    /// Immutable, UI-safe snapshot of the lobby state. It deliberately
    /// contains no networking types (client, tasks, tokens) — the same
    /// honesty boundary as MatchBootstrap's NetworkJoinStatus.
    /// </summary>
    public readonly struct LobbyStatus
    {
        public LobbyStatus(
            LobbyPhase phase, string code, int localSlot,
            FactionId? slot0Faction, FactionId? slot1Faction,
            bool slot0Ready, bool slot1Ready,
            string opponentBuild, string message)
        {
            Phase = phase;
            Code = code;
            LocalSlot = localSlot;
            Slot0Faction = slot0Faction;
            Slot1Faction = slot1Faction;
            Slot0Ready = slot0Ready;
            Slot1Ready = slot1Ready;
            OpponentBuild = opponentBuild;
            Message = message ?? string.Empty;
        }

        public LobbyPhase Phase { get; }

        /// <summary>Canonical "XXX-XXX" match code; null while no match is held.</summary>
        public string Code { get; }

        /// <summary>Own lobby/relay slot (0 = creator, 1 = joiner); -1 until assigned.</summary>
        public int LocalSlot { get; }

        /// <summary>Faction of lobby slot 0 (the creator), once known.</summary>
        public FactionId? Slot0Faction { get; }

        /// <summary>Faction of lobby slot 1 (the joiner), once known.</summary>
        public FactionId? Slot1Faction { get; }

        public bool Slot0Ready { get; }
        public bool Slot1Ready { get; }

        /// <summary>Build commit of the opponent, once known; null otherwise.</summary>
        public string OpponentBuild { get; }

        /// <summary>German plain-text line for the status band; empty when idle.</summary>
        public string Message { get; }
    }

    /// <summary>
    /// State machine and facade for the sprint-14 match lobby (D-092). It
    /// wraps the engine-free <see cref="LobbyClient"/> behind an
    /// Update-driven, single-flight model so the UI Toolkit menu never
    /// touches threads, tasks or Nova.Networking itself: the menu calls the
    /// actions (<see cref="CreateMatch"/>, <see cref="JoinMatch"/>,
    /// <see cref="SetReady"/>), pumps <see cref="Update"/> every frame and
    /// renders <see cref="Status"/>.
    /// <para>
    /// At most one request is in flight at any time: create/join gate on the
    /// phase, status polls skip while a set-ready runs (and a queued
    /// set-ready blocks the next poll), and the leave on cancel retires its
    /// client along with it. Completed requests are only ever interpreted
    /// when the phase still matches the operation, so a cancel that lands
    /// mid-flight can never resurrect a torn-down session.
    /// </para>
    /// <para>
    /// Once the lobby reports "starting", the session builds the network
    /// <see cref="MatchConfig"/> — the D-093 token plus BOTH slot factions,
    /// which the relay offer does not carry — and hands over to
    /// <see cref="MatchBootstrap.TryStartLobbyMatch"/>. From
    /// <see cref="LobbyPhase.HandedOff"/> on, bootstrap.JoinStatus owns the
    /// progress exactly like the sprint-13 direct path, and polling stops.
    /// </para>
    /// </summary>
    public sealed class LobbySession : IDisposable
    {
        private const float PollIntervalSeconds = 1.5f;
        private const int MaxConsecutivePollNetworkErrors = 3;

        private const string MalformedMessage = "Der Server hat eine ungültige Antwort gesendet.";
        private const string TechnicalMessage = "Technischer Fehler bei der Lobby-Anfrage.";
        private const string NotConfiguredMessage =
            "Die Lobby ist nicht konfiguriert — die Direktverbindung funktioniert weiterhin.";

        private readonly MatchBootstrap _bootstrap;

        private CancellationTokenSource _cancellation;
        private LobbyClient _client;
        private bool _disposed;

        private LobbyPhase _phase = LobbyPhase.Idle;
        private string _failureMessage;
        private string _code;
        private string _relayHost;
        private int _relayPort;
        private int _localSlot = -1;
        private FactionId _localFaction;
        private readonly FactionId?[] _factions = new FactionId?[2];
        private readonly bool[] _ready = new bool[2];
        private string _opponentBuild;

        private float _pollCountdown;
        private int _pollNetworkErrors;
        private bool? _queuedReady;
        private bool _readyValueInFlight;

        private Task<LobbyResult<CreateMatchResponse>> _createTask;
        private Task<LobbyResult<JoinMatchResponse>> _joinTask;
        private Task<LobbyResult<MatchStatusResponse>> _pollTask;
        private Task<LobbyResult<SetReadyResponse>> _readyTask;

        /// <summary>
        /// <paramref name="bootstrap"/> receives the finished config at
        /// handoff; it may be null (unwired scene), which fails cleanly at
        /// handoff time with a plain-text message.
        /// </summary>
        public LobbySession(MatchBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
            _cancellation = new CancellationTokenSource();
        }

        /// <summary>True when a lobby endpoint is configured (environment or Resources asset).</summary>
        public static bool IsConfigured => LobbyConfig.TryLoad(out _);

        /// <summary>The current immutable snapshot for the UI.</summary>
        public LobbyStatus Status => new LobbyStatus(
            _phase, _code, _localSlot,
            _factions[0], _factions[1],
            _ready[0], _ready[1],
            _opponentBuild, ComposeMessage());

        /// <summary>True while a ready toggle is queued or in flight; the UI dims its Bereit button meanwhile.</summary>
        public bool ReadyRequestInFlight => _readyTask != null || _queuedReady != null;

        /// <summary>
        /// Creates a lobby match for <paramref name="faction"/> (slot 0).
        /// Only accepted from Idle/Failed; a held slot from a failed attempt
        /// is released first.
        /// </summary>
        public void CreateMatch(FactionId faction)
        {
            if (_disposed || (_phase != LobbyPhase.Idle && _phase != LobbyPhase.Failed)) return;
            Teardown(sendLeave: true);
            if (!EnsureClient())
            {
                Fail(NotConfiguredMessage);
                return;
            }
            _phase = LobbyPhase.Creating;
            _localFaction = faction;
            _createTask = _client.CreateMatchAsync(BuildInfo.Commit, (int)faction, _cancellation.Token);
        }

        /// <summary>
        /// Joins a lobby match by code for <paramref name="faction"/> (slot 1).
        /// An invalid code fails locally, without any request.
        /// </summary>
        public void JoinMatch(string code, FactionId faction)
        {
            if (_disposed || (_phase != LobbyPhase.Idle && _phase != LobbyPhase.Failed)) return;
            if (!LobbyCode.TryNormalize(code, out string normalized))
            {
                Teardown(sendLeave: true);
                Fail("Der Match-Code muss die Form „XXX-XXX“ haben (ohne die Zeichen 0/O/1/I/L).");
                return;
            }
            Teardown(sendLeave: true);
            if (!EnsureClient())
            {
                Fail(NotConfiguredMessage);
                return;
            }
            _phase = LobbyPhase.Joining;
            _localFaction = faction;
            _code = normalized; // the join response carries no code; kept for status/leave
            _joinTask = _client.JoinMatchAsync(normalized, BuildInfo.Commit, (int)faction, _cancellation.Token);
        }

        /// <summary>
        /// Toggles the own ready flag. The latest call wins: while a poll or
        /// a previous set-ready is in flight, the request is queued and sent
        /// as soon as the wire is free.
        /// </summary>
        public void SetReady(bool ready)
        {
            if (_disposed || _client == null || _code == null || _localSlot < 0) return;
            if (_phase != LobbyPhase.WaitingForOpponent && _phase != LobbyPhase.ReadyExchange) return;
            _queuedReady = ready;
        }

        /// <summary>
        /// User-facing abort: releases the held slot via leave-match (creator
        /// waiting alone included), retires the client and returns to Idle.
        /// </summary>
        public void Cancel()
        {
            if (_disposed) return;
            Teardown(sendLeave: true);
        }

        /// <summary>
        /// Lifecycle twin of <see cref="Cancel"/> — same teardown, safe to
        /// call at any time (e.g. when the menu returns after a match).
        /// Idempotent.
        /// </summary>
        public void Reset()
        {
            if (_disposed) return;
            Teardown(sendLeave: true);
        }

        /// <summary>
        /// Frame pump driven by the UI: completes finished requests and runs
        /// the non-overlapping status poll (~1.5 s) while the lobby is
        /// waiting or exchanging ready flags.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_disposed) return;

            DrainCreate();
            DrainJoin();
            DrainReady();
            DrainPoll();

            if (_phase == LobbyPhase.WaitingForOpponent || _phase == LobbyPhase.ReadyExchange)
            {
                _pollCountdown -= deltaTime;
                if (_pollCountdown <= 0f)
                {
                    _pollCountdown = PollIntervalSeconds;
                    TryStartPoll();
                }
            }

            TryStartReadyRequest();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cancellation.Cancel();
            // No leave here: Dispose runs at scene/teardown time where an HTTP
            // round-trip cannot be awaited; the server-side match expiry reaps
            // the code. The client is closed, never disposed mid-flight twice.
            _client?.Dispose();
            _client = null;
            _phase = LobbyPhase.Idle;
        }

        // --- request completion ------------------------------------------

        private void DrainCreate()
        {
            Task<LobbyResult<CreateMatchResponse>> task = _createTask;
            if (task == null || !task.IsCompleted) return;
            _createTask = null;
            if (task.IsCanceled) return;
            if (_phase != LobbyPhase.Creating) return; // torn down meanwhile
            if (task.IsFaulted)
            {
                Fail(TechnicalMessage);
                return;
            }

            LobbyResult<CreateMatchResponse> result = task.Result;
            if (!result.Ok)
            {
                Fail(result.Message);
                return;
            }
            CreateMatchResponse created = result.Value;
            if (!LobbyCode.TryNormalize(created.Code, out string code)
                || created.Slot < 0 || created.Slot > 1
                || string.IsNullOrWhiteSpace(created.RelayHost))
            {
                Fail(MalformedMessage);
                return;
            }

            _code = code;
            _relayHost = created.RelayHost;
            _relayPort = created.RelayPort;
            _localSlot = created.Slot;
            _factions[_localSlot] = _localFaction;
            _phase = LobbyPhase.WaitingForOpponent;
            _pollCountdown = 0f; // first status check immediately
        }

        private void DrainJoin()
        {
            Task<LobbyResult<JoinMatchResponse>> task = _joinTask;
            if (task == null || !task.IsCompleted) return;
            _joinTask = null;
            if (task.IsCanceled) return;
            if (_phase != LobbyPhase.Joining) return; // torn down meanwhile
            if (task.IsFaulted)
            {
                Fail(TechnicalMessage);
                return;
            }

            LobbyResult<JoinMatchResponse> result = task.Result;
            if (!result.Ok)
            {
                Fail(result.Message); // BuildMismatch text already names both builds
                return;
            }
            JoinMatchResponse joined = result.Value;
            if (joined.Slot < 0 || joined.Slot > 1
                || string.IsNullOrWhiteSpace(joined.RelayHost)
                || !TryConvertFaction(joined.OpponentFaction, out FactionId opponentFaction))
            {
                Fail(MalformedMessage);
                return;
            }

            _relayHost = joined.RelayHost;
            _relayPort = joined.RelayPort;
            _localSlot = joined.Slot;
            _factions[_localSlot] = _localFaction;
            _factions[1 - _localSlot] = opponentFaction; // the joiner always meets the creator's slot
            _opponentBuild = joined.OpponentBuild;
            _phase = LobbyPhase.ReadyExchange;
            _pollCountdown = 0f;
        }

        private void DrainReady()
        {
            Task<LobbyResult<SetReadyResponse>> task = _readyTask;
            if (task == null || !task.IsCompleted) return;
            _readyTask = null;
            if (task.IsCanceled) return;
            if (!IsInRoom()) return; // torn down meanwhile
            if (task.IsFaulted)
            {
                Fail(TechnicalMessage);
                return;
            }

            LobbyResult<SetReadyResponse> result = task.Result;
            if (!result.Ok)
            {
                Fail(result.Message);
                return;
            }
            _ready[_localSlot] = _readyValueInFlight;
        }

        private void DrainPoll()
        {
            Task<LobbyResult<MatchStatusResponse>> task = _pollTask;
            if (task == null || !task.IsCompleted) return;
            _pollTask = null;
            if (task.IsCanceled) return;
            if (!IsInRoom()) return; // torn down meanwhile
            if (task.IsFaulted)
            {
                Fail(TechnicalMessage);
                return;
            }

            LobbyResult<MatchStatusResponse> result = task.Result;
            if (!result.Ok)
            {
                // A short connectivity hiccup must not blow up the lobby;
                // anything else (unknown code, expired, HTTP) is terminal.
                if (result.ErrorKind == LobbyErrorKind.NetworkError)
                {
                    _pollNetworkErrors++;
                    if (_pollNetworkErrors < MaxConsecutivePollNetworkErrors)
                    {
                        return;
                    }
                }
                Fail(result.Message);
                return;
            }
            _pollNetworkErrors = 0;

            MatchStatusResponse status = result.Value;
            if (status.Slots == null || status.Slots.Length != 2)
            {
                Fail(MalformedMessage);
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                MatchSlotStatus slot = status.Slots[i];
                if (slot == null) continue; // empty seat
                if (!TryConvertFaction(slot.Faction, out FactionId faction))
                {
                    Fail(MalformedMessage);
                    return;
                }
                _factions[i] = faction;
                _ready[i] = slot.Ready;
                if (i != _localSlot)
                {
                    _opponentBuild = slot.BuildCommit;
                }
            }

            switch (status.State)
            {
                case LobbyMatchState.Open:
                case LobbyMatchState.Ready:
                    if (_phase == LobbyPhase.WaitingForOpponent && status.Slots[1 - _localSlot] != null)
                    {
                        _phase = LobbyPhase.ReadyExchange;
                    }
                    return;
                case LobbyMatchState.Starting:
                    TryHandOff(status.TokenHex);
                    return;
                case LobbyMatchState.Closed:
                    Fail("Das Match wurde geschlossen. Bitte lege ein neues an.");
                    return;
                case LobbyMatchState.Expired:
                    Fail("Dieses Match ist abgelaufen. Bitte lass dir einen neuen Code geben.");
                    return;
                default:
                    Fail(MalformedMessage);
                    return;
            }
        }

        // --- handoff -------------------------------------------------------

        private void TryHandOff(string tokenHex)
        {
            if (_factions[0] == null || _factions[1] == null || _localSlot < 0
                || string.IsNullOrWhiteSpace(_relayHost)
                || !RelayProtocol.TryParseMatchToken(tokenHex, out ulong token))
            {
                Fail(MalformedMessage);
                return;
            }
            if (_bootstrap == null)
            {
                Fail("Der Match-Start ist in dieser Szene nicht verdrahtet.");
                return;
            }

            _phase = LobbyPhase.Starting;
            // The relay offer carries seed, slot and delay — but NO factions.
            // Both clients must submit the identical FactionPerSlot with their
            // fingerprint, and the lobby status is the shared source for it.
            MatchConfig config = MatchConfig.NetworkVsHuman(_relayHost, _relayPort, token);
            config.FactionPerSlot[0] = _factions[0].Value;
            config.FactionPerSlot[1] = _factions[1].Value;

            if (_bootstrap.TryStartLobbyMatch(config, (byte)_localSlot))
            {
                _phase = LobbyPhase.HandedOff;
                return;
            }

            string message = _bootstrap.JoinStatus.Phase == NetworkJoinPhase.Failed
                ? _bootstrap.JoinStatus.Message
                : "Der Match-Start wurde abgelehnt.";
            Fail(message);
        }

        // --- request starters (single-flight) ------------------------------

        private void TryStartPoll()
        {
            if (_client == null || _code == null || _localSlot < 0) return;
            if (_pollTask != null || _readyTask != null || _queuedReady != null) return;
            _pollTask = _client.GetStatusAsync(_code, _localSlot, _cancellation.Token);
        }

        private void TryStartReadyRequest()
        {
            if (_queuedReady == null || _client == null || _code == null) return;
            if (_readyTask != null || _pollTask != null) return;
            if (!IsInRoom())
            {
                _queuedReady = null;
                return;
            }
            _readyValueInFlight = _queuedReady.Value;
            _queuedReady = null;
            _readyTask = _client.SetReadyAsync(_code, _localSlot, _readyValueInFlight, _cancellation.Token);
        }

        // --- teardown --------------------------------------------------------

        /// <summary>
        /// Cancels every in-flight request, optionally releases a held slot
        /// via leave-match, retires the client and returns to Idle. The leave
        /// is deliberately fire-and-forget on the RETIRED client: disposing
        /// it immediately would kill the request on the wire, so the client
        /// disposes itself when the call completes.
        /// </summary>
        private void Teardown(bool sendLeave)
        {
            _cancellation.Cancel();
            // The source is not disposed: tasks registered on its token may
            // still complete on the thread pool, and touching a disposed
            // source from a continuation is a race. GC reclaims it.
            _cancellation = new CancellationTokenSource();

            bool leavable = sendLeave && _client != null && _code != null && _localSlot >= 0
                && (_phase == LobbyPhase.WaitingForOpponent
                    || _phase == LobbyPhase.ReadyExchange
                    || _phase == LobbyPhase.Failed);
            if (leavable)
            {
                LobbyClient retiring = _client;
                _client = null;
                Task<LobbyResult<LeaveMatchResponse>> leave =
                    retiring.LeaveMatchAsync(_code, _localSlot, CancellationToken.None);
                leave.ContinueWith(
                    _ => retiring.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);
            }
            else
            {
                _client?.Dispose();
                _client = null;
            }

            _createTask = null;
            _joinTask = null;
            _pollTask = null;
            _readyTask = null;
            _queuedReady = null;
            _phase = LobbyPhase.Idle;
            _failureMessage = null;
            _code = null;
            _relayHost = null;
            _relayPort = 0;
            _localSlot = -1;
            _factions[0] = null;
            _factions[1] = null;
            _ready[0] = false;
            _ready[1] = false;
            _opponentBuild = null;
            _pollNetworkErrors = 0;
            _pollCountdown = 0f;
        }

        private bool EnsureClient()
        {
            if (_client != null) return true;
            if (!LobbyConfig.TryLoad(out LobbyConfig config)) return false;
            _client = new LobbyClient(config.SupabaseFunctionsUrl, config.AnonKey);
            return true;
        }

        private void Fail(string message)
        {
            _queuedReady = null;
            _phase = LobbyPhase.Failed;
            _failureMessage = string.IsNullOrWhiteSpace(message) ? "Unbekannter Fehler." : message;
        }

        private bool IsInRoom()
        {
            return _phase == LobbyPhase.WaitingForOpponent || _phase == LobbyPhase.ReadyExchange;
        }

        private string ComposeMessage()
        {
            switch (_phase)
            {
                case LobbyPhase.Failed:
                    return _failureMessage ?? "Unbekannter Fehler.";
                case LobbyPhase.Creating:
                    return "Lege Match an …";
                case LobbyPhase.Joining:
                    return "Trete Match bei …";
                case LobbyPhase.WaitingForOpponent:
                    return OwnReady()
                        ? "Du bist bereit — warte auf einen Gegenspieler …"
                        : "Warte auf einen Gegenspieler …";
                case LobbyPhase.ReadyExchange:
                    bool ownReady = OwnReady();
                    bool opponentReady = _localSlot >= 0 && _ready[1 - _localSlot];
                    if (ownReady && opponentReady) return "Verbinde …";
                    if (ownReady) return "Du bist bereit — warte auf den Gegenspieler …";
                    if (opponentReady) return "Der Gegenspieler ist bereit — drück auf „Bereit“.";
                    return "Drück auf „Bereit“, wenn du startklar bist.";
                case LobbyPhase.Starting:
                case LobbyPhase.HandedOff:
                    return "Verbinde …";
                default:
                    return string.Empty;
            }
        }

        private bool OwnReady()
        {
            return _localSlot >= 0 && _ready[_localSlot];
        }

        private static bool TryConvertFaction(int value, out FactionId faction)
        {
            faction = FactionId.Alliance;
            if (value != (int)FactionId.Alliance && value != (int)FactionId.Legion)
            {
                return false;
            }
            faction = (FactionId)value;
            return true;
        }
    }
}
