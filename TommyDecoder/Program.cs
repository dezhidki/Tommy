using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using SimpleJSON;
using Tommy;

namespace TommyDecoder
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = Console.In.ReadToEnd();

            using (var sr = new StringReader(input))
            {
                var node = TOML.Parse(sr);
                var obj = new JSONObject();
                Traverse(obj, node);
                Console.WriteLine(obj.ToString());
            }
        }

        static void Traverse(JSONNode obj, TomlNode node)
        {
            if (obj is JSONArray jsonArr && node is TomlArray tomlArray)
            {
                foreach (var tomlArrayValue in tomlArray.Children)
                {
                    var newNode = new JSONObject();
                    jsonArr.Add(newNode);
                    Traverse(newNode, tomlArrayValue);
                }
                return;
            }

            if (node.HasValue)
            {
                switch (node)
                {
                    case TomlString str:
                        obj["type"] = "string";
                        obj["value"] = str.Value;
                        break;
                    case TomlInteger i:
                        obj["type"] = "integer";
                        obj["value"] = i.Value.ToString(CultureInfo.InvariantCulture);
                        break;
                    case TomlFloat f:
                        obj["type"] = "float";
                        obj["value"] = f.Value.ToString("G",CultureInfo.InvariantCulture);
                        break;
                    case TomlDateTime dt:
                        obj["type"] = "datetime";
                        obj["value"] = dt.Value.ToString("O", CultureInfo.InvariantCulture);
                        break;
                    case TomlBoolean b:
                        obj["type"] = "bool";
                        obj["value"] = b.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
                        break;
                    case TomlArray arr:
                        var jsonArray = new JSONArray();
                        obj["type"] = "array";
                        obj["value"] = jsonArray;
                        foreach (var arrValue in arr.Children)
                        {
                            var o = new JSONObject();
                            jsonArray.Add(o);
                            Traverse(o, arrValue);
                        }
                        break;
                }
                return;
            }

            foreach (var key in node.Keys)
            {
                var val = node[key];
                JSONNode newNode;
                if (val is TomlArray arr && arr.ChildrenCount > 0 && arr[0] is TomlTable)
                    newNode = new JSONArray();
                else
                    newNode = new JSONObject();
                obj[key] = newNode;
                Traverse(newNode, val);
            }
        }
    }
}
