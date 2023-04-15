using System;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NativeSqlValueConversor : ISqlTranslation
    {
        /// <summary>
        /// An array of native types used for fast type checking and conversion.
        /// </summary>
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

        /// <summary>
        /// Determines if a type is nullable.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is nullable, false otherwise.</returns>
        internal static bool IsNative(Type type)
        {
            return type == null || IsNullable(type) || type.IsPrimitive || type.IsEnum || nativeTypes.Contains(type);
        }

        /// <summary>
        /// Determines if a type is nullable.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is nullable, false otherwise.</returns>
        internal static bool IsNullable(Type type)
        {
            return type == typeof(Nullable<>) || type.Name == "Nullable`1";
        }

        /// <summary>
        /// Converts a SQL value to its equivalent .NET representation based on the expected data type.
        /// </summary>
        /// <param name="value">The SQL value to be converted.</param>
        /// <param name="expectedType">The expected data type of the .NET representation.</param>
        /// <returns>The equivalent .NET representation of the SQL value.</returns>
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

        /// <summary>
        /// Converts a value to its equivalent SQL representation based on its data type.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <param name="type">The data type of the value.</param>
        /// <returns>The equivalent SQL representation of the value.</returns>
        public object ToSqlValue(object value, Type type)
        {
            if (TranslationUtils.IsNull(value))
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
