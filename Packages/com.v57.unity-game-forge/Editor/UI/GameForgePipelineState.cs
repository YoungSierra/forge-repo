using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace V57.GameForge.Editor
{
    public enum PipelineNodeStatus
    {
        Pending,
        Running,
        Ready,
        Blocked
    }

    public sealed class PipelineCard
    {
        public string Id;
        public string Title;
        public string Action = "";
        public string Detail = "";
        public int Done;
        public int Total;
        public PipelineNodeStatus Status = PipelineNodeStatus.Pending;
    }

    /// <summary>
    /// Live /game-setup graph. Fed by GF-PROGRESS lines and game-setup-progress.md.
    /// </summary>
    public static class GameForgePipelineState
    {
        public static readonly string[] StageOrder =
        {
            "INIT", "SETUP", "0", "A", "B", "C", "D", "E", "F",
            "G-OVR", "G-RENDER", "G-LIGHT", "G-POST", "G-VIS", "G-CAM",
            "G-FEEL", "G-FUNC", "G-QA", "G-PERF", "H", "I", "CHECKLIST"
        };

        private static readonly Regex ProgressLine = new(
            @"GF-PROGRESS\s+stage=(?<stage>\S+)\s+spec=(?<spec>\S+)\s+done=(?<done>\d+)\s+total=(?<total>\d+)\s+action=""(?<action>[^""]*)""\s+status=(?<status>ready|running|blocked)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex StageStartLine = new(
            @"Starting Stage\s+(?<stage>INIT|SETUP|CHECKLIST|G-[A-Z0-9-]+|[0A-I])\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static event Action Changed;
        public static void NotifyChanged() => Changed?.Invoke();
        public static event Action<string> FocusRequested;
        public static event Action StopRequested;
        public static event Action RunSetupRequested;

        public static string ProjectTitle { get; private set; } = "Game setup";
        public static string CurrentStage { get; private set; } = "";
        public static string CurrentAction { get; private set; } = "";
        public static string CurrentScript { get; private set; } = "";
        public static bool AwaitingUser { get; private set; }
        public static string AwaitStageId { get; private set; } = "";
        public static string AwaitMessage { get; private set; } = "";
        public static string AwaitPrefix { get; private set; } = "";
        public static string AwaitScene { get; private set; } = "";
        public static string AwaitTdd { get; private set; } = "";
        public static bool Vertical { get; set; }
        public static bool DummyRunning { get; private set; }
        public static readonly List<PipelineCard> Stages = new();
        public static readonly List<PipelineCard> Specs = new();

        public static int ReadyCount
        {
            get
            {
                var n = 0;
                foreach (var s in Stages)
                    if (s.Status == PipelineNodeStatus.Ready) n++;
                return n;
            }
        }

        public static int StageTotal => Stages.Count;

        public static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            switch (id.ToUpperInvariant())
            {
                case "INIT": return "Confirm variables";
                case "SETUP": return "Pack and MCP";
                case "0": return "MCP preflight";
                case "A": return "TDD readiness";
                case "B": return "TDD health";
                case "C": return "CONTEXT";
                case "D": return "Specs";
                case "E": return "Bootstrap";
                case "F": return "Implement";
                case "G": return "Crafts";
                case "G-OVR": return "Scene layout";
                case "G-RENDER": return "Render pipeline";
                case "G-LIGHT": return "Lighting";
                case "G-POST": return "Post-processing";
                case "G-VIS": return "Visuals";
                case "G-CAM": return "Camera";
                case "G-FEEL": return "Game feel";
                case "G-FUNC": return "Playable";
                case "G-QA": return "Product QA";
                case "G-PERF": return "Performance";
                case "H": return "Review";
                case "I": return "Definition of done";
                case "CHECKLIST": return "Checklist";
                default: return id;
            }
        }

        public static bool IsCraft(string id) =>
            !string.IsNullOrEmpty(id) && id.StartsWith("G-", StringComparison.OrdinalIgnoreCase);

        static GameForgePipelineState()
        {
            ResetCatalog();
        }

        public static void ResetCatalog()
        {
            Stages.Clear();
            Specs.Clear();
            foreach (var id in StageOrder)
            {
                Stages.Add(new PipelineCard
                {
                    Id = id,
                    Title = DisplayName(id),
                    Total = 1
                });
            }
        }

        private const string TrackCreatesPrefKey = "V57.GameForge.TrackSetupCreates";
        private static readonly object CreatedLock = new();

        public static string ProgressFilePath
        {
            get
            {
                var root = ProjectRootSafe();
                return Path.GetFullPath(Path.Combine(root, "V57", "docs", "reports", "game-setup-progress.md"));
            }
        }

        /// <summary>Append-only list of files created during /game-setup. Built as each write happens.</summary>
        public static string CreatedFilePath
        {
            get
            {
                var root = ProjectRootSafe();
                return Path.GetFullPath(Path.Combine(root, "V57", "docs", "reports", "game-setup-created.md"));
            }
        }

        /// <summary>When true, new write_file paths are appended to the created-files list.</summary>
        public static bool TrackSetupCreates
        {
            get => EditorPrefs.GetBool(TrackCreatesPrefKey, false);
            set
            {
                if (EditorPrefs.GetBool(TrackCreatesPrefKey, false) == value) return;
                EditorPrefs.SetBool(TrackCreatesPrefKey, value);
            }
        }

        public static bool HasCheckpoint
        {
            get
            {
                try
                {
                    var path = ProgressFilePath;
                    if (!File.Exists(path)) return false;
                    var text = File.ReadAllText(path);
                    return text.IndexOf("GF-PROGRESS", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string NextSetupCommand =>
            HasCheckpoint ? "/game-setup --continue" : "/game-setup";

        /// <summary>
        /// Append a path the moment a new file is written during /game-setup.
        /// Skips files that already existed and anything outside the project.
        /// </summary>
        public static void NoteCreatedFile(string path, bool existedBeforeWrite)
        {
            if (!TrackSetupCreates || existedBeforeWrite) return;
            var rel = NormalizeCreatedPath(path);
            if (string.IsNullOrEmpty(rel) || !IsSafeCreatedPath(rel)) return;
            lock (CreatedLock)
            {
                try
                {
                    var listed = ReadCreatedUnlocked();
                    if (ContainsPath(listed, rel)) return;
                    var file = CreatedFilePath;
                    var dir = Path.GetDirectoryName(path: file);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    if (!File.Exists(file))
                    {
                        File.WriteAllText(file,
                            "# Game setup — created files\n\n" +
                            "Appended as each new file is written. Clear progress deletes this list.\n\n");
                    }

                    var stage = string.IsNullOrEmpty(CurrentStage) ? "" : CurrentStage;
                    var line = string.IsNullOrEmpty(stage)
                        ? $"- {rel}\n"
                        : $"- {rel} ({stage})\n";
                    File.AppendAllText(file, line, Encoding.UTF8);
                }
                catch
                {
                    /* locked or unwritable — next write retries */
                }
            }
        }

        public static IReadOnlyList<string> ReadCreatedFiles()
        {
            lock (CreatedLock)
                return ReadCreatedUnlocked();
        }

        /// <summary>
        /// Warn, then delete every file listed as created plus the checkpoint.
        /// Returns false if the user cancels.
        /// </summary>
        public static bool ClearProgress()
        {
            var created = ReadCreatedFiles();
            var preview = new StringBuilder();
            var shown = 0;
            foreach (var rel in created)
            {
                if (shown >= 14)
                {
                    preview.AppendLine($"… and {created.Count - shown} more");
                    break;
                }

                preview.AppendLine(rel);
                shown++;
            }

            var body = created.Count == 0
                ? "No created files are listed yet.\n\nThis still forgets the pipeline checkpoint. The next Run game setup starts at Confirm variables."
                : $"This deletes {created.Count} file(s) created during game setup, plus the checkpoint. This cannot be undone.\n\n{preview}";

            if (!EditorUtility.DisplayDialog(
                    "Clear pipeline progress",
                    body,
                    "Delete created files",
                    "Cancel"))
                return false;

            if (DummyRunning) StopDummy();

            var deleted = 0;
            foreach (var rel in created)
                if (TryDeleteCreated(rel)) deleted++;

            TryDeleteFile(CreatedFilePath);
            TryDeleteFile(ProgressFilePath);
            TrackSetupCreates = false;

            ResetCatalog();
            Specs.Clear();
            CurrentStage = "";
            CurrentAction = "";
            CurrentScript = "";
            ProjectTitle = "Game setup";
            ClearAwait();
            Changed?.Invoke();
            if (deleted > 0)
                AssetDatabase.Refresh();
            return true;
        }

        public static void ReloadFromDisk()
        {
            try
            {
                var path = ProgressFilePath;
                if (!File.Exists(path)) return;
                Ingest(File.ReadAllText(path), notify: true);
            }
            catch
            {
                /* ignore missing or locked report */
            }
        }

        public static bool Ingest(string text, bool notify = true)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var changed = false;
            var dump = IsInstructionDump(text);
            if (!dump)
            foreach (Match m in ProgressLine.Matches(text))
            {
                Apply(
                    m.Groups["stage"].Value,
                    m.Groups["spec"].Value,
                    int.Parse(m.Groups["done"].Value),
                    int.Parse(m.Groups["total"].Value),
                    m.Groups["action"].Value,
                    m.Groups["status"].Value);
                changed = true;
            }

            if (!IsInstructionDump(text) && NoteStageProse(text))
                changed = true;

            if (!IsInstructionDump(text) &&
                !_popupSuppressed &&
                string.IsNullOrEmpty(_dismissedConfirmKey) &&
                (text.IndexOf("confirm required", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 text.IndexOf("Reply `continue`", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var stage = string.IsNullOrEmpty(CurrentStage) ? "INIT" : CurrentStage;
                MarkRunning(stage, "waiting for confirm");
                SetAwait(stage, "Confirm the proposed values, then continue.");
                changed = true;
            }

            if (changed && notify)
                Changed?.Invoke();
            return changed;
        }

        public static void RequestFocus(string stageId) => FocusRequested?.Invoke(stageId);

        private static string _dismissedConfirmKey = "";
        private static string _activeConfirmKey = "";
        private static bool _popupSuppressed;

        public static void SetAwait(string stageId, string message)
        {
            AwaitingUser = true;
            AwaitStageId = stageId ?? "";
            AwaitMessage = message ?? "";
        }

        public static void NoteConfirmPrompt(ConfirmPrompt prompt)
        {
            if (prompt == null) return;
            var key = ConfirmKey(prompt);
            if (_popupSuppressed &&
                (string.IsNullOrEmpty(_dismissedConfirmKey) ||
                 string.Equals(_dismissedConfirmKey, key, StringComparison.Ordinal)))
                return;
            if (!string.IsNullOrEmpty(_dismissedConfirmKey) &&
                string.Equals(_dismissedConfirmKey, key, StringComparison.Ordinal))
                return;

            _popupSuppressed = false;

            var stage = "INIT";
            var message = prompt.Subtitle ?? "";
            if (prompt.Kind == ConfirmPromptKind.InitDefaults)
            {
                stage = "INIT";
                message = string.IsNullOrEmpty(prompt.Subtitle)
                    ? "Confirm the proposed values, then press Play."
                    : prompt.Subtitle;
                AwaitTdd = ValueOf(prompt, "tdd");
                AwaitPrefix = ValueOf(prompt, "prefix");
                AwaitScene = ValueOf(prompt, "scene");
            }
            else if (prompt.Kind == ConfirmPromptKind.StageContinue)
            {
                stage = string.IsNullOrEmpty(prompt.StageId) ? CurrentStage : prompt.StageId;
                message = string.IsNullOrEmpty(prompt.BodySummary)
                    ? "Press Play on this stage to continue."
                    : prompt.BodySummary;
            }
            else if (prompt.Kind == ConfirmPromptKind.Stopped)
            {
                stage = string.IsNullOrEmpty(prompt.StageId) ? CurrentStage : prompt.StageId;
                message = string.IsNullOrEmpty(prompt.NeededFromUser)
                    ? prompt.Reason ?? "Provide the requested input, then press Play."
                    : prompt.NeededFromUser;
            }

            if (string.IsNullOrEmpty(stage))
                stage = "INIT";
            _activeConfirmKey = key;
            _dismissedConfirmKey = "";
            MarkRunning(stage, "waiting for confirm");
            SetAwait(stage, message);
            Changed?.Invoke();
        }

        /// <summary>Close the current confirm immediately so Accept cannot be sent twice.</summary>
        public static void DismissConfirm()
        {
            if (!string.IsNullOrEmpty(_activeConfirmKey))
                _dismissedConfirmKey = _activeConfirmKey;
            else if (!string.IsNullOrEmpty(AwaitStageId))
                _dismissedConfirmKey = AwaitStageId + "|" + (AwaitMessage ?? "");
            else if (string.IsNullOrEmpty(_dismissedConfirmKey))
                _dismissedConfirmKey = "dismissed";
            _popupSuppressed = true;
            _activeConfirmKey = "";
            ClearAwait();
            Changed?.Invoke();
        }

        private static string ConfirmKey(ConfirmPrompt prompt)
        {
            if (prompt == null) return "";
            var stage = prompt.Kind == ConfirmPromptKind.InitDefaults
                ? "INIT"
                : (prompt.StageId ?? "");
            return prompt.Kind + "|" + stage + "|" + (prompt.Title ?? "") + "|" + ValueOf(prompt, "tdd");
        }

        private static string ValueOf(ConfirmPrompt prompt, string name)
        {
            if (prompt?.Variables == null) return "";
            foreach (var row in prompt.Variables)
            {
                if (string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase))
                    return row.ProposedValue ?? "";
            }

            return "";
        }

        public static void ClearAwait()
        {
            AwaitingUser = false;
            AwaitStageId = "";
            AwaitMessage = "";
            AwaitPrefix = "";
            AwaitScene = "";
            AwaitTdd = "";
        }

        public static void ToggleDummy()
        {
            if (DummyRunning) return;
            StartDummy();
        }

        public static void StartDummy()
        {
            DummyRunning = true;
            _dummyIndex = 0;
            _dummyTick = 0;
            ResetCatalog();
            Specs.Clear();
            ClearAwait();
            CurrentScript = "";
            UnityEditor.EditorApplication.update -= DummyTick;
            UnityEditor.EditorApplication.update += DummyTick;
            Changed?.Invoke();
        }

        public static void ClearDummy()
        {
            StopDummy();
            ResetCatalog();
            Specs.Clear();
            CurrentStage = "";
            CurrentAction = "";
            CurrentScript = "";
            ProjectTitle = "Game setup";
            Changed?.Invoke();
        }

        public static void StopDummy()
        {
            DummyRunning = false;
            _dummyHold = false;
            UnityEditor.EditorApplication.update -= DummyTick;
            ClearAwait();
            Changed?.Invoke();
        }

        /// <summary>True from Play click until the session is actually busy or the launch is cancelled.</summary>
        public static bool SetupLaunching { get; private set; }

        public static bool SetupRunLive =>
            SetupLaunching || DummyRunning || GameForgeSession.IsBusy || GameForgeChatWindow.HasActiveRun;

        public static void RequestStop()
        {
            SetupLaunching = false;
            if (DummyRunning) StopDummy();
            StopRequested?.Invoke();
            Changed?.Invoke();
        }

        public static void RequestRunSetup()
        {
            SetupLaunching = true;
            if (string.IsNullOrEmpty(CurrentAction))
                CurrentAction = "starting";
            Changed?.Invoke();
            RunSetupRequested?.Invoke();
        }

        public static void ClearSetupLaunch()
        {
            if (!SetupLaunching && GameForgeSession.IsBusy) return;
            var changed = SetupLaunching;
            SetupLaunching = false;
            if (string.Equals(CurrentAction, "starting", StringComparison.OrdinalIgnoreCase))
            {
                CurrentAction = "";
                changed = true;
            }

            if (changed)
                Changed?.Invoke();
        }

        public static void AcceptAwait()
        {
            if (!AwaitingUser) return;
            AcknowledgeConfirm(AwaitStageId);
        }

        /// <summary>User accepted the current gate. That is not proof the stage finished.</summary>
        public static void NoteUserReply(string text)
        {
            var t = (text ?? "").Trim();
            if (t.Length == 0 || t.Length > 48) return;
            if (!IsConfirmReply(t)) return;
            if (!AwaitingUser) return;
            AcknowledgeConfirm(AwaitStageId);
        }

        public static void NoteLiveText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Ingest(text, notify: true);
        }

        public static void ApplyChatLog(IReadOnlyList<(string role, string text)> entries, bool notify = true)
        {
            if (DummyRunning || entries == null || entries.Count == 0) return;
            ResetCatalog();
            CurrentStage = "";
            CurrentAction = "";
            CurrentScript = "";
            ClearAwait();

            var sawSetup = false;
            var sawConfirmAsk = false;
            var sawConfirmReply = false;
            foreach (var entry in entries)
            {
                var text = entry.text ?? "";
                var you = string.Equals(entry.role, "you", StringComparison.OrdinalIgnoreCase);
                if (you && text.TrimStart().StartsWith("/game-setup", StringComparison.OrdinalIgnoreCase))
                    sawSetup = true;
                if (!you && text.IndexOf("confirm required", StringComparison.OrdinalIgnoreCase) >= 0)
                    sawConfirmAsk = true;
                if (you && IsConfirmReply(text) && sawConfirmAsk)
                    sawConfirmReply = true;
                if (!IsInstructionDump(text))
                    Ingest(text, notify: false);
            }

            ReloadFromDiskSilent();

            var pending = LatestUnansweredConfirm(entries);
            var live = false;
            try { live = GameForgeSession.IsBusy; }
            catch { /* session not ready */ }

            if (sawSetup && string.IsNullOrEmpty(CurrentStage) && live && pending == null)
                MarkRunning("INIT", sawConfirmReply ? "confirmed — waiting for agent" : "resolving variables");
            else if (live && sawConfirmReply)
            {
                var init = FindStage("INIT");
                if (init != null && init.Status != PipelineNodeStatus.Ready)
                {
                    init.Status = PipelineNodeStatus.Running;
                    init.Action = "confirmed — waiting for agent";
                    CurrentStage = "INIT";
                    CurrentAction = init.Action;
                }
            }

            if (pending != null)
                NoteConfirmPrompt(pending);
            else if (live && sawConfirmAsk && !sawConfirmReply && !_popupSuppressed && string.IsNullOrEmpty(_dismissedConfirmKey))
            {
                MarkRunning(string.IsNullOrEmpty(CurrentStage) ? "INIT" : CurrentStage, "waiting for confirm");
                SetAwait(string.IsNullOrEmpty(CurrentStage) ? "INIT" : CurrentStage, "Confirm the proposed values, then continue.");
            }

            if (!live)
                ClearIdleRunning();

            if (notify)
                Changed?.Invoke();
        }

        public static void ClearIdleRunning()
        {
            if (DummyRunning || SetupLaunching || AwaitingUser) return;
            try
            {
                if (GameForgeSession.IsBusy) return;
            }
            catch
            {
                /* session not ready */
            }

            foreach (var stage in Stages)
            {
                if (stage.Status != PipelineNodeStatus.Running) continue;
                stage.Status = PipelineNodeStatus.Pending;
                if (stage.Action != null &&
                    (stage.Action.IndexOf("waiting for agent", StringComparison.OrdinalIgnoreCase) >= 0
                     || stage.Action.IndexOf("confirmed", StringComparison.OrdinalIgnoreCase) >= 0))
                    stage.Action = "";
            }

            foreach (var spec in Specs)
            {
                if (spec.Status == PipelineNodeStatus.Running)
                    spec.Status = PipelineNodeStatus.Pending;
            }

            if (CurrentAction != null &&
                (CurrentAction.IndexOf("waiting for agent", StringComparison.OrdinalIgnoreCase) >= 0
                 || CurrentAction.IndexOf("confirmed", StringComparison.OrdinalIgnoreCase) >= 0))
                CurrentAction = "";
        }

        private static ConfirmPrompt LatestUnansweredConfirm(IReadOnlyList<(string role, string text)> entries)
        {
            ConfirmPrompt pending = null;
            var answered = false;
            foreach (var entry in entries)
            {
                var text = entry.text ?? "";
                var you = string.Equals(entry.role, "you", StringComparison.OrdinalIgnoreCase);
                if (you && IsConfirmReply(text) && pending != null)
                {
                    pending = null;
                    answered = true;
                    continue;
                }

                if (you) continue;
                var prompt = GameForgeConfirmPromptParser.TryParse(text);
                if (prompt == null) continue;
                if (prompt.Kind == ConfirmPromptKind.InitDefaults &&
                    !GameForgeConfirmPromptParser.IsInitTableComplete(prompt))
                    continue;
                pending = prompt;
                answered = false;
            }

            return answered && pending == null ? null : pending;
        }

        private static void ReloadFromDiskSilent()
        {
            try
            {
                var path = ProgressFilePath;
                if (!File.Exists(path)) return;
                Ingest(File.ReadAllText(path), notify: false);
            }
            catch
            {
                /* ignore */
            }
        }

        private static bool IsInstructionDump(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 280) return false;
            if (GameForgeConfirmPromptParser.TryParse(text) != null)
                return false;
            return text.IndexOf("How to reply", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("will not start", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Proposed values", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool NoteStageProse(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var changed = false;
            foreach (Match m in StageStartLine.Matches(text))
            {
                var stage = m.Groups["stage"].Value;
                MarkRunning(stage, "starting");
                changed = true;
            }
            return changed;
        }

        private static bool IsConfirmReply(string text)
        {
            var t = text.Trim().Trim('`', '"', '\'', '.', '!');
            return t.Equals("continue", StringComparison.OrdinalIgnoreCase)
                || t.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || t.Equals("ok", StringComparison.OrdinalIgnoreCase)
                || t.Equals("accept", StringComparison.OrdinalIgnoreCase)
                || t.Equals("y", StringComparison.OrdinalIgnoreCase)
                || t.Equals("confirm", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("confirm ", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("continue ", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("yes ", StringComparison.OrdinalIgnoreCase);
        }

        private static void AcknowledgeConfirm(string stageId, bool notify = true)
        {
            var stage = string.IsNullOrEmpty(stageId) ? "INIT" : stageId;
            var card = FindStage(stage);
            if (card != null && card.Status != PipelineNodeStatus.Ready)
            {
                card.Status = PipelineNodeStatus.Running;
                card.Action = "confirmed — waiting for agent";
                CurrentStage = card.Id;
                CurrentAction = card.Action;
            }

            ClearAwait();
            if (DummyRunning)
                _dummyHold = false;
            if (notify)
                Changed?.Invoke();
        }

        private static void MarkRunning(string stage, string action)
        {
            var card = FindStage(stage);
            if (card == null) return;
            if (card.Status == PipelineNodeStatus.Ready) return;
            card.Status = PipelineNodeStatus.Running;
            if (!string.IsNullOrEmpty(action))
                card.Action = action;
            CurrentStage = card.Id;
            CurrentAction = card.Action;
            MarkPriorReady(stage, PipelineNodeStatus.Running);
        }

        private static int _dummyIndex;
        private static int _dummyTick;
        private static bool _dummyHold;
        private static readonly string[] DummyScripts =
        {
            "RaceConfig.cs", "KartTuning.cs", "InputReader.cs", "DashSystem.cs",
            "TrackBuilder.cs", "LightingSetup.cs", "RaceHud.cs", "PlayabilityCert.cs"
        };

        private static void DummyTick()
        {
            if (!DummyRunning) return;
            if (++_dummyTick < 40) return;
            _dummyTick = 0;
            if (_dummyHold) return;

            if (_dummyIndex > 0 && _dummyIndex <= Stages.Count)
                Stages[_dummyIndex - 1].Status = PipelineNodeStatus.Ready;

            if (_dummyIndex >= Stages.Count)
            {
                CurrentStage = "CHECKLIST";
                CurrentAction = "dummy complete";
                CurrentScript = "";
                StopDummy();
                return;
            }

            var card = Stages[_dummyIndex];
            card.Status = PipelineNodeStatus.Running;
            var script = DummyScripts[_dummyIndex % DummyScripts.Length];
            card.Action = "writing " + script;
            CurrentStage = card.Id;
            CurrentAction = card.Action;
            CurrentScript = script;
            card.Done = 1;
            card.Total = 2;

            if (card.Id == "INIT" || card.Id == "F" || card.Id == "G-QA")
            {
                _dummyHold = true;
                SetAwait(card.Id, card.Id == "INIT"
                    ? "Dummy: confirm variables, then press Play on the node."
                    : "Dummy: press Play on this node to continue.");
            }

            _dummyIndex++;
            Changed?.Invoke();
        }

        private static string ExtractScript(string action)
        {
            if (string.IsNullOrEmpty(action)) return "";
            var start = action.LastIndexOf(' ');
            var token = start >= 0 ? action.Substring(start + 1) : action;
            if (token.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                token.EndsWith(".uss", StringComparison.OrdinalIgnoreCase) ||
                token.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                return token;
            return "";
        }

        private static void Apply(string stage, string spec, int done, int total, string action, string status)
        {
            stage = (stage ?? "").Trim();
            spec = (spec ?? "").Trim();
            if (spec.Equals("none", StringComparison.OrdinalIgnoreCase) || spec == "-")
                spec = "";

            CurrentStage = stage;
            CurrentAction = action ?? "";
            CurrentScript = ExtractScript(action);

            var card = FindStage(stage);
            if (card == null)
            {
                card = new PipelineCard { Id = stage, Title = DisplayName(stage) };
                Stages.Add(card);
            }

            card.Action = action ?? "";
            card.Done = done;
            card.Total = Math.Max(1, total);
            card.Status = ParseStatus(status);
            card.Detail = string.IsNullOrEmpty(spec) ? "" : spec;

            if (!string.IsNullOrEmpty(spec))
                UpsertSpec(spec, action, done, total, card.Status);
            else if (stage.Equals("F", StringComparison.OrdinalIgnoreCase) &&
                     card.Status != PipelineNodeStatus.Running)
                Specs.Clear();

            MarkPriorReady(stage, card.Status);
        }

        private static void UpsertSpec(string spec, string action, int done, int total, PipelineNodeStatus status)
        {
            PipelineCard found = null;
            foreach (var s in Specs)
            {
                if (string.Equals(s.Id, spec, StringComparison.OrdinalIgnoreCase))
                {
                    found = s;
                    break;
                }
            }

            if (found == null)
            {
                found = new PipelineCard { Id = spec, Title = spec };
                Specs.Add(found);
            }

            found.Action = action ?? "";
            found.Done = done;
            found.Total = Math.Max(1, total);
            found.Status = status;
            found.Detail = "spec";
        }

        private static void MarkPriorReady(string stage, PipelineNodeStatus status)
        {
            if (status != PipelineNodeStatus.Ready &&
                status != PipelineNodeStatus.Running &&
                status != PipelineNodeStatus.Blocked)
                return;

            var idx = IndexOfStage(stage);
            if (idx <= 0) return;
            for (var i = 0; i < idx; i++)
            {
                if (Stages[i].Status == PipelineNodeStatus.Pending)
                    Stages[i].Status = PipelineNodeStatus.Ready;
            }
        }

        private static PipelineCard FindStage(string id)
        {
            foreach (var s in Stages)
            {
                if (string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }

        private static int IndexOfStage(string id)
        {
            for (var i = 0; i < Stages.Count; i++)
            {
                if (string.Equals(Stages[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static PipelineNodeStatus ParseStatus(string raw)
        {
            if (string.Equals(raw, "ready", StringComparison.OrdinalIgnoreCase))
                return PipelineNodeStatus.Ready;
            if (string.Equals(raw, "blocked", StringComparison.OrdinalIgnoreCase))
                return PipelineNodeStatus.Blocked;
            return PipelineNodeStatus.Running;
        }

        private static string ApplicationDataPathSafe()
        {
            try { return UnityEngine.Application.dataPath; }
            catch { return ""; }
        }

        private static string ProjectRootSafe()
        {
            var root = GameForgeBootstrap.ProjectRoot;
            if (string.IsNullOrEmpty(root))
                root = Path.GetDirectoryName(ApplicationDataPathSafe()) ?? "";
            return root ?? "";
        }

        private static List<string> ReadCreatedUnlocked()
        {
            var list = new List<string>();
            try
            {
                var path = CreatedFilePath;
                if (!File.Exists(path)) return list;
                foreach (var raw in File.ReadAllLines(path))
                {
                    var rel = ParseCreatedLine(raw);
                    if (string.IsNullOrEmpty(rel) || ContainsPath(list, rel)) continue;
                    list.Add(rel);
                }
            }
            catch
            {
                /* missing or locked */
            }

            return list;
        }

        private static string ParseCreatedLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var line = raw.Trim();
            if (!line.StartsWith("-", StringComparison.Ordinal)) return null;
            line = line.Substring(1).Trim();
            if (line.StartsWith("`", StringComparison.Ordinal) && line.EndsWith("`", StringComparison.Ordinal) && line.Length >= 2)
                line = line.Substring(1, line.Length - 2);
            var paren = line.LastIndexOf(" (", StringComparison.Ordinal);
            if (paren > 0)
                line = line.Substring(0, paren).Trim();
            return NormalizeCreatedPath(line);
        }

        private static string NormalizeCreatedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var n = path.Trim().Replace('\\', '/');
            if (n.StartsWith("./", StringComparison.Ordinal))
                n = n.Substring(2);
            var root = ProjectRootSafe().Replace('\\', '/').TrimEnd('/');
            if (!string.IsNullOrEmpty(root) && n.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
                n = n.Substring(root.Length + 1);
            return n.Trim().TrimStart('/');
        }

        private static bool ContainsPath(List<string> list, string rel)
        {
            foreach (var item in list)
                if (string.Equals(item, rel, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static bool IsSafeCreatedPath(string rel)
        {
            if (string.IsNullOrEmpty(rel)) return false;
            if (rel.IndexOf("..", StringComparison.Ordinal) >= 0) return false;
            if (Path.IsPathRooted(rel)) return false;
            var lower = rel.Replace('\\', '/').ToLowerInvariant();
            if (lower.StartsWith("packages/com.v57.unity-game-forge/", StringComparison.Ordinal)) return false;
            if (lower.StartsWith("v57/agents/", StringComparison.Ordinal)) return false;
            if (lower.StartsWith("library/", StringComparison.Ordinal)) return false;
            if (lower.StartsWith(".git/", StringComparison.Ordinal)) return false;
            if (lower == "v57/docs/reports/game-setup-created.md") return false;
            if (lower == "v57/docs/reports/game-setup-progress.md") return false;
            return true;
        }

        private static bool TryDeleteCreated(string rel)
        {
            if (!IsSafeCreatedPath(rel)) return false;
            var root = ProjectRootSafe();
            if (string.IsNullOrEmpty(root)) return false;
            var full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar)));
            var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!full.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(full, rootFull, StringComparison.OrdinalIgnoreCase))
                return false;

            var asset = rel.Replace('\\', '/');
            if (asset.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(asset, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                if (AssetDatabase.DeleteAsset(asset))
                    return true;
            }

            try
            {
                if (File.Exists(full))
                {
                    File.Delete(full);
                    TryDeleteFile(full + ".meta");
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void TryDeleteFile(string full)
        {
            try
            {
                if (!string.IsNullOrEmpty(full) && File.Exists(full))
                    File.Delete(full);
            }
            catch
            {
                /* locked or missing */
            }
        }
    }
}
