using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents the information for building a SQL query.
    /// </summary>
    public class QueryInfo
    {
        private IReadonlyQueryInfo _queryInfo;

        /// <summary>
        /// Gets the WHERE clause builder.
        /// </summary>
        public QueryBuilder Where { get; }

        /// <summary>
        /// Gets the HAVING clause builder.
        /// </summary>
        public QueryBuilder Having { get; }

        /// <summary>
        /// Gets or sets the columns to group by.
        /// </summary>
        public Column[] GroupsBy { get; set; } = new Column[0];

        /// <summary>
        /// Gets the list of join queries.
        /// </summary>
        public List<JoinQuery> Joins { get; } = new List<JoinQuery>();

        /// <summary>
        /// Gets or sets the columns to order by.
        /// </summary>
        public ColumnOrder[] Orders { get; set; } = new ColumnOrder[0];

        /// <summary>
        /// Gets or sets the columns to select.
        /// </summary>
        public Column[] Select { get; set; } = new Column[] { Column.All };

        /// <summary>
        /// Gets the query configuration.
        /// </summary>
        public QueryConfig Config { get; }

        /// <summary>
        /// Gets the database table name.
        /// </summary>
        public DbName TableName { get; }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string From => this.TableName.Name;

        /// <summary>
        /// Gets the table alias.
        /// </summary>
        public string Alias => this.TableName.Alias;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryInfo"/> class with the specified configuration and table name.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="table">The table name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the configuration or table name is null.</exception>
        public QueryInfo(QueryConfig config, DbName table)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this._queryInfo = new ReadonlyQueryInfo(config, table);
            this.TableName = table;

            this.Where = new QueryBuilder(this.ToReadOnly());
            this.Having = new QueryBuilder(this.ToReadOnly());
        }

        /// <summary>
        /// Loads the query information from another <see cref="QueryInfo"/> instance.
        /// </summary>
        /// <param name="info">The query information to load from.</param>
        internal void LoadFrom(QueryInfo info)
        {
            this.Joins.Clear();
            this.Where.Clear();
            this.Having.Clear();

            this.Where.Add(info.Where);
            this.Having.Add(info.Having);
            this.Joins.AddRange(info.Joins);
            this.Select = (Column[])info.Select.Clone();
            this.GroupsBy = (Column[])info.GroupsBy.Clone();
            this.Orders = (ColumnOrder[])info.Orders.Clone();
        }

        /// <summary>
        /// Converts the query information to a read-only format.
        /// </summary>
        /// <returns>The read-only query information.</returns>
        public IReadonlyQueryInfo ToReadOnly()
        {
            return this._queryInfo;
        }

        /// <summary>
        /// Determines whether the query is a count query.
        /// </summary>
        /// <returns>True if the query is a count query; otherwise, false.</returns>
        internal bool IsCount()
        {
            if (this.Select.Length != 1)
                return false;

            string select = this.Select[0].ToExpression(this.ToReadOnly()).ToString().ToLower();
            return select.StartsWith("count(");
        }
    }
}