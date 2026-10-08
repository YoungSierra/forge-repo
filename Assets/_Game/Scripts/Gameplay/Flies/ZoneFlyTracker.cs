using System.Collections.Generic;
using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flies
{
    /// <summary>
    /// TDD §B FlyCollection for the active zone: checks the Professor's capsule centre against the zone's flies on every
    /// <see cref="PlayerMovedEvent"/>, publishes <see cref="FlyCollectedEvent"/> and, once, <see cref="ZoneCompletedEvent"/>.
    /// </summary>
    public sealed class ZoneFlyTracker : MonoBehaviour
    {
        #region Fields

        private readonly List<FlyController> _flies = new List<FlyController>();
        private ZoneFlyModel _model = new ZoneFlyModel();

        #endregion

        #region Public Methods

        public int CollectedCount => _model.CollectedCount;

        public int ZoneTotal => _model.ZoneTotal;

        public bool IsZoneComplete => _model.IsComplete;

        public string ZoneId => _model.ZoneId;

        /// <summary>Registers a fly of the next zone; rejected (error) once the zone started.</summary>
        public bool RegisterFly(FlyController fly)
        {
            if (fly == null || !_model.Register(fly.FlyId))
            {
                Debug.LogError($"{nameof(ZoneFlyTracker)}: fly '{fly?.FlyId}' registered after zone start or duplicated; ignored.", this);
                return false;
            }

            _flies.Add(fly);
            return true;
        }

        /// <summary>New zone run: clears previous flies, registers these, closes registration.</summary>
        public void BeginZone(string zoneId, IReadOnlyList<FlyController> flies)
        {
            _model = new ZoneFlyModel();
            _flies.Clear();
            foreach (FlyController fly in flies)
            {
                RegisterFly(fly);
            }

            _model.Begin(zoneId);
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        #endregion

        #region Private Methods

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (!_model.Started || _model.IsComplete)
            {
                return;
            }

            for (int i = 0; i < _flies.Count; i++)
            {
                FlyController fly = _flies[i];
                if (fly == null || !fly.IsWithinReach(moved.Center) || !_model.Collect(fly.FlyId))
                {
                    continue;
                }

                fly.Collect(_model.IsComplete);
                EventBus.Publish(new FlyCollectedEvent(fly.FlyId, _model.ZoneId, _model.CollectedCount, _model.ZoneTotal));
                if (_model.IsComplete)
                {
                    EventBus.Publish(new ZoneCompletedEvent(_model.ZoneId));
                    return;
                }
            }
        }

        #endregion
    }
}
