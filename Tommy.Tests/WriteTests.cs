using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Tommy.Tests
{
    [TestFixture]
    public class WriteTests
    {
        [Test]
        [TestCaseSource(nameof(WriteSuccessTests), new object[] {nameof(ArrayTests)}, Category = "Array tests")]
        [TestCaseSource(nameof(WriteSuccessTests), new object[] {nameof(BooleanTests)}, Category = "Boolean tests")]
        [TestCaseSource(nameof(WriteSuccessTests), new object[] {nameof(CommentTests)}, Category = "Comment tests")]
        [TestCaseSource(nameof(WriteSuccessTests), new object[] {nameof(DateTimeTests)}, Category = "DateTime tests")]
        [TestCaseSource(nameof(WriteSuccessTests), new object[] {nameof(FloatTests)}, Category = "Float tests")]
        [TestCaseSource(nameof(WriteSuccessTests), new object[] {nameof(GenericTests)}, Category = "Generic tests")]
        public void TestSuccessWrite(WriteSuccessTest test)
        {
            using var tw = File.CreateText(Path.Combine("cases", "write", $"{test.FileName}.toml"));
            test.Table.WriteTo(tw);
        }

        private static IEnumerable<WriteSuccessTest> WriteSuccessTests(string testName)
        {
            var type = typeof(WriteTests).GetNestedType(testName,
                                                        BindingFlags.IgnoreCase |
                                                        BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Static);

            var methods =
                type?.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                     .Where(p => p.PropertyType == typeof(TomlTable) && p.CanRead) ??
                throw new NullReferenceException($"Couldn't find test type {testName}");
            foreach (var m in methods)
                yield return new WriteSuccessTest($"{type.Name}::{m.Name}", m.Name, m.GetValue(null) as TomlTable);
        }

        public record WriteSuccessTest(string Name, string FileName, TomlTable Table)
        {
            public override string ToString() => Name;
        }

        private static class ArrayTests
        {
            private static TomlTable Array1 => new()
            {
                ["integers"] = new TomlArray {1, 2, 3}
            };

            private static TomlTable Array2 => new()
            {
                ["colors"] = new TomlArray {"red", "yellow", "green"}
            };

            private static TomlTable Array3 => new()
            {
                ["nested_array_of_int"] = new TomlArray
                {
                    new TomlArray {1, 2},
                    new TomlArray {3, 4, 5}
                }
            };

            private static TomlTable Array4 => new()
            {
                ["nested_array_of_int"] = new TomlArray
                {
                    new TomlString
                    {
                        Value = "all",
                        IsMultiline = false,
                        PreferLiteral = false
                    },
                    new TomlString
                    {
                        Value = "strings",
                        IsMultiline = false,
                        PreferLiteral = true
                    },
                    new TomlString
                    {
                        Value = "are the same",
                        IsMultiline = true,
                        PreferLiteral = false
                    },
                    new TomlString
                    {
                        Value = "type",
                        IsMultiline = true,
                        PreferLiteral = true
                    }
                }
            };

            private static TomlTable Array5 => new()
            {
                ["nested_mixed_array"] = new TomlArray
                {
                    new TomlArray {1, 2},
                    new TomlArray {"a", "b", "c"}
                }
            };

            private static TomlTable Array6 => new()
            {
                ["numbers"] = new TomlArray {0.1, 0.2, 0.5, 1, 2, 5}
            };

            private static TomlTable Array7 => new()
            {
                ["contributors"] = new TomlArray
                {
                    "Foo Bar <foo@example.com>",
                    new TomlTable
                    {
                        ["name"] = "Baz Qux",
                        ["email"] = "bazqux@example.com",
                        ["url"] = "https://example.com/bazqux"
                    }
                }
            };

            private static TomlTable ArrayOfTables1 => new()
            {
                ["products"] = new TomlArray
                {
                    IsTableArray = true,
                    [0] = new TomlTable
                    {
                        ["name"] = "Hammer",
                        ["sku"] = 738594937
                    },
                    [1] = new TomlTable(),
                    [2] = new TomlTable
                    {
                        ["name"] = "Nail",
                        ["sku"] = 284758393,
                        ["color"] = "gray"
                    },
                }
            };

            private static TomlTable ArrayOfTables2 => new()
            {
                ["fruit"] = new TomlArray
                {
                    IsTableArray = true,
                    [0] = new TomlTable
                    {
                        ["name"] = "apple",
                        ["physical"] = new TomlTable
                        {
                            ["color"] = "red",
                            ["shape"] = "round"
                        },
                        ["variety"] = new TomlArray
                        {
                            IsTableArray = true,
                            [0] = new TomlTable
                            {
                                ["name"] = "red delicious"
                            },
                            [1] = new TomlTable
                            {
                                ["name"] = "granny smith"
                            }
                        }
                    },
                    [1] = new TomlTable
                    {
                        ["name"] = "banana",
                        ["variety"] = new TomlArray
                        {
                            IsTableArray = true,
                            [0] = new TomlTable
                            {
                                ["name"] = "plantain"
                            }
                        }
                    }
                }
            };

            private static TomlTable ArrayOfTables3 => new()
            {
                ["points"] = new TomlArray
                {
                    new TomlTable { ["x"] = 1, ["y"] = 2, ["z"] = 3 },
                    new TomlTable { ["x"] = 7, ["y"] = 8, ["z"] = 9 },
                    new TomlTable { ["x"] = 2, ["y"] = 4, ["z"] = 8 },
                }
            };
        }

        private static class BooleanTests
        {
            private static TomlTable Boolean1 => new()
            {
                ["bool1"] = true,
                ["bool2"] = false,
            };
        }

        private static class CommentTests
        {
            private static TomlTable Comment1 => new()
            {
                Comment = "This is a full-line comment",
                ["key"] = new TomlString
                {
                    Comment = "This is a comment for a value",
                    Value = "value"
                }
            };
            
            private static TomlTable Comment2 => new()
            {
                Comment = "eol comments can go anywhere",
                ["abc"] = new TomlArray
                {
                    Comment = "This is an array comment",
                    [0] = new TomlInteger
                    {
                        Comment = "Comment 1",
                        Value = 123
                    },
                    [1] = new TomlInteger
                    {
                        Comment = "Comment 2",
                        Value = 456
                    }
                }
            };
            
            private static TomlTable Comment3 => new()
            {
                Comment = "This is a full-line\tcomment with a tab in the middle",
                ["key"] = new TomlString
                {
                    Comment = "This is a comment\twith a tab in the middle for a value",
                    Value = "value"
                }
            };
        }

        private static class DateTimeTests
        {
            private static TomlTable DateLocal1 => new()
            {
                ["ld1"] = new TomlDateTimeLocal
                {
                    Value = DateTime.Parse("1979-05-27"),
                    Style = TomlDateTimeLocal.DateTimeStyle.Date
                }
            };
            
            private static TomlTable DateTimeOffset1 => new()
            {
                ["odt1"] = DateTimeOffset.Parse("1979-05-27T07:32:00Z"),
                ["odt2"] = DateTimeOffset.Parse("1979-05-27T00:32:00-07:00"),
                ["odt3"] = new TomlDateTimeOffset
                {
                    SecondsPrecision = 6,
                    Value = DateTimeOffset.Parse("1979-05-27T00:32:00.999999-07:00")
                },
                ["odt4"] = new TomlDateTimeOffset
                {
                    SecondsPrecision = 3,
                    Value = DateTimeOffset.Parse("1979-05-27T07:32:00.123Z")
                },
                ["odt5"] = new TomlDateTimeOffset
                {
                    SecondsPrecision = 4,
                    Value = DateTimeOffset.Parse("1979-05-27T07:32:00.1239Z")
                }
            };

            private static TomlTable DateTimeLocal1 => new()
            {
                ["ldt1"] = DateTime.Parse("1979-05-27T07:32:00"),
                ["ldt2"] = new TomlDateTimeLocal
                {
                    SecondsPrecision = 6,
                    Value = DateTime.Parse("1979-05-27T00:32:00.999999")
                }
            };
            
            private static TomlTable TimeLocal1 => new()
            {
                ["lt1"] = new TomlDateTimeLocal
                {
                    Style = TomlDateTimeLocal.DateTimeStyle.Time,
                    Value = DateTime.Parse("07:32:00")
                },
                ["lt2"] = new TomlDateTimeLocal
                {
                    SecondsPrecision = 6,
                    Style = TomlDateTimeLocal.DateTimeStyle.Time,
                    Value = DateTime.Parse("00:32:00.999999")
                }
            };
        }

        private static class FloatTests
        {
            private static TomlTable Float1 => new()
            {
                ["flt1"] = 1.0,
                ["flt2"] = -0.01,
                ["flt4"] = 5e+22,
                ["flt5"] = 1e06,
                ["flt6"] = -2E-2,
                ["flt7"] = 6.626e-34,
                ["flt8"] = 224_617.445_991_228,
                ["flt9"] = -0e0
            };
            
            private static TomlTable Float2 => new()
            {
                ["sf1"] = double.PositiveInfinity,
                ["sf2"] = double.NegativeInfinity,
                ["sf3"] = double.NaN,
                ["sf4"] = -double.NaN,
            };
        }

        private static class GenericTests
        {
            private static TomlTable Generic1 => new()
            {
                Comment = "This is a TOML document.",
                ["title"] = "TOML Example",
                ["owner"] =
                {
                    ["name"] = "Tom Preston-Werner",
                    ["dob"] = new TomlDateTimeOffset
                    {
                        Comment = "First class dates",
                        Value = DateTimeOffset.Parse("1979-05-27T07:32:00-08:00")
                    }
                },
                ["database"] =
                {
                    ["server"] = "192.168.1.1",
                    ["ports"] = { 8001, 8001, 8002 },
                    ["connection_max"] = 5000,
                    ["enabled"] = true
                },
                ["servers"] =
                {
                    Comment = "Comments on sections are put on top of the section",
                    ["alpha"] =
                    {
                        ["ip"] = "10.0.0.1",
                        ["dc"] = "eqdc10"
                    },
                    ["beta"] =
                    {
                        ["ip"] = "10.0.0.2",
                        ["dc"] = "eqdc10"
                    }
                },
                ["clients"] =
                {
                    ["data"] = { [0] = { "gamma", "delta" }, [1] = { 1, 2 } }
                },
                ["hosts"] = { "alpha", "omega" }
            };
        }
    }
}