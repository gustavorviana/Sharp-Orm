using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Mysql.ColumnTypes
{
    public class MysqlStringColumnType : IColumnTypeMap
    {
        public bool CanWork(Type type) => type == typeof(string);

        public string Build(DataColumn column)
        {
            var maxSize = column.MaxLength;
            if (maxSize <= 0)
                maxSize = 65535;

            if (maxSize == 255)
                return "TINYTEXT";

            if (maxSize <= 16383)
                return string.Concat("VARCHAR(", maxSize, ")");

            if (maxSize <= 65635)
                return "TEXT";

            if (maxSize <= 16777215)
                return "MEDIUMTEXT";

            return "LONGTEXT";
        }
    }
}
