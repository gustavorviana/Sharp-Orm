using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sgbd.Mysql
{
    internal class MysqlSelectGrammar : MysqlGrammarBase
    {
        public MysqlSelectGrammar(GrammarBase grammar) : base(grammar)
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
                builder.Add("SELECT COUNT(*) FROM (");

            ConfigureSelect(true, safeDistinct ? null : column, true);

            if (safeDistinct)
                builder.Add(") ").Add(Info.Config.ApplyNomenclature("count"));
        }


        private void ConfigureSelect(bool configureWhereParams, Column countColumn, bool isCount)
        {
            bool _isCount = countColumn != null;
            bool isCustomCount = countColumn != null && countColumn.IsCount;

            builder.Add("SELECT ");

            if (_isCount && !isCustomCount)
                builder.Add("COUNT(");

            if (Query.Distinct && !isCustomCount)
                builder.Add("DISTINCT ");

            if (_isCount)
            {
                WriteSelect(countColumn);
                if (!isCustomCount)
                    builder.Add(')');
            }
            else
            {
                WriteSelectColumns();
            }

            builder.Add(" FROM ").Add(GetTableName(true));

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

            builder.Add(" LIMIT ").Add(Query.Limit ?? int.MaxValue);

            if (Query.Offset != null)
                builder.Add(" OFFSET ").Add(Query.Offset);
        }
    }
}
