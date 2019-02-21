using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TommyTests
{
    public static class Utils
    {
        public static Stream ToStream(this string self)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(self));
        }
    }
}
