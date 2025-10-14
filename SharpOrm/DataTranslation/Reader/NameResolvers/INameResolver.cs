namespace SharpOrm.DataTranslation.Reader.NameResolvers
{
    /// <summary>
    /// Defines a contract for resolving column names with specific formatting rules.
    /// </summary>
    internal interface INameResolver
    {
        /// <summary>
        /// Resolves and formats a column name according to the resolver's rules.
        /// </summary>
        /// <param name="name">The base column name to resolve. Can be null.</param>
        /// <returns>The formatted column name.</returns>
        string Get(string name = null);
    }
}
