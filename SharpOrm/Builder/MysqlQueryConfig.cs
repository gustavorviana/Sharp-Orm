namespace SharpOrm.Builder
{
    public class MysqlQueryConfig : IQueryConfig
    {
        public bool OnlySafeModifications { get; }

        public int CommandTimeout { get; set; } = 30;

        public MysqlQueryConfig()
        {

        }

        public MysqlQueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
        }

        public Grammar NewGrammar(Query query)
        {
            return new MysqlGrammar(query);
        }

        public string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('`', '`');
        }
    }
}
