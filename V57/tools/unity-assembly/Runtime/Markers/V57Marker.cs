using System;
using System.Collections.Generic;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Typed level marker created in Edit Mode by <c>AssemblyRunner.BuildLevelScenes()</c> from the blockout's
    /// <c>Marker_*</c> nodes. Gameplay reads spawn points, zones and camera placements from here instead of
    /// searching the hierarchy at runtime (<see cref="Find"/> uses a registry, not GameObject.Find).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class V57Marker : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private V57MarkerKind _kind = V57MarkerKind.Other;
        [SerializeField] private string _id = string.Empty;
        [Tooltip("Local-space bounds of the source marker mesh (zones/bounds); zero size for empties.")]
        [SerializeField] private Bounds _localBounds;

        #endregion

        #region Fields

        private static readonly List<V57Marker> Active = new List<V57Marker>();

        #endregion

        #region Public Methods

        public static IReadOnlyList<V57Marker> All => Active;

        public V57MarkerKind Kind => _kind;

        public string Id => _id;

        public Bounds LocalBounds => _localBounds;

        public static V57Marker Find(V57MarkerKind kind, string id)
        {
            foreach (V57Marker marker in Active)
            {
                if (marker != null && marker._kind == kind && string.Equals(marker._id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return marker;
                }
            }

            return null;
        }

        /// <summary>Edit-mode setup used by the assembly tools.</summary>
        public void Configure(V57MarkerKind kind, string id, Bounds localBounds)
        {
            _kind = kind;
            _id = id ?? string.Empty;
            _localBounds = localBounds;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _kind == V57MarkerKind.Zone ? Color.cyan : _kind == V57MarkerKind.Spawn ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 size = _localBounds.size == Vector3.zero ? Vector3.one * 0.25f : _localBounds.size;
            Gizmos.DrawWireCube(_localBounds.center, size);
        }

        #endregion

        #region Private Methods

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
        }

        #endregion
    }
}
