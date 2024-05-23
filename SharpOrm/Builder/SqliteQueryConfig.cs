using System;

namespace SharpOrm.Builder
{
    public class SqliteQueryConfig : MysqlQueryConfig
    {
        public override bool CanUpdateJoin { get; } = false;

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
            return new SqliteTableGrammar(this, schema);
        }
    }
}
