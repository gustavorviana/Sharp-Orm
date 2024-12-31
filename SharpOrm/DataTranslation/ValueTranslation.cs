using SharpOrm.Builder;
using System;

namespace SharpOrm.DataTranslation
{
    public class ValueTranslation<TModel, TProvider> : ISqlTranslation
    {
        private readonly Func<TModel, TProvider> _convertToSql;
        private readonly Func<TProvider, TModel> _convertFromSql;

        public bool CanWork(Type type) => type == typeof(TModel) || type == typeof(TProvider);

        public ValueTranslation(Func<TModel, TProvider> convertToSql, Func<TProvider, TModel> convertFromSql)
        {
            _convertToSql = convertToSql ?? throw new ArgumentNullException(nameof(convertToSql));
            _convertFromSql = convertFromSql ?? throw new ArgumentNullException(nameof(convertFromSql));
        }

        public object FromSqlValue(object value, Type expectedType)
        {
            if ((value is null || value is DBNull) && !ReflectionUtils.IsNullable(typeof(TModel)))
                value = ReflectionUtils.GetDefault(expectedType);

            return _convertFromSql((TProvider)value);
        }

        public object ToSqlValue(object value, Type type)
        {
            if ((value is null || value is DBNull) && !ReflectionUtils.IsNullable(typeof(TProvider)))
                value = ReflectionUtils.GetDefault(type);

            return _convertToSql((TModel)value);
        }
    }
}
