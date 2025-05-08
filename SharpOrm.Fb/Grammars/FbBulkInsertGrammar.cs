using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;

namespace SharpOrm.Fb.Grammars
{
    internal class FbBulkInsertGrammar : InsertBaseGrammar, IBulkInsertGrammar
    {
        private const int MAX_LINES = 10_000;

        public FbBulkInsertGrammar(Query query) : base(query, true)
        {
        }

        public virtual void Build(IEnumerable<Row> rows)
        {
            var batchBuilder = (BatchQueryBuilder)Builder;

            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                Build(@enum.Current);

                while (InternalBulkInsert(@enum))
                {
                    batchBuilder.Remove(0, 2);
                    batchBuilder.SetCursor(0, 0);

                    AppendInsertHeader(@enum.Current.ColumnNames);
                    Builder.Add("VALUES ");

                    batchBuilder.RestoreCursor();
                }
            }
        }

        private bool InternalBulkInsert(IEnumerator<Row> @enum)
        {
            int addedLines = 0;
            while (@enum.MoveNext())
            {
                ((BatchQueryBuilder)Builder).CreateSavePoint();

                Builder.Add(", ");
                AppendInsertCells(@enum.Current.Cells);

                addedLines++;
                if (Builder.Parameters.Count > Query.Config.DbParamsLimit || addedLines >= MAX_LINES)
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