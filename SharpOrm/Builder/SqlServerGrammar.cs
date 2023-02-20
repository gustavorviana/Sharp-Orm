﻿using System;
using System.Linq;

namespace SharpOrm.Builder
{
    public class SqlServerGrammar : MysqlGrammar
    {
        protected SqlServerQueryConfig Config => this.Info.Config as SqlServerQueryConfig;
        public SqlServerGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            if (this.Query.Offset != null && this.Config.UseOldPagination) this.WriteSelectWithOldPagination(configureWhereParams);
            else this.WriteSelectPagination(configureWhereParams);
        }

        protected override void ConfigureInsert(Cell[] cells, bool getGeneratedId)
        {
            base.ConfigureInsert(cells, false);

            if (getGeneratedId)
                this.QueryBuilder.Append("; SELECT SCOPE_IDENTITY();");
        }

        private void WriteSelectPagination(bool configureWhereParams)
        {
            this.QueryBuilder.Append("SELECT ");

            if (this.Query.Limit != null && this.Query.Offset == null)
                this.QueryBuilder.AppendFormat("TOP ({0}) ", this.Query.Limit);

            if (this.Query.Distinct)
                this.QueryBuilder.Append("DISTINCT ");

            this.WriteSelectColumns();
            this.QueryBuilder.AppendFormat(" FROM {0}", this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);
            this.WriteGroupBy();

            if (this.IsCount())
                return;

            this.ApplyOrderBy();

            if (this.Query.Offset == null)
                return;

            this.QueryBuilder.Append($" OFFSET {this.Query.Offset} ROWS");

            if (this.Query.Limit != null)
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

        private bool IsCount()
        {
            if (this.Info.Select.Length != 1)
                return true;

            string select = this.Info.Select[0].ToExpression(this.Info.ToReadOnly()).ToString().ToLower();
            return select.StartsWith("count(");
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
            if (this.Info.Orders.Length == 0)
                throw new InvalidOperationException("You cannot page the result without a field for ordering.");

            this.QueryBuilder.Append("SELECT ROW_NUMBER() OVER(ORDER BY ");
            this.QueryBuilder.Append(string.Join(", ", this.Info.Orders.Select(col => $"{col.Column.ToExpression(this.Info.ToReadOnly())} {col.Order}")));
            this.QueryBuilder.Append(") AS [grammar_rownum], ");
        }
    }
}
