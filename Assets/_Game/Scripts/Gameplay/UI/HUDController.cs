using ProfessorSprat.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>
    /// TDD §9.1 UI_HUDCounter via §B-S HUDController: the fly counter "N / T" (pop 1.0 → 1.35 → 1.0 in 200 ms on each
    /// collect) and the key glyph while the key is carried. Text only until UI art is delivered.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HUDController : MonoBehaviour
    {
        #region Fields

        private const float PopDuration = 0.2f;
        private const float PopScale = 1.35f;

        private Label _counter;
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
            if (_counter == null || _popElapsed >= PopDuration)
            {
                return;
            }

            _popElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_popElapsed / PopDuration);
            float scale = 1f + (PopScale - 1f) * Mathf.Sin(t * Mathf.PI);
            _counter.style.scale = new Scale(new Vector3(scale, scale, 1f));
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
                _counter.EnableInClassList("hud-counter--last", _total > 0 && _total - _collected == 1);
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
