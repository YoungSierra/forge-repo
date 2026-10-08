using System.Collections.Generic;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Sprat
{
    /// <summary>
    /// Plain reaction state of Sprat (TDD §B SpratCompanion rule 4): timers and near crabs, resolved by priority
    /// Spin &gt; Freeze &gt; Hazard &gt; Excited &gt; Idle. Owned by <see cref="SpratCompanion"/>.
    /// </summary>
    public sealed class SpratReactions
    {
        #region Fields

        private readonly Dictionary<string, Vector3> _nearCrabs = new Dictionary<string, Vector3>();
        private float _spinRemaining;
        private float _excitedRemaining;

        #endregion

        #region Public Methods

        public bool Frozen { get; set; }

        public void Spin(float duration) => _spinRemaining = duration;

        public void Excite(float duration) => _excitedRemaining = duration;

        public void SetCrabNear(string crabId, bool near, Vector3 position)
        {
            if (near)
            {
                _nearCrabs[crabId] = position;
            }
            else
            {
                _nearCrabs.Remove(crabId);
            }
        }

        public SpratState Tick(float deltaTime)
        {
            _spinRemaining = Mathf.Max(0f, _spinRemaining - deltaTime);
            _excitedRemaining = Mathf.Max(0f, _excitedRemaining - deltaTime);
            if (_spinRemaining > 0f)
            {
                return SpratState.Spin;
            }

            if (Frozen)
            {
                return SpratState.Freeze;
            }

            if (_nearCrabs.Count > 0)
            {
                return SpratState.Hazard;
            }

            return _excitedRemaining > 0f ? SpratState.Excited : SpratState.Idle;
        }

        public bool TryNearestCrab(Vector3 from, out Vector3 nearest)
        {
            nearest = Vector3.zero;
            float best = float.MaxValue;
            foreach (Vector3 position in _nearCrabs.Values)
            {
                float distance = (position - from).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = position;
                }
            }

            return best < float.MaxValue;
        }

        #endregion
    }
}
