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
        public void TestComments()
        {
            string input = @"
            # This is a test comment
            # The first comment will always be attached to the root node

            # This comment is related to the section
            [test]
            # This comment is related to the value
            val = 'foo'

            # Multiline comments are permitted
            # As long as you want!
            [test2]
            val = 'bar'
            ";

            var expectedNode = new TomlTable
            {
                Comment = "This is a test comment\r\nThe first comment will always be attached to the root node",
                ["test"] =
                {
                    Comment = "This comment is related to the section",
                    ["val"] = new TomlString
                    {
                        Comment = "This comment is related to the value",
                        Value = "foo"
                    }
                },
                ["test2"] =
                {
                    Comment = "Multiline comments are permitted\r\nAs long as you want!",
                    ["val"] = "bar"
                }
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr), false);
            }
        }

        [TestMethod]
        public void TestValueParse()
        {
            var input = @"
            # Test of various values

            str1 = 'Hello, world!'

            int1 = 10
            int2 = +10
            int3 = -10
            int4 = 1_0_0_0

            hex = 0xbeef
            oct = 0o1337
            bin = 0b110011

            float1 = 1.0
            float2 = +1.0
            float3 = -1.0
            float4 = 0.125
            float5 = 1e+6
            float6 = 1e-6

            inftest = inf
            inftest2 = -inf
            nantest = nan

            bool1 = true
            bool2 = false

            date1 = 1979-05-27T07:32:00Z
            date2 = 1979-05-27 07:32:00Z
            date3 = 1979-05-27T07:32:00
            date4 = 07:32:00
            date5 = 1979-05-27
            ";

            var expectedNode = new TomlTable
            {
                ["str1"] = "Hello, world!",
                ["int1"] = 10,
                ["int2"] = +10,
                ["int3"] = -10,
                ["int4"] = 1000,
                ["hex"] = 0xbeef,
                ["oct"] = Convert.ToInt32("1337", 8),
                ["bin"] = 0b110011,

                ["float1"] = 1.0,
                ["float2"] = 1.0,
                ["float3"] = -1.0,
                ["float4"] = 0.125,
                ["float5"] = 1e6,
                ["float6"] = 1e-6,

                ["inftest"] = float.PositiveInfinity,
                ["inftest2"] = float.NegativeInfinity,
                ["nantest"] = float.NaN,

                ["bool1"] = true,
                ["bool2"] = false,

                ["date1"] = new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Utc),
                ["date2"] = new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Utc),
                ["date3"] = new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Local),
                ["date4"] = new DateTime(DateTime.Today.Year,
                                         DateTime.Today.Month,
                                         DateTime.Today.Day,
                                         7,
                                         32,
                                         0,
                                         DateTimeKind.Local),
                ["date5"] = new DateTime(1979, 05, 27, 0, 0, 0, DateTimeKind.Local)
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestArrayTableParse()
        {
            var input = @"
            # Test array tables

            [[test]]
            foo = 'Hello'
            bar = 'World'

            [[test]]
            foo = 'Foo'
            bar = 'Bar'

            [[nested-keys]]
            foo = 'Foo'
            bar = 'Bar'

                [[nested-keys.inside]]
                insider = 'wew'

                [[nested-keys.inside]]
                insider = 'wew2'

            [[nested-keys]]
            foo = 'Foo2'
            bar = 'Bar2'

                [[nested-keys.inside]]
                insider = 'wew2'
            ";

            var expectedNode = new TomlTable
            {
                ["test"] = new TomlArray
                {
                    IsTableArray = true,
                    [0] =
                    {
                        ["foo"] = "Hello",
                        ["bar"] = "World"
                    },
                    [1] =
                    {
                        ["foo"] = "Foo",
                        ["bar"] = "Bar"
                    }
                },
                ["nested-keys"] = new TomlArray
                {
                    IsTableArray = true,
                    [0] =
                    {
                        ["foo"] = "Foo",
                        ["bar"] = "Bar",
                        ["inside"] = new TomlArray
                        {
                            IsTableArray = true,
                            [0] = 
                            {
                                ["insider"] = "wew"
                            },
                            [1] =
                            {
                                ["insider"] = "wew2"
                            }
                        }
                    },
                    [1] =
                    {
                        ["foo"] = "Foo2",
                        ["bar"] = "Bar2",
                        ["inside"] = new TomlArray
                        {
                            IsTableArray = true,
                            [0] =
                            {
                                ["insider"] = "wew2"
                            }
                        }
                    }
                }
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestInlineTableParse()
        {
            var input = @"
            # Test inline tables

            inline-table = { foo = ""foo"", bar = ""bar"" } # Inline table support
            inline-table-arrays = { arr = [                 # Multiline inline tables only allowed for multiline values
                        ""foo"",
                        ""bar"",
            ] }
            
            nested-table = { foo.bar = { bar = 'Hello', foo.baz = 'World!' }, ""$0"" = ""Hello, world!"" } # Nested inline tables with literal keys
            ";

            var expectedNode = new TomlTable
            {
                ["inline-table"] = new TomlTable
                {
                    IsInline = true,
                    ["foo"] = "foo",
                    ["bar"] = "bar"
                },
                ["inline-table-arrays"] = new TomlTable
                {
                    IsInline = true,
                    ["arr"] =
                    {
                        [0] = "foo",
                        [1] = "bar"
                    }
                },
                ["nested-table"] = new TomlTable
                {
                    IsInline = true,
                    ["foo"] =
                    {
                        ["bar"] = new TomlTable
                        {
                            IsInline = true,
                            ["bar"] = "Hello",
                            ["foo"] =
                            {
                                ["baz"] = "World!"
                            }
                        }
                    },
                    ["$0"] = "Hello, world!"
                }
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestArrayParse()
        {
            var input = @"
            # Test array values

            array = [ ""foo"", ""bar"", ""baz"" ]
            multiline_array = [ ""foo"",
            ""bar"",
            ""baz"", # The terminating comma is permitted
            ]
            complex_array = [ 
                                """"""\
                                This is a test of a complex \
                                multiline \
                                string\
                                """""",
                                'bar',
                                '''Just to make sure
we still work as expected
because reasons'''
                                ,
                                
                                
                                ""baz""
                                ,
                                          # Comments still work

                                ]
           
            empty_array = []
            multi_array = [[""bananas"", 'apples'], [""Dunno what this is""], [ 'Veemo', 'Woomy!' ]]
            ";

            var expectedNode = new TomlTable
            {
                ["array"] = {"foo", "bar", "baz"},
                ["multiline_array"] = {"foo", "bar", "baz"},
                ["complex_array"] =
                {
                    "This is a test of a complex multiline string", "bar",
                    $"Just to make sure{Environment.NewLine}we still work as expected{Environment.NewLine}because reasons",
                    "baz"
                },
                ["empty_array"] = new TomlArray(),
                ["multi_array"] = { new TomlNode[] { "bananas", "apples"}, new TomlNode[] { "Dunno what this is" }, new TomlNode[] { "Veemo", "Woomy!"}}
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestKeyParse()
        {
            var input = @"
            # This is a test comment
            
            key=""value"" # This is a comment
            # key = ""value2""
            bare_key = ""value""#Another comment
            bare-key = ""value""
            1234 = ""value""
            escaped-key = ""Hello\nWorld""
            literal-key = 'Hello\nWorld'
            escaped-quote = ""Hello, \""world\""""
            ";

            var expectedNode = new TomlTable
            {
                ["key"] = "value",
                ["bare_key"] = "value",
                ["bare-key"] = "value",
                ["1234"] = "value",
                ["escaped-key"] = "Hello\nWorld",
                ["literal-key"] = "Hello\\nWorld",
                ["escaped-quote"] = "Hello, \"world\""
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestTableParse()
        {
            var input = @"
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
                
            [ a . b . c  . d  ] # We should also allow spaces around the table key
            str1 = ""Hello, separated!""
            ";

            var expectedNode = new TomlTable
            {
                ["str1"] = "Hello, root node!",
                ["foo"] =
                {
                    ["str1"] = "Hello, foo!",
                    ["bar"] =
                    {
                        ["str1"] = "Hello, foo.bar!",
                        ["$baz ^?\n"] =
                        {
                            ["str1"] = "Hello, weird boy!"
                        }
                    }
                },
                ["baz"] =
                {
                    ["str1"] = "Hello, baz!"
                },
                ["a"] =
                {
                    ["b"] =
                    {
                        ["c"] =
                        {
                            ["d"] =
                            {
                                ["str1"] = "Hello, separated!"
                            }
                        }
                    }
                }
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestEmptyKey()
        {
            var input = @"
            key = # This should be invalid
            ";

            using (var sr = new StringReader(input))
            {
                var fail = false;
                try
                {
                    TOML.Parse(sr);
                }
                catch (Exception e)
                {
                    fail = true;
                }

                if (!fail)
                    Assert.Fail("The invalid key should cause an exception");
            }
        }

        [TestMethod]
        public void TestSubkeyParse()
        {
            var input = @"
            # Test parsing  subkeys

            test.str1 = ""Hello, world!""
            test.str2 = ""Hello, world!""

            ""test"".str4 = ""Hello, world!""
            test2.""$foo\n"".'bar\n'.baz = ""Hello, world!""
            ";

            var correctNode = new TomlTable
            {
                ["test"] =
                {
                    ["str1"] = "Hello, world!",
                    ["str2"] = "Hello, world!",
                    ["str4"] = "Hello, world!"
                },
                ["test2"] =
                {
                    ["$foo\n"] =
                    {
                        ["bar\\n"] =
                        {
                            ["baz"] = "Hello, world!"
                        }
                    }
                }
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(correctNode, TOML.Parse(sr));
            }
        }

        [TestMethod]
        public void TestMultilineValueParse()
        {
            var input = @"
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
            
            # Some strings to test literal multiline parsing
            regex2 = '''I [dw]on't need \d{2} apples'''

            lines  = '''
The first newline is
trimmed in raw strings.
  All other whitespace
  is preserved.
'''
            ";

            var expectedNode = new TomlTable
            {
                ["str1"] = "The quick brown fox jumps over the lazy dog.",
                ["str2"] = "The quick brown fox jumps over the lazy dog.",
                ["str3"] = "The quick brown fox jumps over the lazy dog.",
                ["regex2"] = @"I [dw]on't need \d{2} apples",
                ["lines"] =
                    $"The first newline is{Environment.NewLine}trimmed in raw strings.{Environment.NewLine}  All other whitespace{Environment.NewLine}  is preserved.{Environment.NewLine}"
            };

            using (var sr = new StringReader(input))
            {
                Assert.That.TomlNodesAreEqual(expectedNode, TOML.Parse(sr));
            }
        }
    }
}