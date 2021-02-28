using SimpleJSON;

namespace Tommy.Tests.Util
{
    public static class TomlExtensions
    {
        public static string ToCompactJsonString(this TomlNode node)
        {
            var obj = new JSONObject();
            Traverse(obj, node);
            return obj.ToString();
        }

        private static void Traverse(JSONNode obj, TomlNode node, string nodeKey = null, bool isChild = false)
        {
            static void Add(JSONNode obj, string k, JSONNode add)
            {
                if (k != null)
                    obj.Add(k, add);
                else
                    obj.Add(add);
            }

            // Normal table, add it to the root
            if (node is TomlTable tbl && isChild)
            {
                var jsonObj = new JSONObject();
                Add(obj, nodeKey, jsonObj);
                Traverse(jsonObj, tbl);
                return;
            }

            if (node.HasValue)
            {
                switch (node)
                {
                    case TomlString str:
                        Add(obj, nodeKey, str.Value);
                        break;
                    case TomlInteger i:
                        Add(obj, nodeKey, i.Value);
                        break;
                    case TomlFloat f:
                        Add(obj, nodeKey, f.Value);
                        break;
                    case TomlDateTime dt:
                        Add(obj, nodeKey, dt.ToInlineToml());
                        break;
                    case TomlBoolean b:
                        Add(obj, nodeKey, b.Value);
                        break;
                    case TomlArray arr:
                        var jsonArray = new JSONArray();
                        Add(obj, nodeKey, jsonArray);
                        foreach (var arrValue in arr.Children)
                            Traverse(jsonArray, arrValue, isChild: true);

                        break;
                }

                return;
            }

            foreach (var key in node.Keys)
                Traverse(obj, node[key], key, true);
        }
    }
}