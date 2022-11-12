﻿using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;

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
        #endregion

        #region Query
        /// <summary>
        /// Returns all rows of the table
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static Row[] ReadRows(this Query query)
        {
            List<Row> rows = new List<Row>();

            using (var reader = query.ExecuteReader())
                while (reader.Read())
                    rows.Add(reader.GetRow());

            return rows.ToArray();
        }

        /// <summary>
        /// Returns the first row of the table (if the table returns no value, null will be returned).
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static Row FirstRow(this Query query)
        {
            query.Offset = 0;
            query.Limit = 1;
            using (var reader = query.ExecuteReader())
                if (reader.Read())
                    return reader.GetRow();

            return null;
        }

        public static T[] GetAll<T>(this Query query) where T : new()
        {
            if (Query.DefaultTranslator == null)
                throw new NullReferenceException($"The \"{nameof(Query.DefaultTranslator)}\" property must be set");

            List<T> rows = new List<T>();

            using (var reader = query.ExecuteReader())
                while (reader.Read())
                    rows.Add(Query.DefaultTranslator.ParseFromReader<T>(reader));

            return rows.ToArray();
        }

        public static T FirstOrDefault<T>(this Query query) where T : new()
        {
            if (Query.DefaultTranslator == null)
                throw new NullReferenceException($"The \"{nameof(Query.DefaultTranslator)}\" property must be set");

            using (var reader = query.ExecuteReader())
                return reader.Read() ? Query.DefaultTranslator.ParseFromReader<T>(reader) : default;
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
        #endregion

        #region DbDataReader
        /// <summary>
        /// Check if column name exists.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool Contains(this DbDataReader reader, string name)
        {
            return reader.GetOrdinal(name) >= 0;
        }

        /// <summary>
        /// Get value by column name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetByName<T>(this DbDataReader reader, string name)
        {
            return (T)reader.GetByName(name);
        }

        /// <summary>
        /// Get value by column name.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object GetByName(this DbDataReader reader, string name)
        {
            return reader.GetValue(reader.GetOrdinal(name));
        }

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

        #endregion
    }
}