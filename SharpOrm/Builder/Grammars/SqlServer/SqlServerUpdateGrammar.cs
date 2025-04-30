using SharpOrm;
using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerUpdateGrammar : SqlServerGrammarBase, IUpdateGrammar
    {
        public SqlServerUpdateGrammar(Query query) : base(query)
        {
        }

        public void Build(IEnumerable<Cell> cells)
        {
            ThrowOffsetNotSupported();

            var needAlias = !string.IsNullOrEmpty(Info.TableName.Alias) || Query.IsNoLock();
            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                Builder.Add("UPDATE ").Add(FixTableName(needAlias ? Info.TableName.TryGetAlias() : Info.TableName.Name));
                AddLimit();
                Builder.Add(" SET ");
                Builder.AddJoin(WriteUpdateCell, ", ", en);
            }

            if (needAlias || Info.Joins.Any())
                Builder.Add(" FROM ").Add(GetTableName(true));

            if (Query.IsNoLock())
                Builder.Add(" WITH (NOLOCK)");

            ApplyJoins();
            WriteWhere(true);
        }
    }
}