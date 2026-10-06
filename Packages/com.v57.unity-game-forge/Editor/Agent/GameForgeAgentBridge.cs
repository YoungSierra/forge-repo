using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Talks to the local Node sidecar (127.0.0.1) with a session token.
    /// Token is kept in <see cref="SessionState"/> so Play Mode / domain reload
    /// reconnects instead of killing a busy agent.
    /// </summary>
    public sealed class GameForgeAgentBridge : IDisposable
    {
        public const string DefaultBaseUrl = "http://127.0.0.1:3857";
        private const string TokenStateKey = "V57.GameForge.AgentToken";
        private const string SessionIdStateKey = "V57.GameForge.ActiveSessionId";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(15) };
        /// <summary>Short-timeout client for cancel/health — must not block Stop behind a live SSE read.</summary>
        private static readonly HttpClient ControlHttp = new() { Timeout = TimeSpan.FromSeconds(8) };

        private Process _process;
        private CancellationTokenSource _runCts;
        public string BaseUrl { get; private set; } = DefaultBaseUrl;
        public string SessionToken { get; private set; } = "";
        public string ActiveSessionId
        {
            get => _activeSessionId;
            set
            {
                _activeSessionId = value ?? "";
                SessionState.SetString(SessionIdStateKey, _activeSessionId);
            }
        }
        private string _activeSessionId = "";

        public bool IsRunning => _process != null && !_process.HasExited;
        /// <summary>True while POST kickoff reads the first session event (poll watcher owns the rest).</summary>
        public bool IsKickoffBusy => _kickoffCts != null && !_kickoffCts.IsCancellationRequested;
        public bool IsBusy => IsKickoffBusy || GameForgeRunWatcher.IsWatching;

        private CancellationTokenSource _kickoffCts;

        public async Task EnsureStartedAsync()
        {
            RestorePersistedAuth();

            // Sidecar is a separate Node process — it survives Play Mode / domain reload.
            // Never kill it just because this C# wrapper lost its Process handle.
            if (await HealthOkAsync())
            {
                if (!string.IsNullOrEmpty(SessionToken) && await AuthorizedAsync())
                    return;

                // Alive but our token does not match (stale Editor state) — restart.
                KillListenersOnPort(3857);
            }
            else if (IsRunning)
            {
                Stop();
                KillListenersOnPort(3857);
            }
            else
            {
                KillListenersOnPort(3857);
            }

            StartProcess();
            for (var i = 0; i < 40; i++)
            {
                await Task.Delay(250);
                if (await HealthOkAsync() && await AuthorizedAsync()) return;
            }

            throw new InvalidOperationException("GameForge agent sidecar did not become healthy.");
        }

        private void RestorePersistedAuth()
        {
            if (string.IsNullOrEmpty(SessionToken))
                SessionToken = SessionState.GetString(TokenStateKey, "") ?? "";
            if (string.IsNullOrEmpty(_activeSessionId))
                _activeSessionId = SessionState.GetString(SessionIdStateKey, "") ?? "";
        }

        private void PersistAuth()
        {
            SessionState.SetString(TokenStateKey, SessionToken ?? "");
            SessionState.SetString(SessionIdStateKey, _activeSessionId ?? "");
        }

        /// <summary>Busy session ids reported by the live sidecar (survives Editor reload).</summary>
        public async Task<string[]> GetBusySessionIdsAsync()
        {
            try
            {
                RestorePersistedAuth();
                if (!await HealthOkAsync()) return Array.Empty<string>();
                using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/api/health");
                if (!string.IsNullOrEmpty(SessionToken))
                    req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                using var res = await ControlHttp.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();
                return ParseBusySessionIds(body);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static string[] ParseBusySessionIds(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
            var key = "\"busySessions\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return Array.Empty<string>();
            var lb = json.IndexOf('[', i + key.Length);
            var rb = lb >= 0 ? json.IndexOf(']', lb) : -1;
            if (lb < 0 || rb < 0) return Array.Empty<string>();
            var inner = json.Substring(lb + 1, rb - lb - 1);
            if (string.IsNullOrWhiteSpace(inner)) return Array.Empty<string>();
            var parts = inner.Split(',');
            var list = new System.Collections.Generic.List<string>();
            foreach (var p in parts)
            {
                var s = p.Trim().Trim('"');
                if (s.Length > 0) list.Add(s);
            }
            return list.ToArray();
        }

        private async Task<bool> AuthorizedAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/api/agent/providers");
                if (!string.IsNullOrEmpty(SessionToken))
                    req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                using var res = await Http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static void KillListenersOnPort(int port)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c for /f \"tokens=5\" %a in ('netstat -ano ^| findstr :{port} ^| findstr LISTENING') do taskkill /F /PID %a",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(5000);
            }
            catch
            {
                /* best-effort */
            }
        }

        public async Task<bool> HealthOkAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/api/health");
                if (!string.IsNullOrEmpty(SessionToken))
                    req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                using var res = await ControlHttp.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Immediately abort kickoff or legacy SSE read (does not wait on the sidecar).</summary>
        public void CancelLocalRun()
        {
            try { _kickoffCts?.Cancel(); } catch { /* ignore */ }
            try { _runCts?.Cancel(); } catch { /* ignore */ }
        }

        public async Task<string> GetAsync(string path)
        {
            await EnsureStartedAsync();
            using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + path);
            req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
            using var res = await Http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Non-blocking "What's happening?" snapshot. Does not cancel or share the LLM turn.
        /// Pass null/empty to resolve the active busy session on the sidecar.
        /// </summary>
        public async Task<string> GetSessionActivityAsync(string sessionId = null)
        {
            var id = (sessionId ?? "").Trim();
            if (string.IsNullOrEmpty(id)) id = (ActiveSessionId ?? "").Trim();

            // Prefer the resolver endpoint — works even with a stale/wrong session id.
            try
            {
                var q = string.IsNullOrEmpty(id)
                    ? "/api/sessions/activity"
                    : "/api/sessions/activity?id=" + Uri.EscapeDataString(id);
                return await GetAsync(q);
            }
            catch (HttpRequestException)
            {
                /* fall through */
            }
            catch (Exception ex) when (ex.Message.IndexOf("404", StringComparison.Ordinal) >= 0)
            {
                /* fall through — old sidecar or missing session */
            }

            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    return await GetAsync($"/api/sessions/{Uri.EscapeDataString(id)}/activity");
                }
                catch (Exception ex) when (ex.Message.IndexOf("404", StringComparison.Ordinal) >= 0)
                {
                    throw new InvalidOperationException(
                        "Activity endpoint missing or session gone. Restart the agent (Workbench → Bootstrap Agent) and try again.",
                        ex);
                }
            }

            throw new InvalidOperationException(
                "No active session for activity. Start a run, or restart the agent (Workbench → Bootstrap Agent).");
        }

        /// <summary>
        /// Incremental event poll — short HTTP, survives domain reload. Prefer over SSE.
        /// </summary>
        public async Task<GameForgePollEventsResult> PollSessionEventsAsync(string sessionId, int after)
        {
            await EnsureStartedAsync();
            var path = $"/api/sessions/{Uri.EscapeDataString(sessionId)}/events/poll?after={Math.Max(0, after)}";
            var json = await GetAsync(path);
            return ParsePollEvents(json);
        }

        private static GameForgePollEventsResult ParsePollEvents(string json)
        {
            var result = new GameForgePollEventsResult();
            if (string.IsNullOrEmpty(json)) return result;
            result.SessionId = JsonStringField(json, "sessionId") ?? "";
            result.ForgeMode = JsonStringField(json, "forgeMode") ?? "";
            result.Busy = json.IndexOf("\"busy\":true", StringComparison.OrdinalIgnoreCase) >= 0;
            result.NextAfter = JsonIntField(json, "nextAfter", 0);
            result.EventLines = ParseJsonObjectArray(json, "events");
            return result;
        }

        private static int JsonIntField(string json, string field, int fallback)
        {
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return fallback;
            var colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return fallback;
            var p = colon + 1;
            while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
            var start = p;
            while (p < json.Length && (char.IsDigit(json[p]) || json[p] == '-')) p++;
            if (p == start) return fallback;
            return int.TryParse(json.Substring(start, p - start), out var n) ? n : fallback;
        }

        private static List<string> ParseJsonObjectArray(string json, string field)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return list;
            var lb = json.IndexOf('[', i + key.Length);
            if (lb < 0) return list;
            var depth = 0;
            var end = -1;
            for (var p = lb; p < json.Length; p++)
            {
                var c = json[p];
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) { end = p; break; }
                }
            }
            if (end < 0) return list;
            var inner = json.Substring(lb + 1, end - lb - 1);
            var objDepth = 0;
            var start = -1;
            for (var p = 0; p < inner.Length; p++)
            {
                var c = inner[p];
                if (c == '{')
                {
                    if (objDepth == 0) start = p;
                    objDepth++;
                }
                else if (c == '}')
                {
                    objDepth--;
                    if (objDepth == 0 && start >= 0)
                    {
                        list.Add(inner.Substring(start, p - start + 1));
                        start = -1;
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Start a sidecar run via POST SSE, grab session id, then detach.
        /// The run continues in Node; <see cref="GameForgeRunWatcher"/> polls events.
        /// </summary>
        public async Task<string> KickOffPostSseAsync(string path, string json, CancellationToken ct)
        {
            await EnsureStartedAsync();
            BeginKickoff(ct);
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path)
                {
                    Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json")
                };
                req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                req.Headers.TryAddWithoutValidation("Accept", "text/event-stream");

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _kickoffCts.Token);
                using var res = await Http.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    linked.Token);

                if (!res.IsSuccessStatusCode)
                {
                    var errBody = await res.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"{(int)res.StatusCode} {path}: {errBody}");
                }

                var sessionId = "";
                using (var stream = await res.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (!linked.Token.IsCancellationRequested)
                    {
                        string line;
                        try { line = await reader.ReadLineAsync(); }
                        catch { break; }
                        if (line == null) break;
                        if (line.Length == 0) continue;
                        if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                            line = line.Substring(5).Trim();
                        if (line.Length == 0 || line == "[DONE]") continue;

                        var type = JsonStringField(line, "type");
                        var id = JsonStringField(line, "id");
                        if (type == "session" && !string.IsNullOrEmpty(id))
                        {
                            sessionId = id;
                            ActiveSessionId = id;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(sessionId))
                    sessionId = ActiveSessionId ?? "";

                if (!string.IsNullOrEmpty(sessionId))
                {
                    if (!GameForgeRunWatcher.IsWatching)
                        GameForgeRunWatcher.StartWatching(sessionId, fromIndex: 0);
                }
                else
                    throw new InvalidOperationException("Sidecar did not return a session id.");

                return sessionId;
            }
            finally
            {
                EndKickoff();
            }
        }

        /// <summary>Kick off GET SSE (continue / legacy) then poll.</summary>
        public async Task<string> KickOffGetSseAsync(string path, CancellationToken ct)
        {
            await EnsureStartedAsync();
            BeginKickoff(ct);
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + path);
                req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                req.Headers.TryAddWithoutValidation("Accept", "text/event-stream");

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _kickoffCts.Token);
                using var res = await Http.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    linked.Token);

                if (!res.IsSuccessStatusCode)
                {
                    var errBody = await res.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"{(int)res.StatusCode} {path}: {errBody}");
                }

                var sessionId = ActiveSessionId ?? "";
                using (var stream = await res.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (!linked.Token.IsCancellationRequested)
                    {
                        string line;
                        try { line = await reader.ReadLineAsync(); }
                        catch { break; }
                        if (line == null) break;
                        if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                            line = line.Substring(5).Trim();
                        if (line.Length == 0) continue;
                        var type = JsonStringField(line, "type");
                        var id = JsonStringField(line, "id");
                        if (type == "session" && !string.IsNullOrEmpty(id))
                        {
                            sessionId = id;
                            ActiveSessionId = id;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(sessionId))
                    throw new InvalidOperationException("No session id for poll watch.");

                if (!GameForgeRunWatcher.IsWatching)
                    GameForgeRunWatcher.StartWatching(sessionId, fromIndex: 0);
                return sessionId;
            }
            finally
            {
                EndKickoff();
            }
        }

        private void BeginKickoff(CancellationToken external)
        {
            try { _kickoffCts?.Cancel(); } catch { /* ignore */ }
            _kickoffCts?.Dispose();
            _kickoffCts = CancellationTokenSource.CreateLinkedTokenSource(external);
            GameForgeSession.NotifyBusyChanged();
        }

        private void EndKickoff()
        {
            _kickoffCts?.Dispose();
            _kickoffCts = null;
            GameForgeSession.NotifyBusyChanged();
        }

        public async Task<string> PostJsonAsync(string path, string json)
        {
            await EnsureStartedAsync();
            using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path)
            {
                Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
            using var res = await Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"{(int)res.StatusCode} {path}: {body}");
            return body;
        }

        /// <summary>Upload a compressed image as multipart field <c>file</c>.</summary>
        public async Task<string> PostMultipartFileAsync(string path, string fieldName, string fileName, byte[] bytes, string contentType)
        {
            await EnsureStartedAsync();
            if (bytes == null || bytes.Length == 0)
                throw new InvalidOperationException("Empty attachment payload");
            using var content = new MultipartFormDataContent();
            var part = new ByteArrayContent(bytes);
            var mime = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType.Trim();
            try
            {
                part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
            }
            catch
            {
                part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            }
            content.Add(part, string.IsNullOrEmpty(fieldName) ? "file" : fieldName,
                string.IsNullOrEmpty(fileName) ? "capture.jpg" : fileName);
            using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path) { Content = content };
            if (!string.IsNullOrEmpty(SessionToken))
                req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
            using var res = await Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"{(int)res.StatusCode} {path}: {body}");
            return body;
        }

        public async Task<string> DeleteAsync(string path)
        {
            await EnsureStartedAsync();
            using var req = new HttpRequestMessage(HttpMethod.Delete, BaseUrl + path);
            req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
            using var res = await Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"{(int)res.StatusCode} {path}: {body}");
            return body;
        }

        /// <summary>
        /// POST or GET that streams SSE <c>data:</c> lines.
        /// </summary>
        public async Task StreamSseAsync(string path, string json, Action<string> onEvent, CancellationToken ct) =>
            await StreamSseAsync(HttpMethod.Post, path, json, onEvent, ct);

        /// <summary>GET SSE (reattach to a live session after Play Mode / domain reload).</summary>
        public async Task StreamSseGetAsync(string path, Action<string> onEvent, CancellationToken ct) =>
            await StreamSseAsync(HttpMethod.Get, path, null, onEvent, ct);

        public async Task StreamSseAsync(
            HttpMethod method,
            string path,
            string json,
            Action<string> onEvent,
            CancellationToken ct)
        {
            await EnsureStartedAsync();
            BeginRun(ct);

            using var req = new HttpRequestMessage(method, BaseUrl + path);
            if (method == HttpMethod.Post)
            {
                req.Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json");
            }
            req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
            req.Headers.TryAddWithoutValidation("Accept", "text/event-stream");

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _runCts.Token);
            var cancelled = false;
            try
            {
                using var res = await Http.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    linked.Token);

                if (!res.IsSuccessStatusCode)
                {
                    var errBody = await res.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"{(int)res.StatusCode} {path}: {errBody}");
                }

                using var stream = await res.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using (linked.Token.Register(() =>
                       {
                           cancelled = true;
                           try { stream.Dispose(); } catch { /* ignore */ }
                       }))
                {
                    while (!linked.Token.IsCancellationRequested)
                    {
                        string line;
                        try
                        {
                            line = await reader.ReadLineAsync();
                        }
                        catch (ObjectDisposedException)
                        {
                            cancelled = true;
                            break;
                        }
                        catch (IOException)
                        {
                            cancelled = true;
                            break;
                        }

                        if (line == null) break;
                        if (line.Length == 0) continue;
                        if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                            line = line.Substring(5).Trim();
                        if (line.Length == 0 || line == "[DONE]") continue;

                        var id = JsonStringField(line, "id");
                        var type = JsonStringField(line, "type");
                        if (type == "session" && !string.IsNullOrEmpty(id))
                            ActiveSessionId = id;

                        onEvent?.Invoke(line);
                    }
                }

                if (linked.Token.IsCancellationRequested)
                    cancelled = true;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                EndRun();
            }

            if (cancelled)
                throw new OperationCanceledException();
        }

        /// <summary>Ask the sidecar to abort busy sessions (local kickoff should already be cancelled).</summary>
        public async Task StopSidecarCancelAsync()
        {
            RestorePersistedAuth();
            await PostCancelPulseAsync();
            ActiveSessionId = "";
            await PostCancelPulseAsync();
        }

        /// <summary>Legacy name — cancels local SSE then notifies sidecar.</summary>
        public async Task StopActiveRunAsync()
        {
            CancelLocalRun();
            await StopSidecarCancelAsync();
        }

        private async Task PostCancelPulseAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/sessions/cancel")
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(SessionToken))
                    req.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                using var res = await ControlHttp.SendAsync(req);
                var text = await res.Content.ReadAsStringAsync();
                Debug.Log($"[GameForge] Stop: {(int)res.StatusCode} {text}");

                if ((int)res.StatusCode != 401) return;

                await EnsureStartedAsync();
                using var req2 = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/sessions/cancel")
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                req2.Headers.TryAddWithoutValidation("X-GameForge-Token", SessionToken);
                using var res2 = await ControlHttp.SendAsync(req2);
                Debug.Log($"[GameForge] Stop retry: {(int)res2.StatusCode} {await res2.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameForge] Stop request failed: " + ex.Message);
            }
        }

        /// <summary>Hard-stop: cancel run + kill sidecar listeners (emergency).</summary>
        public void KillAllRuns()
        {
            try { _runCts?.Cancel(); } catch { /* ignore */ }
            ActiveSessionId = "";
            Restart();
        }

        private void BeginRun(CancellationToken external)
        {
            try { _runCts?.Cancel(); } catch { /* ignore */ }
            _runCts?.Dispose();
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(external);
            GameForgeSession.NotifyBusyChanged();
        }

        private void EndRun()
        {
            _runCts?.Dispose();
            _runCts = null;
            // Do NOT clear ActiveSessionId — sidecar may still be busy after Play/reload disconnect.
            GameForgeSession.NotifyBusyChanged();
        }

        private void StartProcess()
        {
            Stop();
            SessionToken = Guid.NewGuid().ToString("N");
            PersistAuth();
            var agentRoot = GameForgeBootstrap.AgentRoot;
            var entry = Path.Combine(agentRoot, "index.js");
            if (!File.Exists(entry))
                throw new FileNotFoundException("Agent entry missing. Open Workbench and click Bootstrap Agent.", entry);

            if (!GameForgeBootstrap.IsNodeAvailable(out var node))
                throw new InvalidOperationException("Node.js not found on PATH.");

            var projectRoot = GameForgeBootstrap.ProjectRoot;
            var psi = new ProcessStartInfo
            {
                FileName = node,
                Arguments = $"\"{entry}\"",
                WorkingDirectory = agentRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            ApplyEnvFile(psi, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".v57", "gameforge.env"));
            ApplyEnvFile(psi, Path.Combine(projectRoot, ".env.local"));

            psi.Environment["GAMEFORGE_TOKEN"] = SessionToken;
            psi.Environment["GAMEFORGE_HOST"] = "127.0.0.1";
            psi.Environment["GAMEFORGE_PORT"] = "3857";
            psi.Environment["GAMEFORGE_PROJECT_ROOT"] = projectRoot;
            psi.Environment["HOST"] = "127.0.0.1";
            psi.Environment["PORT"] = "3857";

            // Node does not use the Windows root store. AV HTTPS scanning (e.g. Kaspersky)
            // MITMs api2.cursor.sh with a local root — export it so @cursor/sdk TLS works.
            TryApplyExtraCaCerts(psi, agentRoot);

            _process = Process.Start(psi);
            if (_process == null)
                throw new InvalidOperationException("Failed to start agent process.");

            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) Debug.Log($"[GameForge Agent] {Redact(e.Data)}");
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) Debug.LogWarning($"[GameForge Agent] {Redact(e.Data)}");
            };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Stop()
        {
            if (_process == null) return;
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit(3000);
                }
            }
            catch
            {
                /* ignore */
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        /// <summary>Kill the sidecar so the next API call reloads updated agent JS.</summary>
        public void Restart()
        {
            try { _runCts?.Cancel(); } catch { /* ignore */ }
            _runCts?.Dispose();
            _runCts = null;
            ActiveSessionId = "";
            SessionToken = "";
            PersistAuth();
            Stop();
            KillListenersOnPort(3857);
            GameForgeSession.NotifyBusyChanged();
        }

        public void Dispose()
        {
            try { _runCts?.Cancel(); } catch { /* ignore */ }
            _runCts?.Dispose();
            Stop();
        }

        private static void ApplyEnvFile(ProcessStartInfo psi, string path)
        {
            if (!File.Exists(path)) return;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim().Trim('"');
                if (key.Length == 0) continue;
                // Empty placeholders must not wipe a key already set by ~/.v57/gameforge.env.
                if (val.Length == 0) continue;
                psi.Environment[key] = val;
            }
        }

        /// <summary>
        /// Export common AV/SSL-inspection roots from the Windows store into a PEM bundle and
        /// point Node at it via NODE_EXTRA_CA_CERTS (read once at process start).
        /// Skips if the env already sets NODE_EXTRA_CA_CERTS.
        /// </summary>
        private static void TryApplyExtraCaCerts(ProcessStartInfo psi, string agentRoot)
        {
            try
            {
                if (psi.Environment.ContainsKey("NODE_EXTRA_CA_CERTS")
                    && !string.IsNullOrWhiteSpace(psi.Environment["NODE_EXTRA_CA_CERTS"]))
                    return;

                var pem = ExportSslInspectionRootsPem(agentRoot);
                // Prefer freshly exported bundle; otherwise reuse an existing .certs PEM.
                if (string.IsNullOrEmpty(pem))
                {
                    var existing = Path.Combine(agentRoot, ".certs", "extra-ca.pem");
                    if (File.Exists(existing) && new FileInfo(existing).Length > 0)
                        pem = existing;
                }
                if (string.IsNullOrEmpty(pem)) return;
                psi.Environment["NODE_EXTRA_CA_CERTS"] = pem;
                Debug.Log($"[GameForge] NODE_EXTRA_CA_CERTS → {pem} (AV/SSL inspection roots for Cursor API)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] Could not export extra CA certs: " + ex.Message);
            }
        }

        private static readonly string[] SslInspectionNameHints =
        {
            "Kaspersky", "Zscaler", "Fortinet", "Netskope", "Sophos", "ESET",
            "Avast", "AVG", "Bitdefender", "Blue Coat", "Forcepoint", "McAfee Web",
            "Symantec Web", "CrowdStrike", "Palo Alto", "Check Point"
        };

        /// <summary>Writes Tools/gameforge-agent/.certs/extra-ca.pem when inspection roots exist.</summary>
        public static string ExportSslInspectionRootsPem(string agentRoot)
        {
            if (string.IsNullOrEmpty(agentRoot)) return null;
            var dir = Path.Combine(agentRoot, ".certs");
            Directory.CreateDirectory(dir);
            var pemPath = Path.Combine(dir, "extra-ca.pem");
            var sb = new StringBuilder();
            var exported = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (StoreLocation loc in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {
                try
                {
                    using var store = new X509Store(StoreName.Root, loc);
                    store.Open(OpenFlags.ReadOnly);
                    foreach (X509Certificate2 cert in store.Certificates)
                    {
                        var subject = cert.Subject ?? "";
                        var issuer = cert.Issuer ?? "";
                        var hit = false;
                        foreach (var hint in SslInspectionNameHints)
                        {
                            if (subject.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0
                                || issuer.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                hit = true;
                                break;
                            }
                        }
                        if (!hit) continue;

                        var thumb = cert.Thumbprint ?? "";
                        if (!string.IsNullOrEmpty(thumb) && !seen.Add(thumb)) continue;

                        byte[] raw;
                        try { raw = cert.Export(X509ContentType.Cert); }
                        catch { continue; }
                        sb.AppendLine("-----BEGIN CERTIFICATE-----");
                        sb.AppendLine(Convert.ToBase64String(raw, Base64FormattingOptions.InsertLineBreaks));
                        sb.AppendLine("-----END CERTIFICATE-----");
                        exported++;
                    }
                }
                catch
                {
                    /* store unavailable */
                }
            }

            if (exported == 0)
            {
                // Keep a previously exported bundle (e.g. from an earlier Bootstrap).
                if (File.Exists(pemPath) && new FileInfo(pemPath).Length > 0)
                    return pemPath;
                return null;
            }
            File.WriteAllText(pemPath, sb.ToString(), Encoding.ASCII);
            return pemPath;
        }

        private static string Redact(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return System.Text.RegularExpressions.Regex.Replace(
                s,
                @"(api[_-]?key|token|authorization|CURSOR_API_KEY|OPENAI_API_KEY)\s*[:=]\s*\S+",
                "$1=••••",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static string JsonStringField(string json, string field)
        {
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return null;
            var colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return null;
            var q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return null;
            var sb = new StringBuilder();
            for (var p = q1 + 1; p < json.Length; p++)
            {
                var c = json[p];
                if (c == '\\' && p + 1 < json.Length)
                {
                    var n = json[++p];
                    sb.Append(n switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '"' => '"',
                        '\\' => '\\',
                        _ => n
                    });
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
