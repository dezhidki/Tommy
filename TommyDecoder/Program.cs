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
                foreach (var tomlArrayValue in tomlArray.Values)
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
                        foreach (var arrValue in arr.Values)
                        {
                            var o = new JSONObject();
                            jsonArray.Add(o);
                            Traverse(o, arrValue);
                        }
                        break;
                }
                return;
            }

            foreach (var keyValuePair in node.Children)
            {
                JSONNode newNode;
                if (keyValuePair.Value is TomlArray arr && arr.Values.Count > 0 && arr.Values[0] is TomlTable)
                    newNode = new JSONArray();
                else
                    newNode = new JSONObject();
                obj[keyValuePair.Key] = newNode;
                Traverse(newNode, keyValuePair.Value);
            }

            //if (node is TomlTable tbl)
            //{
            //    JSONNode o;
            //    if (nodeKey == null)
            //        o = obj;
            //    else
            //    {
            //        o = new JSONObject();
            //        obj[nodeKey] = o;
            //    }
                
            //    foreach (var keyValuePair in tbl.Children)
            //    {
            //        Traverse(o, keyValuePair.Value, keyValuePair.Key);
            //    }
            //}
        }
    }
}
