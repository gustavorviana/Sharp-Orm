using SharpOrm.Builder;
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
        /// Adds a WHERE clause with a specified operation and value using an SQL expression.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the comparison on.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The SQL expression representing the value to compare with.</param>
        public static QueryBase Where(this QueryBase qBase, string column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "AND");
        }

        /// <summary>
        /// Adds a WHERE clause with a specified operation and value using an SQL expression.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the comparison on.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The SQL expression representing the value to compare with.</param>
        public static QueryBase Where(this QueryBase qBase, Column column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "AND");
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
        /// Adds a WHERE clause with a specified operation between two columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column1">The first column for comparison.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2">The second column for comparison.</param>
        public static QueryBase Where(this QueryBase qBase, Column column1, string operation, Column column2)
        {
            return qBase.WriteWhere(column1, operation, column2, "AND");
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
        /// Adds a WHERE clause using the "IN" operator to check if the column value is among the items specified in the expression.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static QueryBase WhereIn(this QueryBase qBase, string column, SqlExpression expression)
        {
            return qBase.Where(column, "IN", expression);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the column value is among the items specified in the expression.
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
            return WhereInColumn(qBase, false, false, value, columns);
        }

        /// <summary>
        /// Adds a WHERE clause using the "NOT IN" operator to check if the specified value is not in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static QueryBase WhereNotInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, true, false, value, columns);
        }

        #endregion

        #region Or

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
        /// Adds an OR WHERE clause with a specified operation and SQL expression value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the comparison on.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The SQL expression representing the value to compare with.</param>
        public static QueryBase OrWhere(this QueryBase qBase, string column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "OR");
        }

        /// <summary>
        /// Adds an OR WHERE clause with a specified operation and SQL expression value.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the comparison on.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="value">The SQL expression representing the value to compare with.</param>
        public static QueryBase OrWhere(this QueryBase qBase, Column column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "OR");
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
        /// Adds an OR WHERE clause between two columns with a specified operation.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column1">The first column for comparison.</param>
        /// <param name="operation">The operation to perform (e.g., "=", "LIKE", ">", etc.).</param>
        /// <param name="column2">The second column for comparison.</param>
        public static QueryBase OrWhere(this QueryBase qBase, Column column1, string operation, Column column2)
        {
            return qBase.WriteWhere(column1, operation, column2, "OR");
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
        /// Adds an OR WHERE clause using the "IN" operator to check if the column value is among the items specified in the expression.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="column">The column to perform the "IN" comparison on.</param>
        public static QueryBase OrWhereIn(this QueryBase qBase, string column, SqlExpression expression)
        {
            return qBase.OrWhere(column, "IN", expression);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the column value is among the items specified in the expression.
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
            return WhereInColumn(qBase, false, true, value, columns);
        }

        /// <summary>
        /// Adds an OR WHERE clause using the "NOT IN" operator to check if the specified value is not in any of the specified columns.
        /// </summary>
        /// <param name="qBase">The QueryBase object to apply the filter on.</param>
        /// <param name="value">The value to check for in the columns.</param>
        /// <param name="columns">The names of the columns to check.</param>
        public static QueryBase OrWhereNotInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, true, true, value, columns);
        }

        #endregion

        private static QueryBase WhereInColumn(QueryBase qBase, bool not, bool or, object value, IEnumerable<string> columns)
        {
            string @in = not ? "NOT IN" : "IN";
            columns = columns.Select(qBase.Info.Config.ApplyNomenclature);
            var exp = new SqlExpression($"? {@in} ({string.Join(",", columns)})", value);

            return or ? qBase.OrWhere(exp) : qBase.Where(exp);
        }

        internal static SqlExpression ToSafeExpression(this ISqlExpressible exp, IReadonlyQueryInfo info, bool allowAlias)
        {
            if (exp is ISqlExpressibleAlias iAliasExp)
                return iAliasExp.ToExpression(info, allowAlias);

            return exp.ToExpression(info);
        }

        #region Query

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
        /// Update row from table, disregarding column names.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <param name="columnsToIgnore">Columns that should not be updated.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Launched when obj is null or columnsToIgnore is null or has no columns.</exception>
        public static int UpdateExcept<T>(this Query<T> query, T obj, params string[] columnsToIgnore) where T : new()
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if ((columnsToIgnore?.Length ?? 0) == 0)
                throw new ArgumentNullException(nameof(columnsToIgnore));

            return query.Update(query.TableInfo.GetObjCells(obj, false, query.Info.Config.ForeignLoader).Where(i => !columnsToIgnore.Contains(i.Name)).ToArray());
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Id of row (long).</returns>
        public static long InsertL<T>(this Query<T> query, T obj) where T : new()
        {
            return query.InsertL(query.TableInfo.GetRow(obj, true, query.Creator.Config.ForeignLoader).Cells);
        }

        /// <summary>
        /// Inserts a new row into the table using the provided dictionary of cell values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="cells">A dictionary containing column names and their corresponding values for the new row.</param>
        /// <returns>Id of row.</returns>
        public static long InsertL(this Query query, Dictionary<string, object> cells)
        {
            return query.Insert(cells.Select(x => new Cell(x.Key, x.Value)).ToArray());
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
                return result is DBNull ? 0 : Convert.ToInt64(result);
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
            using (query = (Query<T>)query.Clone(false))
            {
                foreach (var column in toCheckColumns)
                    query.Where(column, query.TableInfo.GetValue(obj, column));

                if (query.Any()) query.Update(obj);
                else query.Insert(obj);
            }
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
                throw new ArgumentException("At least one column name must be entered.", nameof(toCheckColumns));

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

        /// <summary>
        /// Checks if the DbDataReader contains a column with the specified name.
        /// </summary>
        /// <param name="reader">The DbDataReader to check for the column.</param>
        /// <param name="key">The name of the column to check for.</param>
        /// <returns>True if the column exists in the reader, otherwise false.</returns>
        [Obsolete("will be removed in version 1.2.28.")]
        public static bool HasName(this DbDataReader reader, string key)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i) == key)
                    return true;

            return false;
        }

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