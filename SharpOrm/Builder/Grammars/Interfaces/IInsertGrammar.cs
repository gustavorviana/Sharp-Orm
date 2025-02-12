using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Interfaces
{
    public interface IInsertGrammar : IGrammarBase
    {
        void Build(IEnumerable<Cell> cells, bool returnsInsetionId);
        void Build(QueryBase query, string[] columnNames);
        void Build(SqlExpression expression, string[] columnNames);
    }
}
