using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Linq;

namespace SharpOrm.Fb.Grammars
{
    internal class FbDeleteGrammar : FbGrammarBase, IDeleteGrammar
    {
        public FbDeleteGrammar(Query query) : base(query)
        {
        }

        public void Build()
        {
            ThrowOffsetNotSupported();
            ThrowJoinNotSupported();

            Builder.Add("DELETE FROM ").Add(GetTableName(true));

            WriteWhere(true);

            if (Info.Orders.Length > 0)
                ApplyOrderBy();

            ApplyRowsTo();
        }

        public void BuildIncludingJoins(DbName[] joinNames)
        {
            throw new NotSupportedException(FbMessages.Grammar.DeleteIncludingJoinsNotSupported);
        }
    }
}