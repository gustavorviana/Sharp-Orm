using System;

namespace SharpOrm.Builder.DataTranslation
{
    public abstract class SqlValueConversor
    {
        public abstract object FromDb(object value, Type columnType, string columnName);

        public abstract object ToDb(object value, Type columnType, string columnName);
    }
}