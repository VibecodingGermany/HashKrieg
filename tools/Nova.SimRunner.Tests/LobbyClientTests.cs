using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Nova.Networking.Lobby;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Lobby client unit tests (.NET lane): the Supabase Edge Functions of the
    /// sprint-14 lobby (D-092) are mocked by an in-process HttpListener on
    /// loopback with an ephemeral port — no test touches an external network.
    /// Covers the request/response contract of all five endpoints (paths,
    /// headers, bodies), the HTTP-status → ErrorKind mapping incl. German
    /// plain-text messages, malformed-response and network-error hardening,
    /// and the LobbyCode normalization rules.
    /// </summary>
    [TestFixture]
    public sealed class LobbyClientTests
    {
        private const string AnonKey = "anon-test-key";
        private const string Code = "K7F-2Q9";
        private const string CreatorBuild = "aaa1111";
        private const string JoinerBuild = "bbb2222";

        private FakeLobbyServer _server;
        private LobbyClient _client;

        [SetUp]
        public void SetUp()
        {
            _server = new FakeLobbyServer();
            _client = new LobbyClient(_server.BaseUrl, AnonKey);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _server.Dispose();
        }

        // ----- Roundtrips: all five endpoints -----

        [Test]
        public async Task CreateMatch_PostsContract_AndParsesResponse()
        {
            _server.Map("/create-match", 200,
                "{\"code\":\"K7F-2Q9\",\"relayHost\":\"relay.example.org\",\"relayPort\":7777,\"slot\":0}");

            LobbyResult<CreateMatchResponse> result = await _client.CreateMatchAsync(CreatorBuild, faction: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.True, result.Message);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.None));
            Assert.That(result.Value.Code, Is.EqualTo(Code));
            Assert.That(result.Value.RelayHost, Is.EqualTo("relay.example.org"));
            Assert.That(result.Value.RelayPort, Is.EqualTo(7777));
            Assert.That(result.Value.Slot, Is.EqualTo(0));

            FakeLobbyServer.RecordedRequest request = _server.LastRequest;
            AssertRequestShell(request, "/create-match");
            JObject body = JObject.Parse(request.Body);
            Assert.That((string)body["buildCommit"], Is.EqualTo(CreatorBuild));
            Assert.That((int)body["faction"], Is.EqualTo(1));
        }

        [Test]
        public async Task JoinMatch_NormalizesCode_AndParsesResponse()
        {
            _server.Map("/join-match", 200,
                "{\"relayHost\":\"relay.example.org\",\"relayPort\":7777,\"slot\":1,\"opponentFaction\":0,\"opponentBuild\":\"" + CreatorBuild + "\"}");

            // Lowercase without dash must normalize to the canonical form on the wire.
            LobbyResult<JoinMatchResponse> result = await _client.JoinMatchAsync("k7f2q9", JoinerBuild, faction: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.True, result.Message);
            Assert.That(result.Value.RelayHost, Is.EqualTo("relay.example.org"));
            Assert.That(result.Value.RelayPort, Is.EqualTo(7777));
            Assert.That(result.Value.Slot, Is.EqualTo(1));
            Assert.That(result.Value.OpponentFaction, Is.EqualTo(0));
            Assert.That(result.Value.OpponentBuild, Is.EqualTo(CreatorBuild));

            FakeLobbyServer.RecordedRequest request = _server.LastRequest;
            AssertRequestShell(request, "/join-match");
            JObject body = JObject.Parse(request.Body);
            Assert.That((string)body["code"], Is.EqualTo(Code));
            Assert.That((string)body["buildCommit"], Is.EqualTo(JoinerBuild));
            Assert.That((int)body["faction"], Is.EqualTo(1));
        }

        [Test]
        public async Task GetStatus_ParsesSlots_AndHasNoTokenUnlessStarting()
        {
            _server.Map("/match-status", 200,
                "{\"state\":\"ready\",\"slots\":[" +
                "{\"faction\":1,\"ready\":true,\"buildCommit\":\"" + CreatorBuild + "\"},null]}");

            LobbyResult<MatchStatusResponse> result = await _client.GetStatusAsync(Code, slot: 0, CancellationToken.None);

            Assert.That(result.Ok, Is.True, result.Message);
            Assert.That(result.Value.State, Is.EqualTo(LobbyMatchState.Ready));
            Assert.That(result.Value.Slots.Length, Is.EqualTo(2));
            Assert.That(result.Value.Slots[0].Faction, Is.EqualTo(1));
            Assert.That(result.Value.Slots[0].Ready, Is.True);
            Assert.That(result.Value.Slots[0].BuildCommit, Is.EqualTo(CreatorBuild));
            Assert.That(result.Value.Slots[1], Is.Null, "an empty slot is a null array entry");
            Assert.That(result.Value.TokenHex, Is.Null, "tokenHex is only set when state == starting");

            FakeLobbyServer.RecordedRequest request = _server.LastRequest;
            AssertRequestShell(request, "/match-status");
            JObject body = JObject.Parse(request.Body);
            Assert.That((string)body["code"], Is.EqualTo(Code));
            Assert.That((int)body["slot"], Is.EqualTo(0));
        }

        [Test]
        public async Task GetStatus_Starting_CarriesTokenHex()
        {
            _server.Map("/match-status", 200,
                "{\"state\":\"starting\",\"tokenHex\":\"0123456789abcdef\",\"slots\":[" +
                "{\"faction\":0,\"ready\":true,\"buildCommit\":\"" + CreatorBuild + "\"}," +
                "{\"faction\":1,\"ready\":true,\"buildCommit\":\"" + JoinerBuild + "\"}]}");

            LobbyResult<MatchStatusResponse> result = await _client.GetStatusAsync(Code, slot: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.True, result.Message);
            Assert.That(result.Value.State, Is.EqualTo(LobbyMatchState.Starting));
            Assert.That(result.Value.TokenHex, Is.EqualTo("0123456789abcdef"));
            Assert.That(result.Value.Slots[1].BuildCommit, Is.EqualTo(JoinerBuild));
        }

        [Test]
        public async Task SetReady_PostsReadyFlag_AndParsesState()
        {
            _server.Map("/set-ready", 200, "{\"state\":\"ready\"}");

            LobbyResult<SetReadyResponse> result = await _client.SetReadyAsync(Code, slot: 0, ready: true, CancellationToken.None);

            Assert.That(result.Ok, Is.True, result.Message);
            Assert.That(result.Value.State, Is.EqualTo(LobbyMatchState.Ready));

            FakeLobbyServer.RecordedRequest request = _server.LastRequest;
            AssertRequestShell(request, "/set-ready");
            JObject body = JObject.Parse(request.Body);
            Assert.That((string)body["code"], Is.EqualTo(Code));
            Assert.That((int)body["slot"], Is.EqualTo(0));
            Assert.That((bool)body["ready"], Is.True);
        }

        [Test]
        public async Task LeaveMatch_PostsContract_AndReturnsOk()
        {
            _server.Map("/leave-match", 200, "{}");

            LobbyResult<LeaveMatchResponse> result = await _client.LeaveMatchAsync(Code, slot: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.True, result.Message);

            FakeLobbyServer.RecordedRequest request = _server.LastRequest;
            AssertRequestShell(request, "/leave-match");
            JObject body = JObject.Parse(request.Body);
            Assert.That((string)body["code"], Is.EqualTo(Code));
            Assert.That((int)body["slot"], Is.EqualTo(1));
        }

        // ----- Status mapping -----

        [Test]
        public async Task JoinMatch_BuildMismatch_ReturnsBothCommits_AndGermanMessage()
        {
            _server.Map("/join-match", 409,
                "{\"error\":\"build_mismatch\",\"creatorBuild\":\"" + CreatorBuild + "\",\"yourBuild\":\"" + JoinerBuild + "\"}");

            LobbyResult<JoinMatchResponse> result = await _client.JoinMatchAsync(Code, JoinerBuild, faction: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.BuildMismatch));
            Assert.That(result.CreatorBuild, Is.EqualTo(CreatorBuild));
            Assert.That(result.YourBuild, Is.EqualTo(JoinerBuild));
            Assert.That(result.Message, Does.Contain("unterschiedliche Versionen"));
        }

        [Test]
        public async Task JoinMatch_UnknownCode_Maps404()
        {
            _server.Map("/join-match", 404, "{\"error\":\"unknown_code\"}");

            LobbyResult<JoinMatchResponse> result = await _client.JoinMatchAsync(Code, JoinerBuild, faction: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.UnknownCode));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task GetStatus_Expired_Maps410()
        {
            _server.Map("/match-status", 410, "{\"error\":\"expired\"}");

            LobbyResult<MatchStatusResponse> result = await _client.GetStatusAsync(Code, slot: 0, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.Expired));
        }

        [Test]
        public async Task CreateMatch_RelayBusy_Maps409()
        {
            _server.Map("/create-match", 409, "{\"error\":\"relay_busy\"}");

            LobbyResult<CreateMatchResponse> result = await _client.CreateMatchAsync(CreatorBuild, faction: 0, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.RelayBusy));
        }

        [Test]
        public async Task JoinMatch_MatchFull_Maps409()
        {
            _server.Map("/join-match", 409, "{\"error\":\"match_full\"}");

            LobbyResult<JoinMatchResponse> result = await _client.JoinMatchAsync(Code, JoinerBuild, faction: 1, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.MatchFull));
        }

        [Test]
        public async Task GetStatus_ServerError_MapsHttpError_WithStatusCode()
        {
            _server.Map("/match-status", 500, "{\"error\":\"internal\"}");

            LobbyResult<MatchStatusResponse> result = await _client.GetStatusAsync(Code, slot: 0, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.HttpError));
            Assert.That(result.HttpStatusCode, Is.EqualTo(500));
        }

        // ----- Hardening -----

        [Test]
        public async Task CreateMatch_BrokenJson_MapsMalformedResponse()
        {
            _server.Map("/create-match", 200, "this is not json");

            LobbyResult<CreateMatchResponse> result = await _client.CreateMatchAsync(CreatorBuild, faction: 0, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.MalformedResponse));
        }

        [Test]
        public async Task CreateMatch_MissingFields_MapsMalformedResponse()
        {
            _server.Map("/create-match", 200, "{\"slot\":0}");

            LobbyResult<CreateMatchResponse> result = await _client.CreateMatchAsync(CreatorBuild, faction: 0, CancellationToken.None);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.MalformedResponse));
        }

        [Test]
        public async Task CreateMatch_ConnectionRefused_MapsNetworkError()
        {
            int deadPort = FakeLobbyServer.FindFreeLoopbackPort(); // bound and immediately released: nothing listens
            using (var client = new LobbyClient($"http://127.0.0.1:{deadPort}", AnonKey))
            {
                LobbyResult<CreateMatchResponse> result = await client.CreateMatchAsync(CreatorBuild, faction: 0, CancellationToken.None);

                Assert.That(result.Ok, Is.False);
                Assert.That(result.ErrorKind, Is.EqualTo(LobbyErrorKind.NetworkError));
            }
        }

        [Test]
        public void Ctor_RequiresHttps_OutsideLoopback()
        {
            Assert.That(() => new LobbyClient("http://example.com/functions/v1", AnonKey), Throws.ArgumentException);
            Assert.That(() => new LobbyClient("not a url", AnonKey), Throws.ArgumentException);
            Assert.That(() => new LobbyClient("https://example.com/functions/v1", ""), Throws.ArgumentException);

            using (var loopback = new LobbyClient("http://127.0.0.1:9", AnonKey))
            using (var https = new LobbyClient("https://example.com/functions/v1", AnonKey))
            {
                // Constructing both is enough: loopback-http and https are legal.
            }
        }

        [Test]
        public void JoinMatch_InvalidCode_ThrowsBeforeAnyRequest()
        {
            Assert.That(
                () => _client.JoinMatchAsync("K7F-2Q0", JoinerBuild, faction: 1, CancellationToken.None),
                Throws.ArgumentException);
            Assert.That(_server.Requests, Is.Empty);
        }

        private static void AssertRequestShell(FakeLobbyServer.RecordedRequest request, string expectedPath)
        {
            Assert.That(request, Is.Not.Null, "the mock server must have recorded a request");
            Assert.That(request.Path, Is.EqualTo(expectedPath));
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.ApiKey, Is.EqualTo(AnonKey), "Supabase apikey header");
            Assert.That(request.Authorization, Is.EqualTo("Bearer " + AnonKey), "Supabase Authorization header");
            Assert.That(request.ContentType, Does.StartWith("application/json"));
        }

        /// <summary>
        /// In-process stand-in for the Supabase Edge Functions: an HttpListener
        /// on a loopback ephemeral port mapping request paths to (status, json)
        /// responses and recording every incoming request for assertions.
        /// </summary>
        private sealed class FakeLobbyServer : IDisposable
        {
            internal sealed class RecordedRequest
            {
                public string Path;
                public string Method;
                public string Body;
                public string ApiKey;
                public string Authorization;
                public string ContentType;
            }

            private readonly HttpListener _listener;
            private readonly Task _acceptLoop;
            private readonly Dictionary<string, (int Status, string Json)> _routes = new Dictionary<string, (int, string)>();
            private readonly List<RecordedRequest> _requests = new List<RecordedRequest>();
            private readonly object _requestsGate = new object();

            internal FakeLobbyServer()
            {
                int port = FindFreeLoopbackPort();
                BaseUrl = $"http://127.0.0.1:{port}";
                _listener = new HttpListener();
                _listener.Prefixes.Add(BaseUrl + "/");
                _listener.Start();
                _acceptLoop = Task.Run(AcceptLoopAsync);
            }

            internal string BaseUrl { get; }

            internal void Map(string path, int status, string json)
            {
                lock (_requestsGate)
                {
                    _routes[path] = (status, json);
                }
            }

            internal IReadOnlyList<RecordedRequest> Requests
            {
                get
                {
                    lock (_requestsGate)
                    {
                        return _requests.ToArray();
                    }
                }
            }

            internal RecordedRequest LastRequest
            {
                get
                {
                    lock (_requestsGate)
                    {
                        return _requests.Count == 0 ? null : _requests[_requests.Count - 1];
                    }
                }
            }

            /// <summary>Reserves an ephemeral loopback port and releases it again.</summary>
            internal static int FindFreeLoopbackPort()
            {
                var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                int port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                return port;
            }

            private async Task AcceptLoopAsync()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (HttpListenerException) { break; } // stopped in Dispose
                    catch (ObjectDisposedException) { break; }
                    catch (InvalidOperationException) { break; }
                    _ = HandleAsync(context);
                }
            }

            private async Task HandleAsync(HttpListenerContext context)
            {
                try
                {
                    string body;
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }

                    lock (_requestsGate)
                    {
                        _requests.Add(new RecordedRequest
                        {
                            Path = context.Request.Url.AbsolutePath,
                            Method = context.Request.HttpMethod,
                            Body = body,
                            ApiKey = context.Request.Headers["apikey"],
                            Authorization = context.Request.Headers["Authorization"],
                            ContentType = context.Request.ContentType,
                        });
                    }

                    (int Status, string Json) route;
                    lock (_requestsGate)
                    {
                        if (!_routes.TryGetValue(context.Request.Url.AbsolutePath, out route))
                        {
                            route = (404, "{\"error\":\"unknown_code\"}");
                        }
                    }

                    byte[] payload = Encoding.UTF8.GetBytes(route.Json);
                    context.Response.StatusCode = route.Status;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    context.Response.ContentLength64 = payload.Length;
                    await context.Response.OutputStream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
                    context.Response.Close();
                }
                catch (Exception)
                {
                    // A failed mock response fails the client call, which the test sees.
                    try { context.Response.Abort(); } catch (Exception) { /* listener already gone */ }
                }
            }

            public void Dispose()
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener.Close();
                try { _acceptLoop.Wait(TimeSpan.FromSeconds(2)); } catch (Exception) { /* best effort */ }
            }
        }
    }

    /// <summary>LobbyCode normalization rules (D-092 alphabet, "XXX-XXX" display form).</summary>
    [TestFixture]
    public sealed class LobbyCodeTests
    {
        [Test]
        public void TryNormalize_AcceptsLowercase_WithoutDash_WithWhitespace()
        {
            Assert.That(LobbyCode.TryNormalize("k7f2q9", out string normalized), Is.True);
            Assert.That(normalized, Is.EqualTo("K7F-2Q9"));

            Assert.That(LobbyCode.TryNormalize("  k7f-2q9  ", out normalized), Is.True);
            Assert.That(normalized, Is.EqualTo("K7F-2Q9"));

            Assert.That(LobbyCode.TryNormalize("K7F-2Q9", out normalized), Is.True);
            Assert.That(normalized, Is.EqualTo("K7F-2Q9"));
        }

        [Test]
        public void TryNormalize_RejectsLookalikeCharacters()
        {
            Assert.That(LobbyCode.TryNormalize("K7F-2Q0", out _), Is.False, "digit zero");
            Assert.That(LobbyCode.TryNormalize("K7F-2QO", out _), Is.False, "letter O");
            Assert.That(LobbyCode.TryNormalize("1I2345", out _), Is.False, "1 and I");
            Assert.That(LobbyCode.TryNormalize("K7FL2Q", out _), Is.False, "letter L");
        }

        [Test]
        public void TryNormalize_RejectsWrongLengths_AndMisplacedDashes()
        {
            Assert.That(LobbyCode.TryNormalize(null, out _), Is.False);
            Assert.That(LobbyCode.TryNormalize("", out _), Is.False);
            Assert.That(LobbyCode.TryNormalize("K7F-2Q", out _), Is.False, "too short");
            Assert.That(LobbyCode.TryNormalize("K7F-2Q99", out _), Is.False, "too long");
            Assert.That(LobbyCode.TryNormalize("K7F2-Q9", out _), Is.False, "dash not at position 3");
            Assert.That(LobbyCode.TryNormalize("K7F2Q9-", out _), Is.False);
        }

        [Test]
        public void Format_ProducesCanonicalForm_AndRejectsGarbage()
        {
            Assert.That(LobbyCode.Format("k7f2q9"), Is.EqualTo("K7F-2Q9"));
            Assert.That(() => LobbyCode.Format("nope!"), Throws.ArgumentException);
        }

        [Test]
        public void IsValidCode_FollowsTheSameRulesAsTryNormalize()
        {
            Assert.That(LobbyCode.IsValidCode("K7F-2Q9"), Is.True);
            Assert.That(LobbyCode.IsValidCode("k7f2q9"), Is.True);
            Assert.That(LobbyCode.IsValidCode("K7F-2Q0"), Is.False);
            Assert.That(LobbyCode.IsValidCode("K7F-2Q"), Is.False);
        }
    }
}
