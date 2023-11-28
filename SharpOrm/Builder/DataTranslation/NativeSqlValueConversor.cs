using System;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NativeSqlValueConversor : ISqlTranslation
    {
        private static readonly BinaryTranslator binaryTranslator = new BinaryTranslator();
        private static readonly NumericTranslation numericTranslation = new NumericTranslation();
        /// <summary>
        /// An array of native types used for fast type checking and conversion.
        /// </summary>
        private static readonly Type[] nativeTypes = new Type[]
        {
            typeof(DBNull),
            typeof(string),
            typeof(DateTime),
            typeof(Guid),
            typeof(TimeSpan)
        };
        public string GuidFormat { get; set; } = "D";

        public bool CanWork(Type type) => IsNative(type);

        /// <summary>
        /// Determines if a type is nullable.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is nullable, false otherwise.</returns>
        internal static bool IsNative(Type type)
        {
            if (type == null)
                return true;

            if (Nullable.GetUnderlyingType(type) is Type nType)
                type = nType;

            return type.IsPrimitive || type.IsEnum || nativeTypes.Contains(type) || numericTranslation.CanWork(type) || binaryTranslator.CanWork(type);
        }

        /// <summary>
        /// Checks if two types are considered similar for specific cases.
        /// </summary>
        /// <param name="type1">The first Type to compare.</param>
        /// <param name="type2">The second Type to compare.</param>
        /// <returns>True if the types are considered similar, otherwise false.</returns>
        internal static bool IsSimilar(Type type1, Type type2)
        {
            return type1 == type2 ||
                    (type1 == typeof(string) && type2 == typeof(Guid)) ||
                    (type2 == typeof(string) && type1 == typeof(Guid)) ||
                    BinaryTranslator.IsSame(type1, type2) ||
                    (TranslationUtils.IsNumberWithoutDecimal(type1) == TranslationUtils.IsNumberWithoutDecimal(type2)) ||
                    (TranslationUtils.IsNumberWithDecimal(type1) == TranslationUtils.IsNumberWithDecimal(type2));
        }

        /// <summary>
        /// Converts a SQL value to its equivalent .NET representation based on the expected data type.
        /// </summary>
        /// <param name="value">The SQL value to be converted.</param>
        /// <param name="expectedType">The expected data type of the .NET representation.</param>
        /// <returns>The equivalent .NET representation of the SQL value.</returns>
        public object FromSqlValue(object value, Type expectedType)
        {
            if (value is DBNull || value is null)
                return null;

            if (expectedType == typeof(string))
                return value is string ? value : value.ToString();

            if (expectedType == typeof(bool))
                return Convert.ToBoolean(value);

            if (expectedType.IsEnum)
                return Enum.ToObject(expectedType, value);

            if (expectedType == typeof(Guid))
                return value is Guid guid ? guid : Guid.Parse((string)value);

            if (expectedType == typeof(TimeSpan) && value is DateTime date)
                return date.TimeOfDay;

            if (numericTranslation.CanWork(expectedType))
                return numericTranslation.FromSqlValue(value, expectedType);

            if (binaryTranslator.CanWork(expectedType))
                return binaryTranslator.FromSqlValue(value, expectedType);

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
                return guid.ToString(GuidFormat);

            if (type.IsEnum)
                return Convert.ToInt32(value);

            if (value is bool vBool)
                return vBool ? 1 : 0;

            if (value is DateTime || value is TimeSpan)
                return value;

            if (numericTranslation.CanWork(type))
                return numericTranslation.ToSqlValue(value, type);

            if (binaryTranslator.CanWork(type))
                return binaryTranslator.ToSqlValue(value, type);

            return value?.ToString();
        }
    }
}
