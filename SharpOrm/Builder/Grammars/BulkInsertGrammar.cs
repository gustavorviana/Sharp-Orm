using SharpOrm.Builder.Grammars.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public class BulkInsertGrammar : InsertBaseGrammar, IBulkInsertGrammar
    {
        private const int LinesLimit = 1000;

        public BulkInsertGrammar(Query query) : base(query, true)
        {
        }

        public virtual void Build(IEnumerable<Row> rows)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    return;

                Build(@enum.Current.Cells);

                while (InternalBulkInsert(@enum))
                {
                    ((BatchQueryBuilder)Builder).Remove(0, 2);
                    ((BatchQueryBuilder)Builder).SetCursor(0, 0);

                    AppendInsertHeader(@enum.Current.Cells.Select(c => c.Name).ToArray());
                    Builder.Add("VALUES ");

                    ((BatchQueryBuilder)Builder).RestoreCursor();
                }
            }
        }

        private bool InternalBulkInsert(IEnumerator<Row> @enum)
        {
            int qtdLinex = 0;
            while (@enum.MoveNext())
            {
                qtdLinex++;
                ((BatchQueryBuilder)Builder).CreateSavePoint();

                Builder.Add(", ");
                AppendInsertCells(@enum.Current.Cells);

                if (Builder.Parameters.Count > Query.Config.DbParamsLimit || qtdLinex >= LinesLimit)
                {
                    ((BatchQueryBuilder)Builder).BuildSavePoint();
                    return true;
                }

                ((BatchQueryBuilder)Builder).ResetSavePoint();
            }

            return false;
        }
    }
}
