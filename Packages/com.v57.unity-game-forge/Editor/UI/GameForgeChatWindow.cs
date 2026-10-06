using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Clean iterate chat: mode + message + model sheet, Left Shift+Tab cycles modes.
    /// </summary>
    public sealed class GameForgeChatWindow : EditorWindow
    {
        private static GameForgeChatWindow _instance;
        private static readonly string[] ModeCycle = { "agent", "ask" };

        private VisualElement _root;
        private VisualElement _sheetHost;
        private VisualElement _contextBlock;
        private VisualElement _contextRow;
        private ScrollView _log;
        private TextField _input;
        private Label _modePill;
        private Label _contextLabel;
        private Label _status;
        private Label _emptyHint;
        private Button _sendBtn;
        private Label _runStatus;
        private VisualElement _thinkingBox;
        private Label _thinkingHead;
        private Label _thinkingBody;
        private readonly System.Text.StringBuilder _thinkingBuf = new();
        private readonly List<string> _writtenFiles = new();
        private int _toolCalls;
        private string _latestAssistantText = "";
        private string _activeOp = "";
        private double _thinkingStartedAt;
        private bool _thinkingTimerActive;
        private IVisualElementScheduledItem _thinkingTick;
        /// <summary>True from BeginThinking until FinalizeThinking — keeps Stop visible if SSE/busy poll desyncs.</summary>
        private bool _runUiActive;
        private GameForgeModelPicker _picker;
        private GameForgeChatModePicker _modePicker;
        private GameForgeChatSlashMenu _slashMenu;
        private GameForgeConfirmSheet _confirmSheet;
        private Button _continueBtn;
        private Button _activityBtn;
        private VisualElement _activityPanel;
        private ScrollView _activityScroll;
        private VisualElement _attachStrip;
        private readonly List<PendingAttachment> _pendingAttachments = new();
        private const int MaxAttachments = 4;
        private const int MaxAttachBytes = 2 * 1024 * 1024;

        private sealed class PendingAttachment
        {
            public string Id;
            public string FileName;
            public Texture2D Thumb;
            public byte[] PreviewBytes;
            public VisualElement Chip;
        }
        private Label _activityBody;
        private IVisualElementScheduledItem _activityPoll;
        private bool _activityOpen;
        private float _activityScrollY;
        private IVisualElementScheduledItem _busyHeartbeat;
        private int _lastHeartbeatEvents = -1;
        private int _heartbeatStalePolls;
        private string _checkpointSessionId = "";
        private int _checkpointTurn;
        private string _checkpointOp = "";
        private const int InputLinePx = 18;
        private const int InputPadPx = 10;
        private const int InputMinPx = 28;
        private const int InputMaxPx = 140;
        private const string ChatLogSessionKey = "V57.GameForge.ChatLog.v1";
        private const int MaxPersistedMessages = 300;

        [Serializable]
        private sealed class PersistedChatEntry
        {
            public string role;
            public string text;
            public string head;
        }

        [Serializable]
        private sealed class PersistedChatLog
        {
            public PersistedChatEntry[] entries;
        }

        private static readonly List<PersistedChatEntry> SharedEntries = new();
        private static bool _persistedLoaded;

        public static bool HasActiveRun
        {
            get
            {
                if (GameForgeSession.IsBusy) return true;
                return _instance != null && (_instance._runUiActive || _instance._thinkingTimerActive);
            }
        }

        public static void NotifyPipelineSettled()
        {
            GameForgePipelineState.ClearSetupLaunch();
            GameForgePipelineState.ClearIdleRunning();
            GameForgePipelineState.NotifyChanged();
        }

        public static void SubmitPipelineContinue() => SubmitPipelineReply("continue");

        public static void SubmitPipelineReply(string message)
        {
            EnsureInstance();
            if (_instance == null || string.IsNullOrWhiteSpace(message)) return;
            _instance._input?.SetValueWithoutNotify(message.Trim());
            _ = _instance.SendAsync();
        }

        /// <summary>Open chat and send a slash skill through the same path as the compose box.</summary>
        public static void SubmitSlash(string command)
        {
            EnsureInstance();
            if (_instance == null) return;
            _instance.ClearStopGuardsForNewRun();
            if (!GameForgeSession.IsBusy && (_instance._runUiActive || _instance._thinkingTimerActive))
                _instance.FinalizeThinking(cancelled: false);
            if (_instance._input != null)
                _instance._input.SetValueWithoutNotify(command ?? "");
            _ = _instance.SendAsync();
        }

        /// <summary>Host the live chat inside the pipeline column instead of a floating window.</summary>
        public static void MountInto(VisualElement host)
        {
            if (host == null) return;
            if (_mountedHost == host && _instance != null && _instance._root == host)
                return;

            CloseStandaloneChats();
            var embed = CreateInstance<GameForgeChatWindow>();
            embed.hideFlags = HideFlags.HideAndDontSave;
            _mountedHost = host;
            _instance = embed;
            embed.BuildInto(host, focusInput: false);
        }

        private static VisualElement _mountedHost;

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            if (_mountedHost != null && _mountedHost.panel != null)
            {
                MountInto(_mountedHost);
                return;
            }
            Open();
        }

        private static void CloseStandaloneChats()
        {
            var found = Resources.FindObjectsOfTypeAll<GameForgeChatWindow>();
            foreach (var w in found)
            {
                if (w == null) continue;
                if (_mountedHost != null && w == _instance) continue;
                try { w.Close(); }
                catch { /* already closing */ }
            }
        }

        private void BuildInto(VisualElement root, bool focusInput)
        {
            _root = root;
            root.Clear();
            BuildUi(root);
            _runStopHandled = LoadRunStopHandled();
            RestorePersistedLog();
            RefreshBusyUi();
            RefreshContext();
            ApplyModeTheme(GameForgeSession.ChatMode);
            _ = RefreshProvidersAsync();
            _ = TryReattachActiveSessionAsync();
            root.schedule.Execute(() =>
            {
                if (this == null) return;
                RecoverOrphanThinkingChrome();
                RefreshBusyUi();
            }).StartingIn(400);
            if (focusInput)
            {
                root.schedule.Execute(() =>
                {
                    root.Focus();
                    _input?.Focus();
                });
            }
        }

        [MenuItem("V57/GameForge/Chat", false, 10)]
        public static void Open()
        {
            _instance = GetWindow<GameForgeChatWindow>();
            _instance.titleContent = new GUIContent("GameForge Chat");
            _instance.minSize = new Vector2(360, 480);
            _instance.Focus();
        }

        public static void AppendExternal(string role, string text)
        {
            if (_instance == null) Open();
            _instance.AppendOnMain(role, text);
        }

        /// <summary>Opens chat and pre-fills the compose field (e.g. console log handoff).</summary>
        public static void PrefillComposeExternal(string text)
        {
            if (_instance == null) Open();
            _instance.PrefillComposeOnMain(text ?? "");
        }

        public static void SetStatusExternal(string text)
        {
            if (_instance == null) Open();
            _instance.SetStatusOnMain(text);
        }

        /// <summary>Progress lives in the Thinking bubble (no duplicate header banner).</summary>
        public static void ShowRunBanner(string title, string detail = null)
        {
            if (_instance == null) Open();
            var line = CombineStatus(title, detail);
            _instance.SetRunStatusOnMain(line);
            if (!string.IsNullOrEmpty(line))
                _instance.AppendThinkingOnMain(line);
        }

        public static void UpdateRunBanner(string detail)
        {
            if (_instance == null) return;
            _instance.SetRunStatusOnMain(detail);
            if (!string.IsNullOrEmpty(detail))
                _instance.AppendThinkingOnMain(detail);
        }

        public static void HideRunBanner()
        {
            if (_instance == null) return;
            _instance.SetRunStatusOnMain(null);
        }

        public static void StartThinking(string title)
        {
            if (_instance == null) Open();
            EditorApplication.delayCall += () =>
            {
                if (_instance == null) return;
                _instance.BeginThinkingRun(title);
            };
        }

        public static void FinishThinking(bool cancelled = false)
        {
            if (_instance == null) return;
            EditorApplication.delayCall += () =>
            {
                if (_instance == null) return;
                _instance.FinalizeThinking(cancelled);
                _instance.SetRunStatusLocal(null);
            };
        }

        private static string CombineStatus(string title, string detail)
        {
            if (string.IsNullOrEmpty(title)) return detail ?? "";
            if (string.IsNullOrEmpty(detail)) return title;
            return $"{title} · {detail}";
        }

        private bool _reattachInFlight;
        private bool _pipelineBusyNotified;

        private void RefreshBusyUi()
        {
            RecoverOrphanThinkingChrome();

            var busy = GameForgeSession.IsBusy || _runUiActive || _thinkingTimerActive;
            if (!busy)
                _suppressThinkingRestart = false;
            if (_sendBtn != null)
            {
                _sendBtn.text = busy ? "■" : "↑";
                _sendBtn.tooltip = busy ? "Stop" : "Send";
                _sendBtn.EnableInClassList("gf-chat-send-stop", busy);
                _sendBtn.EnableInClassList("gf-btn-primary", !busy);
            }

            _modePicker?.SetInteractable(!busy);
            _picker?.SetInteractable(!busy);
            if (_continueBtn != null && _continueBtn.style.display == DisplayStyle.Flex)
                _continueBtn.SetEnabled(!busy);
            if (_activityBtn != null)
            {
                _activityBtn.style.display = busy ? DisplayStyle.Flex : DisplayStyle.None;
                if (!busy && _activityOpen) CloseActivityPanel();
            }
            if (!busy) SetRunStatusLocal(null);
            if (_pipelineBusyNotified != busy)
            {
                _pipelineBusyNotified = busy;
                if (!busy)
                {
                    GameForgePipelineState.ClearSetupLaunch();
                    GameForgePipelineState.ClearIdleRunning();
                }
                GameForgePipelineState.NotifyChanged();
            }
            // Do not reopen Thinking after we already finalized a reply while remote busy lags.
            if (!_runStopHandled &&
                !_suppressThinkingRestart &&
                !GameForgeSession.ReattachSuppressed &&
                (GameForgeSession.IsBusy || _runUiActive) &&
                (_thinkingBox == null || _thinkingBox.parent == null))
                BeginThinkingRun("Agent running…");
            if (_status != null && busy && string.IsNullOrEmpty(_status.text))
                _status.text = GameForgeSession.RemoteBusy && !GameForgeSession.Bridge.IsBusy
                    ? "Agent running (sidecar)…"
                    : "Running…";
            if (_status != null && !busy &&
                (_status.text == "Running…" || _status.text == "Stopping…" ||
                 _status.text == "Agent running (sidecar)…" || _status.text == "Reconnecting…" ||
                 _status.text == "Stopped" || _status.text == "Cancelled"))
                _status.text = "";
        }

        /// <summary>
        /// After Stop, late poll/SSE can recreate Thinking while the sidecar is already idle.
        /// Tear that chrome down so the send button returns to ↑.
        /// </summary>
        private void RecoverOrphanThinkingChrome()
        {
            if (GameForgeSession.IsBusy || GameForgeRunWatcher.IsWatching)
                return;
            if (GameForgeSession.RemoteBusy)
                return;

            var statusStuck = _status != null &&
                              (_status.text == "Stopped" || _status.text == "Stopping…" ||
                               _status.text == "Cancelled");
            if (!_runStopHandled && !statusStuck && !GameForgeSession.ReattachSuppressed)
                return;

            var hasLiveChrome = (_thinkingBox != null && _thinkingBox.parent != null &&
                                 !_thinkingBox.ClassListContains("is-stopped"))
                                || _runUiActive
                                || _thinkingTimerActive;
            if (!hasLiveChrome) return;

            if (_thinkingBox != null && _thinkingBox.parent != null &&
                !_thinkingBox.ClassListContains("is-stopped"))
                _thinkingBox.RemoveFromHierarchy();
            ClearThinkingState();
            _runStopHandled = true;
            _suppressThinkingRestart = true;
            PersistRunStopHandled(true);
            if (_status != null &&
                (_status.text == "Stopped" || _status.text == "Stopping…" ||
                 _status.text == "Running…" || _status.text == "Agent running (sidecar)…" ||
                 _status.text == "Cancelled"))
                _status.text = "";
        }

        private const string RunStopHandledKey = "V57.GameForge.RunStopHandled";

        private static void PersistRunStopHandled(bool value)
        {
            try { SessionState.SetBool(RunStopHandledKey, value); }
            catch { /* ignore */ }
        }

        private static bool LoadRunStopHandled()
        {
            try { return SessionState.GetBool(RunStopHandledKey, false); }
            catch { return false; }
        }

        private void ClearStopGuardsForNewRun()
        {
            _runStopHandled = false;
            PersistRunStopHandled(false);
            GameForgeSession.ClearReattachSuppress();
            _suppressThinkingRestart = false;
        }

        private void OnSendOrStop()
        {
            if (GameForgeSession.IsBusy || _runUiActive || _thinkingTimerActive)
                _ = StopRunAsync();
            else
                _ = SendAsync();
        }

        private async System.Threading.Tasks.Task StopRunAsync()
        {
            _status.text = "Stopping…";
            SetRunStatusLocal("Stopping…");
            var sid = GameForgeSession.Bridge.ActiveSessionId;
            if (!string.IsNullOrEmpty(sid)) _checkpointSessionId = sid;
            GameForgeSession.BeginStop();
            FinalizeThinking(cancelled: true);
            SetRunStatusLocal(null);
            _status.text = "Stopped";
            RefreshBusyUi();
            await RefreshContinueButtonAsync();
        }

        private void SetContinueVisible(bool visible)
        {
            if (_continueBtn == null) return;
            _continueBtn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                _continueBtn.text = _checkpointTurn > 0
                    ? $"Continue · turn {_checkpointTurn}"
                    : "Continue";
                _continueBtn.SetEnabled(!GameForgeSession.IsBusy);
            }
        }

        private void RefreshContinueButton() => _ = RefreshContinueButtonAsync();

        private async System.Threading.Tasks.Task RefreshContinueButtonAsync()
        {
            try
            {
                var sid = !string.IsNullOrEmpty(_checkpointSessionId)
                    ? _checkpointSessionId
                    : GameForgeSession.Bridge.ActiveSessionId;
                if (string.IsNullOrEmpty(sid) || GameForgeSession.IsBusy)
                {
                    SetContinueVisible(false);
                    return;
                }

                await GameForgeSession.Bridge.EnsureStartedAsync();
                var json = await GameForgeSession.Bridge.GetAsync($"/api/sessions/{sid}/checkpoint");
                var resumable = JsonBool(json, "resumable");
                _checkpointTurn = JsonInt(json, "turn", _checkpointTurn);
                _checkpointOp = JsonField(json, "op") ?? _checkpointOp ?? "";
                if (resumable) _checkpointSessionId = sid;
                SetContinueVisible(resumable);
            }
            catch
            {
                SetContinueVisible(false);
            }
        }

        private void ToggleActivityPanel()
        {
            if (_activityOpen) CloseActivityPanel();
            else OpenActivityPanel();
        }

        private void OpenActivityPanel()
        {
            if (_activityPanel == null) return;
            _activityOpen = true;
            _activityPanel.style.display = DisplayStyle.Flex;
            ResetActivityScrollTop();
            if (_activityBody != null) _activityBody.text = "Loading…";
            _ = RefreshActivityAsync();
            _activityPoll?.Pause();
            _activityPoll = _activityPanel.schedule.Execute(() =>
            {
                if (!_activityOpen) return;
                _ = RefreshActivityAsync();
            }).Every(2000);
        }

        private void CloseActivityPanel()
        {
            _activityOpen = false;
            _activityPoll?.Pause();
            _activityPoll = null;
            if (_activityPanel != null) _activityPanel.style.display = DisplayStyle.None;
        }

        private async System.Threading.Tasks.Task RefreshActivityAsync()
        {
            if (_activityBody == null) return;
            try
            {
                await GameForgeSession.Bridge.EnsureStartedAsync();
                var sid = GameForgeSession.Bridge.ActiveSessionId;
                if (string.IsNullOrEmpty(sid) && GameForgeSession.RemoteBusySessionIds is { Length: > 0 })
                    sid = GameForgeSession.RemoteBusySessionIds[0];
                var json = await GameForgeSession.Bridge.GetSessionActivityAsync(sid);
                var text = FormatActivitySnapshot(json);
                _activityBody.schedule.Execute(() =>
                {
                    if (_activityBody == null) return;
                    CaptureActivityScroll();
                    _activityBody.text = text;
                    RestoreActivityScrollDeferred();
                });
            }
            catch (Exception ex)
            {
                var raw = ex.Message ?? "";
                var msg = raw.IndexOf("404", StringComparison.Ordinal) >= 0 ||
                          raw.IndexOf("Activity endpoint", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          raw.IndexOf("No active session", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "Activity unavailable. Restart agent (Workbench → Bootstrap Agent) if this persists.\n" + Trunc(raw, 140)
                    : "Could not load activity: " + Trunc(raw, 160);
                _activityBody.schedule.Execute(() =>
                {
                    if (_activityBody == null) return;
                    CaptureActivityScroll();
                    _activityBody.text = msg;
                    RestoreActivityScrollDeferred();
                });
            }
        }

        private void CaptureActivityScroll()
        {
            if (_activityScroll == null) return;
            _activityScrollY = _activityScroll.scrollOffset.y;
        }

        private void RestoreActivityScroll()
        {
            if (_activityScroll == null) return;
            _activityScroll.scrollOffset = new Vector2(0, _activityScrollY);
        }

        private void RestoreActivityScrollDeferred()
        {
            RestoreActivityScroll();
            if (_activityScroll == null) return;
            _activityScroll.schedule.Execute(RestoreActivityScroll).StartingIn(0);
            _activityScroll.schedule.Execute(RestoreActivityScroll).StartingIn(50);
        }

        private void ResetActivityScrollTop()
        {
            if (_activityScroll == null) return;
            _activityScrollY = 0f;
            _activityScroll.scrollOffset = Vector2.zero;
        }

        private static string FormatActivitySnapshot(string json)
        {
            if (string.IsNullOrEmpty(json)) return "No activity yet.";
            var sb = new System.Text.StringBuilder();
            var busy = JsonBool(json, "busy");
            var slug = JsonField(json, "slug") ?? "";
            var forgeMode = JsonField(json, "forgeMode") ?? "";
            var op = JsonField(json, "op") ?? "";
            var mode = JsonField(json, "mode") ?? "";
            var turn = JsonInt(json, "turn");
            var events = JsonInt(json, "eventCount");
            var model = JsonNestedField(json, "provider", "model")
                        ?? JsonNestedField(json, "provider", "label")
                        ?? "";

            sb.Append(busy ? "Busy" : "Idle");
            if (!string.IsNullOrEmpty(forgeMode)) sb.Append(" · ").Append(forgeMode);
            if (!string.IsNullOrEmpty(op)) sb.Append(" · ").Append(op);
            if (!string.IsNullOrEmpty(mode)) sb.Append(" · ").Append(mode);
            if (turn > 0) sb.Append(" · turn ").Append(turn);
            sb.Append('\n');
            if (!string.IsNullOrEmpty(slug)) sb.Append("Project: ").Append(slug).Append('\n');
            if (!string.IsNullOrEmpty(model)) sb.Append("Model: ").Append(model).Append('\n');
            if (events > 0) sb.Append("Events: ").Append(events).Append('\n');
            var note = JsonField(json, "note");
            if (!string.IsNullOrEmpty(note)) sb.Append(note).Append('\n');

            var statuses = JsonStringArray(json, "lastStatus");
            if (statuses.Count > 0)
            {
                sb.Append("\nRecent status:\n");
                for (var i = Math.Max(0, statuses.Count - 12); i < statuses.Count; i++)
                    sb.Append("· ").Append(statuses[i]).Append('\n');
            }

            var files = JsonStringArray(json, "recentFiles");
            if (files.Count > 0)
            {
                sb.Append("\nRecent files:\n");
                for (var i = Math.Max(0, files.Count - 16); i < files.Count; i++)
                    sb.Append("· ").Append(files[i]).Append('\n');
            }

            var tools = JsonToolSummaries(json);
            if (tools.Count > 0)
            {
                sb.Append("\nRecent tools:\n");
                for (var i = Math.Max(0, tools.Count - 16); i < tools.Count; i++)
                    sb.Append("· ").Append(tools[i]).Append('\n');
            }

            var snippet = JsonField(json, "lastAssistantSnippet");
            if (!string.IsNullOrEmpty(snippet))
            {
                sb.Append("\nLast assistant:\n");
                sb.Append(snippet.Trim());
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>One-line pulse for the status bar while a run is active.</summary>
        private static string FormatActivityPulse(string json, int prevEvents, out int events, out bool eventsAdvanced)
        {
            events = JsonInt(json, "eventCount");
            eventsAdvanced = events > prevEvents && prevEvents >= 0;
            if (string.IsNullOrEmpty(json)) return "Running…";

            var busy = JsonBool(json, "busy");
            if (!busy) return "Idle";

            var tools = JsonToolSummaries(json);
            var statuses = JsonStringArray(json, "lastStatus");
            var last = tools.Count > 0
                ? tools[tools.Count - 1]
                : statuses.Count > 0 ? statuses[statuses.Count - 1] : "";

            if (!string.IsNullOrEmpty(last))
                return $"Running · {Trunc(last.Trim(), 52)} · events {events}";
            return events > 0 ? $"Running · events {events}" : "Running…";
        }

        private static string JsonNestedField(string json, string parent, string child)
        {
            var key = $"\"{parent}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return null;
            var brace = json.IndexOf('{', i + key.Length);
            if (brace < 0) return null;
            var depth = 0;
            var end = -1;
            for (var p = brace; p < json.Length; p++)
            {
                var c = json[p];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) { end = p; break; }
                }
            }
            if (end < 0) return null;
            return JsonField(json.Substring(brace, end - brace + 1), child);
        }

        private static List<string> JsonStringArray(string json, string field)
        {
            var list = new List<string>();
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
            for (var p = 0; p < inner.Length; p++)
            {
                if (inner[p] != '"') continue;
                var sb = new System.Text.StringBuilder();
                for (var q = p + 1; q < inner.Length; q++)
                {
                    var c = inner[q];
                    if (c == '\\' && q + 1 < inner.Length)
                    {
                        sb.Append(inner[++q]);
                        continue;
                    }
                    if (c == '"')
                    {
                        list.Add(sb.ToString());
                        p = q;
                        break;
                    }
                    sb.Append(c);
                }
            }
            return list;
        }

        private static List<string> JsonToolSummaries(string json)
        {
            var list = new List<string>();
            var key = "\"recentTools\"";
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
                        var obj = inner.Substring(start, p - start + 1);
                        var name = JsonField(obj, "name") ?? "tool";
                        var path = JsonField(obj, "path");
                        var status = JsonField(obj, "status");
                        var line = name;
                        if (!string.IsNullOrEmpty(status)) line += " · " + status;
                        if (!string.IsNullOrEmpty(path)) line += " · " + path;
                        list.Add(line);
                        start = -1;
                    }
                }
            }
            return list;
        }

        private async System.Threading.Tasks.Task ContinueFromCheckpointAsync()
        {
            if (GameForgeSession.IsBusy || _runUiActive || _thinkingTimerActive)
            {
                Append("notice", "A run is already in progress — click ■ to stop.");
                return;
            }

            var sid = _checkpointSessionId;
            if (string.IsNullOrEmpty(sid))
            {
                Append("error", "No checkpoint session — Stop an Agent run first.");
                return;
            }

            SetContinueVisible(false);
            Append("you", "continue");
            ClearStopGuardsForNewRun();
            BeginThinkingRun("Continuing…");
            ShowRunBanner("Continue", $"turn {_checkpointTurn}");
            RefreshBusyUi();

            try
            {
                GameForgeSession.Bridge.ActiveSessionId = sid;
                var activityJson = await GameForgeSession.Bridge.GetSessionActivityAsync(sid);
                var after = ParseActivityEventCount(activityJson);
                GameForgeRunWatcher.StartWatching(sid, fromIndex: after);

                var slug = EscapeJson(GameForgeSession.SelectedSlug);
                var modeJson = EscapeJson(GameForgeSession.ModeLabel(GameForgeSession.Mode));
                var op = (_checkpointOp ?? "").Trim();

                // Staged skills: prefer /chat with forceChatSkillId (works for Cursor + LLM).
                if (op == "game-setup" || op == "game-setup-run-all"
                    || GameForgeSession.Mode == GameForgeMode.Production)
                {
                    var setupSkill = GameForgeSession.GameSetupRunAll || op == "game-setup-run-all"
                        ? "game-setup-run-all"
                        : "game-setup";
                    GameForgePipelineState.TrackSetupCreates = true;
                    var body =
                        $"{{\"slug\":\"{slug}\",\"message\":\"continue\",\"chatMode\":\"agent\",\"forgeMode\":\"Production\",\"forceChatSkillId\":\"{setupSkill}\"}}";
                    await GameForgeSession.Bridge.KickOffPostSseAsync(
                        "/api/sessions/chat",
                        body,
                        default);
                }
                else if (op == "prototype-full")
                {
                    var body =
                        $"{{\"slug\":\"{slug}\",\"message\":\"continue\",\"chatMode\":\"agent\",\"forgeMode\":\"Prototype\",\"forceChatSkillId\":\"prototype-full\"}}";
                    await GameForgeSession.Bridge.KickOffPostSseAsync(
                        "/api/sessions/chat",
                        body,
                        default);
                }
                else
                {
                    await GameForgeSession.Bridge.KickOffPostSseAsync(
                        $"/api/sessions/{sid}/continue",
                        "{\"message\":\"continue\"}",
                        default);
                }
            }
            catch (OperationCanceledException)
            {
                Append("notice", "Continue cancelled.");
                FinalizeThinking(cancelled: true);
            }
            catch (Exception ex)
            {
                Append("error", ex.Message);
                FinalizeThinking(cancelled: true);
            }
            finally
            {
                await GameForgeSession.RefreshRemoteBusyAsync();
                if (!GameForgeSession.IsBusy)
                {
                    if (_thinkingBox != null) FinalizeThinking(cancelled: false);
                    HideRunBanner();
                    await RefreshContinueButtonAsync();
                }
                RefreshBusyUi();
            }
        }

        private void AppendOnMain(string role, string text)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                Append(role, text);
            };
        }

        private void PrefillComposeOnMain(string text)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                SetInputValueMultiline(text);
                _input?.Focus();
            };
        }

        private void SetStatusOnMain(string text)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null || _status == null) return;
                _status.text = text ?? "";
                Repaint();
            };
        }

        private void SetRunStatusOnMain(string text)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                SetRunStatusLocal(text);
            };
        }

        private void RefreshContextOnMain()
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                RefreshContext();
            };
        }

        private static void SyncForgeModeFromActivityJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var forgeMode = JsonField(json, "forgeMode");
            if (!string.IsNullOrEmpty(forgeMode))
                GameForgeSession.ApplyRemoteForgeMode(forgeMode);
        }

        private void SetRunStatusLocal(string text)
        {
            // Never show LLM/tool progress under the context bar — Thinking bubble owns that.
            if (_runStatus != null)
            {
                _runStatus.text = "";
                _runStatus.style.display = DisplayStyle.None;
            }

            if (!string.IsNullOrEmpty(text) && _thinkingBox != null && _thinkingBox.parent != null)
                AppendThinking(text);
        }

        private void AppendThinkingOnMain(string text)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                AppendThinking(text);
            };
        }

        private void BeginThinkingRun(string title)
        {
            // After user Stop, ignore late session/status events that would reopen chrome.
            if (_runStopHandled) return;

            _suppressThinkingRestart = false;
            _runUiActive = true;
            _thinkingBuf.Clear();
            _writtenFiles.Clear();
            _toolCalls = 0;
            _latestAssistantText = "";
            _thinkingStartedAt = EditorApplication.timeSinceStartup;
            EnsureThinkingBubble();
            RefreshThinkingHead();
            if (_thinkingBody != null) _thinkingBody.text = "";
            _thinkingBox?.EnableInClassList("is-done", false);
            StartThinkingTimer();
            StartBusyHeartbeat();
            if (!string.IsNullOrEmpty(title))
                AppendThinking(title);
            else
                PinThinkingBubble();
            RefreshBusyUi();
        }

        private void StartBusyHeartbeat()
        {
            StopBusyHeartbeat();
            _lastHeartbeatEvents = -1;
            _heartbeatStalePolls = 0;
            if (_thinkingBox == null) return;
            _busyHeartbeat = _thinkingBox.schedule.Execute(() => _ = PulseBusyActivityAsync()).Every(3000);
            _ = PulseBusyActivityAsync();
        }

        private void StopBusyHeartbeat()
        {
            _busyHeartbeat?.Pause();
            _busyHeartbeat = null;
            _lastHeartbeatEvents = -1;
            _heartbeatStalePolls = 0;
        }

        private async System.Threading.Tasks.Task PulseBusyActivityAsync()
        {
            if (_runStopHandled)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    RecoverOrphanThinkingChrome();
                    RefreshBusyUi();
                };
                return;
            }
            if (!GameForgeSession.IsBusy && !_runUiActive && !_thinkingTimerActive) return;
            try
            {
                await GameForgeSession.Bridge.EnsureStartedAsync();
                var sid = GameForgeSession.Bridge.ActiveSessionId;
                if (string.IsNullOrEmpty(sid) && GameForgeSession.RemoteBusySessionIds is { Length: > 0 })
                    sid = GameForgeSession.RemoteBusySessionIds[0];

                // Kickoff has not returned a session id yet. Do not treat that gap as a finished run.
                if (GameForgeSession.Bridge.IsKickoffBusy)
                    return;
                if (string.IsNullOrEmpty(sid) && _thinkingStartedAt > 0 &&
                    EditorApplication.timeSinceStartup - _thinkingStartedAt < 12d)
                    return;

                // Sidecar idle but Thinking chrome still up (missed done / orphaned watch).
                if (string.IsNullOrEmpty(sid) && !GameForgeSession.RemoteBusy)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null) return;
                        if (GameForgeRunWatcher.IsWatching)
                            GameForgeRunWatcher.StopWatching();
                        if (_thinkingBox != null || _runUiActive || _thinkingTimerActive)
                        {
                            FinalizeThinking(cancelled: false);
                            SetRunStatusLocal(null);
                        }
                        RefreshBusyUi();
                    };
                    return;
                }

                if (string.IsNullOrEmpty(sid)) return;

                var json = await GameForgeSession.Bridge.GetSessionActivityAsync(sid);
                var pulse = FormatActivityPulse(json, _lastHeartbeatEvents, out var events, out var advanced);
                if (events >= 0)
                {
                    if (advanced || _lastHeartbeatEvents < 0)
                        _heartbeatStalePolls = 0;
                    else if (_lastHeartbeatEvents == events)
                        _heartbeatStalePolls++;
                    _lastHeartbeatEvents = events;
                }

                var busyFlag = JsonBool(json, "busy");
                // Activity says not busy and events stalled — close Thinking so Continue works.
                if (!busyFlag && _heartbeatStalePolls >= 4 &&
                    (_thinkingBox != null || _runUiActive || _thinkingTimerActive))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null) return;
                        if (GameForgeRunWatcher.IsWatching)
                            GameForgeRunWatcher.StopWatching();
                        FinalizeThinking(cancelled: false);
                        SetRunStatusLocal(null);
                        RefreshBusyUi();
                    };
                    return;
                }

                if (_heartbeatStalePolls >= 8)
                    pulse += " · idle 24s+ (LLM thinking or stalled — open ? or Stop)";

                EditorApplication.delayCall += () =>
                {
                    if (this == null || _status == null) return;
                    if (!GameForgeSession.IsBusy && !_runUiActive && !_thinkingTimerActive) return;
                    _status.text = pulse;
                };

                if (_activityOpen && _activityBody != null)
                    _ = RefreshActivityAsync();
            }
            catch
            {
                /* heartbeat is best-effort */
            }
        }

        private void StartThinkingTimer()
        {
            StopThinkingTimer();
            _thinkingTimerActive = true;
            if (_thinkingBox == null) return;
            _thinkingTick = _thinkingBox.schedule.Execute(TickThinkingTimer).Every(250);
        }

        private void StopThinkingTimer()
        {
            _thinkingTimerActive = false;
            try { _thinkingTick?.Pause(); } catch { /* ignore */ }
            _thinkingTick = null;
        }

        private void TickThinkingTimer()
        {
            if (!_thinkingTimerActive) return;
            RefreshThinkingHead();
            // Keep Stop (■) in sync even if BusyChanged missed a poll blip.
            if (_sendBtn != null && _sendBtn.text != "■")
                RefreshBusyUi();
        }

        private void RefreshThinkingHead()
        {
            if (_thinkingHead == null) return;
            var secs = Math.Max(0, (int)(EditorApplication.timeSinceStartup - _thinkingStartedAt));
            _thinkingHead.text = secs <= 0 ? "Thinking…" : $"Thinking…  {secs}s";
        }

        private void EnsureThinkingBubble()
        {
            if (_log == null) return;
            if (_emptyHint != null && _emptyHint.parent == _log)
                _emptyHint.RemoveFromHierarchy();

            // ScrollView children live under contentContainer, not the ScrollView itself.
            if (_thinkingBox != null && _thinkingBox.parent != null)
            {
                PinThinkingBubble();
                return;
            }

            _thinkingBox = new VisualElement();
            _thinkingBox.AddToClassList("gf-msg");
            _thinkingBox.AddToClassList("gf-msg-thinking");
            var mark = new VisualElement();
            mark.AddToClassList("gf-msg-mark");
            mark.AddToClassList("gf-msg-mark-think");
            var col = new VisualElement();
            col.AddToClassList("gf-msg-col");
            _thinkingHead = LabelCls("gf-msg-role", "Thinking…");
            _thinkingBody = LabelCls("gf-msg-body", "");
            _thinkingBody.AddToClassList("gf-msg-thinking-body");
            col.Add(_thinkingHead);
            col.Add(_thinkingBody);
            _thinkingBox.Add(mark);
            _thinkingBox.Add(col);
            _log.Add(_thinkingBox);
        }

        private void AppendThinking(string text, bool inline = false)
        {
            if (_runStopHandled) return;
            if (string.IsNullOrEmpty(text)) return;
            var trimmed = text.Trim();
            // Ignore bare punctuation crumbs from streamed assistant deltas
            if (trimmed.Length <= 2 && trimmed.IndexOfAny(new[] { '.', '…', '·', '-' }) >= 0
                && trimmed.IndexOfAny("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()) < 0)
                return;

            if (!_runUiActive)
            {
                _runUiActive = true;
                RefreshBusyUi();
            }
            EnsureThinkingBubble();
            // Dedupe consecutive identical lines (status+banner double-fire).
            var bufStr = _thinkingBuf.ToString();
            var nl = bufStr.LastIndexOf('\n');
            var prev = nl < 0 ? bufStr : bufStr.Substring(nl + 1);
            if (!inline && string.Equals(prev, trimmed, StringComparison.Ordinal))
            {
                RefreshThinkingHead();
                PinThinkingBubble();
                return;
            }

            if (inline && _thinkingBuf.Length > 0)
            {
                if (!char.IsWhiteSpace(_thinkingBuf[_thinkingBuf.Length - 1]))
                    _thinkingBuf.Append(' ');
                _thinkingBuf.Append(trimmed);
            }
            else
            {
                if (_thinkingBuf.Length > 0) _thinkingBuf.Append('\n');
                _thinkingBuf.Append(trimmed);
            }
            // Cap buffer growth on very long runs
            const int maxChars = 12000;
            if (_thinkingBuf.Length > maxChars)
            {
                var s = _thinkingBuf.ToString();
                _thinkingBuf.Clear();
                _thinkingBuf.Append(s.Substring(s.Length - maxChars));
            }

            if (_thinkingHead != null)
                RefreshThinkingHead();
            if (_thinkingBody != null)
                _thinkingBody.text = FormatThinkingBody(_thinkingBuf.ToString());
            _thinkingBody?.EnableInClassList("gf-msg-thinking-body", true);
            PinThinkingBubble();
            GameForgePipelineState.NoteLiveText(trimmed);
            if (!IsLogNearBottom()) return;
            _log?.schedule.Execute(() =>
            {
                if (_log != null && IsLogNearBottom())
                    _log.scrollOffset = new Vector2(0, float.MaxValue);
            });
        }

        /// <summary>
        /// Track the real assistant answer separately from Thinking chrome / tools / status.
        /// Cursor: growing snapshots or short token deltas. OpenAI/MiniMax: full message per turn.
        /// Never concatenate markdown rewrites like **Biol** → **Biolum** (not a string prefix).
        /// </summary>
        private void NoteAssistantText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var next = text;
            if (string.IsNullOrWhiteSpace(next)) return;
            if (IsRunChromeText(next.Trim())) return;

            if (string.IsNullOrEmpty(_latestAssistantText))
            {
                _latestAssistantText = next;
                GameForgePipelineState.NoteLiveText(next);
                return;
            }

            var prev = _latestAssistantText;

            // Cumulative snapshot (growing prefix)
            if (next.StartsWith(prev, StringComparison.Ordinal))
            {
                _latestAssistantText = next;
                GameForgePipelineState.NoteLiveText(next);
                return;
            }

            // Older/partial cumulative chunk
            if (prev.StartsWith(next, StringComparison.Ordinal))
                return;

            // Growing snapshot rewrite (markdown / retokenize) — prefer newer when longer
            if (next.Length >= prev.Length)
            {
                _latestAssistantText = next;
                GameForgePipelineState.NoteLiveText(next);
                return;
            }

            // Short token delta — append without inventing spaces
            if (next.Length <= 32)
            {
                _latestAssistantText = prev + next;
                return;
            }

            // Shorter non-delta snapshot — keep the fuller text
        }

        private static bool IsRunChromeText(string t)
        {
            if (string.IsNullOrEmpty(t)) return true;
            if (t.StartsWith("Thinking", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Agent running", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Agent working", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Agent still working", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Continuing from checkpoint", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Reconnected", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Stopping", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void PinThinkingBubble()
        {
            if (_log == null || _thinkingBox == null || _thinkingBox.parent == null) return;
            // Keep Thinking at the bottom of the log as SYSTEM spam grows.
            _thinkingBox.BringToFront();
            if (IsLogNearBottom())
                _log.scrollOffset = new Vector2(0, float.MaxValue);
        }

        /// <summary>Live log: last lines of the thinking buffer (tools + assistant), not a single crumb.</summary>
        private static string FormatThinkingBody(string full)
        {
            if (string.IsNullOrEmpty(full)) return "";
            var lines = full.Replace("\r\n", "\n").Split('\n');
            const int maxLines = 48;
            var start = lines.Length > maxLines ? lines.Length - maxLines : 0;
            var slice = string.Join("\n", lines, start, lines.Length - start);
            return Trunc(slice, 8000);
        }

        private bool IsLogNearBottom()
        {
            if (_log == null) return true;
            var viewHeight = _log.layout.height;
            var contentHeight = _log.contentContainer.layout.height;
            if (viewHeight <= 0 || contentHeight <= 0) return true;
            var maxScroll = Mathf.Max(0f, contentHeight - viewHeight);
            return _log.scrollOffset.y >= maxScroll - 48f;
        }

        /// <summary>After user Stop, do not auto-reattach or restart Thinking chrome.</summary>
        private bool _runStopHandled;

        /// <summary>
        /// After a successful FinalizeThinking, do not spawn a fresh Thinking bubble just because
        /// remote busy lags a few hundred ms (soft-advice / health poll). Cleared when idle or
        /// when the user starts a new run.
        /// </summary>
        private bool _suppressThinkingRestart;

        private void FinalizeThinking(bool cancelled = false)
        {
            if (cancelled && _runStopHandled) return;

            var reply = _latestAssistantText;
            if (IsRunChromeText(reply)) reply = "";
            _latestAssistantText = "";
            _runUiActive = false;
            // Prevent RefreshBusyUi from immediately reopening Thinking while sidecar busy lags.
            _suppressThinkingRestart = true;

            if (_thinkingBox == null || _thinkingBox.parent == null)
            {
                _thinkingBuf.Clear();
                StopThinkingTimer();
                if (cancelled)
                {
                    _runStopHandled = true;
                    PersistRunStopHandled(true);
                }
                if (!cancelled && !string.IsNullOrEmpty(reply))
                {
                    Append("agent", reply);
                    MaybeScheduleConfirmSheet(reply);
                }
                RefreshBusyUi();
                return;
            }

            if (cancelled)
            {
                _runStopHandled = true;
                PersistRunStopHandled(true);
                _thinkingBox.EnableInClassList("is-stopped", true);
                var head = "Stopped";
                var body = _toolCalls > 0
                    ? $"Stopped after {_toolCalls} tool call(s)."
                    : "Run cancelled.";
                if (_thinkingHead != null) _thinkingHead.text = head;
                if (_thinkingBody != null) _thinkingBody.text = body;
                PersistEntry("thinking", body, head);
                ClearThinkingState();
                RefreshBusyUi();
                return;
            }

            // Drop Thinking chrome — no green "Done / Finished." bubble
            var fileSummary = _writtenFiles.Count > 0 ? BuildRunSummary() : null;
            _thinkingBox.RemoveFromHierarchy();
            ClearThinkingState();
            RefreshBusyUi();

            if (!string.IsNullOrEmpty(reply))
            {
                Append("agent", reply);
                MaybeScheduleConfirmSheet(reply);
            }
            else if (!string.IsNullOrEmpty(fileSummary))
                Append("notice", fileSummary);
        }

        private const string DoneSoundPrefKey = "V57.GameForge.PlayDoneSound";

        /// <summary>
        /// Play packaged Spark.mp3 when the agent finishes an iteration (SSE <c>done</c>),
        /// not on Send kickoff.
        /// </summary>
        private static void PlayAgentIterationFinishedSound()
        {
            if (!EditorPrefs.GetBool(DoneSoundPrefKey, true)) return;
            GameForgeEditorAudio.PlayAgentDone();
        }

        /// <summary>
        /// Show INIT/continue/STOP sheet. Retries briefly — poll watcher may still mark IsBusy
        /// for a few hundred ms after the assistant message arrives.
        /// </summary>
        private void MaybeScheduleConfirmSheet(string agentText)
        {
            if (string.IsNullOrWhiteSpace(agentText)) return;
            if (GameForgeConfirmPromptParser.TryParse(agentText) == null) return;

            TryShowConfirmSheet(agentText);
            if (_confirmSheet != null && _confirmSheet.IsOpen &&
                GameForgeConfirmPromptParser.IsInitTableComplete(agentText))
                return;

            EditorApplication.delayCall += () => TryShowConfirmSheet(agentText);
            _root?.schedule.Execute(() => TryShowConfirmSheet(agentText)).StartingIn(450);
            _root?.schedule.Execute(() => TryShowConfirmSheet(agentText)).StartingIn(1400);
            _root?.schedule.Execute(() => TryShowConfirmSheet(agentText)).StartingIn(2800);
        }

        private void TryShowConfirmSheet(string agentText)
        {
            if (_confirmSheet == null || string.IsNullOrWhiteSpace(agentText)) return;
            var prompt = GameForgeConfirmPromptParser.TryParse(agentText);
            if (prompt == null) return;

            if (_confirmSheet.IsOpen)
            {
                if (prompt.Kind == ConfirmPromptKind.InitDefaults &&
                    _confirmSheet.PromptKind == ConfirmPromptKind.InitDefaults &&
                    prompt.Variables.Count > _confirmSheet.VariableCount)
                    _confirmSheet.Show(prompt);
                GameForgePipelineState.NoteConfirmPrompt(prompt);
                return;
            }

            if (prompt.Kind == ConfirmPromptKind.InitDefaults &&
                !GameForgeConfirmPromptParser.IsInitTableComplete(prompt))
                return;

            _confirmSheet.Show(prompt);
            GameForgePipelineState.NoteConfirmPrompt(prompt);
        }

        private void ClearThinkingState()
        {
            StopThinkingTimer();
            StopBusyHeartbeat();
            _runUiActive = false;
            _thinkingBuf.Clear();
            _writtenFiles.Clear();
            _toolCalls = 0;
            _thinkingBox = null;
            _thinkingHead = null;
            _thinkingBody = null;
        }

        private static void NoteSetupCreated(string path)
        {
            if (!GameForgePipelineState.TrackSetupCreates || string.IsNullOrEmpty(path)) return;
            var existed = false;
            try
            {
                var n = path.Trim().Replace('\\', '/');
                var root = GameForgeBootstrap.ProjectRoot ?? "";
                var full = Path.IsPathRooted(n)
                    ? Path.GetFullPath(n)
                    : Path.GetFullPath(Path.Combine(root, n));
                existed = File.Exists(full) || Directory.Exists(full);
            }
            catch
            {
                existed = false;
            }

            GameForgePipelineState.NoteCreatedFile(path, existed);
        }

        private string BuildRunSummary()
        {
            if (_writtenFiles.Count > 0)
            {
                var unique = new List<string>();
                foreach (var f in _writtenFiles)
                {
                    if (!unique.Contains(f)) unique.Add(f);
                }
                var show = unique.Count <= 6 ? unique : unique.GetRange(0, 6);
                var list = string.Join("\n", show.ConvertAll(p => "· " + p));
                var more = unique.Count > 6 ? $"\n· … +{unique.Count - 6} more" : "";
                return $"Wrote {unique.Count} file(s):\n{list}{more}";
            }

            if (_toolCalls > 0)
                return $"Finished · {_toolCalls} tool call(s).";
            return "Finished.";
        }

        /// <summary>Consume one SSE JSON payload line into the chat UI.</summary>
        public static void SyncPipelineFromLog(bool notify = true)
        {
            EnsurePersistedLoaded();
            var rows = new List<(string role, string text)>(SharedEntries.Count);
            foreach (var e in SharedEntries)
            {
                if (e == null) continue;
                rows.Add((e.role, e.text));
            }
            GameForgePipelineState.ApplyChatLog(rows, notify);
        }

        public static void HandleStreamEvent(string jsonLine)
        {
            if (_instance == null) Open();
            _instance.HandleStreamEventLocal(jsonLine);
        }

        private void HandleStreamEventLocal(string jsonLine)
        {
            if (string.IsNullOrWhiteSpace(jsonLine)) return;
            var type = JsonField(jsonLine, "type");

            // After user Stop, ignore late poll/SSE that would reopen Thinking.
            // Still allow terminal events so checkpoint notices are not lost if Stop raced.
            if (_runStopHandled && type is not ("done" or "cancelled" or "error"))
                return;

            switch (type)
            {
                case "session":
                {
                    var sid = JsonField(jsonLine, "id") ?? "";
                    if (!string.IsNullOrEmpty(sid))
                        _checkpointSessionId = sid;
                    var shortId = sid.Length > 8 ? sid.Substring(0, 8) : sid;
                    var forgeMode = JsonField(jsonLine, "forgeMode");
                    var op = JsonField(jsonLine, "op") ?? "run";
                    if (!string.IsNullOrEmpty(forgeMode))
                    {
                        GameForgeSession.ApplyRemoteForgeMode(forgeMode);
                        RefreshContextOnMain();
                    }
                    _activeOp = op;
                    AppendOnMain("system", string.IsNullOrEmpty(shortId)
                        ? $"Session · {forgeMode ?? GameForgeSession.ModeLabel(GameForgeSession.Mode)}"
                        : $"Session {shortId}… · {forgeMode ?? GameForgeSession.ModeLabel(GameForgeSession.Mode)}");
                    var slug = GameForgeSession.SelectedSlug;
                    var title = op == "generate"
                        ? (string.IsNullOrEmpty(slug) ? "Building prototype…" : $"Building {slug}…")
                        : op == "continue"
                            ? "Continuing from checkpoint…"
                            : "Agent working…";
                    // Status line only (thinking bubble already started for chat; generate starts here)
                    if (_thinkingBox == null || _thinkingBox.parent == null)
                        BeginThinkingRun(title);
                    else
                        SetRunStatusLocal(title);
                    SetStatusOnMain("Running…");
                    SetContinueVisible(false);
                    break;
                }
                case "status":
                {
                    var msg = JsonField(jsonLine, "message") ?? "";
                    if (!string.IsNullOrEmpty(msg))
                    {
                        // Progress belongs in Thinking — SYSTEM bubbles only in /debugmode
                        if (GameForgeSession.ChatDebugMode)
                            AppendOnMain("system", msg);
                        GameForgePipelineState.NoteLiveText(msg);
                        SetStatusOnMain(Trunc(msg, 80));
                        SetRunStatusOnMain(msg);
                        if (GameForgeMcpApprovalUi.LooksLikeApprovalNeeded(msg))
                            GameForgeMcpApprovalUi.Prompt(msg);
                        EditorApplication.delayCall += () =>
                        {
                            if (this == null) return;
                            AppendThinking(msg);
                        };
                    }
                    break;
                }
                case "mcp-approval":
                {
                    var reason = JsonField(jsonLine, "reason") ?? "revoked";
                    var msg = JsonField(jsonLine, "message")
                              ?? "Unity MCP needs Allow for «GameForge Chat» in Project Settings.";
                    AppendOnMain("notice", msg);
                    SetStatusOnMain("Unity MCP approval needed");
                    SetRunStatusOnMain(msg);
                    GameForgeMcpApprovalUi.Prompt(reason + ": " + msg, force: true);
                    break;
                }
                case "assistant":
                {
                    var text = JsonField(jsonLine, "text");
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Apply synchronously so FinalizeThinking on the next SSE event still sees it
                        // (delayCall raced with done and left MiniMax answers as empty AGENT).
                        var beforeLen = _latestAssistantText?.Length ?? 0;
                        NoteAssistantText(text);
                        var after = _latestAssistantText ?? "";
                        MaybeScheduleConfirmSheet(after);
                        EditorApplication.delayCall += () =>
                        {
                            if (this == null) return;
                            if (after.Length > beforeLen)
                            {
                                var grown = after.Substring(beforeLen);
                                if (beforeLen == 0)
                                    AppendThinking(after.TrimEnd());
                                else
                                    AppendThinking(grown.TrimEnd(), inline: true);
                            }
                            // Do not Prompt on assistant text — happy-path SETUP reports
                            // mention "Unity CLI" and used to reopen the setup dialog in a loop.
                        };
                    }
                    break;
                }
                case "tool":
                {
                    var name = JsonField(jsonLine, "name") ?? "tool";
                    var path = JsonField(jsonLine, "path") ?? JsonField(jsonLine, "file") ?? "";
                    var status = JsonField(jsonLine, "status");
                    var line = string.IsNullOrEmpty(path) ? name : $"{name} · {path}";
                    if (GameForgeSession.ChatDebugMode)
                        AppendOnMain("system", string.IsNullOrEmpty(path) ? $"tool · {name}" : $"{name} · {path}");
                    _toolCalls++;
                    if (name == "write_file" && !string.IsNullOrEmpty(path))
                    {
                        _writtenFiles.Add(path);
                        // status=call fires before the write, so a missing file is a create.
                        if (string.IsNullOrEmpty(status) || status == "call")
                            NoteSetupCreated(path);
                    }
                    SetRunStatusOnMain(Trunc(line, 72));
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null) return;
                        AppendThinking(line);
                    };
                    break;
                }
                case "advice":
                {
                    var dig = JsonField(jsonLine, "digest");
                    if (!string.IsNullOrEmpty(dig)) AppendOnMain("system", Trunc(dig, 1200));
                    break;
                }
                case "notice":
                {
                    var notice = JsonField(jsonLine, "message") ?? JsonField(jsonLine, "text") ?? "";
                    if (!string.IsNullOrEmpty(notice))
                        AppendOnMain("notice", notice);
                    break;
                }
                case "error":
                {
                    var err = JsonField(jsonLine, "error") ?? jsonLine;
                    var kind = JsonField(jsonLine, "errorKind");
                    if (!string.IsNullOrEmpty(kind))
                        err = $"[{kind}] {err}";
                    AppendOnMain("error", err);
                    SetStatusOnMain("Error");
                    if (GameForgeMcpApprovalUi.LooksLikeApprovalNeeded(err))
                        GameForgeMcpApprovalUi.Prompt(err, force: true);
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null) return;
                        FinalizeThinking(cancelled: true);
                        SetRunStatusLocal(null);
                    };
                    break;
                }
                case "cancelled":
                {
                    var resumable = JsonBool(jsonLine, "resumable");
                    _checkpointTurn = JsonInt(jsonLine, "turn", _checkpointTurn);
                    AppendOnMain("notice", resumable
                        ? $"Stopped — checkpoint ready{( _checkpointTurn > 0 ? $" (turn {_checkpointTurn})" : "")}. Press Continue to resume."
                        : "Cancelled.");
                    SetStatusOnMain(resumable ? "Checkpoint ready" : "Cancelled");
                    if (!resumable) GameForgeSession.Bridge.ActiveSessionId = "";
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null) return;
                        FinalizeThinking(cancelled: true);
                        SetRunStatusLocal(null);
                        SetContinueVisible(resumable);
                        NotifyPipelineSettled();
                        _ = GameForgeSession.RefreshRemoteBusyAsync();
                    };
                    break;
                }
                case "done":
                {
                    var st = JsonField(jsonLine, "status") ?? "finished";
                    var resumable = JsonBool(jsonLine, "resumable");
                    _checkpointTurn = JsonInt(jsonLine, "turn", _checkpointTurn);
                    if (st == "cancelled" || resumable)
                    {
                        AppendOnMain("notice", resumable
                            ? $"Stopped — checkpoint ready{( _checkpointTurn > 0 ? $" (turn {_checkpointTurn})" : "")}. Press Continue to resume."
                            : "Cancelled.");
                        SetStatusOnMain(resumable ? "Checkpoint ready" : "Cancelled");
                        EditorApplication.delayCall += () =>
                        {
                            if (this == null) return;
                            FinalizeThinking(cancelled: true);
                            SetRunStatusLocal(null);
                            SetContinueVisible(resumable);
                            _ = GameForgeSession.RefreshRemoteBusyAsync();
                        };
                    }
                    else
                    {
                        GameForgeSession.Bridge.ActiveSessionId = "";
                        if (GameForgeSession.ChatDebugMode)
                            AppendOnMain("system", "Done.");
                        SetStatusOnMain("");
                        PlayAgentIterationFinishedSound();
                        var filesSnapshot = new List<string>(_writtenFiles);
                        var opSnapshot = _activeOp;
                        EditorApplication.delayCall += () =>
                        {
                            if (this == null) return;
                            FinalizeThinking(cancelled: false);
                            SetRunStatusLocal(null);
                            SetContinueVisible(false);
                            TryPostGenerateHook(opSnapshot, filesSnapshot);
                            _activeOp = "";
                            _ = GameForgeSession.RefreshRemoteBusyAsync();
                        };
                    }
                    break;
                }
            }
        }

        private void TryPostGenerateHook(string op, List<string> writtenFiles)
        {
            var shouldRun = op == "generate" || (writtenFiles != null && writtenFiles.Count > 0);
            if (!shouldRun) return;
            try
            {
                var msg = GameForgePostGenerate.Run(
                    GameForgeSession.SelectedSlug,
                    GameForgeSession.Mode.ToString(),
                    writtenFiles);
                if (!string.IsNullOrEmpty(msg))
                    Append("notice", msg);
            }
            catch (Exception ex)
            {
                Append("error", "Post-generate: " + Trunc(ex.Message, 120));
            }
        }

        private static string JsonField(string json, string field)
        {
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return null;
            var colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return null;
            var q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return null;
            // Skip whitespace — if value is not a string (true/false/number), bail for JsonField
            var scan = colon + 1;
            while (scan < json.Length && char.IsWhiteSpace(json[scan])) scan++;
            if (scan >= json.Length || json[scan] != '"') return null;
            q1 = scan;
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

        private static string EscapeJson(string s) => GameForgeJson.Escape(s);

        private static bool JsonBool(string json, string field, bool fallback = false)
        {
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return fallback;
            var colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return fallback;
            var slice = json.Substring(colon + 1, Math.Min(16, json.Length - colon - 1)).TrimStart();
            if (slice.StartsWith("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (slice.StartsWith("false", StringComparison.OrdinalIgnoreCase)) return false;
            if (slice.StartsWith("\"true\"", StringComparison.OrdinalIgnoreCase)) return true;
            return fallback;
        }

        private static int JsonInt(string json, string field, int fallback = 0)
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

        private static int ParseActivityEventCount(string json) => JsonInt(json, "eventCount", 0);

        private void OnEnable()
        {
            _instance = this;
            GameForgeSession.Changed += RefreshContext;
            GameForgeSession.BusyChanged += RefreshBusyUi;
            GameForgeSession.OrphanRemoteBusy += OnOrphanRemoteBusy;
            GameForgeRunWatcher.OnEvent -= HandleStreamEvent;
            GameForgeRunWatcher.OnEvent += HandleStreamEvent;
        }

        private void OnDisable()
        {
            CloseActivityPanel();
            GameForgeSession.Changed -= RefreshContext;
            GameForgeSession.BusyChanged -= RefreshBusyUi;
            GameForgeSession.OrphanRemoteBusy -= OnOrphanRemoteBusy;
            GameForgeRunWatcher.OnEvent -= HandleStreamEvent;
            if (_instance == this) _instance = null;
        }

        private void OnOrphanRemoteBusy()
        {
            if (this == null) return;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                _ = TryReattachActiveSessionAsync();
            };
        }

        private void CreateGUI()
        {
            _root = rootVisualElement;
            _root.Clear();
            BuildUi(_root);
            _runStopHandled = LoadRunStopHandled();
            RestorePersistedLog();
            // After reload: restore history before Thinking chrome (BuildUi must not call RefreshBusyUi).
            RefreshBusyUi();
            RefreshContext();
            ApplyModeTheme(GameForgeSession.ChatMode);
            _ = RefreshProvidersAsync();
            _ = TryReattachActiveSessionAsync();
            // Heal stuck Stop chrome from a prior domain reload / late events.
            _root.schedule.Execute(() =>
            {
                if (this == null) return;
                RecoverOrphanThinkingChrome();
                RefreshBusyUi();
            }).StartingIn(400);
            _root.schedule.Execute(() =>
            {
                _root.Focus();
                _input?.Focus();
            });
        }

        /// <summary>
        /// After Play Mode / domain reload: resume poll watcher (no long-lived SSE).
        /// </summary>
        private async System.Threading.Tasks.Task TryReattachActiveSessionAsync()
        {
            if (_reattachInFlight || GameForgeSession.ReattachSuppressed || _runStopHandled) return;
            if (GameForgeRunWatcher.IsWatching) return;
            _reattachInFlight = true;
            try
            {
                await GameForgeRunWatcher.ResumeIfNeededAsync();
                if (!GameForgeRunWatcher.IsWatching)
                {
                    RecoverOrphanThinkingChrome();
                    RefreshBusyUi();
                    return;
                }

                if (_runStopHandled || GameForgeSession.ReattachSuppressed)
                {
                    GameForgeRunWatcher.StopWatching();
                    RecoverOrphanThinkingChrome();
                    RefreshBusyUi();
                    return;
                }

                var target = GameForgeSession.Bridge.ActiveSessionId ?? "";
                if (!string.IsNullOrEmpty(target))
                {
                    try
                    {
                        var activityJson = await GameForgeSession.Bridge.GetSessionActivityAsync(target);
                        SyncForgeModeFromActivityJson(activityJson);
                        RefreshContextOnMain();
                    }
                    catch
                    {
                        /* activity is best-effort on reattach */
                    }
                }

                var noticeKey = "V57.GameForge.ReattachNotice." + target;
                if (!SessionState.GetBool(noticeKey, false) && !string.IsNullOrEmpty(target))
                {
                    SessionState.SetBool(noticeKey, true);
                    AppendOnMain("notice",
                        "Reconnected after Editor reload — agent keeps running in the sidecar.");
                }

                BeginThinkingRun("Reconnected…");
                ShowRunBanner("Agent still working…", "poll");
                RefreshBusyUi();
            }
            finally
            {
                _reattachInFlight = false;
            }
        }

        private void BuildUi(VisualElement root)
        {
            root.AddToClassList("gf-chat-root");
            root.focusable = true;
            root.tabIndex = 0;
            TryAddStyles(root);
            root.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);

            var head = new VisualElement();
            head.AddToClassList("gf-chat-head");
            head.Add(LabelCls("gf-chat-head-title", "Chat"));
            var headActions = new VisualElement();
            headActions.AddToClassList("gf-chat-head-actions");
            headActions.Add(MakeThemeBtn());
            headActions.Add(Btn("Clear", ClearLog, ghost: true));
            _continueBtn = Btn("Continue", () => _ = ContinueFromCheckpointAsync(), ghost: true);
            _continueBtn.style.display = DisplayStyle.None;
            _continueBtn.tooltip = "Resume from last Stop checkpoint (lab-style)";
            headActions.Add(_continueBtn);
            _activityBtn = Btn("?", ToggleActivityPanel, ghost: true);
            _activityBtn.AddToClassList("gf-icon-btn");
            _activityBtn.AddToClassList("gf-activity-btn");
            _activityBtn.style.display = DisplayStyle.None;
            _activityBtn.tooltip = "What's happening? (read-only — does not interrupt the run)";
            headActions.Add(_activityBtn);
            headActions.Add(Btn("Workbench", GameForgeWindow.Open, ghost: true));
            head.Add(headActions);
            root.Add(head);
            GameForgeSession.ApplyThemeClass(root);

            _activityPanel = new VisualElement();
            _activityPanel.AddToClassList("gf-activity-panel");
            _activityPanel.style.display = DisplayStyle.None;

            var activityHead = new VisualElement();
            activityHead.AddToClassList("gf-activity-head");
            var activityTitle = new Label("Agent activity");
            activityTitle.AddToClassList("gf-activity-title");
            activityHead.Add(activityTitle);
            var activityClose = Btn("×", CloseActivityPanel, ghost: true);
            activityClose.AddToClassList("gf-activity-close");
            activityClose.tooltip = "Close";
            activityHead.Add(activityClose);
            _activityPanel.Add(activityHead);

            _activityScroll = new ScrollView(ScrollViewMode.Vertical);
            _activityScroll.AddToClassList("gf-activity-scroll");
            _activityScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _activityScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _activityScroll.RegisterCallback<WheelEvent>(_ =>
            {
                _activityScrollY = _activityScroll.scrollOffset.y;
            });

            _activityBody = new Label("Loading…");
            _activityBody.AddToClassList("gf-activity-body");
            _activityScroll.Add(_activityBody);
            _activityPanel.Add(_activityScroll);
            root.Add(_activityPanel);

            var contextBlock = new VisualElement();
            contextBlock.AddToClassList("gf-chat-context-block");
            _contextBlock = contextBlock;
            _contextRow = new VisualElement();
            _contextRow.AddToClassList("gf-chat-context");
            _modePill = LabelCls("gf-chat-mode-pill", "Agent");
            _contextRow.Add(_modePill);
            _contextLabel = LabelCls("gf-chat-context-text", "");
            _contextRow.Add(_contextLabel);
            contextBlock.Add(_contextRow);

            // Run/LLM progress stays in Thinking bubble — do not duplicate under the context row.
            _runStatus = LabelCls("gf-run-status", "");
            _runStatus.style.display = DisplayStyle.None;
            contextBlock.Add(_runStatus);
            root.Add(contextBlock);

            _log = new ScrollView();
            _log.AddToClassList("gf-chat-log");
            _log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _log.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _emptyHint = LabelCls("gf-chat-empty", "Write a message to get started.");
            _log.Add(_emptyHint);
            root.Add(_log);

            // Sheet sits above the compose bar — never inside the pill
            _sheetHost = new VisualElement();
            _sheetHost.AddToClassList("gf-chat-sheet-host");
            root.Add(_sheetHost);

            _slashMenu = new GameForgeChatSlashMenu();
            _slashMenu.SkillChosen += OnSlashSkill;
            _sheetHost.Add(_slashMenu.Root);

            _confirmSheet = new GameForgeConfirmSheet();
            _confirmSheet.ReplySubmitted += msg => _ = SendMessageAsync(msg);
            _sheetHost.Add(_confirmSheet.Root);

            var form = new VisualElement();
            form.AddToClassList("gf-chat-form");
            form.AddToClassList("gf-chat-pill");

            _modePicker = new GameForgeChatModePicker();
            _modePicker.Root.tooltip = "Mode (Shift+Tab)";
            _modePicker.SelectionChanged += mode =>
            {
                GameForgeSession.ChatMode = mode;
                GameForgeSession.NotifyChanged();
                ApplyModeTheme(mode);
                RefreshContext();
            };
            form.Add(_modePicker.Root);

            var attachBtn = new Button(OnPickAttachment) { text = "📎" };
            attachBtn.AddToClassList("gf-chat-attach");
            attachBtn.tooltip = "Attach image (file pick). Or Ctrl+V paste screenshot / image, or drag onto chat.";
            form.Add(attachBtn);

            _input = new TextField { multiline = true };
            _input.AddToClassList("gf-chat-input");
            _input.value = "";
            _input.RegisterValueChangedCallback(evt =>
            {
                ResizeInputToContent();
                OnInputChanged(evt.newValue);
            });
            _input.RegisterCallback<GeometryChangedEvent>(_ => ResizeInputToContent());
            _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            form.Add(_input);
            ResizeInputToContent();

            _picker = new GameForgeModelPicker(null);
            _picker.Root.AddToClassList("mp-chat");
            _picker.SetSheetHost(_sheetHost);
            _picker.Root.tooltip = "Model";
            _picker.SelectionChanged += async (provider, model) =>
            {
                GameForgeSession.ProviderId = provider;
                GameForgeSession.ModelId = model;
                try
                {
                    await GameForgeSession.Bridge.PostJsonAsync(
                        "/api/agent/provider",
                        $"{{\"id\":\"{provider}\",\"model\":\"{model}\"}}");
                    _status.text = $"{provider} · {model}";
                }
                catch (Exception ex)
                {
                    _status.text = Trunc(ex.Message, 60);
                }

                RefreshContext();
            };
            form.Add(_picker.Root);

            _sendBtn = new Button(OnSendOrStop) { text = "↑" };
            _sendBtn.AddToClassList("gf-btn");
            _sendBtn.AddToClassList("gf-btn-primary");
            _sendBtn.AddToClassList("gf-chat-send");
            _sendBtn.tooltip = "Send";
            form.Add(_sendBtn);

            _attachStrip = new VisualElement();
            _attachStrip.AddToClassList("gf-chat-attach-strip");
            _attachStrip.style.display = DisplayStyle.None;
            root.Add(_attachStrip);

            root.Add(form);
            RegisterAttachDrop(root);
            RegisterAttachDrop(form);
            _status = LabelCls("gf-chat-status", "");
            root.Add(_status);
            ApplyDebugClass();

            root.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_picker != null && _picker.IsOpen)
                {
                    var inTrigger = _picker.Root.worldBound.Contains(evt.position);
                    var inSheet = _sheetHost.worldBound.Contains(evt.position);
                    if (!inTrigger && !inSheet) _picker.Close();
                }

                if (_modePicker != null && !_modePicker.Root.worldBound.Contains(evt.position))
                    _modePicker.Close();

                if (_slashMenu != null && _slashMenu.IsOpen &&
                    !_slashMenu.Root.worldBound.Contains(evt.position) &&
                    (_input == null || !_input.worldBound.Contains(evt.position)))
                    _slashMenu.Close();
            });
        }

        private void OnInputChanged(string value)
        {
            if (_slashMenu == null) return;
            if (string.IsNullOrEmpty(value))
            {
                _slashMenu.Close();
                return;
            }

            // Cursor-like: only when the line is a slash skill draft (leading /)
            var trimmed = value.TrimStart();
            if (trimmed.StartsWith("/", StringComparison.Ordinal))
                _slashMenu.UpdateFilter(trimmed.Split('\n')[0]);
            else
                _slashMenu.Close();
        }

        private void OnSlashSkill(string skillId)
        {
            if (skillId == "debugmode")
            {
                GameForgeSession.ToggleChatDebugMode();
                ClearInputField();
                ApplyDebugClass();
                RefreshContext();
                if (_status != null) _status.text = "";
                return;
            }

            // Agent-forwarded skills: fill compose and run immediately (Enter / click).
            SetInputValueScrubbed("/" + skillId);
            _slashMenu?.Close();
            if (!GameForgeSession.IsBusy)
                _ = SendAsync();
        }

        private bool TryHandleSlashCommand(string msg)
        {
            var cmd = msg.Trim().TrimStart('/').Split(' ', '\n')[0].ToLowerInvariant();
            if (cmd is "debugmode" or "debug")
            {
                OnSlashSkill("debugmode");
                return true;
            }

            // Forward known agent skills to the sidecar (do not treat as unknown).
            if (cmd is "prototype-full" or "game-setup" or "game-setup-run-all")
                return false;

            if (msg.StartsWith("/", StringComparison.Ordinal))
            {
                Append("notice", $"Unknown skill /{cmd}. Type / to list skills.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set input text and strip leftover Enter newlines that UI Toolkit may insert
        /// after KeyDown (same race as Send / slash confirm).
        /// </summary>
        private void SetInputValueScrubbed(string value)
        {
            SetInputValueInternal(value, singleLineOnly: true);
        }

        /// <summary>Multi-line prefill (console log handoff) — keeps full body, not first line only.</summary>
        private void SetInputValueMultiline(string value)
        {
            SetInputValueInternal(value, singleLineOnly: false);
        }

        private void SetInputValueInternal(string value, bool singleLineOnly)
        {
            if (_input == null) return;
            var wanted = value ?? "";
            _input.SetValueWithoutNotify(wanted);
            ResizeInputToContent();
            void Scrub()
            {
                if (_input == null) return;
                var v = _input.value ?? "";
                v = v.Replace("\r\n", "\n").Replace('\r', '\n');
                if (singleLineOnly && v.IndexOf('\n') >= 0)
                    v = v.Split('\n')[0];
                else
                    v = v.TrimEnd('\n');
                // Enter alone after clear → only whitespace / newline
                if (string.IsNullOrWhiteSpace(v) && string.IsNullOrEmpty(wanted.Trim()))
                    v = "";
                if (v == (_input.value ?? "")) return;
                _input.SetValueWithoutNotify(v);
                ResizeInputToContent();
            }

            _input.schedule.Execute(Scrub).StartingIn(1);
            _input.schedule.Execute(Scrub).StartingIn(30);
            _input.schedule.Execute(Scrub).StartingIn(80);
        }

        private void ApplyDebugClass()
        {
            _root?.EnableInClassList("gf-chat-debug", GameForgeSession.ChatDebugMode);
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                if (_confirmSheet != null && _confirmSheet.IsOpen)
                {
                    _confirmSheet.Close();
                    evt.StopImmediatePropagation();
                    return;
                }

                _picker?.Close();
                _modePicker?.Close();
                _slashMenu?.Close();
                evt.StopImmediatePropagation();
                return;
            }

            if (evt.keyCode != KeyCode.Tab || !evt.shiftKey) return;
            evt.StopImmediatePropagation();
            if (GameForgeSession.IsBusy) return;
            CycleChatMode();
        }

        private void CycleChatMode()
        {
            if (GameForgeSession.IsBusy) return;
            _picker?.Close();
            _modePicker?.Close();

            var cur = GameForgeSession.ChatMode;
            if (cur is not ("agent" or "ask")) cur = "agent";
            var idx = Array.IndexOf(ModeCycle, cur);
            if (idx < 0) idx = 0;
            var next = ModeCycle[(idx + 1) % ModeCycle.Length];
            GameForgeSession.ChatMode = next;
            _modePicker?.SetMode(next, notify: false);
            GameForgeSession.NotifyChanged();
            ApplyModeTheme(next);
            RefreshContext();
            Repaint();
        }

        private void ApplyModeTheme(string mode)
        {
            if (_root == null) return;
            _root.EnableInClassList("gf-chat-ask", mode == "ask");
            _root.EnableInClassList("gf-chat-plan", false);
            _root.EnableInClassList("gf-chat-agent", mode != "ask");
        }

        private void RefreshContext()
        {
            if (_contextLabel == null) return;
            ApplyDebugClass();
            GameForgeSession.ApplyThemeClass(_root);
            UpdateThemeButtonLabel();

            var mode = LabelFor(GameForgeSession.ChatMode);
            if (_modePill != null) _modePill.text = mode;

            var slug = string.IsNullOrEmpty(GameForgeSession.SelectedSlug) ? "No TDD" : GameForgeSession.SelectedSlug;
            var debug = GameForgeSession.ChatDebugMode ? " · debug" : "";
            _contextLabel.text =
                $"{slug} · {GameForgeSession.ModeLabel(GameForgeSession.Mode)}{debug}";
            _modePicker?.SetMode(GameForgeSession.ChatMode, notify: false);
            ApplyModeTheme(GameForgeSession.ChatMode);
            Repaint();
        }

        private void ToggleTheme()
        {
            GameForgeSession.ToggleUiTheme();
            UpdateThemeButtonLabel();
        }

        private void UpdateThemeButtonLabel()
        {
            if (_root == null) return;
            var b = _root.Q<Button>(className: "gf-theme-toggle");
            if (b == null) return;
            b.text = ThemeButtonLabel();
            b.tooltip = GameForgeSession.IsDarkTheme ? "Switch to light theme" : "Switch to dark theme";
        }

        private Button MakeThemeBtn()
        {
            var b = Btn(ThemeButtonLabel(), ToggleTheme, ghost: true);
            b.AddToClassList("gf-theme-toggle");
            b.tooltip = GameForgeSession.IsDarkTheme ? "Switch to light theme" : "Switch to dark theme";
            return b;
        }

        private static string ThemeButtonLabel() =>
            GameForgeSession.IsDarkTheme ? "☀" : "☾";

        private void ResizeInputToContent()
        {
            if (_input == null) return;
            var text = _input.value ?? "";
            var hardLines = Math.Max(1, text.Split('\n').Length);
            var width = _input.resolvedStyle.width;
            if (float.IsNaN(width) || width < 40f) width = 220f;
            var charsPerLine = Math.Max(12, (int)(width / 7.2f));
            var softLines = 0;
            foreach (var line in text.Split('\n'))
                softLines += Math.Max(1, (line.Length + charsPerLine - 1) / charsPerLine);
            if (softLines < 1) softLines = 1;

            var lines = Math.Max(hardLines, softLines);
            var height = Math.Min(InputMaxPx, Math.Max(InputMinPx, lines * InputLinePx + InputPadPx));
            _input.style.height = height;
            _input.style.minHeight = height;

            // Single-line: center with Agent / model / send (Cursor-style).
            // Multi-line: pin controls to the bottom of the growing field.
            var form = _input.parent;
            form?.EnableInClassList("gf-chat-pill-multiline", height > InputMinPx + 2);
        }

        private async System.Threading.Tasks.Task RefreshProvidersAsync()
        {
            try
            {
                var json = await GameForgeSession.Bridge.GetAsync("/api/agent/providers");
                var catalog = GameForgeModelPicker.ParseStatusJson(json);
                _picker?.SetCatalog(catalog);
                if (!string.IsNullOrEmpty(catalog.active)) GameForgeSession.ProviderId = catalog.active;
                if (!string.IsNullOrEmpty(catalog.model)) GameForgeSession.ModelId = catalog.model;
                RefreshContext();
                if (_status != null) _status.text = "";
            }
            catch (Exception ex)
            {
                _picker?.SetCatalog(new ProviderCatalogState
                {
                    configured = false,
                    missingHint =
                        "Agent offline — open Workbench and click Bootstrap Agent. " + Trunc(ex.Message, 100)
                });
                if (_status != null) _status.text = Trunc(ex.Message, 80);
            }
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (_slashMenu != null && _slashMenu.IsOpen)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    SuppressTextFieldKeyDefault(evt);
                    _slashMenu.Close();
                    return;
                }

                if (evt.keyCode == KeyCode.DownArrow)
                {
                    SuppressTextFieldKeyDefault(evt);
                    _slashMenu.MoveSelection(+1);
                    return;
                }

                if (evt.keyCode == KeyCode.UpArrow)
                {
                    SuppressTextFieldKeyDefault(evt);
                    _slashMenu.MoveSelection(-1);
                    return;
                }

                // Enter or Tab confirms highlight (Shift+Tab still cycles chat mode)
                var confirmEnter = !evt.shiftKey
                    && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
                var confirmTab = evt.keyCode == KeyCode.Tab && !evt.shiftKey;
                if ((confirmEnter || confirmTab) && _slashMenu.VisibleCount > 0)
                {
                    SuppressTextFieldKeyDefault(evt);
                    if (!_slashMenu.HasSelection)
                        _slashMenu.MoveSelection(+1);
                    _slashMenu.ConfirmSelection();
                    // Enter may still inject '\n' after this handler — scrub once more.
                    _input?.schedule.Execute(() =>
                    {
                        if (_input == null) return;
                        var v = (_input.value ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
                        if (v.IndexOf('\n') < 0 && !string.IsNullOrWhiteSpace(v)) return;
                        if (v.IndexOf('\n') >= 0) v = v.Split('\n')[0];
                        if (string.IsNullOrWhiteSpace(v)) v = "";
                        _input.SetValueWithoutNotify(v);
                        ResizeInputToContent();
                    }).StartingIn(1);
                    return;
                }
            }

            if (evt.keyCode == KeyCode.Tab && evt.shiftKey)
            {
                SuppressTextFieldKeyDefault(evt);
                if (!GameForgeSession.IsBusy) CycleChatMode();
                return;
            }

            // Ctrl/Cmd+V: attach clipboard image (bitmap) or image file path — Cursor-like
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.V)
            {
                if (TryAttachFromClipboard())
                {
                    SuppressTextFieldKeyDefault(evt);
                    return;
                }
            }

            // Enter sends · Shift+Enter newline (Cursor-like)
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
            if (evt.shiftKey) return;

            // Stop TextField from inserting a leftover '\n' after we clear.
            SuppressTextFieldKeyDefault(evt);
            if (GameForgeSession.IsBusy) return;
            _ = SendAsync();
        }

        /// <summary>
        /// UI Toolkit: stop propagation + IgnoreEvent (replaces obsolete PreventDefault).
        /// </summary>
        private static void SuppressTextFieldKeyDefault(KeyDownEvent evt)
        {
            evt.StopImmediatePropagation();
            var ve = evt.target as VisualElement ?? evt.currentTarget as VisualElement;
            ve?.focusController?.IgnoreEvent(evt);
        }

        private void ClearInputField()
        {
            if (_input == null) return;
            _input.SetValueWithoutNotify("");
            ResizeInputToContent();
            // TextField may still apply Enter after KeyDown — scrub on next frames.
            _input.schedule.Execute(() =>
            {
                if (_input == null) return;
                var v = _input.value ?? "";
                if (v.Length == 0) return;
                if (string.IsNullOrWhiteSpace(v) || v == "\n" || v == "\r\n")
                {
                    _input.SetValueWithoutNotify("");
                    ResizeInputToContent();
                }
            }).StartingIn(1);
            _input.schedule.Execute(() =>
            {
                if (_input == null) return;
                var v = _input.value ?? "";
                if (v == "\n" || v == "\r\n" || (v.Length <= 2 && string.IsNullOrWhiteSpace(v)))
                {
                    _input.SetValueWithoutNotify("");
                    ResizeInputToContent();
                }
            }).StartingIn(30);
        }

        private async System.Threading.Tasks.Task SendAsync()
        {
            if (GameForgeSession.IsBusy || _runUiActive || _thinkingTimerActive)
            {
                Append("notice", "A run is already in progress — click ■ to stop.");
                return;
            }

            var msg = _input?.value?.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            _slashMenu?.Close();
            _confirmSheet?.Close();
            ClearInputField();

            // Local slash skills (never hit the agent)
            if (TryHandleSlashCommand(msg))
                return;

            await SendMessageAsync(msg);
        }

        private async System.Threading.Tasks.Task SendMessageAsync(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            if (GameForgeSession.IsBusy || _runUiActive || _thinkingTimerActive)
            {
                Append("notice", "A run is already in progress — click ■ to stop.");
                return;
            }

            msg = msg.Trim();
            _confirmSheet?.Close();
            _picker?.Close();
            _modePicker?.Close();

            Append("you", msg);
            _status.text = "…";
            ClearStopGuardsForNewRun();
            BeginThinkingRun("Agent working…");
            ShowRunBanner("Agent working…", Trunc(msg, 72));
            RefreshBusyUi();

            try
            {
                var slug = GameForgeSession.SelectedSlug;
                if (string.IsNullOrEmpty(slug))
                {
                    Append("error", "Select a TDD in the Workbench first.");
                    FinalizeThinking(cancelled: true);
                    HideRunBanner();
                    _status.text = "";
                    return;
                }

                var chatMode = GameForgeSession.ChatMode;
                if (chatMode is not ("agent" or "ask")) chatMode = "agent";
                // /prototype-full and /game-setup-run-all must write — force Agent.
                if ((msg.TrimStart().StartsWith("/prototype-full", StringComparison.OrdinalIgnoreCase)
                     || msg.TrimStart().StartsWith("/game-setup", StringComparison.OrdinalIgnoreCase))
                    && chatMode != "agent")
                {
                    chatMode = "agent";
                    GameForgeSession.ChatMode = "agent";
                    _modePicker?.SetMode("agent", notify: false);
                    ApplyModeTheme("agent");
                    Append("notice", "Switched to Agent mode for production/prototype skill.");
                }

                var trimmedMsg = msg.TrimStart();
                if (trimmedMsg.StartsWith("/game-setup-run-all", StringComparison.OrdinalIgnoreCase))
                {
                    GameForgeSession.Mode = GameForgeMode.Production;
                    GameForgeSession.GameSetupRunAll = true;
                    GameForgePipelineState.TrackSetupCreates = true;
                    GameForgeProjectIdentity.SyncPlayerSettingsFromSlug(slug);
                }
                else if (trimmedMsg.StartsWith("/game-setup", StringComparison.OrdinalIgnoreCase))
                {
                    GameForgeSession.Mode = GameForgeMode.Production;
                    GameForgeSession.GameSetupRunAll = false;
                    GameForgePipelineState.TrackSetupCreates = true;
                    GameForgeProjectIdentity.SyncPlayerSettingsFromSlug(slug);
                }
                else if (trimmedMsg.StartsWith("/prototype-full", StringComparison.OrdinalIgnoreCase)
                         || trimmedMsg.StartsWith("/prototype", StringComparison.OrdinalIgnoreCase))
                {
                    GameForgeSession.Mode = GameForgeMode.Prototype;
                    GameForgePipelineState.TrackSetupCreates = false;
                }
                else if (trimmedMsg.StartsWith("/vertical-slice", StringComparison.OrdinalIgnoreCase))
                    GameForgeSession.Mode = GameForgeMode.VerticalSlice;
                else if (trimmedMsg.Equals("continue", StringComparison.OrdinalIgnoreCase)
                         && GameForgeSession.Mode == GameForgeMode.Production)
                    GameForgePipelineState.TrackSetupCreates = true;

                var escaped = EscapeJson(msg);
                var skillId = "";
                if (trimmedMsg.StartsWith("/game-setup-run-all", StringComparison.OrdinalIgnoreCase))
                    skillId = "game-setup-run-all";
                else if (trimmedMsg.StartsWith("/game-setup", StringComparison.OrdinalIgnoreCase))
                    skillId = "game-setup";
                else if (trimmedMsg.StartsWith("/prototype-full", StringComparison.OrdinalIgnoreCase))
                    skillId = "prototype-full";
                var force = string.IsNullOrEmpty(skillId) ? "" : $",\"forceChatSkillId\":\"{skillId}\"";
                var attachJson = BuildAttachmentIdsJson();
                var body =
                    $"{{\"slug\":\"{EscapeJson(slug)}\",\"message\":\"{escaped}\",\"chatMode\":\"{chatMode}\",\"forgeMode\":\"{EscapeJson(GameForgeSession.ModeLabel(GameForgeSession.Mode))}\"{force}{attachJson}}}";

                await GameForgeSession.Bridge.KickOffPostSseAsync(
                    "/api/sessions/chat",
                    body,
                    default);
                ClearPendingAttachments();
            }
            catch (OperationCanceledException)
            {
                Append("notice", "Run cancelled.");
                FinalizeThinking(cancelled: true);
                _status.text = "";
            }
            catch (Exception ex)
            {
                Append("error", ex.Message);
                FinalizeThinking(cancelled: true);
                _status.text = "";
            }
            finally
            {
                await GameForgeSession.RefreshRemoteBusyAsync();
                if (!GameForgeSession.IsBusy)
                {
                    if (_thinkingBox != null) FinalizeThinking(cancelled: false);
                    HideRunBanner();
                }
                RefreshBusyUi();
            }
        }

        private void ClearLog()
        {
            SharedEntries.Clear();
            SavePersisted();
            _thinkingBox = null;
            _thinkingHead = null;
            _thinkingBody = null;
            _latestAssistantText = "";
            _log?.Clear();
            if (_emptyHint != null) _log?.Add(_emptyHint);
            if (_status != null) _status.text = "";
            Repaint();
        }

        private void RestorePersistedLog()
        {
            EnsurePersistedLoaded();
            if (SharedEntries.Count == 0 || _log == null) return;

            if (_emptyHint != null && _emptyHint.parent == _log)
                _emptyHint.RemoveFromHierarchy();

            foreach (var entry in SharedEntries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.role)) continue;
                if (entry.role == "thinking")
                {
                    // Done/Finished noise — only restore in /debugmode; keep Stopped visible
                    var head = entry.head ?? "";
                    if (head.Equals("Done", StringComparison.OrdinalIgnoreCase) &&
                        !GameForgeSession.ChatDebugMode)
                        continue;
                    RenderThinkingEntry(head.Length > 0 ? head : "Done", entry.text ?? "", done: true);
                }
                else
                    RenderMessage(entry.role, entry.text ?? "", scroll: false);
            }

            _log.schedule.Execute(() =>
            {
                if (_log != null) _log.scrollOffset = new Vector2(0, float.MaxValue);
            });
        }

        private static void EnsurePersistedLoaded()
        {
            if (_persistedLoaded) return;
            _persistedLoaded = true;
            SharedEntries.Clear();
            var raw = SessionState.GetString(ChatLogSessionKey, "");
            if (string.IsNullOrEmpty(raw)) return;
            try
            {
                var dto = JsonUtility.FromJson<PersistedChatLog>(raw);
                if (dto?.entries == null) return;
                foreach (var e in dto.entries)
                {
                    if (e != null) SharedEntries.Add(e);
                }
            }
            catch
            {
                SharedEntries.Clear();
            }
        }

        private static void SavePersisted()
        {
            EnsurePersistedLoaded();
            while (SharedEntries.Count > MaxPersistedMessages)
                SharedEntries.RemoveAt(0);
            var dto = new PersistedChatLog { entries = SharedEntries.ToArray() };
            SessionState.SetString(ChatLogSessionKey, JsonUtility.ToJson(dto));
        }

        private static void PersistEntry(string role, string text, string head = null)
        {
            EnsurePersistedLoaded();
            SharedEntries.Add(new PersistedChatEntry
            {
                role = role ?? "system",
                text = text ?? "",
                head = head ?? ""
            });
            GameForgePipelineState.Ingest(text);
            if (!string.IsNullOrEmpty(head))
                GameForgePipelineState.Ingest(head);
            SavePersisted();
        }

        private void Append(string role, string text)
        {
            PersistEntry(role, text);
            if (string.Equals(role, "you", StringComparison.OrdinalIgnoreCase))
                GameForgePipelineState.NoteUserReply(text);
            RenderMessage(role, text, scroll: true);
        }

        private void RenderThinkingEntry(string head, string body, bool done)
        {
            if (_log == null) return;
            if (_emptyHint != null && _emptyHint.parent == _log)
                _emptyHint.RemoveFromHierarchy();

            var box = new VisualElement();
            box.AddToClassList("gf-msg");
            box.AddToClassList("gf-msg-thinking");
            if (head.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
                box.AddToClassList("is-stopped");
            else if (done)
                box.AddToClassList("is-done");
            var mark = new VisualElement();
            mark.AddToClassList("gf-msg-mark");
            mark.AddToClassList("gf-msg-mark-think");
            var col = new VisualElement();
            col.AddToClassList("gf-msg-col");
            var headLabel = LabelCls("gf-msg-role", head);
            var bodyLabel = LabelCls("gf-msg-body", body);
            bodyLabel.AddToClassList("gf-msg-thinking-body");
            col.Add(headLabel);
            col.Add(bodyLabel);
            box.Add(mark);
            box.Add(col);
            _log.Add(box);
        }

        private void RenderMessage(string role, string text, bool scroll)
        {
            if (_log == null) return;

            if (_emptyHint != null && _emptyHint.parent == _log)
                _emptyHint.RemoveFromHierarchy();

            var box = new VisualElement();
            box.AddToClassList("gf-msg");
            if (role == "you") box.AddToClassList("gf-msg-you");
            else if (role is "agent" or "sync") box.AddToClassList("gf-msg-agent");
            else if (role == "error") box.AddToClassList("gf-msg-error");
            else if (role == "notice") box.AddToClassList("gf-msg-notice");
            else box.AddToClassList("gf-msg-system");

            var mark = new VisualElement();
            mark.AddToClassList("gf-msg-mark");
            if (role == "you") mark.AddToClassList("gf-msg-mark-you");
            else if (role == "agent") mark.AddToClassList("gf-msg-mark-agent");
            else if (role == "error") mark.AddToClassList("gf-msg-mark-error");
            else if (role == "notice") mark.AddToClassList("gf-msg-mark-notice");
            else mark.AddToClassList("gf-msg-mark-sys");

            var col = new VisualElement();
            col.AddToClassList("gf-msg-col");

            var head = new VisualElement();
            head.AddToClassList("gf-msg-head");
            var roleLabel = role switch
            {
                "notice" => "CHAT",
                "error" => "ERROR",
                _ => role.ToUpperInvariant()
            };
            head.Add(LabelCls("gf-msg-role", roleLabel));
            Button copy = null;
            copy = new Button(() => CopyBubble(text, copy)) { text = "📋" };
            copy.AddToClassList("gf-msg-copy");
            copy.tooltip = "Copy message";
            head.Add(copy);
            col.Add(head);
            col.Add(LabelCls("gf-msg-body", text));

            box.Add(mark);
            box.Add(col);
            _log.Add(box);
            if (_runUiActive && _thinkingBox != null && _thinkingBox.parent != null)
                PinThinkingBubble();
            if (scroll)
                ScheduleLogScroll(box, role);
            Repaint();
        }

        /// <summary>Long agent replies start at the top; other roles stick to the log bottom.</summary>
        private void ScheduleLogScroll(VisualElement box, string role)
        {
            _log.schedule.Execute(() =>
            {
                if (_log == null || box.parent == null) return;
                if (role is "agent" or "sync")
                    _log.ScrollTo(box);
                else
                    _log.scrollOffset = new Vector2(0, float.MaxValue);
            });
        }

        private static void CopyBubble(string text, Button btn)
        {
            EditorGUIUtility.systemCopyBuffer = text ?? "";
            if (btn == null) return;
            var prev = btn.text;
            btn.text = "✓";
            btn.schedule.Execute(() => btn.text = prev).ExecuteLater(900);
        }

        private static string LabelFor(string mode) => mode switch
        {
            "ask" => "Ask",
            _ => "Agent"
        };

        private string BuildAttachmentIdsJson()
        {
            if (_pendingAttachments.Count == 0) return "";
            var sb = new System.Text.StringBuilder(",\"attachmentIds\":[");
            for (var i = 0; i < _pendingAttachments.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(EscapeJson(_pendingAttachments[i].Id)).Append('"');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private void OnPickAttachment()
        {
            if (GameForgeSession.IsBusy) return;
            try
            {
                var path = EditorUtility.OpenFilePanel("Attach gameplay capture", "", "png,jpg,jpeg,webp");
                if (string.IsNullOrEmpty(path)) return;
                AttachPathSync(path);
            }
            catch (Exception ex)
            {
                ReportAttachError(ex);
            }
        }

        private void RegisterAttachDrop(VisualElement el)
        {
            if (el == null) return;
            el.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0) return;
                foreach (var p in DragAndDrop.paths)
                {
                    if (IsImagePath(p))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.StopPropagation();
                        return;
                    }
                }
            });
            el.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.paths == null) return;
                foreach (var p in DragAndDrop.paths)
                {
                    if (IsImagePath(p)) AttachPathSync(p);
                }
                DragAndDrop.AcceptDrag();
                evt.StopPropagation();
            });
        }

        private static bool IsImagePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
            try
            {
                var e = Path.GetExtension(path).ToLowerInvariant();
                return e is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp";
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// True only for clipboard contents that look like a single image file path
        /// (not chat text — which used to throw on Path.GetExtension and block Ctrl+V).
        /// </summary>
        private static bool LooksLikeImageFilePath(string clip)
        {
            if (string.IsNullOrWhiteSpace(clip)) return false;
            // Chat paste: multi-line or long prose must never enter path APIs
            if (clip.IndexOf('\n') >= 0 || clip.IndexOf('\r') >= 0) return false;
            if (clip.Length > 512) return false;
            if (clip.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;

            var path = clip.Trim();
            if (path.Length >= 2 && path[0] == '"' && path[^1] == '"')
                path = path.Substring(1, path.Length - 2).Trim();
            if (string.IsNullOrEmpty(path)) return false;
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;

            // Require a real path shape (drive, UNC, or separator) — not free-form sentences
            var hasSep = path.IndexOf('\\') >= 0 || path.IndexOf('/') >= 0;
            var isDrive = path.Length >= 3
                && char.IsLetter(path[0])
                && path[1] == ':'
                && (path[2] == '\\' || path[2] == '/');
            var isUnc = path.StartsWith(@"\\", StringComparison.Ordinal);
            if (!hasSep && !isDrive && !isUnc) return false;

            return IsImagePath(path);
        }

        private bool TryAttachFromClipboard()
        {
            // 1) Real clipboard image (PNG/DIB/HDROP) — only then suppress default paste
            try
            {
                if (GameForgeClipboardImage.TryGetImage(out var bytes, out var name)
                    && bytes != null && bytes.Length > 0)
                {
                    _ = UploadAttachmentAsync(bytes, name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ReportAttachError(ex);
                // Do not suppress TextField paste — user may have text + stale image format
                return false;
            }

            // 2) Copied image file path only (never treat chat sentences as paths)
            try
            {
                var clip = EditorGUIUtility.systemCopyBuffer?.Trim() ?? "";
                if (!LooksLikeImageFilePath(clip)) return false;
                if (clip.Length >= 2 && clip[0] == '"' && clip[^1] == '"')
                    clip = clip.Substring(1, clip.Length - 2);
                if (!File.Exists(clip)) return false;
                AttachPathSync(clip);
                return true;
            }
            catch
            {
                // Plain text paste must always fall through to the TextField
                return false;
            }
        }

        /// <summary>Read file on UI thread then upload (no Texture2D required for typical PNG/JPEG).</summary>
        private void AttachPathSync(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    SafeAppend("error", "[attachment] File not found.");
                    return;
                }
                var bytes = File.ReadAllBytes(path);
                _ = UploadAttachmentAsync(bytes, Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                ReportAttachError(ex);
            }
        }

        private async System.Threading.Tasks.Task UploadAttachmentAsync(byte[] rawBytes, string fileName)
        {
            try
            {
                if (_pendingAttachments.Count >= MaxAttachments)
                {
                    SafeAppend("error", $"[attachment] Max {MaxAttachments} images per message. Remove one first.");
                    return;
                }
                if (rawBytes == null || rawBytes.Length == 0)
                {
                    SafeAppend("error", "[attachment] Empty image.");
                    return;
                }

                if (!PrepareUploadPayload(rawBytes, fileName, out var payload, out var mime, out var uploadName, out var err))
                {
                    SafeAppend("error", err);
                    return;
                }

                var bridge = GameForgeSession.Bridge;
                if (bridge == null)
                {
                    SafeAppend("error", "[attachment] Agent bridge not ready — Bootstrap Agent.");
                    return;
                }

                string body;
                try
                {
                    // Prefer JSON+base64 — Unity's MultipartFormDataContent is a known NRE source.
                    var b64 = Convert.ToBase64String(payload);
                    var json =
                        $"{{\"base64\":\"{b64}\",\"mime\":\"{EscapeJson(mime)}\",\"fileName\":\"{EscapeJson(uploadName)}\"}}";
                    body = await bridge.PostJsonAsync("/api/attachments/json", json);
                }
                catch (Exception ex)
                {
                    ReportAttachError(ex);
                    return;
                }

                // Back on whatever thread — marshal UI to Editor delayCall
                var capturedBody = body;
                var capturedName = uploadName;
                var capturedKb = payload.Length / 1024;
                var previewBytes = payload; // keep for thumbnail + lightbox
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        if (!this) return;
                        var id = JsonField(capturedBody ?? "", "id");
                        if (string.IsNullOrEmpty(id))
                        {
                            SafeAppend("error", "[attachment] Upload failed: " + Trunc(capturedBody ?? "", 160));
                            return;
                        }
                        var thumb = TryMakeThumb(previewBytes, 72);
                        AddAttachmentChip(id, capturedName, thumb, previewBytes);
                        SafeAppend("notice", $"Attached {capturedName} ({capturedKb} KB).");
                    }
                    catch (Exception ex)
                    {
                        ReportAttachError(ex);
                    }
                };
            }
            catch (Exception ex)
            {
                ReportAttachError(ex);
            }
        }

        /// <summary>
        /// Prefer raw PNG/JPEG under the size cap (no Unity texture APIs).
        /// Only downscales via CPU textures when the file is over the limit.
        /// </summary>
        private static bool PrepareUploadPayload(
            byte[] raw,
            string fileName,
            out byte[] payload,
            out string mime,
            out string uploadName,
            out string error)
        {
            payload = null;
            mime = "image/jpeg";
            uploadName = "capture.jpg";
            error = null;

            var baseName = string.IsNullOrEmpty(fileName)
                ? "capture"
                : Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(baseName)) baseName = "capture";

            if (LooksLikeImage(raw, out mime))
            {
                if (raw.Length <= MaxAttachBytes)
                {
                    payload = raw;
                    var ext = mime switch
                    {
                        "image/png" => ".png",
                        "image/webp" => ".webp",
                        "image/gif" => ".gif",
                        _ => ".jpg"
                    };
                    uploadName = baseName + ext;
                    return true;
                }

                // Oversize: try CPU shrink to JPEG
                try
                {
                    var jpg = DownscaleToJpegCpu(raw, 1280, 72);
                    if (jpg != null && jpg.Length > 0 && jpg.Length <= MaxAttachBytes)
                    {
                        payload = jpg;
                        mime = "image/jpeg";
                        uploadName = baseName + ".jpg";
                        return true;
                    }
                    error =
                        $"[attachment] Image too large ({raw.Length / 1024} KB). Max {MaxAttachBytes / 1024} KB — use a smaller PNG/JPEG.";
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    error = "[attachment] Could not downscale image: " + Trunc(ex.Message, 160);
                    return false;
                }
            }

            error = "[attachment] Unsupported image. Use PNG, JPEG, WebP, or GIF.";
            return false;
        }

        private void SafeAppend(string role, string text)
        {
            try
            {
                if (!this) return;
                Append(role, text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] Append failed: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        private void ReportAttachError(Exception ex)
        {
            Debug.LogException(ex);
            var type = ex?.GetType().Name ?? "Error";
            var msg = ex?.Message ?? "Unknown error";
            var stack = ex?.StackTrace ?? "";
            var frame = "";
            if (!string.IsNullOrEmpty(stack))
            {
                var line = stack.Split('\n')[0].Trim();
                if (line.Length > 0) frame = "\n" + Trunc(line, 160);
            }
            SafeAppend("error",
                $"[attachment] {type}: {Trunc(msg, 180)}{frame}\n→ Try a smaller PNG/JPEG, or Bootstrap Agent and retry.");
        }

        private static bool LooksLikeImage(byte[] raw, out string mime)
        {
            mime = "application/octet-stream";
            if (raw == null || raw.Length < 12) return false;
            if (raw[0] == 0x89 && raw[1] == 0x50 && raw[2] == 0x4E && raw[3] == 0x47)
            {
                mime = "image/png";
                return true;
            }
            if (raw[0] == 0xFF && raw[1] == 0xD8)
            {
                mime = "image/jpeg";
                return true;
            }
            if (raw[0] == 0x47 && raw[1] == 0x49 && raw[2] == 0x46)
            {
                mime = "image/gif";
                return true;
            }
            if (raw[0] == 0x52 && raw[1] == 0x49 && raw[2] == 0x46 && raw[3] == 0x46
                && raw[8] == 0x57 && raw[9] == 0x45 && raw[10] == 0x42 && raw[11] == 0x50)
            {
                mime = "image/webp";
                return true;
            }
            // BMP
            if (raw[0] == 0x42 && raw[1] == 0x4D)
            {
                mime = "image/bmp";
                return true;
            }
            return false;
        }

        /// <summary>Last-resort CPU downscale when the raw file exceeds the upload cap.</summary>
        private static byte[] DownscaleToJpegCpu(byte[] raw, int maxDim, int quality)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(tex, raw, false))
                    return null;
                var w = tex.width;
                var h = tex.height;
                if (w < 1 || h < 1) return null;

                Texture2D work = tex;
                Texture2D scaled = null;
                if (w > maxDim || h > maxDim)
                {
                    var scale = Mathf.Min((float)maxDim / w, (float)maxDim / h);
                    var nw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
                    var nh = Mathf.Max(1, Mathf.RoundToInt(h * scale));
                    scaled = ScaleTextureCpu(tex, nw, nh);
                    if (scaled != null) work = scaled;
                }

                var jpg = ImageConversion.EncodeToJPG(work, quality);
                if (scaled != null) UnityEngine.Object.DestroyImmediate(scaled);
                return jpg;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        private static Texture2D ScaleTextureCpu(Texture2D src, int nw, int nh)
        {
            if (src == null || nw < 1 || nh < 1) return null;
            Color32[] srcPixels;
            try { srcPixels = src.GetPixels32(); }
            catch { return null; }
            var sw = src.width;
            var sh = src.height;
            if (srcPixels == null || sw < 1 || sh < 1 || srcPixels.Length < sw * sh) return null;

            var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
            var dstPixels = new Color32[nw * nh];
            for (var y = 0; y < nh; y++)
            {
                var sy = Mathf.Clamp((int)((y + 0.5f) / nh * sh), 0, sh - 1);
                for (var x = 0; x < nw; x++)
                {
                    var sx = Mathf.Clamp((int)((x + 0.5f) / nw * sw), 0, sw - 1);
                    dstPixels[y * nw + x] = srcPixels[sy * sw + sx];
                }
            }
            dst.SetPixels32(dstPixels);
            dst.Apply(false, false);
            return dst;
        }

        private void AddAttachmentChip(string id, string fileName, Texture2D thumb, byte[] previewBytes = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_attachStrip == null)
            {
                _pendingAttachments.Add(new PendingAttachment
                {
                    Id = id,
                    FileName = fileName,
                    Thumb = thumb,
                    PreviewBytes = previewBytes
                });
                return;
            }

            var chip = new VisualElement();
            chip.AddToClassList("gf-attach-chip");

            if (thumb != null)
            {
                var img = new Image { image = thumb, scaleMode = ScaleMode.ScaleAndCrop };
                img.AddToClassList("gf-attach-thumb");
                img.tooltip = "Click to preview";
                img.RegisterCallback<ClickEvent>(_ => ShowAttachmentPreview(previewBytes, fileName, thumb));
                chip.Add(img);
            }
            else
            {
                var ph = new VisualElement();
                ph.AddToClassList("gf-attach-thumb");
                ph.AddToClassList("gf-attach-thumb-placeholder");
                ph.tooltip = "Image attached";
                if (previewBytes != null && previewBytes.Length > 0)
                    ph.RegisterCallback<ClickEvent>(_ => ShowAttachmentPreview(previewBytes, fileName, null));
                chip.Add(ph);
            }

            var label = new Label(Trunc(fileName ?? "image", 18));
            label.AddToClassList("gf-attach-name");
            chip.Add(label);
            var rem = new Button(() => RemovePendingAttachment(id)) { text = "×" };
            rem.AddToClassList("gf-attach-remove");
            rem.tooltip = "Remove";
            chip.Add(rem);
            _attachStrip.Add(chip);
            _attachStrip.style.display = DisplayStyle.Flex;
            _pendingAttachments.Add(new PendingAttachment
            {
                Id = id,
                FileName = fileName,
                Thumb = thumb,
                PreviewBytes = previewBytes,
                Chip = chip
            });
        }

        /// <summary>Small CPU thumb for the chip — never throws; returns null on failure.</summary>
        private static Texture2D TryMakeThumb(byte[] bytes, int maxEdge)
        {
            if (bytes == null || bytes.Length == 0 || maxEdge < 8) return null;
            Texture2D src = null;
            try
            {
                src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(src, bytes, false))
                    return null;
                var w = src.width;
                var h = src.height;
                if (w < 1 || h < 1) return null;
                var scale = Mathf.Min((float)maxEdge / w, (float)maxEdge / h, 1f);
                var nw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
                var nh = Mathf.Max(1, Mathf.RoundToInt(h * scale));
                if (nw == w && nh == h)
                {
                    // Own a copy so DestroyImmediate(src) in finally doesn't wipe the chip image.
                    var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    copy.SetPixels32(src.GetPixels32());
                    copy.Apply(false, false);
                    return copy;
                }
                return ScaleTextureCpu(src, nw, nh);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] Thumb failed: " + ex.Message);
                return null;
            }
            finally
            {
                if (src != null) UnityEngine.Object.DestroyImmediate(src);
            }
        }

        private void ShowAttachmentPreview(byte[] bytes, string fileName, Texture2D fallbackThumb)
        {
            if (_root == null) return;
            Texture2D full = null;
            try
            {
                if (bytes != null && bytes.Length > 0)
                {
                    full = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(full, bytes, false))
                    {
                        UnityEngine.Object.DestroyImmediate(full);
                        full = null;
                    }
                }
            }
            catch
            {
                if (full != null)
                {
                    UnityEngine.Object.DestroyImmediate(full);
                    full = null;
                }
            }

            var tex = full != null ? full : fallbackThumb;
            if (tex == null)
            {
                SafeAppend("notice", "No preview available for this attachment.");
                return;
            }

            // Remove any existing preview
            _root.Q(className: "gf-attach-preview-overlay")?.RemoveFromHierarchy();

            var overlay = new VisualElement();
            overlay.AddToClassList("gf-attach-preview-overlay");
            overlay.pickingMode = PickingMode.Position;

            var panel = new VisualElement();
            panel.AddToClassList("gf-attach-preview-panel");

            var title = new Label(string.IsNullOrEmpty(fileName) ? "Preview" : fileName);
            title.AddToClassList("gf-attach-preview-title");
            panel.Add(title);

            var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
            img.AddToClassList("gf-attach-preview-img");
            panel.Add(img);

            var close = new Button(() =>
            {
                overlay.RemoveFromHierarchy();
                if (full != null) UnityEngine.Object.DestroyImmediate(full);
            })
            { text = "Close" };
            close.AddToClassList("gf-btn");
            close.AddToClassList("gf-btn-primary");
            close.AddToClassList("gf-attach-preview-close");
            panel.Add(close);

            overlay.Add(panel);
            overlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == overlay)
                {
                    overlay.RemoveFromHierarchy();
                    if (full != null) UnityEngine.Object.DestroyImmediate(full);
                }
            });
            _root.Add(overlay);
        }

        private void RemovePendingAttachment(string id)
        {
            for (var i = _pendingAttachments.Count - 1; i >= 0; i--)
            {
                if (_pendingAttachments[i].Id != id) continue;
                _pendingAttachments[i].Chip?.RemoveFromHierarchy();
                if (_pendingAttachments[i].Thumb != null)
                    UnityEngine.Object.DestroyImmediate(_pendingAttachments[i].Thumb);
                _pendingAttachments.RemoveAt(i);
            }
            if (_attachStrip != null && _pendingAttachments.Count == 0)
                _attachStrip.style.display = DisplayStyle.None;
        }

        private void ClearPendingAttachments()
        {
            foreach (var a in _pendingAttachments)
            {
                a.Chip?.RemoveFromHierarchy();
                if (a.Thumb != null) UnityEngine.Object.DestroyImmediate(a.Thumb);
            }
            _pendingAttachments.Clear();
            if (_attachStrip != null) _attachStrip.style.display = DisplayStyle.None;
            _root?.Q(className: "gf-attach-preview-overlay")?.RemoveFromHierarchy();
        }

        private static Label LabelCls(string cls, string text)
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            return l;
        }

        private static Button Btn(string text, Action onClick, bool ghost = false)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("gf-btn");
            if (ghost) b.AddToClassList("gf-btn-ghost");
            return b;
        }

        private static void TryAddStyles(VisualElement root)
        {
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GameForgeChatWindow).Assembly);
                if (info != null)
                {
                    var path = Path.Combine(info.assetPath, "UI/GameForge.uss").Replace('\\', '/');
                    var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                    if (sheet != null)
                    {
                        root.styleSheets.Add(sheet);
                        return;
                    }
                }
            }
            catch
            {
                /* fall through */
            }

            foreach (var guid in AssetDatabase.FindAssets("GameForge t:StyleSheet"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("GameForge.uss", StringComparison.OrdinalIgnoreCase)) continue;
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null) root.styleSheets.Add(sheet);
                return;
            }
        }

        private static string Trunc(string s, int n) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n) + "…");
    }
}

