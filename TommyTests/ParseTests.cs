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
        public void TestFailedValidatorEntries()
        {
            string input = @"[[a]]
    [[a.b]]
        [a.b.c]
            d = ""val0""
    [[a.b]]
        [a.b.c]
            d = ""val1""

";

            using (var sr = new StringReader(input))
            {
                TOML.Parse(sr);
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

            var expectedNode = new TomlNode
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

            var expectedNode = new TomlNode
            {
                ["test"] = new TomlNode[]
                {
                    new TomlTable
                    {
                        ["foo"] = "Hello",
                        ["bar"] = "World"
                    },
                    new TomlTable
                    {
                        ["foo"] = "Foo",
                        ["bar"] = "Bar"
                    }
                },
                ["nested-keys"] = new TomlNode[]
                {
                    new TomlTable
                    {
                        ["foo"] = "Foo",
                        ["bar"] = "Bar",
                        ["inside"] = new TomlNode[]
                        {
                            new TomlTable
                            {
                                ["insider"] = "wew"
                            },
                            new TomlTable
                            {
                                ["insider"] = "wew2"
                            }
                        }
                    },
                    new TomlTable
                    {
                        ["foo"] = "Foo2",
                        ["bar"] = "Bar2",
                        ["inside"] = new TomlNode[]
                        {
                            new TomlTable
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

            var expectedNode = new TomlNode
            {
                ["inline-table"] = new TomlTable
                {
                    ["foo"] = "foo",
                    ["bar"] = "bar"
                },
                ["inline-table-arrays"] = new TomlTable
                {
                    ["arr"] = new TomlNode[] {"foo", "bar"}
                },
                ["nested-table"] = new TomlTable
                {
                    ["foo"] = new TomlNode
                    {
                        ["bar"] = new TomlTable
                        {
                            ["bar"] = "Hello",
                            ["foo"] = new TomlNode
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
            multi_array = [[""bananas"", 'apples'], ""Dunno what this is"", [ 'Veemo', 'Woomy!' ]]
            ";

            var expectedNode = new TomlNode
            {
                ["array"] = new TomlNode[] {"foo", "bar", "baz"},
                ["multiline_array"] = new TomlNode[] {"foo", "bar", "baz"},
                ["complex_array"] = new TomlNode[]
                {
                    "This is a test of a complex multiline string", "bar",
                    $"Just to make sure{Environment.NewLine}we still work as expected{Environment.NewLine}because reasons",
                    "baz"
                },
                ["empty_array"] = new TomlArray(),
                ["multi_array"] = new TomlNode[]
                    {new TomlNode[] {"bananas", "apples"}, "Dunno what this is", new TomlNode[] {"Veemo", "Woomy!"}}
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
                },
                ["a"] = new TomlNode
                {
                    ["b"] = new TomlNode
                    {
                        ["c"] = new TomlNode
                        {
                            ["d"] = new TomlTable
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

            var expectedNode = new TomlNode
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