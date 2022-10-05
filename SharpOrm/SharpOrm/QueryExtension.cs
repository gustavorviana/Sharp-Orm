using SharpOrm.Builder;
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
        public static Cell GetCell(this DbDataReader reader, int index)
        {
            if (index < 0 || index > reader.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(reader.GetName(index), reader[index]);
        }

        public static Row GetRow(this DbDataReader reader)
        {
            Cell[] cells = new Cell[reader.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = reader.GetCell(i);

            return new Row(cells);
        }

        public static Row[] ReadRows(this Query query)
        {
            List<Row> rows = new List<Row>();

            using (var reader = query.ExecuteReader())
                while (reader.Read())
                    rows.Add(reader.GetRow());

            return rows.ToArray();
        }

        public static Row FirstOrDefault(this Query query)
        {
            query.Offset = 0;
            query.Limit = 1;
            using (var reader = query.ExecuteReader())
                if (reader.Read())
                    return reader.GetRow();

            return null;
        }

        public static bool Any(this Query query)
        {
            return query.Count() > 0;
        }

        /// <summary>
        /// Cria ou atualiza uma linha no banco de dados.
        /// </summary>
        /// <param name="query">Query com a conexão que deverá ser utilizada.</param>
        /// <param name="row">Linha que deverá ser atualizada.</param>
        /// <param name="toCheckColumns">Colunas que deverão ser verificadas para atualizar as linhas do banco de dados.</param>
        public static void InsertOrUpdate(this Query query, Row row, params string[] toCheckColumns)
        {
            query = query.Clone(false);

            foreach (var column in toCheckColumns)
                query.Where(column, "=", row[column]);

            if (query.Any()) query.Update(row.Cells);
            else query.Insert(row.Cells);
        }
        #endregion
    }
}
