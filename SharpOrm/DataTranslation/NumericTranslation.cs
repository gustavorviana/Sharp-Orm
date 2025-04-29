using System;
using System.Globalization;

namespace SharpOrm.DataTranslation
{
    public class NumericTranslation : ISqlTranslation
    {
        public CultureInfo Culture { get; set; } = CultureInfo.DefaultThreadCurrentCulture;

        public bool CanWork(Type type) => TranslationUtils.IsNumeric(type) || type == typeof(string);

        public virtual object FromSqlValue(object value, Type expectedType)
        {
            if (value.GetType() == expectedType)
                return value;

            if (expectedType == typeof(int))
                return Convert.ToInt32(value, Culture);

            if (expectedType == typeof(long))
                return Convert.ToInt64(value, Culture);

            if (expectedType == typeof(byte))
                return Convert.ToByte(value, Culture);

            if (expectedType == typeof(sbyte))
                return Convert.ToSByte(value, Culture);

            if (expectedType == typeof(short))
                return Convert.ToInt16(value, Culture);

            if (expectedType == typeof(ushort))
                return Convert.ToUInt16(value, Culture);

            if (expectedType == typeof(uint))
                return Convert.ToUInt32(value, Culture);

            if (expectedType == typeof(ulong))
                return Convert.ToUInt64(value, Culture);

            if (expectedType == typeof(decimal))
                return Convert.ToDecimal(value, Culture);

            if (expectedType == typeof(float))
                return Convert.ToSingle(value, Culture);

            if (expectedType == typeof(double))
                return Convert.ToDouble(value, Culture);

            throw new NotSupportedException();
        }

        public virtual object ToSqlValue(object value, Type type)
        {
            return value;
        }
    }
}