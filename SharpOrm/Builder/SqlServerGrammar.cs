using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQL Server.
    /// </summary>
    public class SqlServerGrammar : MysqlGrammar
    {
        protected SqlServerQueryConfig Config => this.Info.Config as SqlServerQueryConfig;
        protected bool HasOffset => this.Query.Offset is int offset && offset >= 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerGrammar"/> class with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        public SqlServerGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureDelete()
        {
            this.ThrowDeleteJoinsNotSupported();
            this.ThrowOffsetNotSupported();
            this.builder.Add("DELETE");
            this.AddLimit();

            if (this.Query.IsNoLock() || this.Query.Info.Joins.Any())
                this.builder.Add(' ').Add(this.TryGetTableAlias(this.Query));

            this.builder.Add(" FROM ").Add(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.builder.Add(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        protected override void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.builder
                .Add(' ')
                .Add(join.Type)
                .Add(" JOIN ")
                .Add(this.GetTableName(join, true));

            if (join.IsNoLock())
                this.builder.Add(" WITH (NOLOCK)");

            this.builder.Add(" ON ");

            this.WriteWhereContent(join.Info);
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            this.ThrowOffsetNotSupported();
            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                this.builder.Add("UPDATE ").Add(this.Info.Joins.Any() ? ApplyNomenclature(this.Info.TableName.ToString()) : this.GetTableName(false));
                this.AddLimit();
                this.builder.Add(" SET ");
                this.builder.AddJoin(WriteUpdateCell, ", ", en);
            }

            if (this.Info.Joins.Any() || this.Query.IsNoLock())
                this.builder.Add(" FROM ").Add(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.builder.Add(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        protected override void ConfigureCount(Column column)
        {
            if (!this.Config.UseOldPagination && column != null && (this.Query.Distinct != column.IsAll()))
            {
                this.builder.Add("SELECT ");
                this.WriteCountColumn(column);
                this.WriteSelectFrom(true);
                return;
            }

            this.builder.Add("SELECT COUNT(*) FROM (");
            this.ConfigureSelect(true, column, true);
            this.builder.Add(") AS [count]");
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, null, false);
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn, bool isCount)
        {
            if (this.HasOffset && this.Config.UseOldPagination) this.WriteSelectWithOldPagination(configureWhereParams, countColumn, isCount);
            else this.WriteSelect(configureWhereParams, isCount);
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            base.ConfigureInsert(cells, false);

            if (getGeneratedId && this.Query.ReturnsInsetionId)
                this.builder.Add("; SELECT SCOPE_IDENTITY();");
        }

        private void WriteSelect(bool configureWhereParams, bool isCount)
        {
            this.builder.Add("SELECT");

            if (this.Query.Distinct)
                this.builder.Add(" DISTINCT");

            if (!HasOffset && !isCount)
                this.AddLimit();

            this.builder.Add(' ');
            this.WriteSelectColumns();
            this.WriteSelectFrom(configureWhereParams);

            if (isCount)
                return;

            this.ApplyOrderBy();
            this.WritePagination();
        }

        internal SqlExpression GetSelectFrom()
        {
            this.builder.Clear();
            WriteSelectFrom(true);
            this.ApplyOrderBy();
            this.WritePagination();

            try
            {
                return this.builder.ToExpression();
            }
            finally
            {
                this.builder.Clear();
            }
        }

        private void WriteSelectFrom(bool configureWhereParams)
        {
            this.builder.Add(" FROM ").Add(this.GetTableName(true));

            this.WriteOptions();
            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();
        }

        private void WritePagination()
        {
            if (!HasOffset)
                return;

            this.ValidateOffsetOrderBy();
            this.builder.Add(" OFFSET ").Add(this.Query.Offset).Add(" ROWS");

            if (this.Query.Limit >= 0)
                this.builder.Add(" FETCH NEXT ").Add(this.Query.Limit).Add(" ROWS ONLY");
        }

        private void WriteOptions()
        {
            if (this.Query.IsNoLock())
                this.builder.Add(" WITH (NOLOCK)");
        }

        private void AddLimit()
        {
            if (this.Query.Limit is int limit && limit >= 0)
                this.builder.Add(" TOP(").Add(limit).Add(')');
        }

        private QueryBuilder WriteCountColumn(Column column)
        {
            if (column.IsCount) return this.builder.AddExpression(column);

            string countCol = column?.GetCountColumn();
            if (string.IsNullOrEmpty(countCol))
                throw new NotSupportedException("The name of a column or '*' must be entered for counting.");

            this.builder.Add("COUNT(");
            if (countCol == "*" || countCol.EndsWith(".*"))
                return this.builder.Add("*)");

            if (this.Query.Distinct)
                this.builder.Add("DISTINCT ");

            WriteSelect(column);
            return this.builder.Add(')');
        }

        private void WriteSelectWithOldPagination(bool configureWhereParams, Column countColunm, bool isCount)
        {
            this.builder.Add("SELECT * FROM (");
            this.WriteRowNumber();

            if (countColunm == null) this.WriteSelectColumns();
            else WriteSelect(countColunm);

            this.builder.Add(" FROM ").Add(this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.builder.Add(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();
            this.builder.Add(") ").Add(this.TryGetTableAlias(this.Query));

            if (!isCount)
                this.ApplyPagination();
        }

        private void ApplyPagination()
        {
            this.builder.Add(" WHERE [grammar_rownum] ");

            if (this.Query.Offset != null && this.Query.Limit != null)
                this.builder.AddFormat("BETWEEN {0} AND {1}", this.Query.Offset + 1, this.Query.Offset + this.Query.Limit);
            else if (this.Query.Offset != null)
                this.builder.Add("> ").Add(this.Query.Offset);
        }

        private void WriteRowNumber()
        {
            this.ValidateOffsetOrderBy();

            this.builder.Add("SELECT ROW_NUMBER() OVER(ORDER BY ");
            this.ApplyOrderBy(this.Info.Orders, true);
            this.builder.Add(") AS [grammar_rownum], ");
        }

        private void ValidateOffsetOrderBy()
        {
            if (this.Info.Orders.Length == 0)
                throw new InvalidOperationException($"You cannot use {nameof(Query)}.{nameof(Query.Offset)} without defining a sort column.");
        }
    }
}
