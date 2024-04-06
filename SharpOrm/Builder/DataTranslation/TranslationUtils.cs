using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    internal static class TranslationUtils
    {
        /// <summary>
        /// An array of native types used for fast type checking and conversion.
        /// </summary>
        private static readonly Type[] nativeTypes = new Type[]
        {
            typeof(bool),
            typeof(DBNull),
            typeof(string),
            typeof(DateTime),
            typeof(Guid),
            typeof(TimeSpan),
            typeof(DateTimeOffset)
        };

        public static bool IsNative(Type type, bool ignoreBuffer)
        {
            if (type is null)
                return true;

            if (Nullable.GetUnderlyingType(type) is Type nType)
                type = nType;

            return type.IsPrimitive || type.IsEnum || nativeTypes.Contains(type) || IsNumeric(type) || (!ignoreBuffer && IsBuffer(type));
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

        public static bool IsBuffer(Type type)
        {
            return type == typeof(byte[]) || typeof(Stream).IsAssignableFrom(type);
        }

        public static bool IsInvalidPk(object value)
        {
            return IsNull(value) || IsZero(value) || (value is Guid guid && guid == Guid.Empty);
        }

        public static bool IsNull(object value)
        {
            return value == null || value is DBNull || value.Equals(DateTime.MinValue);
        }

        public static bool IsZero(object value)
        {
            return IsNumeric(value?.GetType()) && value.Equals(Activator.CreateInstance(value.GetType()));
        }

        public static bool IsNumeric(Type type)
        {
            return IsNumberWithoutDecimal(type) || IsNumberWithDecimal(type);
        }

        public static bool IsNumberWithDecimal(Type type)
        {
            return type == typeof(decimal) || type == typeof(float) || type == typeof(double);
        }

        public static bool IsNumberWithoutDecimal(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(Int16) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(Int64)
            || type == typeof(UInt64);
        }

        public static T FromSql<T>(this TranslationRegistry registry, object value)
        {
            if (value is null)
                return default(T);

            return (T)registry.FromSql(value, typeof(T));
        }

        public static void AddToArray<T>(ref T[] array, IList<T> items)
        {
            int lastSize = array.Length;
            Array.Resize(ref array, array.Length + items.Count);

            for (int i = 0; i < items.Count; i++)
                array[lastSize + i] = items[i];
        }

        public static bool IsNullOrEmpty(ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }
    }
}
