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

        /// <summary>
        /// Filters the characters in the string using the provided function and returns a new string with the remaining characters.
        /// </summary>
        /// <param name="value">The input string.</param>
        /// <param name="func">The function used to determine which characters to keep.</param>
        /// <returns>A new string containing only the characters that satisfy the provided function.</returns>
        public static string Only(this string value, Func<char, bool> func)
        {
            return new string(value.Where(c => func(c)).ToArray());
        }

        /// <summary>
        /// Sanitizes the SQL name by adding the specified prefix and suffix to each part of the name separated by dots.
        /// </summary>
        /// <param name="value">The input string representing the SQL name.</param>
        /// <param name="prefix">The prefix character to add.</param>
        /// <param name="suffix">The suffix character to add.</param>
        /// <returns>A sanitized SQL name with the specified prefix and suffix added to each part.</returns>
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

        /// <summary>
        /// Checks if the collection of strings contains a specific string using the specified string comparison.
        /// </summary>
        /// <param name="values">The collection of strings to search in.</param>
        /// <param name="toCompare">The string to compare against the collection.</param>
        /// <param name="stringComparison">The string comparison type to use.</param>
        /// <returns>True if the collection contains the string, otherwise false.</returns>
        public static bool Contains(this IEnumerable<string> values, string toCompare, StringComparison stringComparison)
        {
            return values.Any(v => v.Equals(toCompare, stringComparison));
        }
    }
}
