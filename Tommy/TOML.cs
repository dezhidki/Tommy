using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tommy
{
    public class TomlNode
    {
        public string Key { get; set; }

        public string RawValue { get; set; }

        public Dictionary<string, TomlNode> Children { get; } = new Dictionary<string, TomlNode>();
    }

    public static class TOML
    {
        private const char COMMENT_SYMBOL = '#';
        private const char KEY_VALUE_SEPARATOR = '=';
        private const char NEWLINE_CHARACTER = '\n';
        private const char NEWLINE_CARRIAGE_RETURN_CHARACTER = '\r';
        private const char SUBKEY_SEPARATOR = '.';
        private const char ESCAPE_SYMBOL = '\\';
        private const char BASIC_STRING_SYMBOL = '\"';

        enum ParseState
        {
            None,
            Key,
            Value
        }

        private static bool IsQuoted(char c) => c == BASIC_STRING_SYMBOL || c == '\'';

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        private static bool IsNewLine(char c) => c == NEWLINE_CHARACTER || c == NEWLINE_CARRIAGE_RETURN_CHARACTER;

        private static bool IsEmptySpace(char c) => IsWhiteSpace(c) || IsNewLine(c);

        private static bool IsBareKey(char c) => 'A' <= c && c <= 'Z' || 
                                                 'a' <= c && c <= 'z' || 
                                                 '0' <= c && c <= '9' ||
                                                 c == '_' || c == '-';

        private static bool IsTripleQuote(char quote, TextReader reader, out string excess)
        {
            char[] buffer = new char[2];
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
                return false;
            }

            if (c == quote)
                return true;

            if (isBasic && c == ESCAPE_SYMBOL)
            {
                // Don't stop if we encounter \" while parsing a basic string
                if (next >= 0 && (char)next == quote)
                    escaped = true;
            }

            if (c == NEWLINE_CHARACTER)
                throw new Exception("Encountered newline in single line quote");

            sb.Append(c);
            return false;
        }

        private static string ReadQuotedValueSingleLine(char quote, TextReader reader, string initialData)
        {
            bool isBasic = quote == BASIC_STRING_SYMBOL;
            StringBuilder sb = new StringBuilder();

            bool escaped = false;

            // Catch up with possible initial data
            for (var i = 0; i < initialData.Length; i++)
                if (ProcessQuotedValueCharacter(quote, isBasic, initialData[i],
                    i < initialData.Length - 1 ? initialData[i + 1] : -1, sb, ref escaped))
                {
                    return isBasic ? sb.ToString().Unescape() : sb.ToString();
                }

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
            StringBuilder sb = new StringBuilder();

            bool escaped = false;
            bool skipWhitespace = false;
            int quotesEncountered = 0;

            int cur;
            while ((cur = reader.Read()) >= 0)
            {
                char c = (char) cur;

                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (skipWhitespace)
                {
                    if (IsWhiteSpace(c))
                        continue;
                    skipWhitespace = false;
                }

                if (c == ESCAPE_SYMBOL)
                {
                    int next = reader.Peek();
                    if (next >= 0)
                    {
                        if (IsWhiteSpace((char) next))
                        {
                            skipWhitespace = true;
                            continue;
                        }

                        if (isBasic && (char) next == quote)
                            escaped = true;
                    }
                }

                if (c == quote)
                    quotesEncountered++;
                else
                    quotesEncountered = 0;

                if (quotesEncountered == 3)
                    break;

                sb.Append(c);
            }

            // Remove last three quotes
            sb.Length -= 3;

            return isBasic ? sb.ToString().Unescape() : sb.ToString();
        }

        public static TomlNode Parse(TextReader reader)
        {
            TomlNode result = new TomlNode();

            TomlNode currentNode = result;

            ParseState state = ParseState.None;

            StringBuilder buffer = new StringBuilder();
            string key = string.Empty;

            int currentChar;
            while ((currentChar = reader.Read()) >= 0)
            {
                char c = (char) currentChar;

                if (state == ParseState.None)
                {
                    // Skip white space
                    if(IsWhiteSpace(c) || IsNewLine(c))
                        continue;
                    
                    // Start of a comment; ignore until newline
                    if (c == COMMENT_SYMBOL)
                    {
                        reader.ReadLine();
                        continue;
                    }

                    //TODO: Section

                    if (IsBareKey(c))
                    {
                        state = ParseState.Key;
                        buffer.Append(c);
                        continue;
                    }

                    throw new Exception($"Unexpected character \"{c}\"");
                } else if (state == ParseState.Key)
                {
                    // TODO: Subkey

                    if (IsQuoted(c))
                    {
                        //TODO: Quoted key
                    }

                    if (c == SUBKEY_SEPARATOR)
                    {
                        //TODO: Separator
                    }

                    if (IsBareKey(c))
                    {
                        buffer.Append(c);
                        continue;
                    }

                    if(IsWhiteSpace(c))
                        continue;

                    if (c == KEY_VALUE_SEPARATOR)
                    {
                        state = ParseState.Value;

                        key = buffer.ToString();
                        buffer.Length = 0;

                        continue;
                    }

                    throw new Exception("Invalid character in key!");
                } else if (state == ParseState.Value)
                {
                    if(IsWhiteSpace(c))
                        continue;

                    if (IsQuoted(c))
                    {
                        var value = IsTripleQuote(c, reader, out var excess) ? ReadQuotedValueMultiLine(c, reader) : ReadQuotedValueSingleLine(c, reader, excess);
                        currentNode.Children[key] = new TomlNode
                        {
                            Key = key,
                            RawValue = value
                        };

                        key = string.Empty;
                    }

                    if(c == COMMENT_SYMBOL)
                        throw new Exception("The key has no value!");

                    state = ParseState.None;
                }
            }

            return result;
        }
    }

    static class ParseUtils
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
