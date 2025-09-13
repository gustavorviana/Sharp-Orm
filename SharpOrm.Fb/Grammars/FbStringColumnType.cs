using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Firebird.ColumnTypes
{
    public class FbStringColumnType : IColumnTypeMap
    {
        public bool CanWork(Type type) => type == typeof(string);

        public string Build(DataColumn column)
        {
            var maxSize = column.MaxLength;

            if (maxSize <= 0)
                maxSize = 32765;

            if (maxSize <= 32765)
                return string.Concat("VARCHAR(", maxSize, ")");

            return "BLOB SUB_TYPE TEXT";
        }
    }
}