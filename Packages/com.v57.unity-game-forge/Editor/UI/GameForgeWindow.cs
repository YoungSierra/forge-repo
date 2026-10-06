using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Workbench shell: header + sidebar (Setup/Context/Agent/Chat) + stacked panels.
    /// Chat opens the dockable GameForge Chat window.
    /// </summary>
    public sealed class GameForgeWindow : EditorWindow
    {
        private List<TddInfo> _tdds = new();
        private DropdownField _tddDropdown;
        private ScrollView _mechScroll;
        private VisualElement _mechList;
        private Label _statusLabel;
        private Label _footerIdle;
        private Label _headerConnected;
        private Label _setupReady;
        private Label _providerHint;
        private Label _modeHint;
        private GameForgeModelPicker _picker;
        private readonly List<Button> _modeButtons = new();
        private Label _nodeDot, _envDot, _agentDot, _mcpDot;
        private Label _nodeText, _envText, _agentText, _mcpText;
        private Label _nodeSub, _envSub, _agentSub, _mcpSub;
        private Label _nodeBadge, _envBadge, _agentBadge, _mcpBadge;
        private Button _stopBtn;
        private Button _genBtn;
        private VisualElement _benchmarkPanel;
        private Label _benchmarkBody;
        private Label _contextStatus;
        private Button _openGraphBtn;
        private VisualElement _workbenchPage;
        private VisualElement _pipelinePage;
        private GameForgePipelineGraphView _pipelineGraph;
        private VisualElement _pipelineChat;
        private VisualElement _pipelineChatHost;
        private VisualElement _chatGrip;
        private float _chatWidth = 420f;
        private bool _chatCollapsed;
        private Button _pipeHorizontal;
        private Button _pipeVertical;
        private Button _tabWorkbench;
        private Button _tabPipeline;
        private VisualElement _footer;
        private VisualElement _sidebar;
        private VisualElement _setupPanel;
        private VisualElement _ctxPanel;
        private VisualElement _agentPanel;
        private readonly List<Button> _navButtons = new();
        private ScrollView _workbenchScroll;
        private readonly Dictionary<string, VisualElement> _sectionPanels = new();
        private readonly Dictionary<string, VisualElement> _sectionBodies = new();
        private readonly Dictionary<string, Label> _sectionChevrons = new();

        [MenuItem("V57/GameForge/Workbench %&g", false, 0)]
        public static void Open()
        {
            var win = GetWindow<GameForgeWindow>();
            win.titleContent = new GUIContent("GameForge");
            win.minSize = new Vector2(720, 640);
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            BuildUi(rootVisualElement);
            RefreshHealth();
            RefreshTdds();
            _ = RefreshProvidersAsync();
            GameForgeSession.Changed += OnSessionChanged;
            GameForgeSession.BusyChanged += RefreshBusyUi;
            GameForgePipelineState.Changed += OnPipelineChanged;
            GameForgePipelineState.StopRequested += OnStopRun;
            GameForgePipelineState.RunSetupRequested += OnRunSetupRequested;
            EditorApplication.update += PollPipelineProgress;
            GameForgePipelineState.ReloadFromDisk();
            RefreshBusyUi();
        }

        private void OnDisable()
        {
            GameForgeSession.Changed -= OnSessionChanged;
            GameForgeSession.BusyChanged -= RefreshBusyUi;
            GameForgePipelineState.Changed -= OnPipelineChanged;
            GameForgePipelineState.StopRequested -= OnStopRun;
            GameForgePipelineState.RunSetupRequested -= OnRunSetupRequested;
            EditorApplication.update -= PollPipelineProgress;
        }

        private void RefreshBusyUi()
        {
            var busy = GameForgeSession.IsBusy;
            if (_stopBtn != null)
            {
                _stopBtn.SetEnabled(busy);
                _stopBtn.tooltip = busy
                    ? (GameForgeSession.RemoteBusy && !GameForgeSession.Bridge.IsBusy
                        ? "Stop sidecar run (UI disconnected)"
                        : "Stop")
                    : "Stop (no active run)";
            }
            foreach (var b in _modeButtons)
                b?.SetEnabled(!busy);
            _picker?.SetInteractable(!busy);
            if (!busy)
            {
                GameForgePipelineState.ClearSetupLaunch();
                GameForgePipelineState.ClearIdleRunning();
            }
            if (_footerIdle != null)
                _footerIdle.text = busy ? "Busy" : "Idle";
            RefreshPipeline();
        }

        private void OnPipelineChanged()
        {
            RefreshPipeline();
        }

        private void ShowPage(bool pipeline)
        {
            if (_workbenchPage != null)
                _workbenchPage.style.display = pipeline ? DisplayStyle.None : DisplayStyle.Flex;
            if (_sidebar != null)
                _sidebar.style.display = pipeline ? DisplayStyle.None : DisplayStyle.Flex;
            if (_pipelinePage != null)
                _pipelinePage.style.display = pipeline ? DisplayStyle.Flex : DisplayStyle.None;
            _tabWorkbench?.EnableInClassList("is-on", !pipeline);
            _tabPipeline?.EnableInClassList("is-on", pipeline);
            if (pipeline)
            {
                GameForgePipelineState.ReloadFromDisk();
                RefreshPipeline();
                EnsurePipelineChatHidden();
            }
        }

        private double _nextPipelinePoll;

        private void PollPipelineProgress()
        {
            if (EditorApplication.timeSinceStartup < _nextPipelinePoll) return;
            _nextPipelinePoll = EditorApplication.timeSinceStartup + 1.5;
            if (_pipelinePage == null || _pipelinePage.resolvedStyle.display == DisplayStyle.None) return;
            if (!GameForgeSession.IsBusy && !GameForgePipelineState.AwaitingUser && !GameForgeChatWindow.HasActiveRun) return;
            RefreshPipeline();
        }

        private bool _refreshingPipeline;
        private bool _pipelineRefreshQueued;

        private void RefreshPipeline()
        {
            if (_pipelineGraph == null) return;
            if (_pipelinePage != null && _pipelinePage.style.display == DisplayStyle.None) return;
            if (_refreshingPipeline)
            {
                _pipelineRefreshQueued = true;
                return;
            }
            _refreshingPipeline = true;
            try
            {
                GameForgeChatWindow.SyncPipelineFromLog(notify: false);
                _pipelineGraph.Refresh();
                SyncPipeOrientation();
            }
            finally
            {
                _refreshingPipeline = false;
                if (_pipelineRefreshQueued)
                {
                    _pipelineRefreshQueued = false;
                    EditorApplication.delayCall += RefreshPipeline;
                }
            }
        }

        private void OnSessionChanged()
        {
            GameForgeSession.ApplyThemeClass(rootVisualElement);
            UpdateThemeButtonLabel();
            UpdateModeButtons();
            Repaint();
        }

        private void BuildUi(VisualElement root)
        {
            root.AddToClassList("gf-root");
            root.AddToClassList("gf-shell");
            TryAddStyles(root);
            GameForgeSession.ApplyThemeClass(root);

            if (GameForgeSession.Mode != GameForgeMode.Production)
                GameForgeSession.Mode = GameForgeMode.Production;

            root.Add(BuildHeader());

            var shell = new VisualElement();
            shell.AddToClassList("gf-shell-body");
            root.Add(shell);

            _sidebar = BuildSidebar();
            shell.Add(_sidebar);

            var content = new VisualElement();
            content.AddToClassList("gf-shell-content");
            shell.Add(content);

            _workbenchScroll = new ScrollView(ScrollViewMode.Vertical);
            _workbenchPage = _workbenchScroll;
            _workbenchScroll.AddToClassList("gf-scroll");
            _workbenchScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _workbenchScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            content.Add(_workbenchScroll);

            var body = new VisualElement();
            body.AddToClassList("gf-body");
            _workbenchScroll.Add(body);

            _setupPanel = BuildSetupCard();
            body.Add(_setupPanel);

            _ctxPanel = BuildContextCard();
            body.Add(_ctxPanel);
            RefreshOpenGraphButton();

            _agentPanel = BuildAgentCard();
            body.Add(_agentPanel);

            _benchmarkPanel = null;
            _benchmarkBody = null;

            _pipelinePage = new VisualElement();
            _pipelinePage.AddToClassList("gf-pipeline-page");
            _pipelinePage.style.display = DisplayStyle.None;
            _pipelinePage.Add(BuildPipelineToolbar());
            var split = new VisualElement();
            split.AddToClassList("gf-pipe-split");
            _pipelineGraph = new GameForgePipelineGraphView();
            split.Add(_pipelineGraph);
            _pipelineChat = new VisualElement();
            _pipelineChat.AddToClassList("gf-pipe-chat");
            _pipelineChat.style.display = DisplayStyle.None;
            _chatGrip = new VisualElement();
            _chatGrip.AddToClassList("gf-pipe-chat-grip");
            _chatGrip.tooltip = "Drag to resize · click to collapse";
            var gripMark = new Label("‹");
            gripMark.AddToClassList("gf-pipe-chat-grip-mark");
            gripMark.pickingMode = PickingMode.Ignore;
            _chatGrip.Add(gripMark);
            BindChatResize(_chatGrip);
            _pipelineChat.Add(_chatGrip);
            _pipelineChatHost = new VisualElement();
            _pipelineChatHost.AddToClassList("gf-pipe-chat-host");
            _pipelineChat.Add(_pipelineChatHost);
            split.Add(_pipelineChat);
            _pipelinePage.Add(split);
            content.Add(_pipelinePage);

            var footer = new VisualElement();
            _footer = footer;
            footer.AddToClassList("gf-footer");
            var footLeft = new VisualElement();
            footLeft.AddToClassList("gf-footer-left");
            var footDot = new Label();
            footDot.AddToClassList("gf-footer-dot");
            footLeft.Add(footDot);
            _statusLabel = LabelCls("gf-status", "Ready");
            footLeft.Add(_statusLabel);
            footer.Add(footLeft);
            _footerIdle = LabelCls("gf-footer-idle", "Idle");
            footer.Add(_footerIdle);
            root.Add(footer);

            ShowPage(false);
            SelectNav("setup");
            RefreshPipeline();
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("gf-header");

            var left = new VisualElement();
            left.AddToClassList("gf-header-left");
            var mark = LabelCls("gf-brand-mark", "◆");
            left.Add(mark);
            left.Add(LabelCls("gf-brand-title", "GameForge"));
            var project = LabelCls("gf-project-pill", "V57 GameForge");
            left.Add(project);
            header.Add(left);

            var right = new VisualElement();
            right.AddToClassList("gf-header-right");
            var tabs = new VisualElement();
            tabs.AddToClassList("gf-header-tabs");
            _tabWorkbench = Btn("Workbench", () => ShowPage(false), ghost: true);
            _tabWorkbench.AddToClassList("gf-header-tab");
            _tabPipeline = Btn("Pipeline", () => ShowPage(true), ghost: true);
            _tabPipeline.AddToClassList("gf-header-tab");
            tabs.Add(_tabWorkbench);
            tabs.Add(_tabPipeline);
            right.Add(tabs);

            var connected = new VisualElement();
            connected.AddToClassList("gf-connected-status");
            var cDot = new Label();
            cDot.AddToClassList("gf-connected-dot");
            connected.Add(cDot);
            _headerConnected = LabelCls("gf-connected-text", "Connected");
            connected.Add(_headerConnected);
            right.Add(connected);

            right.Add(MakeThemeBtn());
            header.Add(right);
            return header;
        }

        private VisualElement BuildSidebar()
        {
            var side = new VisualElement();
            side.AddToClassList("gf-sidebar");
            _navButtons.Clear();
            side.Add(MakeNavBtn("⚙", "Setup", "setup", () => FocusSection("setup")));
            side.Add(MakeNavBtn("◷", "Context", "context", () => FocusSection("context")));
            side.Add(MakeNavBtn("◉", "Agent", "agent", () => FocusSection("agent")));
            side.Add(MakeNavBtn("💬", "Chat", "chat", () =>
            {
                SelectNav("chat");
                GameForgeChatWindow.Open();
            }));
            return side;
        }

        private Button MakeNavBtn(string icon, string label, string id, Action onClick)
        {
            var b = new Button(onClick);
            b.AddToClassList("gf-nav-btn");
            b.userData = id;
            var iconL = new Label(icon);
            iconL.AddToClassList("gf-nav-icon");
            iconL.pickingMode = PickingMode.Ignore;
            var textL = new Label(label);
            textL.AddToClassList("gf-nav-label");
            textL.pickingMode = PickingMode.Ignore;
            b.Add(iconL);
            b.Add(textL);
            _navButtons.Add(b);
            return b;
        }

        private void SelectNav(string id)
        {
            foreach (var b in _navButtons)
                b.EnableInClassList("is-active", string.Equals(b.userData as string, id, StringComparison.Ordinal));
        }

        private void FocusSection(string id)
        {
            SelectNav(id);
            ShowPage(false);
            if (!_sectionPanels.TryGetValue(id, out var panel)) return;
            var wasCollapsed = panel.ClassListContains("is-collapsed");
            SetSectionCollapsed(id, !wasCollapsed);
            if (wasCollapsed && _workbenchScroll != null)
                _workbenchScroll.ScrollTo(panel);
        }

        private void SetSectionCollapsed(string id, bool collapsed)
        {
            if (!_sectionPanels.TryGetValue(id, out var panel)) return;
            panel.EnableInClassList("is-collapsed", collapsed);
            if (_sectionBodies.TryGetValue(id, out var body))
                body.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            if (_sectionChevrons.TryGetValue(id, out var chev))
                chev.text = collapsed ? "▸" : "▾";
        }

        private VisualElement BuildSetupCard()
        {
            var card = MakePanelCard("setup", "⚙", "Setup", out var body, out var trailing);
            card.name = "gf-panel-setup";
            _setupReady = LabelCls("gf-panel-meta", "—");
            trailing.Add(_setupReady);

            body.Add(MakeCheck(out _nodeDot, out _nodeText, out _nodeSub, out _nodeBadge));
            body.Add(MakeCheck(out _envDot, out _envText, out _envSub, out _envBadge));
            body.Add(MakeCheck(out _agentDot, out _agentText, out _agentSub, out _agentBadge));
            body.Add(MakeCheck(out _mcpDot, out _mcpText, out _mcpSub, out _mcpBadge));

            var setupRow = new VisualElement();
            setupRow.AddToClassList("gf-action-row");
            setupRow.Add(Btn("API Keys", OpenEnvHelp, ghost: true));
            setupRow.Add(Btn("Bootstrap Agent", () =>
            {
                GameForgeSession.Bridge.Restart();
                GameForgeBootstrap.BootstrapMenu();
                RefreshHealth();
            }, ghost: true));
            setupRow.Add(Btn("MCP Settings", OpenMcpSettings, ghost: true));
            setupRow.Add(Btn("Test MCP", () => _ = OnTestMcpAsync(), ghost: true));
            setupRow.Add(Btn("Refresh", () => { RefreshHealth(); _ = RefreshProvidersAsync(); }, ghost: true));
            body.Add(setupRow);
            return card;
        }

        private VisualElement BuildContextCard()
        {
            var card = MakePanelCard("context", "◷", "Context", out var body, out _);
            card.name = "gf-panel-context";
            card.AddToClassList("gf-context-panel");

            body.Add(LabelCls("gf-field-label", "TDD"));
            var tddRow = new VisualElement();
            tddRow.AddToClassList("gf-tdd-row");
            _tddDropdown = new DropdownField();
            _tddDropdown.AddToClassList("gf-field");
            _tddDropdown.AddToClassList("gf-dropdown");
            _tddDropdown.RegisterValueChangedCallback(_ =>
            {
                SyncSelectedTdd();
                RebuildMechanics();
            });
            tddRow.Add(_tddDropdown);
            tddRow.Add(MakeTddFolderBtn());
            body.Add(tddRow);
            var tddActions = new VisualElement();
            tddActions.AddToClassList("gf-action-row");
            tddActions.Add(Btn("Refresh TDDs", RefreshTdds, ghost: true));
            body.Add(tddActions);

            body.Add(LabelCls("gf-field-label", "MECHANICS"));
            _mechScroll = new ScrollView();
            _mechScroll.AddToClassList("gf-mech-scroll");
            _mechScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _mechScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _mechList = _mechScroll.contentContainer;
            body.Add(_mechScroll);

            _contextStatus = LabelCls("gf-hint", "Post-mortem runs readiness + graph. Open Graph shows the HTML.");
            _contextStatus.style.whiteSpace = WhiteSpace.Normal;
            body.Add(_contextStatus);
            var ctxRow = new VisualElement();
            ctxRow.AddToClassList("gf-action-row");
            ctxRow.Add(Btn("Run Post-Mortem", () => _ = OnContextPostmortemAsync(), primary: true));
            _openGraphBtn = Btn("Open Graph", GameForgeContextGraphWindow.OpenDemo, ghost: true);
            ctxRow.Add(_openGraphBtn);
            body.Add(ctxRow);
            return card;
        }

        private VisualElement BuildAgentCard()
        {
            var card = MakePanelCard("agent", "◉", "Agent", out var body, out _);
            card.name = "gf-panel-agent";

            _picker = new GameForgeModelPicker(null);
            _picker.SelectionChanged += async (provider, model) =>
            {
                GameForgeSession.ProviderId = provider;
                GameForgeSession.ModelId = model;
                GameForgeSession.NotifyChanged();
                await ApplyProviderAsync(provider, model);
            };
            body.Add(_picker.Root);

            _providerHint = LabelCls("gf-hint", "");
            body.Add(_providerHint);

            var modeRow = new VisualElement();
            modeRow.AddToClassList("gf-mode-row");
            modeRow.Add(LabelCls("gf-mode-caption", "Mode"));
            _modeButtons.Clear();
            var modeBtn = MakeModeBtn("Production", GameForgeMode.Production);
            modeBtn.AddToClassList("gf-mode-pill");
            _modeButtons.Add(modeBtn);
            modeRow.Add(modeBtn);
            body.Add(modeRow);
            UpdateModeButtons();

            _modeHint = LabelCls("gf-hint", ModeHint(GameForgeSession.Mode));
            _modeHint.AddToClassList("gf-mode-hint");
            body.Add(_modeHint);

            _genBtn = Btn(BuildButtonLabel(GameForgeSession.Mode), () => _ = OnGenerate(), primary: true);
            _genBtn.AddToClassList("gf-btn-block");
            body.Add(_genBtn);
            UpdateGenerateButton();

            _stopBtn = Btn("Stop", OnStopRun, ghost: true);
            _stopBtn.AddToClassList("gf-btn-block");
            _stopBtn.AddToClassList("gf-btn-stop");
            _stopBtn.tooltip = "Stop the active Generate / Chat run";
            _stopBtn.SetEnabled(false);
            body.Add(_stopBtn);
            return card;
        }

        private VisualElement MakePanelCard(
            string sectionId,
            string icon,
            string title,
            out VisualElement body,
            out VisualElement trailing,
            bool startExpanded = true)
        {
            var root = new VisualElement();
            root.AddToClassList("gf-setup-panel");
            root.AddToClassList("gf-panel-card");

            var head = new VisualElement();
            head.AddToClassList("gf-panel-head");
            head.AddToClassList("gf-panel-head-toggle");
            var chev = LabelCls("gf-panel-chev", startExpanded ? "▾" : "▸");
            chev.pickingMode = PickingMode.Ignore;
            head.Add(chev);
            head.Add(LabelCls("gf-panel-icon", icon));
            head.Add(LabelCls("gf-panel-title", title));
            trailing = new VisualElement();
            trailing.AddToClassList("gf-panel-trailing");
            trailing.pickingMode = PickingMode.Ignore;
            head.Add(trailing);

            body = new VisualElement();
            body.AddToClassList("gf-panel-body");
            body.style.display = startExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (!startExpanded) root.AddToClassList("is-collapsed");

            _sectionPanels[sectionId] = root;
            _sectionBodies[sectionId] = body;
            _sectionChevrons[sectionId] = chev;

            head.RegisterCallback<ClickEvent>(_ => FocusSection(sectionId));
            root.Add(head);
            root.Add(body);
            return root;
        }

        private VisualElement BuildPipelineToolbar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("gf-pipe-tools");

            var left = new VisualElement();
            left.AddToClassList("gf-pipe-tools-cluster");

            var orient = new VisualElement();
            orient.AddToClassList("gf-pipe-seg");
            _pipeHorizontal = MakePipeTool("↔", () => SetPipeOrientation(false), icon: true);
            _pipeHorizontal.tooltip = "Horizontal layout";
            _pipeVertical = MakePipeTool("↕", () => SetPipeOrientation(true), icon: true);
            _pipeVertical.tooltip = "Vertical layout";
            orient.Add(_pipeHorizontal);
            orient.Add(_pipeVertical);
            left.Add(orient);
            left.Add(MakePipeDivider());
            var testDummy = MakePipeTool("Test dummy", () =>
            {
                GameForgePipelineState.ToggleDummy();
                ShowPage(true);
            });
            testDummy.style.display = DisplayStyle.None;
            left.Add(testDummy);
            var clearDummy = MakePipeTool("Clear dummy", () =>
            {
                GameForgePipelineState.ClearDummy();
                RefreshPipeline();
            });
            clearDummy.style.display = DisplayStyle.None;
            left.Add(clearDummy);
            var clearProgress = MakePipeTool("Clear progress", () =>
            {
                if (!GameForgePipelineState.ClearProgress()) return;
                RefreshPipeline();
                SetStatus("Created files and pipeline checkpoint deleted. Next Run game setup starts at Confirm variables.", StatusKind.Warn);
            });
            clearProgress.tooltip = "Warn, then delete files created during game setup and forget the checkpoint so the next run starts from zero.";
            left.Add(clearProgress);

            var spacer = new VisualElement();
            spacer.AddToClassList("gf-pipe-tools-spacer");

            bar.Add(left);
            bar.Add(spacer);
            SyncPipeOrientation();
            return bar;
        }

        private static VisualElement MakePipeDivider()
        {
            var line = new VisualElement();
            line.AddToClassList("gf-pipe-divider");
            return line;
        }

        private static Button MakePipeTool(string text, Action onClick, bool primary = false, bool icon = false)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("gf-pipe-tool");
            if (primary) b.AddToClassList("gf-pipe-tool-primary");
            if (icon) b.AddToClassList("gf-pipe-tool-icon");
            return b;
        }

        private void OnRunSetupRequested() => _ = RunGameSetupFromPipeline();

        private void EnsurePipelineChatHidden()
        {
            if (_pipelineChat == null || _pipelineChatHost == null) return;
            _pipelineChat.style.display = DisplayStyle.Flex;
            _chatCollapsed = true;
            ApplyChatWidth();
            GameForgeChatWindow.MountInto(_pipelineChatHost);
        }

        private void ShowPipelineChat()
        {
            if (_pipelineChat == null || _pipelineChatHost == null) return;
            _pipelineChat.style.display = DisplayStyle.Flex;
            _chatCollapsed = false;
            ApplyChatWidth();
            GameForgeChatWindow.MountInto(_pipelineChatHost);
        }

        private void TogglePipelineChat()
        {
            if (_pipelineChat == null || _pipelineChat.style.display == DisplayStyle.None)
            {
                ShowPipelineChat();
                return;
            }

            _chatCollapsed = !_chatCollapsed;
            ApplyChatWidth();
        }

        private void ApplyChatWidth()
        {
            if (_pipelineChat == null) return;
            if (_chatCollapsed)
            {
                _pipelineChat.AddToClassList("is-collapsed");
                _pipelineChat.style.width = 18;
                if (_pipelineChatHost != null)
                    _pipelineChatHost.style.display = DisplayStyle.None;
                if (_chatGrip != null)
                {
                    var mark = _chatGrip.Q<Label>(className: "gf-pipe-chat-grip-mark");
                    if (mark != null) mark.text = "›";
                    _chatGrip.tooltip = "Expand chat";
                }
                return;
            }

            _pipelineChat.RemoveFromClassList("is-collapsed");
            _chatWidth = Mathf.Clamp(_chatWidth, 280f, 720f);
            _pipelineChat.style.width = _chatWidth;
            if (_pipelineChatHost != null)
                _pipelineChatHost.style.display = DisplayStyle.Flex;
            if (_chatGrip != null)
            {
                var mark = _chatGrip.Q<Label>(className: "gf-pipe-chat-grip-mark");
                if (mark != null) mark.text = "‹";
                _chatGrip.tooltip = "Drag to resize · click to collapse";
            }
        }

        private void BindChatResize(VisualElement grip)
        {
            var tracking = false;
            var moved = false;
            var origin = 0f;
            var start = 0f;
            grip.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                tracking = true;
                moved = false;
                origin = evt.position.x;
                start = _chatWidth;
                grip.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            grip.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!tracking || !grip.HasPointerCapture(evt.pointerId) || _chatCollapsed) return;
                var dx = evt.position.x - origin;
                if (Mathf.Abs(dx) < 3f) return;
                moved = true;
                _chatWidth = start - dx;
                ApplyChatWidth();
            });
            grip.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!tracking) return;
                tracking = false;
                grip.ReleasePointer(evt.pointerId);
                if (!moved)
                    TogglePipelineChat();
            });
        }

        private void SetPipeOrientation(bool vertical)
        {
            GameForgePipelineState.Vertical = vertical;
            RefreshPipeline();
        }

        private void SyncPipeOrientation()
        {
            // Active segment follows the current graph orientation.
            var vertical = GameForgePipelineState.Vertical;
            _pipeHorizontal?.EnableInClassList("is-on", !vertical);
            _pipeVertical?.EnableInClassList("is-on", vertical);
        }

        private void ToggleTheme()
        {
            GameForgeSession.ToggleUiTheme();
            UpdateThemeButtonLabel();
        }

        private void UpdateThemeButtonLabel()
        {
            var b = rootVisualElement.Q<Button>(className: "gf-theme-toggle");
            if (b == null) return;
            b.text = ThemeButtonLabel();
            b.tooltip = GameForgeSession.IsDarkTheme ? "Switch to light theme" : "Switch to dark theme";
        }

        private static string ThemeButtonLabel() =>
            GameForgeSession.IsDarkTheme ? "☀" : "☾";

        private Button MakeThemeBtn()
        {
            var b = Btn(ThemeButtonLabel(), ToggleTheme, ghost: true);
            b.AddToClassList("gf-icon-btn");
            b.AddToClassList("gf-theme-toggle");
            b.tooltip = GameForgeSession.IsDarkTheme ? "Switch to light theme" : "Switch to dark theme";
            return b;
        }

        private static Button MakeTddFolderBtn()
        {
            var b = Btn("📁", OpenTddsFolder, ghost: true);
            b.AddToClassList("gf-icon-btn");
            b.AddToClassList("gf-tdd-folder");
            b.tooltip = "Open Docs/tdds — one folder per game; any .md TDD file inside (TDD.md not required)";
            return b;
        }

        private static void OpenTddsFolder()
        {
            var dir = TddParser.TddsRoot;
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        private Button MakeModeBtn(string label, GameForgeMode mode, bool enabled = true)
        {
            var btn = new Button(() =>
            {
                if (!enabled) return;
                GameForgeSession.Mode = mode;
                GameForgeSession.NotifyChanged();
                UpdateModeButtons();
                UpdateGenerateButton();
                if (_modeHint != null) _modeHint.text = ModeHint(mode);
            }) { text = label };
            btn.AddToClassList("gf-seg-btn");
            btn.userData = mode;
            if (!enabled)
            {
                btn.SetEnabled(false);
                btn.tooltip = "Temporarily disabled — Production only";
            }
            return btn;
        }

        private void UpdateModeButtons()
        {
            foreach (var b in _modeButtons)
            {
                var mode = (GameForgeMode)b.userData;
                b.EnableInClassList("gf-seg-btn-active", mode == GameForgeSession.Mode);
            }

            if (_modeHint != null) _modeHint.text = ModeHint(GameForgeSession.Mode);
            UpdateGenerateButton();
        }

        private void UpdateGenerateButton()
        {
            if (_genBtn == null) return;
            var mode = GameForgeSession.Mode;
            _genBtn.text = BuildButtonLabel(mode);
            _genBtn.tooltip = mode switch
            {
                GameForgeMode.Prototype => "Run V57 /prototype for the selected TDD",
                GameForgeMode.VerticalSlice => "Run V57 /vertical-slice for the selected TDD",
                GameForgeMode.Production => "Run V57 /game-setup for the selected TDD",
                _ => "Build"
            };
        }

        private static string BuildButtonLabel(GameForgeMode mode) => mode switch
        {
            GameForgeMode.VerticalSlice => "Build Vertical Slice",
            GameForgeMode.Production => "Build Production",
            _ => "Build Prototype"
        };

        private static string ModeHint(GameForgeMode mode) => mode switch
        {
            GameForgeMode.VerticalSlice => "Playable E2E loop — V57 /vertical-slice.",
            GameForgeMode.Production => "Full SDD pipeline — V57 /game-setup.",
            _ => "Fast graybox spike — V57 /prototype."
        };

        private static VisualElement MakeCheck(out Label icon, out Label title, out Label subtitle, out Label badge)
        {
            var row = new VisualElement();
            row.AddToClassList("gf-check-row");

            icon = new Label("✓");
            icon.AddToClassList("gf-check-icon");
            row.Add(icon);

            var col = new VisualElement();
            col.AddToClassList("gf-check-col");
            title = new Label();
            title.AddToClassList("gf-check-label");
            subtitle = new Label();
            subtitle.AddToClassList("gf-check-sub");
            col.Add(title);
            col.Add(subtitle);
            row.Add(col);

            badge = LabelCls("gf-check-badge", "—");
            row.Add(badge);
            return row;
        }

        private static Label LabelCls(string cls, string text)
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            return l;
        }

        private static Button Btn(string text, Action onClick, bool primary = false, bool ghost = false)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("gf-btn");
            if (primary) b.AddToClassList("gf-btn-primary");
            if (ghost) b.AddToClassList("gf-btn-ghost");
            return b;
        }

        private void RefreshHealth()
        {
            var nodeOk = GameForgeBootstrap.IsNodeAvailable(out var nodePath);
            SetCheck(_nodeDot, _nodeText, _nodeSub, _nodeBadge, nodeOk,
                nodeOk ? "Node.js detected" : "Node.js 22+ missing",
                nodeOk ? Trunc(nodePath, 64) : "Install Node.js 22+");

            var envPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".v57", "gameforge.env");
            var envLocal = Path.Combine(GameForgeBootstrap.ProjectRoot, ".env.local");
            var envOk = File.Exists(envPath) || File.Exists(envLocal);
            SetCheck(_envDot, _envText, _envSub, _envBadge, envOk,
                envOk ? "API keys file found" : "Missing API keys file",
                envOk ? (File.Exists(envLocal) ? ".env.local" : "~/.v57/gameforge.env") : "Create ~/.v57/gameforge.env");

            var agentOk = GameForgeBootstrap.IsAgentInstalled;
            SetCheck(_agentDot, _agentText, _agentSub, _agentBadge, agentOk,
                agentOk ? "Agent package installed" : "Agent package missing",
                agentOk ? "Connected to Unity Editor" : "Run Bootstrap Agent");

            var mcpOk = IsUnityMcpPackagePresent(out var mcpDetail);
            var mcpTitle = mcpOk ? "Unity CLI · Pipeline" : "Unity CLI missing";
            var mcpSub = mcpDetail;
            if (mcpOk && mcpDetail.Contains("Pipeline", StringComparison.Ordinal))
            {
                var dash = mcpDetail.IndexOf(" — ", StringComparison.Ordinal);
                if (dash > 0)
                {
                    mcpTitle = mcpDetail.Substring(0, dash);
                    mcpSub = mcpDetail.Substring(dash + 3);
                }
            }
            SetCheck(_mcpDot, _mcpText, _mcpSub, _mcpBadge, mcpOk, mcpTitle, mcpSub);
            UpdateSetupReadyCount();
        }

        private void UpdateSetupReadyCount()
        {
            if (_setupReady == null) return;
            var ok = 0;
            if (_nodeDot != null && _nodeDot.ClassListContains("gf-check-icon-ok")) ok++;
            if (_envDot != null && _envDot.ClassListContains("gf-check-icon-ok")) ok++;
            if (_agentDot != null && _agentDot.ClassListContains("gf-check-icon-ok")) ok++;
            if (_mcpDot != null && _mcpDot.ClassListContains("gf-check-icon-ok")) ok++;
            _setupReady.text = $"{ok}/4 ready";
        }

        private static bool IsUnityMcpPackagePresent(out string detail)
        {
            try
            {
                string pipelineVersion = null;
                string legacyVersion = null;
                foreach (var info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                {
                    if (info.name == "com.unity.pipeline")
                        pipelineVersion = info.version;
                    else if (info.name == "com.unity.ai.assistant")
                        legacyVersion = info.version;
                }

                if (!string.IsNullOrEmpty(pipelineVersion))
                {
                    detail = $"Unity CLI · Pipeline {pipelineVersion} — install CLI + `unity mcp configure cursor`";
                    return true;
                }

                if (!string.IsNullOrEmpty(legacyVersion))
                {
                    detail = $"Legacy MCP {legacyVersion} (deprecated) — run `unity pipeline install`; see UNITY_CLI_MIGRATION.md";
                    return true;
                }

                detail = "Missing com.unity.pipeline — install Unity CLI and run `unity pipeline install`";
                return false;
            }
            catch (Exception ex)
            {
                detail = "Package Manager query failed: " + ex.Message;
                return false;
            }
        }

        private void RefreshTdds()
        {
            _tdds = TddParser.ListTdds();
            var choices = new List<string>();
            foreach (var t in _tdds) choices.Add($"{t.slug} — {t.projectName}");
            if (_tddDropdown != null)
            {
                _tddDropdown.choices = choices;
                if (choices.Count > 0)
                    SyncDropdownFromSession();
            }

            SyncSelectedTdd();
            RebuildMechanics();
            SetStatus(_tdds.Count == 0 ? "No TDDs in Docs/tdds" : $"{_tdds.Count} TDD(s)", _tdds.Count == 0 ? StatusKind.Warn : StatusKind.Ok);
        }

        /// <summary>Align dropdown UI with persisted session — never overwrite session on refresh.</summary>
        private void SyncDropdownFromSession()
        {
            if (_tddDropdown == null || _tddDropdown.choices == null || _tddDropdown.choices.Count == 0)
                return;

            var slug = GameForgeSession.SelectedSlug;
            if (!string.IsNullOrEmpty(slug))
            {
                var prefer = _tddDropdown.choices.Find(c => c.StartsWith(slug + " ", StringComparison.Ordinal));
                if (prefer != null)
                    _tddDropdown.SetValueWithoutNotify(prefer);
                return;
            }

            _tddDropdown.SetValueWithoutNotify(_tddDropdown.choices[0]);
        }

        private void SyncSelectedTdd()
        {
            var tdd = SelectedTddFromDropdown();
            GameForgeSession.SetSelectedTdd(tdd?.slug ?? "", tdd?.projectName ?? "");
            if (!string.IsNullOrEmpty(tdd?.slug))
                GameForgeProjectIdentity.SyncPlayerSettingsFromSlug(tdd.slug);
        }

        private void RebuildMechanics()
        {
            if (_mechList == null) return;
            _mechList.Clear();
            var tdd = SelectedTdd();
            if (tdd == null || tdd.mechanics.Count == 0)
            {
                _mechList.Add(LabelCls("gf-hint", "No mechanics parsed from TDD."));
                return;
            }

            foreach (var m in tdd.mechanics)
            {
                var row = new VisualElement();
                row.AddToClassList("gf-mech-row");
                var name = LabelCls("gf-mech-name", m.title);
                var badge = LabelCls("gf-badge", string.IsNullOrEmpty(m.type) ? "feature" : m.type);
                row.Add(name);
                row.Add(badge);
                _mechList.Add(row);
            }
        }

        private TddInfo SelectedTdd()
        {
            if (_tdds.Count == 0) return null;

            var slug = GameForgeSession.SelectedSlug;
            if (!string.IsNullOrEmpty(slug))
            {
                var bySlug = _tdds.Find(t => string.Equals(t.slug, slug, StringComparison.Ordinal));
                if (bySlug != null) return bySlug;
            }

            return SelectedTddFromDropdown();
        }

        private TddInfo SelectedTddFromDropdown()
        {
            if (_tddDropdown == null || _tdds.Count == 0) return null;
            var idx = _tddDropdown.index;
            if (idx < 0 || idx >= _tdds.Count) return _tdds[0];
            return _tdds[idx];
        }

        private async System.Threading.Tasks.Task RefreshProvidersAsync()
        {
            try
            {
                var json = await GameForgeSession.Bridge.GetAsync("/api/agent/providers");
                var catalog = GameForgeModelPicker.ParseStatusJson(json);
                _picker?.SetCatalog(catalog);
                if (!string.IsNullOrEmpty(catalog.active))
                    GameForgeSession.ProviderId = catalog.active;
                if (!string.IsNullOrEmpty(catalog.model))
                    GameForgeSession.ModelId = catalog.model;

                if (_providerHint != null)
                {
                    _providerHint.RemoveFromClassList("gf-hint-danger");
                    if (!catalog.configured)
                    {
                        _providerHint.AddToClassList("gf-hint-danger");
                        _providerHint.text = catalog.missingHint ?? "No API keys configured.";
                    }
                    else
                    {
                        _providerHint.text = $"Active: {catalog.active} · {catalog.model}";
                    }
                }

                SetCheck(_agentDot, _agentText, _agentSub, _agentBadge, true,
                    "Agent sidecar reachable", "Connected to Unity Editor");
                SetStatus("Agent connected", StatusKind.Ok);
            }
            catch (Exception ex)
            {
                _picker?.SetCatalog(new ProviderCatalogState
                {
                    configured = false,
                    missingHint =
                        "Agent offline — click Bootstrap Agent. " + Trunc(ex.Message, 100)
                });
                SetCheck(_agentDot, _agentText, _agentSub, _agentBadge, false,
                    "Agent offline", Trunc(ex.Message, 80));
                if (_providerHint != null)
                {
                    _providerHint.AddToClassList("gf-hint-danger");
                    _providerHint.text = Trunc(ex.Message, 120);
                }

                SetStatus("Agent offline: " + Trunc(ex.Message, 80), StatusKind.Bad);
            }
        }

        private async System.Threading.Tasks.Task ApplyProviderAsync(string provider, string model)
        {
            try
            {
                var body = $"{{\"id\":\"{provider}\",\"model\":\"{model}\"}}";
                await GameForgeSession.Bridge.PostJsonAsync("/api/agent/provider", body);
                SetStatus($"Provider → {provider} · {model}", StatusKind.Ok);
            }
            catch (Exception ex)
            {
                SetStatus("Provider switch failed: " + ex.Message, StatusKind.Bad);
            }
        }

        private async System.Threading.Tasks.Task RunGameSetupFromPipeline()
        {
            if (SelectedTdd() == null)
            {
                GameForgePipelineState.ClearSetupLaunch();
                ShowPage(false);
                SetStatus("Select a TDD first.", StatusKind.Warn);
                return;
            }

            if (GameForgePipelineState.DummyRunning)
                GameForgePipelineState.ClearDummy();

            if (GameForgeSession.IsBusy)
            {
                SetStatus("Already running — press Stop first.", StatusKind.Warn);
                ShowPage(true);
                EnsurePipelineChatHidden();
                return;
            }

            SyncSelectedTdd();
            GameForgeSession.Mode = GameForgeMode.Production;
            GameForgeSession.GameSetupRunAll = false;
            if (GameForgeSession.ChatMode != "agent")
                GameForgeSession.ChatMode = "agent";
            GameForgeSession.NotifyChanged();
            UpdateModeButtons();
            UpdateGenerateButton();
            ShowPage(true);
            EnsurePipelineChatHidden();
            var resume = GameForgePipelineState.HasCheckpoint;
            var command = GameForgePipelineState.NextSetupCommand;
            SetStatus(resume ? "Resuming saved pipeline…" : "Starting game setup from Confirm variables…", StatusKind.Ok);
            GameForgeChatWindow.SubmitSlash(command);
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task OnGenerate()
        {
            var tdd = SelectedTdd();
            if (tdd == null)
            {
                SetStatus("Select a TDD first.", StatusKind.Warn);
                return;
            }

            if (GameForgeSession.IsBusy)
            {
                SetStatus("Already running — press Stop first.", StatusKind.Warn);
                return;
            }

            // Generate always runs as Agent (Ask/Plan are read-only)
            if (GameForgeSession.ChatMode != "agent")
            {
                GameForgeSession.ChatMode = "agent";
                GameForgeSession.NotifyChanged();
            }

            var buildLabel = BuildButtonLabel(GameForgeSession.Mode);
            GameForgeChatWindow.Open();
            GameForgeChatWindow.AppendExternal(
                "you",
                $"{buildLabel} · {tdd.slug}");
            GameForgeChatWindow.ShowRunBanner(
                $"{buildLabel} · {tdd.slug}…",
                GameForgeSession.ModeLabel(GameForgeSession.Mode));
            GameForgeChatWindow.StartThinking($"{buildLabel}…");
            SetStatus(buildLabel + "…", StatusKind.Ok);
            RefreshBusyUi();

            try
            {
                GameForgeProjectIdentity.SyncPlayerSettingsFromSlug(tdd.slug);

                var slugJson = EscapeJson(tdd.slug);
                var modeJson = EscapeJson(GameForgeSession.ModeLabel(GameForgeSession.Mode));

                if (GameForgeSession.Mode == GameForgeMode.Production)
                {
                    var chatBody =
                        $"{{\"slug\":\"{slugJson}\",\"message\":\"/game-setup\",\"chatMode\":\"agent\",\"forgeMode\":\"{modeJson}\"}}";
                    await GameForgeSession.Bridge.KickOffPostSseAsync(
                        "/api/sessions/chat",
                        chatBody,
                        default);
                }
                else
                {
                    var body =
                        $"{{\"slug\":\"{slugJson}\",\"forgeMode\":\"{modeJson}\",\"qualityNotes\":\"\"}}";
                    await GameForgeSession.Bridge.KickOffPostSseAsync(
                        "/api/sessions/generate-final",
                        body,
                        default);
                }

                SetStatus(buildLabel + " started — see Chat.", StatusKind.Ok);
            }
            catch (OperationCanceledException)
            {
                if (!GameForgeSession.ReattachSuppressed)
                {
                    GameForgeChatWindow.AppendExternal("notice", buildLabel + " stopped.");
                    GameForgeChatWindow.FinishThinking(cancelled: true);
                }
                SetStatus(buildLabel + " stopped.", StatusKind.Warn);
            }
            catch (Exception ex) when (IsLikelyUserCancel(ex))
            {
                SetStatus(buildLabel + " stopped.", StatusKind.Warn);
            }
            catch (Exception ex)
            {
                if (!GameForgeSession.ReattachSuppressed)
                {
                    GameForgeChatWindow.AppendExternal("error", ex.Message);
                    GameForgeChatWindow.FinishThinking(cancelled: true);
                }
                SetStatus(buildLabel + " failed: " + Trunc(ex.Message, 80), StatusKind.Bad);
            }
            finally
            {
                if (!GameForgeSession.IsBusy)
                    GameForgeChatWindow.HideRunBanner();
                RefreshBusyUi();
            }
        }

        private void OnStopRun()
        {
            EnsurePipelineChatHidden();
            GameForgeChatWindow.UpdateRunBanner("Stopping…");
            SetStatus("Stopping…", StatusKind.Warn);
            GameForgeSession.BeginStop();
            GameForgeChatWindow.FinishThinking(cancelled: true);
            GameForgeChatWindow.HideRunBanner();
            SetStatus("Stopped.", StatusKind.Warn);
            RefreshBusyUi();
        }

        private async System.Threading.Tasks.Task OnSyncPreview()
        {
            try
            {
                var tdd = SelectedTdd();
                var body = $"{{\"slug\":\"{tdd?.slug}\"}}";
                var res = await GameForgeSession.Bridge.PostJsonAsync("/api/sessions/sync-tdd/preview", body);
                GameForgeChatWindow.Open();
                GameForgeChatWindow.AppendExternal("sync", Trunc(res, 600));
                SetStatus("Sync preview done", StatusKind.Ok);
            }
            catch (Exception ex)
            {
                SetStatus("Sync failed: " + ex.Message, StatusKind.Bad);
            }
        }

        private async System.Threading.Tasks.Task OnShowBenchmarkAsync()
        {
            try
            {
                var json = await GameForgeSession.Bridge.GetAsync("/api/benchmark");
                if (_benchmarkPanel != null) _benchmarkPanel.style.display = DisplayStyle.Flex;
                if (_benchmarkBody != null) _benchmarkBody.text = FormatBenchmark(json);
                SetStatus("Benchmark loaded", StatusKind.Ok);
            }
            catch (Exception ex)
            {
                SetStatus("Benchmark failed: " + Trunc(ex.Message, 80), StatusKind.Bad);
            }
        }

        private async System.Threading.Tasks.Task OnClearBenchmarkAsync()
        {
            try
            {
                await GameForgeSession.Bridge.DeleteAsync("/api/benchmark");
                if (_benchmarkPanel != null) _benchmarkPanel.style.display = DisplayStyle.Flex;
                if (_benchmarkBody != null) _benchmarkBody.text = "(no runs yet)";
                SetStatus("Benchmark history cleared", StatusKind.Ok);
            }
            catch (Exception ex)
            {
                SetStatus("Clear benchmark failed: " + Trunc(ex.Message, 80), StatusKind.Bad);
            }
        }

        private async System.Threading.Tasks.Task OnContextPostmortemAsync()
        {
            if (_contextStatus != null)
                _contextStatus.text = "Restarting agent + running post-mortem…";
            SetStatus("Context post-mortem…", StatusKind.Ok);
            try
            {
                // Sidecar keeps old JS in memory — restart so /api/context/postmortem exists.
                GameForgeSession.Bridge.Restart();
                var json = await GameForgeSession.Bridge.PostJsonAsync("/api/context/postmortem", "{}");
                var readiness = ExtractJsonString(json, "readiness") ?? "?";
                var phase2 = ExtractJsonString(json, "phase2") ?? "?";
                var summary = ExtractJsonString(json, "summary") ?? Trunc(json, 200);
                if (_contextStatus != null)
                    _contextStatus.text = summary;
                GameForgeChatWindow.AppendExternal(
                    "notice",
                    $"Context post-mortem · {readiness} · {phase2}");
                SetStatus($"Post-mortem {readiness}", StatusKind.Ok);
            }
            catch (Exception ex)
            {
                var msg = Trunc(ex.Message, 160);
                if (msg.IndexOf("404", System.StringComparison.Ordinal) >= 0)
                    msg = "Agent missing /api/context/postmortem — click Bootstrap Agent, then retry.";
                if (_contextStatus != null) _contextStatus.text = "Failed: " + msg;
                SetStatus("Post-mortem failed: " + Trunc(msg, 80), StatusKind.Bad);
            }
            finally
            {
                RefreshOpenGraphButton();
            }
        }

        private void RefreshOpenGraphButton()
        {
            var exists = GameForgeContextGraphWindow.GraphExists();
            if (_openGraphBtn == null) return;
            _openGraphBtn.SetEnabled(exists);
            _openGraphBtn.tooltip = exists
                ? "Open context graph HTML in the browser"
                : "No graph yet — run Post-Mortem first";
        }

        private static string FormatBenchmark(string json)
        {
            if (string.IsNullOrEmpty(json)) return "(empty)";
            var last = ExtractJsonObject(json, "last");
            if (string.IsNullOrEmpty(last) || last == "null")
                return "No benchmark yet. Run Generate or Chat once.";

            var provider = ExtractJsonString(last, "providerId")
                           ?? ExtractJsonString(last, "provider")
                           ?? "?";
            var model = ExtractJsonString(last, "model") ?? "?";
            var status = ExtractJsonString(last, "status") ?? "?";
            var op = ExtractJsonString(last, "op") ?? "?";
            var slug = ExtractJsonString(last, "slug") ?? "";
            var ms = ExtractJsonInt(last, "durationMs");
            var turns = ExtractJsonInt(last, "turns");
            var tools = ExtractJsonInt(last, "tools");
            var files = ExtractJsonInt(last, "files");

            return
                $"Last · {status}\n" +
                $"{provider} / {model}\n" +
                $"op={op}" + (string.IsNullOrEmpty(slug) ? "" : $" · {slug}") + "\n" +
                (ms >= 0 ? $"duration={ms}ms" : "duration=?") +
                (turns >= 0 ? $" · turns={turns}" : "") +
                (tools >= 0 ? $" · tools={tools}" : "") +
                (files >= 0 ? $" · files={files}" : "");
        }

        private static string ExtractJsonObject(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return null;
            var colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return null;
            var p = colon + 1;
            while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
            if (p >= json.Length) return null;
            if (json[p] == 'n') return "null";
            if (json[p] != '{') return null;
            var depth = 0;
            for (var j = p; j < json.Length; j++)
            {
                var c = json[j];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(p, j - p + 1);
                }
            }
            return null;
        }

        private enum StatusKind { Ok, Warn, Bad }

        private void SetStatus(string s, StatusKind kind)
        {
            if (_statusLabel == null) return;
            _statusLabel.text = s;
            _statusLabel.EnableInClassList("gf-status-ok", kind == StatusKind.Ok);
            _statusLabel.EnableInClassList("gf-status-warn", kind == StatusKind.Warn);
            _statusLabel.EnableInClassList("gf-status-bad", kind == StatusKind.Bad);
            if (_headerConnected != null)
            {
                if (kind == StatusKind.Ok && s.IndexOf("Agent connected", StringComparison.OrdinalIgnoreCase) >= 0)
                    _headerConnected.text = "Connected";
                else if (kind == StatusKind.Bad && s.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0)
                    _headerConnected.text = "Offline";
            }
            Repaint();
        }

        private void SetCheck(Label icon, Label title, Label subtitle, Label badge, bool ok, string titleText, string subText)
        {
            if (icon == null || title == null) return;
            icon.text = ok ? "✓" : "!";
            icon.EnableInClassList("gf-check-icon-ok", ok);
            icon.EnableInClassList("gf-check-icon-bad", !ok);
            title.text = titleText ?? "";
            if (subtitle != null) subtitle.text = subText ?? "";
            if (badge != null)
            {
                badge.text = ok ? "Ready" : "Fix";
                badge.EnableInClassList("is-ready", ok);
                badge.EnableInClassList("is-bad", !ok);
            }
            UpdateSetupReadyCount();
        }

        private static void OpenEnvHelp()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".v57", "gameforge.env");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(path))
            {
                var example = Path.Combine(GameForgeBootstrap.ProjectRoot, "gameforge.env.example");
                if (File.Exists(example)) File.Copy(example, path);
                else File.WriteAllText(path, "CURSOR_API_KEY=\nAGENT_PROVIDER=cursor\n");
            }

            EditorUtility.RevealInFinder(path);
        }

        private static void OpenMcpSettings()
        {
            var doc = Path.Combine(GameForgeBootstrap.ProjectRoot, "V57", "docs", "mcp", "UNITY_CLI_MIGRATION.md");
            if (File.Exists(doc))
                EditorUtility.RevealInFinder(doc);
            else
                GameForgeMcpApprovalUi.Prompt(force: true);
        }

        private async System.Threading.Tasks.Task OnTestMcpAsync()
        {
            SetStatus("Testing Unity MCP…", StatusKind.Warn);
            try
            {
                var json = await GameForgeSession.Bridge.PostJsonAsync("/api/mcp/ping", "{}");
                var ok = json.IndexOf("\"ok\":true", StringComparison.Ordinal) >= 0
                         || json.IndexOf("\"ok\": true", StringComparison.Ordinal) >= 0;
                var toolCount = ExtractJsonInt(json, "toolCount");
                var err = ExtractJsonString(json, "error");
                var hint = ExtractJsonString(json, "hint");
                var revoked = json.IndexOf("\"revoked\":true", StringComparison.Ordinal) >= 0
                              || json.IndexOf("\"revoked\": true", StringComparison.Ordinal) >= 0;

                var disabled = json.IndexOf("\"disabled\":true", StringComparison.Ordinal) >= 0
                               || json.IndexOf("\"disabled\": true", StringComparison.Ordinal) >= 0;

                var mode = ExtractJsonString(json, "mode");
                var cliAvailable = json.IndexOf("\"cliAvailable\":true", StringComparison.Ordinal) >= 0
                                   || json.IndexOf("\"cliAvailable\": true", StringComparison.Ordinal) >= 0;

                if (disabled)
                {
                    var detail = "Unity MCP off in Forge Chat (GAMEFORGE_UNITY_MCP=0)";
                    SetCheck(_mcpDot, _mcpText, _mcpSub, _mcpBadge, false, "Unity MCP disabled", detail);
                    SetStatus(detail, StatusKind.Warn);
                    GameForgeChatWindow.AppendExternal("notice",
                        detail + " — unset or set GAMEFORGE_UNITY_MCP=1 to re-enable.");
                    return;
                }

                if (ok)
                {
                    var modeLabel = string.IsNullOrEmpty(mode) ? "MCP" : mode.ToUpperInvariant();
                    var capacityHint = toolCount >= 0 && toolCount < 15 && mode == "legacy"
                        ? " · few tools (legacy Capacity limit — migrate to Unity CLI)"
                        : toolCount >= 0 && toolCount < 15
                            ? " · few tools (Editor open + pipeline installed?)"
                            : "";
                    var detail = toolCount >= 0
                        ? $"Unity {modeLabel} live · {toolCount} tool(s) · GameForge Chat{capacityHint}"
                        : $"Unity {modeLabel} live · GameForge Chat";
                    SetCheck(_mcpDot, _mcpText, _mcpSub, _mcpBadge, true, $"Unity {modeLabel} live", Trunc(detail, 140));
                    SetStatus(Trunc(detail, 120), StatusKind.Ok);
                    GameForgeChatWindow.AppendExternal("notice", detail);
                    if (!string.IsNullOrEmpty(hint))
                        GameForgeChatWindow.AppendExternal("notice", hint);
                }
                else
                {
                    var detail = !string.IsNullOrEmpty(err) ? Trunc(err, 100) : "Unity MCP not responding";
                    if (revoked && mode == "legacy") detail = "Revoked/Pending — Allow «GameForge Chat» or migrate to Unity CLI";
                    else if (revoked) detail = "MCP connect failed — ensure Editor is open on this project";
                    else if (!cliAvailable && mode != "legacy") detail = "Unity CLI not found — install CLI + `unity pipeline install`";
                    SetCheck(_mcpDot, _mcpText, _mcpSub, _mcpBadge, false, "Unity MCP issue", detail);
                    SetStatus(detail, StatusKind.Bad);
                    var msg = detail;
                    if (!string.IsNullOrEmpty(hint)) msg += "\n" + hint;
                    GameForgeChatWindow.AppendExternal("error", msg);
                    GameForgeMcpApprovalUi.Prompt(msg, force: true);
                }
            }
            catch (Exception ex)
            {
                SetCheck(_mcpDot, _mcpText, _mcpSub, _mcpBadge, false, "MCP ping failed", "Bootstrap Agent?");
                SetStatus("MCP ping failed: " + Trunc(ex.Message, 80), StatusKind.Bad);
                GameForgeChatWindow.AppendExternal("error", "MCP ping failed: " + ex.Message);
            }
        }

        private static int ExtractJsonInt(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return -1;
            var key = $"\"{field}\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return -1;
            var colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return -1;
            var p = colon + 1;
            while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
            var end = p;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            if (end == p) return -1;
            return int.TryParse(json.Substring(p, end - p), out var n) ? n : -1;
        }

        private static string ExtractJsonString(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return null;
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
                    sb.Append(json[++p]);
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static void TryAddStyles(VisualElement root)
        {
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GameForgeWindow).Assembly);
                if (info != null)
                {
                    var path = System.IO.Path.Combine(info.assetPath, "UI/GameForge.uss").Replace('\\', '/');
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

        private static bool IsLikelyUserCancel(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is OperationCanceledException) return true;
                var msg = cur.Message ?? "";
                if (msg.IndexOf("transport connection", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (msg.IndexOf("operation was canceled", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (msg.IndexOf("anuló", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return GameForgeSession.ReattachSuppressed;
        }

        private static string EscapeJson(string s) => GameForgeJson.Escape(s);

        /// <summary>Turn SSE / JSON agent payloads into a readable chat line.</summary>
        private static (string role, string text) FormatAgentPayload(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return ("system", "(empty response)");

            var errors = new List<string>();
            var statuses = new List<string>();
            var assistants = new List<string>();

            foreach (var line in raw.Replace("\r\n", "\n").Split('\n'))
            {
                var t = line.Trim();
                if (t.Length == 0) continue;
                if (t.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    t = t.Substring(5).Trim();
                if (string.IsNullOrEmpty(t) || t == "[DONE]") continue;

                var type = JsonStringField(t, "type");
                if (string.IsNullOrEmpty(type)) continue;
                switch (type)
                {
                    case "error":
                        errors.Add(JsonStringField(t, "error") ?? t);
                        break;
                    case "status":
                        statuses.Add(JsonStringField(t, "message") ?? "");
                        break;
                    case "assistant":
                        assistants.Add(JsonStringField(t, "text") ?? "");
                        break;
                    case "advice":
                        statuses.Add(JsonStringField(t, "digest") ?? "");
                        break;
                }
            }

            if (errors.Count > 0)
                return ("error", string.Join("\n", errors));
            if (assistants.Count > 0)
                return ("agent", Trunc(string.Concat(assistants), 4000));
            if (statuses.Count > 0)
                return ("system", Trunc(string.Join("\n", statuses), 2000));
            return ("system", Trunc(raw, 1200));
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
}
