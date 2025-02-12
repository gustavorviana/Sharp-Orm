﻿using SharpOrm;
using System;
using System.Linq;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerDeleteGrammar : SqlServerGrammarBase
    {
        public SqlServerDeleteGrammar(GrammarBase owner) : base(owner)
        {
        }

        public void Build()
        {
            ThrowDeleteJoinsNotSupported();
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

        protected void ThrowDeleteJoinsNotSupported()
        {
            if (IsMultipleTablesDeleteWithJoin())
                throw new NotSupportedException("Delete operations on multiple tables with JOINs are not supported in SQL Server. Please execute separate DELETE statements for each table.");
        }
    }
}
