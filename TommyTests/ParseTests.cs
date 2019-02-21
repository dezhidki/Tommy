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
            
            key = ""value""
            bare_key = ""value""
            bare-key = ""value""
            1234 = ""value""
            ";
            Dictionary<string, string> keys = new Dictionary<string, string>()
            {
                ["key"] = "value",
                ["bare_key"] = "value",
                ["bare-key"] = "value",
                ["1234"] = "value"
            };

            using (StringReader sr = new StringReader(input))
            {
                var node = TOML.Parse(sr);

                // There should be four children
                Assert.AreEqual(4, node.Children.Count);
                
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

                if (keys.Count != 0)
                    Assert.Fail($"{keys.Count} keys were left undefined!");
            }
        }
    }
}
