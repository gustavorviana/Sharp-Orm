using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Represents a column type mapping for enumeration types.
    /// </summary>
    public class EnumColumnType : IColumnTypeMap
    {
        private readonly string _raw;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumColumnType"/> class.
        /// </summary>
        /// <param name="raw">The raw SQL type for enum types.</param>
        public EnumColumnType(string raw)
        {
            _raw = raw;
        }

        /// <summary>
        /// Builds the SQL column type definition for enum columns.
        /// </summary>
        /// <param name="column">The data column to build the type for.</param>
        /// <returns>The SQL column type string.</returns>
        public string Build(DataColumn column) => _raw;

        /// <summary>
        /// Determines whether this type mapper can handle the specified .NET type.
        /// </summary>
        /// <param name="type">The .NET type to check.</param>
        /// <returns>True if the type is an enum; otherwise, false.</returns>
        public bool CanWork(Type type) => type.IsEnum;
    }
}
