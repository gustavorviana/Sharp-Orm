namespace SharpOrm.DataTranslation.Reader.NameResolvers
{
    /// <summary>
    /// Resolves column names for parent table relationships by prefixing them with a parent identifier.
    /// </summary>
    /// <remarks>
    /// This resolver formats column names as "{prefix}_c_{name}" for regular columns,
    /// or "{prefix}_" when no column name is provided.
    /// </remarks>
    internal class ParentColumnNameResolver : INameResolver
    {
        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentColumnNameResolver"/> class.
        /// </summary>
        /// <param name="prefix">The prefix to use for parent table identification.</param>
        public ParentColumnNameResolver(string prefix)
        {
            _prefix = prefix;
        }

        /// <summary>
        /// Resolves a column name by adding the parent table prefix.
        /// </summary>
        /// <param name="name">The column name to resolve.</param>
        /// <returns>
        /// The formatted column name in the pattern "{prefix}_c_{name}" if a name is provided,
        /// or "{prefix}_" if the name is null or empty.
        /// </returns>
        public string Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return $"{_prefix}_";

            return $"{_prefix}_c_{name}";
        }
    }
}
