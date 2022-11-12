namespace SharpOrm.Builder
{
    public class MysqlQueryConfig : IQueryConfig
    {
        public bool OnlySafeModifications { get; }

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
            return $"`{string.Join("`.`", name.AlphaNumericOnly('_', '.').Split('.'))}`";
        }
    }
}
