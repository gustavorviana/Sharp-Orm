namespace SharpOrm.Builder
{
    public class DefaultQueryConfig : IQueryConfig
    {
        public Grammar NewGrammar(Query query)
        {
            return new MysqlGrammar(query);
        }
    }
}
