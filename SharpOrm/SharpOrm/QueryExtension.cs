using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace SharpOrm
{
    public static class QueryExtension
    {
        #region Where
        public static QueryBase Where(this QueryBase qBase, string column, string operation, object value)
        {
            qBase.Where(column, operation, value, "AND");
            return qBase;
        }

        public static QueryBase Where(this QueryBase qBase, string column, string operation, SqlExpression value)
        {
            qBase.Where(column, operation, value, "AND");
            return qBase;
        }

        public static QueryBase Where(this QueryBase qBase, Column column, string operation, SqlExpression value)
        {
            qBase.Where(column, operation, value, "AND");
            return qBase;
        }

        public static QueryBase Where(this QueryBase qBase, Column column, string operation, object value)
        {
            qBase.Where(column, operation, value, "AND");
            return qBase;
        }

        public static QueryBase Where(this QueryBase qBase, Column column1, string operation, Column column2)
        {
            qBase.Where(column1, operation, column2, "AND");
            return qBase;
        }

        public static QueryBase Where(this QueryBase qBase, QueryCallback callback)
        {
            var query = new QueryBase(qBase);
            callback(query);
            return qBase.Where((SqlExpression)query.info.Wheres);
        }

        public static QueryBase Where(this QueryBase qBase, SqlExpression expression)
        {
            qBase.Where(expression, "AND");
            return qBase;
        }

        #endregion

        #region Or
        public static QueryBase Or(this QueryBase qBase, string column, string operation, object value)
        {
            qBase.Where(column, operation, value, "OR");
            return qBase;
        }

        public static QueryBase Or(this QueryBase qBase, string column, string operation, SqlExpression value)
        {
            qBase.Where(column, operation, value, "OR");
            return qBase;
        }

        public static QueryBase Or(this QueryBase qBase, Column column, string operation, SqlExpression value)
        {
            qBase.Where(column, operation, value, "OR");
            return qBase;
        }

        public static QueryBase Or(this QueryBase qBase, Column column, string operation, object value)
        {
            qBase.Where(column, operation, value, "OR");
            return qBase;
        }

        public static QueryBase Or(this QueryBase qBase, Column column1, string operation, Column column2)
        {
            qBase.Where(column1, operation, column2, "OR");
            return qBase;
        }

        public static QueryBase Or(this QueryBase qBase, QueryCallback callback)
        {
            var query = new QueryBase(qBase);
            callback(query);
            return qBase.Or((SqlExpression)query.info.Wheres);
        }

        public static QueryBase Or(this QueryBase qBase, SqlExpression expression)
        {
            qBase.Where(expression, "OR");
            return qBase;
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

        #endregion
    }
}
