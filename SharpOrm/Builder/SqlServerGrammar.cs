using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    public class SqlServerGrammar : MysqlGrammar
    {
        protected SqlServerQueryConfig Config => this.Info.Config as SqlServerQueryConfig;
        protected bool HasOffset => this.Query.Offset is int offset && offset > 0;
        protected bool HasLimit => this.Query.Limit is int limit && limit > 0;

        public SqlServerGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureDelete()
        {
            this.QueryBuilder.Append("DELETE");

            if (this.Query.Limit > 0)
                this.AppendLimit();

            this.ApplyDeleteJoins();
            this.QueryBuilder.Append(" FROM ").Append(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        protected override void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.QueryBuilder
                .Append(' ')
                .Append(join.Type)
                .Append(" JOIN ")
                .Append(this.GetTableName(join, true));

            if (join.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.QueryBuilder.Append(" ON ");

            this.WriteWhereContent(join.Info);
        }

        protected override bool CanApplyDeleteJoins()
        {
            return base.CanApplyDeleteJoins() || this.Query.IsNoLock();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                this.QueryBuilder.Append("UPDATE ").Append(this.GetTableName(false));
                this.QueryBuilder.Append(" SET ");
                this.QueryBuilder.AppendJoin(WriteUpdateCell, ", ", en);
            }

            if (this.Info.Joins.Any() || this.Query.IsNoLock())
                this.QueryBuilder.Append(" FROM ").Append(this.GetTableName(false));

            if (this.Query.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        protected override void ConfigureCount(Column column)
        {
            bool isOldOrDistinct = this.HasOffset || this.Config.UseOldPagination || (column == null || column == Column.All) && this.Query.Distinct;
            if (isOldOrDistinct)
                this.QueryBuilder.Append("SELECT COUNT(*) FROM (");

            this.ConfigureSelect(true, column);

            if (isOldOrDistinct)
                this.QueryBuilder.Append(") AS [count]");
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, null);
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn)
        {
            if (this.HasOffset && this.Config.UseOldPagination) this.WriteSelectWithOldPagination(configureWhereParams, countColumn);
            else this.WriteSelect(configureWhereParams, countColumn);
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            base.ConfigureInsert(cells, false);

            if (getGeneratedId)
                this.QueryBuilder.Append("; SELECT SCOPE_IDENTITY();");
        }

        private void WriteSelect(bool configureWhereParams, Column countColumn)
        {
            bool isCount = countColumn != null;
            this.QueryBuilder.Append("SELECT");

            if (this.Query.Distinct && !this.Info.IsCount() && countColumn == null)
                this.QueryBuilder.Append(" DISTINCT");

            if (HasLimit && !HasOffset && !this.Info.IsCount())
                this.AppendLimit();

            this.QueryBuilder.Append(' ');

            if (isCount)
                this.WriteCountColumn(countColumn);
            else
                this.WriteSelectColumns();

            this.QueryBuilder.Append(" FROM ").Append(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();

            if (isCount)
                return;

            this.ApplyOrderBy();
            if (!HasOffset)
                return;

            this.ValidateOffsetOrderBy();
            this.QueryBuilder.Append(" OFFSET ").Append(this.Query.Offset).Append(" ROWS");

            if (HasLimit)
                this.QueryBuilder.Append(" FETCH NEXT ").Append(this.Query.Limit).Append(" ROWS ONLY");
        }

        private void AppendLimit()
        {
            this.QueryBuilder.Append(" TOP(").Append(this.Query.Limit).Append(')');
        }

        private System.Text.StringBuilder WriteCountColumn(Column column)
        {
            string countCol = column?.GetCountColumn();
            if (string.IsNullOrEmpty(countCol))
                throw new NotSupportedException("The name of a column or '*' must be entered for counting.");

            this.QueryBuilder.Append("COUNT(");
            if (countCol == "*" || countCol.EndsWith(".*"))
                return this.QueryBuilder.Append("*)");

            if (this.Query.Distinct)
                this.QueryBuilder.Append("DISTINCT ");

            return this.QueryBuilder.Append(WriteSelect(column)).Append(')');
        }

        private void WriteSelectWithOldPagination(bool configureWhereParams, Column countColunm)
        {
            this.QueryBuilder.Append("SELECT * FROM (");
            this.WriteRowNumber();
            if (countColunm == null)
                this.WriteSelectColumns();
            else
                this.QueryBuilder.Append(WriteSelect(countColunm));
            this.QueryBuilder.Append(" FROM ").Append(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();
            this.QueryBuilder.Append(") ").Append(this.TryGetTableAlias(this.Query));
            this.ApplyPagination();
        }

        private void ApplyPagination()
        {
            this.QueryBuilder.Append(" WHERE [grammar_rownum] ");

            if (this.Query.Offset != null && this.Query.Limit != null)
                this.QueryBuilder.AppendFormat("BETWEEN {0} AND {1}", this.Query.Offset + 1, this.Query.Offset + this.Query.Limit);
            else if (this.Query.Offset != null)
                this.QueryBuilder.Append("> ").Append(this.Query.Offset);
        }

        private void WriteRowNumber()
        {
            this.ValidateOffsetOrderBy();

            this.QueryBuilder.Append("SELECT ROW_NUMBER() OVER(ORDER BY ");
            this.ApplyOrderBy(this.Info.Orders, true);
            this.QueryBuilder.Append(") AS [grammar_rownum], ");
        }

        private void ValidateOffsetOrderBy()
        {
            if (this.Info.Orders.Length == 0)
                throw new InvalidOperationException($"You cannot use {nameof(Query)}.{nameof(Query.Offset)} without defining a sort column.");
        }
    }
}
