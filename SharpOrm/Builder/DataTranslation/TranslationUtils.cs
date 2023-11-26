using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpOrm.Builder.DataTranslation
{
    internal static class TranslationUtils
    {
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
            return (T)registry.FromSql(value, typeof(T));
        }

        public static void AddToArray<T>(ref T[] array, IList<T> items)
        {
            int lastSize = array.Length;
            Array.Resize(ref array, array.Length + items.Count);

            for (int i = 0; i < items.Count; i++)
                array[lastSize + i] = items[i];
        }
    }
}
