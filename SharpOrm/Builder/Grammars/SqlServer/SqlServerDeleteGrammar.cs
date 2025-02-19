using SharpOrm;
using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Linq;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerDeleteGrammar : SqlServerGrammarBase, IDeleteGrammar
    {
        public SqlServerDeleteGrammar(Query query) : base(query)
        {
        }

        public void Build()
        {
            ThrowOffsetNotSupported();
            Builder.Add("DELETE");
            AddLimit();

            if (Query.IsNoLock() || Query.Info.Joins.Any())
                Builder.Add(' ').Add(TryGetTableAlias(Query));

            Builder.Add(" FROM ").Add(GetTableName(true));

            if (Query.IsNoLock())
                Builder.Add(" WITH (NOLOCK)");

            ApplyJoins();
            WriteWhere(true);
        }

        public void BuildIncludingJoins(DbName[] joinNames)
        {
            throw new NotSupportedException(Messages.Grammar.SqlServer.DeleteIncludingJoinsNotSupported);
        }
    }
}
