using System;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation
{
    public class TranslationRegistry
    {
        private static TranslationRegistry _default = new TranslationRegistry();

        public static TranslationRegistry Default
        {
            get => _default;
            set => _default = value ?? throw new ArgumentNullException(nameof(Default));
        }

        private readonly NativeSqlTranslation native = new NativeSqlTranslation();
        public ISqlTranslation[] Translators { get; set; } = new ISqlTranslation[0];
        public string GuidFormat
        {
            get => native.GuidFormat;
            set => native.GuidFormat = value;
        }

        public TimeZoneInfo TimeZone
        {
            get => native.TimeZone;
            set => native.TimeZone = value;
        }

        public object ToSql(object value)
        {
            if (value is null || value is DBNull)
                return DBNull.Value;

            Type type = value?.GetType();

            if (this.GetFor(type) is ISqlTranslation conversor)
                return conversor.ToSqlValue(value, type);

            throw new NotSupportedException($"Type \"{type.FullName}\" is not supported");
        }

        public object FromSql(object value, Type expectedType)
        {
            if (value is null || value is DBNull)
                return null;

            expectedType = GetValidTypeFor(expectedType);

            if (this.GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.FromSqlValue(value, expectedType);

            return value;
        }

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

        public static ISqlTranslation GetOf(MemberInfo property)
        {
            if (property.GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute attribute)
                return (ISqlTranslation)Activator.CreateInstance(attribute.Type);

            return null;
        }
    }
}
