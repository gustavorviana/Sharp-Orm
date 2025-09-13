using System;
using System.Data;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Interface for mapping .NET types to SQL column types.
    /// </summary>
    public interface IColumnTypeMap : ICanWork<Type>
    {
        /// <summary>
        /// Perform the conversion of DataColumn information to a string representing the SQL type in the database.
        /// </summary>
        /// <param name="column">The DataColumn to convert.</param>
        /// <returns>A string representing the SQL type.</returns>
        string Build(DataColumn column);
    }
}
