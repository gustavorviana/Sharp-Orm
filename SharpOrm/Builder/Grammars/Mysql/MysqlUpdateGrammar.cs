using SharpOrm.Msg;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Mysql
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

                Builder.Add("UPDATE ").Add(GetTableName(false));
                if (Info.Joins.Count > 0 && !string.IsNullOrEmpty(Info.TableName.Alias))
                    Builder.Add(' ').Add(FixTableName(Info.TableName.Alias));

                ApplyJoins();

                Builder.Add(" SET ");
                Builder.AddJoin(WriteUpdateCell, ", ", en);

                WriteWhere(true);
            }

            AddLimit();
        }
    }
}
