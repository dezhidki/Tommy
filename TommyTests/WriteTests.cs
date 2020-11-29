using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tommy;

namespace TommyTests
{
    [TestClass]
    public class WriteTests
    {
        [TestMethod]
        public void TestArrayConstruct()
        {
            var expectedResult = @"array = [ ""hello world"" ]";

            var table = new TomlTable
            {
                ["array"] = new TomlArray
                {
                    "hello world"
                }
            };

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
                table.WriteTo(sw);

            var actualResult = sb.ToString();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void ObjectConstructTest()
        {
            var node = new TomlTable
            {
                ["hello"] = new TomlTable
                {
                    Comment = "This table is used for Hello, world -commands!",
                    ["key"] = "wew",
                    ["key2"] = "we\\\\w2",
                    ["key3"] = new TomlString
                        {
                            Value = "wew\\\\w2",
                            PreferLiteral = true,
                        },
                    ["test"] = new TomlTable
                    {
                        Comment = "This is another section table!",
                        ["foo"] = new TomlString
                        {
                            CollapseLevel = 1,
                            Value = "Value"
                        },
                        ["bar"] = new TomlInteger
                        {
                            Comment = "How many bars there are to eat",
                            Value = 10
                        }
                    }
                },
                ["inline-test"] = new TomlTable
                {
                    IsInline = true,
                    ["foo"] = 10,
                    ["bar"] = "test",
                    ["baz"] = new TomlTable
                    {
                        ["qux"] = new TomlString
                        {
                            CollapseLevel = 1,
                            Value = "test"
                        }
                    }
                },
                ["value"] = 10.0,
                ["table-test"] = new TomlArray
                {
                    IsTableArray = true,
                    [0] = new TomlTable
                    {
                        ["wew"] = "wew"
                    },
                    [1] = new TomlTable
                    {
                        ["wew"] = "veemo"
                    },
                    [2] = new TomlTable
                    {
                        ["wew"] = "woomy",
                        ["wobbly"] = new TomlTable
                        {
                            ["baz"] = new TomlString
                            {
                                CollapseLevel = 1,
                                Value = "test"
                            }
                        }
                    }
                }
            };

            using (var sw = new StringWriter())
            {
                node.WriteTo(sw);
                sw.Flush();

                string s = sw.ToString();
                File.WriteAllText("out.toml", s);
            }
        }
    }
}