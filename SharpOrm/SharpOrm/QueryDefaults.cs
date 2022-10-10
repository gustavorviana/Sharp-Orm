using SharpOrm.Builder;
using System.Data.Common;

namespace SharpOrm
{
    /// <summary>
    /// Default configuration for Query.
    /// </summary>
    public static class QueryDefaults
    {
        /// <summary>
        /// IQueryConfig defaults to "Query". The default object is "DefaultQueryConfig"
        /// </summary>
        public static IQueryConfig Config { get; set; } = new DefaultQueryConfig();

        /// <summary>
        /// Default connection to a Query object. The default connection is null.
        /// </summary>
        public static DbConnection Connection { get; set; }
    }
}