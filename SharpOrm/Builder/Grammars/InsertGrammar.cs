using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars
{
    public class InsertGrammar : GrammarBase
    {
        internal protected InsertGrammar(Query query, QueryBuilder builder) : base(query, builder)
        {
        }

        public virtual void ConfigureInsertQuery(QueryBase query, string[] columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.builder.AddAndReplace(
                query.ToString(),
                '?',
                (count) => this.builder.AddParameter(query.Info.Where.Parameters[count - 1])
            );
        }

        public virtual void ConfigureInsertExpression(SqlExpression expression, string[] columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.builder.AddAndReplace(
                expression.ToString(),
                '?',
                (count) => this.builder.AddParameter(expression.Parameters[count - 1])
            );
        }

        public virtual void ConfigureBulkInsert(IEnumerable<Row> rows)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                this.ConfigureInsert(@enum.Current.Cells, false);

                while (@enum.MoveNext())
                {
                    this.builder.Add(", ");
                    this.AppendInsertCells(@enum.Current.Cells);
                }
            }
        }


        public virtual void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            AppendInsertHeader(cells.Select(c => c.Name).ToArray());
            builder.Add("VALUES ");
            AppendInsertCells(cells);

            if (getGeneratedId && Query.ReturnsInsetionId)
                builder.Add("; SELECT LAST_INSERT_ID();");
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
