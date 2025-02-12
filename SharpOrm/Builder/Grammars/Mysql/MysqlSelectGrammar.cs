using SharpOrm.Builder.Grammars.Interfaces;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlSelectGrammar : MysqlGrammarBase, ISelectGrammar
    {
        public MysqlSelectGrammar(Query query) : base(query)
        {
        }

        public virtual void BuildSelect(bool configureWhereParams)
        {
            ConfigureSelect(configureWhereParams, null, false);
        }

        public virtual void BuildCount(Column column)
        {
            bool safeDistinct = (column == null || column.IsAll()) && Query.Distinct;

            if (safeDistinct)
                Builder.Add("SELECT COUNT(*) FROM (");

            ConfigureSelect(true, safeDistinct ? null : column, true);

            if (safeDistinct)
                Builder.Add(") ").Add(Info.Config.ApplyNomenclature("count"));
        }


        private void ConfigureSelect(bool configureWhereParams, Column countColumn, bool isCount)
        {
            bool _isCount = countColumn != null;
            bool isCustomCount = countColumn != null && countColumn.IsCount;

            Builder.Add("SELECT ");

            if (_isCount && !isCustomCount)
                Builder.Add("COUNT(");

            if (Query.Distinct && !isCustomCount)
                Builder.Add("DISTINCT ");

            if (_isCount)
            {
                WriteSelect(countColumn);
                if (!isCustomCount)
                    Builder.Add(')');
            }
            else
            {
                WriteSelectColumns();
            }

            Builder.Add(" FROM ").Add(GetTableName(true));

            ApplyJoins();
            WriteWhere(configureWhereParams);

            WriteGroupBy();

            if (isCount || _isCount)
                return;

            if (CanWriteOrderby())
                ApplyOrderBy();

            WritePagination();
        }

        private void WritePagination()
        {
            if (Query.Limit is null && Query.Offset is null)
                return;

            Builder.Add(" LIMIT ").Add(Query.Limit ?? int.MaxValue);

            if (Query.Offset != null)
                Builder.Add(" OFFSET ").Add(Query.Offset);
        }
    }
}
