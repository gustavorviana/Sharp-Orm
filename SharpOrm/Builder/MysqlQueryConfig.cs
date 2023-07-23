namespace SharpOrm.Builder
{
    public class MysqlQueryConfig : QueryConfig
    {
        public MysqlQueryConfig()
        {

        }

        public MysqlQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        public override Grammar NewGrammar(Query query)
        {
            return new MysqlGrammar(query);
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('`', '`');
        }
    }
}
