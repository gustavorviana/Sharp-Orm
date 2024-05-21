using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    public class SqliteQueryConfig : MysqlQueryConfig
    {
        public SqliteQueryConfig()
        {

        }

        public SqliteQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('"', '"');
        }

        public override Grammar NewGrammar(Query query)
        {
            return new SqliteGrammar(query);
        }

        public override TableGrammar NewTableGrammar(TableSchema schema)
        {
            throw new NotSupportedException($"{this.GetType().FullName} does not support creating/editing/removing tables.");
        }
    }
}
