using SharpOrm.Msg;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlUpsertGrammar : InsertGrammar, IUpsertGrammar
    {
        public MysqlUpsertGrammar(GrammarBase grammar) : base(grammar)
        {
        }

        public void Build(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            WriteInsert(target, insertColumns);
            WriteSelect(source, insertColumns);
            WriteOnDuplicate(target, source.Alias, updateColumns);
            builder.Add(';');
        }

        public void Build(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                string[] insertColumns = @enum.Current.ColumnNames;
                string sourceAlias = Info.Config.ApplyNomenclature("Source");

                WriteInsert(target, insertColumns);
                WriteRowSourceHeader(@enum, sourceAlias);
                WriteOnDuplicate(target, sourceAlias, updateColumns);
                builder.Add(';');
            }
        }

        private void WriteInsert(UpsertQueryInfo target, string[] insertColumns)
        {
            builder.AddFormat("INSERT INTO {0} (", Info.Config.ApplyNomenclature(target.TableName.Name));
            WriteColumns("", insertColumns);
            builder.Add(')');
        }

        private void WriteRowSourceHeader(IEnumerator<Row> rows, string alias)
        {
            string[] columnNames = rows.Current.ColumnNames;
            builder.Add(" VALUES ");

            AppendInsertCells(rows.Current.Cells);

            while (rows.MoveNext())
            {
                builder.Add(", ");
                AppendInsertCells(rows.Current.Cells);
            }

            builder.AddFormat(" AS {0}", alias);
        }

        private void WriteSelect(UpsertQueryInfo source, string[] insertColumns)
        {
            builder.Add(" SELECT ");
            WriteColumns(source.Alias + '.', insertColumns);
            builder.AddFormat(" FROM {0}", source.GetFullName());
        }

        private void WriteOnDuplicate(UpsertQueryInfo target, string sourceAlias, string[] updateColumns)
        {
            builder.Add(" ON DUPLICATE KEY UPDATE ");

            WriteColumn(sourceAlias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                builder.Add(", ");
                WriteColumn(sourceAlias, updateColumns[i]);
            }
        }

        private void WriteColumns(string prefix, string[] columns)
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

        private void WriteColumn(string srcAlias, string column)
        {
            column = Info.Config.ApplyNomenclature(column);
            builder.AddFormat("{1}={0}.{1}", srcAlias, column);
        }
    }
}
