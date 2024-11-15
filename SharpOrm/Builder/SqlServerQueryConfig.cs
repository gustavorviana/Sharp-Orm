using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides configuration for building SQL Server queries.
    /// </summary>
    public class SqlServerQueryConfig : QueryConfig
    {
        private const char StrDelimitor = '\'';
        /// <summary>
        /// Gets or sets a value indicating whether to use old pagination without LIMIT and OFFSET, using only ROW_NUMBER().
        /// </summary>
        public bool UseOldPagination { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerQueryConfig"/> class.
        /// </summary>
        public SqlServerQueryConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerQueryConfig"/> class with a flag indicating if only safe modifications are allowed.
        /// </summary>
        /// <param name="onlySafeModifications">If true, only safe modifications are allowed.</param>
        public SqlServerQueryConfig(bool onlySafeModifications) : base(onlySafeModifications)
        {
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('[', ']');
        }

        public override Grammar NewGrammar(Query query)
        {
            return new SqlServerGrammar(query);
        }

        public override TableGrammar NewTableGrammar(TableSchema schema)
        {
            return new SqlServerTableGrammar(this, schema);
        }

        /// <summary>
        /// creates a connection line for the local connection: Data Source=localhost\SQLEXPRESS;Initial Catalog={catalog};Integrated Security=True;
        /// </summary>
        /// <param name="initialCatalog"></param>
        /// <returns></returns>
        public static string GetLocalConnectionString(string initialCatalog)
        {
            return $@"Data Source=localhost;Initial Catalog={initialCatalog};Integrated Security=True";
        }

        public override string EscapeString(string value) => BasicEscapeString(value, StrDelimitor);

        public override QueryConfig Clone(bool? safeOperations = null)
        {
            var clone = new SqlServerQueryConfig(safeOperations ?? this.OnlySafeModifications);
            this.CopyTo(clone);
            return clone;
        }
    }
}
