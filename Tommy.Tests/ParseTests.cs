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
        private static readonly ParseTestCollection BasicTests = new()
        {
            new(@"
            # This is a full-line comment
            key = ""value""  # This is a comment at the end of a line
            another = ""# This is not a comment""
            ",
                @"{""key"":""value"",""another"":""# This is not a comment""}")
        };

        private static readonly ParseTestCollection KeysTests = new()
        {
            new(@"
            key = ""value""
            bare_key = ""value""
            bare-key = ""value""
            1234 = ""value""
            ",
                @"{""key"":""value"",""bare_key"":""value"",""bare-key"":""value"",""1234"":""value""}"),
            new(@"
            ""127.0.0.1"" = ""value""
            ""character encoding"" = ""value""
            ""ʎǝʞ"" = ""value""
            'key2' = ""value""
            'quoted ""value""' = ""value""
            ",
                @"{""127.0.0.1"":""value"",""character encoding"":""value"",""ʎǝʞ"":""value"",""key2"":""value"",""quoted \""value\"""":""value""}"),
            new(@"
            name = ""Orange""
            physical.color = ""orange""
            physical.shape = ""round""
            site.""google.com"" = true
            ",
                @"{""name"":""Orange"",""physical"":{""color"":""orange"",""shape"":""round""},""site"":{""google.com"":true}}"),
            new(@"
            fruit.name = ""banana""     # this is best practice
            fruit. color = ""yellow""    # same as fruit.color
            fruit . flavor = ""banana""   # same as fruit.flavor
            ",
                @"{""fruit"":{""name"":""banana"",""color"":""yellow"",""flavor"":""banana""}}"),
            new(@"
            # This makes the key ""fruit"" into a table.
            fruit.apple.smooth = true

            # So then you can add to the table ""fruit"" like so:
            fruit.orange = 2
            ",
                @"{""fruit"":{""apple"":{""smooth"":true},""orange"":2}}"),
            new(@"
            apple.type = ""fruit""
            orange.type = ""fruit""

            apple.skin = ""thin""
            orange.skin = ""thick""

            apple.color = ""red""
            orange.color = ""orange""
            ",
                @"{""apple"":{""type"":""fruit"",""skin"":""thin"",""color"":""red""},""orange"":{""type"":""fruit"",""skin"":""thick"",""color"":""orange""}}"),
            new(@"
            3.14159 = ""pi""
            ",
                @"{""3"":{""14159"":""pi""}}")
        };

        [Test]
        [TestCaseSource(nameof(BasicTests), Category = "Basic tests")]
        [TestCaseSource(nameof(KeysTests), Category = "Key tests")]
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
    }
}