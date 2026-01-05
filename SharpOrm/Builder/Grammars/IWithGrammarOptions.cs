namespace SharpOrm.Builder.Grammars
{
    /// <summary>
    /// Provides options for customizing the execution of the grammar.
    /// </summary>
    public interface IWithGrammarOptions
    {
        /// <summary>
        /// Gets or sets the options for customizing the execution of the grammar.
        /// </summary>
        IGrammarOptions GrammarOptions { get; set; }
    }
}
