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
            if (string.IsNullOrEmpty(query.info.From))
                throw new ArgumentNullException(nameof(query.info.From));

            StringBuilder builder = new StringBuilder("SELECT ");
            if (query.Distinct)
                builder.Append("DISTINCT ");

            builder.Append(string.Join(", ", query.GetInfo().Select));
            builder.AppendFormat(" FROM {0}", this.GetTableName(query, true));

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
                    this.WriteJoin(builder, join);
        }

        protected virtual void WriteJoin(StringBuilder builder, JoinQuery join)
        {
            if (!string.IsNullOrEmpty(join.Type))
                builder.Append($" {join.Type}");

            builder.Append($" JOIN {this.GetTableName(join, true)} ON {join.GetInfo().Wheres}");
        }

        public virtual DbCommand CreateInsert(QueryBase query, Cell[] cells)
        {
            StringBuilder builder = new StringBuilder("INSERT INTO ");
            builder.Append(this.GetTableName(query, false));
            builder.AppendFormat(
                " ({0}) VALUES ({1})",
                string.Join(", ", cells.Select(c => c.Name)),
                string.Join(", ", this.RegisterValuesParameters(query, cells))
            );

            return query.GetInfo().SetCommandText(builder);
        }

        private IEnumerable<string> RegisterValuesParameters(QueryBase query, Cell[] cells)
        {
            foreach (var cell in cells)
                yield return query.RegisterParameterValue(cell.Value);
        }

        public virtual DbCommand CreateUpdate(QueryBase query, Cell[] cells)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("UPDATE {0} SET ", this.GetTableName(query, false));
            builder.Append(string.Join(", ", cells.Select(c => $"{c.Name} = {query.RegisterParameterValue(c.Value)}")));

            this.WriteWhere(builder, query);
            return query.GetInfo().SetCommandText(builder);
        }

        public virtual DbCommand CreateDelete(QueryBase query)
        {
            StringBuilder builder = new StringBuilder("DELETE FROM ");
            builder.Append(this.GetTableName(query, false));

            this.WriteWhere(builder, query);
            return query.GetInfo().SetCommandText(builder);
        }

        private void WriteWhere(StringBuilder builder, QueryBase query)
        {
            if (query.info.Wheres.Length > 0)
                builder.AppendFormat(" WHERE {0}", query.info.Wheres);
        }

        protected virtual string GetTableName(QueryBase query, bool withAlias)
        {
            string name = query.info.From.AlphaNumericOnly(' ');
            return !withAlias || string.IsNullOrEmpty(query.info.Alias) ? name : $"{name} {query.info.Alias.AlphaNumericOnly()}";
        }
    }
}
