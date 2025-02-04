using System;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerSelectGrammar : SqlServerGrammarBase
    {
        protected SqlServerQueryConfig Config => Info.Config as SqlServerQueryConfig;

        protected bool HasOffset => Query.Offset is int offset && offset >= 0;

        public SqlServerSelectGrammar(GrammarBase owner) : base(owner)
        {
        }

        public void BuildCount(Column column)
        {
            if (!Config.UseOldPagination && column != null && Query.Distinct != column.IsAll())
            {
                builder.Add("SELECT ");
                WriteCountColumn(column);
                WriteSelectFrom(true);
                return;
            }

            builder.Add("SELECT COUNT(*) FROM (");
            ConfigureSelect(true, column, true);
            builder.Add(") AS [count]");
        }

        private QueryBuilder WriteCountColumn(Column column)
        {
            if (column.IsCount) return builder.AddExpression(column);

            string countCol = column?.GetCountColumn();
            if (string.IsNullOrEmpty(countCol))
                throw new NotSupportedException("The name of a column or '*' must be entered for counting.");

            builder.Add("COUNT(");
            if (countCol == "*" || countCol.EndsWith(".*"))
                return builder.Add("*)");

            if (Query.Distinct)
                builder.Add("DISTINCT ");

            WriteSelect(column);
            return builder.Add(')');
        }

        public void BuildSelect(bool configureWhereParams)
        {
            ConfigureSelect(configureWhereParams, null, false);
        }


        private void WriteSelectWithOldPagination(bool configureWhereParams, Column countColunm, bool isCount)
        {
            builder.Add("SELECT * FROM (");
            WriteRowNumber();

            if (countColunm == null) WriteSelectColumns();
            else WriteSelect(countColunm);

            builder.Add(" FROM ").Add(GetTableName(true));

            if (Query.IsNoLock())
                builder.Add(" WITH (NOLOCK)");

            ApplyJoins();
            WriteWhere(configureWhereParams);
            WriteGroupBy();
            builder.Add(") ").Add(TryGetTableAlias(Query));

            if (!isCount)
                ApplyPagination();
        }


        internal void WriteSelectFrom(bool configureWhereParams)
        {
            builder.Add(" FROM ").Add(GetTableName(true));

            WriteOptions();
            ApplyJoins();
            WriteWhere(configureWhereParams);
            WriteGroupBy();
        }

        internal void WritePagination()
        {
            if (!HasOffset)
                return;

            ValidateOffsetOrderBy();
            builder.Add(" OFFSET ").Add(Query.Offset).Add(" ROWS");

            if (Query.Limit >= 0)
                builder.Add(" FETCH NEXT ").Add(Query.Limit).Add(" ROWS ONLY");
        }

        private void WriteOptions()
        {
            if (Query.IsNoLock())
                builder.Add(" WITH (NOLOCK)");
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn, bool isCount)
        {
            if (HasOffset && Config.UseOldPagination) WriteSelectWithOldPagination(configureWhereParams, countColumn, isCount);
            else WriteSelect(configureWhereParams, isCount);
        }

        private void WriteSelect(bool configureWhereParams, bool isCount)
        {
            builder.Add("SELECT");

            if (Query.Distinct)
                builder.Add(" DISTINCT");

            if (!HasOffset && !isCount)
                AddLimit();

            builder.Add(' ');
            WriteSelectColumns();
            WriteSelectFrom(configureWhereParams);

            if (isCount)
                return;

            ApplyOrderBy();
            WritePagination();
        }

        private void ApplyPagination()
        {
            builder.Add(" WHERE [grammar_rownum] ");

            if (Query.Offset != null && Query.Limit != null)
                builder.AddFormat("BETWEEN {0} AND {1}", Query.Offset + 1, Query.Offset + Query.Limit);
            else if (Query.Offset != null)
                builder.Add("> ").Add(Query.Offset);
        }

        private void WriteRowNumber()
        {
            ValidateOffsetOrderBy();

            builder.Add("SELECT ROW_NUMBER() OVER(ORDER BY ");
            ApplyOrderBy(Info.Orders, true);
            builder.Add(") AS [grammar_rownum], ");
        }

    }
}
