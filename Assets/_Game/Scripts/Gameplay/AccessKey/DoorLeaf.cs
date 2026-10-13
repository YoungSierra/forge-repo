using UnityEngine;

namespace ProfessorSprat.Gameplay.AccessKey
{
    /// <summary>
    /// One leaf of a lab double door (gameplay prefab of AST-ENV-LABDOOR-001, D-270): its blocking collider lives on this
    /// prefab with the mesh, so sliding the leaf opens the passage. A <see cref="ZoneDoorController"/> in leaves mode
    /// (from the map's Door marker) takes the leaves within reach and slides each to its own side when unlocked.
    /// </summary>
    public sealed class DoorLeaf : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Collider _blocker;

        #endregion

        #region Public Methods

        public Collider Blocker => _blocker;

        /// <summary>Leaf width (metres) along the given horizontal axis, from the blocker bounds.</summary>
        public float WidthAlong(Vector3 axis)
        {
            if (_blocker == null)
            {
                return 1.5f;
            }

            Vector3 size = _blocker.bounds.size;
            return Mathf.Abs(axis.x) * size.x + Mathf.Abs(axis.z) * size.z;
        }

        #endregion
    }
}
