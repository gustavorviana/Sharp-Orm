namespace SharpOrm.Builder
{
    public class DefaultQueryConfig : IQueryConfig
    {
        public bool OnlySafeModifications { get; }
        public string ColumnPrefix { get; set; }
        public string ColumnSuffix { get; set; }

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
