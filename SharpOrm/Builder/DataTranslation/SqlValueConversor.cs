using System;

namespace SharpOrm.Builder.DataTranslation
{
    public abstract class SqlValueConversor
    {
        public abstract object FromSqlValue(object value, Type columnType, string columnName);

        public abstract object ToSqlValue(object value, Type columnType, string columnName);
    }
}