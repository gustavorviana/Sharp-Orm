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
            this.QueryBuilder.AppendFormat("DELETE");
            if (this.Query.Limit > 0)
                this.QueryBuilder.AppendFormat(" TOP({0})", this.Query.Limit);

            this.ApplyDeleteJoins();
            this.QueryBuilder.AppendFormat(" FROM {0}", this.Info.TableName.GetName(true, this.Info.Config));

            if (this.Query.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(true);
        }

        private void ApplyDeleteJoins()
        {
            if (this.Info.Joins.Count == 0 && !this.Query.IsNoLock())
                return;

            this.QueryBuilder.AppendFormat(" {0}", this.Info.TableName.TryGetAlias(this.Info.Config));

            if (!(this.Query.deleteJoins?.Length >= 1))
                return;

            foreach (var join in this.Info.Joins)
                this.QueryBuilder.AppendFormat(",{0}", join.Info.TableName.TryGetAlias(join.Info.Config));
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            this.QueryBuilder.AppendFormat(
                "UPDATE {0} SET {1}",
                this.GetTableName(false),
                string.Join(", ", cells.Select(c => $"{this.ApplyTableColumnConfig(c.Name)} = {this.RegisterCellValue(c)}"))
            );

            if (this.Info.Joins.Any() || this.Query.IsNoLock())
                this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(false));

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
            this.QueryBuilder.Append("SELECT ");

            if (this.Query.Distinct && !this.Info.IsCount() && countColumn == null)
                this.QueryBuilder.Append("DISTINCT ");

            if (HasLimit && !HasOffset && !this.Info.IsCount())
                this.QueryBuilder.AppendFormat("TOP ({0}) ", this.Query.Limit);

            if (isCount)
                this.WriteCountColumn(countColumn);
            else
                this.WriteSelectColumns();

            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));

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
            this.QueryBuilder.AppendFormat(" OFFSET {0} ROWS", this.Query.Offset);

            if (HasLimit)
                this.QueryBuilder.AppendFormat(" FETCH NEXT {0} ROWS ONLY", this.Query.Limit);
        }

        private void WriteCountColumn(Column column)
        {
            if (this.Info.Select.Length > 1)
                throw new NotSupportedException("It's not possible to count more than one column.");

            string countCol = column?.GetCountColumn();
            if (string.IsNullOrEmpty(countCol))
                throw new NotSupportedException("The name of a column or '*' must be entered for counting.");

            if (countCol == "*" || countCol.EndsWith(".*"))
                this.QueryBuilder.AppendFormat("COUNT(*)");
            else
                this.QueryBuilder.AppendFormat("COUNT({0}{1})", this.Query.Distinct ? "DISTINCT " : "", WriteSelect(column));
        }

        private void WriteSelectWithOldPagination(bool configureWhereParams, Column countColunm)
        {
            this.QueryBuilder.Append("SELECT * FROM (");
            this.WriteRowNumber();
            if (countColunm == null)
                this.WriteSelectColumns();
            else
                this.QueryBuilder.Append(WriteSelect(countColunm));
            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));

            if (this.Query.IsNoLock())
                this.QueryBuilder.Append(" WITH (NOLOCK)");

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();
            this.QueryBuilder.AppendFormat(") {0} ", this.GetTableNameIfNoAlias());
            this.ApplyPagination();
        }

        private string GetTableNameIfNoAlias()
        {
            if (string.IsNullOrEmpty(this.Info.Alias))
                return this.GetTableName(false);

            return this.ApplyTableColumnConfig(this.Info.Alias);
        }

        private void ApplyPagination()
        {
            if (this.Query.Offset != null && this.Query.Limit != null)
                this.QueryBuilder.AppendFormat("WHERE [grammar_rownum] BETWEEN {0} AND {1}", this.Query.Offset + 1, this.Query.Offset + this.Query.Limit);
            else if (this.Query.Offset != null)
                this.QueryBuilder.AppendFormat("WHERE [grammar_rownum] > {0}", this.Query.Offset);
        }

        private void WriteRowNumber()
        {
            this.ValidateOffsetOrderBy();

            this.QueryBuilder.Append("SELECT ROW_NUMBER() OVER(ORDER BY ");
            this.QueryBuilder.Append(string.Join(", ", this.Info.Orders.Select(col => $"{col.Column.ToExpression(this.Info.ToReadOnly())} {col.Order}")));
            this.QueryBuilder.Append(") AS [grammar_rownum], ");
        }

        private void ValidateOffsetOrderBy()
        {
            if (this.Info.Orders.Length == 0)
                throw new InvalidOperationException($"You cannot use {nameof(Query)}.{nameof(Query.Offset)} without defining a sort column.");
        }
    }
}
