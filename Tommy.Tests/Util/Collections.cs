using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tommy.Tests.Util
{
    public class ParseTestCollection : IEnumerable
    {
        public class ParseTest
        {
            public readonly string Toml;
            public readonly string ExpectedJson;

            public ParseTest(string toml, string json)
            {
                Toml = toml;
                ExpectedJson = json;
            }
        }

        private readonly List<ParseTest> tests = new();

        public void Add(ParseTest test) => tests.Add(test);
            
        private static string UnIndentToml(string toml) => string.Join("\n",
                                                                       toml.Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        public IEnumerator GetEnumerator() => tests.Select(parseTest => new[] {UnIndentToml(parseTest.Toml), parseTest.ExpectedJson}).GetEnumerator();
    }
}