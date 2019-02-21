using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tommy;

namespace TommyTests
{
    [TestClass]
    public class ParseTests
    {
        [TestMethod]
        public void TestKeyParse()
        {
            string input = @"
            # This is a test comment
            
            key = ""value"" # This is a comment
            # key = ""value2""
            bare_key = ""value""
            bare-key = ""value""
            1234 = ""value""
            escaped-key = ""Hello\nWorld""
            literal-key = 'Hello\nWorld'
            escaped-quote = ""Hello, \""world\""""
            ";
            Dictionary<string, string> keys = new Dictionary<string, string>()
            {
                ["key"] = "value",
                ["bare_key"] = "value",
                ["bare-key"] = "value",
                ["1234"] = "value",
                ["escaped-key"] = "Hello\nWorld",
                ["literal-key"] = @"Hello\nWorld",
                ["escaped-quote"] = "Hello, \"world\""
            };

            using (StringReader sr = new StringReader(input))
            {
                var node = TOML.Parse(sr);

                // There should be four children
                Assert.AreEqual(keys.Count, node.Children.Count, $"There should be {keys.Count} defined, but found {node.Children.Count}");
                
                foreach (var keyValuePair in node.Children)
                {
                    if (keys.TryGetValue(keyValuePair.Key, out var value))
                    {
                        Assert.AreEqual(value, keyValuePair.Value.RawValue, $"Expected [{keyValuePair.Key}]={value} but got {keyValuePair.Value.RawValue}");
                        keys.Remove(keyValuePair.Key);
                    }
                    else
                        Assert.Fail($"Found an undefined key {keyValuePair.Key} = {keyValuePair.Value}");
                }
            }
        }

        [TestMethod]
        public void TestEmptyKey()
        {
            string input = @"
            key = # This should be invalid
            ";

            using (StringReader sr = new StringReader(input))
            {
                bool fail = false;
                try
                {
                    TOML.Parse(sr);
                }
                catch (Exception e)
                {
                    fail = true;
                }

                if(!fail)
                    Assert.Fail("The invalid key should cause an exception");
            }
        }
    }
}
