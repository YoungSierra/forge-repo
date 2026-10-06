using NUnit.Framework;

namespace V57.GoldPath.Tests
{
    public sealed class GoldPathConditionTests
    {
        #region Fields

        private FakeProbe _probe;

        #endregion

        #region Nested Types

        private sealed class FakeProbe : IGoldPathProbe
        {
            public bool TryGetBool(string key, out bool value)
            {
                value = key == "ball.in_play";
                return key == "ball.in_play" || key == "game.over";
            }

            public bool TryGetFloat(string key, out float value)
            {
                value = key == "score.value" ? 150f : 0f;
                return key == "score.value";
            }

            public bool TryGetString(string key, out string value)
            {
                value = key == "session.state" ? "Playing" : null;
                return value != null;
            }
        }

        #endregion

        #region Tests

        [SetUp]
        public void SetUp()
        {
            _probe = new FakeProbe();
            GoldPathProbeRegistry.Register(_probe);
        }

        [TearDown]
        public void TearDown()
        {
            GoldPathProbeRegistry.Unregister(_probe);
        }

        [TestCase("score.value > 100", true)]
        [TestCase("score.value >= 150", true)]
        [TestCase("score.value < 100", false)]
        [TestCase("score.value != 150", false)]
        [TestCase("ball.in_play", true)]
        [TestCase("!ball.in_play", false)]
        [TestCase("!game.over", true)]
        [TestCase("ball.in_play == true", true)]
        [TestCase("ball.in_play == 1", true)]
        [TestCase("session.state == playing", true)]
        [TestCase("session.state != Playing", false)]
        [TestCase("unknown.key", false)]
        [TestCase("unknown.key == 0", false)]
        public void EvaluatesAgainstProbes(string text, bool expected)
        {
            GoldPathCondition condition = GoldPathCondition.Parse(text);

            Assert.IsTrue(condition.IsValid);
            Assert.AreEqual(expected, condition.Evaluate(out string detail), detail);
        }

        [Test]
        public void UnknownKeyDetailSaysUnknown()
        {
            GoldPathCondition.Parse("nobody.answers").Evaluate(out string detail);

            StringAssert.StartsWith("unknown", detail);
        }

        #endregion
    }
}
