using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>TDD §9.1 UI_Loading: full-screen cover for scene loads and the respawn fade (UI Toolkit, no art).</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScreenFade : MonoBehaviour
    {
        #region Fields

        private VisualElement _cover;

        #endregion

        #region Public Methods

        public float Opacity => _cover != null ? _cover.resolvedStyle.opacity : 0f;

        public IEnumerator CoFade(float target, float duration)
        {
            Bind();
            if (_cover == null)
            {
                yield break;
            }

            float start = _cover.style.opacity.value;
            float elapsed = 0f;
            _cover.style.display = DisplayStyle.Flex;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _cover.style.opacity = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _cover.style.opacity = target;
            _cover.style.display = target > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Bind();
            if (_cover != null)
            {
                _cover.style.opacity = 0f;
                _cover.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region Private Methods

        private void Bind()
        {
            if (_cover == null)
            {
                _cover = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("loading-cover");
            }
        }

        #endregion
    }
}
