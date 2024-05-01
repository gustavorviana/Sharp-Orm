using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm
{
    public static class QueryExtension
    {
        #region Where

        public static QueryBase WhereColumn(this QueryBase query, string column1, string column2)
        {
            return query.WhereColumn(column1, "=", column2);
        }

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
        public static QueryBase WhereContains(this QueryBase qBase, string column, string value)
        {
            return qBase.Where(column, "LIKE", $"%{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static QueryBase WhereStartsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.Where(column, "LIKE", $"{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static QueryBase WhereEndsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.Where(column, "LIKE", $"%{value.SanitizeSqlValue()}");
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not contains the value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to filter.</param>
        /// <param name="column">The column on which the "contains" condition is applied.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static QueryBase WhereNotContains(this QueryBase qBase, string column, string value)
        {
            return qBase.Where(column, "NOT LIKE", $"%{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static QueryBase WhereNotStartsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.Where(column, "NOT LIKE", $"{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds a WHERE clause that checks if the column not ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static QueryBase WhereNotEndsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.Where(column, "NOT LIKE", $"%{value.SanitizeSqlValue()}");
        }

        /// <summary>
        /// Adds a WHERE clause with a specified operation and value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the comparison on.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The value to compare with.</param>
        public static QueryBase Where(this QueryBase qBase, Column column, string operation, object value)
        {
            return qBase.WriteWhere(column, operation, value, "AND");
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public static QueryBase WhereIn<T>(this QueryBase qBase, string column, params T[] items)
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
        public static QueryBase WhereNotIn<T>(this QueryBase qBase, string column, params T[] items)
        {
            return qBase.Where(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static QueryBase WhereIn(this QueryBase qBase, string column, SqlExpression expression)
        {
            return qBase.Where(column, "IN", expression);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        public static QueryBase WhereNotIn(this QueryBase qBase, string column, SqlExpression expression)
        {
            return qBase.Where(column, "NOT IN", expression);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">An IEnumerable collection of items to check against the column value.</param>
        public static QueryBase WhereIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
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
        public static QueryBase WhereNotIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
        {
            return qBase.Where(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds a WHERE clause using the "IN" operator to check if the specified value is in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static QueryBase WhereInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, false, value, columns, QueryBase.AND);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the specified value is not in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static QueryBase WhereNotInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, true, value, columns, QueryBase.AND);
        }

        #endregion

        #region Or

        public static QueryBase OrWhereColumn(this QueryBase query, string column1, string column2)
        {
            return query.OrWhereColumn(column1, "=", column2);
        }

        public static QueryBase OrWhereNotColumn(this QueryBase query, string column1, string column2)
        {
            return query.OrWhereColumn(column1, "!=", column2);
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column contains the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "contains" comparison on.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static QueryBase OrWhereContains(this QueryBase qBase, string column, string value)
        {
            return qBase.OrWhere(column, "LIKE", $"%{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static QueryBase OrWhereStartsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.OrWhere(column, "LIKE", $"{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static QueryBase OrWhereEndsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.OrWhere(column, "LIKE", $"%{value.SanitizeSqlValue()}");
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not contains the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "contains" comparison on.</param>
        /// <param name="value">The value to search for within the specified column.</param>
        public static QueryBase OrWhereNotContains(this QueryBase qBase, string column, string value)
        {
            return qBase.OrWhere(column, "NOT LIKE", $"%{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not starts with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "starts with" comparison on.</param>
        /// <param name="value">The value that the column should start with.</param>
        public static QueryBase OrWhereNotStartsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.OrWhere(column, "NOT LIKE", $"{value.SanitizeSqlValue()}%");
        }

        /// <summary>
        /// Adds an OR WHERE clause that checks if the column not ends with the specified value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "ends with" comparison on.</param>
        /// <param name="value">The value that the column should end with.</param>
        public static QueryBase OrWhereNotEndsWith(this QueryBase qBase, string column, string value)
        {
            return qBase.OrWhere(column, "NOT LIKE", $"%{value.SanitizeSqlValue()}");
        }

        /// <summary>
        /// Adds an OR WHERE clause with a specified operation and value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the comparison on.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The value to compare with.</param>
        public static QueryBase OrWhere(this QueryBase qBase, Column column, string operation, object value)
        {
            return qBase.WriteWhere(column, operation, value, "OR");
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">The array of items to check against the column value.</param>
        public static QueryBase OrWhereIn<T>(this QueryBase qBase, string column, params T[] items)
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
        public static QueryBase OrWhereNotIn<T>(this QueryBase qBase, string column, params T[] items)
        {
            return qBase.OrWhere(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static QueryBase OrWhereIn(this QueryBase qBase, string column, SqlExpression expression)
        {
            return qBase.OrWhere(column, "IN", expression);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the column value is among the items specified in the values.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "NOT IN" comparison on.</param>
        public static QueryBase OrWhereNotIn(this QueryBase qBase, string column, SqlExpression expression)
        {
            return qBase.OrWhere(column, "NOT IN", expression);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to compare.</typeparam>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        /// <param name="items">An IEnumerable collection of items to check against the column value.</param>
        public static QueryBase OrWhereIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
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
        public static QueryBase OrWhereNotIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
        {
            return qBase.OrWhere(column, "NOT IN", items);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "IN" operator to check if the specified value is in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static QueryBase OrWhereInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, false, value, columns, QueryBase.OR);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the specified value is not in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static QueryBase OrWhereNotInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, true, value, columns, QueryBase.OR);
        }

        #endregion


        private static QueryBase WhereInColumn(QueryBase qBase, bool not, object value, IEnumerable<string> columns, string whereType)
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

        public static void InsertLot<T>(this Query<T> query, IEnumerable<T> rows, int pageSize) where T : class, new()
        {
            using (var enumerator = rows.GetEnumerator())
                while (enumerator.MoveNext())
                    query.BulkInsert(GetPage(enumerator, pageSize));
        }

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
        /// <param name="calls">Calls to retrieve the names of the properties.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Launched when obj is null or columnsToIgnore is null or has no columns.</exception>
        public static int UpdateExcept<T>(this Query<T> query, T obj, params Expression<ColumnExpression<T>>[] calls) where T : new()
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var props = PropertyExpressionVisitor.VisitProperties(calls).ToArray();
            return query.Update(query.GetCellsOf(obj, false, props, false));
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
        public static int UpdateExcept<T>(this Query<T> query, T obj, params string[] columns) where T : new()
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return query.Update(SqlExtension.GetCellsByName(query.GetCellsOf(obj, false), columns, true));
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Id of row (long).</returns>
        public static long InsertL<T>(this Query<T> query, T obj) where T : new()
        {
            return query.InsertL(query.TableInfo.GetRow(obj, true, query.Config.LoadForeign).Cells);
        }

        /// <summary>
        /// Inserts a new row into the table using the provided dictionary of cell values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="cells">A dictionary containing column names and their corresponding values for the new row.</param>
        /// <returns>Id of row.</returns>
        public static long InsertL(this Query query, Dictionary<string, object> cells)
        {
            return query.InsertL(cells.Select(x => new Cell(x.Key, x.Value)).ToArray());
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Id of row (long).</returns>
        public static long InsertL(this Query query, params Cell[] cells)
        {
            if (cells.Length == 0)
                throw new InvalidOperationException(Messages.AtLeastOneColumnRequired);

            using (Grammar grammar = query.Info.Config.NewGrammar(query))
            using (DbCommand cmd = grammar.Insert(cells))
            {
                object result = cmd.ExecuteScalar();
                query.Token.ThrowIfCancellationRequested();
                return TranslationUtils.IsNumeric(result?.GetType()) ? Convert.ToInt64(result) : 0;
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

        public static void BulkUpsert<T>(this Query<T> query, IEnumerable<T> items, string[] toCheckColumns) where T : new()
        {
            foreach (var item in items)
                query.Upsert(item, toCheckColumns);
        }

        /// <summary>
        /// Inserts or updates a record of type T using the provided query, object, and columns to check for upserting.
        /// </summary>
        /// <typeparam name="T">The type of the record to insert or update.</typeparam>
        /// <param name="query">The Query&lt;T&gt; object representing the database query.</param>
        /// <param name="obj">The object of type T to be inserted or updated.</param>
        /// <param name="toCheckColumns">An array of column names to check for upserting.</param>
        /// <remarks>
        /// This method inserts a new record if it doesn't exist or updates an existing record if it matches the specified columns.
        /// </remarks>
        public static void Upsert<T>(this Query<T> query, T obj, string[] toCheckColumns) where T : new()
        {
            if (toCheckColumns.Length < 1)
                throw new ArgumentException(Messages.AtLeastOneColumnRequired, nameof(toCheckColumns));

            using (query = (Query<T>)query.Clone(false))
            {
                foreach (var column in toCheckColumns)
                    query.Where(column, query.TableInfo.GetValue(obj, column));

                if (query.Any()) query.Update(obj);
                else query.Insert(obj);
            }
        }

        public static void BulkUpsert(this Query query, IEnumerable<Row> rows, string[] toCheckColumns)
        {
            foreach (var row in rows)
                query.Upsert(row, toCheckColumns);
        }

        /// <summary>
        /// Inserts or updates a row using the provided query, row data, and columns to check for upserting.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="row">The Row object containing the row data to be inserted or updated.</param>
        /// <param name="toCheckColumns">An array of column names to check for upserting.</param>
        /// <remarks>
        /// This method inserts a new row if it doesn't exist or updates an existing row if it matches the specified columns.
        /// </remarks>
        public static void Upsert(this Query query, Row row, string[] toCheckColumns)
        {
            if (toCheckColumns.Length < 1)
                throw new ArgumentException(Messages.AtLeastOneColumnRequired, nameof(toCheckColumns));

            using (query = query.Clone(false))
            {
                foreach (var column in toCheckColumns)
                    query.Where(column, row[column]);

                if (query.Any()) query.Update(row.Cells);
                else query.Insert(row.Cells);
            }
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

        public static object Get(this DbDataReader reader, string key)
        {
            key = key.ToLower();
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).ToLower() == key)
                    return reader[i];

            return DBNull.Value;
        }

        /// <summary>
        /// Gets the index of the column with the specified name in the DbDataReader.
        /// </summary>
        /// <param name="reader">The DbDataReader to get the column index from.</param>
        /// <param name="name">The name of the column to find the index for.</param>
        /// <returns>The index of the column, or -1 if the column is not found.</returns>
        public static int GetIndexOf(this DbDataReader reader, string name)
        {
            name = name.ToLower();
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).ToLower() == name)
                    return i;

            return -1;
        }

        #endregion
    }
}