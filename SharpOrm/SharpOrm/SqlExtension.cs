using System;
using System.Data.Common;
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

        internal static void LoadFromDataReader(this Model model, DbDataReader reader)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                model.columns[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader[i];
        }
    }
}
