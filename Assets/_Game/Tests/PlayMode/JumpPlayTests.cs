using System.Collections;
using NUnit.Framework;
using ProfessorSprat.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B Jump PlayMode criteria in SCN_Jump_Test (ledge top y 0.5 at x −20…−10).</summary>
    public sealed class JumpPlayTests : PlayTestBase
    {
        #region Public Methods

        [UnityTest]
        public IEnumerator ACJMP03_CoyoteJumpAfterLedge()
        {
            yield return LoadScene("SCN_Jump_Test");
            yield return Place(new Vector3(-11f, 0.55f, 0f), 90f);
            yield return WaitSeconds(0.2f);
            Professor.SetMoveInput(Vector3.right * 0.3f);
            float guard = 0f;
            while (Professor.IsGrounded && guard < 3f)
            {
                yield return WaitStep();
                guard += Time.fixedDeltaTime;
            }

            float takeoff = Professor.transform.position.y;
            yield return WaitSeconds(0.10f);
            Jump.PressJump();
            float peak = takeoff;
            for (int i = 0; i < 80; i++)
            {
                yield return WaitStep();
                peak = Mathf.Max(peak, Professor.transform.position.y);
            }

            Assert.That(peak - takeoff, Is.GreaterThanOrEqualTo(1.8f), "coyote jump executes within 0.12 s");
            Professor.SetMoveInput(Vector3.zero);
        }

        [UnityTest]
        public IEnumerator ACJMP04_BufferedJumpOnLanding()
        {
            yield return LoadScene("SCN_Jump_Test");
            using (EventRecorder<LandedEvent> landed = new EventRecorder<LandedEvent>())
            using (EventRecorder<JumpStartedEvent> jumped = new EventRecorder<JumpStartedEvent>())
            {
                yield return Place(new Vector3(0f, 3f, 0f), 0f);
                bool pressed = false;
                for (int i = 0; i < 120 && jumped.Count == 0; i++)
                {
                    float y = Professor.transform.position.y;
                    float speed = Mathf.Abs(Professor.Velocity.y);
                    if (!pressed && !Professor.IsGrounded && y <= speed * 0.08f + 0.5f * 17.66f * 0.0064f)
                    {
                        Jump.PressJump();
                        pressed = true;
                    }

                    yield return WaitStep();
                }

                Assert.IsTrue(pressed, "press issued about 0.08 s before landing");
                Assert.AreEqual(1, jumped.Count, "buffered jump fired");
                Assert.That(jumped.Steps[0] - landed.Steps[landed.Count - 1], Is.InRange(0, 1), "within 1 frame of landing");
            }
        }

        [UnityTest]
        public IEnumerator ACJMP05_NoDoubleJump()
        {
            yield return LoadScene("SCN_Jump_Test");
            using (EventRecorder<JumpStartedEvent> jumped = new EventRecorder<JumpStartedEvent>())
            {
                float ground = Professor.transform.position.y;
                Jump.PressJump();
                yield return WaitSeconds(0.5f);
                Jump.PressJump();
                float peak = ground;
                for (int i = 0; i < 60; i++)
                {
                    yield return WaitStep();
                    peak = Mathf.Max(peak, Professor.transform.position.y);
                }

                Assert.AreEqual(1, jumped.Count, "airborne press ignored");
                Assert.That(peak - ground, Is.LessThanOrEqualTo(2.3f));
            }
        }

        #endregion
    }
}
