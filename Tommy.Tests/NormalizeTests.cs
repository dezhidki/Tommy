using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Tommy.Tests
{
    [TestFixture]
    public class NormalizeTests
    {
        [Test]
        [TestCaseSource(nameof(NormalizeTestIter))]
        public void TestNormalize(NormalizeTest test)
        {
            TomlNode tomlNode = null;
            try
            {
                tomlNode = TOML.Parse(new StringReader(test.InToml));
            }
            catch (TomlParseException pe)
            {
                var sb = new StringBuilder().AppendLine("Parse error:");
                foreach (var se in pe.SyntaxErrors)
                    sb.AppendLine($"({se.Line}:{se.Column}) [{se.ParseState}] {se.Message}");
                Assert.Fail(sb.ToString());
            }

            using var tw = new StringWriter();
            tomlNode.WriteTo(tw);

            var str = tw.ToString();

            Assert.AreEqual(test.OutToml, str);
        }

        private static IEnumerable<NormalizeTest> NormalizeTestIter()
        {
            var casesPath = Path.Combine("cases", "normalize");
            foreach (var tomlFile in Directory.EnumerateFiles(casesPath, "*-in.toml"))
            {
                var name = Path.GetFileNameWithoutExtension(tomlFile)[..^3];
                yield return new NormalizeTest(File.ReadAllText(tomlFile),
                                               File.ReadAllText(Path.Combine(casesPath, $"{name}-out.toml")),
                                               name);
            }
        }

        public record NormalizeTest(string InToml, string OutToml, string TestName)
        {
            public override string ToString() => TestName;
        }
    }
}