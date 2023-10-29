namespace SharpOrm.Builder
{
    public interface IGrammarOptions
    {
        /// <summary>
        /// Options for customizing the execution of the grammar.
        /// </summary>
        /// <remarks>For example: SqlServerGrammarOptions.NoLock to have queries written with NOLOCK.</remarks>
        object GrammarOptions { get; set; }
    }
}
