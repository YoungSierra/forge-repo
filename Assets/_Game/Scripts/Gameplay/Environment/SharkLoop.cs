using UnityEngine;
using V57.GoldPath;

namespace ProfessorSprat.Gameplay.Environment
{
    /// <summary>
    /// The shark's patrol loop (Level_01 V10, D-270): the map's shark event marker (<c>Marker_Event_shark</c>, a
    /// <c>CIRCLE_XZ</c> of radius r scaled per axis: an ellipse with semi-axes r·scale.x × r·scale.z, "patrulla eliptica")
    /// is swum around at <see cref="_speed"/>, facing the travel direction, while <c>AmbientCreature</c> plays its transit
    /// clip. Each shark keeps its own height and starts at the angle of its placed position. No marker → stays in place.
    /// </summary>
    public sealed class SharkLoop : MonoBehaviour
    {
        #region Fields

        [Tooltip("Marker id of the loop (V57Marker kind Other, from Marker_Event_<id>).")]
        [SerializeField] private string _loopMarkerId = "Event_shark";
        [Tooltip("Swim speed along the loop, metres per second.")]
        [SerializeField] private float _speed = 2.2f;
        [Tooltip("How fast the body turns to the swim direction (1/s).")]
        [SerializeField] private float _turnRate = 2f;

        private Vector3 _center;
        private float _a;
        private float _b;
        private float _angle;
        private float _height;
        private bool _active;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            V57Marker marker = V57Marker.Find(V57MarkerKind.Other, _loopMarkerId);
            if (marker == null)
            {
                return;
            }

            V57LayoutProps props = marker.GetComponent<V57LayoutProps>();
            float radius = props != null ? props.GetFloat("volume_radius", 1f) : 1f;
            Vector3 scale = marker.transform.lossyScale;
            _center = marker.transform.position;
            _a = Mathf.Max(0.5f, radius * scale.x);
            _b = Mathf.Max(0.5f, radius * scale.z);
            _height = transform.position.y;
            Vector3 local = transform.position - _center;
            _angle = Mathf.Atan2(local.z / _b, local.x / _a);
            _active = true;
            Place(1f);
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            // Constant ground speed on an ellipse: advance the angle by speed / local radius of curvature (arc length).
            float dx = -_a * Mathf.Sin(_angle);
            float dz = _b * Mathf.Cos(_angle);
            float arcPerRadian = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dz * dz));
            _angle += _speed / arcPerRadian * Time.deltaTime;
            Place(1f - Mathf.Exp(-_turnRate * Time.deltaTime));
        }

        #endregion

        #region Private Methods

        private void Place(float turnBlend)
        {
            Vector3 position = _center + new Vector3(_a * Mathf.Cos(_angle), 0f, _b * Mathf.Sin(_angle));
            position.y = _height;
            Vector3 tangent = new Vector3(-_a * Mathf.Sin(_angle), 0f, _b * Mathf.Cos(_angle));
            transform.position = position;
            if (tangent.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(tangent.normalized, Vector3.up), turnBlend);
            }
        }

        #endregion
    }
}
