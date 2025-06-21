using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace SharpOrm.DataTranslation
{
    internal static class TranslationUtils
    {
        /// <summary>
        /// An array of native types used for fast type checking and conversion.
        /// </summary>
        private static readonly Type[] _nativeTypes = new Type[]
        {
            typeof(bool),
            typeof(DBNull),
            typeof(string),
            typeof(DateTime),
            typeof(Guid),
            typeof(TimeSpan),
            typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
            ,typeof(DateOnly)
            ,typeof(TimeOnly)
#endif
        };

        public static bool IsNative(Type type, bool ignoreBuffer)
        {
            if (type is null)
                return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) )
                return IsNative(Nullable.GetUnderlyingType(type), ignoreBuffer);

            return type.IsPrimitive || type.IsEnum || _nativeTypes.Contains(type) || IsNumeric(type) || !ignoreBuffer && IsBuffer(type);
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
                    type1 == typeof(string) && type2 == typeof(Guid) ||
                    type2 == typeof(string) && type1 == typeof(Guid) ||
                    IsNumberWithoutDecimal(type1) == IsNumberWithoutDecimal(type2) ||
                    IsNumberWithDecimal(type1) == IsNumberWithDecimal(type2);
        }

        public static bool IsBuffer(Type type)
        {
            return type == typeof(byte[]) || typeof(Stream).IsAssignableFrom(type);
        }

        public static bool IsInvalidPk(object value)
        {
            return IsNull(value) || IsZero(value) || value is Guid guid && guid == Guid.Empty;
        }

        public static bool IsNull(object value)
        {
            return value == null || value is DBNull || value.Equals(DateTime.MinValue);
        }

        public static bool IsZero(object value)
        {
            return IsNumeric(value?.GetType()) && value.Equals(Activator.CreateInstance(value.GetType()));
        }

        public static bool IsDateOrTime(Type type)
        {
            return new Type[]
            {
#if NET6_0_OR_GREATER
                typeof(DateOnly),
                typeof(TimeOnly),
#endif
                typeof(TimeSpan),
                typeof(DateTime),
                typeof(DateTimeOffset)
            }.Contains(type);
        }

        /// <summary>
        /// Try parse object as number, 0 on fail.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int TryNumeric(object value)
        {
            return TranslationUtils.IsNumeric(value?.GetType()) ? Convert.ToInt32(value) : 0;
        }

        public static bool IsNumeric(Type type)
        {
            return IsNumberWithoutDecimal(type) || IsNumberWithDecimal(type);
        }

        public static bool IsNumericString(string value)
        {
            if (string.IsNullOrEmpty(value) || CheckDot(value[0]) || CheckDot(value[value.Length - 1]))
                return false;

            bool hasDot = false;

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                bool digit = char.IsDigit(c);
                bool isDot = c == '.' || c == ',';
                if (!digit && !isDot)
                    return false;

                if (digit)
                    continue;

                if (hasDot)
                    return false;

                if (isDot)
                    hasDot = true;
            }

            return true;
        }

        private static bool CheckDot(char c)
        {
            return c == '.' || c == ',';
        }

        public static bool IsNumberWithDecimal(Type type)
        {
            return type == typeof(decimal) || type == typeof(float) || type == typeof(double);
        }

        public static bool IsNumberWithoutDecimal(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(short) || type == typeof(ushort) || type == typeof(uint) || type == typeof(long)
            || type == typeof(ulong);
        }

        public static T FromSql<T>(this TranslationRegistry registry, object value)
        {
            if (value is null || value is DBNull)
                return default;

            return (T)registry.FromSql(value, typeof(T));
        }

        public static bool IsNullOrEmpty(ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }
    }
}
