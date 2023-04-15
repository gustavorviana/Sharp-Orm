using System;
using System.Collections.Generic;
using System.Text;

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
            return IsNumeric(value) && value.Equals(Activator.CreateInstance(value.GetType()));
        }

        public static bool IsNumeric(object value)
        {
            return IsNumberWithoutDecimal(value) || IsNumberWithDecimal(value);
        }

        public static bool IsNumberWithDecimal(object value)
        {
            return value is decimal || value is float || value is double;
        }

        public static bool IsNumberWithoutDecimal(object value)
        {
            return value is int || value is long || value is byte || value is sbyte
            || value is Int16 || value is UInt16 || value is UInt32 || value is Int64
            || value is UInt64;
        }
    }
}
