using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tommy;

namespace TommyTests
{
    [TestClass]
    public class WriteTests
    {
        [TestMethod]
        public void ObjectConstructTest()
        {
            var node = new TomlTable
            {
                ["hello"] = new TomlNode
                {
                    ["key"] = "wew",
                    ["test"] = new TomlTable
                    {
                        ["foo"] = "Value"
                    }
                }
            };
        }
    }
}
