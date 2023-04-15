using System;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NativeSqlValueConversor : ISqlTranslation
    {
        private static readonly Type[] nativeTypes = new Type[]
        {
            typeof(DBNull),
            typeof(string),
            typeof(DateTime),
            typeof(Guid),
            typeof(TimeSpan),
            typeof(decimal)
        };

        public bool CanWork(Type type) => IsNative(type);

        internal static bool IsNative(Type type)
        {
            return type == null || IsNullable(type) || type.IsPrimitive || type.IsEnum || nativeTypes.Contains(type);
        }

        internal static bool IsNullable(Type type)
        {
            return type == typeof(Nullable<>) || type.Name == "Nullable`1";
        }

        public object FromSqlValue(object value, Type expectedType)
        {
            if (value is DBNull)
                return null;

            if (expectedType == typeof(Guid))
                return Guid.Parse((string)value);

            if (expectedType.IsEnum)
                return Enum.ToObject(expectedType, value);

            if (expectedType == typeof(bool) && value is int i)
                return i == 1;

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

            if (value is bool vBool)
                return vBool ? 1 : 0;

            return value;
        }
    }
}
