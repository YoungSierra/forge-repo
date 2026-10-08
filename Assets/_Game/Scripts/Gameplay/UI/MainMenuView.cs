using ProfessorSprat.Gameplay.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>TDD §9.1 UI_MainMenu (standalone, SCN_MainMenu_Boot): Start, Continue (when a save exists), Quit.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private SaveService _save;
        [SerializeField] private string _firstLevelScene = "SCN_HydroStation_Gameplay";

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                return;
            }

            Button start = root.Q<Button>("start");
            Button resume = root.Q<Button>("continue");
            Button quit = root.Q<Button>("quit");
            if (start != null)
            {
                start.clicked += StartGame;
                start.Focus();
            }

            if (resume != null)
            {
                resume.clicked += StartGame;
                resume.style.display = _save != null && _save.HasSave ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (quit != null)
            {
                quit.clicked += Application.Quit;
            }
        }

        #endregion

        #region Private Methods

        private void StartGame()
        {
            if (Application.CanStreamedLevelBeLoaded(_firstLevelScene))
            {
                SceneManager.LoadScene(_firstLevelScene);
            }
        }

        #endregion
    }
}
