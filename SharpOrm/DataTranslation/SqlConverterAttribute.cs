using System;

namespace SharpOrm.DataTranslation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class SqlConverterAttribute : Attribute
    {
        public Type Type { get; }
        public SqlConverterAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
