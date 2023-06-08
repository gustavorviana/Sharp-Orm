using System;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation
{
    public class TranslationConfig
    {
        private static readonly NativeSqlValueConversor native = new NativeSqlValueConversor();
        public ISqlTranslation[] Translators { get; set; } = new ISqlTranslation[0];

        public object ToSql(object value)
        {
            Type type = value?.GetType();

            if (this.GetFor(type) is ISqlTranslation conversor)
                return conversor.ToSqlValue(value, type);

            throw new NotSupportedException($"Type \"{type.FullName}\" is not supported");
        }

        public object FromSql(object value, Type expectedType)
        {
            if (expectedType != null && Nullable.GetUnderlyingType(expectedType) is Type underlyingType)
                expectedType = underlyingType;

            if (this.GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.FromSqlValue(value, expectedType);

            return value;
        }

        public ISqlTranslation GetFor(Type type)
        {
            if (this.Translators?.FirstOrDefault(c => c.CanWork(type)) is ISqlTranslation conversor)
                return conversor;

            if (native.CanWork(type))
                return native;

            return null;
        }

        public ISqlTranslation GetOf(MemberInfo property)
        {
            if (property.GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute attribute)
                return (ISqlTranslation)Activator.CreateInstance(attribute.Type);

            return null;
        }
    }
}
