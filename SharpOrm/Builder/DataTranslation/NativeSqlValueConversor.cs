using System;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NativeSqlValueConversor : ISqlTranslation
    {
        private static readonly Type[] types = new Type[]
        {
            typeof(DBNull),
            typeof(string),
            typeof(DateTime),
            typeof(Guid),
            typeof(TimeSpan),
            typeof(decimal),
            typeof(Nullable<>)
        };

        public bool CanWork(Type type) => type == null || type.IsPrimitive || type.IsEnum || types.Contains(type);

        public object FromSqlValue(object value, Type expectedType)
        {
            if (value is DBNull)
                return null;

            if (expectedType == typeof(Guid))
                return Guid.Parse((string)value);

            if (expectedType == typeof(DateTime?) && value is DBNull)
                return DateTime.MinValue;

            if (expectedType.IsEnum)
                return Enum.ToObject(expectedType, value);

            return value;
        }

        public object ToSqlValue(object value, Type type)
        {
            if (value == null || value is DBNull || value is DateTime date && date == DateTime.MinValue)
                return DBNull.Value;

            if (value is Guid guid)
                return guid.ToString();

            if (type.IsEnum)
                return Convert.ToInt32(value);

            return value;
        }
    }
}
