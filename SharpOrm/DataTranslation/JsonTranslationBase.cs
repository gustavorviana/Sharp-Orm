using System;

namespace SharpOrm.DataTranslation
{
    public abstract class JsonTranslationBase : ISqlTranslation
    {
        public bool CanWork(Type type) => true;

        public object FromSqlValue(object value, Type expectedType)
        {
            if (value == null || value == DBNull.Value)
                return null;

            return Deserialize(value.ToString(), expectedType);
        }

        public object ToSqlValue(object value, Type type)
        {
            if (value == null)
                return null;

            return Serialize(value);
        }

        protected abstract object Deserialize(string value, Type type);

        protected abstract string Serialize(object value);
    }
}
