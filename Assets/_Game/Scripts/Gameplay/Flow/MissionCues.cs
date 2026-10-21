using System.Collections;
using System.Collections.Generic;
using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// Mission feedback script (owner request, D-287): turns gameplay events into Sprat / Professor barks, objective
    /// banners, screen flashes and camera shakes, the way the reference build does. Owns no gameplay state; every
    /// one-time line fires once per run.
    /// </summary>
    public sealed class MissionCues : MonoBehaviour
    {
        #region Fields

        private const float LockedRepeat = 6f;

        [SerializeField] private string _areaName = "RESEARCH WING B-17";

        private static readonly string[] FallLines =
        {
            "Splash! Back up we go.",
            "Wet socks. Again.",
            "Note to self: avoid the water.",
        };

        private readonly HashSet<string> _said = new HashSet<string>();
        private float _lockedCooldown;
        private int _fallLine;

        #endregion

        #region Public Methods

        /// <summary>Opening line once play begins (after the intro and any training).</summary>
        public void RunStarted(bool afterTraining)
        {
            if (afterTraining)
            {
                EventBus.Publish(new BannerEvent("TRAINING COMPLETE", "THE CLOCK STARTS NOW"));
            }

            StartCoroutine(Delayed(afterTraining ? 1.2f : 0.6f, () => Bark(Speaker.Sprat, "Follow the cyan trail! The key appears when every fly is caught.", 4.5f)));
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<FlyCollectedEvent>(OnFly);
            EventBus.Subscribe<CrabProximityEvent>(OnCrabNear);
            EventBus.Subscribe<CrabContactPlayerEvent>(OnHit);
            EventBus.Subscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Subscribe<StompLandedEvent>(OnStompLanded);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnRespawned);
            EventBus.Subscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Subscribe<KeyCollectedEvent>(OnKeyCollected);
            EventBus.Subscribe<DoorLockedEvent>(OnDoorLocked);
            EventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
            EventBus.Subscribe<ZoneClearedEvent>(OnCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFly);
            EventBus.Unsubscribe<CrabProximityEvent>(OnCrabNear);
            EventBus.Unsubscribe<CrabContactPlayerEvent>(OnHit);
            EventBus.Unsubscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Unsubscribe<StompLandedEvent>(OnStompLanded);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnRespawned);
            EventBus.Unsubscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Unsubscribe<KeyCollectedEvent>(OnKeyCollected);
            EventBus.Unsubscribe<DoorLockedEvent>(OnDoorLocked);
            EventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
            EventBus.Unsubscribe<ZoneClearedEvent>(OnCleared);
        }

        private void Update()
        {
            _lockedCooldown -= Time.deltaTime;
        }

        #endregion

        #region Private Methods

        private void OnFly(FlyCollectedEvent fly)
        {
            int left = fly.ZoneTotal - fly.NewCount;
            if (fly.ZoneTotal >= 4 && fly.NewCount == fly.ZoneTotal / 2)
            {
                Once("half", Speaker.Sprat, $"Halfway! {left} more to go!");
            }
            else if (left == 1)
            {
                Once("last", Speaker.Sprat, "Just one more fly!");
            }
        }

        private void OnCrabNear(CrabProximityEvent near)
        {
            if (near.Near)
            {
                Once("crab", Speaker.Sprat, "Crabs! Jump, then STOMP on them!", 3.4f);
            }
        }

        private void OnHit(CrabContactPlayerEvent hit)
        {
            EventBus.Publish(new ScreenFlashEvent(FlashKind.White, 0.3f, 0.35f));
            EventBus.Publish(new CameraShakeEvent(0.25f));
            Once("hit", Speaker.Professor, "Ow! Pinchy. Jump over and STOMP it.");
        }

        private void OnCrabDefeated(CrabDefeatedEvent defeated)
        {
            Once("stomped", Speaker.Sprat, "Ha! Flattened!", 2.4f);
        }

        private void OnStompLanded(StompLandedEvent landed) => EventBus.Publish(new CameraShakeEvent(0.28f));

        private void OnRespawned(PlayerRespawnedEvent respawned)
        {
            EventBus.Publish(new ScreenFlashEvent(FlashKind.Blue, 0.85f, 0.6f));
            Bark(Speaker.Professor, FallLines[_fallLine++ % FallLines.Length]);
        }

        private void OnKeySpawned(KeySpawnedEvent spawned)
        {
            EventBus.Publish(new ScreenFlashEvent(FlashKind.White, 0.55f, 0.9f));
            EventBus.Publish(new BannerEvent("ACCESS KEY MATERIALISED", _areaName));
            StartCoroutine(Delayed(0.9f, () => Bark(Speaker.Sprat, "The key! Over there — grab it!")));
        }

        private void OnKeyCollected(KeyCollectedEvent collected)
        {
            EventBus.Publish(new BannerEvent("KEY ACQUIRED", "BRING IT TO THE DOOR"));
        }

        private void OnDoorLocked(DoorLockedEvent locked)
        {
            if (_lockedCooldown > 0f)
            {
                return;
            }

            _lockedCooldown = LockedRepeat;
            EventBus.Publish(new CameraShakeEvent(0.12f));
            Bark(Speaker.Professor, "Locked. We need the access key first.");
        }

        private void OnDoorUnlocked(DoorUnlockedEvent unlocked)
        {
            EventBus.Publish(new BannerEvent("DOOR UNLOCKED", $"{_areaName} · EXIT"));
        }

        private void OnCleared(ZoneClearedEvent cleared)
        {
            if (cleared.IsLastZone)
            {
                EventBus.Publish(new BannerEvent("MISSION COMPLETE", $"{_areaName} SECURED"));
            }
        }

        private void Once(string id, Speaker speaker, string text, float duration = 3.2f)
        {
            if (_said.Add(id))
            {
                Bark(speaker, text, duration);
            }
        }

        private static void Bark(Speaker speaker, string text, float duration = 3.2f) => EventBus.Publish(new BarkEvent(speaker, text, duration));

        private static IEnumerator Delayed(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        #endregion
    }
}
