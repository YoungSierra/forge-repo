using System.Collections;
using NUnit.Framework;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B Stomp PlayMode criteria in SCN_Stomp_Test (static crab at (0, 0, 8)).</summary>
    public sealed class StompPlayTests : PlayTestBase
    {
        #region Public Methods

        [UnityTest]
        public IEnumerator ACSTP03_GroundedStompIgnored()
        {
            yield return LoadScene("SCN_Stomp_Test");
            using (EventRecorder<StompLandedEvent> landed = new EventRecorder<StompLandedEvent>())
            {
                Vector3 velocity = Professor.Velocity;
                Stomp.PressStomp();
                yield return WaitSeconds(0.3f);
                Assert.IsFalse(Stomp.IsStomping);
                Assert.AreEqual(0, landed.Count);
                Assert.AreEqual(velocity.x, Professor.Velocity.x, 1e-3f);
            }
        }

        [UnityTest]
        public IEnumerator ACSTP04_DefeatsCrabWithinRadiusAndBounces()
        {
            yield return LoadScene("SCN_Stomp_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            yield return DropAndStomp(crab.transform.position + new Vector3(0f, 0f, -0.9f));
            using (EventRecorder<StompLandedEvent> landed = new EventRecorder<StompLandedEvent>())
            using (EventRecorder<CrabDefeatedEvent> defeated = new EventRecorder<CrabDefeatedEvent>())
            {
                float ground = 0.05f;
                float peak = 0f;
                for (int i = 0; i < 120; i++)
                {
                    yield return WaitStep();
                    if (landed.Count > 0)
                    {
                        peak = Mathf.Max(peak, Professor.transform.position.y - ground);
                    }
                }

                Assert.AreEqual(1, landed.Count);
                Assert.AreEqual(1, defeated.Count, "crab at 0.9 m defeated");
                Assert.That(defeated.Steps[0] - landed.Steps[0], Is.InRange(0, 1));
                Assert.That(peak, Is.InRange(0.7f, 0.9f), "bounce 0.8 m");
            }
        }

        [UnityTest]
        public IEnumerator ACSTP05_CrabOutsideRadiusNotDefeated()
        {
            yield return LoadScene("SCN_Stomp_Test");
            CrabController crab = Object.FindAnyObjectByType<CrabController>();
            using (EventRecorder<CrabDefeatedEvent> defeated = new EventRecorder<CrabDefeatedEvent>())
            using (EventRecorder<StompLandedEvent> landed = new EventRecorder<StompLandedEvent>())
            {
                yield return DropAndStomp(crab.transform.position + new Vector3(0f, 0f, -1.05f));
                while (landed.Count == 0)
                {
                    yield return WaitStep();
                }

                Assert.AreEqual(StompState.LandRecovery, Stomp.State);
                yield return WaitSeconds(0.3f);
                Assert.AreEqual(0, defeated.Count, "crab at 1.05 m survives");
                Assert.AreEqual(StompState.Idle, Stomp.State, "0.25 s recovery");
            }
        }

        [UnityTest]
        public IEnumerator ACSTP06_StraightDown()
        {
            yield return LoadScene("SCN_Stomp_Test");
            yield return Place(new Vector3(-5f, 2.5f, 0f), 0f);
            Professor.SetMoveInput(Vector3.forward);
            yield return WaitSeconds(0.1f);
            Stomp.PressStomp();
            Assert.IsTrue(Stomp.IsStomping);
            while (Stomp.IsStomping)
            {
                yield return WaitStep();
                if (Stomp.IsStomping)
                {
                    Assert.AreEqual(0f, Professor.HorizontalVelocity.magnitude, 1e-4f);
                }
            }

            Professor.SetMoveInput(Vector3.zero);
        }

        [UnityTest]
        public IEnumerator ACSTP07_NotEligibleRightAfterJump()
        {
            yield return LoadScene("SCN_Stomp_Test");
            yield return Place(new Vector3(-5f, 0.05f, 0f), 0f);
            yield return WaitSeconds(0.2f);
            Jump.PressJump();
            yield return WaitSeconds(0.10f);
            Stomp.PressStomp();
            Assert.IsFalse(Stomp.IsStomping, "0.10 s after the jump: not eligible");
            yield return WaitSeconds(0.10f);
            Stomp.PressStomp();
            Assert.IsTrue(Stomp.IsStomping, "0.20 s after the jump: eligible");
        }

        #endregion

        #region Private Methods

        private IEnumerator DropAndStomp(Vector3 landing)
        {
            yield return Place(landing + Vector3.up * 2.5f, 0f);
            yield return WaitStep();
            Stomp.PressStomp();
            Assert.IsTrue(Stomp.IsStomping, "airborne without a jump: eligible");
        }

        #endregion
    }
}
