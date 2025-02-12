using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars
{
    public class InsertLotGrammar : InsertGrammar, IBulkInsertRow
    {
        public InsertLotGrammar(GrammarBase owner) : base(owner, true)
        {
        }

        public virtual void BuildBulkInsert(IEnumerable<Row> rows)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                BuildInsert(@enum.Current.Cells);

                while (InternalBulkInsert(@enum))
                {
                    ((LotQueryBuilder)Builder).Remove(0, 2);
                    ((LotQueryBuilder)Builder).SetCursor(0, 0);

                    AppendInsertHeader(@enum.Current.Cells.Select(c => c.Name).ToArray());
                    Builder.Add("VALUES ");

                    ((LotQueryBuilder)Builder).RestoreCursor();
                }
            }
        }

        private bool InternalBulkInsert(IEnumerator<Row> @enum)
        {
            while (@enum.MoveNext())
            {
                ((LotQueryBuilder)Builder).CreateSavePoint();

                Builder.Add(", ");
                AppendInsertCells(@enum.Current.Cells);

                if (Builder.Parameters.Count > Query.Config.InsertLimitParams)
                {
                    ((LotQueryBuilder)Builder).BuildSavePoint();
                    return true;
                }

                ((LotQueryBuilder)Builder).ResetSavePoint();
            }

            return false;
        }
    }
}
