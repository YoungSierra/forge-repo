using System;
using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;
using UnityEngine.Audio;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>
    /// One audio cue slot (TDD §10). The clip stays empty until delivered (no placeholder audio). Optional variations
    /// (delivered takes <c>&lt;ID&gt;_02</c>, <c>_03</c>…) are picked at random, never the same take twice in a row.
    /// </summary>
    [Serializable]
    public sealed class AudioCue
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioClip[] _variations = new AudioClip[0];

        private int _last = -1;

        public string Id => _id;

        public AudioClip Clip => _clip;

        public AudioClip Pick()
        {
            int count = 1 + (_variations != null ? _variations.Length : 0);
            if (count == 1)
            {
                return _clip;
            }

            int index = UnityEngine.Random.Range(0, count - 1);
            if (index >= _last && _last >= 0)
            {
                index++;
            }

            _last = index;
            AudioClip picked = index == 0 ? _clip : _variations[index - 1];
            return picked != null ? picked : _clip;
        }
    }

    /// <summary>
    /// TDD §B-S AudioService (Unity built-in audio, §10): plays the cue mapped to each gameplay event, drives the music
    /// intensity layer (crab near / one fly left) and switches mixer snapshots (KeyCeremony, Paused). Empty slots log once.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        #region Fields

        [SerializeField] private AudioSource _oneShots;
        [Tooltip("Source for footsteps only (its pitch is varied per step without touching other one-shots).")]
        [SerializeField] private AudioSource _footsteps;
        [Tooltip("Footstep volume at walking speed and at full run (m/s → 0..1 between 0.1 and the run speed).")]
        [SerializeField] private Vector2 _footstepVolume = new Vector2(0.55f, 0.9f);
        [Tooltip("Random pitch range per step.")]
        [SerializeField] private Vector2 _footstepPitch = new Vector2(0.93f, 1.07f);
        [SerializeField] private float _footstepRunSpeed = 5.5f;
        [SerializeField] private AudioSource _musicIntensity;
        [SerializeField] private List<AudioCue> _cues = new List<AudioCue>();
        [SerializeField] private AudioMixerSnapshot _gameplay;
        [SerializeField] private AudioMixerSnapshot _keyCeremony;
        [SerializeField] private AudioMixerSnapshot _paused;

        private readonly HashSet<string> _missingReported = new HashSet<string>();
        private readonly HashSet<string> _nearCrabs = new HashSet<string>();
        private bool _oneFlyLeft;

        #endregion

        #region Public Methods

        public float MusicIntensityTarget => _nearCrabs.Count > 0 || _oneFlyLeft ? 1f : 0f;

        public void Play(string cueId, Vector3 position)
        {
            AudioCue cue = _cues.Find(c => c.Id == cueId);
            if (cue == null || cue.Clip == null || _oneShots == null)
            {
                if (_missingReported.Add(cueId))
                {
                    Debug.LogWarning($"{nameof(AudioService)}: cue '{cueId}' has no delivered clip (Docs/V57/MISSING_ASSETS.md).", this);
                }

                return;
            }

            _oneShots.transform.position = position;
            _oneShots.PlayOneShot(cue.Pick());
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<FootstepEvent>(OnFootstep);
            EventBus.Subscribe<JumpStartedEvent>(OnJumpStarted);
            EventBus.Subscribe<LandedEvent>(OnLanded);
            EventBus.Subscribe<StompLandedEvent>(OnStompLanded);
            EventBus.Subscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Subscribe<CrabContactPlayerEvent>(OnCrabContact);
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Subscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
            EventBus.Subscribe<CrabProximityEvent>(OnCrabProximity);
            EventBus.Subscribe<PauseChangedEvent>(OnPauseChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FootstepEvent>(OnFootstep);
            EventBus.Unsubscribe<JumpStartedEvent>(OnJumpStarted);
            EventBus.Unsubscribe<LandedEvent>(OnLanded);
            EventBus.Unsubscribe<StompLandedEvent>(OnStompLanded);
            EventBus.Unsubscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Unsubscribe<CrabContactPlayerEvent>(OnCrabContact);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Unsubscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
            EventBus.Unsubscribe<CrabProximityEvent>(OnCrabProximity);
            EventBus.Unsubscribe<PauseChangedEvent>(OnPauseChanged);
        }

        private void Update()
        {
            // Player SFX volume (D-287); one-shot volume scales multiply the source volume.
            float sfx = GameSettings.SfxVolume;
            if (_oneShots != null)
            {
                _oneShots.volume = sfx;
            }
            if (_footsteps != null)
            {
                _footsteps.volume = sfx;
            }

            if (_musicIntensity != null)
            {
                _musicIntensity.volume = Mathf.MoveTowards(_musicIntensity.volume, MusicIntensityTarget, Time.unscaledDeltaTime / 0.5f);
            }
        }

        #endregion

        #region Private Methods

        private void OnFootstep(FootstepEvent step)
        {
            AudioCue cue = _cues.Find(c => c.Id == "SFX_Footstep_Steel");
            if (cue == null || cue.Clip == null || _footsteps == null)
            {
                Play("SFX_Footstep_Steel", step.Position);
                return;
            }

            float run01 = Mathf.InverseLerp(0.1f, _footstepRunSpeed, step.Speed);
            _footsteps.transform.position = step.Position;
            _footsteps.pitch = UnityEngine.Random.Range(_footstepPitch.x, _footstepPitch.y);
            _footsteps.PlayOneShot(cue.Pick(), Mathf.Lerp(_footstepVolume.x, _footstepVolume.y, run01));
        }

        private void OnJumpStarted(JumpStartedEvent started) => Play("SFX_Jump", started.Position);

        private void OnLanded(LandedEvent landed) => Play("SFX_Land", landed.Position);

        private void OnStompLanded(StompLandedEvent landed) => Play("SFX_StompImpact", landed.Position);

        private void OnCrabDefeated(CrabDefeatedEvent defeated) => Play("SFX_CrabDefeat", defeated.Position);

        private void OnCrabContact(CrabContactPlayerEvent contact) => Play("SFX_CrabContact", transform.position);

        private void OnDoorUnlocked(DoorUnlockedEvent unlocked) => Play("MUS_DoorUnlockCadence", transform.position);

        private void OnFlyCollected(FlyCollectedEvent collected)
        {
            _oneFlyLeft = collected.ZoneTotal - collected.NewCount == 1;
            Play(_oneFlyLeft ? "SFX_FlyPopLast" : "SFX_FlyPop", transform.position);
        }

        private void OnKeySpawned(KeySpawnedEvent spawned)
        {
            _oneFlyLeft = false;
            Play("MUS_KeyMotif", spawned.KeyPosition);
            if (_keyCeremony != null)
            {
                _keyCeremony.TransitionTo(0.2f);
            }
            Invoke(nameof(RestoreGameplaySnapshot), 1.2f);
        }

        private void RestoreGameplaySnapshot()
        {
            if (_gameplay != null)
            {
                _gameplay.TransitionTo(0.3f);
            }
        }

        private void OnCrabProximity(CrabProximityEvent proximity)
        {
            if (proximity.Near)
            {
                _nearCrabs.Add(proximity.CrabId);
            }
            else
            {
                _nearCrabs.Remove(proximity.CrabId);
            }
        }

        private void OnPauseChanged(PauseChangedEvent changed)
        {
            AudioMixerSnapshot snapshot = changed.Paused ? _paused : _gameplay;
            if (snapshot != null)
            {
                snapshot.TransitionTo(0.1f);
            }
        }

        #endregion
    }
}
