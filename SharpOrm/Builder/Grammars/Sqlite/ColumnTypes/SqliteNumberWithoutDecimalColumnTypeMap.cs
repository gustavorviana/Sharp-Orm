using SharpOrm.DataTranslation;
using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Sqlite.ColumnTypes
{
    public class SqliteNumberWithoutDecimalColumnTypeMap : IColumnTypeMap
    {
        public bool CanWork(Type type)
        {
            return TranslationUtils.IsNumberWithoutDecimal(type) || type == typeof(bool);
        }

        public string Build(DataColumn column)
        {
            return "INTEGER";
        }
    }
}
