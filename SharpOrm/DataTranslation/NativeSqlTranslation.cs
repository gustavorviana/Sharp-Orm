using System;
using System.Linq;

namespace SharpOrm.DataTranslation
{
    internal class NativeSqlTranslation : ISqlTranslation
    {
        private static readonly BinaryTranslator _binaryTranslator = new BinaryTranslator();
        internal readonly NumericTranslation _numericTranslation = new NumericTranslation();
        internal readonly DateTranslation _dateTranslation = new DateTranslation();
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
            get => _dateTranslation.DbTimeZone;
            set => _dateTranslation.DbTimeZone = value;
        }

        /// <summary>
        /// Timezone in which dates should be converted to work within the code.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo TimeZone
        {
            get => _dateTranslation.CodeTimeZone;
            set => _dateTranslation.CodeTimeZone = value;
        }

        /// <summary>
        /// Indicates whether empty strings should be converted to null values.
        /// </summary>
        public bool EmptyStringToNull { get; set; }

        /// <summary>
        /// Specifies the behavior when an invalid value is encountered during Guid or Enum conversion.
        /// </summary>
        /// <value>Default is <see cref="InvalidValueBehavior.ThrowException"/>.</value>
        public InvalidValueBehavior InvalidValueBehavior { get; set; } = InvalidValueBehavior.ReturnDefault;

        public bool CanWork(Type type) => TranslationUtils.IsNative(type, true) || _binaryTranslator.CanWork(type) || _dateTranslation.CanWork(type);

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
                return ParseGuid(value);

            if (_numericTranslation.CanWork(expectedType))
                return _numericTranslation.FromSqlValue(value, expectedType);

            if (_binaryTranslator.CanWork(expectedType))
                return _binaryTranslator.FromSqlValue(value, expectedType);

            if (_dateTranslation.CanWork(expectedType))
                return _dateTranslation.FromSqlValue(value, expectedType);

            return value;
        }

        private object ParseEnum(object value, Type expectedType)
        {
            if (value is string strVal && !IsNumericString(strVal))
            {
                if (Enum.IsDefined(expectedType, value))
                    return Enum.Parse(expectedType, strVal);

                if (InvalidValueBehavior == InvalidValueBehavior.ReturnDefault)
                    return Activator.CreateInstance(expectedType);

                throw new ArgumentException(
                    $"The value '{strVal}' is not a valid member of enum '{expectedType.Name}'.",
                    nameof(value)
                );
            }

            try
            {
                var underlying = Enum.GetUnderlyingType(expectedType);
                value = Convert.ChangeType(value, underlying);
            }
            catch (FormatException)
            {
                if (InvalidValueBehavior == InvalidValueBehavior.ThrowException)
                    throw;

                return Activator.CreateInstance(expectedType);
            }

            if (Enum.IsDefined(expectedType, value))
                return Enum.ToObject(expectedType, value);

            if (InvalidValueBehavior == InvalidValueBehavior.ReturnDefault)
                return Activator.CreateInstance(expectedType);

            throw new ArgumentException(
                $"The value '{value}' is not a valid member of enum '{expectedType.Name}'.",
                nameof(value)
            );
        }

        private Guid ParseGuid(object value)
        {
            if (value is Guid guid)
                return guid;

            if (value is string strGuid)
            {
                try
                {
                    return Guid.Parse(strGuid);
                }
                catch (FormatException)
                {
                    if (InvalidValueBehavior == InvalidValueBehavior.ReturnDefault)
                        return default(Guid);

                    throw new ArgumentException($"Cannot convert invalid value '{strGuid}' to Guid", nameof(value));
                }
            }

            if (InvalidValueBehavior == InvalidValueBehavior.ReturnDefault)
                return default(Guid);

            throw new ArgumentException($"Cannot convert invalid value to Guid", nameof(value));
        }

        private static bool IsNumericString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            return value.All(char.IsNumber);
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
                return _numericTranslation.ToSqlValue(value, type);

            if (_binaryTranslator.CanWork(type))
                return _binaryTranslator.ToSqlValue(value, type);

            if (_dateTranslation.CanWork(type))
            {
                value = _dateTranslation.ToSqlValue(value, type);
                if (value?.GetType() != typeof(string))
                    return value;
            }

            return StringToSql(value);
        }

        private bool IsNumeric(object value, Type type)
        {
            return _numericTranslation.CanWork(type) && (!(value is string strVal) || TranslationUtils.IsNumericString(strVal));
        }

        private object StringToSql(object value)
        {
            if (!(value is string strValue))
                return value.ToString();

            if (EmptyStringToNull && string.IsNullOrEmpty(strValue))
                return DBNull.Value;

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
