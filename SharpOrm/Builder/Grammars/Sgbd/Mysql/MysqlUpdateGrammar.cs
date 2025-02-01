using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sgbd.Mysql
{
    internal class MysqlUpdateGrammar : MysqlGrammarBase
    {
        public MysqlUpdateGrammar(GrammarBase grammar) : base(grammar)
        {
        }

        public virtual void Build(IEnumerable<Cell> cells)
        {
            ThrowOffsetNotSupported();

            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                builder.Add("UPDATE ").Add(GetTableName(false));
                if (Info.Joins.Count > 0 && !string.IsNullOrEmpty(Info.TableName.Alias))
                    builder.Add(' ').Add(FixTableName(Info.TableName.Alias));

                ApplyJoins();

                builder.Add(" SET ");
                builder.AddJoin(WriteUpdateCell, ", ", en);

                WriteWhere(true);
            }

            AddLimit();
        }
    }
}
