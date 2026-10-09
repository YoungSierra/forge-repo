using NUnit.Framework;
using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Tests.EditMode
{
    /// <summary>Player camera control (D-254/D-255): orbit limits, idle recentering and zoom range.</summary>
    public sealed class CameraLookTests
    {
        #region Fields

        private CameraConfig _config;

        #endregion

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CameraConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void PitchStaysInsideTheConfiguredRange()
        {
            CameraLook look = new CameraLook(_config);
            look.AddLook(new Vector2(0f, 500f));
            Assert.AreEqual(_config.PitchRange.y, look.Pitch, 0.001f);
            look.AddLook(new Vector2(0f, -1000f));
            Assert.AreEqual(_config.PitchRange.x, look.Pitch, 0.001f);
        }

        [Test]
        public void OrbitHoldsDuringTheDelayThenRecentersBehindTheProfessor()
        {
            CameraLook look = new CameraLook(_config);
            look.AddLook(new Vector2(60f, 20f));
            Tick(look, _config.RecenterDelay * 0.9f);
            Assert.AreEqual(60f, look.Yaw, 0.001f);
            Tick(look, _config.RecenterDelay + _config.RecenterTime * 8f);
            Assert.AreEqual(0f, look.Yaw, 0.05f);
            Assert.AreEqual(0f, look.Pitch, 0.05f);
        }

        [Test]
        public void ZoomIsClampedAndNotRecentered()
        {
            CameraLook look = new CameraLook(_config);
            for (int i = 0; i < 50; i++)
            {
                look.AddZoom(_config.ZoomStep);
            }

            Assert.AreEqual(_config.ZoomRange.x, look.Zoom, 0.001f);
            Tick(look, 10f);
            Assert.AreEqual(_config.ZoomRange.x, look.Zoom, 0.001f);
            for (int i = 0; i < 50; i++)
            {
                look.AddZoom(-_config.ZoomStep);
            }

            Assert.AreEqual(_config.ZoomRange.y, look.Zoom, 0.001f);
        }

        #endregion

        #region Private Methods

        private static void Tick(CameraLook look, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.02f)
            {
                look.Tick(0.02f);
            }
        }

        #endregion
    }
}
