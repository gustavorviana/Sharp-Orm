using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlInsertGrammar : InsertGrammar
    {
        public MysqlInsertGrammar(Query query) : base(query)
        {
        }

        public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            Build(cells);
            if (getGeneratedId && Query.ReturnsInsetionId)
                Builder.Add("; SELECT LAST_INSERT_ID();");
        }
    }
}
