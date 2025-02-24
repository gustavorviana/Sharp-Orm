using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerUpsertGrammar : InsertBaseGrammar, IUpsertGrammar
    {
        public SqlServerUpsertGrammar(Query query) : base(query)
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
            Builder.AddFormat("MERGE INTO {0}", target.GetFullName());
            SqlServerGrammarOptions.WriteTo(Builder, Query);
        }

        private void WriteRows(IEnumerator<Row> rows, string[] insertColumns, string sourceAlias)
        {
            Builder.Add(" USING(VALUES ");
            AppendInsertCells(rows.Current);

            while (rows.MoveNext())
            {
                Builder.Add(", ");

                AppendInsertCells(rows.Current);
            }
            Builder.AddFormat(") AS {0} (", sourceAlias);

            Builder.Add(Info.Config.ApplyNomenclature(insertColumns[0]));

            for (int i = 1; i < insertColumns.Length; i++)
            {
                Builder.Add(", ").Add(Info.Config.ApplyNomenclature(insertColumns[i]));
            }

            Builder.Add(')');
        }

        private void WriteSourceHeader(UpsertQueryInfo source)
        {
            Builder.AddFormat(" USING {0}", source.GetFullName());
        }

        private void WriteWhereColumns(UpsertQueryInfo target, string sourceAlias, string[] whereColumns)
        {
            Builder.AddFormat(" ON ");

            WriteColumn(sourceAlias, target.Alias, whereColumns[0]);

            for (int i = 1; i < whereColumns.Length; i++)
            {
                Builder.Add(" AND ");
                WriteColumn(sourceAlias, target.Alias, whereColumns[i]);
            }
        }

        private void WriteMatchedColumns(UpsertQueryInfo target, string sourceAlias, string[] updateColumns)
        {
            Builder
                .Add(" WHEN MATCHED THEN UPDATE SET ");

            WriteColumn(target.Alias, sourceAlias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                Builder.Add(", ");
                WriteColumn(target.Alias, sourceAlias, updateColumns[i]);
            }
        }

        private void WriteNotMatchedColumns(string sourceAlias, string[] insertColumns)
        {
            Builder.Add(" WHEN NOT MATCHED THEN INSERT (");
            WriteInsertColumns(string.Empty, insertColumns);
            Builder.Add(") VALUES (");
            WriteInsertColumns($"{sourceAlias}.", insertColumns);
            Builder.Add(");");
        }

        private void WriteInsertColumns(string prefix, string[] columns)
        {
            var column = Info.Config.ApplyNomenclature(columns[0]);
            Builder.AddFormat("{0}{1}", prefix, column);

            for (int i = 1; i < columns.Length; i++)
            {
                Builder.Add(", ");
                column = Info.Config.ApplyNomenclature(columns[i]);

                Builder.AddFormat("{0}{1}", prefix, column);
            }
        }

        private void WriteColumn(string alias1, string alias2, string column)
        {
            column = Info.Config.ApplyNomenclature(column);
            Builder.AddFormat("{0}.{1}={2}.{1}", alias1, column, alias2);
        }
    }
}
