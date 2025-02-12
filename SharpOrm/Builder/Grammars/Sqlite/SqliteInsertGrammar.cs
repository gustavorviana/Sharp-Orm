using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    internal class SqliteInsertGrammar : InsertGrammar
    {
        public SqliteInsertGrammar(Query query) : base(query)
        {
        }

        public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            SqliteGrammar.ThrowNotSupportedOperations(Query, "INSERT");
            Build(cells);

            if (getGeneratedId && Query.ReturnsInsetionId)
                Builder.Add("; SELECT last_insert_rowid();");
        }
    }
}
