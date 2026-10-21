using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>
    /// Code-driven feedback particles (owner request, D-287, after the reference build): spark bursts, soft glow flares
    /// and expanding bubble rings on collection, stomp, landing, hits, the key ceremony, door unlock and falls. Three
    /// authored particle systems are emitted into with per-event colour, count, speed and size.
    /// </summary>
    public sealed class FeedbackFX : MonoBehaviour
    {
        #region Fields

        private static readonly Color Cyan = new Color(0.33f, 0.94f, 1f);
        private static readonly Color Amber = new Color(1f, 0.69f, 0.38f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.44f);
        private static readonly Color Dust = new Color(0.62f, 0.72f, 0.8f);
        private static readonly Color Sky = new Color(0.62f, 0.83f, 1f);
        private static readonly Color Green = new Color(0.36f, 1f, 0.6f);

        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private ParticleSystem _glow;
        [SerializeField] private ParticleSystem _rings;
        [Tooltip("Above this horizontal speed (m/s) footsteps kick up a little dust.")]
        [SerializeField] private float _runDustSpeed = 3f;

        #endregion

        #region Public Methods

        /// <summary>Burst of <paramref name="count"/> sparks flying outward (plus <paramref name="up"/> upward bias).</summary>
        public void Sparks(Vector3 position, Color color, int count, float speed, float life, float size, float up = 0.6f)
        {
            if (_sparks == null)
            {
                return;
            }

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) * 0.6f + up;
                emit.position = position;
                emit.velocity = direction.normalized * speed * Random.Range(0.45f, 1f);
                emit.startColor = color;
                emit.startSize = size * Random.Range(0.6f, 1.2f);
                emit.startLifetime = life * Random.Range(0.7f, 1.1f);
                _sparks.Emit(emit, 1);
            }
        }

        /// <summary>Soft additive flare that grows and fades.</summary>
        public void Glow(Vector3 position, Color color, float size, float life)
        {
            if (_glow == null)
            {
                return;
            }

            _glow.Emit(new ParticleSystem.EmitParams { position = position, startColor = color, startSize = size, startLifetime = life, velocity = Vector3.zero, applyShapeToPosition = false }, 1);
        }

        /// <summary>Flat ring expanding to <paramref name="radius"/> on the ground plane.</summary>
        public void Ring(Vector3 position, Color color, float radius, float life)
        {
            if (_rings == null)
            {
                return;
            }

            _rings.Emit(new ParticleSystem.EmitParams { position = position + Vector3.up * 0.05f, startColor = color, startSize = radius * 2f, startLifetime = life, velocity = Vector3.zero, applyShapeToPosition = false }, 1);
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<FlyCollectedEvent>(OnFly);
            EventBus.Subscribe<StompLandedEvent>(OnStomp);
            EventBus.Subscribe<LandedEvent>(OnLanded);
            EventBus.Subscribe<FootstepEvent>(OnFootstep);
            EventBus.Subscribe<CrabContactPlayerEvent>(OnHit);
            EventBus.Subscribe<PlayerMovedEvent>(OnMoved);
            EventBus.Subscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Subscribe<KeyCollectedEvent>(OnKeyCollected);
            EventBus.Subscribe<DoorUnlockedEvent>(OnDoor);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnRespawned);
            EventBus.Subscribe<ZoneCompletedEvent>(OnZoneCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFly);
            EventBus.Unsubscribe<StompLandedEvent>(OnStomp);
            EventBus.Unsubscribe<LandedEvent>(OnLanded);
            EventBus.Unsubscribe<FootstepEvent>(OnFootstep);
            EventBus.Unsubscribe<CrabContactPlayerEvent>(OnHit);
            EventBus.Unsubscribe<PlayerMovedEvent>(OnMoved);
            EventBus.Unsubscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Unsubscribe<KeyCollectedEvent>(OnKeyCollected);
            EventBus.Unsubscribe<DoorUnlockedEvent>(OnDoor);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnRespawned);
            EventBus.Unsubscribe<ZoneCompletedEvent>(OnZoneCompleted);
        }

        #endregion

        #region Private Methods

        private Vector3 _player;

        private void OnMoved(PlayerMovedEvent moved) => _player = moved.Position;

        private void OnFly(FlyCollectedEvent fly)
        {
            Sparks(fly.Position, Cyan, 26, 3.2f, 0.55f, 0.2f);
            Glow(fly.Position, Cyan * 1.6f, 1.7f, 0.32f);
            Glow(fly.Position, Color.white, 0.7f, 0.18f);
            Ring(new Vector3(fly.Position.x, _player.y, fly.Position.z), Cyan, 0.9f, 0.35f);
        }

        private void OnStomp(StompLandedEvent stomp)
        {
            Ring(stomp.Position, Amber, 1.3f, 0.4f);
            Ring(stomp.Position, Gold, 0.7f, 0.25f);
            Glow(stomp.Position + Vector3.up * 0.25f, Amber * 1.5f, 2f, 0.3f);
            Sparks(stomp.Position, Amber, 22, 4f, 0.5f, 0.22f, 0.25f);
            Sparks(stomp.Position + Vector3.up * 0.1f, Cyan, 18, 4.5f, 0.55f, 0.15f, 1.4f);
        }

        private void OnLanded(LandedEvent landed)
        {
            int count = 6 + Mathf.Min(10, Mathf.RoundToInt(landed.FallDistance * 3f));
            Sparks(landed.Position, Dust, count, 1.4f + landed.FallDistance * 0.3f, 0.45f, 0.2f, 0.15f);
        }

        private void OnFootstep(FootstepEvent step)
        {
            if (step.Speed > _runDustSpeed)
            {
                Sparks(step.Position, Dust, 2, 0.6f, 0.35f, 0.14f, 0.4f);
            }
        }

        private void OnHit(CrabContactPlayerEvent hit)
        {
            Vector3 chest = _player + Vector3.up * 0.6f;
            Sparks(chest, Gold, 14, 3f, 0.4f, 0.18f);
            Sparks(chest, Sky, 16, 3.5f, 0.35f, 0.14f);
            Glow(chest, Sky * 1.6f, 1.4f, 0.3f);
        }

        private void OnKeySpawned(KeySpawnedEvent spawned)
        {
            Glow(spawned.KeyPosition, Amber * 2f, 4.5f, 0.9f);
            Sparks(spawned.KeyPosition, new Color(1f, 0.68f, 0.17f), 60, 5f, 1f, 0.3f);
            Ring(spawned.KeyPosition + Vector3.down * 0.4f, new Color(1f, 0.68f, 0.17f), 2.4f, 0.9f);
        }

        private void OnKeyCollected(KeyCollectedEvent collected)
        {
            Sparks(_player + Vector3.up * 1.2f, Gold, 30, 3f, 0.5f, 0.2f);
        }

        private void OnDoor(DoorUnlockedEvent unlocked)
        {
            Ring(_player, Green, 1.8f, 0.6f);
            Sparks(_player + Vector3.up * 1.5f, Green, 24, 3f, 0.6f, 0.18f);
            Sparks(_player + Vector3.up * 1.5f, Gold, 16, 2.5f, 0.6f, 0.18f);
        }

        private void OnRespawned(PlayerRespawnedEvent respawned)
        {
            Sparks(respawned.Position, Cyan, 30, 3f, 0.6f, 0.22f, 1.2f);
            Ring(respawned.Position, Cyan, 1.5f, 0.6f);
        }

        private void OnZoneCompleted(ZoneCompletedEvent completed)
        {
            Ring(_player, Cyan, 3f, 0.6f);
            Glow(_player + Vector3.up * 1f, Cyan * 1.5f, 3f, 0.5f);
        }

        #endregion
    }
}
