﻿using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm
{
    public static class QueryExtension
    {
        #region Where

        /// <summary>
        /// Adds a condition to the query where the value of the first column must be equal to the value of the second column.
        /// </summary>
        /// <param name="query">The query to which the condition will be added.</param>
        /// <param name="column1">The name of the first column.</param>
        /// <param name="column2">The name of the second column.</param>
        /// <returns>The modified query with the added condition.</returns>
        public static QueryBase WhereColumn(this QueryBase query, string column1, string column2)
        {
            return query.WhereColumn(column1, "=", column2);
        }

        /// <summary>
        /// Adds a condition to the query where the value of the first column must not be equal to the value of the second column.
        /// </summary>
        /// <param name="query">The query to which the condition will be added.</param>
        /// <param name="column1">The name of the first column.</param>
        /// <param name="column2">The name of the second column.</param>
        /// <returns>The modified query with the added condition.</returns>
        public static QueryBase WhereNotColumn(this QueryBase query, string column1, string column2)
        {
            return query.WhereColumn(column1, "!=", column2);
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column contains the value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to filter.</param>
        /// <param name="column">The column on which the "contains" condition is applied.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static Q WhereContains<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "LIKE", string.Concat("%", value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static Q WhereStartsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "LIKE", string.Concat(value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static Q WhereEndsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "LIKE", string.Concat("%", value.SanitizeSqlValue()));
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not contains the value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to filter.</param>
        /// <param name="column">The column on which the "contains" condition is applied.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static Q WhereNotContains<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "NOT LIKE", string.Concat("%", value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static Q WhereNotStartsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "NOT LIKE", string.Concat(value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static Q WhereNotEndsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "NOT LIKE", string.Concat("%", value.SanitizeSqlValue()));
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public static QueryBase WhereIn<T>(this QueryBase qBase, object column, params T[] items)
        {
            return qBase.Where(column, "IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the column value is not among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public static QueryBase WhereNotIn<T>(this QueryBase qBase, object column, params T[] items)
        {
            return qBase.Where(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static Q WhereIn<Q>(this Q qBase, object column, SqlExpression expression) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "IN", expression);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static Q WhereIn<Q>(this Q qBase, object column, Query query) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "IN", query);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        public static Q WhereNotIn<Q>(this Q qBase, object column, SqlExpression expression) where Q : QueryBase
        {
            return (Q)qBase.Where(column, "NOT IN", expression);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">An IEnumerable collection of items to check against the column value.</param>
        public static QueryBase WhereIn<T>(this QueryBase qBase, object column, IEnumerable<T> items)
        {
            return qBase.Where(column, "IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the column value is not among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        /// <param name="items">An IEnumerable collection of items to check against the column value.</param>
        public static QueryBase WhereNotIn<T>(this QueryBase qBase, object column, IEnumerable<T> items)
        {
            return qBase.Where(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the specified value is in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static Q WhereInColumn<Q>(this Q qBase, object value, params string[] columns) where Q : QueryBase
        {
            return (Q)WhereInColumn(qBase, false, value, columns, QueryBase.AND);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the specified value is not in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static Q WhereNotInColumn<Q>(this Q qBase, object value, params string[] columns) where Q : QueryBase
        {
            return (Q)WhereInColumn(qBase, true, value, columns, QueryBase.AND);
        }

        /// <summary>
        /// A NOT LIKE clause for each option <paramref name="likeOptions"/>.
        /// </summary>
        /// <param name="qBase"></param>
        /// <param name="column">Column to be checked.</param>
        /// <param name="likeOptions">LIKE checks that should be added.</param>
        /// <returns></returns>
        public static Q WhereNotLikeIn<Q>(this Q qBase, object column, params string[] likeOptions) where Q : QueryBase
        {
            qBase.WriteWhereType(QueryBase.AND);
            qBase.Info.Where.Add("NOT ");
            InternalWhereLikeIn(qBase, column, likeOptions);
            return qBase;
        }

        /// <summary>
        /// A LIKE clause for each option <paramref name="likeOptions"/>.
        /// </summary>
        /// <param name="qBase"></param>
        /// <param name="column">Column to be checked.</param>
        /// <param name="likeOptions">LIKE checks that should be added.</param>
        /// <returns></returns>
        public static Q WhereLikeIn<Q>(this Q qBase, object column, params string[] likeOptions) where Q : QueryBase
        {
            qBase.WriteWhereType(QueryBase.AND);
            return InternalWhereLikeIn(qBase, column, likeOptions);
        }

        #endregion

        #region Or

        /// <summary>
        /// Adds an OR condition to the query that compares the values of two columns for equality.
        /// </summary>
        /// <param name="query">The query object to which the condition will be added.</param>
        /// <param name="column1">The name of the first column to be compared.</param>
        /// <param name="column2">The name of the second column to be compared.</param>
        /// <returns>The updated query object with the added condition.</returns>
        public static Q OrWhereColumn<Q>(this Q query, string column1, string column2) where Q : QueryBase
        {
            return (Q)query.OrWhereColumn(column1, "=", column2);
        }

        /// <summary>
        /// Adds an OR condition to the query that compares the values of two columns for inequality.
        /// </summary>
        /// <param name="query">The query object to which the condition will be added.</param>
        /// <param name="column1">The name of the first column to be compared.</param>
        /// <param name="column2">The name of the second column to be compared.</param>
        /// <returns>The updated query object with the added condition.</returns>
        public static Q OrWhereNotColumn<Q>(this Q query, string column1, string column2) where Q : QueryBase
        {
            return (Q)query.OrWhereColumn(column1, "!=", column2);
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column contains the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "contains" comparison on.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static Q OrWhereContains<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "LIKE", string.Concat("%", value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static Q OrWhereStartsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "LIKE", string.Concat(value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static Q OrWhereEndsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "LIKE", string.Concat("%", value.SanitizeSqlValue()));
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not contains the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "contains" comparison on.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static Q OrWhereNotContains<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "NOT LIKE", string.Concat("%", value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static Q OrWhereNotStartsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "NOT LIKE", string.Concat(value.SanitizeSqlValue(), "%"));
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static Q OrWhereNotEndsWith<Q>(this Q qBase, object column, string value) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "NOT LIKE", string.Concat("%", value.SanitizeSqlValue()));
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public static QueryBase OrWhereIn<T>(this QueryBase qBase, object column, params T[] items)
        {
            return qBase.OrWhere(column, "IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the column value is not among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public static QueryBase OrWhereNotIn<T>(this QueryBase qBase, object column, params T[] items)
        {
            return qBase.OrWhere(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static Q OrWhereIn<Q>(this Q qBase, object column, SqlExpression expression) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "IN", expression);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static Q OrWhereIn<Q>(this Q qBase, object column, Query query) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "IN", query);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        public static Q OrWhereNotIn<Q>(this Q qBase, object column, SqlExpression expression) where Q : QueryBase
        {
            return (Q)qBase.OrWhere(column, "NOT IN", expression);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">An IEnumerable collection of items to check against the column value.</param>
        public static QueryBase OrWhereIn<T>(this QueryBase qBase, object column, IEnumerable<T> items)
        {
            return qBase.OrWhere(column, "IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the column value is not among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        /// <param name="items">An IEnumerable collection of items to check against the column value.</param>
        public static QueryBase OrWhereNotIn<T>(this QueryBase qBase, object column, IEnumerable<T> items)
        {
            return qBase.OrWhere(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the specified value is in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static Q OrWhereInColumn<Q>(this Q qBase, object value, params string[] columns) where Q : QueryBase
        {
            return WhereInColumn(qBase, false, value, columns, QueryBase.OR);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the specified value is not in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static Q OrWhereNotInColumn<Q>(this Q qBase, object value, params string[] columns) where Q : QueryBase
        {
            return WhereInColumn(qBase, true, value, columns, QueryBase.OR);
        }

        /// <summary>
        /// A NOT LIKE clause for each option <paramref name="likeOptions"/>.
        /// </summary>
        /// <param name="qBase"></param>
        /// <param name="column">Column to be checked.</param>
        /// <param name="likeOptions">LIKE checks that should be added.</param>
        /// <returns></returns>
        public static Q OrWhereNotLikeIn<Q>(this Q qBase, object column, params string[] likeOptions) where Q : QueryBase
        {
            qBase.WriteWhereType(QueryBase.OR);
            qBase.Info.Where.Add(" NOT ");
            InternalWhereLikeIn(qBase, column, likeOptions);
            return qBase;
        }

        /// <summary>
        /// A LIKE clause for each option <paramref name="likeOptions"/>.
        /// </summary>
        /// <param name="qBase"></param>
        /// <param name="column">Column to be checked.</param>
        /// <param name="likeOptions">LIKE checks that should be added.</param>
        /// <returns></returns>
        public static Q OrWhereLikeIn<Q>(this Q qBase, object column, params string[] likeOptions) where Q : QueryBase
        {
            qBase.WriteWhereType(QueryBase.OR);
            return InternalWhereLikeIn(qBase, column, likeOptions);
        }

        #endregion

        /// <summary>
        /// A LIKE clause for each option <paramref name="likeOptions"/>.
        /// </summary>
        /// <param name="qBase"></param>
        /// <param name="column"></param>
        /// <param name="likeOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static Q InternalWhereLikeIn<Q>(Q qBase, object column, string[] likeOptions) where Q : QueryBase
        {
            if (likeOptions == null || likeOptions.Length < 1)
                throw new ArgumentNullException(nameof(likeOptions));

            QueryBuilder builder = new QueryBuilder(qBase);
            builder.Add('(').AddColumn(column).Add(" LIKE ").AddParameter(likeOptions[0]);

            for (int i = 1; i < likeOptions.Length; i++)
                builder.Add(" OR ").AddColumn(column).Add(" LIKE ").AddParameter(likeOptions[i]);

            builder.Add(')');
            qBase.Info.Where.Add(builder);
            return qBase;
        }

        private static Q WhereInColumn<Q>(Q qBase, bool not, object value, IEnumerable<string> columns, string whereType) where Q : QueryBase
        {
            qBase.WriteWhereType(whereType);
            qBase.Info.Where.AddParameter(value);

            if (not)
                qBase.Info.Where.Add(" NOT");

            qBase.Info.Where.Add(" IN ").WriteEnumerableAsValue(columns.Select(c => new Column(qBase.Info.Config.ApplyNomenclature(c))), true);

            return qBase;
        }

        internal static SqlExpression ToSafeExpression(this ISqlExpressible exp, IReadonlyQueryInfo info, bool allowAlias)
        {
            if (exp is ISqlExpressibleAlias iAliasExp)
                return iAliasExp.ToExpression(info, allowAlias);

            return exp.ToExpression(info);
        }

        #region Query

        /// <summary>
        /// Inserts a large number of rows into the database in batches of the specified page size using the given query.
        /// </summary>
        /// <typeparam name="T">The type of the rows to be inserted.</typeparam>
        /// <param name="query">The query object used to execute the batch insertion.</param>
        /// <param name="rows">The collection of rows to be inserted into the database.</param>
        /// <param name="pageSize">The number of rows to be inserted in each batch.</param>
        public static void InsertLot<T>(this Query<T> query, IEnumerable<T> rows, int pageSize) where T : class, new()
        {
            using (var enumerator = rows.GetEnumerator())
                while (enumerator.MoveNext())
                    query.BulkInsert(GetPage(enumerator, pageSize));
        }

        /// <summary>
        /// Inserts a large number of rows into the database in batches of the specified page size using the given query.
        /// </summary>
        /// <param name="query">The query object used to execute the batch insertion.</param>
        /// <param name="rows">The collection of rows to be inserted into the database.</param>
        /// <param name="pageSize">The number of rows to be inserted in each batch.</param>
        public static void InsertLot(this Query query, IEnumerable<Row> rows, int pageSize)
        {
            using (var enumerator = rows.GetEnumerator())
                while (enumerator.MoveNext())
                    query.BulkInsert(GetPage(enumerator, pageSize));
        }

        internal static IEnumerable<T> GetPage<T>(IEnumerator<T> enumerator, int pageSize)
        {
            do yield return enumerator.Current; while (--pageSize > 0 && enumerator.MoveNext());
        }

        /// <summary>
        /// Get a <see cref="DataTable"/> with values from a SELECT query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static DataTable ReadTable(this Query query)
        {
            DataTable dt = new DataTable();

            using (var reader = query.ExecuteReader())
                dt.Load(reader);

            query.Token.ThrowIfCancellationRequested();
            return dt;
        }

        /// <summary>
        /// Update the row of a table, ignoring the values of the properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <param name="expression">Expression to retrieve the names of the properties.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Launched when obj is null or columnsToIgnore is null or has no columns.</exception>
        public static int UpdateExcept<T>(this Query<T> query, T obj, Expression<ColumnExpression<T>> expression)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return query.Update(query.GetObjectReader(ReadMode.None, false).Except(expression).ReadCells(obj));
        }

        /// <summary>
        /// Update row from table, disregarding column names.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <param name="columns">Columns that should not be updated.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Launched when obj is null or columnsToIgnore is null or has no columns.</exception>
        public static int UpdateExcept<T>(this Query<T> query, T obj, params string[] columns)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return query.Update(query.GetObjectReader(ReadMode.None, false).Except(columns).ReadCells(obj));
        }

        /// <summary>
        /// Inserts the specified cells into the database using the given query and returns the result as an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the result expected from the query execution.</typeparam>
        /// <param name="query">The query object used to execute the insertion.</param>
        /// <param name="cells">An array of cells to be inserted into the database.</param>
        /// <returns>An object of type T representing the result of the insertion.</returns>
        [Obsolete("It will be removed in version 4.0.")]
        public static T Insert<T>(this Query query, params Cell[] cells)
        {
            return Insert<T>(query, (IEnumerable<Cell>)cells);
        }

        /// <summary>
        /// Inserts the specified object into the database using the given query and returns the result as an object of type R.
        /// </summary>
        /// <typeparam name="T">The type of the object to be inserted.</typeparam>
        /// <typeparam name="R">The type of the result expected from the query execution.</typeparam>
        /// <param name="query">The query object used to execute the insertion.</param>
        /// <param name="obj">The object to be inserted into the database.</param>
        /// <returns>An object of type R representing the result of the insertion.</returns>
        [Obsolete("It will be removed in version 4.0.")]
        public static R Insert<T, R>(this Query<T> query, T obj)
        {
            return Insert<R>(query, query.GetObjectReader(ReadMode.ValidOnly, true).ReadCells(obj));
        }

        /// <summary>
        /// Inserts the specified cells into the database using the given query and returns the result as an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the result expected from the query execution.</typeparam>
        /// <param name="query">The query object used to execute the insertion.</param>
        /// <param name="cells">The collection of cells to be inserted into the database.</param>
        /// <returns>An object of type T representing the result of the insertion.</returns>
        [Obsolete("It will be removed in version 4.0.")]
        public static T Insert<T>(this Query query, IEnumerable<Cell> cells)
        {
            using (var cmd = query.GetCommand())
            {
                cmd.SetExpression(query.GetGrammar().Insert(cells));
                return cmd.ExecuteScalar<T>();
            }
        }

        /// <summary>
        /// Creates a pager for rows using the provided query, peer page count, and current page number.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="peerPage">The number of peer pages to display on either side of the current page.</param>
        /// <param name="currentPage">The current page number.</param>
        /// <returns>A Pager object containing the paginated rows.</returns>
        public static Pager<Row> PaginateRows(this Query query, int peerPage, int currentPage)
        {
            return Pager<Row>.FromBuilder(query, peerPage, currentPage);
        }

        /// <summary>
        /// Checks if there is any value in the table.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool Any(this Query query)
        {
            return query.Count() > 0;
        }

        /// <summary>
        /// Checks if there is any value in the table.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<bool> AnyAsync(this Query query, CancellationToken token = default)
        {
            return await query.CountAsync(token) > 0;
        }

        /// <summary>
        /// Performs a bulk upsert operation on a collection of items in the query.
        /// </summary>
        /// <typeparam name="T">The type of the items to be upserted.</typeparam>
        /// <param name="query">The query object to perform the upsert operation on.</param>
        /// <param name="items">The collection of items to be upserted.</param>
        /// <param name="toCheckColumnsExp">Columns to check for conflicts during the upsert operation.</param>
        public static void BulkUpsert<T>(this Query<T> query, IEnumerable<T> items, Expression<ColumnExpression<T>> toCheckColumnsExp)
        {
            foreach (var item in items)
                query.Upsert(item, toCheckColumnsExp);
        }

        /// <summary>
        /// Performs a bulk upsert operation on a collection of items in the query.
        /// </summary>
        /// <typeparam name="T">The type of the items to be upserted.</typeparam>
        /// <param name="query">The query object to perform the upsert operation on.</param>
        /// <param name="items">The collection of items to be upserted.</param>
        /// <param name="toCheckColumns">The columns to check for conflicts during the upsert operation.</param>
        public static void BulkUpsert<T>(this Query<T> query, IEnumerable<T> items, string[] toCheckColumns)
        {
            foreach (var item in items)
                query.Upsert(item, toCheckColumns);
        }

        /// <summary>
        /// Performs a bulk delete operation on a collection of values in the query.
        /// </summary>
        /// <param name="query">The query object to perform the delete operation on.</param>
        /// <param name="values">The array of values to be deleted.</param>
        /// <param name="insertLot">An optional integer specifying the insert lot size. Default is 0.</param>
        /// <returns>The number of rows deleted.</returns>
        public static int BulkDelete(this Query query, Row[] values, int? insertLot = 0)
        {
            using (var bulk = new BulkOperation(query, values, insertLot))
                return bulk.Delete();
        }

        /// <summary>
        /// Performs a bulk update operation on a collection of values in the query.
        /// </summary>
        /// <typeparam name="T">The type of the items to be updated.</typeparam>
        /// <param name="query">The query object to perform the update operation on.</param>
        /// <param name="values">The array of values to be updated.</param>
        /// <param name="toCheckColumnsExp">Columns to check for conflicts during the update operation.</param>
        /// <param name="insertLot">An optional integer specifying the insert lot size. Default is 0.</param>
        /// <returns>The number of rows updated.</returns>
        public static int BulkUpdate<T>(this Query query, Row[] values, Expression<ColumnExpression<T>> toCheckColumnsExp, int? insertLot = 0)
        {
            using (var bulk = new BulkOperation(query, values, insertLot))
                return bulk.Update(toCheckColumnsExp);
        }

        /// <summary>
        /// Performs a bulk update operation on a collection of values in the query.
        /// </summary>
        /// <param name="query">The query object to perform the update operation on.</param>
        /// <param name="values">The array of values to be updated.</param>
        /// <param name="comparationColumns">The columns that should be compared to perform the update.</param>
        /// <param name="insertLot">An optional integer specifying the insert lot size. Default is 0.</param>
        /// <returns>The number of rows updated.</returns>
        public static int BulkUpdate(this Query query, Row[] values, string[] comparationColumns, int? insertLot = 0)
        {
            using (var bulk = new BulkOperation(query, values, insertLot))
                return bulk.Update(comparationColumns);
        }

        #region Join

        /// <summary>
        /// Adds an INNER JOIN clause to the query between the main table and the specified table.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="leftColumn">The column from the main table to join on.</param>
        /// <param name="operation">The operation to perform for the join condition (e.g., "=", ">", etc.).</param>
        /// <param name="rightColumn">The column from the joined table to join on.</param>
        /// <returns>A Query object with the INNER JOIN clause applied.</returns>
        public static Query InnerJoin(this Query query, string table, string leftColumn, string operation, string rightColumn)
        {
            return query.Join(table, leftColumn, operation, rightColumn, "INNER");
        }

        /// <summary>
        /// Adds an INNER JOIN clause to the query between the main table and the specified table using the "=" operation.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="leftColumn">The column from the main table to join on.</param>
        /// <param name="rightColumn">The column from the joined table to join on.</param>
        /// <returns>A Query object with the INNER JOIN clause applied.</returns>
        public static Query InnerJoin(this Query query, string table, string leftColumn, string rightColumn)
        {
            return query.Join(table, leftColumn, "=", rightColumn, "INNER");
        }

        /// <summary>
        /// Adds a LEFT JOIN clause to the query between the main table and the specified table.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="leftColumn">The column from the main table to join on.</param>
        /// <param name="operation">The operation to perform for the join condition (e.g., "=", ">", etc.).</param>
        /// <param name="rightColumn">The column from the joined table to join on.</param>
        /// <returns>A Query object with the LEFT JOIN clause applied.</returns>
        public static Query LeftJoin(this Query query, string table, string leftColumn, string operation, string rightColumn)
        {
            return query.Join(table, leftColumn, operation, rightColumn, "LEFT");
        }

        /// <summary>
        /// Adds a LEFT JOIN clause to the query between the main table and the specified table using the "=" operation.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="leftColumn">The column from the main table to join on.</param>
        /// <param name="rightColumn">The column from the joined table to join on.</param>
        /// <returns>A Query object with the LEFT JOIN clause applied.</returns>
        public static Query LeftJoin(this Query query, string table, string leftColumn, string rightColumn)
        {
            return query.Join(table, leftColumn, "=", rightColumn, "LEFT");
        }

        /// <summary>
        /// Adds a RIGHT JOIN clause to the query between the main table and the specified table.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="leftColumn">The column from the main table to join on.</param>
        /// <param name="operation">The operation to perform for the join condition (e.g., "=", ">", etc.).</param>
        /// <param name="rightColumn">The column from the joined table to join on.</param>
        /// <returns>A Query object with the RIGHT JOIN clause applied.</returns>
        public static Query RightJoin(this Query query, string table, string leftColumn, string operation, string rightColumn)
        {
            return query.Join(table, leftColumn, operation, rightColumn, "RIGHT");
        }

        /// <summary>
        /// Adds a RIGHT JOIN clause to the query between the main table and the specified table using the "=" operation.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="leftColumn">The column from the main table to join on.</param>
        /// <param name="rightColumn">The column from the joined table to join on.</param>
        /// <returns>A Query object with the RIGHT JOIN clause applied.</returns>
        public static Query RightJoin(this Query query, string table, string leftColumn, string rightColumn)
        {
            return query.Join(table, leftColumn, "=", rightColumn, "RIGHT");
        }

        #endregion

        #endregion

        #region DbDataReader

        /// <summary>
        /// Retrieves the value of the specified column from the data reader, ignoring the case of the column name.
        /// </summary>
        /// <param name="reader">The data reader to retrieve the value from.</param>
        /// <param name="key">The name of the column to retrieve the value from.</param>
        /// <returns>The value of the specified column, or DBNull.Value if the column is not found.</returns>
        public static object Get(this DbDataReader reader, string key)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(key, StringComparison.OrdinalIgnoreCase))
                    return reader[i];

            return DBNull.Value;
        }

        /// <summary>
        /// Retrieves the index of the specified column from the data reader, ignoring the case of the column name.
        /// </summary>
        /// <param name="record">The data record to retrieve the column index from.</param>
        /// <param name="name">The name of the column to find the index of.</param>
        /// <returns>The index of the specified column, or -1 if the column is not found.</returns>
        public static int GetIndexOf(this IDataRecord record, string name)
        {
            for (int i = 0; i < record.FieldCount; i++)
                if (record.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;

            return -1;
        }

        #endregion
    }
}