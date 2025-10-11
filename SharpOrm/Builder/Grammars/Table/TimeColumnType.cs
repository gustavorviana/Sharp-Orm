using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Represents a column type mapping for time types including TimeSpan and TimeOnly (NET6.0+).
    /// </summary>
    public class TimeColumnType : IColumnTypeMap
    {
        private readonly string _raw;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeColumnType"/> class.
        /// </summary>
        /// <param name="raw">The raw SQL type for time types.</param>
        public TimeColumnType(string raw)
        {
            _raw = raw;
        }

        /// <summary>
        /// Builds the SQL column type definition for time columns.
        /// </summary>
        /// <param name="column">The data column to build the type for.</param>
        /// <returns>The SQL column type string.</returns>
        public string Build(DataColumn column) => _raw;

        /// <summary>
        /// Determines whether this type mapper can handle the specified .NET type.
        /// </summary>
        /// <param name="type">The .NET type to check.</param>
        /// <returns>True if the type is TimeSpan or TimeOnly (NET6.0+); otherwise, false.</returns>
        public bool CanWork(Type type)
        {
            if (type == typeof(TimeSpan))
                return true;

#if NET6_0_OR_GREATER
            return type == typeof(TimeOnly);
#else
            return false;
#endif
        }
    }
}
