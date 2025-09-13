using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.SqlServer.ColumnTypes
{
    public class SqlServerStringColumnTypeMap : IColumnTypeMap
    {
        private readonly bool _useUnicode;

        public SqlServerStringColumnTypeMap(bool useUnicode)
        {
            _useUnicode = useUnicode;
        }

        public bool CanWork(Type type)
        {
            return type == typeof(string);
        }

        public string Build(DataColumn column)
        {
            var baseType = _useUnicode ? "NVARCHAR" : "VARCHAR";
            var maxSize = _useUnicode ? 4000 : 8000;

            if (column.MaxLength > 0 && column.MaxLength <= maxSize)
                return $"{baseType}({column.MaxLength})";

            if (column.MaxLength == -1 || column.MaxLength > maxSize)
                return $"{baseType}(MAX)";

            return $"{baseType}(255)";
        }
    }
}
