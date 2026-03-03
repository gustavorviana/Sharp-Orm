namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerGrammarBase : GrammarBase
    {
        public SqlServerGrammarBase(Query query) : base(query)
        {
        }

        protected void AddLimit()
        {
            if (Query.Limit is int limit && limit >= 0)
                Builder.Add(" TOP(").Add(limit).Add(')');
        }

        protected void WriteGrammarOptions(QueryBase query, bool isSelect)
        {
            if (GetOptions(query) is SqlServerGrammarOptions options)
                options.WriteTo(Builder, isSelect);
        }

        protected bool HasGrammarOptions()
        {
            return HasGrammarOptions(Query);
        }

        protected bool HasGrammarOptions(QueryBase query)
        {
            return GetOptions(query)?.HasHints() ?? false;
        }

        protected SqlServerGrammarOptions GetOptions()
        {
            return GetOptions(Query);
        }

        protected SqlServerGrammarOptions GetOptions(QueryBase query)
        {
            return (query as IWithGrammarOptions)?.GrammarOptions as SqlServerGrammarOptions;
        }
    }
}
