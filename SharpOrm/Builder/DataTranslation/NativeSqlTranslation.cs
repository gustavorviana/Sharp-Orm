using System;

namespace SharpOrm.Builder.DataTranslation
{
    internal class NativeSqlTranslation : ISqlTranslation
    {
        private static readonly BinaryTranslator binaryTranslator = new BinaryTranslator();
        private static readonly NumericTranslation numericTranslation = new NumericTranslation();
        internal readonly DateTranslation dateTranslation = new DateTranslation();

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
                return Enum.ToObject(expectedType, value);

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

            if (numericTranslation.CanWork(type))
                return numericTranslation.ToSqlValue(value, type);

            if (binaryTranslator.CanWork(type))
                return binaryTranslator.ToSqlValue(value, type);

            if (dateTranslation.CanWork(type) || value is string strVal && strVal.Length > 0)
                return dateTranslation.ToSqlValue(value, type);

            return value?.ToString();
        }
    }
}
