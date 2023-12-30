using System;

namespace SharpOrm.Builder.DataTranslation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class SqlConverterAttribute : Attribute
    {
        public Type Type { get; }
        public SqlConverterAttribute(Type type)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
