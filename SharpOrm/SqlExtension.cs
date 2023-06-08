using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    public static class SqlExtension
    {
        /// <summary>
        /// Sanitizes a string value for use in SQL queries, replacing all occurrences
        /// of the % and _ characters with their respective escaped versions.
        /// </summary>
        /// <param name="value">The string value to sanitize.</param>
        /// <returns>A sanitized version of the input string.</returns>
        public static string SanitizeSqlValue(this string value)
        {
            return value.Replace("%", "\\%").Replace("_", "\\_");
        }

        public static string Only(this string value, Func<char, bool> func)
        {
            return new string(value.Where(c => func(c)).ToArray());
        }

        public static string AlphaNumericOnly(this string value, params char[] exceptions)
        {
            return value.Only(c => char.IsLetterOrDigit(c) || exceptions.Contains(c));
        }

        public static string SanitizeSqlName(this string value, char prefix, char suffix)
        {
            string[] splitNames = value.Split('.');
            for (int i = 0; i < splitNames.Length; i++)
            {
                if (splitNames[i] == "*")
                    continue;

                splitNames[i] = string.Format("{0}{1}{2}", prefix, splitNames[i].Only(c => c != prefix && c != suffix), suffix);
            }

            return string.Join(".", splitNames);
        }

        public static bool Contains(this IEnumerable<string> values, string toCompare, StringComparison stringComparison)
        {
            return values.Any(v => v.Equals(toCompare, stringComparison));
        }
    }
}
