using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureInsertQuery(QueryBase query, IEnumerable<string> columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.QueryBuilder.AppendReplaced(
                query.ToString(),
                '?',
                (count) => this.RegisterClausuleParameter(query.Info.Where.Parameters[count - 1])
            );
        }

        protected override void ConfigureInsertExpression(SqlExpression expression, IEnumerable<string> columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.QueryBuilder.AppendReplaced(
                expression.ToString(),
                '?',
                (count) => this.RegisterClausuleParameter(expression.Parameters[count - 1])
            );
        }

        protected override void ConfigureBulkInsert(IEnumerable<Row> rows)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new ArgumentNullException(nameof(rows));

                this.ConfigureInsert(@enum.Current.Cells, false);

                while (@enum.MoveNext())
                {
                    this.QueryBuilder.Append(", ");
                    this.AppendInsertCells(@enum.Current.Cells);
                }
            }
        }

        protected override void ConfigureDelete()
        {
            this.QueryBuilder.Append("DELETE");
            this.ApplyDeleteJoins();
            this.QueryBuilder.Append(" FROM ").Append(this.Info.TableName.GetName(true, this.Info.Config));

            this.ApplyJoins();
            this.WriteWhere(true);

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            if (this.Query.Limit > 0)
                this.QueryBuilder.Append(" LIMIT ").Append(this.Query.Limit);
        }

        private void ApplyDeleteJoins()
        {
            if (!this.Info.Joins.Any())
                return;

            this.QueryBuilder
                .Append(' ')
                .Append(this.Info.TableName.TryGetAlias(this.Info.Config));

            if (!(this.Query.deleteJoins?.Any() ?? false))
                return;

            foreach (var join in this.Info.Joins.Where(j => this.CanDeleteJoin(j.Info)))
                this.QueryBuilder.AppendFormat(",{0}", join.Info.TableName.TryGetAlias(join.Info.Config));
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            this.AppendInsertHeader(cells.Select(c => this.ApplyTableColumnConfig(c.Name)));
            this.QueryBuilder.Append("VALUES ");
            this.AppendInsertCells(cells);

            if (getGeneratedId)
                this.QueryBuilder.Append("; SELECT LAST_INSERT_ID();");
        }

        protected void AppendInsertHeader(IEnumerable<string> columns)
        {
            this.QueryBuilder
                .Append("INSERT INTO ")
                .Append(this.GetTableName(false))
                .Append(" (")
                .AppendJoin(", ", columns)
                .Append(") ");
        }

        protected void AppendInsertCells(IEnumerable<Cell> cells)
        {
            this.QueryBuilder.Append('(').AppendJoin(", ", cells.Select(c => this.RegisterCellValue(c))).Append(')');
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

            this.QueryBuilder.Append(" FROM ").Append(this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);

            this.WriteGroupBy();

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.ValidateOffset();
            if (this.Query.Limit == null)
                return;

            this.QueryBuilder.Append(" LIMIT ").Append(this.Query.Limit);

            if (this.Query.Offset != null)
                this.QueryBuilder.Append(" OFFSET ").Append(this.Query.Offset);
        }

        protected bool CanWriteOrderby()
        {
            if (this.Info.Select.Length != 1)
                return true;

            return !this.Info.Select[0].ToExpression(this.Info.ToReadOnly()).ToString().ToLower().StartsWith("count(");
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            this.QueryBuilder.Append("UPDATE ").Append(this.GetTableName(false));
            this.ApplyJoins();

            this.QueryBuilder.Append(" SET ");
            this.QueryBuilder.AppendJoin(WriteUpdateCell, ", ", cells);

            this.WriteWhere(true);
        }

        protected void ApplyJoins()
        {
            if (this.Info.Joins.Count > 0)
                foreach (var join in this.Info.Joins)
                    this.WriteJoin(join);
        }

        protected virtual void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.QueryBuilder
                .Append(' ')
                .Append(join.Type)
                .Append(" JOIN ")
                .Append(this.GetTableName(join, true))
                .Append(" ON ");

            this.WriteWhereContent(join.Info);
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (this.Info.Where.Empty)
                return;

            this.QueryBuilder.Append(" WHERE ");
            if (configureParameters) this.WriteWhereContent(this.Info);
            else this.QueryBuilder.Append(this.Info.Where);
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
