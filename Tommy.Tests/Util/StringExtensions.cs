using System;

namespace Tommy.Tests.Util
{
    public static class StringExtensions
    {
        public static string NormalizeNewLines(this string str) => str.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }
}