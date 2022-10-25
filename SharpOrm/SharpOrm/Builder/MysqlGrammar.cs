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
            this.WriteWhere();
        }

        protected override void ConfigureInsert(Cell[] cells)
        {
            this.QueryBuilder.AppendFormat(
                "INSERT INTO {0} ({1}) VALUES ({2})",
                this.GetTableName(false),
                string.Join(", ", cells.Select(c => this.Info.ApplyColumnConfig(c.Name))),
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
            this.WriteWhere();

            this.WriteGroupBy();

            if (this.Query.Limit != null)
                this.QueryBuilder.AppendFormat(" LIMIT {0}", this.Query.Limit);

            if (this.Query.Offset != null)
                this.QueryBuilder.AppendFormat(" OFFSET {0}", this.Query.Offset);

            if (this.Info.Orders.Count > 0)
                this.QueryBuilder.AppendFormat(" ORDER BY {0}", string.Join(", ", this.Info.Orders.Select(col => $"{col.Column.ToExpression(this.Query)} {col.Order}")));
        }

        private void ApplyJoins()
        {
            if (this.Info.Joins.Count > 0)
                foreach (var join in this.Info.Joins)
                    this.WriteJoin(join);
        }

        protected virtual void WriteJoin(JoinQuery join)
        {
            if (!string.IsNullOrEmpty(join.Type))
                this.QueryBuilder.Append($" {join.Type}");

            this.QueryBuilder.Append($" JOIN {this.GetTableName(true)} ON {join.info.Wheres}");
        }

        protected override void ConfigureUpdate(Cell[] cells)
        {
            this.QueryBuilder.AppendFormat(
                "UPDATE {0} SET {1}",
                this.GetTableName(false),
                string.Join(", ", cells.Select(c => $"{this.Info.ApplyColumnConfig(c.Name)} = {RegisterValueParam(c.Value)}"))
            );
            this.WriteWhere();
        }

        private void WriteWhere(bool configureParameters = true)
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
