using NUnit.Framework;

namespace V57.GoldPath.Tests
{
    public sealed class JsonPreprocessorTests
    {
        [TestCase(@"{""a"":null,""b"":1}", @"{""b"":1}")]
        [TestCase(@"{""a"":1,""b"":null}", @"{""a"":1}")]
        [TestCase(@"{""a"":1,""b"":null,""c"":2}", @"{""a"":1,""c"":2}")]
        [TestCase(@"{ ""a"" : null }", @"{}")]
        [TestCase(@"{""s"":""null"",""n"":[null,1]}", @"{""s"":""null"",""n"":[null,1]}")]
        [TestCase(@"{""o"":{""x"":null},""y"":3}", @"{""o"":{},""y"":3}")]
        public void StripsNullMembers(string input, string expected)
        {
            Assert.AreEqual(expected, JsonPreprocessor.Process(input));
        }

        [TestCase(@"{""value"":3}", @"{""value"":""3""}")]
        [TestCase(@"{""value"": true }", @"{""value"": ""true"" }")]
        [TestCase(@"{""value"":""Playing""}", @"{""value"":""Playing""}")]
        [TestCase(@"{""other"":3}", @"{""other"":3}")]
        [TestCase(@"{""value"":-1.5e2,""x"":1}", @"{""value"":""-1.5e2"",""x"":1}")]
        public void QuotesScalarsOfSelectedKeys(string input, string expected)
        {
            Assert.AreEqual(expected, JsonPreprocessor.Process(input, "value"));
        }
    }
}
