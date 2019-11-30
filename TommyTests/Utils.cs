using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tommy;

namespace TommyTests
{
    public static class Utils
    {
        public static void TomlNodesAreEqual(this Assert assert, TomlNode expected, TomlNode actual, bool ignoreComments = true)
        {
            Assert.AreEqual(expected.ChildrenCount, actual.ChildrenCount);
            Assert.IsInstanceOfType(actual, expected.GetType());
            Assert.AreEqual(expected.CollapseLevel, actual.CollapseLevel);

            if(!ignoreComments)
                Assert.AreEqual(expected.Comment, actual.Comment, "The comments are not the same!");

            if (actual is TomlString actString && expected is TomlString expString)
                Assert.AreEqual(expString.Value.Replace("\r\n", "\n"), actString.Value.Replace("\r\n", "\n"));
            else if (actual is TomlBoolean actBool && expected is TomlString expBool)
                Assert.AreEqual(expBool.Value, actBool.Value);
            else if (actual is TomlInteger actInt && expected is TomlInteger expInt)
                Assert.AreEqual(expInt.Value, actInt.Value);
            else if (actual is TomlFloat actFloat && expected is TomlString expFloat)
                Assert.AreEqual(expFloat.Value, actFloat.Value);
            else if (actual is TomlDateTime actDateTime && expected is TomlDateTime expDateTime)
                Assert.AreEqual(expDateTime.Value.ToUniversalTime(), actDateTime.Value.ToUniversalTime());

            if (actual is TomlArray actualArray && expected is TomlArray expectedArray)
            {
                Assert.AreEqual(expectedArray.ChildrenCount, actualArray.ChildrenCount, "Array lengths are not the same!");

                if (expectedArray.ChildrenCount > 0)
                    foreach (var expectedActualPair in expectedArray.Children.Zip(actualArray.Children,
                                                                                (ex, act) => new
                                                                                {
                                                                                        expectedNode = ex,
                                                                                        actualNode = act
                                                                                }))
                        Assert.That.TomlNodesAreEqual(expectedActualPair.expectedNode, expectedActualPair.actualNode);
            }

            var actualKeys = new HashSet<string>(actual.Keys);

            foreach (var expectedKey in expected.Keys)
            {
                if (!actual.TryGetNode(expectedKey, out var node))
                    Assert.Fail($"Child with name {expectedKey} does not exist!");
                Assert.That.TomlNodesAreEqual(expected[expectedKey], node);
                actualKeys.Remove(expectedKey);
            }

            if (actualKeys.Count != 0)
                Assert.Fail($"The following keys were unexpected: {string.Join(", ", actualKeys)}");
        }
    }
}