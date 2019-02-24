using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                foreach (var keyValuePair in node.Children)
                {
                    var o = new JSONObject();
                    obj[keyValuePair.Key] = o;
                    Traverse(o, keyValuePair.Value, keyValuePair.Key);
                }

                Console.WriteLine(obj.ToString());
            }
        }

        static void Traverse(JSONNode obj, TomlNode node, string nodeKey = null)
        {
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
                        obj["type"] = "array";
                        var jsonArray = new JSONArray();
                        foreach (var arrValue in arr.Values)
                        {
                            var o = new JSONObject();
                            jsonArray.Add(o);
                            Traverse(o, arrValue);
                        }
                        obj["value"] = jsonArray;
                        break;
                }
                return;
            }

            if (node is TomlTable tbl)
            {
                var o = new JSONObject();
                obj[nodeKey] = o;
                
                foreach (var keyValuePair in tbl.Children)
                {
                    Traverse(o, keyValuePair.Value, keyValuePair.Key);
                }
            }
        }
    }
}
