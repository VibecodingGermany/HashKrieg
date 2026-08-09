using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nova.Networking.Lobby
{
    /// <summary>
    /// Engine-free client for the sprint-14 match lobby (D-092): talks JSON over
    /// HTTPS to the Supabase Edge Functions that mediate matchmaking (see the
    /// task's HTTP contract; base URL is the functions URL, e.g.
    /// https://&lt;project&gt;.supabase.co/functions/v1). Every request is a POST
    /// with the Supabase headers "apikey" and "Authorization: Bearer &lt;anonKey&gt;".
    /// Pure BCL + Newtonsoft.Json — no UnityEngine, no UnityWebRequest — so the
    /// same sources compile into the headless test lane.
    /// </summary>
    public sealed class LobbyClient : IDisposable
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private const string CreateMatchPath = "/create-match";
        private const string JoinMatchPath = "/join-match";
        private const string MatchStatusPath = "/match-status";
        private const string SetReadyPath = "/set-ready";
        private const string LeaveMatchPath = "/leave-match";

        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _anonKey;

        /// <summary>
        /// <paramref name="handler"/> is injectable for tests; an injected handler
        /// stays owned by the caller and is not disposed here.
        /// </summary>
        /// <exception cref="ArgumentException">Invalid base URL (https required, http only on loopback) or empty anon key.</exception>
        public LobbyClient(string baseUrl, string anonKey, HttpMessageHandler handler = null)
        {
            if (string.IsNullOrWhiteSpace(anonKey))
            {
                throw new ArgumentException("Anon key must not be empty.", nameof(anonKey));
            }
            if (string.IsNullOrWhiteSpace(baseUrl) ||
                !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new ArgumentException("Base URL must be an absolute http(s) URL.", nameof(baseUrl));
            }
            if (uri.Scheme != Uri.UriSchemeHttps && !IsLoopbackHost(uri.Host))
            {
                throw new ArgumentException("Base URL must use https; plain http is only allowed on loopback (tests).", nameof(baseUrl));
            }

            _baseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
            _anonKey = anonKey;
            _http = handler != null ? new HttpClient(handler, disposeHandler: false) : new HttpClient();
            _http.Timeout = RequestTimeout;
        }

        /// <summary>Creates a match; returns the code plus the relay endpoint and slot 0.</summary>
        public Task<LobbyResult<CreateMatchResponse>> CreateMatchAsync(string buildCommit, int faction, CancellationToken cancellationToken = default)
        {
            var request = new CreateMatchRequest { BuildCommit = buildCommit, Faction = faction };
            return PostAsync<CreateMatchRequest, CreateMatchResponse>(CreateMatchPath, request, cancellationToken);
        }

        /// <summary>Joins an open match by code (normalized before sending); returns the relay endpoint, slot 1 and opponent info.</summary>
        public Task<LobbyResult<JoinMatchResponse>> JoinMatchAsync(string code, string buildCommit, int faction, CancellationToken cancellationToken = default)
        {
            var request = new JoinMatchRequest { Code = NormalizeCode(code), BuildCommit = buildCommit, Faction = faction };
            return PostAsync<JoinMatchRequest, JoinMatchResponse>(JoinMatchPath, request, cancellationToken);
        }

        /// <summary>Polls the lobby state of a match.</summary>
        public Task<LobbyResult<MatchStatusResponse>> GetStatusAsync(string code, int slot, CancellationToken cancellationToken = default)
        {
            var request = new MatchStatusRequest { Code = NormalizeCode(code), Slot = slot };
            return PostAsync<MatchStatusRequest, MatchStatusResponse>(MatchStatusPath, request, cancellationToken);
        }

        /// <summary>Sets the ready flag of one slot.</summary>
        public Task<LobbyResult<SetReadyResponse>> SetReadyAsync(string code, int slot, bool ready, CancellationToken cancellationToken = default)
        {
            var request = new SetReadyRequest { Code = NormalizeCode(code), Slot = slot, Ready = ready };
            return PostAsync<SetReadyRequest, SetReadyResponse>(SetReadyPath, request, cancellationToken);
        }

        /// <summary>Leaves the match.</summary>
        public Task<LobbyResult<LeaveMatchResponse>> LeaveMatchAsync(string code, int slot, CancellationToken cancellationToken = default)
        {
            var request = new LeaveMatchRequest { Code = NormalizeCode(code), Slot = slot };
            return PostAsync<LeaveMatchRequest, LeaveMatchResponse>(LeaveMatchPath, request, cancellationToken);
        }

        private async Task<LobbyResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest requestBody, CancellationToken cancellationToken)
            where TResponse : class
        {
            string json = JsonConvert.SerializeObject(requestBody);
            using (var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + path))
            {
                request.Headers.TryAddWithoutValidation("apikey", _anonKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // Caller cancellation stays cancellation.
                }
                catch (TaskCanceledException)
                {
                    return LobbyResult<TResponse>.Failure(LobbyErrorKind.NetworkError); // HttpClient timeout.
                }
                catch (HttpRequestException)
                {
                    return LobbyResult<TResponse>.Failure(LobbyErrorKind.NetworkError);
                }

                using (response)
                {
                    int status = (int)response.StatusCode;
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (status == 200)
                    {
                        TResponse value = Deserialize<TResponse>(body);
                        return value != null
                            ? LobbyResult<TResponse>.Success(value)
                            : LobbyResult<TResponse>.Failure(LobbyErrorKind.MalformedResponse);
                    }

                    switch (status)
                    {
                        case 404:
                            return LobbyResult<TResponse>.Failure(LobbyErrorKind.UnknownCode, status);
                        case 410:
                            return LobbyResult<TResponse>.Failure(LobbyErrorKind.Expired, status);
                        case 409:
                            return MapConflict<TResponse>(body, status);
                        default:
                            return LobbyResult<TResponse>.Failure(LobbyErrorKind.HttpError, status);
                    }
                }
            }
        }

        private static LobbyResult<TResponse> MapConflict<TResponse>(string body, int status) where TResponse : class
        {
            LobbyErrorBody error = Deserialize<LobbyErrorBody>(body);
            switch (error?.Error)
            {
                case "relay_busy":
                    return LobbyResult<TResponse>.Failure(LobbyErrorKind.RelayBusy, status);
                case "build_mismatch":
                    return LobbyResult<TResponse>.Failure(LobbyErrorKind.BuildMismatch, status, error.CreatorBuild, error.YourBuild);
                case "match_full":
                    return LobbyResult<TResponse>.Failure(LobbyErrorKind.MatchFull, status);
                default:
                    return LobbyResult<TResponse>.Failure(LobbyErrorKind.HttpError, status);
            }
        }

        /// <summary>Defensive JSON: broken payloads and missing required fields (Required.Always) both come back null.</summary>
        private static T Deserialize<T>(string body) where T : class
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(body);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string NormalizeCode(string code)
        {
            if (!LobbyCode.TryNormalize(code, out string normalized))
            {
                throw new ArgumentException("Invalid match code; validate user input with LobbyCode.TryNormalize first.", nameof(code));
            }
            return normalized;
        }

        private static bool IsLoopbackHost(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            // Uri.Host keeps the brackets of an IPv6 literal.
            string bare = host.TrimStart('[').TrimEnd(']');
            return IPAddress.TryParse(bare, out IPAddress address) && IPAddress.IsLoopback(address);
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
