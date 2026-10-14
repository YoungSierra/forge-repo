using System.Collections.Generic;
using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Environment
{
    /// <summary>
    /// Free-roaming shark (owner request, D-279; replaces the fixed ellipse loop). At start each shark samples open-water
    /// nodes around where it was placed, where its whole body can turn without touching any collider in the scene (a
    /// sphere of its half length plus a margin). It then swims node to node, and a leg is taken only when the body
    /// (a sphere of its half width plus the margin) sweeps it without touching anything. Legs also keep away from the
    /// other sharks and the Professor. The shark slows down while it turns, so turns happen inside the clear node.
    /// The delivered clips play as manoeuvres:
    /// <list type="bullet">
    /// <item>TurnLeft/TurnRight on sharp heading changes, TurnUp/TurnDown on climbs and dives;</item>
    /// <item>Attack as a lunge with a speed burst along a long clear leg;</item>
    /// <item>EyeRotation when it slows down to look around, or when the Professor comes close.</item>
    /// </list>
    /// Speed, turn rate, aggression, curiosity and preferred depth are seeded by the shark's name, so each shark behaves
    /// differently. Every manoeuvre clip starts and ends on the transit clip's first pose, so a manoeuvre waits for the
    /// next point of the swim cycle that matches that pose (one per tail beat), and transit resumes from its first frame:
    /// no pose jumps between clips. Ambient only: no collider, no gameplay effect (TDD AmbientCreatures).
    /// </summary>
    public sealed class SharkCruise : MonoBehaviour
    {
        #region Fields

        private const string Transit = "PassiveTransit";
        private const string Attack = "Attack";
        private const string Look = "EyeRotation";
        private const float ManoeuvreLength = 2.5f;
        private const float LookLength = 4f;
        private const float SyncFade = 0.2f;
        private const float SyncTolerance = 0.1f;

        private static readonly List<SharkCruise> Sharks = new List<SharkCruise>();

        [SerializeField] private Animator _animator;
        [Header("Body (metres, shark space: pivot at the belly, +Z forward)")]
        [Tooltip("Height of the body centre above the pivot.")]
        [SerializeField] private float _bodyCentre = 4f;
        [Tooltip("Half width with the fins.")]
        [SerializeField] private float _halfWidth = 4.9f;
        [Tooltip("Half length (nose to tail / 2).")]
        [SerializeField] private float _halfLength = 8.5f;
        [SerializeField] private float _clearance = 1f;
        [Header("Roaming")]
        [Tooltip("Horizontal radius around the placed position.")]
        [SerializeField] private float _roamRadius = 70f;
        [Tooltip("Height band relative to the placed position (min, max).")]
        [SerializeField] private Vector2 _heightBand = new Vector2(-2f, 20f);
        [SerializeField] private int _samples = 1800;
        [Tooltip("Leg length range (min, max).")]
        [SerializeField] private Vector2 _legLength = new Vector2(12f, 36f);
        [SerializeField] private float _sharkSpacing = 24f;
        [SerializeField] private float _playerSpacing = 12f;
        [SerializeField] private float _maxPitch = 18f;
        [SerializeField] private float _playerLookRange = 16f;

        private readonly List<Vector3> _nodes = new List<Vector3>();
        private readonly Dictionary<long, bool> _legs = new Dictionary<long, bool>();
        private readonly List<float> _syncPhases = new List<float>();
        private string _pending;
        private float _transitPhase;
        private System.Random _random;
        private Vector3 _home;
        private int _node = -1;
        private int _previousNode = -1;
        private int _targetNode = -1;
        private Vector3 _playerPosition;
        private bool _hasPlayer;
        private float _speed;
        private float _turnRate;
        private float _aggression;
        private float _curiosity;
        private float _preferredHeight;
        private float _speedFactor = 1f;
        private float _manoeuvreRemaining;
        private float _attackRemaining;
        private float _lookRemaining;
        private float _nextAttack;
        private float _lookCooldown;

        #endregion

        #region Public Methods

        public Vector3 Target => _targetNode >= 0 ? _nodes[_targetNode] : transform.position;

        public string Manoeuvre { get; private set; }

        public int NodeCount => _nodes.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            int seed = 17;
            foreach (char c in name)
            {
                seed = seed * 31 + c;
            }

            _random = new System.Random(seed);
            _speed = Range(1.6f, 2.5f);
            _turnRate = Range(16f, 28f);
            _aggression = Range(0.25f, 1f);
            _curiosity = Range(0.15f, 0.6f);
            _preferredHeight = Range(_heightBand.x, _heightBand.y);
            _nextAttack = Range(6f, 12f) / _aggression;
        }

        private void OnEnable()
        {
            Sharks.Add(this);
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            Sharks.Remove(this);
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void Start()
        {
            _home = transform.position;
            SampleNodes();
            if (_nodes.Count < 2)
            {
                Debug.LogWarning($"{nameof(SharkCruise)} on {name}: no open water around {_home} for its body; stays in place.", this);
                enabled = false;
                return;
            }

            // Start on the clear node nearest to where the map placed it (the placed pose may overlap the scenery).
            _node = Nearest(_home);
            transform.position = _nodes[_node];
            FindSyncPhases();
            CrossFade(Transit, 0f);
            ChooseNextLeg();
            Vector3 first = Target - transform.position;
            if (Flat(first).sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(Flat(first));
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            TickManoeuvres(dt);
            Vector3 toTarget = Target - transform.position;
            if (toTarget.magnitude < Mathf.Max(1.5f, _speed * _speedFactor * dt * 2f))
            {
                transform.position = Target;
                _previousNode = _node;
                _node = _targetNode;
                OnArrived();
                toTarget = Target - transform.position;
            }

            Steer(toTarget, dt);
            // Slow down while not facing the leg, so the turn happens inside the clear node.
            float align = toTarget.sqrMagnitude > 0.01f ? Vector3.Dot(transform.forward, toTarget.normalized) : 1f;
            float turnSlow = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01((align - 0.5f) / 0.5f));
            float step = Mathf.Min(toTarget.magnitude, _speed * _speedFactor * turnSlow * dt);
            // Always along the leg line (the swept, clear corridor); the body orientation catches up while slowed.
            transform.position += toTarget.normalized * step;
        }

        #endregion

        #region Private Methods

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            _playerPosition = moved.Position;
            _hasPlayer = true;
        }

        private void SampleNodes()
        {
            float turnRadius = _halfLength + _clearance;
            for (int i = 0; i < _samples; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Mathf.Sqrt(Range(0f, 1f)) * _roamRadius;
                Vector3 point = _home + new Vector3(Mathf.Cos(angle) * radius, Range(_heightBand.x, _heightBand.y), Mathf.Sin(angle) * radius);
                if (!Physics.CheckSphere(point + Vector3.up * _bodyCentre, turnRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    _nodes.Add(point);
                }
            }
        }

        private int Nearest(Vector3 point)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _nodes.Count; i++)
            {
                float distance = (_nodes[i] - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>The body (half width + margin) sweeps the leg between two clear nodes without touching anything (cached).</summary>
        private bool LegClear(int from, int to)
        {
            long key = from < to ? ((long)from << 32) | (uint)to : ((long)to << 32) | (uint)from;
            if (_legs.TryGetValue(key, out bool clear))
            {
                return clear;
            }

            Vector3 a = _nodes[from] + Vector3.up * _bodyCentre;
            Vector3 leg = _nodes[to] - _nodes[from];
            clear = !Physics.SphereCast(a, _halfWidth + _clearance, leg.normalized, out _, leg.magnitude, ~0, QueryTriggerInteraction.Ignore);
            _legs[key] = clear;
            return clear;
        }

        private void Steer(Vector3 toTarget, float dt)
        {
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector3 flat = Flat(toTarget);
            float pitch = flat.sqrMagnitude > 0.01f ? -Mathf.Atan2(toTarget.y, flat.magnitude) * Mathf.Rad2Deg : 0f;
            pitch = Mathf.Clamp(pitch, -_maxPitch, _maxPitch);
            float yaw = flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat).eulerAngles.y : transform.eulerAngles.y;
            float rate = _turnRate * (_attackRemaining > 0f ? 0.5f : 1f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(pitch, yaw, 0f), rate * dt);
        }

        private void TickManoeuvres(float dt)
        {
            _lookCooldown -= dt;
            // Track the swim phase every frame, so a crossing is measured from the previous frame.
            bool atSync = AtSyncPoint();
            if (_pending != null && atSync)
            {
                StartPending();
                return;
            }

            if (_attackRemaining > 0f)
            {
                _attackRemaining -= dt;
                float t = 1f - _attackRemaining / ManoeuvreLength;
                // Wind-up, lunge, glide: slow for the first quarter, a burst through the middle, back to cruise.
                _speedFactor = t < 0.25f ? 0.6f : t < 0.6f ? 2.6f : Mathf.Lerp(2.6f, 1f, (t - 0.6f) / 0.4f);
                if (_attackRemaining <= 0f)
                {
                    EndManoeuvre();
                }

                return;
            }

            if (_lookRemaining > 0f)
            {
                _lookRemaining -= dt;
                _speedFactor = Mathf.MoveTowards(_speedFactor, 0.35f, dt);
                if (_lookRemaining <= 0f)
                {
                    EndManoeuvre();
                }

                return;
            }

            _speedFactor = Mathf.MoveTowards(_speedFactor, 1f, dt * 0.5f);
            if (_manoeuvreRemaining > 0f)
            {
                _manoeuvreRemaining -= dt;
                if (_manoeuvreRemaining <= 0f)
                {
                    EndManoeuvre();
                }

                return;
            }

            _nextAttack -= dt;
            bool facingLeg = Vector3.Dot(transform.forward, (Target - transform.position).normalized) > 0.95f;
            if (_nextAttack <= 0f && facingLeg && (Target - transform.position).magnitude > 15f)
            {
                _nextAttack = Range(7f, 16f) / _aggression;
                Request(Attack);
                return;
            }

            if (_hasPlayer && _lookCooldown <= 0f && (_playerPosition - transform.position).sqrMagnitude < _playerLookRange * _playerLookRange)
            {
                StartLook();
            }
        }

        private void OnArrived()
        {
            if (!Busy && _random.NextDouble() < _curiosity * 0.5f)
            {
                StartLook();
            }

            ChooseNextLeg();
        }

        private bool Busy => _pending != null || _manoeuvreRemaining > 0f || _attackRemaining > 0f || _lookRemaining > 0f;

        private void StartLook()
        {
            _lookCooldown = LookLength + Range(8f, 16f);
            Request(Look);
        }

        /// <summary>Queues a manoeuvre clip; it starts at the next swim-cycle point that matches its first pose.</summary>
        private void Request(string clip)
        {
            if (!Busy)
            {
                _pending = clip;
            }
        }

        private void StartPending()
        {
            string clip = _pending;
            _pending = null;
            if (clip == Attack)
            {
                _attackRemaining = ManoeuvreLength;
            }
            else if (clip == Look)
            {
                _lookRemaining = LookLength;
            }
            else
            {
                _manoeuvreRemaining = ManoeuvreLength;
            }

            CrossFade(clip, SyncFade);
        }

        private void EndManoeuvre()
        {
            _manoeuvreRemaining = 0f;
            _attackRemaining = 0f;
            _lookRemaining = 0f;
            // The clip ended on the transit's first pose: resume transit from its first frame.
            CrossFade(Transit, SyncFade);
        }

        private void CrossFade(string state, float fade)
        {
            Manoeuvre = state;
            _transitPhase = 0f;
            if (_animator != null && _animator.HasState(0, Animator.StringToHash(state)))
            {
                _animator.CrossFadeInFixedTime(state, fade, 0, 0f);
            }
        }

        /// <summary>True when the transit clip just passed a point of its cycle that matches its first pose.</summary>
        private bool AtSyncPoint()
        {
            if (_animator == null || _syncPhases.Count == 0)
            {
                return true;
            }

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(Transit) || _animator.IsInTransition(0))
            {
                return false;
            }

            float previous = _transitPhase;
            float phase = Mathf.Repeat(info.normalizedTime, 1f);
            _transitPhase = phase;
            foreach (float sync in _syncPhases)
            {
                bool crossed = phase >= previous ? sync > previous && sync <= phase : sync > previous || sync <= phase;
                if (crossed)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Phases of the transit cycle whose pose matches its first frame (within 10 cm on every bone; local minima),
        /// sampled once from the clip on this shark before the Animator takes over.
        /// </summary>
        private void FindSyncPhases()
        {
            AnimationClip transit = null;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && clip.name == Transit)
                    {
                        transit = clip;
                    }
                }
            }

            if (transit == null)
            {
                return;
            }

            Transform[] bones = _animator.GetComponentsInChildren<Transform>();
            int frames = Mathf.Max(2, Mathf.RoundToInt(transit.length * transit.frameRate));
            Vector3[] first = Pose(transit, 0f, bones);
            float[] distance = new float[frames + 1];
            for (int f = 0; f <= frames; f++)
            {
                Vector3[] pose = Pose(transit, transit.length * f / frames, bones);
                float max = 0f;
                for (int b = 0; b < bones.Length; b++)
                {
                    max = Mathf.Max(max, (pose[b] - first[b]).magnitude);
                }

                distance[f] = max;
            }

            _syncPhases.Add(0f);
            for (int f = 1; f < frames; f++)
            {
                if (distance[f] < SyncTolerance && distance[f] <= distance[f - 1] && distance[f] <= distance[f + 1])
                {
                    _syncPhases.Add((float)f / frames);
                }
            }
        }

        private Vector3[] Pose(AnimationClip clip, float time, Transform[] bones)
        {
            clip.SampleAnimation(_animator.gameObject, time);
            Vector3[] pose = new Vector3[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                pose[b] = _animator.transform.InverseTransformPoint(bones[b].position);
            }

            return pose;
        }

        /// <summary>Next clear leg from the current node, scored for variety, preferred depth and spacing.</summary>
        private void ChooseNextLeg()
        {
            Vector3 position = _nodes[_node];
            int best = -1;
            float bestScore = float.MinValue;
            int start = _random.Next(_nodes.Count);
            int tested = 0;
            for (int k = 0; k < _nodes.Count && tested < 40; k++)
            {
                int i = (start + k) % _nodes.Count;
                if (i == _node || i == _previousNode)
                {
                    continue;
                }

                Vector3 leg = _nodes[i] - position;
                float length = leg.magnitude;
                if (length < _legLength.x || length > _legLength.y)
                {
                    continue;
                }

                tested++;
                if (!AwayFromOthers(position, _nodes[i]) || !LegClear(_node, i))
                {
                    continue;
                }

                float turn = Mathf.Abs(Vector3.SignedAngle(Flat(transform.forward), Flat(leg), Vector3.up));
                float depth = Mathf.Abs(_nodes[i].y - (_home.y + _preferredHeight));
                float score = Range(0f, 2f) - Mathf.Abs(turn - 50f) * 0.015f - depth * 0.08f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            // Nothing new reachable: swim back to the previous node (that leg is known to be clear).
            _targetNode = best >= 0 ? best : _previousNode >= 0 ? _previousNode : _node;
            StartTurnFor(Target - transform.position);
        }

        private void StartTurnFor(Vector3 leg)
        {
            if (Busy || leg.sqrMagnitude < 0.01f)
            {
                return;
            }

            float yaw = Vector3.SignedAngle(Flat(transform.forward), Flat(leg), Vector3.up);
            float climb = Mathf.Atan2(leg.y, Flat(leg).magnitude) * Mathf.Rad2Deg;
            string clip = Mathf.Abs(yaw) > 30f ? (yaw > 0f ? "TurnRight" : "TurnLeft")
                : Mathf.Abs(climb) > 10f ? (climb > 0f ? "TurnUp" : "TurnDown")
                : null;
            if (clip != null)
            {
                Request(clip);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private bool AwayFromOthers(Vector3 from, Vector3 to)
        {
            foreach (SharkCruise other in Sharks)
            {
                if (other == this)
                {
                    continue;
                }

                if (DistanceToSegment(other.transform.position, from, to) < _sharkSpacing || DistanceToSegment(other.Target, from, to) < _sharkSpacing)
                {
                    return false;
                }
            }

            return !_hasPlayer || DistanceToSegment(_playerPosition, from, to) > _playerSpacing + _halfWidth;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(point, a + ab * t);
        }

        #endregion
    }
}
