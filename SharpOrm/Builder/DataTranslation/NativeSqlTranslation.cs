using System;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NativeSqlTranslation : ISqlTranslation
    {
        private static readonly BinaryTranslator binaryTranslator = new BinaryTranslator();
        private static readonly NumericTranslation numericTranslation = new NumericTranslation();
        public string GuidFormat { get; set; } = "D";

        public TimeZoneInfo TimeZone { get; set; }

        public bool CanWork(Type type) => TranslationUtils.IsNative(type, true) || binaryTranslator.CanWork(type);

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

            if (expectedType == typeof(DateTime))
                return ParseDateTime(value);

            if (expectedType == typeof(TimeSpan))
                return ParseTimespan(value);

            if (numericTranslation.CanWork(expectedType))
                return numericTranslation.FromSqlValue(value, expectedType);

            if (binaryTranslator.CanWork(expectedType))
                return binaryTranslator.FromSqlValue(value, expectedType);

            return value;
        }

        private object ParseTimespan(object obj)
        {
            obj = ParseDateTime(obj);

            if (obj is DateTime date)
                return date.TimeOfDay;

            return obj;
        }

        private object ParseDateTime(object obj)
        {
            if (obj is DateTimeOffset offset)
                return TimeZone == null ? offset.DateTime : TimeZoneInfo.ConvertTimeFromUtc(offset.UtcDateTime, TimeZone);

            if (obj is string strDate)
                return DateTime.TryParse(strDate, out var date) ? date : (DateTime?)null;

            return obj;
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

            if (value is DateTime || value is TimeSpan || value is DateTimeOffset)
                return value;

            if (numericTranslation.CanWork(type))
                return numericTranslation.ToSqlValue(value, type);

            if (binaryTranslator.CanWork(type))
                return binaryTranslator.ToSqlValue(value, type);

            return value?.ToString();
        }
    }
}
