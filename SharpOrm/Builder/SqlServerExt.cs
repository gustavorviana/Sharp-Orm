namespace SharpOrm.Builder
{
    public static class SqlServerExt
    {
        public static void EnableNoLock(this IGrammarOptions options)
        {
            options.GrammarOptions = new SqlServerGrammarOptions
            {
                NoLock = true
            };
        }

        public static bool IsNoLock(this IGrammarOptions options)
        {
            return options.GrammarOptions is SqlServerGrammarOptions opt && opt.NoLock;
        }
    }
}
