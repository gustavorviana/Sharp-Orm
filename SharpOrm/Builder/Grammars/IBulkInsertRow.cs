using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars
{
    public interface IBulkInsertRow : IGrammar
    {
        void BuildBulkInsert(IEnumerable<Row> rows);
    }
}
