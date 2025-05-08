using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public abstract class InsertBaseGrammar : GrammarBase
    {
        protected InsertBaseGrammar(Query query, bool useLotQueryBuilder) : base(query, useLotQueryBuilder)
        {
        }

        public InsertBaseGrammar(Query query) : base(query)
        {
        }

        protected void Build(IEnumerable<Cell> cells)
        {
            AppendInsertHeader(cells.Select(c => c.Name).ToArray());
            Builder.Add("VALUES ");
            AppendInsertCells(cells);
        }

        protected void Build(Row row)
        {
            AppendInsertHeader(row.ColumnNames);
            Builder.Add("VALUES ");
            AppendInsertCells(row.Cells);
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

        public virtual void Build(QueryBase query, string[] columnNames)
        {
            AppendInsertHeader(columnNames);
            Builder.AddAndReplace(
                query.ToString(),
                '?',
                (count) => Builder.AddParameter(query.Info.Where.Parameters[count - 1])
            );
        }
    }
}
