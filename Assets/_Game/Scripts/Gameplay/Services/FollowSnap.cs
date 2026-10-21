using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>
    /// Keeps an ambient effect (the floating motes) around the Professor: the object jumps to his position snapped to a
    /// coarse grid, so a world-space particle volume always surrounds him without visibly following (D-287).
    /// </summary>
    public sealed class FollowSnap : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float _grid = 10f;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnMoved);
        }

        #endregion

        #region Private Methods

        private void OnMoved(PlayerMovedEvent moved)
        {
            Vector3 p = moved.Position;
            transform.position = new Vector3(Mathf.Round(p.x / _grid) * _grid, Mathf.Round(p.y / _grid) * _grid, Mathf.Round(p.z / _grid) * _grid);
        }

        #endregion
    }
}
