using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public class InsertGrammar : GrammarBase
    {
        public InsertGrammar(GrammarBase owner) : base(owner, true)
        {
        }

        public virtual void BuildInsertQuery(QueryBase query, string[] columnNames)
        {
            AppendInsertHeader(columnNames);
            Builder.AddAndReplace(
                query.ToString(),
                '?',
                (count) => Builder.AddParameter(query.Info.Where.Parameters[count - 1])
            );
        }

        public virtual void BuildInsertExpression(SqlExpression expression, string[] columnNames)
        {
            AppendInsertHeader(columnNames);
            Builder.AddAndReplace(
                expression.ToString(),
                '?',
                (count) => Builder.AddParameter(expression.Parameters[count - 1])
            );
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

        public virtual void BuildInsert(IEnumerable<Cell> cells)
        {
            AppendInsertHeader(cells.Select(c => c.Name).ToArray());
            Builder.Add("VALUES ");
            AppendInsertCells(cells);
        }

        protected void AppendInsertHeader(string[] columns)
        {
            columns = columns.Select(Info.Config.ApplyNomenclature).ToArray();
            Builder.Add("INSERT INTO ").Add(GetTableName(false));

            if (columns.Length > 0) Builder.Add(" (").AddJoin(", ", columns).Add(") ");
            else Builder.Add(' ');
        }

        protected void AppendInsertCells(IEnumerable<Cell> cells)
        {
            Builder.Add('(');
            AppendCells(cells);
            Builder.Add(")");
        }

        /// <summary>
        /// Appends the cells to the query.
        /// </summary>
        /// <param name="values">The cells.</param>
        protected void AppendCells(IEnumerable<Cell> values)
        {
            AddParams(values, cell => cell.Value);
        }
    }
}
