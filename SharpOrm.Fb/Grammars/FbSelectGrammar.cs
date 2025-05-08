using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;

namespace SharpOrm.Fb.Grammars
{
    internal class FbSelectGrammar : FbGrammarBase, ISelectGrammar
    {
        public FbSelectGrammar(Query query) : base(query)
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
                if (!isCount)
                {
                    if (Query.Limit != null)
                        Builder.Add("FIRST ").Add(Query.Limit).Add(" ");

                    if (Query.Offset != null)
                        Builder.Add("SKIP ").Add(Query.Offset).Add(" ");
                }

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
        }
    }
}
