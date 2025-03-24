using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerInsertGrammar : InsertGrammar
    {
        public SqlServerInsertGrammar(Query query) : base(query)
        {
        }

        public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            Build(cells);

            if (getGeneratedId && Query.ReturnsInsetionId)
                Builder.Add("; SELECT SCOPE_IDENTITY();");
        }
    }
}
