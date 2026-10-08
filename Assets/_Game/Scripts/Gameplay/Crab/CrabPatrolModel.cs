using UnityEngine;

namespace ProfessorSprat.Gameplay.Crab
{
    /// <summary>
    /// Plain patrol state: the crab moves between its origin (s = 0) and origin + axis × range (one-way range) at a fixed
    /// speed, pausing at each endpoint. Round trip = 2 × range / speed + 2 × pause. Range 0 = static crab.
    /// </summary>
    public sealed class CrabPatrolModel
    {
        #region Public Methods

        public CrabPatrolModel(float range, float speed, float pause)
        {
            Range = Mathf.Max(0f, range);
            Speed = speed;
            Pause = pause;
            Direction = 1;
            PauseRemaining = Range > 0f ? 0f : float.MaxValue;
        }

        public float Range { get; }

        public float Speed { get; }

        public float Pause { get; }

        /// <summary>Distance from the origin along the axis.</summary>
        public float Offset { get; private set; }

        /// <summary>+1 towards the endpoint, -1 back to the origin.</summary>
        public int Direction { get; private set; }

        public float PauseRemaining { get; private set; }

        public bool IsMoving => Range > 0f && PauseRemaining <= 0f;

        public void Step(float deltaTime)
        {
            if (Range <= 0f)
            {
                return;
            }

            if (PauseRemaining > 0f)
            {
                PauseRemaining -= deltaTime;
                return;
            }

            Offset += Direction * Speed * deltaTime;
            if (Offset >= Range || Offset <= 0f)
            {
                Offset = Mathf.Clamp(Offset, 0f, Range);
                Direction = -Direction;
                PauseRemaining = Pause;
            }
        }

        #endregion
    }
}
