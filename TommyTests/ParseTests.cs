using System;
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
            bare_key = ""value""#Another comment
            bare-key = ""value""
            1234 = ""value""
            escaped-key = ""Hello\nWorld""
            literal-key = 'Hello\nWorld'
            escaped-quote = ""Hello, \""world\""""
            ";

            var expectedNode = new TomlNode
            {
                    ["key"] = "value",
                    ["bare_key"] = "value",
                    ["bare-key"] = "value",
                    ["1234"] = "value",
                    ["escaped-key"] = "Hello\nWorld",
                    ["literal-key"] = "Hello\\nWorld",
                    ["escaped-quote"] = "Hello, \"world\""
            };
            
            using(StringReader sr = new StringReader(input))
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
        }

        [TestMethod]
        public void TestTableParse()
        {
            string input = @"
            # Test of table nodes

            str1 = ""Hello, root node!""
            
            [foo] # base table
            str1 = ""Hello, foo!""

            [foo.bar] # Test that subkeying works
            str1 = ""Hello, foo.bar!""

            [foo.bar.""$baz ^?\n""] # Test that stringed values work too
            str1 = ""Hello, weird boy!""

            [ baz ] # This has some extra whitespace
            str1 = ""Hello, baz!""
            ";

            var expectedNode = new TomlNode
            {
                    ["str1"] = "Hello, root node!",
                    ["foo"] = new TomlTable
                    {
                        ["str1"] = "Hello, foo!",
                        ["bar"] = new TomlTable
                        {
                            ["str1"] = "Hello, foo.bar!",
                            ["$baz ^?\n"] = new TomlTable
                            {
                                ["str1"] = "Hello, weird boy!"
                            }
                        }
                    },
                    ["baz"] = new TomlTable
                    {
                        ["str1"] = "Hello, baz!"
                    }
            };

            using (StringReader sr = new StringReader(input))
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
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

        [TestMethod]
        public void TestSubkeyParse()
        {
            string input = @"
            # Test parsing  subkeys

            test.str1 = ""Hello, world!""
            test.str2 = ""Hello, world!""

            ""test"".str4 = ""Hello, world!""
            test2.""$foo\n"".'bar\n'.baz = ""Hello, world!""
            ";

            var correctNode = new TomlNode
            {
                ["test"] = new TomlNode
                {
                    ["str1"] = "Hello, world!",
                    ["str2"] = "Hello, world!",
                    ["str4"] = "Hello, world!"
                },
                ["test2"] = new TomlNode
                {
                    ["$foo\n"] = new TomlNode
                    {
                        ["bar\\n"] = new TomlNode
                        {
                            ["baz"] = "Hello, world!"
                        }
                    }
                }
            };

            using (StringReader sr = new StringReader(input))
                Assert.That.TomlNodesAreEqual(correctNode, TOML.Parse(sr));
        }

        [TestMethod]
        public void TestMultilineValueParse()
        {
            string input = @"
            # The following strings are byte-for-byte equivalent:
            str1 = ""The quick brown fox jumps over the lazy dog.""

            str2 = """"""
The quick brown \


                    fox jumps over \
                    the lazy dog.""""""

            str3 = """"""\
                    The quick brown \
                    fox jumps over \
                    the lazy dog.\
                   """"""

            regex2 = '''I [dw]on't need \d{2} apples'''

            lines  = '''
The first newline is
trimmed in raw strings.
  All other whitespace
  is preserved.
'''
            ";

            var expectedNode = new TomlNode
            {
                ["str1"] = "The quick brown fox jumps over the lazy dog.",
                ["str2"] = "The quick brown fox jumps over the lazy dog.",
                ["str3"] = "The quick brown fox jumps over the lazy dog.",
                ["regex2"] = @"I [dw]on't need \d{2} apples",
                ["lines"] = $"The first newline is{Environment.NewLine}trimmed in raw strings.{Environment.NewLine}  All other whitespace{Environment.NewLine}  is preserved.{Environment.NewLine}"
            };

            using (StringReader sr = new StringReader(input))
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
        }
    }
}
