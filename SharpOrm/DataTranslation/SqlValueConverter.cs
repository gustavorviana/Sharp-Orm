using SharpOrm.Builder;
using System;
using System.Data;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Abstract base class that provides bidirectional conversion between C# types and SQL types.
    /// Combines SQL translation capabilities with column type mapping for database schema generation.
    /// Doc: https://github.com/gustavorviana/Sharp-Orm/wiki/Custom-SQL-Translation
    /// </summary>
    public abstract class SqlValueConverter : ISqlTranslation, IColumnTypeMap
    {
        /// <summary>
        /// Gets the database type that this converter works with.
        /// </summary>
        protected abstract Type DatabaseType { get; }

        /// <summary>
        /// Determines whether this converter can handle the specified .NET type.
        /// </summary>
        /// <param name="type">The .NET type to check.</param>
        /// <returns>True if the converter can handle the type; otherwise, false.</returns>
        protected abstract bool CanWork(Type type);

        bool ISqlTranslation.CanWork(Type type) => type == DatabaseType || CanWork(type);
        bool ICanWork<Type>.CanWork(Type type) => type == DatabaseType || CanWork(type);

        /// <summary>
        /// Converts a SQL value to the expected .NET type.
        /// </summary>
        /// <param name="value">The SQL value to convert.</param>
        /// <param name="expectedType">The expected .NET type.</param>
        /// <returns>The converted .NET value.</returns>
        public abstract object FromSqlValue(object value, Type expectedType);

        /// <summary>
        /// Converts a .NET value to a SQL-compatible value.
        /// </summary>
        /// <param name="value">The .NET value to convert.</param>
        /// <returns>The SQL-compatible value.</returns>
        protected abstract object ToSqlValue(object value);

        /// <summary>
        /// Builds the SQL column type definition for this converter.
        /// </summary>
        /// <param name="column">The data column to build the type for.</param>
        /// <returns>The SQL column type string.</returns>
        protected abstract string BuildSqlColumn(DataColumn column);

        string IColumnTypeMap.Build(DataColumn column) => BuildSqlColumn(column);

        object ISqlTranslation.ToSqlValue(object value, Type type)
            => ToSqlValue(value);
    }
}
