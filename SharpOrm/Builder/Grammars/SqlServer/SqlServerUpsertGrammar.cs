using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerUpsertGrammar : InsertGrammar, IUpsertGrammar
    {
        public SqlServerUpsertGrammar(GrammarBase grammar) : base(grammar)
        {
        }

        public void Build(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            WriteTargetHeader(target);
            WriteSourceHeader(source);

            WriteWhereColumns(target, source.Alias, whereColumns);
            WriteMatchedColumns(target, source.Alias, updateColumns);
            WriteNotMatchedColumns(source.Alias, insertColumns);
        }

        public void Build(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                string[] insertColumns = @enum.Current.ColumnNames;
                string sourceAlias = Info.Config.ApplyNomenclature("Source");

                WriteTargetHeader(target);
                WriteRows(@enum, insertColumns, sourceAlias);

                WriteWhereColumns(target, sourceAlias, whereColumns);
                WriteMatchedColumns(target, sourceAlias, updateColumns);
                WriteNotMatchedColumns(sourceAlias, insertColumns);
            }
        }

        private void WriteTargetHeader(UpsertQueryInfo target)
        {
            builder.AddFormat("MERGE INTO {0}", target.GetFullName());
            SqlServerGrammarOptions.WriteTo(builder, Query);
        }

        private void WriteRows(IEnumerator<Row> rows, string[] insertColumns, string sourceAlias)
        {
            builder.Add(" USING(VALUES ");
            AppendInsertCells(rows.Current);

            while (rows.MoveNext())
            {
                builder.Add(", ");

                AppendInsertCells(rows.Current);
            }
            builder.AddFormat(") AS {0} (", sourceAlias);

            builder.Add(Info.Config.ApplyNomenclature(insertColumns[0]));

            for (int i = 1; i < insertColumns.Length; i++)
            {
                builder.Add(", ").Add(Info.Config.ApplyNomenclature(insertColumns[i]));
            }

            builder.Add(')');
        }

        private void WriteSourceHeader(UpsertQueryInfo source)
        {
            builder.AddFormat(" USING {0}", source.GetFullName());
        }

        private void WriteWhereColumns(UpsertQueryInfo target, string sourceAlias, string[] whereColumns)
        {
            builder.AddFormat(" ON ");

            WriteColumn(sourceAlias, target.Alias, whereColumns[0]);

            for (int i = 1; i < whereColumns.Length; i++)
            {
                builder.Add(" AND ");
                WriteColumn(sourceAlias, target.Alias, whereColumns[i]);
            }
        }

        private void WriteMatchedColumns(UpsertQueryInfo target, string sourceAlias, string[] updateColumns)
        {
            builder
                .Add(" WHEN MATCHED THEN UPDATE SET ");

            WriteColumn(target.Alias, sourceAlias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                builder.Add(", ");
                WriteColumn(target.Alias, sourceAlias, updateColumns[i]);
            }
        }

        private void WriteNotMatchedColumns(string sourceAlias, string[] insertColumns)
        {
            builder.Add(" WHEN NOT MATCHED THEN INSERT (");
            WriteInsertColumns("", insertColumns);
            builder.Add(") VALUES (");
            WriteInsertColumns($"{sourceAlias}.", insertColumns);
            builder.Add(");");
        }

        private void WriteInsertColumns(string prefix, string[] columns)
        {
            var column = Info.Config.ApplyNomenclature(columns[0]);
            builder.AddFormat("{0}{1}", prefix, column);

            for (int i = 1; i < columns.Length; i++)
            {
                builder.Add(", ");
                column = Info.Config.ApplyNomenclature(columns[i]);

                builder.AddFormat("{0}{1}", prefix, column);
            }
        }

        private void WriteColumn(string alias1, string alias2, string column)
        {
            column = Info.Config.ApplyNomenclature(column);
            builder.AddFormat("{0}.{1}={2}.{1}", alias1, column, alias2);
        }
    }
}
