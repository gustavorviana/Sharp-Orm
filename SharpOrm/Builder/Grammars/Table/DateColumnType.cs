using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Represents a column type mapping for date and time types including DateTime, DateTimeOffset, and DateOnly (NET6.0+).
    /// </summary>
    public class DateColumnType : IColumnTypeMap
    {
        private readonly string _rawDateTimeOffset;
        private readonly string _rawDateTime;
        private readonly string _rawDateOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateColumnType"/> class.
        /// </summary>
        /// <param name="rawDateTimeOffset">The raw SQL type for DateTimeOffset.</param>
        /// <param name="rawDateTime">The raw SQL type for DateTime.</param>
        /// <param name="rawDateOnly">The raw SQL type for DateOnly (NET6.0+).</param>
        public DateColumnType(string rawDateTimeOffset, string rawDateTime, string rawDateOnly)
        {
            _rawDateTimeOffset = rawDateTimeOffset;
            _rawDateTime = rawDateTime;
            _rawDateOnly = rawDateOnly;
        }

        /// <summary>
        /// Builds the SQL column type definition based on the data column type.
        /// </summary>
        /// <param name="column">The data column to build the type for.</param>
        /// <returns>The SQL column type string.</returns>
        public string Build(DataColumn column)
        {
#if NET6_0_OR_GREATER
            if (column.DataType == typeof(DateOnly))
                return _rawDateOnly;
#endif

            return _rawDateTime;
        }

        /// <summary>
        /// Determines whether this type mapper can handle the specified .NET type.
        /// </summary>
        /// <param name="type">The .NET type to check.</param>
        /// <returns>True if the type is DateTime or DateOnly (NET6.0+); otherwise, false.</returns>
        public bool CanWork(Type type)
        {
            if (type == typeof(DateTime))
                return true;

#if NET6_0_OR_GREATER
            return type == typeof(DateOnly);
#else
            return false;
#endif
        }
    }
}
