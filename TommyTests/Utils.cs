using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tommy;

namespace TommyTests
{
    public static class Utils
    {
        public static void TomlNodesAreEqual(this Assert assert, TomlNode expected, TomlNode actual)
        {
            Assert.AreEqual(expected.Children.Count, actual.Children.Count);
            Assert.IsInstanceOfType(actual, expected.GetType());
            Assert.AreEqual(expected.RawValue, actual.RawValue);

            HashSet<string> actualKeys = new HashSet<string>(actual.Children.Keys);

            foreach (var keyValuePair in expected.Children)
            {
                if(!actual.Children.TryGetValue(keyValuePair.Key, out var node))
                    Assert.Fail($"Child with name {keyValuePair.Key} does not exist!");
                Assert.That.TomlNodesAreEqual(keyValuePair.Value, node);
                actualKeys.Remove(keyValuePair.Key);
            }

            if(actualKeys.Count != 0)
                Assert.Fail($"The following keys were unexpected: {string.Join(", ", actualKeys)}");
        }
    }
}
