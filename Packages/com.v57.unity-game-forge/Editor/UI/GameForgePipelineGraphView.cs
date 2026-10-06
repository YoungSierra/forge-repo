using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Read-only pipeline board. Left list + graph. Cards do not move and have no editable edges.
    /// </summary>
    public sealed class GameForgePipelineGraphView : VisualElement
    {
        private readonly ScrollView _list;
        private readonly Label _now;
        private readonly ScrollView _scroll;
        private readonly VisualElement _zoomFrame;
        private readonly VisualElement _canvas;
        private Label _zoomLabel;
        private float _zoom = 1f;
        private readonly CurveLayer _curves;
        private readonly Dictionary<string, VisualElement> _cards = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Vector2> _dragPos = new(System.StringComparer.OrdinalIgnoreCase);
        private VisualElement _leadCard;
        private readonly List<VisualElement> _phaseCards = new();
        private readonly List<VisualElement> _childCards = new();
        private VisualElement _childHost;
        private readonly VisualElement _popup;
        private readonly Label _popupBody;
        private readonly TextField _fieldTdd;
        private readonly TextField _fieldPrefix;
        private readonly TextField _fieldScene;
        private readonly Button _popupAccept;
        private bool _confirmSending;
        private Label _stageCount;
        private bool _dragging;
        private bool _deferRefresh;
        private bool _curveQueued;
        private string _builtKey = "";

        public GameForgePipelineGraphView()
        {
            AddToClassList("gf-pipeline-board");
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Row;
            style.position = Position.Relative;

            var side = new VisualElement();
            side.AddToClassList("gf-pipe-side");
            var sideHead = new VisualElement();
            sideHead.AddToClassList("gf-pipe-side-head");
            var sideTitle = new VisualElement();
            sideTitle.AddToClassList("gf-pipe-side-title-row");
            sideTitle.Add(new Label("Stages") { name = "gf-pipe-side-title" });
            _stageCount = new Label("0 / 0");
            _stageCount.AddToClassList("gf-pipe-count");
            sideTitle.Add(_stageCount);
            sideHead.Add(sideTitle);
            var hideSide = new Button(() => SetSideCollapsed(true)) { text = "‹" };
            hideSide.AddToClassList("gf-pipe-side-hide");
            hideSide.tooltip = "Hide stages";
            sideHead.Add(hideSide);
            side.Add(sideHead);
            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.AddToClassList("gf-pipe-list");
            _list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _list.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            side.Add(_list);
            _side = side;
            _sideGrip = new VisualElement();
            _sideGrip.AddToClassList("gf-pipe-side-grip");
            _sideGrip.tooltip = "Show stages";
            var gripMark = new Label("›");
            gripMark.AddToClassList("gf-pipe-side-grip-mark");
            gripMark.pickingMode = PickingMode.Ignore;
            _sideGrip.Add(gripMark);
            _sideGrip.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                SetSideCollapsed(false);
                evt.StopPropagation();
            });
            Add(_sideGrip);
            Add(side);
            SetSideCollapsed(false);

            var main = new VisualElement();
            main.AddToClassList("gf-pipe-main");
            main.style.position = Position.Relative;
            _now = new Label("No script running");
            _now.AddToClassList("gf-pipe-now");
            main.Add(_now);

            _scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            _scroll.AddToClassList("gf-pipeline-scroll");
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _scroll.RegisterCallback<GeometryChangedEvent>(_ => StretchBoard());
            _scroll.RegisterCallback<WheelEvent>(OnZoomWheel, TrickleDown.TrickleDown);
            _scroll.RegisterCallback<PointerDownEvent>(OnPanDown, TrickleDown.TrickleDown);
            _scroll.RegisterCallback<PointerMoveEvent>(OnPanMove, TrickleDown.TrickleDown);
            _scroll.RegisterCallback<PointerUpEvent>(OnPanUp, TrickleDown.TrickleDown);
            _zoomFrame = new VisualElement();
            _zoomFrame.AddToClassList("gf-pipe-zoom-frame");
            _canvas = new VisualElement();
            _canvas.AddToClassList("gf-pipeline-canvas");
            _canvas.usageHints = UsageHints.GroupTransform;
            _canvas.style.position = Position.Absolute;
            _canvas.style.left = 0;
            _canvas.style.top = 0;
            _curves = new CurveLayer();
            _canvas.Add(_curves);
            _zoomFrame.Add(_canvas);
            _scroll.Add(_zoomFrame);
            main.Add(_scroll);
            main.Add(BuildZoomBar());
            Add(main);

            _popup = new VisualElement();
            _popup.AddToClassList("gf-pipe-popup");
            _popup.style.display = DisplayStyle.None;
            _popup.pickingMode = PickingMode.Position;
            var card = new VisualElement();
            card.AddToClassList("gf-pipe-popup-card");
            var head = new VisualElement();
            head.AddToClassList("gf-pipe-head");
            head.Add(new Label("Input needed"));
            var close = new Button(ClosePopupOnly) { text = "✕" };
            close.tooltip = "Close";
            close.AddToClassList("gf-pipe-stop");
            head.Add(close);
            card.Add(head);
            _popupBody = new Label("");
            _popupBody.AddToClassList("gf-pipe-popup-body");
            card.Add(_popupBody);
            _fieldTdd = new TextField("TDD");
            _fieldPrefix = new TextField("Prefix");
            _fieldScene = new TextField("Scene");
            card.Add(_fieldTdd);
            card.Add(_fieldPrefix);
            card.Add(_fieldScene);
            _popupAccept = new Button(() => SubmitAwait()) { text = "Accept" };
            _popupAccept.AddToClassList("gf-pipe-accept");
            card.Add(_popupAccept);
            _popup.Add(card);
            _popup.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == _popup)
                    ClosePopupOnly();
            });
            Add(_popup);

            GameForgePipelineState.FocusRequested += FocusStage;
        }

        public void Rebuild() => Refresh(force: true);

        public void Refresh(bool force = false)
        {
            if (_dragging)
            {
                _deferRefresh = true;
                return;
            }

            var key = BuildStructureKey();
            if (!force && _builtKey == key && _cards.Count > 0)
            {
                PatchLive();
                return;
            }

            BuildGraph();
            _builtKey = key;
        }

        private void BuildGraph()
        {
            if (_stageCount != null)
                _stageCount.text = GameForgePipelineState.ReadyCount + " / " + GameForgePipelineState.StageTotal;
            _cards.Clear();
            _list.Clear();
            _canvas.Clear();
            _canvas.Add(_curves);
            _curves.ClearLines();

            var script = GameForgePipelineState.CurrentScript;
            var action = GameForgePipelineState.CurrentAction;
            var stageName = GameForgePipelineState.DisplayName(GameForgePipelineState.CurrentStage);
            if (!string.IsNullOrEmpty(script))
                _now.text = "Running " + script + (string.IsNullOrEmpty(action) ? "" : " · " + action);
            else if (!string.IsNullOrEmpty(action))
                _now.text = (string.IsNullOrEmpty(stageName) ? "" : stageName + " · ") + action;
            else
                _now.text = "No script running";

            var phases = new List<PipelineCard>();
            var crafts = new List<PipelineCard>();
            foreach (var stage in GameForgePipelineState.Stages)
            {
                AddListRow(stage);
                if (GameForgePipelineState.IsCraft(stage.Id))
                    crafts.Add(stage);
                else
                    phases.Add(stage);
            }

            var craftsCard = AggregateCrafts(crafts);
            var insertAt = IndexOf(phases, "F");
            phases.Insert(insertAt < 0 ? phases.Count : insertAt + 1, craftsCard);
            AddListRow(craftsCard);

            var vertical = GameForgePipelineState.Vertical;
            _phaseCards.Clear();
            _childCards.Clear();
            _childHost = null;

            var parent = MakeCard(
                "project",
                GameForgePipelineState.ProjectTitle,
                CurrentActionLine(),
                ParentStatus(),
                lead: true,
                GameForgePipelineState.ReadyCount,
                GameForgePipelineState.StageTotal);
            _canvas.Add(parent);
            _cards["project"] = parent;
            _leadCard = parent;

            var phaseCards = _phaseCards;
            for (var i = 0; i < phases.Count; i++)
            {
                var card = MakeCard(phases[i].Id, phases[i].Title, phases[i].Action, phases[i].Status, lead: false, phases[i].Done, phases[i].Total);
                PlacePhase(card, phases[i].Id, i, vertical);
                _canvas.Add(card);
                phaseCards.Add(card);
                _cards[phases[i].Id] = card;
            }

            var children = new List<PipelineCard>();
            VisualElement host = null;
            var current = GameForgePipelineState.CurrentStage ?? "";
            var showSpecs = current.Equals("F", System.StringComparison.OrdinalIgnoreCase) &&
                            GameForgePipelineState.Specs.Count > 0;
            if (showSpecs)
            {
                children.AddRange(GameForgePipelineState.Specs);
                host = Find(phaseCards, "F");
            }
            else
            {
                children.AddRange(crafts);
                host = Find(phaseCards, "G");
            }
            _childHost = host;

            var hostIndex = host == null ? 0 : phaseCards.IndexOf(host);
            var childCards = _childCards;
            for (var i = 0; i < children.Count; i++)
            {
                var card = MakeCard(children[i].Id, children[i].Title, children[i].Action, children[i].Status, lead: false, children[i].Done, children[i].Total);
                PlaceChild(card, children[i].Id, i, hostIndex, vertical);
                _canvas.Add(card);
                childCards.Add(card);
                _cards[children[i].Id] = card;
            }

            PlaceLead(parent, vertical ? 720 : Mathf.Max(phases.Count, 1) * 212f + 48f, vertical);
            FitCanvas();
            DrawLinks(parent, phaseCards, host, childCards, vertical);

            if (GameForgePipelineState.AwaitingUser)
            {
                var init = string.Equals(GameForgePipelineState.AwaitStageId, "INIT", System.StringComparison.OrdinalIgnoreCase);
                _popup.style.display = DisplayStyle.Flex;
                _popup.pickingMode = PickingMode.Position;
                _popup.BringToFront();
                _popupBody.text = string.IsNullOrEmpty(GameForgePipelineState.AwaitMessage)
                    ? (init ? "Confirm the proposed values, then press Play." : "Press Play on this stage to continue.")
                    : GameForgePipelineState.AwaitMessage;
                _fieldTdd.style.display = init ? DisplayStyle.Flex : DisplayStyle.None;
                _fieldPrefix.style.display = init ? DisplayStyle.Flex : DisplayStyle.None;
                _fieldScene.style.display = init ? DisplayStyle.Flex : DisplayStyle.None;
                if (_popupAccept != null)
                {
                    _popupAccept.text = init ? "Accept" : "Continue";
                    _popupAccept.SetEnabled(true);
                }
                _confirmSending = false;
                if (init)
                {
                    if (!string.IsNullOrEmpty(GameForgePipelineState.AwaitTdd))
                        _fieldTdd.SetValueWithoutNotify(GameForgePipelineState.AwaitTdd);
                    if (!string.IsNullOrEmpty(GameForgePipelineState.AwaitPrefix))
                        _fieldPrefix.SetValueWithoutNotify(GameForgePipelineState.AwaitPrefix);
                    else if (string.IsNullOrEmpty(_fieldPrefix.value))
                        _fieldPrefix.SetValueWithoutNotify("Game");
                    if (!string.IsNullOrEmpty(GameForgePipelineState.AwaitScene))
                        _fieldScene.SetValueWithoutNotify(GameForgePipelineState.AwaitScene);
                    else if (string.IsNullOrEmpty(_fieldScene.value))
                        _fieldScene.SetValueWithoutNotify("Assets/Scenes/Main.unity");
                }
            }
            else
            {
                _popup.style.display = DisplayStyle.None;
            }
        }

        private string BuildStructureKey()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(GameForgePipelineState.Vertical ? "v" : "h");
            sb.Append('|');
            foreach (var stage in GameForgePipelineState.Stages)
                sb.Append(stage.Id).Append(',');
            var current = GameForgePipelineState.CurrentStage ?? "";
            var showSpecs = current.Equals("F", System.StringComparison.OrdinalIgnoreCase) &&
                            GameForgePipelineState.Specs.Count > 0;
            sb.Append(showSpecs ? "|specs:" : "|crafts:");
            if (showSpecs)
            {
                foreach (var spec in GameForgePipelineState.Specs)
                    sb.Append(spec.Id).Append(',');
            }

            return sb.ToString();
        }

        private void PatchLive()
        {
            SyncChrome();
            foreach (var stage in GameForgePipelineState.Stages)
            {
                if (_cards.TryGetValue(stage.Id, out var card))
                    ApplyCardFace(card, stage.Id, stage.Title, stage.Action, stage.Status, false, stage.Done, stage.Total);
                PatchListRow(stage);
            }

            if (_cards.TryGetValue("project", out var lead))
                ApplyCardFace(
                    lead,
                    "project",
                    GameForgePipelineState.ProjectTitle,
                    CurrentActionLine(),
                    ParentStatus(),
                    true,
                    GameForgePipelineState.ReadyCount,
                    GameForgePipelineState.StageTotal);

            var crafts = new List<PipelineCard>();
            foreach (var stage in GameForgePipelineState.Stages)
            {
                if (GameForgePipelineState.IsCraft(stage.Id))
                    crafts.Add(stage);
            }

            var craftsCard = AggregateCrafts(crafts);
            if (_cards.TryGetValue(craftsCard.Id, out var craftNode))
                ApplyCardFace(craftNode, craftsCard.Id, craftsCard.Title, craftsCard.Action, craftsCard.Status, false, craftsCard.Done, craftsCard.Total);
            PatchListRow(craftsCard);

            foreach (var spec in GameForgePipelineState.Specs)
            {
                if (_cards.TryGetValue(spec.Id, out var specCard))
                    ApplyCardFace(specCard, spec.Id, spec.Title, spec.Action, spec.Status, false, spec.Done, spec.Total);
            }

            SyncPopup();
        }

        private void SyncChrome()
        {
            if (_stageCount != null)
                _stageCount.text = GameForgePipelineState.ReadyCount + " / " + GameForgePipelineState.StageTotal;
            var script = GameForgePipelineState.CurrentScript;
            var action = GameForgePipelineState.CurrentAction;
            var stageName = GameForgePipelineState.DisplayName(GameForgePipelineState.CurrentStage);
            if (!string.IsNullOrEmpty(script))
                _now.text = "Running " + script + (string.IsNullOrEmpty(action) ? "" : " · " + action);
            else if (!string.IsNullOrEmpty(action))
                _now.text = (string.IsNullOrEmpty(stageName) ? "" : stageName + " · ") + action;
            else
                _now.text = "No script running";
        }

        private void SyncPopup()
        {
            if (GameForgePipelineState.AwaitingUser)
            {
                var init = string.Equals(GameForgePipelineState.AwaitStageId, "INIT", System.StringComparison.OrdinalIgnoreCase);
                _popup.style.display = DisplayStyle.Flex;
                _popup.pickingMode = PickingMode.Position;
                _popupBody.text = string.IsNullOrEmpty(GameForgePipelineState.AwaitMessage)
                    ? (init ? "Confirm the proposed values, then press Play." : "Press Play on this stage to continue.")
                    : GameForgePipelineState.AwaitMessage;
                _fieldTdd.style.display = init ? DisplayStyle.Flex : DisplayStyle.None;
                _fieldPrefix.style.display = init ? DisplayStyle.Flex : DisplayStyle.None;
                _fieldScene.style.display = init ? DisplayStyle.Flex : DisplayStyle.None;
                if (_popupAccept != null)
                {
                    _popupAccept.text = init ? "Accept" : "Continue";
                    if (!_confirmSending)
                        _popupAccept.SetEnabled(true);
                }
                return;
            }

            ClosePopup();
        }

        public void FocusStage(string id)
        {
            if (string.IsNullOrEmpty(id) || !_cards.TryGetValue(id, out var card)) return;
            var x = card.style.left.value.value;
            var y = card.style.top.value.value;
            card.BringToFront();
            var offset = new Vector2(Mathf.Max(0, x - 32f), Mathf.Max(0, y - 24f));
            _scroll.scrollOffset = offset;
            _scroll.schedule.Execute(() => { _scroll.scrollOffset = offset; });
        }

        private VisualElement _side;
        private VisualElement _sideGrip;
        private bool _sideCollapsed;

        private void SetSideCollapsed(bool collapsed)
        {
            _sideCollapsed = collapsed;
            if (_side != null)
                _side.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            if (_sideGrip != null)
                _sideGrip.style.display = collapsed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddListRow(PipelineCard stage)
        {
            var row = new Button(() => FocusStage(stage.Id));
            row.name = "row-" + stage.Id;
            row.AddToClassList("gf-pipe-list-row");
            row.AddToClassList(StatusClass(stage.Status));
            if (string.Equals(stage.Id, GameForgePipelineState.CurrentStage, System.StringComparison.OrdinalIgnoreCase))
                row.AddToClassList("is-current");

            var title = new Label(stage.Title);
            title.AddToClassList("gf-pipe-list-title");
            title.pickingMode = PickingMode.Ignore;
            row.Add(title);

            if (stage.Status == PipelineNodeStatus.Ready)
            {
                var passed = new Label("✓  Passed");
                passed.AddToClassList("gf-pipe-list-passed");
                passed.pickingMode = PickingMode.Ignore;
                row.Add(passed);
            }

            _list.Add(row);
        }

        private void PatchListRow(PipelineCard stage)
        {
            var row = _list.Q<Button>("row-" + stage.Id);
            if (row == null) return;
            row.EnableInClassList("is-ready", stage.Status == PipelineNodeStatus.Ready);
            row.EnableInClassList("is-running", stage.Status == PipelineNodeStatus.Running);
            row.EnableInClassList("is-blocked", stage.Status == PipelineNodeStatus.Blocked);
            row.EnableInClassList("is-pending", stage.Status == PipelineNodeStatus.Pending);
            row.EnableInClassList("is-current", string.Equals(stage.Id, GameForgePipelineState.CurrentStage, System.StringComparison.OrdinalIgnoreCase));
            var passed = row.Q<Label>(className: "gf-pipe-list-passed");
            if (stage.Status == PipelineNodeStatus.Ready)
            {
                if (passed == null)
                {
                    passed = new Label("✓  Passed");
                    passed.AddToClassList("gf-pipe-list-passed");
                    passed.pickingMode = PickingMode.Ignore;
                    row.Add(passed);
                }
            }
            else if (passed != null)
            {
                passed.RemoveFromHierarchy();
            }
        }

        private void ApplyCardFace(VisualElement card, string id, string title, string action, PipelineNodeStatus status, bool lead, int done, int total)
        {
            card.EnableInClassList("is-ready", status == PipelineNodeStatus.Ready);
            card.EnableInClassList("is-running", status == PipelineNodeStatus.Running);
            card.EnableInClassList("is-blocked", status == PipelineNodeStatus.Blocked);
            card.EnableInClassList("is-pending", status == PipelineNodeStatus.Pending);

            var name = card.Q<Label>(className: "gf-pipe-title");
            if (name != null)
                name.text = string.IsNullOrEmpty(title) ? id : title;

            var act = card.Q<Label>(className: "gf-pipe-action");
            if (act != null)
            {
                act.text = action ?? "";
                act.style.display = string.IsNullOrEmpty(action) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            var chip = card.Q<Label>(className: "gf-pipe-chip");
            if (chip != null)
                chip.text = StatusLabel(status);

            var bar = card.Q<ProgressBar>(className: "gf-pipe-bar");
            if (bar != null)
            {
                var pct = lead && total > 0
                    ? Mathf.Clamp01(done / (float)total)
                    : status == PipelineNodeStatus.Ready
                        ? 1f
                        : status == PipelineNodeStatus.Running && total > 0
                            ? Mathf.Clamp01(done / (float)Mathf.Max(1, total))
                            : 0f;
                bar.value = pct * 100f;
                bar.title = Mathf.RoundToInt(pct * 100f) + "%";
            }

            SyncCardAction(card, id, status, lead);
        }

        private void SyncCardAction(VisualElement card, string id, PipelineNodeStatus status, bool lead)
        {
            var head = card.Q(className: "gf-pipe-head");
            if (head == null) return;
            var existing = head.Q<Button>();
            var kind = CardActionKind(id, status, lead);
            if (existing != null && existing.name == kind)
                return;
            if (existing != null)
                existing.RemoveFromHierarchy();
            if (kind == "stop")
            {
                var stop = new Button(() => GameForgePipelineState.RequestStop()) { text = "■", name = "stop" };
                stop.tooltip = "Stop";
                stop.AddToClassList("gf-pipe-stop");
                head.Add(stop);
            }
            else if (kind == "run")
            {
                var play = new Button(() => GameForgePipelineState.RequestRunSetup()) { text = "▶", name = "run" };
                play.tooltip = GameForgePipelineState.HasCheckpoint ? "Resume game setup" : "Run game setup";
                play.AddToClassList("gf-pipe-play");
                head.Add(play);
            }
            else if (kind == "continue")
            {
                var play = new Button(SubmitAwait) { text = "▶", name = "continue" };
                play.tooltip = "Continue this stage";
                play.AddToClassList("gf-pipe-play");
                head.Add(play);
            }
        }

        private static string CardActionKind(string id, PipelineNodeStatus status, bool lead)
        {
            var runLive = GameForgePipelineState.SetupRunLive;
            if (lead && runLive && !NeedsPlay(id)) return "stop";
            if (lead && !runLive && !GameForgePipelineState.AwaitingUser) return "run";
            if (!lead && runLive && status == PipelineNodeStatus.Running && !NeedsPlay(id)) return "stop";
            if (NeedsPlay(id)) return "continue";
            return "";
        }

        private VisualElement MakeCard(string id, string title, string action, PipelineNodeStatus status, bool lead, int done = 0, int total = 0)
        {
            var card = new VisualElement();
            card.userData = id;
            card.pickingMode = PickingMode.Position;
            card.AddToClassList("gf-pipe-card");
            card.usageHints = UsageHints.DynamicTransform;
            BindDrag(card, id);
            if (lead) card.AddToClassList("gf-pipe-parent");
            card.AddToClassList(StatusClass(status));

            var head = new VisualElement();
            head.AddToClassList("gf-pipe-head");
            var name = new Label(string.IsNullOrEmpty(title) ? id : title);
            name.AddToClassList("gf-pipe-title");
            name.pickingMode = PickingMode.Ignore;
            head.pickingMode = PickingMode.Ignore;
            head.Add(name);
            card.Add(head);
            SyncCardAction(card, id, status, lead);

            var pct = lead && total > 0
                ? Mathf.Clamp01(done / (float)total)
                : status == PipelineNodeStatus.Ready
                    ? 1f
                    : status == PipelineNodeStatus.Running && total > 0
                        ? Mathf.Clamp01(done / (float)Mathf.Max(1, total))
                        : 0f;
            var bar = new ProgressBar { value = pct * 100f, title = Mathf.RoundToInt(pct * 100f) + "%" };
            bar.pickingMode = PickingMode.Ignore;
            bar.AddToClassList("gf-pipe-bar");
            card.Add(bar);

            var act = new Label(action ?? "");
            act.AddToClassList("gf-pipe-action");
            act.pickingMode = PickingMode.Ignore;
            act.style.display = string.IsNullOrEmpty(action) ? DisplayStyle.None : DisplayStyle.Flex;
            card.Add(act);

            var chip = new Label(StatusLabel(status));
            chip.AddToClassList("gf-pipe-chip");
            card.Add(chip);
            return card;
        }

        private static bool NeedsPlay(string id)
        {
            if (!GameForgePipelineState.AwaitingUser) return false;
            return string.Equals(id, GameForgePipelineState.AwaitStageId, System.StringComparison.OrdinalIgnoreCase);
        }

        private void SubmitAwait()
        {
            if (_confirmSending || !GameForgePipelineState.AwaitingUser) return;
            _confirmSending = true;
            if (_popupAccept != null)
                _popupAccept.SetEnabled(false);
            ClosePopup();

            var init = string.Equals(GameForgePipelineState.AwaitStageId, "INIT", System.StringComparison.OrdinalIgnoreCase);
            var prefix = _fieldPrefix?.value;
            var scene = _fieldScene?.value;
            GameForgePipelineState.DismissConfirm();
            if (GameForgePipelineState.DummyRunning)
            {
                _confirmSending = false;
                return;
            }

            if (init)
            {
                var parts = new List<string> { "confirm" };
                if (!string.IsNullOrWhiteSpace(prefix))
                    parts.Add("prefix=" + prefix.Trim());
                if (!string.IsNullOrWhiteSpace(scene))
                    parts.Add("scene=" + scene.Trim());
                GameForgeChatWindow.SubmitPipelineReply(string.Join(" ", parts));
                return;
            }

            GameForgeChatWindow.SubmitPipelineContinue();
        }

        private void ClosePopupOnly()
        {
            _confirmSending = false;
            ClosePopup();
            GameForgePipelineState.DismissConfirm();
        }

        private void ClosePopup()
        {
            if (_popup == null) return;
            _popup.style.display = DisplayStyle.None;
            _popup.pickingMode = PickingMode.Ignore;
        }

        private void PlaceLead(VisualElement card, float width, bool vertical)
        {
            var fallback = new Vector2(vertical ? 16 : Mathf.Max(16, width * 0.5f - 120), 16);
            ApplyPlace(card, "project", fallback);
        }

        private void BindDrag(VisualElement card, string id)
        {
            var dragging = false;
            var origin = Vector2.zero;
            var start = Vector2.zero;
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || IsActionTarget(evt.target as VisualElement)) return;
                dragging = true;
                _dragging = true;
                origin = evt.position;
                start = ReadPos(card);
                card.style.translate = new Translate(0, 0, 0);
                card.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging || !card.HasPointerCapture(evt.pointerId)) return;
                var delta = (Vector2)evt.position - origin;
                card.style.translate = new Translate(delta.x, delta.y, 0);
                QueueCurves();
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragging) return;
                dragging = false;
                CommitDrag(card, id, start, (Vector2)evt.position - origin, evt.pointerId);
            });
            card.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (!dragging) return;
                dragging = false;
                CommitDrag(card, id, start, ReadTranslate(card), -1);
            });
        }

        private static bool IsActionTarget(VisualElement target)
        {
            while (target != null)
            {
                if (target is Button) return true;
                target = target.parent;
            }
            return false;
        }

        private void CommitDrag(VisualElement card, string id, Vector2 start, Vector2 delta, int pointerId)
        {
            var next = start + delta;
            card.style.translate = new Translate(0, 0, 0);
            card.style.left = next.x;
            card.style.top = next.y;
            _dragPos[id] = next;
            EndDrag(card, pointerId);
        }

        private void EndDrag(VisualElement card, int pointerId)
        {
            _dragging = false;
            if (pointerId >= 0 && card != null && card.HasPointerCapture(pointerId))
                card.ReleasePointer(pointerId);
            QueueCurves();
            if (!_deferRefresh) return;
            _deferRefresh = false;
            Refresh();
        }

        private void QueueCurves()
        {
            if (_curveQueued) return;
            _curveQueued = true;
            schedule.Execute(() =>
            {
                _curveQueued = false;
                FitCanvas();
                RedrawCurves();
            });
        }

        private static Vector2 ReadTranslate(VisualElement card)
        {
            var t = card.style.translate.value;
            return new Vector2(t.x.value, t.y.value);
        }

        private void StretchBoard()
        {
            var view = _scroll.contentViewport;
            if (view == null) return;
            var h = _scroll.layout.height;
            if (h < 8f) return;
            if (Mathf.Abs(view.resolvedStyle.minHeight.value - h) < 1f) return;
            view.style.minHeight = h;
            FitCanvas();
        }

        private void FitCanvas()
        {
            var width = 640f;
            var height = 420f;
            var view = _scroll?.contentViewport;
            if (view != null && view.layout.height > height)
                height = view.layout.height;
            foreach (var card in _cards.Values)
            {
                var pos = VisualPos(card);
                var x = pos.x + 260f;
                var y = pos.y + 200f;
                if (x > width) width = x;
                if (y > height) height = y;
            }
            if (Mathf.Abs(_canvas.style.width.value.value - width) > 1f)
                _canvas.style.width = width;
            if (Mathf.Abs(_canvas.style.height.value.value - height) > 1f)
                _canvas.style.height = height;
            if (Mathf.Abs(_curves.style.width.value.value - width) > 1f)
                _curves.style.width = width;
            if (Mathf.Abs(_curves.style.height.value.value - height) > 1f)
                _curves.style.height = height;
            ApplyZoom();
        }

        private VisualElement BuildZoomBar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("gf-pipe-zoom");
            bar.pickingMode = PickingMode.Position;
            var outBtn = new Button(() => SetZoom(_zoom - 0.1f)) { text = "−" };
            outBtn.AddToClassList("gf-pipe-zoom-btn");
            outBtn.tooltip = "Zoom out";
            var innBtn = new Button(() => SetZoom(_zoom + 0.1f)) { text = "+" };
            innBtn.AddToClassList("gf-pipe-zoom-btn");
            innBtn.tooltip = "Zoom in";
            _zoomLabel = new Label("100%");
            _zoomLabel.AddToClassList("gf-pipe-zoom-label");
            _zoomLabel.tooltip = "Reset zoom";
            _zoomLabel.RegisterCallback<PointerDownEvent>(_ => SetZoom(1f));
            bar.Add(outBtn);
            bar.Add(_zoomLabel);
            bar.Add(innBtn);
            return bar;
        }

        private bool _panning;
        private Vector2 _panOrigin;
        private Vector2 _panStart;

        private void OnPanDown(PointerDownEvent evt)
        {
            if (evt.button != 2) return;
            _panning = true;
            _panOrigin = evt.position;
            _panStart = _scroll.scrollOffset;
            _scroll.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPanMove(PointerMoveEvent evt)
        {
            if (!_panning || !_scroll.HasPointerCapture(evt.pointerId)) return;
            var delta = (Vector2)evt.position - _panOrigin;
            _scroll.scrollOffset = new Vector2(
                Mathf.Max(0f, _panStart.x - delta.x),
                Mathf.Max(0f, _panStart.y - delta.y));
            evt.StopPropagation();
        }

        private void OnPanUp(PointerUpEvent evt)
        {
            if (evt.button != 2 || !_panning) return;
            _panning = false;
            _scroll.ReleasePointer(evt.pointerId);
        }

        private void OnZoomWheel(WheelEvent evt)
        {
            if (!evt.ctrlKey && !evt.commandKey) return;
            evt.StopPropagation();
            SetZoom(_zoom + (evt.delta.y < 0f ? 0.08f : -0.08f));
        }

        private void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, 0.45f, 1.8f);
            ApplyZoom();
        }

        private float _appliedZoom = -1f;
        private float _appliedZoomW;
        private float _appliedZoomH;

        private void ApplyZoom()
        {
            var z = Mathf.Max(0.45f, _zoom);
            var w = _canvas.resolvedStyle.width;
            var h = _canvas.resolvedStyle.height;
            if (w < 8f) w = _canvas.style.width.value.value;
            if (h < 8f) h = _canvas.style.height.value.value;
            if (Mathf.Abs(_appliedZoom - z) < 0.001f &&
                Mathf.Abs(_appliedZoomW - w) < 1f &&
                Mathf.Abs(_appliedZoomH - h) < 1f)
                return;
            _appliedZoom = z;
            _appliedZoomW = w;
            _appliedZoomH = h;
            _canvas.style.scale = new Scale(new Vector3(z, z, 1f));
            _canvas.style.transformOrigin = new TransformOrigin(0, 0, 0);
            _zoomFrame.style.width = Mathf.Max(64f, w * z);
            _zoomFrame.style.height = Mathf.Max(64f, h * z);
            if (_zoomLabel != null)
                _zoomLabel.text = Mathf.RoundToInt(z * 100f) + "%";
        }

        private void ApplyPlace(VisualElement card, string id, Vector2 fallback)
        {
            card.style.position = Position.Absolute;
            card.style.translate = new Translate(0, 0, 0);
            var pos = _dragPos.TryGetValue(id, out var saved) ? saved : fallback;
            card.style.left = pos.x;
            card.style.top = pos.y;
        }

        private static Vector2 ReadPos(VisualElement card) =>
            new Vector2(card.style.left.value.value, card.style.top.value.value);

        private static Vector2 VisualPos(VisualElement card) => ReadPos(card) + ReadTranslate(card);

        private void RedrawCurves()
        {
            if (_leadCard == null) return;
            _curves.ClearLines();
            DrawLinks(_leadCard, _phaseCards, _childHost, _childCards, GameForgePipelineState.Vertical);
        }

        private void PlacePhase(VisualElement card, string id, int index, bool vertical)
        {
            var fallback = vertical
                ? new Vector2(16, 168 + index * 136)
                : new Vector2(16 + index * 220, 168);
            ApplyPlace(card, id, fallback);
        }

        private void PlaceChild(VisualElement card, string id, int index, int hostIndex, bool vertical)
        {
            var start = Mathf.Max(0, hostIndex - 1);
            var fallback = vertical
                ? new Vector2(280, 168 + hostIndex * 136 + index * 120)
                : new Vector2(16 + (start + index) * 208, 360);
            ApplyPlace(card, id, fallback);
        }

        private void DrawLinks(VisualElement parent, List<VisualElement> phases, VisualElement host, List<VisualElement> children, bool vertical)
        {
            var lead = BottomOf(parent);
            foreach (var phase in phases)
                _curves.Add(lead, TopOf(phase));
            if (host != null)
            {
                var from = BottomOf(host);
                foreach (var child in children)
                    _curves.Add(from, TopOf(child));
            }
            _curves.MarkDirtyRepaint();
        }

        private static Vector2 TopOf(VisualElement card)
        {
            var pos = VisualPos(card);
            return new Vector2(pos.x + 90f, pos.y);
        }

        private static Vector2 BottomOf(VisualElement card)
        {
            var pos = VisualPos(card);
            return new Vector2(pos.x + 90f, pos.y + 118f);
        }

        private static string CurrentActionLine()
        {
            if (!string.IsNullOrEmpty(GameForgePipelineState.CurrentScript))
                return GameForgePipelineState.CurrentScript;
            return GameForgePipelineState.CurrentAction ?? "";
        }

        private static PipelineNodeStatus ParentStatus()
        {
            if (GameForgePipelineState.ReadyCount > 0 &&
                GameForgePipelineState.ReadyCount >= GameForgePipelineState.StageTotal &&
                !GameForgePipelineState.SetupRunLive)
                return PipelineNodeStatus.Ready;
            if (GameForgePipelineState.SetupRunLive)
                return PipelineNodeStatus.Running;
            return PipelineNodeStatus.Pending;
        }

        private static PipelineCard AggregateCrafts(List<PipelineCard> crafts)
        {
            var done = 0;
            var running = false;
            var blocked = false;
            foreach (var c in crafts)
            {
                if (c.Status == PipelineNodeStatus.Ready) done++;
                if (c.Status == PipelineNodeStatus.Running) running = true;
                if (c.Status == PipelineNodeStatus.Blocked) blocked = true;
            }

            var status = PipelineNodeStatus.Pending;
            if (blocked) status = PipelineNodeStatus.Blocked;
            else if (running) status = PipelineNodeStatus.Running;
            else if (crafts.Count > 0 && done == crafts.Count) status = PipelineNodeStatus.Ready;

            return new PipelineCard
            {
                Id = "G",
                Title = "Crafts",
                Action = running ? GameForgePipelineState.CurrentAction : "",
                Done = done,
                Total = Mathf.Max(1, crafts.Count),
                Status = status
            };
        }

        private static int IndexOf(List<PipelineCard> phases, string id)
        {
            for (var i = 0; i < phases.Count; i++)
            {
                if (string.Equals(phases[i].Id, id, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static VisualElement Find(List<VisualElement> cards, string id)
        {
            foreach (var card in cards)
            {
                if ((string)card.userData == id) return card;
            }
            return null;
        }

        private static string StatusClass(PipelineNodeStatus status)
        {
            switch (status)
            {
                case PipelineNodeStatus.Ready: return "is-ready";
                case PipelineNodeStatus.Running: return "is-running";
                case PipelineNodeStatus.Blocked: return "is-blocked";
                default: return "is-pending";
            }
        }

        private static string StatusLabel(PipelineNodeStatus status)
        {
            switch (status)
            {
                case PipelineNodeStatus.Ready: return "Ready";
                case PipelineNodeStatus.Running: return "Running";
                case PipelineNodeStatus.Blocked: return "Blocked";
                default: return "Waiting";
            }
        }

        private sealed class CurveLayer : VisualElement
        {
            private readonly List<(Vector2 a, Vector2 b)> _lines = new();

            public CurveLayer()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0;
                style.top = 0;
                generateVisualContent += OnGenerate;
            }

            public void ClearLines() => _lines.Clear();
            public void Add(Vector2 a, Vector2 b) => _lines.Add((a, b));

            private void OnGenerate(MeshGenerationContext ctx)
            {
                var painter = ctx.painter2D;
                painter.strokeColor = new Color(0.45f, 0.62f, 0.42f, 0.9f);
                painter.lineWidth = 1.5f;
                foreach (var line in _lines)
                {
                    var midX = (line.a.x + line.b.x) * 0.5f;
                    painter.BeginPath();
                    painter.MoveTo(line.a);
                    painter.BezierCurveTo(new Vector2(midX, line.a.y), new Vector2(midX, line.b.y), line.b);
                    painter.Stroke();
                }
            }
        }
    }
}
