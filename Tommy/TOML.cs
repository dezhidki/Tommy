using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tommy
{
    public class TomlNode
    {
        private string _rawValue;
        private Dictionary<string, TomlNode> children;
        public Dictionary<string, TomlNode> Children => children ?? (children = new Dictionary<string, TomlNode>());

        public bool HasValue { get; protected set; }

        public bool IsTable { get; protected set; }

        public TomlNode this[string key]
        {
            get => Children[key];
            set => Children[key] = value;
        }

        public string RawValue
        {
            get => _rawValue;
            set
            {
                HasValue = true;
                _rawValue = value;
            }
        }

        public static implicit operator TomlNode(string str) => new TomlNode {RawValue = str};
    }

    public class TomlTable : TomlNode
    {
        public TomlTable() => IsTable = true;
    }

    public static class TOML
    {
        private const char BASIC_STRING_SYMBOL = '\"';
        private const char COMMENT_SYMBOL = '#';
        private const char ESCAPE_SYMBOL = '\\';
        private const char KEY_VALUE_SEPARATOR = '=';
        private const char NEWLINE_CARRIAGE_RETURN_CHARACTER = '\r';
        private const char NEWLINE_CHARACTER = '\n';
        private const char SUBKEY_SEPARATOR = '.';

        public static TomlNode Parse(TextReader reader)
        {
            var result = new TomlNode();

            var currentNode = result;

            var state = ParseState.None;

            var keyParts = new List<string>();

            int currentChar;
            while ((currentChar = reader.Read()) >= 0)
            {
                char c = (char) currentChar;

                if (state == ParseState.None)
                {
                    // Skip white space
                    if (IsWhiteSpace(c) || IsNewLine(c))
                        continue;

                    // Start of a comment; ignore until newline
                    if (c == COMMENT_SYMBOL)
                    {
                        reader.ReadLine();
                        continue;
                    }

                    //TODO: Tables

                    //TODO: Array tables

                    if (IsBareKey(c) || IsQuoted(c))
                        state = ParseState.Key;
                    else
                        throw new Exception($"Unexpected character \"{c}\"");
                }

                if (state == ParseState.Key)
                {
                    if (IsQuoted(c) || IsBareKey(c))
                    {
                        if (keyParts.Count != 0)
                            throw new Exception("Encountered extra characters in key definition!");

                        ReadKeyName(reader, ref keyParts, c);
                        continue;
                    }

                    if (IsWhiteSpace(c))
                        continue;

                    if (c == KEY_VALUE_SEPARATOR)
                    {
                        state = ParseState.Value;
                        continue;
                    }

                    throw new Exception("Invalid character in key!");
                }

                if (state == ParseState.Value)
                {
                    if (IsWhiteSpace(c))
                        continue;

                    if (IsQuoted(c))
                    {
                        string value = IsTripleQuote(c, reader, out string excess)
                                               ? ReadQuotedValueMultiLine(c, reader)
                                               : ReadQuotedValueSingleLine(c, reader, excess);

                        var node = CreateValueNode(currentNode, keyParts);
                        node.RawValue = value;
                        keyParts.Clear();
                    }

                    // TODO: Numbers, Dates, Booleans, Lists, Inline Tables

                    if (c == COMMENT_SYMBOL)
                        throw new Exception("The key has no value!");

                    state = ParseState.None;
                }
            }

            return result;
        }

        private static bool IsQuoted(char c) => c == BASIC_STRING_SYMBOL || c == '\'';

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        private static bool IsNewLine(char c) => c == NEWLINE_CHARACTER || c == NEWLINE_CARRIAGE_RETURN_CHARACTER;

        private static bool IsEmptySpace(char c) => IsWhiteSpace(c) || IsNewLine(c);

        private static bool IsBareKey(char c) =>
                'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z' || '0' <= c && c <= '9' || c == '_' || c == '-';

        private static bool IsTripleQuote(char quote, TextReader reader, out string excess)
        {
            var buffer = new char[2];
            int read = reader.ReadBlock(buffer, 0, 2);

            if (read == 2 && buffer[0] == quote && buffer[1] == quote)
            {
                excess = null;
                return true;
            }

            excess = new string(buffer);
            return false;
        }

        private static bool ProcessQuotedValueCharacter(char quote, bool isBasic, char c, int next, StringBuilder sb, ref bool escaped)
        {
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

        private static void ReadKeyName(TextReader tr, ref List<string> parts, char firstChar)
        {
            var buffer = new StringBuilder();

            bool quoted = IsQuoted(firstChar);

            if (quoted)
                buffer.Append(ReadQuotedValueSingleLine(firstChar, tr, null));
            else
                buffer.Append(firstChar);

            int cur;
            while ((cur = tr.Read()) >= 0)
            {
                char c = (char) cur;

                // Stop if we see whitespace in non-quoted context; let main parser cause the possible error
                if (IsWhiteSpace(c) || c == KEY_VALUE_SEPARATOR)
                    break;

                if (c == SUBKEY_SEPARATOR)
                {
                    if (buffer.Length == 0)
                        throw new Exception("No subkey identified!");

                    parts.Add(buffer.ToString());
                    buffer.Length = 0;
                    quoted = false;
                    continue;
                }

                if (IsQuoted(c))
                {
                    if (quoted)
                        throw new Exception("Expected a subkey separator but got extra data instead!");
                    if (buffer.Length != 0)
                        throw new Exception("Encountered a premature quote!");

                    buffer.Append(ReadQuotedValueSingleLine(c, tr, null));
                    quoted = true;
                    continue;
                }

                if (IsBareKey(c))
                {
                    buffer.Append(c);
                    continue;
                }

                throw new Exception("Encountered an invalid symbol in key definition!");
            }

            parts.Add(buffer.ToString());
        }

        private static string ReadQuotedValueSingleLine(char quote, TextReader reader, string initialData)
        {
            bool isBasic = quote == BASIC_STRING_SYMBOL;
            var sb = new StringBuilder();

            bool escaped = false;

            // Catch up with possible initial data
            if (initialData != null)
                for (int i = 0; i < initialData.Length; i++)
                    if (ProcessQuotedValueCharacter(quote,
                                                    isBasic,
                                                    initialData[i],
                                                    i < initialData.Length - 1 ? initialData[i + 1] : -1,
                                                    sb,
                                                    ref escaped))
                        return isBasic ? sb.ToString().Unescape() : sb.ToString();

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                char c = (char) cur;
                if (ProcessQuotedValueCharacter(quote, isBasic, c, reader.Peek(), sb, ref escaped))
                    break;
            }

            return isBasic ? sb.ToString().Unescape() : sb.ToString();
        }

        private static string ReadQuotedValueMultiLine(char quote, TextReader reader)
        {
            bool isBasic = quote == BASIC_STRING_SYMBOL;
            var sb = new StringBuilder();

            bool escaped = false;
            bool skipWhitespace = false;
            int quotesEncountered = 0;
            bool first = true;

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                char c = (char) cur;

                // Trim the first newline
                if (first && IsNewLine(c))
                {
                    if (c != NEWLINE_CARRIAGE_RETURN_CHARACTER)
                        first = false;
                    continue;
                }

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
                if (c == ESCAPE_SYMBOL)
                {
                    int next = reader.Peek();
                    if (next >= 0)
                    {
                        // ...and the next char is empty space, we must skip all whitespaces
                        if (IsEmptySpace((char) next))
                        {
                            skipWhitespace = true;
                            continue;
                        }

                        // ...and we are in basic mode with \", skip the character
                        if (isBasic && (char) next == quote)
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

        private static TomlNode CreateValueNode(TomlNode root, List<string> path)
        {
            var latestNode = root;

            foreach (string subkey in path)
            {
                if (latestNode.Children.TryGetValue(subkey, out var node))
                {
                    if (node.HasValue)
                        throw new Exception("The key already has a value assigned to it!");
                    if (node.IsTable)
                        throw new Exception("The key is a table and thus is not a valid value");
                }
                else
                {
                    node = new TomlNode();
                    latestNode[subkey] = node;
                }

                latestNode = node;
            }

            return latestNode;
        }

        private enum ParseState
        {
            None,
            Key,
            Value
        }
    }

    internal static class ParseUtils
    {
        public static string Unescape(this string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return txt;
            var stringBuilder = new StringBuilder(txt.Length);
            for (int i = 0; i < txt.Length;)
            {
                int num = txt.IndexOf('\\', i);
                if (num < 0 || num == txt.Length - 1)
                    num = txt.Length;
                stringBuilder.Append(txt, i, num - i);
                if (num >= txt.Length)
                    break;
                char c = txt[num + 1];
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
}