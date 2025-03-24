using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Interfaces
{
    public interface IUpsertGrammar : IGrammarBase
    {
        void Build(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns);
        void Build(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns);
    }
}
