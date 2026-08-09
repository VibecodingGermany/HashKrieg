using Newtonsoft.Json;

namespace Nova.Networking.Lobby
{
    /// <summary>Error categories of the lobby client. Every non-Ok <see cref="LobbyResult{T}"/> carries one.</summary>
    public enum LobbyErrorKind
    {
        /// <summary>No error; the call succeeded.</summary>
        None = 0,
        /// <summary>409 relay_busy: the server is currently hosting another match.</summary>
        RelayBusy,
        /// <summary>404 unknown_code: no open match under this code.</summary>
        UnknownCode,
        /// <summary>410 expired: the match code is no longer valid.</summary>
        Expired,
        /// <summary>409 build_mismatch: both clients run different builds.</summary>
        BuildMismatch,
        /// <summary>409 match_full: the match already has two players.</summary>
        MatchFull,
        /// <summary>Any other non-success HTTP status.</summary>
        HttpError,
        /// <summary>The server was unreachable (connect failure or timeout).</summary>
        NetworkError,
        /// <summary>The 200 response was not parseable or missed required fields.</summary>
        MalformedResponse,
    }

    /// <summary>
    /// Outcome of one lobby Edge-Function call: on success <see cref="Ok"/> with
    /// <see cref="Value"/>; on failure <see cref="ErrorKind"/> plus a German
    /// plain-text <see cref="Message"/> suitable for direct UI display.
    /// Never carries secret material (anon key, match tokens) in messages.
    /// </summary>
    public sealed class LobbyResult<T> where T : class
    {
        private LobbyResult(T value)
        {
            Ok = true;
            Value = value;
            ErrorKind = LobbyErrorKind.None;
            Message = null;
        }

        private LobbyResult(LobbyErrorKind errorKind, int httpStatusCode, string creatorBuild, string yourBuild)
        {
            Ok = false;
            Value = null;
            ErrorKind = errorKind;
            HttpStatusCode = httpStatusCode;
            CreatorBuild = creatorBuild;
            YourBuild = yourBuild;
            Message = MessageFor(errorKind, httpStatusCode, creatorBuild, yourBuild);
        }

        public bool Ok { get; }
        /// <summary>Response payload; null on failure.</summary>
        public T Value { get; }
        public LobbyErrorKind ErrorKind { get; }
        /// <summary>German plain-text error description; null on success.</summary>
        public string Message { get; }
        /// <summary>HTTP status, set for <see cref="LobbyErrorKind.HttpError"/> (and informative on mapped statuses); 0 otherwise.</summary>
        public int HttpStatusCode { get; }
        /// <summary>Build commit of the match creator; only for <see cref="LobbyErrorKind.BuildMismatch"/>.</summary>
        public string CreatorBuild { get; }
        /// <summary>Build commit of the joining player; only for <see cref="LobbyErrorKind.BuildMismatch"/>.</summary>
        public string YourBuild { get; }

        public static LobbyResult<T> Success(T value)
        {
            return new LobbyResult<T>(value);
        }

        public static LobbyResult<T> Failure(LobbyErrorKind errorKind, int httpStatusCode = 0, string creatorBuild = null, string yourBuild = null)
        {
            return new LobbyResult<T>(errorKind, httpStatusCode, creatorBuild, yourBuild);
        }

        private static string MessageFor(LobbyErrorKind errorKind, int httpStatusCode, string creatorBuild, string yourBuild)
        {
            switch (errorKind)
            {
                case LobbyErrorKind.RelayBusy:
                    return "Aktuell wird bereits ein anderes Match vermittelt. Bitte versuche es gleich erneut.";
                case LobbyErrorKind.UnknownCode:
                    return "Dieser Match-Code ist unbekannt. Bitte prüfe die Eingabe.";
                case LobbyErrorKind.Expired:
                    return "Dieses Match ist abgelaufen. Bitte lass dir einen neuen Code geben.";
                case LobbyErrorKind.BuildMismatch:
                    return $"Ihr spielt auf unterschiedliche Versionen (Lobby-Ersteller: {creatorBuild ?? "?"}, du: {yourBuild ?? "?"}). Bitte aktualisiere das Spiel.";
                case LobbyErrorKind.MatchFull:
                    return "Dieses Match ist bereits voll.";
                case LobbyErrorKind.HttpError:
                    return $"Unerwartete Serverantwort (HTTP {httpStatusCode}).";
                case LobbyErrorKind.NetworkError:
                    return "Der Server ist nicht erreichbar. Bitte prüfe deine Internetverbindung.";
                case LobbyErrorKind.MalformedResponse:
                    return "Der Server hat eine ungültige Antwort gesendet.";
                default:
                    return "Unbekannter Fehler.";
            }
        }
    }

