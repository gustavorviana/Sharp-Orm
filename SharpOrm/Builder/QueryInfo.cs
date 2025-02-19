using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents the information for building a SQL query.
    /// </summary>
    public class QueryInfo : QueryBaseInfo, IRootTypeMap
    {
        Type IRootTypeMap.RootType { get; set; }

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
        /// Initializes a new instance of the <see cref="QueryInfo"/> class with the specified configuration and table name.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="table">The table name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the configuration or table name is null.</exception>
        public QueryInfo(QueryConfig config, DbName table) : base(config, table)
        {
            Having = new QueryBuilder(ToReadOnly());
        }

        /// <summary>
        /// Loads the query information from another <see cref="QueryInfo"/> instance.
        /// </summary>
        /// <param name="info">The query information to load from.</param>
        internal void LoadFrom(QueryInfo info)
        {
            Joins.Clear();
            Where.Clear();
            Having.Clear();

            info.Where.ApplyTo(Where);
            Having.Add(info.Having);
            Joins.AddRange(info.Joins);
            Select = (Column[])info.Select.Clone();
            GroupsBy = (Column[])info.GroupsBy.Clone();
            Orders = (ColumnOrder[])info.Orders.Clone();
        }

        /// <summary>
        /// Determines whether the query is a count query.
        /// </summary>
        /// <returns>True if the query is a count query; otherwise, false.</returns>
        internal bool IsCount()
        {
            if (Select.Length != 1)
                return false;

            string select = Select[0].ToExpression(ToReadOnly()).ToString();
            return select.StartsWith("count(", StringComparison.OrdinalIgnoreCase);
        }
    }
}