using NUnit.Framework;
using ProfessorSprat.Gameplay.Config;
using UnityEditor;

namespace ProfessorSprat.Tests.EditMode
{
    /// <summary>EditMode acceptance criteria on the shipped config assets (AC-*-01).</summary>
    public sealed class ConfigTests
    {
        #region Public Methods

        [Test]
        public void ACLOC01_LocomotionConfigValues()
        {
            LocomotionConfig config = Load<LocomotionConfig>("LocomotionConfig");
            Assert.AreEqual(5.5f, config.RunSpeed, 1e-4f);
            Assert.AreEqual(18f, config.Acceleration, 1e-4f);
            Assert.AreEqual(22f, config.Deceleration, 1e-4f);
            Assert.AreEqual(0.5f, config.StunDuration, 1e-4f);
        }

        [Test]
        public void ACJMP01_JumpConfigValues()
        {
            JumpConfig config = Load<JumpConfig>("JumpConfig");
            Assert.AreEqual(6.6f, config.JumpImpulse, 1e-4f);
            Assert.AreEqual(1.8f, config.FallingGravityScale, 1e-4f);
        }

        [Test]
        public void ACSTP01_StompConfigValues()
        {
            StompConfig config = Load<StompConfig>("StompConfig");
            Assert.AreEqual(1.0f, config.ImpactRadius, 1e-4f);
            Assert.AreEqual(3.0f, config.StompGravityMultiplier, 1e-4f);
            Assert.AreEqual(0.8f, config.BounceHeight, 1e-4f);
        }

        [Test]
        public void ACFLY02_FlyTriggerRadius()
        {
            Assert.AreEqual(0.6f, Load<FlyConfig>("FlyConfig").TriggerRadius, 1e-4f);
        }

        [Test]
        public void ACKEY01_AccessKeyConfigValues()
        {
            AccessKeyConfig config = Load<AccessKeyConfig>("AccessKeyConfig");
            Assert.AreEqual(1.2f, config.CeremonyDuration, 1e-4f);
            Assert.AreEqual(1.5f, config.PickupRadius, 1e-4f);
            Assert.AreEqual(0.8f, config.DoorOpenDuration, 1e-4f);
        }

        [Test]
        public void ACCRB01_CrabConfigValues()
        {
            CrabConfig config = Load<CrabConfig>("CrabConfig");
            Assert.AreEqual(1.2f, config.PatrolSpeed, 1e-4f);
            Assert.AreEqual(0.5f, config.BodyRadius, 1e-4f);
            Assert.AreEqual(4.0f, config.MaxRange, 1e-4f);
            Assert.AreEqual(2.5f, Load<LocomotionConfig>("LocomotionConfig").KnockbackDistance, 1e-4f);
        }

        [Test]
        public void ACSPR01_SpratConfigValues()
        {
            SpratConfig config = Load<SpratConfig>("SpratConfig");
            Assert.AreEqual(0.18f, config.FollowSmoothTime, 1e-4f);
            Assert.AreEqual(1.5f, config.MaxLag, 1e-4f);
            Assert.AreEqual(4.0f, config.SnapDistance, 1e-4f);
        }

        #endregion

        #region Private Methods

        private static T Load<T>(string name) where T : UnityEngine.ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/_Game/Data/Config/{name}.asset");
            Assert.IsNotNull(asset, $"config asset {name} missing");
            return asset;
        }

        #endregion
    }
}
