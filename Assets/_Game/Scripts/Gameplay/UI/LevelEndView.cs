using System.Collections.Generic;
using ProfessorSprat.Gameplay.Flow;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>
    /// TDD §9.1 UI_LevelEnd, as a mission-complete screen (owner request, D-287): a rank (S / A / B / C from time, hits
    /// and falls against the design time), the run stats with the best time, Play again and Main menu.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LevelEndView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private string _menuScene = "SCN_MainMenu_Boot";
        [SerializeField] private RunStats _stats;
        [Tooltip("Reference clear time (s) for the rank: S within 1.6× without hits or falls, A within 2.4× with at most one hit.")]
        [SerializeField] private float _designTime = 240f;

        private VisualElement _panel;
        private Label _results;

        #endregion

        #region Public Methods

        public bool IsVisible => _panel != null && _panel.style.display == DisplayStyle.Flex;

        public string ResultsText => _results != null ? _results.text : string.Empty;

        public string Rank { get; private set; } = string.Empty;

        public void Show(IReadOnlyList<ZoneResult> results)
        {
            Bind();
            if (_panel == null)
            {
                return;
            }

            if (_stats != null)
            {
                FillStats();
                _results.style.display = DisplayStyle.None;
            }
            else
            {
                System.Text.StringBuilder text = new System.Text.StringBuilder();
                foreach (ZoneResult result in results)
                {
                    text.AppendLine($"{result.zoneId}: {result.fliesCollected} / {result.fliesTotal} — {result.bestTimeSeconds:F1} s");
                }

                _results.text = text.ToString();
            }

            _panel.style.display = DisplayStyle.Flex;
            _panel.Q<Button>("again")?.Focus();
        }

        public static string RankFor(float time, int hits, int falls, float designTime)
        {
            if (time <= designTime * 1.6f && hits == 0 && falls == 0)
            {
                return "S";
            }

            if (time <= designTime * 2.4f && hits <= 1)
            {
                return "A";
            }

            return time <= designTime * 4f ? "B" : "C";
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Bind();
            if (_panel == null)
            {
                return;
            }

            _panel.style.display = DisplayStyle.None;
            Button menu = _panel.Q<Button>("continue");
            if (menu != null)
            {
                menu.clicked += OnContinue;
            }

            Button again = _panel.Q<Button>("again");
            if (again != null)
            {
                again.clicked += OnAgain;
            }
        }

        #endregion

        #region Private Methods

        private void FillStats()
        {
            Rank = RankFor(_stats.Elapsed, _stats.Hits, _stats.Falls, _designTime);
            _panel.Q<Label>("rank-letter").text = Rank;
            _panel.Q<Label>("rank-text").text = Rank switch
            {
                "S" => "Flawless. No hits, no splashes: B-17 mastery.",
                "A" => "Sharp climbing. Sprat is impressed.",
                "B" => "Solid run. Follow the fly trail to go faster.",
                _ => "Mission complete. The wing is yours.",
            };

            VisualElement grid = _panel.Q<VisualElement>("stats-grid");
            grid.Clear();
            float best = GameSettings.BestTime;
            AddStat(grid, "Time", PresentationView.FormatTime(_stats.Elapsed) + (_stats.NewBest ? "  ★ NEW BEST" : string.Empty), _stats.NewBest);
            AddStat(grid, "Best", best > 0f ? PresentationView.FormatTime(best) : "—", false);
            AddStat(grid, "Neon Flies", $"{_stats.FliesCollected} / {_stats.FliesTotal}", false);
            AddStat(grid, "Crabs stomped", _stats.CrabsTotal > 0 ? $"{_stats.Stomps} / {_stats.CrabsTotal}" : _stats.Stomps.ToString(), false);
            AddStat(grid, "Crab hits", _stats.Hits.ToString(), false);
            AddStat(grid, "Splashes", _stats.Falls.ToString(), false);
            AddStat(grid, "Jumps", _stats.Jumps.ToString(), false);
        }

        private static void AddStat(VisualElement grid, string name, string value, bool highlight)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList("stat-cell");
            Label n = new Label(name);
            n.AddToClassList("stat-name");
            Label v = new Label(value);
            v.AddToClassList("stat-value");
            v.EnableInClassList("stat-value--best", highlight);
            cell.Add(n);
            cell.Add(v);
            grid.Add(cell);
        }

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
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(_menuScene))
            {
                SceneManager.LoadScene(_menuScene);
            }
        }

        private void OnAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        #endregion
    }
}
