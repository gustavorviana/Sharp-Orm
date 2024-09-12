using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    public class QueryBaseInfo : IReadonlyQueryInfo
    {
        /// <summary>
        /// Gets the WHERE clause builder.
        /// </summary>
        public QueryBuilder Where { get; }

        /// <summary>
        /// Gets the query configuration.
        /// </summary>
        public QueryConfig Config { get; }

        /// <summary>
        /// Gets the database table name.
        /// </summary>
        public DbName TableName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryInfo"/> class with the specified configuration and table name.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="table">The table name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the configuration or table name is null.</exception>
        public QueryBaseInfo(QueryConfig config, DbName table)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this.TableName = table;

            this.Where = new QueryBuilder(this.ToReadOnly());
        }

        /// <summary>
        /// Converts the query information to a read-only format.
        /// </summary>
        /// <returns>The read-only query information.</returns>
        public IReadonlyQueryInfo ToReadOnly()
        {
            return this;
        }
    }
}
