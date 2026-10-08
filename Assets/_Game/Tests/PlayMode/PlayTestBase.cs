using System.Collections;
using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>Records every payload of one event type published on the EventBus, with the physics step it arrived in.</summary>
    public sealed class EventRecorder<T> : System.IDisposable where T : struct
    {
        #region Public Methods

        public EventRecorder()
        {
            EventBus.Subscribe<T>(OnEvent);
        }

        public List<T> Events { get; } = new List<T>();

        public List<int> Steps { get; } = new List<int>();

        public int Count => Events.Count;

        public void Dispose()
        {
            EventBus.Unsubscribe<T>(OnEvent);
        }

        #endregion

        #region Private Methods

        private void OnEvent(T payload)
        {
            Events.Add(payload);
            Steps.Add(PlayTestBase.FixedStep);
        }

        #endregion
    }

    /// <summary>
    /// Shared PlayMode helpers: loads a §13.2 test scene (synthetic geometry) and runs real FixedUpdate physics — no
    /// Physics.Simulate, no time-scale changes. Mechanics are driven through their public API (component tests); the
    /// input-driven acceptance of the whole loop is the gold path.
    /// </summary>
    public abstract class PlayTestBase
    {
        #region Fields

        private static int _fixedStep;

        #endregion

        #region Public Methods

        public static int FixedStep => _fixedStep;

        protected ProfessorLocomotion Professor { get; private set; }

        protected ProfessorJump Jump { get; private set; }

        protected ProfessorStomp Stomp { get; private set; }

        #endregion

        #region Protected Methods

        protected IEnumerator LoadScene(string sceneName)
        {
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Professor = Object.FindAnyObjectByType<ProfessorLocomotion>();
            Jump = Professor.GetComponent<ProfessorJump>();
            Stomp = Professor.GetComponent<ProfessorStomp>();
            yield return WaitSeconds(0.3f);
        }

        /// <summary>Waits real physics steps for at least <paramref name="seconds"/> of simulated time.</summary>
        protected static IEnumerator WaitSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds - 0.0001f)
            {
                yield return new WaitForFixedUpdate();
                _fixedStep++;
                elapsed += Time.fixedDeltaTime;
            }
        }

        protected static IEnumerator WaitStep()
        {
            yield return new WaitForFixedUpdate();
            _fixedStep++;
        }

        protected IEnumerator Place(Vector3 position, float yaw)
        {
            Professor.Teleport(position, Quaternion.Euler(0f, yaw, 0f));
            yield return WaitStep();
        }

        protected static float Horizontal(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.magnitude;
        }

        #endregion
    }
}
