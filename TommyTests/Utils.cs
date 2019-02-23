using System.Collections.Generic;
using System.Linq;
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

            if (actual is TomlString str1 && expected is TomlString str2)
                Assert.AreEqual(str2.RawValue, str1.RawValue);

            if (actual is TomlArray actualArray && expected is TomlArray expectedArray)
            {
                Assert.AreEqual(expectedArray.Values.Count, actualArray.Values.Count, "Array lengths are not the same!");

                if (expectedArray.Values.Count > 0)
                    foreach (var expectedActualPair in expectedArray.Values.Zip(actualArray.Values,
                                                                                (ex, act) => new
                                                                                {
                                                                                        expectedNode = ex,
                                                                                        actualNode = act
                                                                                }))
                        Assert.That.TomlNodesAreEqual(expectedActualPair.expectedNode, expectedActualPair.actualNode);
            }

            var actualKeys = new HashSet<string>(actual.Children.Keys);

            foreach (var keyValuePair in expected.Children)
            {
                if (!actual.Children.TryGetValue(keyValuePair.Key, out var node))
                    Assert.Fail($"Child with name {keyValuePair.Key} does not exist!");
                Assert.That.TomlNodesAreEqual(keyValuePair.Value, node);
                actualKeys.Remove(keyValuePair.Key);
            }

            if (actualKeys.Count != 0)
                Assert.Fail($"The following keys were unexpected: {string.Join(", ", actualKeys)}");
        }
    }
}