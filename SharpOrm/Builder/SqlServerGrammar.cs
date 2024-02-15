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
            this.ThrowOffsetNotSupported();
            this.Constructor.Add("DELETE");

            this.AddLimit();
            this.ApplyDeleteJoins();
            this.Constructor.Add(" FROM ").Add(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.Constructor.Add(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        protected override void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.Constructor
                .Add(' ')
                .Add(join.Type)
                .Add(" JOIN ")
                .Add(this.GetTableName(join, true));

            if (join.IsNoLock())
                this.Constructor.Add(" WITH (NOLOCK)");

            this.Constructor.Add(" ON ");

            this.WriteWhereContent(join.Info);
        }

        protected override bool CanApplyDeleteJoins()
        {
            return base.CanApplyDeleteJoins() || this.Query.IsNoLock();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            this.ThrowOffsetNotSupported();
            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                this.Constructor.Add("UPDATE ").Add(this.GetTableName(false));
                this.AddLimit();
                this.Constructor.Add(" SET ");
                this.Constructor.AddJoin(WriteUpdateCell, ", ", en);
            }

            if (this.Info.Joins.Any() || this.Query.IsNoLock())
                this.Constructor.Add(" FROM ").Add(this.GetTableName(false));

            if (this.Query.IsNoLock())
                this.Constructor.Add(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        protected override void ConfigureCount(Column column)
        {
            bool isOldOrDistinct = this.HasOffset || this.Config.UseOldPagination || (column == null || column == Column.All) && this.Query.Distinct;
            if (isOldOrDistinct)
                this.Constructor.Add("SELECT COUNT(*) FROM (");

            this.ConfigureSelect(true, column);

            if (isOldOrDistinct)
                this.Constructor.Add(") AS [count]");
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
                this.Constructor.Add("; SELECT SCOPE_IDENTITY();");
        }

        private void WriteSelect(bool configureWhereParams, Column countColumn)
        {
            bool isCount = countColumn != null;
            this.Constructor.Add("SELECT");

            if (this.Query.Distinct && !this.Info.IsCount() && countColumn == null)
                this.Constructor.Add(" DISTINCT");

            if (!HasOffset && !this.Info.IsCount())
                this.AddLimit();

            this.Constructor.Add(' ');

            if (isCount)
                this.WriteCountColumn(countColumn);
            else
                this.WriteSelectColumns();

            this.Constructor.Add(" FROM ").Add(this.GetTableName(true));

            this.WriteOptions();
            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();

            if (isCount)
                return;

            this.ApplyOrderBy();
            if (!HasOffset)
                return;

            this.ValidateOffsetOrderBy();
            this.Constructor.Add(" OFFSET ").Add(this.Query.Offset).Add(" ROWS");

            if (HasLimit)
                this.Constructor.Add(" FETCH NEXT ").Add(this.Query.Limit).Add(" ROWS ONLY");
        }

        private void WriteOptions()
        {
            if (this.Query.IsNoLock())
                this.Constructor.Add(" WITH (NOLOCK)");
        }

        private void AddLimit()
        {
            if (this.Query.Limit is int limit && limit > 0)
                this.Constructor.Add(" TOP(").Add(limit).Add(')');
        }

        private QueryConstructor WriteCountColumn(Column column)
        {
            string countCol = column?.GetCountColumn();
            if (string.IsNullOrEmpty(countCol))
                throw new NotSupportedException("The name of a column or '*' must be entered for counting.");

            this.Constructor.Add("COUNT(");
            if (countCol == "*" || countCol.EndsWith(".*"))
                return this.Constructor.Add("*)");

            if (this.Query.Distinct)
                this.Constructor.Add("DISTINCT ");

            WriteSelect(column);
            return this.Constructor.Add(')');
        }

        private void WriteSelectWithOldPagination(bool configureWhereParams, Column countColunm)
        {
            this.Constructor.Add("SELECT * FROM (");
            this.WriteRowNumber();

            if (countColunm == null) this.WriteSelectColumns();
            else WriteSelect(countColunm);

            this.Constructor.Add(" FROM ").Add(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.Constructor.Add(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();
            this.Constructor.Add(") ").Add(this.TryGetTableAlias(this.Query));
            this.ApplyPagination();
        }

        private void ApplyPagination()
        {
            this.Constructor.Add(" WHERE [grammar_rownum] ");

            if (this.Query.Offset != null && this.Query.Limit != null)
                this.Constructor.AddFormat("BETWEEN {0} AND {1}", this.Query.Offset + 1, this.Query.Offset + this.Query.Limit);
            else if (this.Query.Offset != null)
                this.Constructor.Add("> ").Add(this.Query.Offset);
        }

        private void WriteRowNumber()
        {
            this.ValidateOffsetOrderBy();

            this.Constructor.Add("SELECT ROW_NUMBER() OVER(ORDER BY ");
            this.ApplyOrderBy(this.Info.Orders, true);
            this.Constructor.Add(") AS [grammar_rownum], ");
        }

        private void ValidateOffsetOrderBy()
        {
            if (this.Info.Orders.Length == 0)
                throw new InvalidOperationException($"You cannot use {nameof(Query)}.{nameof(Query.Offset)} without defining a sort column.");
        }
    }
}
