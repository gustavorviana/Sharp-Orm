namespace SharpOrm.Builder
{
    public class DefaultQueryConfig : IQueryConfig
    {
        public bool OnlySafeModifications { get; }

        public DefaultQueryConfig()
        {

        }

        public DefaultQueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
        }

        public Grammar NewGrammar(Query query)
        {
            return new MysqlGrammar(query);
        }
    }
}
