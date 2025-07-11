﻿using System;
using System.Globalization;

namespace SharpOrm.DataTranslation
{
    internal class NativeSqlTranslation : ISqlTranslation
    {
        private static readonly BinaryTranslator binaryTranslator = new BinaryTranslator();
        internal readonly NumericTranslation numericTranslation = new NumericTranslation();
        internal readonly DateTranslation dateTranslation = new DateTranslation();
        public EnumSerialization EnumSerialization { get; set; } = EnumSerialization.Value;

        /// <summary>
        /// Format in which the GUID should be read and written in the database.
        /// </summary>
        /// <value>Default value in C#: "D".</value>
        /// <remarks>
        /// <list type="table">
        /// <item>N: 00000000000000000000000000000000</item>
        /// <item>D: 00000000-0000-0000-0000-000000000000</item>
        /// <item>B: {00000000-0000-0000-0000-000000000000}</item>
        /// <item>P: (00000000-0000-0000-0000-000000000000)</item>
        /// <item>X: {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}</item>
        /// </list>
        /// </remarks>
        public string GuidFormat { get; set; } = "D";

        /// <summary>
        /// Timezone in which dates should be stored in the database.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo DbTimeZone
        {
            get => dateTranslation.DbTimeZone;
            set => dateTranslation.DbTimeZone = value;
        }

        /// <summary>
        /// Timezone in which dates should be converted to work within the code.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo TimeZone
        {
            get => dateTranslation.CodeTimeZone;
            set => dateTranslation.CodeTimeZone = value;
        }

        /// <summary>
        /// Indicates whether empty strings should be converted to null values.
        /// </summary>
        public bool EmptyStringToNull { get; set; }

        public bool CanWork(Type type) => TranslationUtils.IsNative(type, true) || binaryTranslator.CanWork(type) || dateTranslation.CanWork(type);

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
                return this.ParseEnum(value, expectedType);

            if (expectedType == typeof(Guid))
                return value is Guid guid ? guid : Guid.Parse((string)value);

            if (numericTranslation.CanWork(expectedType))
                return numericTranslation.FromSqlValue(value, expectedType);

            if (binaryTranslator.CanWork(expectedType))
                return binaryTranslator.FromSqlValue(value, expectedType);

            if (dateTranslation.CanWork(expectedType))
                return dateTranslation.FromSqlValue(value, expectedType);

            return value;
        }

        private object ParseEnum(object value, Type expectedType)
        {
            if (value is string strVal)
                if (!IsNumericString(strVal)) return Enum.Parse(expectedType, strVal);
                else value = int.Parse(strVal);

            return Enum.ToObject(expectedType, value);
        }

        private static bool IsNumericString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            for (int i = 0; i < value.Length; i++)
                if (!char.IsNumber(value[i])) return false;

            return true;
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
                return SerializeEnum(value);

            if (value is bool vBool)
                return vBool ? 1 : 0;

            if (IsNumeric(value, type))
                return numericTranslation.ToSqlValue(value, type);

            if (binaryTranslator.CanWork(type))
                return binaryTranslator.ToSqlValue(value, type);

            if (dateTranslation.CanWork(type))
            {
                value = dateTranslation.ToSqlValue(value, type);
                if (value?.GetType() != typeof(string))
                    return value;
            }

            return StringToSql(value);
        }

        private bool IsNumeric(object value, Type type)
        {
            return numericTranslation.CanWork(type) && (!(value is string strVal) || TranslationUtils.IsNumericString(strVal));
        }

        private string StringToSql(object value)
        {
            if (!(value is string strValue))
                return value.ToString();

            if (EmptyStringToNull && string.IsNullOrEmpty(strValue))
                return null;

            return strValue;
        }

        private object SerializeEnum(object value)
        {
            if (EnumSerialization == DataTranslation.EnumSerialization.Value)
                return Convert.ToInt32(value);

            return value.ToString();
        }
    }
}
