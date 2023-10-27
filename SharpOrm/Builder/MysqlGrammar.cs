﻿using System;
using System.Linq;

namespace SharpOrm.Builder
{
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureInsertQuery(QueryBase query, string[] columnNames)
        {
            this.QueryBuilder
                .AppendFormat(
                    "INSERT INTO {0} ({1}) ",
                    this.GetTableName(false),
                    string.Join(",", columnNames)
                ).AppendReplaced(
                    query.ToString(),
                    '?',
                    (count) => this.RegisterClausuleParameter(query.Info.Where.Parameters[count - 1])
                );
        }

        protected override void ConfigureBulkInsert(Row[] rows)
        {
            this.ConfigureInsert(rows[0].Cells, false);

            for (int i = 1; i < rows.Length; i++)
                this.QueryBuilder.AppendFormat(", ({0})", string.Join(", ", rows[i].Cells.Select(c => this.RegisterCellValue(c))));
        }

        protected override void ConfigureDelete()
        {
            this.QueryBuilder.AppendFormat("DELETE");
            this.ApplyDeleteJoins();
            this.QueryBuilder.AppendFormat(" FROM {0}", this.Info.TableName.GetName(true, this.Info.Config));

            this.ApplyJoins();
            this.WriteWhere(true);

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            if (this.Query.Limit > 0)
                this.QueryBuilder.AppendFormat(" LIMIT {0}", this.Query.Limit);
        }

        private void ApplyDeleteJoins()
        {
            if (this.Info.Joins.Count == 0)
                return;

            this.QueryBuilder.AppendFormat(" {0}", this.Info.TableName.TryGetAlias(this.Info.Config));

            if (!(this.Query.deleteJoins?.Length >= 1))
                return;

            foreach (var join in this.Info.Joins)
                this.QueryBuilder.AppendFormat(",{0}", join.Info.TableName.TryGetAlias(join.Info.Config));
        }

        protected override void ConfigureInsert(Cell[] cells, bool getGeneratedId)
        {
            this.QueryBuilder.AppendFormat(
                "INSERT INTO {0} ({1}) VALUES ({2})",
                this.GetTableName(false),
                string.Join(", ", cells.Select(c => this.ApplyTableColumnConfig(c.Name))),
                string.Join(", ", cells.Select(c => this.RegisterCellValue(c)))
            );

            if (getGeneratedId)
                this.QueryBuilder.Append("; SELECT LAST_INSERT_ID();");
        }

        protected override void ConfigureCount(Column column)
        {
            bool safeDistinct = (column == null || column == Column.All) && this.Query.Distinct;

            if (safeDistinct)
                this.QueryBuilder.Append("SELECT COUNT(*) FROM (");

            this.ConfigureSelect(true, safeDistinct ? null : column);

            if (safeDistinct)
                this.QueryBuilder.AppendFormat(") `count`");
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, null);
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn)
        {
            bool isCount = countColumn != null;
            this.QueryBuilder.Append("SELECT ");

            if (isCount)
                this.QueryBuilder.Append("COUNT(");
            if (this.Query.Distinct)
                this.QueryBuilder.Append("DISTINCT ");

            if (isCount)
            {
                this.QueryBuilder.Append(string.Join(", ", WriteSelect(countColumn)));
                this.QueryBuilder.Append(')');
            }
            else
            {
                this.WriteSelectColumns();
            }

            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);

            this.WriteGroupBy();

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.ValidateOffset();
            if (this.Query.Limit == null)
                return;

            this.QueryBuilder.AppendFormat(" LIMIT {0}", this.Query.Limit);
            if (this.Query.Offset != null)
                this.QueryBuilder.AppendFormat(" OFFSET {0}", this.Query.Offset);
        }

        protected bool CanWriteOrderby()
        {
            if (this.Info.Select.Length != 1)
                return true;

            return !this.Info.Select[0].ToExpression(this.Info.ToReadOnly()).ToString().ToLower().StartsWith("count(");
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
                this.QueryBuilder.AppendFormat(" ORDER BY {0}", string.Join(", ", this.Info.Orders.Select(col => $"{col.Column.ToExpression(this.Info.ToReadOnly())} {col.Order}")));
        }

        protected virtual void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.QueryBuilder.AppendFormat(" {0} JOIN {1} ON ", join.Type, this.GetTableName(join, true));
            this.WriteWhereContent(join.Info);
        }

        protected override void ConfigureUpdate(Cell[] cells)
        {
            this.QueryBuilder.AppendFormat("UPDATE {0}", this.GetTableName(false));
            this.ApplyJoins();

            this.QueryBuilder.AppendFormat(
                " SET {0}",
                string.Join(", ", cells.Select(c => $"{this.ApplyTableColumnConfig(c.Name)} = {this.RegisterCellValue(c)}"))
            );

            this.WriteWhere(true);
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (this.Info.Where.Empty)
                return;

            if (!configureParameters)
            {
                this.QueryBuilder.AppendFormat(" WHERE {0}", this.Info.Where);
                return;
            }

            this.QueryBuilder.Append(" WHERE ");
            this.WriteWhereContent(this.Info);
        }

        protected void WriteWhereContent(QueryInfo info)
        {
            this.QueryBuilder.AppendReplaced(
                info.Where.ToString(),
                '?',
                (count) => this.RegisterClausuleParameter(info.Where.Parameters[count - 1])
            );
        }

        private void ValidateOffset()
        {
            if (this.Query.Offset != null && this.Query.Limit == null)
                throw new InvalidOperationException($"You cannot use {nameof(Query)}.{nameof(Query.Offset)} without {nameof(Query)}.{nameof(Query.Limit)}.");
        }
    }
}
