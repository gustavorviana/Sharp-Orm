using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlMergeGrammar : MysqlGrammarBase, IMergeGrammar
    {
        public MysqlMergeGrammar(GrammarBase grammar) : base(grammar)
        {
        }

        public void Build(MergeQueryInfo target, MergeQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            WriteInsert(target, insertColumns);
            WriteSelect(source, insertColumns);
            WriteOnDuplicate(target, source, updateColumns);
            builder.Add(';');
        }

        private void WriteInsert(MergeQueryInfo target, string[] insertColumns)
        {
            builder.AddFormat("INSERT INTO {0} (", Info.Config.ApplyNomenclature(target.TableName.Name));
            WriteColumns("", insertColumns);
            builder.Add(')');
        }

        private void WriteSelect(MergeQueryInfo source, string[] insertColumns)
        {
            builder.Add(" SELECT ");
            WriteColumns(source.Alias + '.', insertColumns);
            builder.AddFormat(" FROM {0}", source.GetFullName());
        }

        private void WriteOnDuplicate(MergeQueryInfo target, MergeQueryInfo source, string[] updateColumns)
        {
            builder.Add(" ON DUPLICATE KEY UPDATE ");

            WriteColumn(source.Alias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                builder.Add(", ");
                WriteColumn(source.Alias, updateColumns[i]);
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
