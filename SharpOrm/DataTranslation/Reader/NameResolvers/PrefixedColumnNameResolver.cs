namespace SharpOrm.DataTranslation.Reader.NameResolvers
{
    /// <summary>
    /// Resolves column names by adding a fixed prefix to them.
    /// </summary>
    /// <remarks>
    /// This resolver formats column names as "{prefix}_{name}".
    /// </remarks>
    internal class PrefixedColumnNameResolver : INameResolver
    {
        /// <summary>
        /// Gets the prefix used for column name resolution.
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefixedColumnNameResolver"/> class.
        /// </summary>
        /// <param name="prefix">The prefix to prepend to column names.</param>
        public PrefixedColumnNameResolver(string prefix)
        {
            Prefix = prefix;
        }

        /// <summary>
        /// Resolves a column name by prepending the configured prefix.
        /// </summary>
        /// <param name="name">The column name to resolve.</param>
        /// <returns>The formatted column name in the pattern "{prefix}_{name}".</returns>
        public string Get(string name)
        {
            return $"{Prefix}_{name ?? string.Empty}";
        }
    }
}
