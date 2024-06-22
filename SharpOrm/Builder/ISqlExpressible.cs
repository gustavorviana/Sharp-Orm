namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents an object that can be converted to a SQL expression.
    /// </summary>
    public interface ISqlExpressible
    {
        /// <summary>
        /// Converts the object to a SQL expression.
        /// </summary>
        /// <param name="info">The read-only query information.</param>
        /// <returns>The SQL expression.</returns>
        SqlExpression ToExpression(IReadonlyQueryInfo info);
    }
}
