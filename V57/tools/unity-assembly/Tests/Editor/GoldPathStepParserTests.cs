using System.Collections.Generic;
using NUnit.Framework;

namespace V57.GoldPath.Tests
{
    public sealed class GoldPathStepParserTests
    {
        #region Fields

        private const string ContractExample = @"{ ""gold_path"": { ""scene"": ""Assets/_Game/Scenes/SCN_Level01.unity"",
  ""steps"": [
    { ""id"": ""S01"", ""expect"": ""session.state"", ""op"": ""=="", ""value"": ""Playing"", ""timeout_s"": 5, ""screenshot"": true },
    { ""id"": ""S02"", ""do"": ""hold"", ""action"": ""Launcher/ChargePlunger"", ""seconds"": 0.8 },
    { ""id"": ""S03"", ""do"": ""release"", ""action"": ""Launcher/ChargePlunger"" },
    { ""id"": ""S04"", ""expect"": ""ball.in_play"", ""op"": ""=="", ""value"": true, ""timeout_s"": 3 },
    { ""id"": ""S05"", ""expect"": ""score.value"", ""op"": "">"", ""value"": 0, ""timeout_s"": 20, ""screenshot"": true }
  ] } }";

        #endregion

        #region Tests

        [Test]
        public void ParsesContractExample()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathStepParser.ParseAcceptanceJson(ContractExample, "test", errors);

            Assert.That(errors, Is.Empty);
            Assert.AreEqual("Assets/_Game/Scenes/SCN_Level01.unity", document.ScenePath);
            Assert.AreEqual(5, document.Steps.Count);
            Assert.AreEqual(GoldPathStepType.WaitUntil, document.Steps[0].Type);
            Assert.AreEqual(5f, document.Steps[0].TimeoutSeconds);
            Assert.IsTrue(document.Steps[0].CaptureAfter);
            Assert.AreEqual("Playing", document.Steps[0].Condition.Value);
            Assert.AreEqual(GoldPathStepType.Hold, document.Steps[1].Type);
            Assert.AreEqual("Launcher/ChargePlunger", document.Steps[1].Target);
            Assert.AreEqual(0.8f, document.Steps[1].Seconds, 0.0001f);
            Assert.AreEqual(GoldPathStepType.Release, document.Steps[2].Type);
            Assert.AreEqual("true", document.Steps[3].Condition.Value);
            Assert.AreEqual(">", document.Steps[4].Condition.Op);
            Assert.AreEqual("0", document.Steps[4].Condition.Value);
        }

        [Test]
        public void NullGoldPathYieldsNoStepsWithoutErrors()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathStepParser.ParseAcceptanceJson(@"{ ""criteria"": [], ""gold_path"": null }", "test", errors);

            Assert.IsFalse(document.HasSteps);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void DoAndExpectInOneStepBecomeTwoSteps()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathStepParser.ParseGoldPathJson(
                @"{ ""steps"": [ { ""id"": ""S1"", ""do"": ""press"", ""control"": ""<Keyboard>/space"", ""expect"": ""ball.launched"" } ] }", "test", errors);

            Assert.That(errors, Is.Empty);
            Assert.AreEqual(2, document.Steps.Count);
            Assert.AreEqual(GoldPathStepType.Press, document.Steps[0].Type);
            Assert.AreEqual(GoldPathStepType.Expect, document.Steps[1].Type);
            Assert.AreEqual("S1.expect", document.Steps[1].Id);
        }

        [Test]
        public void MoveWithoutSecondsIsHeldUntilRelease()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathStepParser.ParseGoldPathJson(
                @"{ ""steps"": [ { ""do"": ""move"", ""action"": ""Player/Move"", ""value"": ""1,0"" }, { ""do"": ""release"", ""action"": ""Player/Move"" } ] }", "test", errors);

            Assert.That(errors, Is.Empty);
            Assert.AreEqual(GoldPathStepType.Move, document.Steps[0].Type);
            Assert.AreEqual(0f, document.Steps[0].Seconds);
            Assert.AreEqual(GoldPathStepType.Release, document.Steps[1].Type);
        }

        [TestCase(@"{ ""steps"": [ { ""do"": ""jump"", ""action"": ""Player/Jump"" } ] }")]
        [TestCase(@"{ ""steps"": [ { ""do"": ""press"" } ] }")]
        [TestCase(@"{ ""steps"": [ { ""id"": ""S9"" } ] }")]
        [TestCase(@"{ ""steps"": [ { ""expect"": ""score"", ""op"": ""~="", ""value"": 1 } ] }")]
        public void InvalidStepsReportErrors(string json)
        {
            List<string> errors = new List<string>();
            GoldPathStepParser.ParseGoldPathJson(json, "test", errors);

            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void ParsesWaitMoveCaptureAndWaitUntil()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathStepParser.ParseGoldPathJson(
                @"{ ""steps"": [
                    { ""do"": ""wait"", ""seconds"": 1.5 },
                    { ""do"": ""move"", ""action"": ""Player/Move"", ""value"": ""0,1"", ""seconds"": 2 },
                    { ""do"": ""capture"", ""name"": ""mid"" },
                    { ""do"": ""wait_until"", ""condition"": ""enemies.alive <= 0"" } ] }", "test", errors);

            Assert.That(errors, Is.Empty);
            Assert.AreEqual(GoldPathStepType.Wait, document.Steps[0].Type);
            Assert.AreEqual(GoldPathStepType.Move, document.Steps[1].Type);
            Assert.AreEqual("0,1", document.Steps[1].Value);
            Assert.AreEqual(GoldPathStepType.Capture, document.Steps[2].Type);
            Assert.AreEqual("mid", document.Steps[2].Value);
            Assert.AreEqual(GoldPathStepType.WaitUntil, document.Steps[3].Type);
            Assert.AreEqual(GoldPathStepParser.DefaultTimeoutSeconds, document.Steps[3].TimeoutSeconds);
            Assert.AreEqual("<=", document.Steps[3].Condition.Op);
        }

        #endregion
    }
}
