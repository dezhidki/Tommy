using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Tommy
{
    #region TOML Nodes

    public class TomlNode
    {
        private Dictionary<string, TomlNode> children;
        public Dictionary<string, TomlNode> Children => children ?? (children = new Dictionary<string, TomlNode>());

        public virtual bool HasValue { get; } = false;
        public virtual bool IsArray { get; } = false;
        public virtual bool IsTable { get; } = false;
        public virtual bool IsString { get; } = false;
        public virtual bool IsInteger { get; } = false;
        public virtual bool IsFloat { get; } = false;
        public virtual bool IsDateTime { get; } = false;
        public virtual bool IsBoolean { get; } = false;

        public virtual TomlNode this[string key]
        {
            get => Children[key];
            set => Children[key] = value;
        }

        public static implicit operator TomlNode(string value) =>
            new TomlString
            {
                Value = value
            };

        public static implicit operator TomlNode(bool value) =>
            new TomlBoolean
            {
                Value = value
            };

        public static implicit operator TomlNode(long value) =>
            new TomlInteger
            {
                Value = value
            };

        public static implicit operator TomlNode(float value) =>
            new TomlFloat
            {
                Value = value
            };

        public static implicit operator TomlNode(double value) =>
            new TomlFloat
            {
                Value = value
            };

        public static implicit operator TomlNode(DateTime value) =>
            new TomlDateTime
            {
                Value = value
            };

        public static implicit operator TomlNode(TomlNode[] nodes)
        {
            var result = new TomlArray();
            result.Values.AddRange(nodes);
            return result;
        }
    }

    public class TomlString : TomlNode
    {
        public override bool HasValue { get; } = true;
        public override bool IsString { get; } = true;

        public override TomlNode this[string key]
        {
            get => null;
            set { }
        }

        public string Value { get; set; }
    }

    public class TomlInteger : TomlNode
    {
        public override bool IsInteger { get; } = true;

        public override bool HasValue { get; } = true;

        public override TomlNode this[string key]
        {
            get => null;
            set { }
        }

        public long Value { get; set; }
    }

    public class TomlFloat : TomlNode
    {
        public override bool IsFloat { get; } = true;
        public override bool HasValue { get; } = true;

        public override TomlNode this[string key]
        {
            get => null;
            set { }
        }

        public double Value { get; set; }
    }

    public class TomlBoolean : TomlNode
    {
        public override bool IsBoolean { get; } = true;
        public override bool HasValue { get; } = true;

        public override TomlNode this[string key]
        {
            get => null;
            set { }
        }

        public bool Value { get; set; }
    }

    public class TomlDateTime : TomlNode
    {
        public override bool IsDateTime { get; } = true;
        public override bool HasValue { get; } = true;

        public override TomlNode this[string key]
        {
            get => null;
            set { }
        }

        public DateTime Value { get; set; }
    }

    public class TomlArray : TomlNode
    {
        private List<TomlNode> _values;
        public override bool HasValue { get; } = true;
        public override bool IsArray { get; } = true;
        public bool IsArrayTable { get; set; } = false;

        public override TomlNode this[string key]
        {
            get => null;
            set { }
        }

        public TomlNode this[int index]
        {
            get => Values[index];
            set => Values[index] = value;
        }

        public List<TomlNode> Values => _values ?? (_values = new List<TomlNode>());

        public void Add(TomlNode node)
        {
            Values.Add(node);
        }
    }

    public class TomlTable : TomlNode
    {
        public override bool HasValue { get; } = false;
        public override bool IsTable { get; } = true;
    }

    #endregion

    public static class TOML
    {
        public static TomlNode Parse(TextReader reader)
        {
            var rootNode = new TomlNode();

            var currentNode = rootNode;

            var state = ParseState.None;

            var keyParts = new List<string>();

            var insideArrayTable = false;
            var arrayTable = false;

            int currentChar;
            while ((currentChar = reader.Peek()) >= 0)
            {
                var c = (char) currentChar;

                if (state == ParseState.None)
                {
                    // Skip white space
                    if (IsWhiteSpace(c) || IsNewLine(c))
                        goto consume_character;

                    // Start of a comment; ignore until newline
                    if (c == COMMENT_SYMBOL)
                    {
                        reader.ReadLine();
                        continue;
                    }

                    if (c == TABLE_START_SYMBOL)
                    {
                        state = ParseState.Table;
                        goto consume_character;
                    }

                    if (IsBareKey(c) || IsQuoted(c))
                        state = ParseState.KeyValuePair;
                    else
                        throw new Exception($"Unexpected character \"{c}\"");
                }

                if (state == ParseState.KeyValuePair)
                {
                    var keyValuePair = ReadKeyValuePair(reader, keyParts);
                    InsertNode(keyValuePair, currentNode, keyParts);
                    keyParts.Clear();
                    state = ParseState.SkipToNextLine;
                    continue;
                }

                if (state == ParseState.Table)
                {
                    if (keyParts.Count == 0)
                    {
                        // We have array table
                        if (c == TABLE_START_SYMBOL)
                        {
                            // Consume the character
                            reader.Read();
                            arrayTable = true;
                        }

                        ReadKeyName(reader, ref keyParts, TABLE_END_SYMBOL, true);
                        if (keyParts.Count == 0)
                            throw new Exception("The table key is empty!");

                        continue;
                    }

                    if (c == TABLE_END_SYMBOL)
                    {
                        if (arrayTable)
                        {
                            if (reader.Peek() < 0 || (char) reader.Peek() != TABLE_END_SYMBOL)
                                throw new Exception("The array table is not closed!");
                            // Consume the extra closing table symbol
                            reader.Read();
                        }

                        currentNode = CreateTable(rootNode, keyParts, arrayTable);
                        keyParts.Clear();
                        arrayTable = false;
                        state = ParseState.SkipToNextLine;
                        goto consume_character;
                    }

                    if (keyParts.Count != 0)
                        throw new Exception("Encountered unexpected character in table definition!");
                }

                if (state == ParseState.SkipToNextLine)
                {
                    if (IsWhiteSpace(c) || c == NEWLINE_CARRIAGE_RETURN_CHARACTER)
                        goto consume_character;

                    if (c == COMMENT_SYMBOL || c == NEWLINE_CHARACTER)
                    {
                        state = ParseState.None;
                        if (c == COMMENT_SYMBOL)
                        {
                            reader.ReadLine();
                            continue;
                        }

                        goto consume_character;
                    }

                    throw new Exception("Unexpected symbol after the parsed content");
                }

                consume_character:
                reader.Read();
            }

            if(state != ParseState.None && state != ParseState.SkipToNextLine)
                throw new Exception("Unexpected end of file!");

            return rootNode;
        }


        private enum ParseState
        {
            None,
            KeyValuePair,
            SkipToNextLine,
            Table
        }

        #region Character Definitions

        private const char ARRAY_END_SYMBOL = ']';
        private const char ITEM_SEPARATOR = ',';
        private const char ARRAY_START_SYMBOL = '[';
        private const char BASIC_STRING_SYMBOL = '\"';
        private const char COMMENT_SYMBOL = '#';
        private const char ESCAPE_SYMBOL = '\\';
        private const char KEY_VALUE_SEPARATOR = '=';
        private const char NEWLINE_CARRIAGE_RETURN_CHARACTER = '\r';
        private const char NEWLINE_CHARACTER = '\n';
        private const char SUBKEY_SEPARATOR = '.';
        private const char TABLE_END_SYMBOL = ']';
        private const char TABLE_START_SYMBOL = '[';
        private const char INLINE_TABLE_START_SYMBOL = '{';
        private const char INLINE_TABLE_END_SYMBOL = '}';


        private static bool IsQuoted(char c) => c == BASIC_STRING_SYMBOL || c == '\'';

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        private static bool IsNewLine(char c) => c == NEWLINE_CHARACTER || c == NEWLINE_CARRIAGE_RETURN_CHARACTER;

        private static bool IsEmptySpace(char c) => IsWhiteSpace(c) || IsNewLine(c);

        private static bool IsBareKey(char c) =>
            'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z' || '0' <= c && c <= '9' || c == '_' || c == '-';

        private static bool ShouldBeEscaped(char c) => c <= '\u001f' || c == '\u007f';

        #endregion

        #region Type Patterns

        private static readonly Regex IntegerPattern =
            new Regex(@"^(\+|-)?(?!_)(0|(?!0)(_?\d)*)$", RegexOptions.Compiled);

        private static readonly Regex BasedIntegerPattern =
            new Regex(@"^(\+|-)?0(?<base>x|b|o)(?!_)(_?[0-9A-F])*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FloatPattern =
            new Regex(@"^(\+|-)?(?!_)(0|(?!0)(_?\d)+)(((e(\+|-)?(?!_)(_?\d)+)?)|(\.(?!_)(_?\d)+(e(\+|-)?(?!_)(_?\d)+)?))$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, int> bases = new Dictionary<string, int>
        {
            ["x"] = 16,
            ["o"] = 8,
            ["b"] = 2
        };

        private static readonly string[] RFC3339Formats =
        {
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mm:ss.fK",
            "yyyy-MM-dd HH:mm:ss.ffK",
            "yyyy-MM-dd HH:mm:ss.fffK",
            "yyyy-MM-dd HH:mm:ss.ffffK",
            "yyyy-MM-dd HH:mm:ss.fffffK",
            "yyyy-MM-dd HH:mm:ss.ffffffK",
            "yyyy-MM-dd HH:mm:ss.fffffffK"
        };

        private static readonly string[] RFC3339LocalDateTimeFormats =
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.f",
            "yyyy-MM-dd HH:mm:ss.ff",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss.ffff",
            "yyyy-MM-dd HH:mm:ss.fffff",
            "yyyy-MM-dd HH:mm:ss.ffffff",
            "yyyy-MM-dd HH:mm:ss.fffffff"
        };

        private static readonly string LocalDateFormat = "yyyy-MM-dd";

        private static readonly string[] RFC3339LocalTimeFormats =
        {
            "HH:mm:ss",
            "HH:mm:ss.f",
            "HH:mm:ss.ff",
            "HH:mm:ss.fff",
            "HH:mm:ss.ffff",
            "HH:mm:ss.fffff",
            "HH:mm:ss.ffffff",
            "HH:mm:ss.fffffff"
        };

        #endregion

        #region Key-Value pair parsing

        /// <summary>
        ///     Reads a single key-value pair.
        /// </summary>
        /// <remarks>
        ///     The method assumes the cursor is at the start of the key:
        ///     foo.bar = "value"
        ///     ^
        ///     The method consumes all the characters that belong to the key-value-pair:
        ///     foo.bar = "value"
        ///     ^
        /// </remarks>
        /// <param name="reader"></param>
        /// <param name="keyParts"></param>
        /// <returns></returns>
        private static TomlNode ReadKeyValuePair(TextReader reader, List<string> keyParts)
        {
            int cur;
            while ((cur = reader.Peek()) >= 0)
            {
                var c = (char) cur;

                if (IsQuoted(c) || IsBareKey(c))
                {
                    if (keyParts.Count != 0)
                        throw new Exception("Encountered extra characters in key definition!");

                    ReadKeyName(reader, ref keyParts, KEY_VALUE_SEPARATOR);
                    continue;
                }

                if (IsWhiteSpace(c))
                {
                    reader.Read();
                    continue;
                }

                if (c == KEY_VALUE_SEPARATOR)
                {
                    reader.Read();
                    return ReadValue(reader);
                }

                throw new Exception("Invalid character in key!");
            }

            return null;
        }

        /// <summary>
        ///     Reads a single value.
        /// </summary>
        /// <remarks>
        ///     Assumes the cursor is at the start of the value (or whitespace before the value):
        ///     "test"
        ///     ^
        ///     The method consumes all characters that belong to the value:
        ///     "test"
        ///     ^
        /// </remarks>
        /// <param name="reader"></param>
        /// <param name="skipNewlines"></param>
        /// <returns></returns>
        private static TomlNode ReadValue(TextReader reader, bool skipNewlines = false)
        {
            int cur;
            while ((cur = reader.Peek()) >= 0)
            {
                var c = (char) cur;

                if (IsWhiteSpace(c))
                {
                    reader.Read();
                    continue;
                }

                if (c == COMMENT_SYMBOL)
                    throw new Exception("No value found!");

                if (IsNewLine(c))
                {
                    if (skipNewlines)
                    {
                        reader.Read();
                        continue;
                    }

                    throw new Exception("Encountered a newline when expecting a value!");
                }

                if (IsQuoted(c))
                {
                    var value = IsTripleQuote(c, reader, out var excess)
                        ? ReadQuotedValueMultiLine(c, reader)
                        : ReadQuotedValueSingleLine(c, reader, excess);

                    return new TomlString
                    {
                        Value = value
                    };
                }

                if (c == INLINE_TABLE_START_SYMBOL)
                    return ReadInlineTable(reader);

                if (c == ARRAY_START_SYMBOL)
                    return ReadArray(reader);

                return ReadTomlValue(reader);
            }

            return null;
        }

        /// <summary>
        ///     Reads the name of the key (either table key or value key).
        /// </summary>
        /// <remarks>
        ///     Assumes the cursor is at the first value in the key name:
        ///     foo.bar
        ///     ^
        ///     The method consumes all of the key so that the cursor position is after the last character:
        ///     foo.bar
        ///     ^
        /// </remarks>
        /// <param name="reader"></param>
        /// <param name="parts"></param>
        /// <param name="until"></param>
        /// <param name="skipWhitespace"></param>
        private static void ReadKeyName(TextReader reader,
                                        ref List<string> parts,
                                        char until,
                                        bool skipWhitespace = false)
        {
            var buffer = new StringBuilder();
            var quoted = false;
            bool prevWasSpace = false;
            int cur;
            while ((cur = reader.Peek()) >= 0)
            {
                var c = (char) cur;

                // Reached the final character
                if (c == until)
                    break;

                if (IsWhiteSpace(c))
                    if (skipWhitespace)
                    {
                        prevWasSpace = true;
                        goto consume_character;
                    }
                    else
                        break;

                if (c == SUBKEY_SEPARATOR)
                {
                    if (buffer.Length == 0)
                        throw new Exception("No subkey identified!");

                    parts.Add(buffer.ToString());
                    buffer.Length = 0;
                    quoted = false;
                    prevWasSpace = false;
                    goto consume_character;
                }

                if (prevWasSpace)
                    throw new Exception("Invalid spacing in key name!");

                if (IsQuoted(c))
                {
                    if (quoted)
                        throw new Exception("Expected a subkey separator but got extra data instead!");
                    if (buffer.Length != 0)
                        throw new Exception("Encountered a premature quote!");

                    // Consume the quote character and read the key name
                    buffer.Append(ReadQuotedValueSingleLine((char) reader.Read(), reader));
                    quoted = true;
                    continue;
                }

                if (IsBareKey(c))
                {
                    buffer.Append(c);
                    goto consume_character;
                }

                // If we see an invalid symbol, let the next parser handle it
                break;

                consume_character:
                reader.Read();
            }

            if (buffer.Length == 0)
                throw new Exception("Encountered extra . in key definition!");

            parts.Add(buffer.ToString());
        }

        #endregion

        #region Non-string value parsing

        private static bool IsValueSeparator(char c) =>
            c == ITEM_SEPARATOR || c == ARRAY_END_SYMBOL || c == ARRAY_START_SYMBOL;

        private static string ReadRawValue(TextReader reader)
        {
            var result = new StringBuilder();

            int cur;
            while ((cur = reader.Peek()) >= 0)
            {
                var c = (char) cur;

                if (c == COMMENT_SYMBOL || IsNewLine(c) || IsValueSeparator(c))
                    break;

                result.Append(c);

                reader.Read();
            }

            // TODO: Replace trim with space counting
            return result.ToString().Trim();
        }

        private static TomlNode ReadTomlValue(TextReader reader)
        {
            var value = ReadRawValue(reader);

            if (value == "false" || value == "true")
                return bool.Parse(value);

            if (value == "nan" || value == "+nan" || value == "-nan")
                return float.NaN;

            if (value == "inf" || value == "+inf")
                return float.PositiveInfinity;

            if (value == "-inf")
                return float.NegativeInfinity;

            if (IntegerPattern.IsMatch(value))
                return long.Parse(value.Replace("_", ""), CultureInfo.InvariantCulture);

            if (FloatPattern.IsMatch(value))
                return float.Parse(value.Replace("_", ""), CultureInfo.InvariantCulture);

            var match = BasedIntegerPattern.Match(value);
            if (match.Success)
            {
                var numBase = bases.TryGetValue(match.Groups["base"].Value, out var val) ? val : 10;
                return Convert.ToInt64(value.Substring(2).Replace("_", ""), numBase);
            }

            value = value.Replace("T", " ");
            if (DateTime.TryParseExact(value,
                                       RFC3339Formats,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeLocal,
                                       out var dateTimeResult))
                return dateTimeResult;

            if (DateTime.TryParseExact(value,
                                       RFC3339LocalDateTimeFormats,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeLocal,
                                       out dateTimeResult))
                return dateTimeResult;

            if (DateTime.TryParseExact(value,
                                       LocalDateFormat,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeLocal,
                                       out dateTimeResult))
                return dateTimeResult;

            if (DateTime.TryParseExact(value,
                                       RFC3339LocalTimeFormats,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeLocal,
                                       out dateTimeResult))
                return dateTimeResult;

            throw new Exception("Invalid value!");
        }

        /// <summary>
        ///     Reads the array.
        /// </summary>
        /// <remarks>
        ///     Assumes the cursor is at the start of the array:
        ///     ["a", "b"]
        ///     ^
        ///     Consumes all characters until the end of the array:
        ///     ["a", "b"]
        ///     ^
        /// </remarks>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static TomlArray ReadArray(TextReader reader)
        {
            // Consume the start of array character
            reader.Read();

            var result = new TomlArray();

            TomlNode currentValue = null;

            int cur;
            while ((cur = reader.Peek()) >= 0)
            {
                var c = (char) cur;

                if (c == ARRAY_END_SYMBOL)
                {
                    reader.Read();
                    break;
                }

                if (c == COMMENT_SYMBOL)
                {
                    reader.ReadLine();
                    continue;
                }

                if (IsWhiteSpace(c) || IsNewLine(c))
                    goto consume_character;

                if (c == ITEM_SEPARATOR)
                {
                    if (currentValue == null)
                        throw new Exception("Encountered multiple value separators!");

                    result.Add(currentValue);
                    currentValue = null;
                    goto consume_character;
                }

                currentValue = ReadValue(reader, true);

                if (result.Values.Count != 0 && result[0].GetType() != currentValue.GetType())
                    throw new Exception("Arrays cannot have mixed types!");

                continue;

                consume_character:
                reader.Read();
            }

            if (currentValue != null)
                result.Add(currentValue);

            return result;
        }

        private static TomlNode ReadInlineTable(TextReader reader)
        {
            reader.Read();

            var result = new TomlTable();

            TomlNode currentValue = null;

            var keyParts = new List<string>();

            int cur;
            while ((cur = reader.Peek()) >= 0)
            {
                var c = (char) cur;

                if (c == INLINE_TABLE_END_SYMBOL)
                {
                    reader.Read();
                    break;
                }

                if (c == COMMENT_SYMBOL)
                    throw new Exception("Incomplete inline table definition");

                if (IsNewLine(c))
                    throw new Exception("Inline tables are only allowed to be on single line");

                if (IsWhiteSpace(c))
                    goto consume_character;

                if (c == ITEM_SEPARATOR)
                {
                    if (currentValue == null)
                        throw new Exception("Encountered multiple value separators!");

                    InsertNode(currentValue, result, keyParts);
                    keyParts.Clear();
                    currentValue = null;
                    goto consume_character;
                }

                currentValue = ReadKeyValuePair(reader, keyParts);
                continue;

                consume_character:
                reader.Read();
            }

            if (currentValue != null)
                InsertNode(currentValue, result, keyParts);

            return result;
        }

        #endregion

        #region String parsing

        /// <summary>
        ///     Checks whether the quote is triple quote.
        /// </summary>
        /// <remarks>
        ///     Assumes the cursor is at the first quote character:
        ///     """
        ///     ^
        ///     Consumes either one character (if not triple quote) or three characters (if triple quote)
        ///     """
        ///     ^
        /// </remarks>
        /// <param name="quote"></param>
        /// <param name="reader"></param>
        /// <param name="excess"></param>
        /// <returns></returns>
        private static bool IsTripleQuote(char quote, TextReader reader, out char excess)
        {
            // Copypasta, but it's faster...

            int cur;
            // Consume the first quote
            reader.Read();

            if ((cur = reader.Peek()) < 0)
                throw new Exception("Unexpected end of file!");

            if ((char) cur != quote)
            {
                excess = '\0';
                return false;
            }

            // Consume the second quote
            reader.Read();

            if ((cur = reader.Peek()) < 0)
                throw new Exception("Unexpected end of file!");

            if ((char) cur != quote)
            {
                excess = (char) cur;
                return false;
            }

            // Consume the final quote
            reader.Read();

            excess = '\0';
            return true;
        }

        private static bool ProcessQuotedValueCharacter(char quote,
                                                        bool isBasic,
                                                        char c,
                                                        int next,
                                                        StringBuilder sb,
                                                        ref bool escaped)
        {
            if(ShouldBeEscaped(c))
                throw new Exception($"The character U+{(int)c:X8} must be escaped!");

            if (escaped)
            {
                sb.Append(c);
                escaped = false;
                return false;
            }

            if (c == quote)
                return true;

            if (isBasic && c == ESCAPE_SYMBOL)
                if (next >= 0 && (char) next == quote)
                    escaped = true;

            if (c == NEWLINE_CHARACTER)
                throw new Exception("Encountered newline in single line quote");

            sb.Append(c);
            return false;
        }

        /// <summary>
        ///     Reads a single-line string from the string.
        /// </summary>
        /// <remarks>
        ///     Assumes the next available character is at the string contents:
        ///     "test"
        ///     ^
        ///     (possibly with initial data)
        ///     The method consumes the whole string along with the closing quote:
        ///     "test"
        ///     ^
        /// </remarks>
        /// <param name="reader"></param>
        /// <param name="initialData"></param>
        /// <returns></returns>
        private static string ReadQuotedValueSingleLine(char quote, TextReader reader, char initialData = '\0')
        {
            var isBasic = quote == BASIC_STRING_SYMBOL;
            var sb = new StringBuilder();

            var escaped = false;

            if (initialData != '\0' &&
                ProcessQuotedValueCharacter(quote, isBasic, initialData, reader.Peek(), sb, ref escaped))
                return isBasic ? sb.ToString().Unescape() : sb.ToString();

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                var c = (char) cur;
                if (ProcessQuotedValueCharacter(quote, isBasic, c, reader.Peek(), sb, ref escaped))
                    break;
            }

            return isBasic ? sb.ToString().Unescape() : sb.ToString();
        }

        /// <summary>
        ///     Reads a multiline string.
        /// </summary>
        /// <remarks>
        ///     Assumes the cursor is at the *first value* that belongs to the string:
        ///     """test"""
        ///     ^
        ///     Consumes the whole string along with ending quotes:
        ///     """test"""
        ///     ^
        /// </remarks>
        /// <param name="quote"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static string ReadQuotedValueMultiLine(char quote, TextReader reader)
        {
            var isBasic = quote == BASIC_STRING_SYMBOL;
            var sb = new StringBuilder();

            var escaped = false;
            var skipWhitespace = false;
            var quotesEncountered = 0;
            var first = true;

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                var c = (char) cur;

                if (ShouldBeEscaped(c))
                    throw new Exception($"The character U+{(int)c:X8} must be escaped!");

                // Trim the first newline
                if (first && IsNewLine(c))
                {
                    if (c != NEWLINE_CARRIAGE_RETURN_CHARACTER)
                        first = false;
                    continue;
                }

                first = false;

                //TODO: Reuse ProcessQuotedValueCharacter

                // Skip the current character if it is going to be escaped later
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                // If we are currently skipping empty spaces, skip
                if (skipWhitespace)
                {
                    if (IsEmptySpace(c))
                        continue;
                    skipWhitespace = false;
                }

                // If we encounter an escape sequence...
                if (isBasic && c == ESCAPE_SYMBOL)
                {
                    var next = reader.Peek();
                    if (next >= 0)
                    {
                        // ...and the next char is empty space, we must skip all whitespaces
                        if (IsEmptySpace((char) next))
                        {
                            skipWhitespace = true;
                            continue;
                        }

                        // ...and we have \", skip the character
                        if ((char) next == quote)
                            escaped = true;
                    }
                }

                // Count the consecutive quotes
                if (c == quote)
                    quotesEncountered++;
                else
                    quotesEncountered = 0;

                // If the are three quotes, count them as closing quotes
                if (quotesEncountered == 3)
                    break;

                sb.Append(c);
            }

            // Remove last two quotes (third one wasn't included by default
            sb.Length -= 2;

            return isBasic ? sb.ToString().Unescape() : sb.ToString();
        }

        #endregion

        #region Node creation

        private static void InsertNode(TomlNode node, TomlNode root, List<string> path)
        {
            var latestNode = root;

            if (path.Count > 1)
                for (var index = 0; index < path.Count - 1; index++)
                {
                    var subkey = path[index];
                    if (latestNode.Children.TryGetValue(subkey, out var currentNode))
                    {
                        if (currentNode.HasValue)
                            throw new Exception("The key already has a value assigned to it!");
                        if (currentNode.IsTable)
                            throw new Exception("The key is a table and thus is not a valid value");
                    }
                    else
                    {
                        currentNode = new TomlNode();
                        latestNode[subkey] = currentNode;
                    }

                    latestNode = currentNode;
                }

            if(latestNode.Children.ContainsKey(path[path.Count - 1]))
                throw new Exception("The same key is already defined!");

            latestNode[path[path.Count - 1]] = node;
        }

        private static TomlTable CreateTable(TomlNode root, List<string> path, bool arrayTable)
        {
            if (path.Count == 0)
                return null;

            var latestNode = root;

            for (var index = 0; index < path.Count; index++)
            {
                var subkey = path[index];
                if (latestNode.Children.TryGetValue(subkey, out var node))
                {
                    if (node.IsArray && arrayTable)
                    {
                        var arr = (TomlArray) node;

                        if (!arr.IsArrayTable)
                            throw new Exception("The array was defined as a key-value pair!");

                        if (index == path.Count - 1)
                        {
                            latestNode = new TomlTable();
                            arr.Add(latestNode);
                            break;
                        }

                        latestNode = arr[arr.Values.Count - 1];
                        continue;
                    }

                    if (node.HasValue)
                        throw new Exception("The key has a value assigned to it!");

                    if (index == path.Count - 1)
                        throw new Exception("The table has been already defined previously!");
                }
                else
                {
                    if (index == path.Count - 1 && arrayTable)
                    {
                        var table = new TomlTable();
                        var arr = new TomlArray { IsArrayTable = true };
                        arr.Add(table);
                        latestNode[subkey] = arr;
                        latestNode = table;
                        break;
                    }

                    node = index == path.Count - 1 ? new TomlTable() : new TomlNode();
                    latestNode[subkey] = node;
                }

                latestNode = node;
            }

            return (TomlTable) latestNode;
        }

        #endregion
    }

    #region Parse utilities

    internal static class ParseUtils
    {
        public static string Unescape(this string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return txt;
            var stringBuilder = new StringBuilder(txt.Length);
            for (var i = 0; i < txt.Length;)
            {
                var num = txt.IndexOf('\\', i);
                if (num < 0 || num == txt.Length - 1)
                    num = txt.Length;
                stringBuilder.Append(txt, i, num - i);
                if (num >= txt.Length)
                    break;
                var c = txt[num + 1];
                switch (c)
                {
                    case 'b':
                        stringBuilder.Append('\b');
                        break;
                    case 't':
                        stringBuilder.Append('\t');
                        break;
                    case 'n':
                        stringBuilder.Append('\n');
                        break;
                    case 'f':
                        stringBuilder.Append('\f');
                        break;
                    case 'r':
                        stringBuilder.Append('\r');
                        break;
                    case '\'':
                        stringBuilder.Append('\'');
                        break;
                    case '\"':
                        stringBuilder.Append('\"');
                        break;
                    case '\\':
                        stringBuilder.Append('\\');
                        break;
                    default:
                        // TODO: Add Unicode codepoint support
                        throw new Exception("Undefined escape sequence!");
                        break;
                }

                i = num + 2;
            }

            return stringBuilder.ToString();
        }
    }

    #endregion
}