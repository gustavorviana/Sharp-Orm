using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public class InsertGrammar : GrammarBase
    {
        protected InsertGrammar(GrammarBase owner, bool useLotQueryBuilder) : base(owner, useLotQueryBuilder)
        {
        }

        public InsertGrammar(GrammarBase owner) : base(owner)
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
