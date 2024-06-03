namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents an object that can be converted to a SQL expression with an optional alias.
    /// </summary>
    public interface ISqlExpressibleAlias : ISqlExpressible
    {
        /// <summary>
        /// Converts the object to a SQL expression with an optional alias.
        /// </summary>
        /// <param name="info">The read-only query information.</param>
        /// <param name="alias">Whether to include the alias in the expression.</param>
        /// <returns>The SQL expression.</returns>
        SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias);
    }
}
