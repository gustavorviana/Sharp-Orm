using System;
using System.Linq;

namespace SharpOrm
{
    public static class SqlExtension
    {
        public static string Only(this string value, Func<char, bool> func)
        {
            return new string(value.Where(c => func(c)).ToArray());
        }

        public static string AlphaNumericOnly(this string value, params char[] exceptions)
        {
            return value.Only(c => char.IsDigit(c) || char.IsLetter(c) || exceptions.Contains(c));
        }
    }
}
