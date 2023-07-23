using System;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NumericTranslation : ISqlTranslation
    {
        public bool CanWork(Type type) => TranslationUtils.IsNumeric(type);

        public object FromSqlValue(object value, Type expectedType)
        {
            if (value.GetType() == expectedType)
                return value;

            if (expectedType == typeof(int))
                return Convert.ToInt32(value);

            if (expectedType == typeof(long))
                return Convert.ToInt64(value);

            if (expectedType == typeof(byte))
                return Convert.ToByte(value);

            if (expectedType == typeof(sbyte))
                return Convert.ToSByte(value);

            if (expectedType == typeof(short))
                return Convert.ToInt16(value);

            if (expectedType == typeof(ushort))
                return Convert.ToUInt16(value);

            if (expectedType == typeof(uint))
                return Convert.ToUInt32(value);

            if (expectedType == typeof(ulong))
                return Convert.ToUInt64(value);

            if (expectedType == typeof(decimal))
                return Convert.ToDecimal(value);

            if (expectedType == typeof(float))
                return Convert.ToSingle(value);

            if (expectedType == typeof(double))
                return Convert.ToDouble(value);

            throw new NotSupportedException();
        }

        public object ToSqlValue(object value, Type type)
        {
            return value;
        }
    }
}