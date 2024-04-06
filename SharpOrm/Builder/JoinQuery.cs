namespace SharpOrm.Builder
{
    public class JoinQuery : QueryBase, IGrammarOptions
    {
        public object GrammarOptions { get; set; }
        public string Type { get; set; }

        public JoinQuery(QueryConfig config, string table) : this(config, new DbName(table))
        {
        }

        public JoinQuery(QueryConfig config, DbName table) : base(config, table)
        {
        }
    }
}
