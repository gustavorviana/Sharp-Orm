using SharpOrm.DataTranslation;
using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Sqlite.ColumnTypes
{
    public class SqliteNumberWithDecimalColumnTypeMap : IColumnTypeMap
    {
        public bool CanWork(Type type)
        {
            return TranslationUtils.IsNumberWithDecimal(type);
        }

        public string Build(DataColumn column)
        {
            return "REAL";
        }
    }

}
