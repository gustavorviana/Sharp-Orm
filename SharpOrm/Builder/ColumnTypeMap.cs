using System;
using System.Data;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Class responsible for converting the type and size of a C# object to a database type.
    /// </summary>
    public abstract class ColumnTypeMap
    {
        /// <summary>
        /// Check if the requested type is compatible.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract bool CanWork(Type type);

        /// <summary>
        /// Perform the conversion of DataColumn information to a string representing the SQL type in the database.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public abstract string GetTypeString(DataColumn column);
    }
}
