using NUnit.Framework;
using ProfessorSprat.Gameplay.Config;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using ProfessorSprat.Gameplay.Input;
using ProfessorSprat.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace ProfessorSprat.Tests.EditMode
{
    /// <summary>EditMode acceptance criteria on the plain runtime models and derived numbers (TDD §14.3 invariants).</summary>
    public sealed class ModelTests
    {
        #region Public Methods

        [Test]
        public void LookYawTurnsTheMoveBasisImmediately()
        {
            MoveBasis basis = new MoveBasis(90f, 0.2f, 0.8f);
            Vector3 forward = basis.ToWorld(Vector2.up, 0f);
            Vector3 orbited = basis.ToWorld(Vector2.up, 90f);
            Assert.That(Vector3.Distance(forward, Vector3.right), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(orbited, Vector3.back), Is.LessThan(0.001f));
        }

        [Test]
        public void ACJMP02_PeakHeightFromConfig()
        {
            JumpConfig config = AssetDatabase.LoadAssetAtPath<JumpConfig>("Assets/_Game/Data/Config/JumpConfig.asset");
            float peak = config.JumpImpulse * config.JumpImpulse / (2f * JumpConfig.Gravity);
            Assert.That(peak, Is.InRange(2.17f, 2.27f));
        }

        [Test]
        public void ACSTP02_StompDescentTime()
        {
            JumpConfig jump = AssetDatabase.LoadAssetAtPath<JumpConfig>("Assets/_Game/Data/Config/JumpConfig.asset");
            StompConfig stomp = AssetDatabase.LoadAssetAtPath<StompConfig>("Assets/_Game/Data/Config/StompConfig.asset");
            float gravity = JumpConfig.Gravity * jump.FallingGravityScale * stomp.StompGravityMultiplier;
            float descent = Mathf.Sqrt(2f * 1.5f / gravity);
            Assert.That(descent, Is.InRange(0.20f, 0.28f));
        }

        [Test]
        public void ACFLY01_ZoneTotalIsRegisteredFlies()
        {
            ZoneFlyModel model = new ZoneFlyModel();
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(model.Register($"Fly_{i}"));
            }

            model.Begin("Z1");
            Assert.AreEqual(6, model.ZoneTotal);
            Assert.AreEqual(0, model.CollectedCount);
            Assert.IsFalse(model.Register("Fly_late"), "registration closes when the zone starts");
        }

        [Test]
        public void FlyModel_NeverDecrementsAndCompletesOnce()
        {
            ZoneFlyModel model = new ZoneFlyModel();
            model.Register("A");
            model.Register("B");
            model.Begin("Z1");
            Assert.IsTrue(model.Collect("A"));
            Assert.IsFalse(model.Collect("A"));
            Assert.IsTrue(model.Collect("B"));
            Assert.IsTrue(model.IsComplete);
            Assert.AreEqual(2, model.CollectedCount);
        }

        [Test]
        public void CrabPatrol_RoundTripMatchesInvariant()
        {
            CrabPatrolModel patrol = new CrabPatrolModel(4f, 1.2f, 0.4f);
            float elapsed = 0f;
            bool reachedEnd = false;
            const float step = 0.02f;
            while (elapsed < 20f)
            {
                patrol.Step(step);
                elapsed += step;
                reachedEnd |= patrol.Offset >= 4f;
                if (reachedEnd && patrol.Offset <= 0f && patrol.PauseRemaining <= 0f)
                {
                    break;
                }
            }

            Assert.That(elapsed, Is.InRange(7.37f, 7.57f), "2 × 4.0 / 1.2 + 2 × 0.4 = 7.47 s (INV-10)");
        }

        [Test]
        public void Locomotion_StunDoesNotStack()
        {
            LocomotionModel model = new LocomotionModel();
            Assert.IsTrue(model.BeginStun(0.5f, Vector3.forward, 2.5f, 0.5f));
            Assert.IsFalse(model.BeginStun(0.5f, Vector3.back, 2.5f, 0.5f));
        }

        #endregion
    }
}
