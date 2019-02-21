using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    Key = "hello",
                    ["key"] = new TomlNode
                    {
                        Key = "key",
                        RawValue = "Wew"
                    },
                    ["test"] = new TomlTable
                    {
                        ["foo"] = new TomlNode
                        {
                            Key = "foo",
                            RawValue = "Value"
                        }
                    }
                }
            };
        }
    }
}
