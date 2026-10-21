using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flies
{
    /// <summary>
    /// Cyan breadcrumb arc to the next fly (owner request, D-287, after the reference build): one raised quadratic arc of
    /// dots from the Professor to the nearest fly still to collect, with a pulse running along it. Rebuilt on every
    /// collection and when he has moved 2 m, so it leads fly by fly; dots fade with distance.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class FlyTrail : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Color _color = new Color(0.2f, 0.95f, 1f, 1f);
        [SerializeField] private float _dotsPerMetre = 2.2f;
        [SerializeField] private float _dotSize = 0.08f;
        [SerializeField] private Vector2 _fadeDistance = new Vector2(45f, 12f);
        [SerializeField] private int _maxDots = 1500;

        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _phases = new List<float>();
        private ParticleSystem _system;
        private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[0];
        private Vector3 _player;
        private bool _hidden;
        private bool _pending;
        private Vector3 _builtFrom;
        private const float RebuildDistance = 2f;

        #endregion

        #region Public Methods

        public int DotCount => _points.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _system = GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _system.main;
            // A running, looping system with no emission: the dots come only from SetParticles.
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = _maxDots;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = _system.emission;
            emission.enabled = false;
            _system.Play();
        }

        private void OnEnable()
        {
            GameSettings.Changed += Rebuild;
            EventBus.Subscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Subscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Subscribe<PlayerMovedEvent>(OnMoved);
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Rebuild;
            EventBus.Unsubscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Unsubscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Unsubscribe<PlayerMovedEvent>(OnMoved);
        }

        private void LateUpdate()
        {
            int count = _hidden ? 0 : _points.Count;
            if (count == 0)
            {
                _system.SetParticles(_particles, 0);
                return;
            }

            if (_particles.Length < count)
            {
                _particles = new ParticleSystem.Particle[count];
            }

            Camera cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : _player;
            float t = Time.time;
            for (int i = 0; i < count; i++)
            {
                float wave = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(_phases[i] * 18f - t * 5f), 3f);
                float distance = Vector3.Distance(eye, _points[i]);
                float near = Mathf.InverseLerp(_fadeDistance.x, _fadeDistance.y, distance);
                // Mostly dim; the travelling pulse is what reads (the reference build's subtle trail).
                float alpha = (0.12f + 0.88f * wave) * near;
                Color color = _color;
                color.a = alpha;
                ref ParticleSystem.Particle particle = ref _particles[i];
                particle.position = _points[i];
                particle.startColor = color;
                particle.startSize = _dotSize * (0.7f + 0.9f * wave);
                particle.remainingLifetime = 10f;
                particle.startLifetime = 10f;
            }

            _system.SetParticles(_particles, count);
        }

        #endregion

        #region Private Methods

        private void OnZoneStarted(ZoneStartedEvent started)
        {
            // Built on the first Professor position, so the chain starts at the fly nearest to him.
            _hidden = false;
            _pending = true;
        }

        private void OnFlyCollected(FlyCollectedEvent collected) => Rebuild();

        private void OnZoneCompleted(ZoneCompletedEvent completed)
        {
            _hidden = true;
            _points.Clear();
            _phases.Clear();
        }

        private void OnMoved(PlayerMovedEvent moved)
        {
            _player = moved.Position;
            if (_pending || !_hidden && (_player - _builtFrom).sqrMagnitude > RebuildDistance * RebuildDistance)
            {
                _pending = false;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            _points.Clear();
            _phases.Clear();
            List<Vector3> left = new List<Vector3>();
            foreach (FlyController fly in FindObjectsByType<FlyController>(FindObjectsSortMode.None))
            {
                if (fly.isActiveAndEnabled && !fly.Collected)
                {
                    left.Add(fly.transform.position);
                }
            }

            // One arc only: from the Professor's chest to the nearest fly still to catch. Catching it (or moving 2 m)
            // rebuilds the arc toward the next one, so the trail leads fly by fly instead of drawing the whole route.
            _builtFrom = _player;
            if (left.Count == 0)
            {
                return;
            }

            Vector3 a = _player + Vector3.up * 1f;
            Vector3 b = left[0];
            float bestDistance = float.MaxValue;
            foreach (Vector3 fly in left)
            {
                float d2 = (fly - _player).sqrMagnitude;
                if (d2 < bestDistance)
                {
                    bestDistance = d2;
                    b = fly;
                }
            }

            float d = Vector3.Distance(a, b);
            int n = Mathf.Max(4, Mathf.RoundToInt(d * _dotsPerMetre));
            Vector3 mid = Vector3.Lerp(a, b, 0.5f) + Vector3.up * (0.5f + d * 0.06f);
            for (int k = 1; k < n && _points.Count < _maxDots; k++)
            {
                float u = k / (float)n;
                _points.Add((1f - u) * (1f - u) * a + 2f * u * (1f - u) * mid + u * u * b);
                _phases.Add(u * Mathf.Max(1f, d / 6f));
            }
        }

        #endregion
    }
}
