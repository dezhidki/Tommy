/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 * 
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 * 
 * Written by Bunny83 
 * 2012-06-09
 * 
 * Changelog now external. See Changelog.txt
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2012-2019 Markus Göbel (Bunny83)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * * * * */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SimpleJSON
{
    public enum JSONNodeType
    {
        Array = 1,
        Object = 2,
        String = 3,
        Number = 4,
        NullValue = 5,
        Boolean = 6,
        None = 7,
        Custom = 0xFF
    }

    public enum JSONTextMode
    {
        Compact,
        Indent
    }

    public abstract class JSONNode
    {
        [ThreadStatic] private static StringBuilder m_EscapeBuilder;

        internal static StringBuilder EscapeBuilder
        {
            get
            {
                if (m_EscapeBuilder == null)
                    m_EscapeBuilder = new StringBuilder();
                return m_EscapeBuilder;
            }
        }

        internal static string Escape(string aText)
        {
            var sb = EscapeBuilder;
            sb.Length = 0;
            if (sb.Capacity < aText.Length + aText.Length / 10)
                sb.Capacity = aText.Length + aText.Length / 10;
            foreach (var c in aText)
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    default:
                        if (c < ' ' || forceASCII && c > 127)
                        {
                            ushort val = c;
                            sb.Append("\\u").Append(val.ToString("X4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }

            var result = sb.ToString();
            sb.Length = 0;
            return result;
        }

        private static JSONNode ParseElement(string token, bool quoted)
        {
            if (quoted)
                return token;
            if (token.Length <= 5)
            {
                var tmp = token.ToLower();
                if (tmp == "false" || tmp == "true")
                    return tmp == "true";
                if (tmp == "null")
                    return JSONNull.CreateOrGet();
            }

            double val;
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                return val;
            return token;
        }

        public static JSONNode Parse(string aJSON)
        {
            var stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            var i = 0;
            var Token = new StringBuilder();
            var TokenName = "";
            var QuoteMode = false;
            var TokenIsQuoted = false;
            var HasNewlineChar = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }

                        stack.Push(new JSONObject());
                        if (ctx != null) ctx.Add(TokenName, stack.Peek());
                        TokenName = "";
                        Token.Length = 0;
                        ctx = stack.Peek();
                        HasNewlineChar = false;
                        break;

                    case '[':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }

                        stack.Push(new JSONArray());
                        if (ctx != null) ctx.Add(TokenName, stack.Peek());
                        TokenName = "";
                        Token.Length = 0;
                        ctx = stack.Peek();
                        HasNewlineChar = false;
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }

                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (Token.Length > 0 || TokenIsQuoted)
                            ctx.Add(TokenName, ParseElement(Token.ToString(), TokenIsQuoted));
                        if (ctx != null)
                            ctx.Inline = !HasNewlineChar;
                        TokenIsQuoted = false;
                        TokenName = "";
                        Token.Length = 0;
                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }

                        TokenName = Token.ToString();
                        Token.Length = 0;
                        TokenIsQuoted = false;
                        break;

                    case '"':
                        QuoteMode ^= true;
                        TokenIsQuoted |= QuoteMode;
                        break;

                    case ',':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }

                        if (Token.Length > 0 || TokenIsQuoted)
                            ctx.Add(TokenName, ParseElement(Token.ToString(), TokenIsQuoted));
                        TokenIsQuoted = false;
                        TokenName = "";
                        Token.Length = 0;
                        TokenIsQuoted = false;
                        break;

                    case '\r':
                    case '\n':
                        HasNewlineChar = true;
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token.Append(aJSON[i]);
                        break;

                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            var C = aJSON[i];
                            switch (C)
                            {
                                case 't':
                                    Token.Append('\t');
                                    break;
                                case 'r':
                                    Token.Append('\r');
                                    break;
                                case 'n':
                                    Token.Append('\n');
                                    break;
                                case 'b':
                                    Token.Append('\b');
                                    break;
                                case 'f':
                                    Token.Append('\f');
                                    break;
                                case 'u':
                                {
                                    var s = aJSON.Substring(i + 1, 4);
                                    Token.Append((char) int.Parse(
                                                                  s,
                                                                  NumberStyles.AllowHexSpecifier));
                                    i += 4;
                                    break;
                                }
                                default:
                                    Token.Append(C);
                                    break;
                            }
                        }

                        break;
                    case '/':
                        if (allowLineComments && !QuoteMode && i + 1 < aJSON.Length && aJSON[i + 1] == '/')
                        {
                            while (++i < aJSON.Length && aJSON[i] != '\n' && aJSON[i] != '\r') ;
                            break;
                        }

                        Token.Append(aJSON[i]);
                        break;
                    case '\uFEFF': // remove / ignore BOM (Byte Order Mark)
                        break;

                    default:
                        Token.Append(aJSON[i]);
                        break;
                }

                ++i;
            }

            if (QuoteMode) throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
            if (ctx == null)
                return ParseElement(Token.ToString(), TokenIsQuoted);
            return ctx;
        }

        #region Enumerators

        public struct Enumerator
        {
            private enum Type
            {
                None,
                Array,
                Object
            }

            private readonly Type type;
            private Dictionary<string, JSONNode>.Enumerator m_Object;
            private List<JSONNode>.Enumerator m_Array;
            public bool IsValid => type != Type.None;

            public Enumerator(List<JSONNode>.Enumerator aArrayEnum)
            {
                type = Type.Array;
                m_Object = default;
                m_Array = aArrayEnum;
            }

            public Enumerator(Dictionary<string, JSONNode>.Enumerator aDictEnum)
            {
                type = Type.Object;
                m_Object = aDictEnum;
                m_Array = default;
            }

            public KeyValuePair<string, JSONNode> Current
            {
                get
                {
                    if (type == Type.Array)
                        return new KeyValuePair<string, JSONNode>(string.Empty, m_Array.Current);
                    if (type == Type.Object)
                        return m_Object.Current;
                    return new KeyValuePair<string, JSONNode>(string.Empty, null);
                }
            }

            public bool MoveNext()
            {
                if (type == Type.Array)
                    return m_Array.MoveNext();
                if (type == Type.Object)
                    return m_Object.MoveNext();
                return false;
            }
        }

        public struct ValueEnumerator
        {
            private Enumerator m_Enumerator;
            public ValueEnumerator(List<JSONNode>.Enumerator aArrayEnum) : this(new Enumerator(aArrayEnum)) { }

            public ValueEnumerator(Dictionary<string, JSONNode>.Enumerator aDictEnum) :
                this(new Enumerator(aDictEnum)) { }

            public ValueEnumerator(Enumerator aEnumerator) => m_Enumerator = aEnumerator;
            public JSONNode Current => m_Enumerator.Current.Value;
            public bool MoveNext() => m_Enumerator.MoveNext();
            public ValueEnumerator GetEnumerator() => this;
        }

        public struct KeyEnumerator
        {
            private Enumerator m_Enumerator;
            public KeyEnumerator(List<JSONNode>.Enumerator aArrayEnum) : this(new Enumerator(aArrayEnum)) { }

            public KeyEnumerator(Dictionary<string, JSONNode>.Enumerator aDictEnum) :
                this(new Enumerator(aDictEnum)) { }

            public KeyEnumerator(Enumerator aEnumerator) => m_Enumerator = aEnumerator;
            public string Current => m_Enumerator.Current.Key;
            public bool MoveNext() => m_Enumerator.MoveNext();
            public KeyEnumerator GetEnumerator() => this;
        }

        public class LinqEnumerator : IEnumerator<KeyValuePair<string, JSONNode>>,
                                      IEnumerable<KeyValuePair<string, JSONNode>>
        {
            private Enumerator m_Enumerator;
            private JSONNode m_Node;

            internal LinqEnumerator(JSONNode aNode)
            {
                m_Node = aNode;
                if (m_Node != null)
                    m_Enumerator = m_Node.GetEnumerator();
            }

            public IEnumerator<KeyValuePair<string, JSONNode>> GetEnumerator() => new LinqEnumerator(m_Node);

            IEnumerator IEnumerable.GetEnumerator() => new LinqEnumerator(m_Node);

            public KeyValuePair<string, JSONNode> Current => m_Enumerator.Current;
            object IEnumerator.Current => m_Enumerator.Current;
            public bool MoveNext() => m_Enumerator.MoveNext();

            public void Dispose()
            {
                m_Node = null;
                m_Enumerator = new Enumerator();
            }

            public void Reset()
            {
                if (m_Node != null)
                    m_Enumerator = m_Node.GetEnumerator();
            }
        }

        #endregion Enumerators

        #region common interface

        public static bool forceASCII = false;       // Use Unicode by default
        public static bool longAsString = false;     // lazy creator creates a JSONString instead of JSONNumber
        public static bool allowLineComments = true; // allow "//"-style comments at the end of a line

        public abstract JSONNodeType Tag { get; }

        public virtual JSONNode this[int aIndex]
        {
            get => null;
            set { }
        }

        public virtual JSONNode this[string aKey]
        {
            get => null;
            set { }
        }

        public virtual string Value
        {
            get => "";
            set { }
        }

        public virtual int Count => 0;

        public virtual bool IsNumber => false;
        public virtual bool IsString => false;
        public virtual bool IsBoolean => false;
        public virtual bool IsNull => false;
        public virtual bool IsArray => false;
        public virtual bool IsObject => false;

        public virtual bool Inline
        {
            get => false;
            set { }
        }

        public virtual void Add(string aKey, JSONNode aItem) { }

        public virtual void Add(JSONNode aItem) => Add("", aItem);

        public virtual JSONNode Remove(string aKey) => null;

        public virtual JSONNode Remove(int aIndex) => null;

        public virtual JSONNode Remove(JSONNode aNode) => aNode;

        public virtual void Clear() { }

        public virtual JSONNode Clone() => null;

        public virtual IEnumerable<JSONNode> Children
        {
            get { yield break; }
        }

        public IEnumerable<JSONNode> DeepChildren
        {
            get
            {
                foreach (var C in Children)
                    foreach (var D in C.DeepChildren)
                        yield return D;
            }
        }

        public virtual bool HasKey(string aKey) => false;

        public virtual JSONNode GetValueOrDefault(string aKey, JSONNode aDefault) => aDefault;

        public override string ToString()
        {
            var sb = new StringBuilder();
            WriteToStringBuilder(sb, 0, 0, JSONTextMode.Compact);
            return sb.ToString();
        }

        public virtual string ToString(int aIndent)
        {
            var sb = new StringBuilder();
            WriteToStringBuilder(sb, 0, aIndent, JSONTextMode.Indent);
            return sb.ToString();
        }

        internal abstract void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode);

        public abstract Enumerator GetEnumerator();
        public IEnumerable<KeyValuePair<string, JSONNode>> Linq => new LinqEnumerator(this);
        public KeyEnumerator Keys => new(GetEnumerator());
        public ValueEnumerator Values => new(GetEnumerator());

        #endregion common interface

        #region typecasting properties

        public virtual double AsDouble
        {
            get
            {
                var v = 0.0;
                if (double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    return v;
                return 0.0;
            }
            set => Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public virtual int AsInt
        {
            get => (int) AsDouble;
            set => AsDouble = value;
        }

        public virtual float AsFloat
        {
            get => (float) AsDouble;
            set => AsDouble = value;
        }

        public virtual bool AsBool
        {
            get
            {
                var v = false;
                if (bool.TryParse(Value, out v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set => Value = value ? "true" : "false";
        }

        public virtual long AsLong
        {
            get
            {
                long val = 0;
                if (long.TryParse(Value, out val))
                    return val;
                return 0L;
            }
            set => Value = value.ToString();
        }

        public virtual ulong AsULong
        {
            get
            {
                ulong val = 0;
                if (ulong.TryParse(Value, out val))
                    return val;
                return 0;
            }
            set => Value = value.ToString();
        }

        public virtual JSONArray AsArray => this as JSONArray;

        public virtual JSONObject AsObject => this as JSONObject;

        #endregion typecasting properties

        #region operators

        public static implicit operator JSONNode(string s) =>
            s is null ? (JSONNode) JSONNull.CreateOrGet() : new JSONString(s);

        public static implicit operator string(JSONNode d) => d == null ? null : d.Value;

        public static implicit operator JSONNode(double n) => new JSONNumber(n);

        public static implicit operator double(JSONNode d) => d == null ? 0 : d.AsDouble;

        public static implicit operator JSONNode(float n) => new JSONNumber(n);

        public static implicit operator float(JSONNode d) => d == null ? 0 : d.AsFloat;

        public static implicit operator JSONNode(int n) => new JSONNumber(n);

        public static implicit operator int(JSONNode d) => d == null ? 0 : d.AsInt;

        public static implicit operator JSONNode(long n)
        {
            if (longAsString)
                return new JSONString(n.ToString());
            return new JSONNumber(n);
        }

        public static implicit operator long(JSONNode d) => d == null ? 0L : d.AsLong;

        public static implicit operator JSONNode(ulong n)
        {
            if (longAsString)
                return new JSONString(n.ToString());
            return new JSONNumber(n);
        }

        public static implicit operator ulong(JSONNode d) => d == null ? 0 : d.AsULong;

        public static implicit operator JSONNode(bool b) => new JSONBool(b);

        public static implicit operator bool(JSONNode d) => d == null ? false : d.AsBool;

        public static implicit operator JSONNode(KeyValuePair<string, JSONNode> aKeyValue) => aKeyValue.Value;

        public static bool operator ==(JSONNode a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;
            var aIsNull = a is JSONNull || ReferenceEquals(a, null) || a is JSONLazyCreator;
            var bIsNull = b is JSONNull || ReferenceEquals(b, null) || b is JSONLazyCreator;
            if (aIsNull && bIsNull)
                return true;
            return !aIsNull && a.Equals(b);
        }

        public static bool operator !=(JSONNode a, object b) => !(a == b);

        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => base.GetHashCode();

        #endregion operators
    }
    // End of JSONNode

    public class JSONArray : JSONNode
    {
        private readonly List<JSONNode> m_List = new();
        private bool inline;

        public override bool Inline
        {
            get => inline;
            set => inline = value;
        }

        public override JSONNodeType Tag => JSONNodeType.Array;
        public override bool IsArray => true;

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    return new JSONLazyCreator(this);
                return m_List[aIndex];
            }
            set
            {
                if (value == null)
                    value = JSONNull.CreateOrGet();
                if (aIndex < 0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }

        public override JSONNode this[string aKey]
        {
            get => new JSONLazyCreator(this);
            set
            {
                if (value == null)
                    value = JSONNull.CreateOrGet();
                m_List.Add(value);
            }
        }

        public override int Count => m_List.Count;

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var N in m_List)
                    yield return N;
            }
        }

        public override Enumerator GetEnumerator() => new(m_List.GetEnumerator());

        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null)
                aItem = JSONNull.CreateOrGet();
            m_List.Add(aItem);
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return null;
            var tmp = m_List[aIndex];
            m_List.RemoveAt(aIndex);
            return tmp;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            m_List.Remove(aNode);
            return aNode;
        }

        public override void Clear() => m_List.Clear();

        public override JSONNode Clone()
        {
            var node = new JSONArray();
            node.m_List.Capacity = m_List.Capacity;
            foreach (var n in m_List)
                if (n != null)
                    node.Add(n.Clone());
                else
                    node.Add(null);
            return node;
        }


        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            aSB.Append('[');
            var count = m_List.Count;
            if (inline)
                aMode = JSONTextMode.Compact;
            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                    aSB.Append(',');
                if (aMode == JSONTextMode.Indent)
                    aSB.AppendLine();

                if (aMode == JSONTextMode.Indent)
                    aSB.Append(' ', aIndent + aIndentInc);
                m_List[i].WriteToStringBuilder(aSB, aIndent + aIndentInc, aIndentInc, aMode);
            }

            if (aMode == JSONTextMode.Indent)
                aSB.AppendLine().Append(' ', aIndent);
            aSB.Append(']');
        }
    }
    // End of JSONArray

    public class JSONObject : JSONNode
    {
        private readonly Dictionary<string, JSONNode> m_Dict = new();
        private bool inline;

        public override bool Inline
        {
            get => inline;
            set => inline = value;
        }

        public override JSONNodeType Tag => JSONNodeType.Object;
        public override bool IsObject => true;


        public override JSONNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey))
                    return m_Dict[aKey];
                return new JSONLazyCreator(this, aKey);
            }
            set
            {
                if (value == null)
                    value = JSONNull.CreateOrGet();
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey, value);
            }
        }

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return null;
                return m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
                if (value == null)
                    value = JSONNull.CreateOrGet();
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return;
                var key = m_Dict.ElementAt(aIndex).Key;
                m_Dict[key] = value;
            }
        }

        public override int Count => m_Dict.Count;

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var N in m_Dict)
                    yield return N.Value;
            }
        }

        public override Enumerator GetEnumerator() => new(m_Dict.GetEnumerator());

        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null)
                aItem = JSONNull.CreateOrGet();

            if (aKey != null)
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = aItem;
                else
                    m_Dict.Add(aKey, aItem);
            }
            else
            {
                m_Dict.Add(Guid.NewGuid().ToString(), aItem);
            }
        }

        public override JSONNode Remove(string aKey)
        {
            if (!m_Dict.ContainsKey(aKey))
                return null;
            var tmp = m_Dict[aKey];
            m_Dict.Remove(aKey);
            return tmp;
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return null;
            var item = m_Dict.ElementAt(aIndex);
            m_Dict.Remove(item.Key);
            return item.Value;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            try
            {
                var item = m_Dict.Where(k => k.Value == aNode).First();
                m_Dict.Remove(item.Key);
                return aNode;
            }
            catch
            {
                return null;
            }
        }

        public override void Clear() => m_Dict.Clear();

        public override JSONNode Clone()
        {
            var node = new JSONObject();
            foreach (var n in m_Dict) node.Add(n.Key, n.Value.Clone());
            return node;
        }

        public override bool HasKey(string aKey) => m_Dict.ContainsKey(aKey);

        public override JSONNode GetValueOrDefault(string aKey, JSONNode aDefault)
        {
            JSONNode res;
            if (m_Dict.TryGetValue(aKey, out res))
                return res;
            return aDefault;
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            aSB.Append('{');
            var first = true;
            if (inline)
                aMode = JSONTextMode.Compact;
            foreach (var k in m_Dict)
            {
                if (!first)
                    aSB.Append(',');
                first = false;
                if (aMode == JSONTextMode.Indent)
                    aSB.AppendLine();
                if (aMode == JSONTextMode.Indent)
                    aSB.Append(' ', aIndent + aIndentInc);
                aSB.Append('\"').Append(Escape(k.Key)).Append('\"');
                if (aMode == JSONTextMode.Compact)
                    aSB.Append(':');
                else
                    aSB.Append(" : ");
                k.Value.WriteToStringBuilder(aSB, aIndent + aIndentInc, aIndentInc, aMode);
            }

            if (aMode == JSONTextMode.Indent)
                aSB.AppendLine().Append(' ', aIndent);
            aSB.Append('}');
        }
    }
    // End of JSONObject

    public class JSONString : JSONNode
    {
        private string m_Data;

        public JSONString(string aData) => m_Data = aData;

        public override JSONNodeType Tag => JSONNodeType.String;
        public override bool IsString => true;


        public override string Value
        {
            get => m_Data;
            set => m_Data = value;
        }

        public override Enumerator GetEnumerator() => new();

        public override JSONNode Clone() => new JSONString(m_Data);

        internal override void
            WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode) =>
            aSB.Append('\"').Append(Escape(m_Data)).Append('\"');

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
                return true;
            var s = obj as string;
            if (s != null)
                return m_Data == s;
            var s2 = obj as JSONString;
            if (s2 != null)
                return m_Data == s2.m_Data;
            return false;
        }

        public override int GetHashCode() => m_Data.GetHashCode();

        public override void Clear() => m_Data = "";
    }
    // End of JSONString

    public class JSONNumber : JSONNode
    {
        private double m_Data;

        public JSONNumber(double aData) => m_Data = aData;

        public JSONNumber(string aData) => Value = aData;

        public override JSONNodeType Tag => JSONNodeType.Number;
        public override bool IsNumber => true;

        public override string Value
        {
            get => m_Data.ToString(CultureInfo.InvariantCulture);
            set
            {
                double v;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    m_Data = v;
            }
        }

        public override double AsDouble
        {
            get => m_Data;
            set => m_Data = value;
        }

        public override long AsLong
        {
            get => (long) m_Data;
            set => m_Data = value;
        }

        public override ulong AsULong
        {
            get => (ulong) m_Data;
            set => m_Data = value;
        }

        public override Enumerator GetEnumerator() => new();

        public override JSONNode Clone() => new JSONNumber(m_Data);

        internal override void
            WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode) =>
            aSB.Append(Value);

        private static bool IsNumeric(object value) =>
            value is int ||
            value is uint ||
            value is float ||
            value is double ||
            value is decimal ||
            value is long ||
            value is ulong ||
            value is short ||
            value is ushort ||
            value is sbyte ||
            value is byte;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (base.Equals(obj))
                return true;
            var s2 = obj as JSONNumber;
            if (s2 != null)
                return m_Data == s2.m_Data;
            if (IsNumeric(obj))
                return Convert.ToDouble(obj) == m_Data;
            return false;
        }

        public override int GetHashCode() => m_Data.GetHashCode();

        public override void Clear() => m_Data = 0;
    }
    // End of JSONNumber

    public class JSONBool : JSONNode
    {
        private bool m_Data;

        public JSONBool(bool aData) => m_Data = aData;

        public JSONBool(string aData) => Value = aData;

        public override JSONNodeType Tag => JSONNodeType.Boolean;
        public override bool IsBoolean => true;

        public override string Value
        {
            get => m_Data.ToString();
            set
            {
                bool v;
                if (bool.TryParse(value, out v))
                    m_Data = v;
            }
        }

        public override bool AsBool
        {
            get => m_Data;
            set => m_Data = value;
        }

        public override Enumerator GetEnumerator() => new();

        public override JSONNode Clone() => new JSONBool(m_Data);

        internal override void
            WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode) =>
            aSB.Append(m_Data ? "true" : "false");

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is bool)
                return m_Data == (bool) obj;
            return false;
        }

        public override int GetHashCode() => m_Data.GetHashCode();

        public override void Clear() => m_Data = false;
    }
    // End of JSONBool

    public class JSONNull : JSONNode
    {
        private static readonly JSONNull m_StaticInstance = new();
        public static bool reuseSameInstance = true;
        private JSONNull() { }

        public override JSONNodeType Tag => JSONNodeType.NullValue;
        public override bool IsNull => true;

        public override string Value
        {
            get => "null";
            set { }
        }

        public override bool AsBool
        {
            get => false;
            set { }
        }

        public static JSONNull CreateOrGet()
        {
            if (reuseSameInstance)
                return m_StaticInstance;
            return new JSONNull();
        }

        public override Enumerator GetEnumerator() => new();

        public override JSONNode Clone() => CreateOrGet();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            return obj is JSONNull;
        }

        public override int GetHashCode() => 0;

        internal override void
            WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode) =>
            aSB.Append("null");
    }
    // End of JSONNull

    internal class JSONLazyCreator : JSONNode
    {
        private readonly string m_Key;
        private JSONNode m_Node;

        public JSONLazyCreator(JSONNode aNode)
        {
            m_Node = aNode;
            m_Key = null;
        }

        public JSONLazyCreator(JSONNode aNode, string aKey)
        {
            m_Node = aNode;
            m_Key = aKey;
        }

        public override JSONNodeType Tag => JSONNodeType.None;

        public override JSONNode this[int aIndex]
        {
            get => new JSONLazyCreator(this);
            set => Set(new JSONArray()).Add(value);
        }

        public override JSONNode this[string aKey]
        {
            get => new JSONLazyCreator(this, aKey);
            set => Set(new JSONObject()).Add(aKey, value);
        }

        public override int AsInt
        {
            get
            {
                Set(new JSONNumber(0));
                return 0;
            }
            set => Set(new JSONNumber(value));
        }

        public override float AsFloat
        {
            get
            {
                Set(new JSONNumber(0.0f));
                return 0.0f;
            }
            set => Set(new JSONNumber(value));
        }

        public override double AsDouble
        {
            get
            {
                Set(new JSONNumber(0.0));
                return 0.0;
            }
            set => Set(new JSONNumber(value));
        }

        public override long AsLong
        {
            get
            {
                if (longAsString)
                    Set(new JSONString("0"));
                else
                    Set(new JSONNumber(0.0));
                return 0L;
            }
            set
            {
                if (longAsString)
                    Set(new JSONString(value.ToString()));
                else
                    Set(new JSONNumber(value));
            }
        }

        public override ulong AsULong
        {
            get
            {
                if (longAsString)
                    Set(new JSONString("0"));
                else
                    Set(new JSONNumber(0.0));
                return 0L;
            }
            set
            {
                if (longAsString)
                    Set(new JSONString(value.ToString()));
                else
                    Set(new JSONNumber(value));
            }
        }

        public override bool AsBool
        {
            get
            {
                Set(new JSONBool(false));
                return false;
            }
            set => Set(new JSONBool(value));
        }

        public override JSONArray AsArray => Set(new JSONArray());

        public override JSONObject AsObject => Set(new JSONObject());

        public override Enumerator GetEnumerator() => new();

        private T Set<T>(T aVal) where T : JSONNode
        {
            if (m_Key == null)
                m_Node.Add(aVal);
            else
                m_Node.Add(m_Key, aVal);
            m_Node = null; // Be GC friendly.
            return aVal;
        }

        public override void Add(JSONNode aItem) => Set(new JSONArray()).Add(aItem);

        public override void Add(string aKey, JSONNode aItem) => Set(new JSONObject()).Add(aKey, aItem);

        public static bool operator ==(JSONLazyCreator a, object b)
        {
            if (b == null)
                return true;
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(JSONLazyCreator a, object b) => !(a == b);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return true;
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode() => 0;

        internal override void
            WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode) =>
            aSB.Append("null");
    }
    // End of JSONLazyCreator

    public static class JSON
    {
        public static JSONNode Parse(string aJSON) => JSONNode.Parse(aJSON);
    }
}