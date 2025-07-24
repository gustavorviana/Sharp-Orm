using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Sqlite.ColumnTypes
{
    public class SqliteStringColumnTypeMap : IColumnTypeMap
    {
        public bool CanWork(Type type)
        {
            return type == typeof(string);
        }

        public string Build(DataColumn column)
        {
            return column.MaxLength < 1 ? "TEXT" : string.Concat("TEXT(", column.MaxLength, ")");
        }
    }
}
