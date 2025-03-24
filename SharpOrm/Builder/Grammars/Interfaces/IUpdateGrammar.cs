using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Interfaces
{
    public interface IUpdateGrammar : IGrammarBase
    {
        void Build(IEnumerable<Cell> cells);
    }
}
