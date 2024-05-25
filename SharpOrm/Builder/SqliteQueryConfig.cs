using System;
using System.Text;

namespace SharpOrm.Builder
{
    public class SqliteQueryConfig : QueryConfig
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

        public override string EscapeString(string value) => MysqlQueryConfig.Escape(value);

        public override QueryConfig Clone()
        {
            var clone = new SqliteQueryConfig(this.OnlySafeModifications);
            this.CopyTo(clone);
            return clone;
        }
    }
}
