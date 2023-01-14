﻿using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureBulkInsert(Row[] rows)
        {
            this.ConfigureInsert(rows[0].Cells);

            for (int i = 1; i < rows.Length; i++)
                this.QueryBuilder.AppendFormat(", ({0})", string.Join(", ", rows[i].Cells.Select(c => this.RegisterValueParam(c.Value))));
        }

        protected override void ConfigureDelete()
        {
            this.QueryBuilder.AppendFormat("DELETE FROM {0}", this.GetTableName(false));
            this.WriteWhere(true);
        }

        protected override void ConfigureInsert(Cell[] cells)
        {
            this.QueryBuilder.AppendFormat(
                "INSERT INTO {0} ({1}) VALUES ({2})",
                this.GetTableName(false),
                string.Join(", ", cells.Select(c => this.ApplyTableColumnConfig(c.Name))),
                string.Join(", ", cells.Select(c => this.RegisterValueParam(c.Value)))
            );
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.QueryBuilder.Append("SELECT ");
            if (this.Query.Distinct)
                this.QueryBuilder.Append("DISTINCT ");

            this.WriteSelectColumns();
            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);

            this.WriteGroupBy();

            if (this.Query.Limit != null)
                this.QueryBuilder.AppendFormat(" LIMIT {0}", this.Query.Limit);

            if (this.Query.Offset != null)
                this.QueryBuilder.AppendFormat(" OFFSET {0}", this.Query.Offset);

            this.ApplyOrderBy();
        }

        protected void ApplyJoins()
        {
            if (this.Info.Joins.Count > 0)
                foreach (var join in this.Info.Joins)
                    this.WriteJoin(join);
        }

        protected virtual void ApplyOrderBy()
        {
            if (this.Info.Orders.Length > 0)
                this.QueryBuilder.AppendFormat(" ORDER BY {0}", string.Join(", ", this.Info.Orders.Select(col => $"{col.Column.ToExpression(this.Query)} {col.Order}")));
        }

        protected virtual void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.QueryBuilder.Append($" {join.Type} JOIN {this.GetTableName(join.Info, true)} ON {join.Info.Wheres}");
        }

        protected override void ConfigureUpdate(Cell[] cells)
        {
            this.QueryBuilder.AppendFormat(
                "UPDATE {0} SET {1}",
                this.GetTableName(false),
                string.Join(", ", cells.Select(c => $"{this.ApplyTableColumnConfig(c.Name)} = {RegisterValueParam(c.Value)}"))
            );
            this.WriteWhere(true);
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (this.Info.Wheres.Length == 0)
                return;

            var where = new StringBuilder(this.Info.Wheres.ToString());

            if (configureParameters)
                where.Replace('?', (count) => this.RegisterClausuleParameter(this.Info.WhereObjs[count - 1]));

            this.QueryBuilder.AppendFormat(" WHERE {0}", where);
            where.Clear();
        }
    }
}
