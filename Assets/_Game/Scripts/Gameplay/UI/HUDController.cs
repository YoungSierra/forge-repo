using ProfessorSprat.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>
    /// TDD §9.1 UI_HUDCounter via §B-S HUDController: the fly counter "N / T" (pop 1.0 → 1.35 → 1.0 in 200 ms on each
    /// collect, with the starburst flaring behind the fly icon) and the key glyph while the key is carried. Art: the owner's
    /// UI sheet v02 (D-268); a pulsing amber halo behind the banner marks the last fly.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HUDController : MonoBehaviour
    {
        #region Fields

        private const float PopDuration = 0.2f;
        private const float PopScale = 1.35f;
        private const float LastPulseSpeed = 4f;

        private Label _counter;
        private VisualElement _counterGroup;
        private VisualElement _burst;
        private VisualElement _lastFrame;
        private bool _lastFly;
        private VisualElement _keyGlyph;
        private int _collected;
        private int _total;
        private float _popElapsed = PopDuration;

        #endregion

        #region Public Methods

        public string CounterText => _counter != null ? _counter.text : string.Empty;

        public bool KeyGlyphVisible => _keyGlyph != null && _keyGlyph.style.display == DisplayStyle.Flex;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _counter = root?.Q<Label>("fly-counter");
            _keyGlyph = root?.Q<VisualElement>("key-glyph");
            _counterGroup = root?.Q<VisualElement>("counter-group");
            _burst = root?.Q<VisualElement>("fly-burst");
            _lastFrame = root?.Q<VisualElement>("counter-frame");
            Refresh();
            ShowKey(false);
            EventBus.Subscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Subscribe<KeyCollectedEvent>(OnKeyCollected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Unsubscribe<KeyCollectedEvent>(OnKeyCollected);
        }

        private void Update()
        {
            if (_lastFly && _lastFrame != null)
            {
                // Last fly: the amber halo behind the banner breathes (unscaled, so it keeps pulsing in pause).
                _lastFrame.style.opacity = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * LastPulseSpeed);
            }

            if (_counter == null || _popElapsed >= PopDuration)
            {
                return;
            }

            _popElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_popElapsed / PopDuration);
            float scale = 1f + (PopScale - 1f) * Mathf.Sin(t * Mathf.PI);
            _counter.style.scale = new Scale(new Vector3(scale, scale, 1f));
            if (_burst != null)
            {
                float flare = 0.6f + 0.7f * t;
                _burst.style.opacity = Mathf.Sin(t * Mathf.PI);
                _burst.style.scale = new Scale(new Vector3(flare, flare, 1f));
                _burst.style.rotate = new Rotate(new Angle(40f * t, AngleUnit.Degree));
            }
        }

        #endregion

        #region Private Methods

        private void OnZoneStarted(ZoneStartedEvent started)
        {
            _collected = 0;
            _total = started.ZoneTotal;
            ShowKey(false);
            Refresh();
        }

        private void OnFlyCollected(FlyCollectedEvent collected)
        {
            _collected = collected.NewCount;
            _total = collected.ZoneTotal;
            _popElapsed = 0f;
            Refresh();
        }

        private void OnKeyCollected(KeyCollectedEvent collected)
        {
            ShowKey(true);
        }

        private void Refresh()
        {
            if (_counter != null)
            {
                _counter.text = $"{_collected} / {_total}";
                _lastFly = _total > 0 && _total - _collected == 1;
                _counter.EnableInClassList("hud-counter--last", _lastFly);
                _counterGroup?.EnableInClassList("counter-group--last", _lastFly);
            }
        }

        private void ShowKey(bool visible)
        {
            if (_keyGlyph != null)
            {
                _keyGlyph.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        #endregion
    }
}
