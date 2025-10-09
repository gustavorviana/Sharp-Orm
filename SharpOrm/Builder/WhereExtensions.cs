using SharpOrm.Builder.Expressions;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Extension methods for WHERE clause operations.
    /// </summary>
    public static class WhereExtensions
    {
        #region Basic Where Extensions

        /// <summary>
        /// Adds a WHERE clause with the specified column and value (uses = operation).
        /// </summary>
        public static TThis Where<TThis>(this IWhereClause<TThis> query, object column, object value)
            where TThis : IWhereClause<TThis>
        {
            return query.Where(column, "=", value);
        }

        /// <summary>
        /// Adds a WHERE clause using a query builder.
        /// </summary>
        public static TThis Where<TThis>(this IWhereClause<TThis> query, QueryBuilder builder)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.Where(builder);
            return (TThis)query;
        }

        /// <summary>
        /// Adds a WHERE clause using an ISqlExpressible object.
        /// </summary>
        public static TThis Where<TThis>(this IWhereClause<TThis> query, ISqlExpressible expressible, bool allowAlias = false)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.Where(expressible, allowAlias);
            return (TThis)query;
        }

        /// <summary>
        /// Adds a WHERE clause using a callback function.
        /// </summary>
        public static TThis Where<TThis>(this IWhereClause<TThis> query, Action<TThis> callback)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.Where(q =>
            {
                callback((TThis)query);
            });
            return (TThis)query;
        }

        /// <summary>
        /// Adds an OR WHERE clause with the specified column and value (uses = operation).
        /// </summary>
        public static TThis OrWhere<TThis>(this IWhereClause<TThis> query, object column, object value)
            where TThis : IWhereClause<TThis>
        {
            return query.OrWhere(column, "=", value);
        }

        /// <summary>
        /// Adds an OR WHERE clause using a query builder.
        /// </summary>
        public static TThis OrWhere<TThis>(this IWhereClause<TThis> query, QueryBuilder builder)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.OrWhere(builder);
            return (TThis)query;
        }

        /// <summary>
        /// Adds an OR WHERE clause using an ISqlExpressible object.
        /// </summary>
        public static TThis OrWhere<TThis>(this IWhereClause<TThis> query, ISqlExpressible expressible, bool allowAlias = false)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.OrWhere(expressible, allowAlias);
            return (TThis)query;
        }

        /// <summary>
        /// Adds an OR WHERE clause using a callback function.
        /// </summary>
        public static TThis OrWhere<TThis>(this IWhereClause<TThis> query, Action<TThis> callback)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.OrWhere(q =>
            {
                callback((TThis)query);
            });
            return (TThis)query;
        }

        #endregion

        #region Where IN/NOT IN/NULL Extensions

        /// <summary>
        /// Adds a WHERE IN clause.
        /// </summary>
        public static TThis WhereIn<TThis>(this IWhereClause<TThis> query, object column, params object[] values)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.WhereIn(column, values);
            return (TThis)query;
        }

        /// <summary>
        /// Adds a WHERE NOT IN clause.
        /// </summary>
        public static TThis WhereNotIn<TThis>(this IWhereClause<TThis> query, object column, params object[] values)
            where TThis : IWhereClause<TThis>
        {
            var baseQuery = query as QueryBase;
            if (baseQuery == null)
                throw new InvalidOperationException("Query must inherit from QueryBase");

            baseQuery.WhereNotIn(column, values);
            return (TThis)query;
        }

        /// <summary>
        /// Adds a WHERE NULL clause.
        /// </summary>
        public static TThis WhereNull<TThis>(this IWhereClause<TThis> query, object column)
            where TThis : IWhereClause<TThis>
        {
            return query.Where(column, "IS", null);
        }

        /// <summary>
        /// Adds a WHERE NOT NULL clause.
        /// </summary>
        public static TThis WhereNotNull<TThis>(this IWhereClause<TThis> query, object column)
            where TThis : IWhereClause<TThis>
        {
            return query.Where(column, "IS NOT", null);
        }

        /// <summary>
        /// Adds an OR WHERE NULL clause.
        /// </summary>
        public static TThis OrWhereNull<TThis>(this IWhereClause<TThis> query, object column)
            where TThis : IWhereClause<TThis>
        {
            return query.OrWhere(column, "IS", null);
        }

        /// <summary>
        /// Adds an OR WHERE NOT NULL clause.
        /// </summary>
        public static TThis OrWhereNotNull<TThis>(this IWhereClause<TThis> query, object column)
            where TThis : IWhereClause<TThis>
        {
            return query.OrWhere(column, "IS NOT", null);
        }

        #endregion

        #region Typed Where Extensions

        /// <summary>
        /// Adds a WHERE clause using a column expression and value (uses = operation).
        /// </summary>
        public static TThis Where<T, TThis, TValue>(this ITypedWhereClause<T, TThis> query,
            Expression<ColumnExpression<T, TValue>> columnExpression, TValue value)
            where TThis : ITypedWhereClause<T, TThis>
        {
            return query.Where(columnExpression, "=", value);
        }

        /// <summary>
        /// Adds an OR WHERE clause using a column expression and value (uses = operation).
        /// </summary>
        public static TThis OrWhere<T, TThis, TValue>(this ITypedWhereClause<T, TThis> query,
            Expression<ColumnExpression<T, TValue>> columnExpression, TValue value)
            where TThis : ITypedWhereClause<T, TThis>
        {
            return query.OrWhere(columnExpression, "=", value);
        }

        /// <summary>
        /// Adds a WHERE IN clause using a column expression.
        /// </summary>
        public static TThis WhereIn<T, TThis, TValue>(this ITypedWhereClause<T, TThis> query,
            Expression<ColumnExpression<T, TValue>> columnExpression, params TValue[] values)
            where TThis : ITypedWhereClause<T, TThis>
        {
            var typedQuery = query as Query<T>;
            if (typedQuery == null)
                throw new InvalidOperationException("Query must be of type Query<T>");

            var column = typedQuery.GetColumn(columnExpression);
            typedQuery.WhereIn(column, values.Cast<object>().ToArray());
            return (TThis)query;
        }

        /// <summary>
        /// Adds a WHERE NOT IN clause using a column expression.
        /// </summary>
        public static TThis WhereNotIn<T, TThis, TValue>(this ITypedWhereClause<T, TThis> query,
            Expression<ColumnExpression<T, TValue>> columnExpression, params TValue[] values)
            where TThis : ITypedWhereClause<T, TThis>
        {
            var typedQuery = query as Query<T>;
            if (typedQuery == null)
                throw new InvalidOperationException("Query must be of type Query<T>");

            var column = typedQuery.GetColumn(columnExpression);
            typedQuery.WhereNotIn(column, values.Cast<object>().ToArray());
            return (TThis)query;
        }

        /// <summary>
        /// Adds a WHERE NULL clause using a column expression.
        /// </summary>
        public static TThis WhereNull<T, TThis, TValue>(this ITypedWhereClause<T, TThis> query,
            Expression<ColumnExpression<T, TValue>> columnExpression)
            where TThis : ITypedWhereClause<T, TThis>
        {
            return query.Where(columnExpression, "IS", default(TValue));
        }

        /// <summary>
        /// Adds a WHERE NOT NULL clause using a column expression.
        /// </summary>
        public static TThis WhereNotNull<T, TThis, TValue>(this ITypedWhereClause<T, TThis> query,
            Expression<ColumnExpression<T, TValue>> columnExpression)
            where TThis : ITypedWhereClause<T, TThis>
        {
            return query.Where(columnExpression, "IS NOT", default(TValue));
        }

        #endregion
    }
}
