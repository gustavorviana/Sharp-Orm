using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Interfaces
{
    public interface IBulkInsertGrammar : IGrammarBase
    {
        void Build(IEnumerable<Row> rows);
    }
}
