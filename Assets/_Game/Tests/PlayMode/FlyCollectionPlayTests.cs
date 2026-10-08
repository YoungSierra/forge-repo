using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using ProfessorSprat.Gameplay.Flow;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B FlyCollection PlayMode criteria in SCN_FlyCollection_Test (6 flies at z 20, y 0.6).</summary>
    public sealed class FlyCollectionPlayTests : PlayTestBase
    {
        #region Fields

        private ZoneFlyTracker _tracker;
        private List<FlyController> _flies;

        #endregion

        #region Public Methods

        [UnityTest]
        public IEnumerator ACFLY03_CollectWithinRadius()
        {
            yield return Begin();
            using (EventRecorder<FlyCollectedEvent> collected = new EventRecorder<FlyCollectedEvent>())
            {
                yield return StandNear(_flies[0], 0.55f);
                Assert.AreEqual(1, collected.Count, "collected within 1 frame at 0.55 m");
                Assert.AreEqual(1, _tracker.CollectedCount);
            }
        }

        [UnityTest]
        public IEnumerator ACFLY04_ZoneCompleteOnce()
        {
            yield return Begin();
            using (EventRecorder<ZoneCompletedEvent> complete = new EventRecorder<ZoneCompletedEvent>())
            {
                foreach (FlyController fly in _flies)
                {
                    yield return StandNear(fly, 0.3f);
                }

                yield return WaitSeconds(0.3f);
                Assert.AreEqual(1, complete.Count);
                Assert.AreEqual("Z1", complete.Events[0].ZoneId);
            }
        }

        [UnityTest]
        public IEnumerator ACFLY05_CountSurvivesContactAndRespawn()
        {
            yield return Begin();
            for (int i = 0; i < 3; i++)
            {
                yield return StandNear(_flies[i], 0.3f);
            }

            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            yield return Place(crab.transform.position + new Vector3(0f, 0.05f, -0.6f), 0f);
            yield return WaitSeconds(0.6f);
            Object.FindAnyObjectByType<RespawnService>().TriggerRespawn();
            yield return WaitSeconds(0.8f);
            Assert.AreEqual(3, _tracker.CollectedCount);
        }

        [UnityTest]
        public IEnumerator ACFLY06_LateRegistrationRejected()
        {
            yield return Begin(5);
            LogAssert.Expect(LogType.Error, new Regex("registered after zone start"));
            Assert.IsFalse(_tracker.RegisterFly(_flies[5]));
            Assert.AreEqual(5, _tracker.ZoneTotal);
        }

        #endregion

        #region Private Methods

        private IEnumerator Begin(int count = 6)
        {
            yield return LoadScene("SCN_FlyCollection_Test");
            _tracker = Object.FindAnyObjectByType<ZoneFlyTracker>();
            _flies = new List<FlyController>(Object.FindObjectsByType<FlyController>(FindObjectsSortMode.None));
            _flies.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            _tracker.BeginZone("Z1", _flies.GetRange(0, count));
        }

        /// <summary>Teleports the Professor so its capsule centre is <paramref name="distance"/> m from the fly.</summary>
        private IEnumerator StandNear(FlyController fly, float distance)
        {
            Vector3 center = fly.transform.position + new Vector3(0f, 0f, -distance);
            yield return Place(center - (Professor.Center - Professor.transform.position), 0f);
            yield return WaitStep();
        }

        #endregion
    }
}
