using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a typed query that supports WHERE clause operations with lambda expressions.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TThis">The type of the implementing class for fluent interface.</typeparam>
    public interface ITypedWhereClause<T, TThis> : IWhereClause<TThis>
        where TThis : ITypedWhereClause<T, TThis>
    {
        /// <summary>
        /// Adds a WHERE clause using a column expression, operation, and value.
        /// </summary>
        /// <typeparam name="TValue">The type of the column value.</typeparam>
        /// <param name="columnExpression">The column expression.</param>
        /// <param name="operation">The comparison operation.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The current query instance.</returns>
        TThis Where<TValue>(Expression<ColumnExpression<T, TValue>> columnExpression, string operation, TValue value);

        /// <summary>
        /// Adds an OR WHERE clause using a column expression, operation, and value.
        /// </summary>
        /// <typeparam name="TValue">The type of the column value.</typeparam>
        /// <param name="columnExpression">The column expression.</param>
        /// <param name="operation">The comparison operation.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The current query instance.</returns>
        TThis OrWhere<TValue>(Expression<ColumnExpression<T, TValue>> columnExpression, string operation, TValue value);

        /// <summary>
        /// Applies WHERE conditions using a callback that configures a WhereBuilder.
        /// </summary>
        /// <param name="callback">Callback function to configure the WhereBuilder.</param>
        /// <returns>The current query instance.</returns>
        TThis Where(QueryCallback<T> callback);

        /// <summary>
        /// Applies OR WHERE conditions using a callback that configures a WhereBuilder.
        /// </summary>
        /// <param name="callback">Callback function to configure the WhereBuilder.</param>
        /// <returns>The current query instance.</returns>
        TThis OrWhere(QueryCallback<T> callback);
    }
}
