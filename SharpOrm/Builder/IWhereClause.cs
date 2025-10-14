namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a query that supports WHERE clause operations.
    /// </summary>
    /// <typeparam name="TThis">The type of the implementing class for fluent interface.</typeparam>
    public interface IWhereClause<TThis> where TThis : IWhereClause<TThis>
    {
        /// <summary>
        /// Adds a WHERE clause with the specified column, operation, and value.
        /// </summary>
        /// <param name="column">The column to compare.</param>
        /// <param name="operation">The comparison operation.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The current query instance.</returns>
        TThis Where(object column, string operation, object value);

        /// <summary>
        /// Adds a WHERE clause using a SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression.</param>
        /// <returns>The current query instance.</returns>
        TThis Where(SqlExpression expression);

        /// <summary>
        /// Adds an OR WHERE clause with the specified column, operation, and value.
        /// </summary>
        /// <param name="column">The column to compare.</param>
        /// <param name="operation">The comparison operation.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The current query instance.</returns>
        TThis OrWhere(object column, string operation, object value);

        /// <summary>
        /// Adds an OR WHERE clause using a SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression.</param>
        /// <returns>The current query instance.</returns>
        TThis OrWhere(SqlExpression expression);
    }
}
