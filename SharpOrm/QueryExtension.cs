using SharpOrm.Builder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm
{
    public static class QueryExtension
    {
        #region Where
        public static QueryBase Where(this QueryBase qBase, string column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "AND");
        }

        public static QueryBase Where(this QueryBase qBase, Column column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "AND");
        }

        public static QueryBase Where(this QueryBase qBase, Column column, string operation, object value)
        {
            return qBase.WriteWhere(column, operation, value, "AND");
        }

        public static QueryBase Where(this QueryBase qBase, Column column1, string operation, Column column2)
        {
            return qBase.WriteWhere(column1, operation, column2, "AND");
        }

        public static QueryBase WhereIn<T>(this QueryBase qBase, string column, params T[] items)
        {
            return qBase.Where(column, "IN", items);
        }

        public static QueryBase WhereNotIn<T>(this QueryBase qBase, string column, params T[] items)
        {
            return qBase.Where(column, "NOT IN", items);
        }

        public static QueryBase WhereIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
        {
            return qBase.Where(column, "IN", items);
        }

        public static QueryBase WhereNotIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
        {
            return qBase.Where(column, "NOT IN", items);
        }

        public static QueryBase WhereInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, false, false, value, columns);
        }

        public static QueryBase WhereNotInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, true, false, value, columns);
        }

        #endregion

        #region Or
        public static QueryBase OrWhere(this QueryBase qBase, string column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "OR");
        }

        public static QueryBase OrWhere(this QueryBase qBase, Column column, string operation, SqlExpression value)
        {
            return qBase.WriteWhere(column, operation, value, "OR");
        }

        public static QueryBase OrWhere(this QueryBase qBase, Column column, string operation, object value)
        {
            return qBase.WriteWhere(column, operation, value, "OR");
        }

        public static QueryBase OrWhere(this QueryBase qBase, Column column1, string operation, Column column2)
        {
            return qBase.WriteWhere(column1, operation, column2, "OR");
        }

        public static QueryBase OrWhereIn<T>(this QueryBase qBase, string column, params T[] items)
        {
            return qBase.OrWhere(column, "IN", items);
        }

        public static QueryBase OrWhereNotIn<T>(this QueryBase qBase, string column, params T[] items)
        {
            return qBase.OrWhere(column, "NOT IN", items);
        }

        public static QueryBase OrWhereIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
        {
            return qBase.OrWhere(column, "IN", items);
        }

        public static QueryBase OrWhereNotIn<T>(this QueryBase qBase, string column, IEnumerable<T> items)
        {
            return qBase.OrWhere(column, "NOT IN", items);
        }

        public static QueryBase OrWhereInColumn(this QueryBase qBase, object value, params string[] columns)
        {
            return WhereInColumn(qBase, false, true, value, columns);
        }

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

        public static Pager<Row> PaginateRows(this Query query, int peerPage, int currentPage)
        {
            return Pager<Row>.FromBuilder(query, peerPage, currentPage);
        }

        internal static void ValidateTranslator()
        {
            if (Query.Translator == null)
                throw new NullReferenceException($"The \"{nameof(Query.Translator)}\" property must be set");
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

        public static void Upsert<T>(this Query<T> query, T obj, string[] toCheckColumns) where T : new()
        {
            query.Upsert(Query.Translator.ToRow(obj, typeof(T)), toCheckColumns);
        }

        /// <summary>
        /// Creates a row if it does not exist or updates the value if it already exists.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="row">Query with the connection that should be used.</param>
        /// <param name="toCheckColumns">Columns that must be checked to update the database rows.</param>
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

        public static Query InnerJoin(this Query query, string table, string leftColumn, string operation, string rightColumn)
        {
            return query.Join(table, leftColumn, operation, rightColumn, "INNER");
        }

        public static Query InnerJoin(this Query query, string table, string leftColumn, string rightColumn)
        {
            return query.Join(table, leftColumn, "=", rightColumn, "INNER");
        }

        public static Query LeftJoin(this Query query, string table, string leftColumn, string operation, string rightColumn)
        {
            return query.Join(table, leftColumn, operation, rightColumn, "LEFT");
        }

        public static Query LeftJoin(this Query query, string table, string leftColumn, string rightColumn)
        {
            return query.Join(table, leftColumn, "=", rightColumn, "LEFT");
        }

        public static Query RightJoin(this Query query, string table, string leftColumn, string operation, string rightColumn)
        {
            return query.Join(table, leftColumn, operation, rightColumn, "RIGHT");
        }

        public static Query RightJoin(this Query query, string table, string leftColumn, string rightColumn)
        {
            return query.Join(table, leftColumn, "=", rightColumn, "RIGHT");
        }

        #endregion

        #endregion

        #region DbDataReader

        /// <summary>
        /// Get Cell by column index.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Cell GetCell(this DbDataReader reader, int index)
        {
            if (index < 0 || index > reader.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(reader.GetName(index), reader[index]);
        }

        /// <summary>
        /// Get row of current reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Row GetRow(this DbDataReader reader)
        {
            Cell[] cells = new Cell[reader.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = reader.GetCell(i);

            return new Row(cells);
        }

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
