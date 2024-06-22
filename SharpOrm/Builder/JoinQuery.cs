namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a join query with configurable grammar options.
    /// </summary>
    public class JoinQuery : QueryBase, IGrammarOptions
    {
        /// <summary>
        /// Gets or sets the options for customizing the execution of the grammar.
        /// </summary>
        public object GrammarOptions { get; set; }

        /// <summary>
        /// Gets or sets the type of the join.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinQuery"/> class with the specified configuration and table name.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="table">The name of the table.</param>
        public JoinQuery(QueryConfig config, string table) : this(config, new DbName(table))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinQuery"/> class with the specified configuration and table name.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="table">The name of the table.</param>
        public JoinQuery(QueryConfig config, DbName table) : base(config, table)
        {
        }
    }
}
