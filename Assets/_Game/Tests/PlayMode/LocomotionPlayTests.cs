using System.Collections;
using NUnit.Framework;
using ProfessorSprat.Gameplay.Input;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B Locomotion PlayMode criteria in SCN_Locomotion_Test (real physics, CharacterController).</summary>
    public sealed class LocomotionPlayTests : PlayTestBase
    {
        #region Public Methods

        [UnityTest]
        public IEnumerator ACLOC02_ReachesTopSpeedIn035Seconds()
        {
            yield return LoadScene("SCN_Locomotion_Test");
            Professor.SetMoveInput(Vector3.forward);
            yield return WaitSeconds(0.35f);
            Assert.That(Professor.HorizontalVelocity.magnitude, Is.InRange(5.4f, 5.6f));
            Professor.SetMoveInput(Vector3.zero);
        }

        [UnityTest]
        public IEnumerator ACLOC03_KnockbackAndStun()
        {
            yield return LoadScene("SCN_Locomotion_Test");
            Vector3 start = Professor.transform.position;
            Professor.SetMoveInput(Vector3.back);
            Professor.ApplyKnockback(Vector3.forward);
            yield return WaitSeconds(0.25f);
            Assert.IsTrue(Professor.IsStunned, "stunned during the 0.5 s window");
            yield return WaitSeconds(0.25f);
            float displaced = Vector3.Dot(Professor.transform.position - start, Vector3.forward);
            Assert.That(displaced, Is.InRange(2.4f, 2.6f), "knockback 2.5 m ± 0.1 m in 0.5 s (input ignored)");
            Assert.That(Professor.Velocity.y, Is.LessThanOrEqualTo(0f), "knockback is horizontal only");
            yield return WaitSeconds(0.04f);
            Assert.IsFalse(Professor.IsStunned);
            Assert.That(Professor.HorizontalVelocity.magnitude, Is.GreaterThan(0.1f), "input resumes after the stun");
            Professor.SetMoveInput(Vector3.zero);
        }

        [UnityTest]
        public IEnumerator ACLOC04_SteepSlopeSlides()
        {
            yield return LoadScene("SCN_Locomotion_Test");
            yield return Place(new Vector3(15f, 3.0f, 0f), 90f);
            yield return WaitSeconds(0.2f);
            Professor.SetMoveInput(Vector3.right);
            float startX = Professor.transform.position.x;
            float startY = Professor.transform.position.y;
            yield return WaitSeconds(0.5f);
            Vector3 position = Professor.transform.position;
            Assert.That(position.x - startX, Is.LessThanOrEqualTo(0f), "no progress uphill on a 46° slope");
            float slope = new Vector2(startX - position.x, startY - position.y).magnitude / 0.5f;
            Assert.That(slope, Is.GreaterThanOrEqualTo(1.9f), "slides down at ≥ 1.9 m/s");
            Professor.SetMoveInput(Vector3.zero);
        }

        [UnityTest]
        public IEnumerator ACLOC05_CameraRelativeMove()
        {
            yield return LoadScene("SCN_Locomotion_Test");
            MoveBasis basis = new MoveBasis(90f, 0.2f, 0.8f);
            Professor.SetMoveInput(basis.ToWorld(Vector2.up));
            Vector3 start = Professor.transform.position;
            yield return WaitSeconds(0.3f);
            Assert.That(Professor.transform.position.x - start.x, Is.GreaterThan(0.5f), "stick up with yaw 90° moves along +X");
            basis.Update(0f, 1f, 1f);
            Assert.AreEqual(90f, basis.Yaw, 1e-3f, "basis kept while the stick is held");
            basis.Update(0f, 0.1f, 1.1f);
            Assert.AreEqual(0f, basis.Yaw, 1e-3f, "new basis once the stick drops below 0.2");
            Professor.SetMoveInput(Vector3.zero);
        }

        #endregion
    }
}
