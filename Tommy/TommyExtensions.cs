using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tommy
{
    /// <summary>
    /// Class of various extension methods for Tommy
    /// </summary>
    public static class TommyExtensions
    {
        /// <summary>
        /// Tries to parse TOML file.
        /// </summary>
        /// <param name="self">TOML parser to use.</param>
        /// <param name="rootNode">Parsed root node. If parsing fails, the parsed document might not contain all values.</param>
        /// <param name="errors">Parse errors, if any occur.</param>
        /// <returns>True, if parsing succeeded without errors. Otherwise false.</returns>
        public static bool TryParse(this TOMLParser self,
                                    out TomlNode rootNode,
                                    out IEnumerable<TomlSyntaxException> errors)
        {
            try
            {
                rootNode = self.Parse();
                errors = new List<TomlSyntaxException>();
                return true;
            }
            catch (TomlParseException ex)
            {
                rootNode = ex.ParsedNode;
                errors = ex.SyntaxErrors;
                return false;
            }
        }


        /// <summary>
        /// Gets node given a fully-keyed path to it.
        /// </summary>
        /// <param name="self">Node to start search from.</param>
        /// <param name="path">Full path to the target node. The path must follow the TOML format.</param>
        /// <returns>Found node. If no matching node is found, returns null.</returns>
        public static TomlNode FindNode(this TomlNode self, string path)
        {
            bool ProcessQuotedValueCharacter(char quote,
                                                    bool isNonLiteral,
                                                    char c,
                                                    int next,
                                                    StringBuilder sb,
                                                    ref bool escaped)
            {
                if (TomlSyntax.ShouldBeEscaped(c))
                    throw new Exception($"The character U+{(int)c:X8} must be escaped in a string!");

                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    return false;
                }

                if (c == quote) return true;

                if (isNonLiteral && c == TomlSyntax.ESCAPE_SYMBOL)
                    if (next >= 0 && (char)next == quote)
                        escaped = true;

                if (c == TomlSyntax.NEWLINE_CHARACTER)
                    throw new Exception("Encountered newline in single line string!");

                sb.Append(c);
                return false;
            }

            string ReadQuotedValueSingleLine(char quote, TextReader reader, char initialData = '\0')
            {
                var isNonLiteral = quote == TomlSyntax.BASIC_STRING_SYMBOL;
                var sb = new StringBuilder();

                var escaped = false;

                if (initialData != '\0' &&
                    ProcessQuotedValueCharacter(quote, isNonLiteral, initialData, reader.Peek(), sb, ref escaped))
                    return isNonLiteral ? sb.ToString().Unescape() : sb.ToString();

                int cur;
                while ((cur = reader.Read()) >= 0)
                {
                    var c = (char)cur;
                    if (ProcessQuotedValueCharacter(quote, isNonLiteral, c, reader.Peek(), sb, ref escaped)) break;
                }

                return isNonLiteral ? sb.ToString().Unescape() : sb.ToString();
            }

            void ReadKeyName(TextReader reader, List<string> parts)
            {
                var buffer = new StringBuilder();
                var quoted = false;
                int cur;
                while ((cur = reader.Peek()) >= 0)
                {
                    var c = (char)cur;

                    if (TomlSyntax.IsWhiteSpace(c))
                        break;

                    if (c == TomlSyntax.SUBKEY_SEPARATOR)
                    {
                        if (buffer.Length == 0)
                            throw new Exception($"Found an extra subkey separator in {".".Join(parts)}...");

                        parts.Add(buffer.ToString());
                        buffer.Length = 0;
                        quoted = false;
                        goto consume_character;
                    }

                    if (TomlSyntax.IsQuoted(c))
                    {
                        if (quoted)
                            throw new Exception("Expected a subkey separator but got extra data instead!");
                        if (buffer.Length != 0)
                            throw new Exception("Encountered a quote in the middle of subkey name!");

                        // Consume the quote character and read the key name
                        buffer.Append(ReadQuotedValueSingleLine((char)reader.Read(), reader));
                        quoted = true;
                        continue;
                    }

                    if (TomlSyntax.IsBareKey(c))
                    {
                        buffer.Append(c);
                        goto consume_character;
                    }

                    // If we see an invalid symbol, let the next parser handle it
                    throw new Exception($"Unexpected symbol {c}");

                    consume_character:
                    reader.Read();
                }

                if (buffer.Length == 0)
                    throw new Exception($"Found an extra subkey separator in {".".Join(parts)}...");

                parts.Add(buffer.ToString());
            }

            var pathParts = new List<string>();

            using(var sr = new StringReader(path))
                ReadKeyName(sr, pathParts);

            var curNode = self;

            foreach (var pathPart in pathParts)
            {
                if (!curNode.TryGetNode(pathPart, out var node))
                    return null;
                curNode = node;
            }

            return curNode;
        }
    }
}
