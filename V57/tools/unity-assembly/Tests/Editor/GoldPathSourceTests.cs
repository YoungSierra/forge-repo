using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace V57.GoldPath.Tests
{
    public sealed class GoldPathSourceTests
    {
        #region Fields

        private const string Steps = @"""steps"": [ { ""do"": ""wait"", ""seconds"": 1 } ]";
        private string _directory;

        #endregion

        #region Tests

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "v57-goldpath-source-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        [Test]
        public void AuthoredFileWinsOverAcceptance()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathSource.Load(Write("acceptance.json", @"{ ""gold_path"": { ""status"": ""final"", " + Steps + " } }"),
                Write("gold_path.json", @"{ ""scene"": ""SCN_A"", " + Steps + " }"), errors);

            Assert.That(errors, Is.Empty);
            StringAssert.EndsWith("gold_path.json", document.SourcePath);
            Assert.AreEqual("SCN_A", document.ScenePath);
        }

        [Test]
        public void DraftAcceptanceWithoutAuthoredFileIsAnError()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathSource.Load(Write("acceptance.json", @"{ ""gold_path"": { ""status"": ""draft"", " + Steps + " } }"),
                Path.Combine(_directory, "missing.json"), errors);

            Assert.IsFalse(document.HasSteps);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void FinalAcceptanceIsUsedWhenNoAuthoredFile()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathSource.Load(Write("acceptance.json", @"{ ""gold_path"": { ""status"": ""final"", " + Steps + " } }"),
                Path.Combine(_directory, "missing.json"), errors);

            Assert.That(errors, Is.Empty);
            Assert.IsTrue(document.HasSteps);
        }

        #endregion

        #region Private Methods

        private string Write(string name, string json)
        {
            string path = Path.Combine(_directory, name);
            File.WriteAllText(path, json);
            return path;
        }

        #endregion
    }
}
