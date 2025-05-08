using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using SharpOrm.Msg;
using System;

namespace SharpOrm.Fb.Grammars
{
    internal class FbGrammarBase : GrammarBase
    {
        public FbGrammarBase(Query query) : base(query)
        {
        }

        protected void AddLimit()
        {
            if (Query.Limit != null)
                Builder.Add(" FIRST ").Add(Query.Limit);

            if (Query.Offset != null)
                Builder.Add(" SKIP ").Add(Query.Offset);
        }

        protected void ApplyRowsTo()
        {
            if (Query.Offset != null && Query.Limit == null)
                throw new NotSupportedException(FbMessages.Grammar.OffsetRequiresLimit);

            if (Query.Limit != null)
                Builder.Add(" ROWS ").Add(Query.Limit);

            if (Query.Offset != null)
                Builder.Add(" TO ").Add(Query.Offset);
        }

        protected override void WriteTable(QueryBase query)
        {
            base.WriteTable(query);
        }
    }
}