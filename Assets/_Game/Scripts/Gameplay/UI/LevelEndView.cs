using System.Collections.Generic;
using ProfessorSprat.Gameplay.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>TDD §9.1 UI_LevelEnd: flies per zone and time; Continue returns to the menu scene.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LevelEndView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private string _menuScene = "SCN_MainMenu_Boot";

        private VisualElement _panel;
        private Label _results;

        #endregion

        #region Public Methods

        public bool IsVisible => _panel != null && _panel.style.display == DisplayStyle.Flex;

        public string ResultsText => _results != null ? _results.text : string.Empty;

        public void Show(IReadOnlyList<ZoneResult> results)
        {
            Bind();
            if (_panel == null)
            {
                return;
            }

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            foreach (ZoneResult result in results)
            {
                text.AppendLine($"{result.zoneId}: {result.fliesCollected} / {result.fliesTotal} — {result.bestTimeSeconds:F1} s");
            }

            _results.text = text.ToString();
            _panel.style.display = DisplayStyle.Flex;
            _panel.Q<Button>("continue")?.Focus();
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Bind();
            if (_panel != null)
            {
                _panel.style.display = DisplayStyle.None;
                Button button = _panel.Q<Button>("continue");
                if (button != null)
                {
                    button.clicked += OnContinue;
                }
            }
        }

        #endregion

        #region Private Methods

        private void Bind()
        {
            if (_panel != null)
            {
                return;
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _panel = root?.Q<VisualElement>("level-end");
            _results = root?.Q<Label>("level-end-results");
        }

        private void OnContinue()
        {
            if (Application.CanStreamedLevelBeLoaded(_menuScene))
            {
                SceneManager.LoadScene(_menuScene);
            }
        }

        #endregion
    }
}
