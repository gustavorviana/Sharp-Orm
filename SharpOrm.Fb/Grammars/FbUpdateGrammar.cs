using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;

namespace SharpOrm.Fb.Grammars
{
    internal class FbUpdateGrammar : FbGrammarBase, IUpdateGrammar
    {
        public FbUpdateGrammar(Query query) : base(query)
        {
        }

        public void Build(IEnumerable<Cell> cells)
        {
            ThrowOffsetNotSupported();
            ThrowJoinNotSupported();

            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                Builder.Add("UPDATE ").Add(GetTableName(false));
                Builder.Add(" SET ");
                Builder.AddJoin(WriteUpdateCell, ", ", en);
            }

            WriteWhere(true);
            ApplyOrderBy();
            ApplyRowsTo();
        }
    }
}