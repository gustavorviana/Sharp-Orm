using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sgbd.SqlServer
{
    internal class SqlServerUpdateGrammar : SqlServerGrammarBase
    {
        public SqlServerUpdateGrammar(GrammarBase owner) : base(owner)
        {
        }

        public void Build(IEnumerable<Cell> cells)
        {
            ThrowOffsetNotSupported();
            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                builder.Add("UPDATE ").Add(Info.Joins.Any() ? FixTableName(Info.TableName.ToString()) : GetTableName(false));
                AddLimit();
                builder.Add(" SET ");
                builder.AddJoin(WriteUpdateCell, ", ", en);
            }

            if (Info.Joins.Any() || Query.IsNoLock())
                builder.Add(" FROM ").Add(GetTableName(true));

            if (Query.IsNoLock())
                builder.Add(" WITH (NOLOCK)");

            ApplyJoins();
            WriteWhere(true);
        }
    }
}