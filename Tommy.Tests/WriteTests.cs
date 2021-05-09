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
    }
}