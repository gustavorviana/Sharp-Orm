using SharpOrm.Builder.Grammars.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    internal class SqliteUpsertGrammar : GrammarBase, IUpsertGrammar
    {
        public SqliteUpsertGrammar(Query query) : base(query)
        {
        }

        public void Build(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            Builder.AddFormat("INSERT INTO {0} (", Info.Config.ApplyNomenclature(target.TableName.Name));
            WriteColumns(string.Empty, insertColumns);
            Builder.Add(')');

            Builder.Add(" SELECT ");
            WriteColumns(source.Alias + '.', insertColumns);

            Builder.AddFormat(" FROM {0}", source.GetFullName());
            Builder.Add(" WHERE true ON CONFLICT(");
            WriteColumns(string.Empty, whereColumns);
            Builder.Add(") SET ");

            WriteColumn(source.Alias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                Builder.Add(", ");
                WriteColumn(source.Alias, updateColumns[i]);
            }
        }

        public void Build(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            throw new NotSupportedException($"The \"Sqlite\" does not support bulk upsert rows.");
        }

        private void WriteColumn(string srcAlias, string column)
        {
            column = Info.Config.ApplyNomenclature(column);
            Builder.AddFormat("{1}={0}.{1}", srcAlias, column);
        }

        private void WriteColumns(string prefix, string[] columns)
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
    }
}