    // ----- Request DTOs (camelCase on the wire) -----

    public sealed class CreateMatchRequest
    {
        [JsonProperty("buildCommit")] public string BuildCommit { get; set; }
        [JsonProperty("faction")] public int Faction { get; set; }
    }

    public sealed class JoinMatchRequest
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("buildCommit")] public string BuildCommit { get; set; }
        [JsonProperty("faction")] public int Faction { get; set; }
    }

    public sealed class MatchStatusRequest
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("slot")] public int Slot { get; set; }
    }

    public sealed class SetReadyRequest
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("ready")] public bool Ready { get; set; }
    }

    public sealed class LeaveMatchRequest
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("slot")] public int Slot { get; set; }
    }

    // ----- Response DTOs. Required.Always makes missing fields fail the
    // deserialization, which the client maps to MalformedResponse. -----

    public sealed class CreateMatchResponse
    {
        [JsonProperty("code", Required = Required.Always)] public string Code { get; set; }
        [JsonProperty("relayHost", Required = Required.Always)] public string RelayHost { get; set; }
        [JsonProperty("relayPort", Required = Required.Always)] public int RelayPort { get; set; }
        [JsonProperty("slot", Required = Required.Always)] public int Slot { get; set; }
    }

    public sealed class JoinMatchResponse
    {
        [JsonProperty("relayHost", Required = Required.Always)] public string RelayHost { get; set; }
        [JsonProperty("relayPort", Required = Required.Always)] public int RelayPort { get; set; }
        [JsonProperty("slot", Required = Required.Always)] public int Slot { get; set; }
        [JsonProperty("opponentFaction", Required = Required.Always)] public int OpponentFaction { get; set; }
        [JsonProperty("opponentBuild", Required = Required.Always)] public string OpponentBuild { get; set; }
    }

    /// <summary>One player slot inside <see cref="MatchStatusResponse"/>; an empty slot is a null array entry.</summary>
    public sealed class MatchSlotStatus
    {
        [JsonProperty("faction", Required = Required.Always)] public int Faction { get; set; }
        [JsonProperty("ready", Required = Required.Always)] public bool Ready { get; set; }
        [JsonProperty("buildCommit", Required = Required.Always)] public string BuildCommit { get; set; }
    }

    public sealed class MatchStatusResponse
    {
        [JsonProperty("state", Required = Required.Always)] public string State { get; set; }
        /// <summary>Exactly two entries; an empty slot is null.</summary>
        [JsonProperty("slots", Required = Required.Always)] public MatchSlotStatus[] Slots { get; set; }
        /// <summary>16 hex characters, only set when <see cref="State"/> is <see cref="LobbyMatchState.Starting"/>.</summary>
        [JsonProperty("tokenHex")] public string TokenHex { get; set; }
    }

    public sealed class SetReadyResponse
    {
        [JsonProperty("state", Required = Required.Always)] public string State { get; set; }
    }

    /// <summary>leave-match answers an empty object.</summary>
    public sealed class LeaveMatchResponse
    {
    }

    /// <summary>Error body of 4xx responses; all fields optional because the shape varies per endpoint.</summary>
    public sealed class LobbyErrorBody
    {
        [JsonProperty("error")] public string Error { get; set; }
        [JsonProperty("creatorBuild")] public string CreatorBuild { get; set; }
        [JsonProperty("yourBuild")] public string YourBuild { get; set; }
    }

    /// <summary>Wire values of <see cref="MatchStatusResponse.State"/>.</summary>
    public static class LobbyMatchState
    {
        public const string Open = "open";
        public const string Ready = "ready";
        public const string Starting = "starting";
        public const string Closed = "closed";
        public const string Expired = "expired";
    }
}
