using System;
using System.IO;
using SimpleJSON;
using Tommy;

namespace TommyDecoder
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var input = Console.In.ReadToEnd();
            JSONNode.forceASCII = true;

            using (var sr = new StringReader(input))
            {
                var node = TOMLParser.Parse(sr);
                var obj = new JSONObject();
                Traverse(obj, node);
                Console.WriteLine(obj.ToString());
            }
        }

        private static void Traverse(JSONNode obj, TomlNode node)
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
                        obj["value"] = str.ToString();
                        break;
                    case TomlInteger i:
                        obj["type"] = "integer";
                        obj["value"] = i.Value.ToString();
                        break;
                    case TomlFloat f:
                        obj["type"] = "float";
                        obj["value"] = f.ToString();
                        break;
                    case TomlDateTime dt:
                        if(dt.OnlyDate)
                            obj["type"] = "date";
                        else if (dt.OnlyTime)
                            obj["type"] = "time";
                        else if (dt.Value.Kind == DateTimeKind.Local)
                            obj["type"] = "datetime-local";
                        else
                            obj["type"] = "datetime";
                        obj["value"] = dt.ToString();
                        break;
                    case TomlBoolean b:
                        obj["type"] = "bool";
                        obj["value"] = b.ToString();
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