using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// Run clock and counters for the HUD timer and the mission-complete screen (D-287): time (paused during training
    /// and pause), crab hits, falls, crabs stomped and jumps. The clock starts when play begins after the opening.
    /// </summary>
    public sealed class RunStats : MonoBehaviour
    {
        #region Public Methods

        public float Elapsed { get; private set; }

        public bool Running { get; private set; }

        public bool Finished { get; private set; }

        public int Hits { get; private set; }

        public int Falls { get; private set; }

        public int Stomps { get; private set; }

        public int Jumps { get; private set; }

        public int CrabsTotal { get; private set; }

        public int FliesCollected { get; private set; }

        public int FliesTotal { get; private set; }

        /// <summary>True when the finished run beat the stored best (set by <see cref="Finish"/>).</summary>
        public bool NewBest { get; private set; }

        /// <summary>Training holds the clock; the run starts when it ends.</summary>
        public void Hold(bool hold)
        {
            if (!Finished)
            {
                Running = !hold;
            }
        }

        public void SetCrabsTotal(int total) => CrabsTotal = total;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Subscribe<JumpStartedEvent>(OnJump);
            EventBus.Subscribe<CrabContactPlayerEvent>(OnHit);
            EventBus.Subscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnRespawned);
            EventBus.Subscribe<ZoneClearedEvent>(OnCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneStartedEvent>(OnZoneStarted);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Unsubscribe<JumpStartedEvent>(OnJump);
            EventBus.Unsubscribe<CrabContactPlayerEvent>(OnHit);
            EventBus.Unsubscribe<CrabDefeatedEvent>(OnCrabDefeated);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnRespawned);
            EventBus.Unsubscribe<ZoneClearedEvent>(OnCleared);
        }

        private void Update()
        {
            // Scaled time: pause (time scale 0) stops the clock; the game-speed assist slows it with the game.
            if (Running && !Finished)
            {
                Elapsed += Time.deltaTime;
            }
        }

        #endregion

        #region Private Methods

        private void OnZoneStarted(ZoneStartedEvent started)
        {
            FliesTotal = started.ZoneTotal;
            FliesCollected = 0;
            // The clock waits for the opening (RunOpening releases it, or training does when it ends).
            Running = false;
        }

        private void OnFlyCollected(FlyCollectedEvent collected)
        {
            FliesCollected = collected.NewCount;
            FliesTotal = collected.ZoneTotal;
        }

        private void OnJump(JumpStartedEvent jump) => Jumps++;

        private void OnHit(CrabContactPlayerEvent hit) => Hits++;

        private void OnCrabDefeated(CrabDefeatedEvent defeated) => Stomps++;

        private void OnRespawned(PlayerRespawnedEvent respawned) => Falls++;

        private void OnCleared(ZoneClearedEvent cleared)
        {
            if (cleared.IsLastZone)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (Finished)
            {
                return;
            }

            Finished = true;
            Running = false;
            float best = GameSettings.BestTime;
            NewBest = best <= 0f || Elapsed < best;
            if (NewBest)
            {
                GameSettings.BestTime = Elapsed;
            }
        }

        #endregion
    }
}
