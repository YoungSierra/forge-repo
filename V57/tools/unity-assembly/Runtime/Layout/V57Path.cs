using System;
using System.Collections.Generic;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// A level path from the map (unity_scene/1.1 <c>trayectorias</c>): a sampled polyline in world space, created in Edit
    /// Mode by the assembly under <c>_Environment/_Paths</c>. Gameplay looks it up by name (<see cref="Find"/>, a registry,
    /// not GameObject.Find) and samples it by distance (<see cref="Evaluate"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class V57Path : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private string _kind = string.Empty;
        [SerializeField] private string _actor = string.Empty;
        [SerializeField] private bool _loop;
        [SerializeField] private string _mode = string.Empty;
        [SerializeField] private float _speed;
        [SerializeField] private string _zone = string.Empty;
        [SerializeField] private Vector3[] _points = new Vector3[0];
        [SerializeField] private string[] _instances = new string[0];

        #endregion

        #region Fields

        private static readonly List<V57Path> Active = new List<V57Path>();

        private float[] _cumulative;

        #endregion

        #region Public Methods

        public static IReadOnlyList<V57Path> All => Active;

        public string Kind => _kind;

        public string Actor => _actor;

        public bool Loop => _loop;

        public string Mode => _mode;

        public float Speed => _speed;

        public string Zone => _zone;

        public IReadOnlyList<Vector3> Points => _points;

        public IReadOnlyList<string> Instances => _instances;

        /// <summary>Total length in metres (closing segment included when the path loops).</summary>
        public float Length
        {
            get
            {
                EnsureLengths();
                return _cumulative.Length > 0 ? _cumulative[_cumulative.Length - 1] : 0f;
            }
        }

        public static V57Path Find(string name)
        {
            foreach (V57Path path in Active)
            {
                if (path != null && string.Equals(path.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>Edit-mode setup used by the assembly tools (points in world space).</summary>
        public void Configure(string kind, string actor, bool loop, string mode, float speed, string zone, Vector3[] points, string[] instances)
        {
            _kind = kind ?? string.Empty;
            _actor = actor ?? string.Empty;
            _loop = loop;
            _mode = mode ?? string.Empty;
            _speed = speed;
            _zone = zone ?? string.Empty;
            _points = points ?? new Vector3[0];
            _instances = instances ?? new string[0];
            _cumulative = null;
        }

        /// <summary>Position and unit tangent at <paramref name="distance"/> metres along the path (wrapped when looping, clamped otherwise).</summary>
        public void Evaluate(float distance, out Vector3 position, out Vector3 tangent)
        {
            EnsureLengths();
            int count = _points.Length;
            if (count == 0)
            {
                position = transform.position;
                tangent = Vector3.forward;
                return;
            }

            if (count == 1)
            {
                position = _points[0];
                tangent = Vector3.forward;
                return;
            }

            float length = Length;
            distance = _loop ? Mathf.Repeat(distance, Mathf.Max(length, 0.0001f)) : Mathf.Clamp(distance, 0f, length);
            int segments = _loop ? count : count - 1;
            int segment = 0;
            while (segment < segments - 1 && _cumulative[segment + 1] < distance)
            {
                segment++;
            }

            Vector3 a = _points[segment];
            Vector3 b = _points[(segment + 1) % count];
            float span = _cumulative[segment + 1] - _cumulative[segment];
            float t = span > 0.0001f ? (distance - _cumulative[segment]) / span : 0f;
            position = Vector3.Lerp(a, b, t);
            Vector3 delta = b - a;
            tangent = delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector3.forward;
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i + 1 < _points.Length; i++)
            {
                Gizmos.DrawLine(_points[i], _points[i + 1]);
            }

            if (_loop && _points.Length > 2)
            {
                Gizmos.DrawLine(_points[_points.Length - 1], _points[0]);
            }
        }

        #endregion

        #region Private Methods

        private void EnsureLengths()
        {
            int count = _points.Length;
            int segments = count < 2 ? 0 : (_loop ? count : count - 1);
            if (_cumulative != null && _cumulative.Length == segments + 1)
            {
                return;
            }

            _cumulative = new float[segments + 1];
            for (int i = 0; i < segments; i++)
            {
                _cumulative[i + 1] = _cumulative[i] + Vector3.Distance(_points[i], _points[(i + 1) % count]);
            }
        }

        #endregion
    }
}
