using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Polls the Node sidecar for session events on <see cref="EditorApplication.update"/>.
    /// Survives domain reload / Play Mode — no long-lived SSE in the Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class GameForgeRunWatcher
    {
        private const string SessionKey = "V57.GameForge.WatchSessionId";
        private const string IndexKey = "V57.GameForge.WatchEventIndex";

        private static string _sessionId = "";
        private static int _eventIndex;
        private static double _nextPollAt;
        private static bool _pollInFlight;
        private static bool _terminalSeen;
        private static int _idleClearPolls;

        public static event Action<string> OnEvent;

        public static bool IsWatching { get; private set; }

        private static bool _stateLoaded;

        static GameForgeRunWatcher()
        {
            EditorApplication.update += Tick;
            EditorApplication.delayCall += WireChatEvents;
        }

        private static void WireChatEvents()
        {
            OnEvent -= GameForgeChatWindow.HandleStreamEvent;
            OnEvent += GameForgeChatWindow.HandleStreamEvent;
        }

        /// <summary>Restore watch state from SessionState on first safe editor tick.</summary>
        private static void LoadPersistedStateIfNeeded()
        {
            if (_stateLoaded) return;
            try
            {
                _sessionId = SessionState.GetString(SessionKey, "") ?? "";
                _eventIndex = SessionState.GetInt(IndexKey, 0);
                IsWatching = !string.IsNullOrEmpty(_sessionId);
                _stateLoaded = true;
            }
            catch
            {
                // Too early (e.g. EditorWindow ctor) — retry on a later update tick.
            }
        }

        public static void StartWatching(string sessionId, int fromIndex = -1)
        {
            sessionId = (sessionId ?? "").Trim();
            if (string.IsNullOrEmpty(sessionId)) return;

            if (fromIndex >= 0 || sessionId != _sessionId)
                _eventIndex = Math.Max(0, fromIndex);

            _sessionId = sessionId;
            _terminalSeen = false;
            _idleClearPolls = 0;
            IsWatching = true;
            _stateLoaded = true;
            _nextPollAt = 0;
            Persist();
            GameForgeSession.Bridge.ActiveSessionId = sessionId;
            GameForgeSession.NotifyBusyChanged();
        }

        public static void StopWatching()
        {
            if (!IsWatching && string.IsNullOrEmpty(_sessionId)) return;
            IsWatching = false;
            _sessionId = "";
            _eventIndex = 0;
            _terminalSeen = false;
            _idleClearPolls = 0;
            try
            {
                SessionState.SetString(SessionKey, "");
                SessionState.SetInt(IndexKey, 0);
            }
            catch
            {
                /* ignore */
            }
            GameForgeSession.NotifyBusyChanged();
        }

        private static void Persist()
        {
            if (!_stateLoaded) return;
            try
            {
                SessionState.SetString(SessionKey, _sessionId ?? "");
                SessionState.SetInt(IndexKey, _eventIndex);
            }
            catch
            {
                /* ignore — editor not ready */
            }
        }

        private static void Tick()
        {
            LoadPersistedStateIfNeeded();
            if (!IsWatching || string.IsNullOrEmpty(_sessionId)) return;
            if (EditorApplication.timeSinceStartup < _nextPollAt) return;
            _nextPollAt = EditorApplication.timeSinceStartup + 0.45;
            if (_pollInFlight) return;
            _ = PollOnceAsync();
        }

        private static async Task PollOnceAsync()
        {
            if (!IsWatching || string.IsNullOrEmpty(_sessionId)) return;
            _pollInFlight = true;
            try
            {
                var result = await GameForgeSession.Bridge.PollSessionEventsAsync(_sessionId, _eventIndex);
                if (result == null) return;

                if (!string.IsNullOrEmpty(result.SessionId) && result.SessionId != _sessionId)
                {
                    _sessionId = result.SessionId;
                    GameForgeSession.Bridge.ActiveSessionId = result.SessionId;
                }

                foreach (var line in result.EventLines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    var type = JsonStringField(line, "type");
                    if (type is "done" or "cancelled" or "error")
                        _terminalSeen = true;
                    try { OnEvent?.Invoke(line); }
                    catch (Exception ex) { Debug.LogWarning("[GameForge] RunWatcher event: " + ex.Message); }
                }

                if (result.NextAfter > _eventIndex)
                {
                    _eventIndex = result.NextAfter;
                    Persist();
                }

                if (!string.IsNullOrEmpty(result.ForgeMode))
                    GameForgeSession.ApplyRemoteForgeMode(result.ForgeMode);

                // Provider "done" may be suppressed until the HTTP handler finishes soft-advice.
                // If the session is already idle without a terminal event (or the event was lost),
                // synthesize done so Chat FinalizeThinking and Stop no longer block Continue.
                if (result.Busy)
                {
                    _idleClearPolls = 0;
                }
                else
                {
                    _idleClearPolls++;
                    if (_terminalSeen || _idleClearPolls >= 2)
                    {
                        if (!_terminalSeen)
                            EmitSyntheticDone();
                        StopWatching();
                        _ = GameForgeSession.RefreshRemoteBusyAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("404", StringComparison.Ordinal) >= 0)
                {
                    if (!_terminalSeen)
                        EmitSyntheticDone();
                    StopWatching();
                    _ = GameForgeSession.RefreshRemoteBusyAsync();
                }
            }
            finally
            {
                _pollInFlight = false;
            }
        }

        private static void EmitSyntheticDone()
        {
            _terminalSeen = true;
            try
            {
                OnEvent?.Invoke("{\"type\":\"done\",\"status\":\"finished\"}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] RunWatcher synthetic done: " + ex.Message);
            }
        }

        /// <summary>After domain reload: resume poll if sidecar still busy.</summary>
        public static async Task ResumeIfNeededAsync()
        {
            LoadPersistedStateIfNeeded();

            // User Stop must win over persisted watch state (reload often lands mid-cancel).
            if (GameForgeSession.ReattachSuppressed)
            {
                StopWatching();
                return;
            }

            await GameForgeSession.RefreshRemoteBusyAsync();
            if (GameForgeSession.ReattachSuppressed)
            {
                StopWatching();
                return;
            }

            var busy = GameForgeSession.RemoteBusySessionIds;
            if (busy == null || busy.Length == 0)
            {
                StopWatching();
                return;
            }

            if (IsWatching && !string.IsNullOrEmpty(_sessionId))
            {
                // Only keep the watch if that session is still busy remotely.
                if (Array.IndexOf(busy, _sessionId) < 0)
                {
                    StopWatching();
                    return;
                }
                _nextPollAt = 0;
                return;
            }

            var known = GameForgeSession.Bridge.ActiveSessionId ?? "";
            var target = Array.IndexOf(busy, known) >= 0 ? known : busy[0];
            if (string.IsNullOrEmpty(target)) return;

            var resumeIndex = SessionState.GetInt(IndexKey, 0);
            if (SessionState.GetString(SessionKey, "") == target)
                resumeIndex = Math.Max(0, resumeIndex);
            else
                resumeIndex = 0;

            StartWatching(target, resumeIndex);
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
            var sb = new System.Text.StringBuilder();
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

    /// <summary>Parsed <c>/events/poll</c> payload.</summary>
    public sealed class GameForgePollEventsResult
    {
        public string SessionId;
        public string ForgeMode;
        public bool Busy;
        public int NextAfter;
        public List<string> EventLines = new();
    }
}
