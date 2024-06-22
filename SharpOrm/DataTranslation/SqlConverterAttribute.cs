using System;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Responsible for indicating which translator should be used for a field or property. Doc: https://github.com/gustavorviana/Sharp-Orm/wiki/Custom-SQL-Translation
    /// </summary>
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
