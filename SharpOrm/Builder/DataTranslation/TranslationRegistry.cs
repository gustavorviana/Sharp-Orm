using System;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Class responsible for translating data between the database and code.
    /// </summary>
    public class TranslationRegistry
    {
        private static TranslationRegistry _default = new TranslationRegistry();

        public static TranslationRegistry Default
        {
            get => _default;
            set => _default = value ?? throw new ArgumentNullException(nameof(Default));
        }

        private readonly NativeSqlTranslation native = new NativeSqlTranslation();

        /// <summary>
        /// Custom value translators.
        /// </summary>
        public ISqlTranslation[] Translators { get; set; } = new ISqlTranslation[0];

        /// <summary>
        /// Format in which the GUID should be read and written in the database.
        /// </summary>
        /// <value>Default value in C#: "D"</value>
        public string GuidFormat
        {
            get => native.GuidFormat;
            set => native.GuidFormat = value;
        }

        /// <summary>
        /// Timezone in which dates should be stored in the database.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo DbTimeZone
        {
            get => native.DbTimeZone;
            set => native.DbTimeZone = value;
        }

        /// <summary>
        /// Timezone in which dates should be converted to work within the code.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo TimeZone
        {
            get => native.TimeZone;
            set => native.TimeZone = value;
        }

        /// <summary>
        /// Converts a C# value to the database.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">It is thrown when there is no converter for the requested value.</exception>
        public object ToSql(object value)
        {
            if (value is null || value is DBNull)
                return DBNull.Value;

            Type type = value?.GetType();

            if (this.GetFor(type) is ISqlTranslation conversor)
                return conversor.ToSqlValue(value, type);

            throw new NotSupportedException($"Type \"{type.FullName}\" is not supported");
        }

        /// <summary>
        /// Converts a value from the database to C#.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <param name="expectedType">Expected type in C#.</param>
        /// <returns></returns>
        public object FromSql(object value)
        {
            if (value is null || value is DBNull)
                return null;

            Type expectedType = value.GetType();

            if (this.GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.FromSqlValue(value, expectedType);

            return value;
        }

        /// <summary>
        /// Converts a value from the database to C#.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <param name="expectedType">Expected type in C#.</param>
        /// <returns></returns>
        public object FromSql(object value, Type expectedType)
        {
            if (value is null || value is DBNull)
                return null;

            expectedType = GetValidTypeFor(expectedType);

            if (this.GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.FromSqlValue(value, expectedType);

            return value;
        }

        /// <summary>
        /// Retrieves a converter for the specified type.
        /// </summary>
        /// <param name="type">The type to be passed to the converter.</param>
        /// <returns></returns>
        public ISqlTranslation GetFor(Type type)
        {
            type = GetValidTypeFor(type);

            if (this.Translators?.FirstOrDefault(c => c.CanWork(type)) is ISqlTranslation conversor)
                return conversor;

            if (native.CanWork(type))
                return native;

            return null;
        }

        public static Type GetValidTypeFor(Type expectedType)
        {
            if (expectedType != null && Nullable.GetUnderlyingType(expectedType) is Type underlyingType)
                return underlyingType;

            return expectedType;
        }

        /// <summary>
        /// Retrieves a value converter for a MemberInfo.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static ISqlTranslation GetOf(MemberInfo member)
        {
            if (member.GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute attribute)
                return (ISqlTranslation)Activator.CreateInstance(attribute.Type);

            return null;
        }

        /// <summary>
        /// Converts the date to the local timezone or the database timezone.
        /// </summary>
        /// <param name="value">The date that should be converted.</param>
        /// <param name="toSql">True to convert to the database timezone; False to convert to the code's timezone.</param>
        /// <returns></returns>
        public DateTime ConvertDate(DateTime value, bool toSql)
        {
            return (DateTime)(toSql ? this.native.ToSqlValue(value, typeof(DateTime)) : this.native.FromSqlValue(value, typeof(DateTime)));
        }
    }
}
