namespace SharpOrm.DataTranslation.Reader.NameResolvers
{
    /// <summary>
    /// Default implementation of <see cref="INameResolver"/> that returns the column name as-is without any transformation.
    /// </summary>
    internal class DefaultNameResolver : INameResolver
    {
        /// <summary>
        /// Returns the column name without any transformation.
        /// </summary>
        /// <param name="name">The column name to resolve.</param>
        /// <returns>The original column name, or an empty string if the name is null.</returns>
        public string Get(string name) => name ?? string.Empty;
    }
}
