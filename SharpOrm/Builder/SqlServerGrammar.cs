using System;
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

        protected override void ConfigureCount()
        {
            this.QueryBuilder.Append("SELECT COUNT(*) FROM (");
            this.ConfigureSelect(true, true);
            this.QueryBuilder.Append(") AS [count]");
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, false);
        }

        private void ConfigureSelect(bool configureWhereParams, bool isCount)
        {
            if (this.HasOffset && this.Config.UseOldPagination) this.WriteSelectWithOldPagination(configureWhereParams);
            else this.WriteSelect(configureWhereParams, isCount);
        }

        protected override void ConfigureInsert(Cell[] cells, bool getGeneratedId)
        {
            base.ConfigureInsert(cells, false);

            if (getGeneratedId)
                this.QueryBuilder.Append("; SELECT SCOPE_IDENTITY();");
        }

        private void WriteSelect(bool configureWhereParams, bool isCount)
        {
            this.QueryBuilder.Append("SELECT ");

            if (this.Query.Distinct && !this.Info.IsCount())
                this.QueryBuilder.Append("DISTINCT ");

            if (HasLimit && !HasOffset && !this.Info.IsCount())
                this.QueryBuilder.AppendFormat("TOP ({0}) ", this.Query.Limit);

            this.WriteSelectColumns();
            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();

            if (isCount)
                return;

            this.ApplyOrderBy();
            if (!HasOffset)
                return;

            this.ValidateOffsetOrderBy();
            this.QueryBuilder.Append($" OFFSET {this.Query.Offset} ROWS");

            if (HasLimit)
                this.QueryBuilder.Append($" FETCH NEXT {this.Query.Limit} ROWS ONLY");
        }

        private void WriteSelectWithOldPagination(bool configureWhereParams)
        {
            this.QueryBuilder.Append("SELECT * FROM (");
            this.WriteRowNumber();
            this.WriteSelectColumns();
            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));
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
                throw new InvalidOperationException("You cannot page the result without a field for ordering.");
        }
    }
}
