using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Tommy.Tests.Util;

// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

namespace Tommy.Tests
{
    public class ParseTests
    {
        [Test]
        [TestCaseSource(nameof(BasicTests), Category = "Basic tests")]
        public void ParsePositiveTest(string toml, string expectedJson)
        {
            Console.WriteLine(toml);
            Console.WriteLine(expectedJson);
            TomlNode tomlNode = null;
            try
            {
                tomlNode = TOML.Parse(new StringReader(toml));
            }
            catch (TomlParseException pe)
            {
                var sb = new StringBuilder().AppendLine("Parse error:");
                foreach (var se in pe.SyntaxErrors)
                    sb.AppendLine($"({se.Line}:{se.Column}) [{se.ParseState}] {se.Message}");
                Assert.Fail(sb.ToString());
            }

            var json = tomlNode.ToJsonString();
            Assert.AreEqual(expectedJson, json);
        }

        private static readonly ParseTestCollection BasicTests = new()
        {
            new(@"
            # This is a full-line comment
            key = ""value""  # This is a comment at the end of a line
            another = ""# This is not a comment""
            ", @"{""key"":""value"",""another"":""# This is not a comment""}")
        };
    }
}