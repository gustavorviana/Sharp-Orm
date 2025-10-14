using SharpOrm.Builder.Expressions;
using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a query that works with a specific table type.
    /// </summary>
    /// <typeparam name="T">The entity type that represents the table.</typeparam>
    internal interface ITypedQuery<T> : IWithTableInfo
    {
        /// <summary>
        /// Converts a column expression to a Column object.
        /// </summary>
        /// <typeparam name="TValue">The type of the column value.</typeparam>
        /// <param name="columnExpression">The column expression.</param>
        /// <returns>A Column object representing the expression.</returns>
        Column GetColumn<TValue>(Expression<ColumnExpression<T, TValue>> columnExpression);

        /// <summary>
        /// Converts a column expression to an array of ExpressionColumn objects.
        /// </summary>
        /// <param name="expression">The column expression.</param>
        /// <returns>An array of ExpressionColumn objects.</returns>
        ExpressionColumn[] GetColumns(Expression<ColumnExpression<T>> expression);

        /// <summary>
        /// Converts a column expression to an array of ExpressionColumn objects with specific configuration.
        /// </summary>
        /// <param name="expression">The column expression.</param>
        /// <param name="config">The expression configuration.</param>
        /// <returns>An array of ExpressionColumn objects.</returns>
        ExpressionColumn[] GetColumns(Expression<ColumnExpression<T>> expression, ExpressionConfig config);
    }
}
