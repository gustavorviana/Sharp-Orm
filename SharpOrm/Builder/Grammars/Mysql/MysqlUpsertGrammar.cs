using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlUpsertGrammar : InsertGrammar, IUpsertGrammar
    {
        public MysqlUpsertGrammar(Query query) : base(query)
        {
        }

        public void Build(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            WriteInsert(target, insertColumns);
            WriteSelect(source, insertColumns);
            WriteOnDuplicate(target, source.Alias, updateColumns);
            Builder.Add(';');
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
                Builder.Add(';');
            }
        }

        private void WriteInsert(UpsertQueryInfo target, string[] insertColumns)
        {
            Builder.AddFormat("INSERT INTO {0} (", Info.Config.ApplyNomenclature(target.TableName.Name));
            WriteColumns(string.Empty, insertColumns);
            Builder.Add(')');
        }

        private void WriteRowSourceHeader(IEnumerator<Row> rows, string alias)
        {
            string[] columnNames = rows.Current.ColumnNames;
            Builder.Add(" VALUES ");

            AppendInsertCells(rows.Current.Cells);

            while (rows.MoveNext())
            {
                Builder.Add(", ");
                AppendInsertCells(rows.Current.Cells);
            }

            Builder.AddFormat(" AS {0}", alias);
        }

        private void WriteSelect(UpsertQueryInfo source, string[] insertColumns)
        {
            Builder.Add(" SELECT ");
            WriteColumns(source.Alias + '.', insertColumns);
            Builder.AddFormat(" FROM {0}", source.GetFullName());
        }

        private void WriteOnDuplicate(UpsertQueryInfo target, string sourceAlias, string[] updateColumns)
        {
            Builder.Add(" ON DUPLICATE KEY UPDATE ");

            WriteColumn(sourceAlias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                Builder.Add(", ");
                WriteColumn(sourceAlias, updateColumns[i]);
            }
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

        private void WriteColumn(string srcAlias, string column)
        {
            column = Info.Config.ApplyNomenclature(column);
            Builder.AddFormat("{1}={0}.{1}", srcAlias, column);
        }

        public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            throw new NotImplementedException();
        }
    }
}
