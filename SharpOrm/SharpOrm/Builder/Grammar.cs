using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public class Grammar
    {
        public virtual DbCommand CreateSelect(QueryBase query)
        {
            if (string.IsNullOrEmpty(query.From))
                throw new ArgumentNullException(nameof(query.From));

            StringBuilder builder = new StringBuilder("SELECT ");
            if (query.Distinct)
                builder.Append("DISTINCT ");

            builder.Append(string.Join(", ", query.GetInfo().Select));
            builder.AppendFormat(" FROM {0}", query.SafeFrom);

            this.ApplyJoins(builder, query);
            this.WriteWhere(builder, query);

            if (query.GetInfo().GroupsBy.Length > 0)
                builder.AppendFormat(" GROUP BY {0}", query.GetInfo().GroupsBy);

            if (query.Limit != null)
                builder.AppendFormat(" LIMIT {0}", query.Limit);

            if (query.Offset != null)
                builder.AppendFormat(" OFFSET {0}", query.Offset);

            if (query.GetInfo().Orders.Count > 0)
                builder.AppendFormat(" ORDER BY {0}", string.Join(", ", query.GetInfo().Orders.Select(col => $"{col.Name} {col.Order}")));
            
            return query.GetInfo().SetCommandText(builder);
        }

        private void ApplyJoins(StringBuilder builder, QueryBase query)
        {
            if (query.GetInfo().Joins.Count > 0)
                foreach (var join in query.GetInfo().Joins)
                    builder.Append($" {join.Type} JOIN {query.SafeFrom} ON {join.GetInfo().Wheres}");
        }

        public virtual DbCommand CreateInsert(QueryBase query, Row row)
        {
            StringBuilder builder = new StringBuilder("INSERT INTO ");
            builder.Append(query.SafeFrom);
            builder.AppendFormat(
                " ({0}) ({1})", 
                string.Join(", ", row.ColumnNames), 
                string.Join(", ", this.RegisterValuesParameters(query, row.Cells))
            );

            return query.GetInfo().SetCommandText(builder);
        }

        private IEnumerable<string> RegisterValuesParameters(QueryBase query, Cell[] cells)
        {
            foreach (var cell in cells)
            {
                query.RegisterParameterValue(out string name, cell.Value);
                yield return name;
            }
        }

        public virtual DbCommand CreateUpdate(QueryBase query, Cell[] cells)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("UPDATE {0} SET ", query.SafeFrom);
            builder.Append(string.Join(", ", cells.Select(c => $"{c.Name} = {c.Value}")));

            this.WriteWhere(builder, query);
            return query.GetInfo().SetCommandText(builder);
        }

        public virtual DbCommand CreateDelete(QueryBase query)
        {
            StringBuilder builder = new StringBuilder("DELETE FROM ");
            builder.Append(query.SafeFrom);

            this.WriteWhere(builder, query);
            return query.GetInfo().SetCommandText(builder);
        }

        private void WriteWhere(StringBuilder builder, QueryBase query)
        {
            if (query.info.Wheres.Length > 0)
                builder.AppendFormat(" WHERE {0}", query.info.Wheres);
        }
    }
}
