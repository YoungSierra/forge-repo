using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Flow;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>
    /// Presentation layer over play (D-287): run timer, centre banners, speech bubbles anchored over the Professor and
    /// Sprat, the level caption, the training card with its world marker, and full-screen flashes. Driven by
    /// <see cref="BannerEvent"/>, <see cref="BarkEvent"/>, <see cref="ScreenFlashEvent"/> and the public API used by
    /// the training and intro systems.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PresentationView : MonoBehaviour
    {
        #region Fields

        private const float BannerHold = 2.4f;
        private const float BubbleFadeOut = 0.25f;

        [SerializeField] private RunStats _stats;
        [SerializeField] private Transform _professor;
        [SerializeField] private Transform _sprat;
        [Tooltip("Bubble anchor height over the Professor's pivot (m).")]
        [SerializeField] private float _professorHead = 2.1f;
        [Tooltip("Bubble anchor height over Sprat's pivot (m).")]
        [SerializeField] private float _spratHead = 0.55f;

        private readonly List<Bubble> _bubbles = new List<Bubble>();
        private VisualElement _root;
        private VisualElement _bubbleLayer;
        private VisualElement _banner;
        private Label _bannerTitle;
        private Label _bannerSub;
        private Label _timer;
        private Label _fps;
        private VisualElement _caption;
        private VisualElement _tutorial;
        private VisualElement _tutorialDots;
        private VisualElement _marker;
        private VisualElement _flash;
        private float _bannerRemaining;
        private float _captionRemaining;
        private float _flashPeak;
        private float _flashDuration;
        private float _flashElapsed = 1f;
        private Vector3? _markerTarget;
        private float _fpsTimer;
        private int _fpsFrames;

        private sealed class Bubble
        {
            public VisualElement Element;
            public Speaker Speaker;
            public float Remaining;
        }

        #endregion

        #region Public Methods

        public string BannerText => _bannerTitle != null ? _bannerTitle.text : string.Empty;

        public bool BannerVisible => _bannerRemaining > 0f;

        public int BubbleCount => _bubbles.Count;

        public string TimerText => _timer != null ? _timer.text : string.Empty;

        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float rest = seconds - minutes * 60f;
            return $"{minutes}:{rest:00.0}";
        }

        /// <summary>The level title card (kicker / title / objective) for <paramref name="duration"/> seconds.</summary>
        public void ShowCaption(string kicker, string title, string text, float duration)
        {
            if (_caption == null)
            {
                return;
            }

            _caption.Q<Label>("caption-kicker").text = kicker;
            _caption.Q<Label>("caption-title").text = title;
            _caption.Q<Label>("caption-text").text = text;
            _caption.AddToClassList("caption--show");
            _captionRemaining = duration;
        }

        public void ShowTutorial(int step, int total, string title, string text, string skipHint)
        {
            if (_tutorial == null)
            {
                return;
            }

            _tutorial.Q<Label>("tut-step").text = $"TRAINING {step + 1} / {total}";
            _tutorial.Q<Label>("tut-title").text = title;
            _tutorial.Q<Label>("tut-text").text = text;
            _tutorial.Q<Label>("tut-skip").text = skipHint;
            _tutorial.RemoveFromClassList("tutorial--ok");
            _tutorial.AddToClassList("tutorial--show");
            _tutorialDots.Clear();
            for (int i = 0; i < total; i++)
            {
                VisualElement dot = new VisualElement();
                dot.AddToClassList("tutorial-dot");
                dot.EnableInClassList("tutorial-dot--done", i < step);
                dot.EnableInClassList("tutorial-dot--current", i == step);
                _tutorialDots.Add(dot);
            }
        }

        public void MarkTutorialStepDone(int step)
        {
            _tutorial?.AddToClassList("tutorial--ok");
            if (_tutorialDots != null && step < _tutorialDots.childCount)
            {
                VisualElement dot = _tutorialDots[step];
                dot.RemoveFromClassList("tutorial-dot--current");
                dot.AddToClassList("tutorial-dot--done");
            }
        }

        public void HideTutorial()
        {
            _tutorial?.RemoveFromClassList("tutorial--show");
            SetMarker(null);
        }

        /// <summary>World point the training marker floats over; null hides it.</summary>
        public void SetMarker(Vector3? world) => _markerTarget = world;

        public void SetTimerTraining(bool training) => _timer?.EnableInClassList("run-timer--training", training);

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _bubbleLayer = _root.Q<VisualElement>("bubbles");
            _banner = _root.Q<VisualElement>("banner");
            _bannerTitle = _root.Q<Label>("banner-title");
            _bannerSub = _root.Q<Label>("banner-sub");
            _timer = _root.Q<Label>("run-timer");
            _fps = _root.Q<Label>("fps");
            _caption = _root.Q<VisualElement>("caption");
            _tutorial = _root.Q<VisualElement>("tutorial");
            _tutorialDots = _root.Q<VisualElement>("tut-dots");
            _marker = _root.Q<VisualElement>("world-marker");
            _flash = _root.Q<VisualElement>("flash");
            EventBus.Subscribe<BannerEvent>(OnBanner);
            EventBus.Subscribe<BarkEvent>(OnBark);
            EventBus.Subscribe<ScreenFlashEvent>(OnFlash);
            EventBus.Subscribe<ZoneClearedEvent>(OnCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BannerEvent>(OnBanner);
            EventBus.Unsubscribe<BarkEvent>(OnBark);
            EventBus.Unsubscribe<ScreenFlashEvent>(OnFlash);
            EventBus.Unsubscribe<ZoneClearedEvent>(OnCleared);
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (_timer != null && _stats != null)
            {
                _timer.text = FormatTime(_stats.Elapsed);
            }

            TickBanner(dt);
            TickCaption(dt);
            TickFlash(dt);
            TickBubbles(Time.deltaTime);
            TickMarker();
            TickFps(dt);
        }

        #endregion

        #region Private Methods

        private void OnBanner(BannerEvent banner)
        {
            if (_banner == null)
            {
                return;
            }

            _bannerTitle.text = banner.Title;
            _bannerSub.text = banner.Subtitle;
            _bannerSub.style.display = string.IsNullOrEmpty(banner.Subtitle) ? DisplayStyle.None : DisplayStyle.Flex;
            _banner.AddToClassList("banner--show");
            _bannerRemaining = BannerHold;
        }

        private void OnBark(BarkEvent bark)
        {
            if (_bubbleLayer == null)
            {
                return;
            }

            // One bubble at a time per scene: older ones close quickly.
            foreach (Bubble old in _bubbles)
            {
                old.Remaining = Mathf.Min(old.Remaining, BubbleFadeOut);
            }

            VisualElement element = new VisualElement();
            element.AddToClassList("bubble");
            element.pickingMode = PickingMode.Ignore;
            VisualElement tag = new VisualElement();
            tag.AddToClassList("bubble-tag");
            tag.AddToClassList(bark.Speaker == Speaker.Sprat ? "bubble-tag--sprat" : "bubble-tag--professor");
            Label initial = new Label(bark.Speaker == Speaker.Sprat ? "S" : "W");
            initial.AddToClassList("bubble-initial");
            tag.Add(initial);
            Label text = new Label(bark.Text);
            text.AddToClassList("bubble-text");
            VisualElement tail = new VisualElement();
            tail.AddToClassList("bubble-tail");
            element.Add(tail);
            element.Add(tag);
            element.Add(text);
            _bubbleLayer.Add(element);
            element.schedule.Execute(() => element.AddToClassList("bubble--show"));
            _bubbles.Add(new Bubble { Element = element, Speaker = bark.Speaker, Remaining = bark.Duration });
        }

        private void OnFlash(ScreenFlashEvent flash)
        {
            if (_flash == null)
            {
                return;
            }

            _flash.EnableInClassList("flash--blue", flash.Kind == FlashKind.Blue);
            _flashPeak = GameSettings.ScreenFlashes ? flash.Peak : flash.Peak * 0.25f;
            _flashDuration = Mathf.Max(0.05f, flash.Duration);
            _flashElapsed = 0f;
        }

        private void OnCleared(ZoneClearedEvent cleared)
        {
            if (cleared.IsLastZone)
            {
                HideTutorial();
            }
        }

        private void TickBanner(float dt)
        {
            if (_bannerRemaining <= 0f)
            {
                return;
            }

            _bannerRemaining -= dt;
            if (_bannerRemaining <= 0f)
            {
                _banner.RemoveFromClassList("banner--show");
            }
        }

        private void TickCaption(float dt)
        {
            if (_captionRemaining <= 0f)
            {
                return;
            }

            _captionRemaining -= dt;
            if (_captionRemaining <= 0f)
            {
                _caption.RemoveFromClassList("caption--show");
            }
        }

        private void TickFlash(float dt)
        {
            if (_flash == null || _flashElapsed >= 1f)
            {
                return;
            }

            _flashElapsed = Mathf.Min(1f, _flashElapsed + dt / _flashDuration);
            float k = 1f - _flashElapsed;
            _flash.style.opacity = _flashPeak * k * k;
        }

        private void TickBubbles(float dt)
        {
            Camera cam = Camera.main;
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = _bubbles[i];
                bubble.Remaining -= dt;
                if (bubble.Remaining <= 0f)
                {
                    bubble.Element.RemoveFromHierarchy();
                    _bubbles.RemoveAt(i);
                    continue;
                }

                if (bubble.Remaining < BubbleFadeOut)
                {
                    bubble.Element.RemoveFromClassList("bubble--show");
                }

                Transform anchor = bubble.Speaker == Speaker.Sprat ? _sprat : _professor;
                float height = bubble.Speaker == Speaker.Sprat ? _spratHead : _professorHead;
                if (cam == null || anchor == null || _root.panel == null)
                {
                    continue;
                }

                // Off-screen speakers keep their line readable: the bubble is clamped to the screen edge (behind the
                // camera, it falls back to over the Professor).
                Vector3 world = anchor.position + Vector3.up * height;
                if (Vector3.Dot(world - cam.transform.position, cam.transform.forward) <= 0.1f && _professor != null)
                {
                    world = _professor.position + Vector3.up * _professorHead;
                }

                Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, cam);
                Rect area = _root.layout;
                float halfWidth = Mathf.Max(120f, bubble.Element.layout.width * 0.5f);
                float height2 = Mathf.Max(80f, bubble.Element.layout.height);
                bubble.Element.style.left = Mathf.Clamp(p.x, halfWidth + 24f, area.width - halfWidth - 24f);
                bubble.Element.style.top = Mathf.Clamp(p.y, height2 + 24f, area.height - 140f);
            }
        }

        private void TickMarker()
        {
            if (_marker == null)
            {
                return;
            }

            Camera cam = Camera.main;
            bool show = false;
            if (_markerTarget.HasValue && cam != null && _root.panel != null)
            {
                Vector3 world = _markerTarget.Value + Vector3.up * (0.9f + 0.08f * Mathf.Sin(Time.unscaledTime * 5f));
                Vector3 view = cam.WorldToViewportPoint(world);
                show = view.z > 0f && view.x > -0.05f && view.x < 1.05f && view.y > -0.05f && view.y < 1.05f;
                if (show)
                {
                    Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, cam);
                    _marker.style.left = p.x;
                    _marker.style.top = p.y;
                }
            }

            _marker.EnableInClassList("world-marker--show", show);
        }

        private void TickFps(float dt)
        {
            if (_fps == null)
            {
                return;
            }

            bool show = GameSettings.ShowFps;
            _fps.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
            {
                return;
            }

            _fpsFrames++;
            _fpsTimer += dt;
            if (_fpsTimer >= 0.5f)
            {
                _fps.text = $"{_fpsFrames / _fpsTimer:0} FPS";
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }
        }

        #endregion
    }
}
