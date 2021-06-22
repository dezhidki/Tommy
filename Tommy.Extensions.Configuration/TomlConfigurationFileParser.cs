using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Tommy.Extensions.Configuration
{
    public class TomlConfigurationFileParser
    {
        private readonly Stack<string> _context = new();

        private readonly IDictionary<string, string> _data =
            new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _currentPath;

        private TomlConfigurationFileParser() { }

        public static IDictionary<string, string> Parse(Stream input)
            => new TomlConfigurationFileParser().ParseStream(input);

        private IDictionary<string, string> ParseStream(Stream input)
        {
            _data.Clear();

            var table = TOML.Parse(new StreamReader(input));
            VisitElement(table);

            return _data;
        }

        private void VisitElement(TomlTable element)
        {
            foreach (var keyValuePair in element.RawTable)
            {
                EnterContext(keyValuePair.Key);
                VisitValue(keyValuePair.Value);
                ExitContext();
            }
        }

        private void VisitValue(TomlNode value)
        {
            switch (value)
            {
                case TomlTable table:
                    VisitElement(table);
                    break;

                case TomlArray array:
                    var index = 0;
                    foreach (var arrayElement in array.RawArray)
                    {
                        EnterContext(index.ToString());
                        VisitValue(arrayElement);
                        ExitContext();
                        index++;
                    }

                    break;

                case TomlBoolean:
                case TomlDateTime:
                case TomlFloat:
                case TomlInteger:
                case TomlString:
                    var key = _currentPath;
                    if (_data.ContainsKey(key)) throw new FormatException($"A duplicate key '{key}' was found.");

                    _data[key] = value.ToString();
                    break;

                default:
                    throw new FormatException($"Unsupported TOML token '{value.GetType()}' was found.");
            }
        }

        private void EnterContext(string context)
        {
            _context.Push(context);
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }
    }
}