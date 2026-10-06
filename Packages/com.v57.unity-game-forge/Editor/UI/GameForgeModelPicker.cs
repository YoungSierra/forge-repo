using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    [Serializable]
    public sealed class ProviderInfo
    {
        public string id;
        public string label;
        public string kind;
        public string model;
        public List<string> suggestedModels = new();
        public bool ready;
    }

    [Serializable]
    public sealed class ProviderCatalogState
    {
        public bool configured;
        public string active;
        public string model;
        public string missingHint;
        public List<ProviderInfo> providers = new();
    }

    /// <summary>
    /// Compact provider/model chooser. Sheet opens in a host panel (chat) or under the trigger (workbench).
    /// </summary>
    public sealed class GameForgeModelPicker
    {
        public event Action<string, string> SelectionChanged;

        private readonly VisualElement _root;
        private readonly Button _trigger;
        private readonly Label _triggerText;
        private readonly VisualElement _sheet;
        private readonly VisualElement _providerRow;
        private readonly ScrollView _modelScroll;
        private readonly VisualElement _modelOptions;
        private readonly TextField _customModel;
        private readonly Label _emptyHint;

        private VisualElement _sheetHost;
        private ProviderCatalogState _state = new();
        private string _providerId = "cursor";
        private string _modelId = "auto";
        private bool _open;

        public VisualElement Root => _root;
        public string ProviderId => _providerId;
        public string ModelId => _modelId;
        public bool IsOpen => _open;

        public GameForgeModelPicker(string label = "Model")
        {
            _root = new VisualElement();
            _root.AddToClassList("mp");

            if (!string.IsNullOrEmpty(label))
            {
                var fieldLabel = new Label(label);
                fieldLabel.AddToClassList("mp-label");
                _root.Add(fieldLabel);
            }

            _trigger = new Button(Toggle) { text = "" };
            _trigger.AddToClassList("mp-trigger");
            _triggerText = new Label("Select model…");
            _triggerText.AddToClassList("mp-trigger-text");
            var chev = new Label("▾");
            chev.AddToClassList("mp-chev");
            _trigger.Add(_triggerText);
            _trigger.Add(chev);
            _root.Add(_trigger);

            _sheet = new VisualElement();
            _sheet.AddToClassList("mp-sheet");
            _sheet.style.display = DisplayStyle.None;

            var sheetHead = new VisualElement();
            sheetHead.AddToClassList("mp-sheet-head");
            sheetHead.Add(LabelCls("mp-sheet-title", "Provider"));
            var close = new Button(Close) { text = "✕" };
            close.AddToClassList("mp-sheet-close");
            sheetHead.Add(close);
            _sheet.Add(sheetHead);

            _providerRow = new VisualElement();
            _providerRow.AddToClassList("mp-provider-row");
            _sheet.Add(_providerRow);

            _sheet.Add(LabelCls("mp-sheet-title", "Model"));

            _modelScroll = new ScrollView();
            _modelScroll.AddToClassList("mp-model-scroll");
            _modelScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _modelScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _modelOptions = _modelScroll.contentContainer;
            _modelOptions.AddToClassList("mp-options");
            _sheet.Add(_modelScroll);

            _emptyHint = LabelCls("mp-empty", "No models for this provider.");
            _emptyHint.style.display = DisplayStyle.None;
            _sheet.Add(_emptyHint);

            _customModel = new TextField { label = "Custom" };
            _customModel.AddToClassList("mp-custom");
            _customModel.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return) return;
                var v = (_customModel.value ?? "").Trim();
                if (v.Length == 0) return;
                SetModel(v, close: true);
                evt.StopPropagation();
            });
            _sheet.Add(_customModel);

            // Default: sheet under trigger (workbench)
            _sheetHost = _root;
            _root.Add(_sheet);
        }

        /// <summary>Mount the sheet in a dedicated host so it never clips inside the compose pill.</summary>
        public void SetSheetHost(VisualElement host)
        {
            if (host == null || host == _sheetHost) return;
            _sheet.RemoveFromHierarchy();
            _sheetHost = host;
            _sheetHost.Add(_sheet);
        }

        public void SetCatalog(ProviderCatalogState state)
        {
            _state = state ?? new ProviderCatalogState();
            if (!string.IsNullOrEmpty(_state.active))
                _providerId = _state.active;
            if (!string.IsNullOrEmpty(_state.model))
                _modelId = _state.model;
            RefreshTrigger();
            RebuildProviders();
            RebuildModels();
        }

        public void SetSelection(string providerId, string modelId)
        {
            if (!string.IsNullOrEmpty(providerId)) _providerId = providerId;
            if (!string.IsNullOrEmpty(modelId)) _modelId = modelId;
            RefreshTrigger();
            RebuildModels();
        }

        public void Close()
        {
            _open = false;
            _sheet.style.display = DisplayStyle.None;
            _trigger.EnableInClassList("is-open", false);
        }

        public void SetInteractable(bool enabled)
        {
            _trigger.SetEnabled(enabled);
            if (!enabled) Close();
            _root.EnableInClassList("is-locked", !enabled);
            _trigger.tooltip = enabled ? "Model" : "Model locked while a run is active";
        }

        private void Toggle()
        {
            if (!_trigger.enabledSelf) return;
            if (_open)
            {
                Close();
                return;
            }

            _open = true;
            _sheet.style.display = DisplayStyle.Flex;
            _trigger.EnableInClassList("is-open", true);
            RebuildProviders();
            RebuildModels();
        }

        private void RebuildProviders()
        {
            _providerRow.Clear();
            if (_state.providers.Count == 0)
            {
                _providerRow.Add(LabelCls("mp-empty", _state.missingHint ?? "No providers configured."));
                return;
            }

            foreach (var p in _state.providers)
            {
                var local = p;
                var btn = new Button(() =>
                {
                    _providerId = local.id;
                    _modelId = string.IsNullOrEmpty(local.model)
                        ? (local.suggestedModels.Count > 0 ? local.suggestedModels[0] : "auto")
                        : local.model;
                    RefreshTrigger();
                    RebuildProviders();
                    RebuildModels();
                    SelectionChanged?.Invoke(_providerId, _modelId);
                })
                {
                    text = string.IsNullOrEmpty(p.label) ? p.id : p.label
                };
                btn.AddToClassList("mp-chip");
                if (p.id == _providerId) btn.AddToClassList("is-active");
                if (!p.ready) btn.AddToClassList("is-muted");
                _providerRow.Add(btn);
            }
        }

        private void RebuildModels()
        {
            _modelOptions.Clear();
            var provider = _state.providers.Find(p => p.id == _providerId);
            var models = provider?.suggestedModels ?? new List<string>();
            if (models.Count == 0 && !string.IsNullOrEmpty(_modelId))
                models.Add(_modelId);

            _emptyHint.style.display = models.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var m in models)
            {
                var local = m;
                var btn = new Button(() => SetModel(local, close: true)) { text = m };
                btn.AddToClassList("mp-option");
                if (m == _modelId) btn.AddToClassList("is-active");
                _modelOptions.Add(btn);
            }

            _customModel.value = "";
        }

        private void SetModel(string model, bool close)
        {
            _modelId = model;
            RefreshTrigger();
            SelectionChanged?.Invoke(_providerId, _modelId);
            if (close) Close();
            else RebuildModels();
        }

        private void RefreshTrigger()
        {
            var provider = _state.providers.Find(p => p.id == _providerId);
            var label = provider != null && !string.IsNullOrEmpty(provider.label) ? provider.label : _providerId;
            var shortModel = string.IsNullOrEmpty(_modelId) ? "—" : _modelId;
            if (shortModel.Length > 22) shortModel = shortModel.Substring(0, 20) + "…";
            _triggerText.text = string.IsNullOrEmpty(_modelId) ? label : shortModel;
            _trigger.tooltip = $"{label} · {_modelId}";
        }

        private static Label LabelCls(string cls, string text)
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            return l;
        }

        public static ProviderCatalogState ParseStatusJson(string json)
        {
            var state = new ProviderCatalogState
            {
                configured = json.Contains("\"configured\":true"),
                active = ExtractString(json, "active"),
                model = ExtractString(json, "model"),
                missingHint = ExtractString(json, "missingHint")
            };

            var providerBlocks = Regex.Matches(json, "\\{[^{}]*\"id\"\\s*:\\s*\"([^\"]+)\"[^{}]*\\}");
            foreach (Match m in providerBlocks)
            {
                var block = m.Value;
                if (!block.Contains("\"kind\"")) continue;
                var info = new ProviderInfo
                {
                    id = ExtractString(block, "id"),
                    label = ExtractString(block, "label"),
                    kind = ExtractString(block, "kind"),
                    model = ExtractString(block, "model"),
                    ready = block.Contains("\"ready\":true")
                };
                foreach (Match sm in Regex.Matches(block, "\"suggestedModels\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline))
                {
                    foreach (Match model in Regex.Matches(sm.Groups[1].Value, "\"([^\"]+)\""))
                        info.suggestedModels.Add(model.Groups[1].Value);
                }

                if (!string.IsNullOrEmpty(info.id))
                    state.providers.Add(info);
            }

            return state;
        }

        private static string ExtractString(string json, string key)
        {
            var m = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]*)\"");
            if (m.Success) return m.Groups[1].Value;
            if (Regex.IsMatch(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*null")) return null;
            return null;
        }
    }
}
