using System.Collections;
using NUnit.Framework;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B CrabEncounter PlayMode criteria in SCN_CrabEncounter_Test (crab at (0, 0, 8) facing +Z).</summary>
    public sealed class CrabEncounterPlayTests : PlayTestBase
    {
        #region Public Methods

        [UnityTest]
        public IEnumerator ACCRB02_ContactKnocksBack()
        {
            yield return LoadScene("SCN_CrabEncounter_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            ZoneFlyTracker tracker = Object.FindAnyObjectByType<ZoneFlyTracker>();
            using (EventRecorder<CrabContactPlayerEvent> contacts = new EventRecorder<CrabContactPlayerEvent>())
            {
                Vector3 contactPoint = crab.transform.position + new Vector3(0f, 0.05f, -0.7f);
                yield return Place(contactPoint, 0f);
                yield return WaitSeconds(0.5f);
                Assert.AreEqual(1, contacts.Count);
                Assert.That(Horizontal(Professor.transform.position, contactPoint), Is.InRange(2.4f, 2.6f));
                Assert.AreEqual(0, tracker.CollectedCount, "fly count unchanged");
            }
        }

        [UnityTest]
        public IEnumerator ACCRB03_StompDefeatsWithoutContact()
        {
            yield return LoadScene("SCN_CrabEncounter_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            using (EventRecorder<CrabContactPlayerEvent> contacts = new EventRecorder<CrabContactPlayerEvent>())
            using (EventRecorder<StompLandedEvent> landed = new EventRecorder<StompLandedEvent>())
            using (EventRecorder<CrabDefeatedEvent> defeated = new EventRecorder<CrabDefeatedEvent>())
            {
                yield return Place(crab.transform.position + Vector3.up * 1.4f, 0f);
                yield return WaitStep();
                Stomp.PressStomp();
                yield return WaitSeconds(0.6f);
                Assert.AreEqual(1, defeated.Count);
                Assert.That(defeated.Steps[0] - landed.Steps[0], Is.InRange(0, 1));
                Assert.IsTrue(crab.IsDefeated);
                foreach (Collider body in crab.GetComponentsInChildren<Collider>(true))
                {
                    Assert.IsFalse(body.enabled, "colliders off on defeat");
                }

                Assert.AreEqual(0, contacts.Count, "stomp wins over body contact");
            }
        }

        [UnityTest]
        public IEnumerator ACCRB04_NoRepeatedContactWithoutLeaving()
        {
            yield return LoadScene("SCN_CrabEncounter_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            using (EventRecorder<CrabContactPlayerEvent> contacts = new EventRecorder<CrabContactPlayerEvent>())
            {
                Vector3 inside = crab.transform.position + new Vector3(0f, 0.05f, -0.6f);
                yield return Place(inside, 0f);
                yield return WaitSeconds(0.6f);
                Assert.AreEqual(1, contacts.Count);
                yield return Place(crab.transform.position + new Vector3(0f, 0.05f, -0.6f), 0f);
                yield return WaitStep();
                Assert.AreEqual(2, contacts.Count, "re-entry after the stun and after leaving the sphere");
            }
        }

        [UnityTest]
        public IEnumerator ACCRB05_RoundTripTime()
        {
            yield return LoadScene("SCN_CrabEncounter_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            yield return Place(new Vector3(-20f, 0.55f, 0f), 0f);
            crab.Init(4f);
            float elapsed = 0f;
            bool reachedEnd = false;
            while (elapsed < 12f)
            {
                yield return WaitStep();
                elapsed += Time.fixedDeltaTime;
                reachedEnd |= crab.Patrol.Offset >= 4f;
                if (reachedEnd && crab.Patrol.Offset <= 0f && crab.Patrol.PauseRemaining <= 0f)
                {
                    break;
                }
            }

            Assert.That(elapsed, Is.InRange(7.37f, 7.57f), "range 4.0 one-way: 7.47 s ± 0.1 s");
        }

        [UnityTest]
        public IEnumerator ACCRB06_ProximityWithHysteresis()
        {
            yield return LoadScene("SCN_CrabEncounter_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            using (EventRecorder<CrabProximityEvent> proximity = new EventRecorder<CrabProximityEvent>())
            {
                yield return Place(crab.transform.position + new Vector3(0f, 0.05f, -2.9f), 0f);
                yield return WaitSeconds(0.1f);
                Assert.AreEqual(1, proximity.Count);
                Assert.IsTrue(proximity.Events[0].Near);
                yield return Place(crab.transform.position + new Vector3(0f, 0.05f, -3.6f), 0f);
                yield return WaitSeconds(0.1f);
                Assert.AreEqual(2, proximity.Count);
                Assert.IsFalse(proximity.Events[1].Near);
            }
        }

        #endregion
    }
}
