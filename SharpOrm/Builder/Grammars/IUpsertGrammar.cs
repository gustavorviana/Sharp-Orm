using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars
{
    public interface IUpsertGrammar
    {
        void Build(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns);
        void Build(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns);
    }
}
