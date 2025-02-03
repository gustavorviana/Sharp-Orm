using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerMergeGrammar : SqlServerGrammarBase
    {
        public SqlServerMergeGrammar(GrammarBase grammar) : base(grammar)
        {
        }

        public void Build(MergeQueryInfo target, MergeQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            WriteTargetHeader(target);
            WriteSourceHeader(source);

            WriteWhereColumns(target, source, whereColumns);
            WriteMatchedColumns(target, source, updateColumns);
            WriteNotMatchedColumns(source, insertColumns);
        }

        private void WriteTargetHeader(MergeQueryInfo target)
        {
            builder.AddFormat("MERGE INTO {0}", target.GetFullName());
            WriteGrammarOptions(Query);
        }

        private void WriteSourceHeader(MergeQueryInfo source)
        {
            builder.AddFormat(" USING {0}", source.GetFullName());
        }

        private void WriteWhereColumns(MergeQueryInfo target, MergeQueryInfo source, string[] whereColumns)
        {
            builder.AddFormat(" ON ");

            WriteColumn(source.Alias, target.Alias, whereColumns[0]);

            for (int i = 1; i < whereColumns.Length; i++)
            {
                builder.Add(" AND ");
                WriteColumn(source.Alias, target.Alias, whereColumns[i]);
            }
        }

        private void WriteMatchedColumns(MergeQueryInfo target, MergeQueryInfo source, string[] updateColumns)
        {
            builder
                .Add(" WHEN MATCHED THEN UPDATE SET");

            WriteColumn(target.Alias, source.Alias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                builder.Add(", ");
                WriteColumn(target.Alias, source.Alias, updateColumns[i]);
            }
        }

        private void WriteNotMatchedColumns(MergeQueryInfo source, string[] insertColumns)
        {
            builder.Add(" WHEN NOT MATCHED THEN INSERT (");
            WriteInsertColumns("", insertColumns);
            builder.Add(") VALUES (");
            WriteInsertColumns($"{source.Alias}.", insertColumns);
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
