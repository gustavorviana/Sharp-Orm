using System;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerSelectGrammar : SqlServerGrammarBase
    {
        private Version useNewPaginationAt = new Version(11, 0);
        protected SqlServerQueryConfig Config => Info.Config as SqlServerQueryConfig;

        protected bool HasOffset => Query.Offset is int offset && offset >= 0;

        public SqlServerSelectGrammar(GrammarBase owner) : base(owner)
        {
        }

        public void BuildCount(Column column)
        {
            if (!NeedObsoleteCount(column))
            {
                Builder.Add("SELECT ");
                WriteCountColumn(column);
                WriteSelectFrom(true);
                return;
            }

            Builder.Add("SELECT COUNT(*) FROM (");
            ConfigureSelect(true, column, true);
            Builder.Add(") AS [count]");
        }

        private bool NeedObsoleteCount(Column column)
        {
            return column == null || (Query.Distinct && column.IsAll()) || UseOldPagination();
        }

        private QueryBuilder WriteCountColumn(Column column)
        {
            if (column.IsCount) return Builder.AddExpression(column);

            string countCol = column?.GetCountColumn();
            if (string.IsNullOrEmpty(countCol))
                throw new NotSupportedException("The name of a column or '*' must be entered for counting.");

            Builder.Add("COUNT(");
            if (countCol == "*" || countCol.EndsWith(".*"))
                return Builder.Add("*)");

            if (Query.Distinct)
                Builder.Add("DISTINCT ");

            WriteSelect(column);
            return Builder.Add(')');
        }

        public void BuildSelect(bool configureWhereParams)
        {
            ConfigureSelect(configureWhereParams, null, false);
        }


        private void WriteSelectWithOldPagination(bool configureWhereParams, Column countColunm, bool isCount)
        {
            Builder.Add("SELECT * FROM (");
            WriteRowNumber();

            if (countColunm == null) WriteSelectColumns();
            else WriteSelect(countColunm);

            Builder.Add(" FROM ").Add(GetTableName(true));

            if (Query.IsNoLock())
                Builder.Add(" WITH (NOLOCK)");

            ApplyJoins();
            WriteWhere(configureWhereParams);
            WriteGroupBy();
            Builder.Add(") ").Add(TryGetTableAlias(Query));

            if (!isCount)
                ApplyPagination();
        }


        internal void WriteSelectFrom(bool configureWhereParams)
        {
            Builder.Add(" FROM ").Add(GetTableName(true));

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
            Builder.Add(" OFFSET ").Add(Query.Offset).Add(" ROWS");

            if (Query.Limit >= 0)
                Builder.Add(" FETCH NEXT ").Add(Query.Limit).Add(" ROWS ONLY");
        }

        private void WriteOptions()
        {
            if (Query.IsNoLock())
                Builder.Add(" WITH (NOLOCK)");
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn, bool isCount)
        {
            if (HasOffset && UseOldPagination()) WriteSelectWithOldPagination(configureWhereParams, countColumn, isCount);
            else WriteSelect(configureWhereParams, isCount);
        }

        private void WriteSelect(bool configureWhereParams, bool isCount)
        {
            Builder.Add("SELECT");

            if (Query.Distinct)
                Builder.Add(" DISTINCT");

            if (!HasOffset && !isCount)
                AddLimit();

            Builder.Add(' ');
            WriteSelectColumns();
            WriteSelectFrom(configureWhereParams);

            if (isCount)
                return;

            ApplyOrderBy();
            WritePagination();
        }

        private void ApplyPagination()
        {
            Builder.Add(" WHERE [grammar_rownum] ");

            if (Query.Offset != null && Query.Limit != null)
                Builder.AddFormat("BETWEEN {0} AND {1}", Query.Offset + 1, Query.Offset + Query.Limit);
            else if (Query.Offset != null)
                Builder.Add("> ").Add(Query.Offset);
        }

        private void WriteRowNumber()
        {
            ValidateOffsetOrderBy();

            Builder.Add("SELECT ROW_NUMBER() OVER(ORDER BY ");
            ApplyOrderBy(Info.Orders, true);
            Builder.Add(") AS [grammar_rownum], ");
        }

        private bool UseOldPagination()
        {
            if (Config.UseOldPagination == null)
                return Query.Manager.GetDbVersion().Major < useNewPaginationAt.Major;

            return Config.UseOldPagination == true;
        }
    }
}
