namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// Represents the grammar options for SQL Server.
    /// </summary>
    public class SqlServerGrammarOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the NOLOCK option is enabled.
        /// </summary>
        public bool NoLock { get; set; }

        internal static void WriteTo(QueryBuilder builder, QueryBase query)
        {
            if (query is IGrammarOptions iOpt && iOpt.GrammarOptions is SqlServerGrammarOptions options)
                options.WriteTo(builder);
        }

        internal void WriteTo(QueryBuilder builder)
        {
            if (NoLock)
                builder.Add(" WITH (NOLOCK)");
        }
    }
}
