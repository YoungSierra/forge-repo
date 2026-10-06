using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    public enum GameForgeMode
    {
        Prototype,
        VerticalSlice,
        Production
    }

    public enum GameForgeUiTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Shared workbench state between the Workbench window and the dockable Chat window.
    /// </summary>
    public static class GameForgeSession
    {
        private const string ThemePrefKey = "V57.GameForge.UiTheme";
        private const string DebugPrefKey = "V57.GameForge.ChatDebugMode";
        private const string SlugPrefKey = "V57.GameForge.SelectedSlug";
        private const string ProjectPrefKey = "V57.GameForge.SelectedProjectName";
        private const string ModePrefKey = "V57.GameForge.ForgeMode";
        private const string GameSetupRunAllPrefKey = "V57.GameForge.GameSetupRunAll";

        public static GameForgeAgentBridge Bridge { get; } = new();

        private static string _selectedSlug = "";
        private static string _selectedProjectName = "";
        private static bool _selectionLoaded;
        private static GameForgeMode _mode = GameForgeMode.Production;
        private static bool _modeLoaded;

        public static string SelectedSlug
        {
            get
            {
                EnsureSelectionLoaded();
                return _selectedSlug;
            }
            set => SetSelectedTdd(value, SelectedProjectName);
        }

        public static string SelectedProjectName
        {
            get
            {
                EnsureSelectionLoaded();
                return _selectedProjectName;
            }
            set => SetSelectedTdd(SelectedSlug, value);
        }

        /// <summary>Persist Workbench TDD selection (survives domain reload / Chat reopen).</summary>
        public static void SetSelectedTdd(string slug, string projectName)
        {
            EnsureSelectionLoaded();
            slug ??= "";
            projectName ??= "";
            if (_selectedSlug == slug && _selectedProjectName == projectName) return;
            _selectedSlug = slug;
            _selectedProjectName = projectName;
            EditorPrefs.SetString(SlugPrefKey, slug);
            EditorPrefs.SetString(ProjectPrefKey, projectName);
            NotifyChanged();
        }

        private static void EnsureSelectionLoaded()
        {
            if (_selectionLoaded) return;
            _selectionLoaded = true;
            _selectedSlug = EditorPrefs.GetString(SlugPrefKey, "");
            _selectedProjectName = EditorPrefs.GetString(ProjectPrefKey, "");
        }

        private static void EnsureModeLoaded()
        {
            if (_modeLoaded) return;
            _modeLoaded = true;
            _mode = ParseMode(EditorPrefs.GetString(ModePrefKey, "Prototype"));
        }

        public static GameForgeMode Mode
        {
            get
            {
                EnsureModeLoaded();
                return _mode;
            }
            set
            {
                EnsureModeLoaded();
                if (_mode == value) return;
                _mode = value;
                EditorPrefs.SetString(ModePrefKey, ModeLabel(value));
                NotifyChanged();
            }
        }

        /// <summary>Sync Workbench mode from sidecar session metadata (poll / activity / SSE).</summary>
        public static void ApplyRemoteForgeMode(string forgeModeLabel, string chatSkillId = null)
        {
            if (!string.IsNullOrWhiteSpace(forgeModeLabel))
            {
                Mode = ParseMode(forgeModeLabel.Trim());
                return;
            }

            if (string.IsNullOrWhiteSpace(chatSkillId)) return;
            if (chatSkillId.Equals("game-setup", StringComparison.OrdinalIgnoreCase)
                || chatSkillId.Equals("game-setup-run-all", StringComparison.OrdinalIgnoreCase))
            {
                Mode = GameForgeMode.Production;
                GameSetupRunAll = chatSkillId.Equals("game-setup-run-all", StringComparison.OrdinalIgnoreCase);
            }
            else if (chatSkillId.Equals("prototype-full", StringComparison.OrdinalIgnoreCase))
                Mode = GameForgeMode.Prototype;
        }
        public static string ProviderId { get; set; } = "cursor";
        public static string ModelId { get; set; } = "auto";
        private static string _chatMode = "agent";

        /// <summary>Chat write mode: <c>agent</c> or <c>ask</c> only.</summary>
        public static string ChatMode
        {
            get
            {
                if (_chatMode == "plan") _chatMode = "agent";
                return _chatMode is "ask" or "agent" ? _chatMode : "agent";
            }
            set
            {
                var next = value == "ask" ? "ask" : "agent";
                if (_chatMode == next) return;
                _chatMode = next;
                NotifyChanged();
            }
        }

        private static bool? _chatDebug;
        private static GameForgeUiTheme? _theme;

        /// <summary>When true, SYSTEM / tool / session chatter is visible in Chat.</summary>
        public static bool ChatDebugMode
        {
            get
            {
                if (_chatDebug.HasValue) return _chatDebug.Value;
                _chatDebug = EditorPrefs.GetBool(DebugPrefKey, false);
                return _chatDebug.Value;
            }
            set
            {
                if (_chatDebug == value) return;
                _chatDebug = value;
                EditorPrefs.SetBool(DebugPrefKey, value);
                NotifyChanged();
            }
        }

        public static void ToggleChatDebugMode() => ChatDebugMode = !ChatDebugMode;

        /// <summary>
        /// When true, Chat Continue / resume uses <c>/game-setup-run-all</c> (chain stages).
        /// Set when the user invokes that slash skill; cleared on plain <c>/game-setup</c>.
        /// </summary>
        public static bool GameSetupRunAll
        {
            get => EditorPrefs.GetBool(GameSetupRunAllPrefKey, false);
            set
            {
                if (EditorPrefs.GetBool(GameSetupRunAllPrefKey, false) == value) return;
                EditorPrefs.SetBool(GameSetupRunAllPrefKey, value);
                NotifyChanged();
            }
        }

        public static GameForgeUiTheme UiTheme
        {
            get
            {
                if (_theme.HasValue) return _theme.Value;
                var raw = EditorPrefs.GetString(ThemePrefKey, nameof(GameForgeUiTheme.Light));
                _theme = raw == nameof(GameForgeUiTheme.Dark) ? GameForgeUiTheme.Dark : GameForgeUiTheme.Light;
                return _theme.Value;
            }
            set
            {
                if (_theme == value) return;
                _theme = value;
                EditorPrefs.SetString(ThemePrefKey, value.ToString());
                NotifyChanged();
            }
        }

        public static bool IsDarkTheme => UiTheme == GameForgeUiTheme.Dark;

        public static void ToggleUiTheme() =>
            UiTheme = IsDarkTheme ? GameForgeUiTheme.Light : GameForgeUiTheme.Dark;

        public static event Action Changed;
        public static event Action BusyChanged;
        /// <summary>Sidecar has busy session(s) but this Editor has no local SSE (orphaned after Play/reload).</summary>
        public static event Action OrphanRemoteBusy;

        /// <summary>Poll watcher, kickoff, or sidecar still running a session.</summary>
        public static bool IsBusy =>
            Bridge.IsBusy || (RemoteBusy && !ReattachSuppressed);

        /// <summary>True when /api/health reports busySessions (survives UI disconnect).</summary>
        public static bool RemoteBusy { get; private set; }

        public static string[] RemoteBusySessionIds { get; private set; } = Array.Empty<string>();

        private static bool _busyPollInFlight;
        private static double _nextBusyPollAt;
        private static bool _wasOrphanRemote;
        /// <summary>After user Stop, do not auto-reattach for a short window (avoids fighting cancel).</summary>
        private static double _suppressReattachUntil;
        private const string SuppressReattachUntilUtcKey = "V57.GameForge.SuppressReattachUntilUtcTicks";

        public static bool ReattachSuppressed
        {
            get
            {
                if (EditorApplication.timeSinceStartup < _suppressReattachUntil)
                    return true;
                try
                {
                    var raw = SessionState.GetString(SuppressReattachUntilUtcKey, "");
                    if (long.TryParse(raw, out var ticks) && ticks > 0)
                        return DateTime.UtcNow.Ticks < ticks;
                }
                catch
                {
                    /* SessionState may be unavailable very early */
                }
                return false;
            }
        }

        public static void SuppressReattach(float seconds = 20f)
        {
            var secs = Math.Max(1f, seconds);
            _suppressReattachUntil = EditorApplication.timeSinceStartup + secs;
            _wasOrphanRemote = true; // prevent immediate OrphanRemoteBusy edge
            try
            {
                // Survive domain reload — timeSinceStartup resets; UTC ticks do not.
                SessionState.SetString(
                    SuppressReattachUntilUtcKey,
                    DateTime.UtcNow.AddSeconds(secs).Ticks.ToString());
            }
            catch
            {
                /* ignore */
            }
        }

        public static void ClearReattachSuppress()
        {
            _suppressReattachUntil = 0;
            try { SessionState.SetString(SuppressReattachUntilUtcKey, ""); }
            catch { /* ignore */ }
        }

        static GameForgeSession()
        {
            EditorApplication.update += BusyPollTick;
            EnsureSelectionLoaded();
            EnsureModeLoaded();
        }

        private static void BusyPollTick()
        {
            if (EditorApplication.timeSinceStartup < _nextBusyPollAt) return;
            _nextBusyPollAt = EditorApplication.timeSinceStartup + 1.25;
            _ = PollRemoteBusyAsync();
        }

        private static async System.Threading.Tasks.Task PollRemoteBusyAsync()
        {
            if (_busyPollInFlight) return;
            _busyPollInFlight = true;
            try
            {
                var ids = await Bridge.GetBusySessionIdsAsync();
                var busy = ids != null && ids.Length > 0;
                var changed = busy != RemoteBusy;
                RemoteBusy = busy;
                RemoteBusySessionIds = ids ?? Array.Empty<string>();

                if (busy && !ReattachSuppressed)
                {
                    if (string.IsNullOrEmpty(Bridge.ActiveSessionId) ||
                        Array.IndexOf(ids, Bridge.ActiveSessionId) < 0)
                        Bridge.ActiveSessionId = ids[0];
                }

                if (changed)
                    NotifyBusyChanged();

                var orphan = busy && !Bridge.IsBusy && !GameForgeRunWatcher.IsWatching && !ReattachSuppressed;
                if (orphan && !_wasOrphanRemote)
                    OrphanRemoteBusy?.Invoke();
                _wasOrphanRemote = orphan || ReattachSuppressed;

                if (busy && !ReattachSuppressed && !GameForgeRunWatcher.IsWatching && !Bridge.IsKickoffBusy)
                    _ = GameForgeRunWatcher.ResumeIfNeededAsync();
            }
            catch
            {
                // Transient health/network blips must NOT clear RemoteBusy — that hid Stop
                // while the sidecar was still on LLM turns / tools.
            }
            finally
            {
                _busyPollInFlight = false;
            }
        }

        /// <summary>Force an immediate remote busy refresh (e.g. after Stop).</summary>
        public static async System.Threading.Tasks.Task RefreshRemoteBusyAsync()
        {
            _nextBusyPollAt = 0;
            await PollRemoteBusyAsync();
        }

        public static void NotifyChanged() => Changed?.Invoke();

        public static void NotifyBusyChanged() => BusyChanged?.Invoke();

        /// <summary>Instant local stop — UI unlocks immediately; sidecar cancel continues in background.</summary>
        public static void BeginStop()
        {
            SuppressReattach(25f);
            Bridge.CancelLocalRun();
            // Sync — must beat domain reload so ResumeIfNeeded does not revive the watch.
            GameForgeRunWatcher.StopWatching();
            RemoteBusy = false;
            RemoteBusySessionIds = Array.Empty<string>();
            _wasOrphanRemote = true;
            NotifyBusyChanged();
            _ = StopActiveRunAsync();
        }

        public static async System.Threading.Tasks.Task StopActiveRunAsync()
        {
            SuppressReattach(25f);
            Bridge.CancelLocalRun();
            GameForgeRunWatcher.StopWatching();
            RemoteBusy = false;
            RemoteBusySessionIds = Array.Empty<string>();
            _wasOrphanRemote = true;
            NotifyBusyChanged();
            try
            {
                await Bridge.StopSidecarCancelAsync();
                await RefreshRemoteBusyAsync();
                if (RemoteBusy)
                {
                    await Bridge.StopSidecarCancelAsync();
                    await RefreshRemoteBusyAsync();
                }
            }
            catch
            {
                /* best-effort */
            }
            finally
            {
                NotifyBusyChanged();
            }
        }

        public static void ApplyThemeClass(VisualElement root)
        {
            if (root == null) return;
            root.EnableInClassList("gf-theme-dark", IsDarkTheme);
            root.EnableInClassList("gf-theme-light", !IsDarkTheme);
        }

        public static string ModeLabel(GameForgeMode mode) => mode switch
        {
            GameForgeMode.VerticalSlice => "Vertical Slice",
            GameForgeMode.Production => "Production",
            _ => "Prototype"
        };

        public static GameForgeMode ParseMode(string label)
        {
            if (string.IsNullOrEmpty(label)) return GameForgeMode.Production;
            if (label.StartsWith("Vertical", StringComparison.OrdinalIgnoreCase)) return GameForgeMode.VerticalSlice;
            if (label.StartsWith("Production", StringComparison.OrdinalIgnoreCase)) return GameForgeMode.Production;
            if (label.StartsWith("Prototype", StringComparison.OrdinalIgnoreCase)) return GameForgeMode.Prototype;
            return GameForgeMode.Production;
        }

        public static string McpStatusLabel()
        {
            try
            {
                string pipeline = null;
                string legacy = null;
                foreach (var info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                {
                    if (info.name == "com.unity.pipeline") pipeline = info.version;
                    else if (info.name == "com.unity.ai.assistant") legacy = info.version;
                }
                if (!string.IsNullOrEmpty(pipeline)) return $"CLI Pipeline {pipeline}";
                if (!string.IsNullOrEmpty(legacy)) return $"Legacy MCP {legacy}";
                return "MCP missing";
            }
            catch
            {
                return "MCP ?";
            }
        }

        public static bool IsMcpPackagePresent()
        {
            try
            {
                foreach (var info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                {
                    if (info.name is "com.unity.pipeline" or "com.unity.ai.assistant") return true;
                }
            }
            catch { /* ignore */ }
            return false;
        }

        public static bool IsPipelinePackagePresent()
        {
            try
            {
                foreach (var info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                {
                    if (info.name == "com.unity.pipeline") return true;
                }
            }
            catch { /* ignore */ }
            return false;
        }

        public static readonly string[] ModeChoices =
        {
            "Prototype",
            "Vertical Slice",
            "Production"
        };

        public static readonly string[] ChatModeChoices =
        {
            "Agent",
            "Ask"
        };
    }
}
