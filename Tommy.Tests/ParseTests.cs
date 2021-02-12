using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using SimpleJSON;
using Tommy.Tests.Util;

namespace Tommy.Tests
{
    public class ParseTests
    {
        [Test]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"keys"}, Category = "Key tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"string"}, Category = "String tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"integer"}, Category = "Integer tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"float"}, Category = "Float tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"boolean"}, Category = "Boolean tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"datetime-offset"}, Category = "Datetime (offset) tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"datetime-local"}, Category = "Datetime (local) tests")]
        public void ParsePositiveTest(SuccessTest test)
        {
            TomlNode tomlNode = null;
            try
            {
                tomlNode = TOML.Parse(new StringReader(test.Toml));
            }
            catch (TomlParseException pe)
            {
                var sb = new StringBuilder().AppendLine("Parse error:");
                foreach (var se in pe.SyntaxErrors)
                    sb.AppendLine($"({se.Line}:{se.Column}) [{se.ParseState}] {se.Message}");
                Assert.Fail(sb.ToString());
            }

            var json = tomlNode.ToCompactJsonString();
            var expectedJson = JSON.Parse(test.Json).ToString(); // Normalize by making it unindented
            Assert.AreEqual(expectedJson, json);
        }

        private static IEnumerable<SuccessTest> TestParseSuccess(string caseSetName)
        {
            var casesPath = Path.Combine("cases", "parse-success", caseSetName);
            foreach (var tomlFile in Directory.EnumerateFiles(casesPath, "*.toml"))
            {
                var testName = Path.GetFileNameWithoutExtension(tomlFile);
                var jsonFile = Path.Combine(casesPath, $"{testName}.json");
                yield return new SuccessTest(File.ReadAllText(tomlFile), File.ReadAllText(jsonFile), testName);
            }
        }

        public record SuccessTest(string Toml, string Json, string TestName)
        {
            public override string ToString() => TestName;
        }
    }
}