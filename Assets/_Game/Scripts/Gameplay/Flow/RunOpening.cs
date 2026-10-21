using System.Collections;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Options;
using ProfessorSprat.Gameplay.UI;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// Start of a run (owner request, D-287): the level title card with the objective, then, once the opening shot has
    /// handed over to play, the training (if enabled) or Sprat's first line.
    /// </summary>
    public sealed class RunOpening : MonoBehaviour
    {
        #region Fields

        [SerializeField] private PresentationView _view;
        [SerializeField] private TutorialService _tutorial;
        [SerializeField] private MissionCues _cues;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private RunStats _stats;
        [SerializeField] private string _kicker = "SECTOR 7 · HYDRO LABS";
        [SerializeField] private string _title = "RESEARCH WING B-17";
        [SerializeField] private float _captionSeconds = 4f;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<ZoneStartedEvent>(OnZoneStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneStartedEvent>(OnZoneStarted);
        }

        #endregion

        #region Private Methods

        private void OnZoneStarted(ZoneStartedEvent started)
        {
            _view?.ShowCaption(_kicker, _title, $"Catch all {started.ZoneTotal} Neon Flies — the access key appears when the last one is caught.", _captionSeconds);
            StartCoroutine(BeginPlay());
        }

        private IEnumerator BeginPlay()
        {
            // Let the intro opening shot (CM_Opening) hand over before training talks over it.
            yield return null;
            while (_cameraRig != null && _cameraRig.OpeningActive)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.2f);
            if (GameSettings.Tutorial && _tutorial != null)
            {
                _tutorial.Begin();
            }
            else
            {
                _stats?.Hold(false);
                _cues?.RunStarted(false);
            }
        }

        #endregion
    }
}
