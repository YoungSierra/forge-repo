using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProfessorSprat.Gameplay.Flies
{
    /// <summary>
    /// Fly beacons, an accessibility assist (D-287): a soft cyan light cone rising from every fly still to catch, so they
    /// can be found from afar. Drawn as instanced meshes (no scene objects) with the light-shaft shader, only while the
    /// "fly beacons" option is on; a beam, not dots, so it never reads as the breadcrumb trail.
    /// </summary>
    public sealed class FlyBeacons : MonoBehaviour
    {
        #region Fields

        [Tooltip("Open cone, pivot at its narrow end (MESH_FX_ShaftCone).")]
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;
        [SerializeField] private float _height = 7f;
        [SerializeField] private float _radius = 0.8f;

        private readonly List<FlyController> _flies = new List<FlyController>();
        private readonly List<Matrix4x4> _matrices = new List<Matrix4x4>();

        #endregion

        #region Public Methods

        public int BeaconCount => GameSettings.FlyBeacons ? _matrices.Count : 0;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
        }

        private void LateUpdate()
        {
            if (!GameSettings.FlyBeacons || _mesh == null || _material == null)
            {
                return;
            }

            // Flies hover in place, so their matrices are rebuilt only when the list changes; drawn every frame.
            if (_matrices.Count > 0)
            {
                RenderParams rp = new RenderParams(_material) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = gameObject.layer };
                Graphics.RenderMeshInstanced(rp, _mesh, 0, _matrices);
            }
        }

        #endregion

        #region Private Methods

        private void OnZoneStarted(ZoneStartedEvent started)
        {
            _flies.Clear();
            _flies.AddRange(FindObjectsByType<FlyController>(FindObjectsSortMode.None));
            Rebuild();
        }

        private void OnFlyCollected(FlyCollectedEvent collected) => Rebuild();

        private void Rebuild()
        {
            _matrices.Clear();
            // The cone's pivot is its narrow end: flipped upside down, it starts at the fly and widens upward.
            Quaternion up = Quaternion.Euler(180f, 0f, 0f);
            Vector3 scale = new Vector3(_radius, _height, _radius);
            foreach (FlyController fly in _flies)
            {
                if (fly != null && fly.isActiveAndEnabled && !fly.Collected)
                {
                    _matrices.Add(Matrix4x4.TRS(fly.transform.position, up, scale));
                }
            }
        }

        #endregion
    }
}
