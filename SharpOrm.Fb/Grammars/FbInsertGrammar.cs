using SharpOrm.Builder.Grammars;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Fb.Grammars
{
    internal class FbInsertGrammar : InsertGrammar
    {
        public FbInsertGrammar(Query query) : base(query)
        {
        }

        public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            Build(cells);

            if (!getGeneratedId || !Query.ReturnsInsetionId)
                return;

            var pkColumn = GetPrimaryKeys().FirstOrDefault();

            Builder
                .Add(" RETURNING ")
                .Add(pkColumn == null ? "1" : Info.Config.ApplyNomenclature(pkColumn.Name));
        }
    }
}
