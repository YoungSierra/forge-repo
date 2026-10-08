using System;
using System.Collections.Generic;
using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>One effect slot (TDD §13.1 VFX). The prefab stays empty until the VFX art is delivered.</summary>
    [Serializable]
    public sealed class EffectSlot
    {
        [SerializeField] private string _id;
        [SerializeField] private ParticleSystem _prefab;

        public string Id => _id;

        public ParticleSystem Prefab => _prefab;
    }

    /// <summary>
    /// TDD §B-S VFXService: plays the effect mapped to each gameplay event from serialized prefabs, one reusable instance
    /// per effect under <c>_Gameplay/Spawned</c>. Empty slots log once (no placeholder effects).
    /// </summary>
    public sealed class VFXService : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Transform _spawnRoot;
        [SerializeField] private List<EffectSlot> _effects = new List<EffectSlot>();

        private readonly Dictionary<string, ParticleSystem> _instances = new Dictionary<string, ParticleSystem>();
        private readonly HashSet<string> _missingReported = new HashSet<string>();

        #endregion

        #region Public Methods

        public void Spawn(string effectId, Vector3 position)
        {
            if (!_instances.TryGetValue(effectId, out ParticleSystem instance))
            {
                EffectSlot slot = _effects.Find(e => e.Id == effectId);
                if (slot == null || slot.Prefab == null || _spawnRoot == null)
                {
                    if (_missingReported.Add(effectId))
                    {
                        Debug.LogWarning($"{nameof(VFXService)}: effect '{effectId}' not delivered (Docs/V57/MISSING_ASSETS.md).", this);
                    }

                    return;
                }

                instance = Instantiate(slot.Prefab, _spawnRoot);
                _instances.Add(effectId, instance);
            }

            instance.transform.position = position;
            instance.Play(true);
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<LandedEvent>(OnLanded);
            EventBus.Subscribe<StompLandedEvent>(OnStompLanded);
            EventBus.Subscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Subscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Subscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnRespawned);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LandedEvent>(OnLanded);
            EventBus.Unsubscribe<StompLandedEvent>(OnStompLanded);
            EventBus.Unsubscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Unsubscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Unsubscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnRespawned);
        }

        #endregion

        #region Private Methods

        private void OnLanded(LandedEvent landed) => Spawn("VFX_LandSplash", landed.Position);

        private void OnStompLanded(StompLandedEvent landed) => Spawn("VFX_StompImpact", landed.Position);

        private void OnCrabDefeated(CrabDefeatedEvent defeated) => Spawn("VFX_CrabDefeat", defeated.Position);

        private void OnFlyCollected(FlyCollectedEvent collected) => Spawn("VFX_FlyPop", transform.position);

        private void OnZoneCompleted(ZoneCompletedEvent completed) => Spawn("VFX_ZoneComplete", transform.position);

        private void OnKeySpawned(KeySpawnedEvent spawned) => Spawn("VFX_KeyMaterialise", spawned.KeyPosition);

        private void OnDoorUnlocked(DoorUnlockedEvent unlocked) => Spawn("VFX_DoorUnlock", transform.position);

        private void OnRespawned(PlayerRespawnedEvent respawned) => Spawn("VFX_RespawnFade", respawned.Position);

        #endregion
    }
}
