using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using SimpleJSON;
using Tommy.Tests.Util;

namespace Tommy.Tests
{
    [TestFixture]
    public class ParseTests
    {
        [Test]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"array"}, Category = "Array tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"boolean"}, Category = "Boolean tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"comment"}, Category = "Comment ignore tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"date-time"}, Category = "Datetime tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"float"}, Category = "Float tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"integer"}, Category = "Integer tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"key-value"}, Category = "Key-value parse tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"qa"}, Category = "Large data parse tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"string"}, Category = "String tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"table"}, Category = "Table tests")]
        [TestCaseSource(nameof(TestParseSuccess), new object[] {"generic"}, Category = "Generic tests")]
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

            var json = tomlNode.ToComplianceTestJson();
            var expectedJson = JSON.Parse(test.Json).ToString(); // Normalize by making it unindented
            Assert.AreEqual(expectedJson, json);
        }

        [TestCaseSource(nameof(TestParseFailure), new object[] {"array"}, Category = "Array invalid parse tests")]
        [TestCaseSource(nameof(TestParseFailure), new object[] {"comment"}, Category = "Comment invalid parse tests")]
        [TestCaseSource(nameof(TestParseFailure), new object[] {"integer"}, Category = "Integer invalid parse tests")]
        [TestCaseSource(nameof(TestParseFailure), new object[] {"key-value"}, Category = "Key-value invalid parse tests")]
        [TestCaseSource(nameof(TestParseFailure), new object[] {"string"}, Category = "String invalid parse tests")]
        [TestCaseSource(nameof(TestParseFailure), new object[] {"table"}, Category = "Table invalid parse tests")]
        public void ParseNegativeTest(FailureTest test) =>
            Assert.Catch<TomlParseException>(() =>
            {
                var table = TOML.Parse(new StringReader(test.Toml));
                Console.WriteLine(table.ToString());
            });

        private static IEnumerable<SuccessTest> TestParseSuccess(string caseSetName)
        {
            var casesPath = Path.Combine("cases", "valid", caseSetName);
            foreach (var tomlFile in Directory.EnumerateFiles(casesPath, "*.toml"))
            {
                var testName = Path.GetFileNameWithoutExtension(tomlFile);
                var jsonFile = Path.Combine(casesPath, $"{testName}.json");
                yield return new SuccessTest(File.ReadAllText(tomlFile), File.ReadAllText(jsonFile), $"{caseSetName}/{testName}");
            }
        }

        private static IEnumerable<FailureTest> TestParseFailure(string caseSetName)
        {
            var casesPath = Path.Combine("cases", "invalid", caseSetName);
            foreach (var tomlFile in Directory.EnumerateFiles(casesPath, "*.toml"))
                yield return new FailureTest(File.ReadAllText(tomlFile),
                                             $"{caseSetName}/{Path.GetFileNameWithoutExtension(tomlFile)}");
        }

        public record FailureTest(string Toml, string TestName)
        {
            public override string ToString() => TestName;
        }

        public record SuccessTest(string Toml, string Json, string TestName)
        {
            public override string ToString() => TestName;
        }
    }
}