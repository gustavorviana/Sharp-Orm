using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public class InsertGrammar : GrammarBase
    {
        public InsertGrammar(GrammarBase owner) : base(owner, (query) => new LotQueryBuilder(query))
        {
        }

        public virtual void BuildInsertQuery(QueryBase query, string[] columnNames)
        {
            AppendInsertHeader(columnNames);
            builder.AddAndReplace(
                query.ToString(),
                '?',
                (count) => builder.AddParameter(query.Info.Where.Parameters[count - 1])
            );
        }

        public virtual void BuildInsertExpression(SqlExpression expression, string[] columnNames)
        {
            AppendInsertHeader(columnNames);
            builder.AddAndReplace(
                expression.ToString(),
                '?',
                (count) => builder.AddParameter(expression.Parameters[count - 1])
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
                    ((LotQueryBuilder)builder).Remove(0, 2);
                    ((LotQueryBuilder)builder).SetCursor(0, 0);

                    AppendInsertHeader(@enum.Current.Cells.Select(c => c.Name).ToArray());
                    builder.Add("VALUES ");

                    ((LotQueryBuilder)builder).RestoreCursor();
                }
            }
        }

        private bool InternalBulkInsert(IEnumerator<Row> @enum)
        {
            while (@enum.MoveNext())
            {
                ((LotQueryBuilder)builder).CreateSavePoint();

                builder.Add(", ");
                AppendInsertCells(@enum.Current.Cells);

                if (builder.Parameters.Count > Query.Config.InsertLimitParams)
                {
                    ((LotQueryBuilder)builder).BuildSavePoint();
                    return true;
                }

                ((LotQueryBuilder)builder).ResetSavePoint();
            }

            return false;
        }

        public virtual void BuildInsert(IEnumerable<Cell> cells)
        {
            AppendInsertHeader(cells.Select(c => c.Name).ToArray());
            builder.Add("VALUES ");
            AppendInsertCells(cells);
        }

        protected void AppendInsertHeader(string[] columns)
        {
            columns = columns.Select(Info.Config.ApplyNomenclature).ToArray();
            builder.Add("INSERT INTO ").Add(GetTableName(false));

            if (columns.Length > 0) builder.Add(" (").AddJoin(", ", columns).Add(") ");
            else builder.Add(' ');
        }

        protected void AppendInsertCells(IEnumerable<Cell> cells)
        {
            builder.Add('(');
            AppendCells(cells);
            builder.Add(")");
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
