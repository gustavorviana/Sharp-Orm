namespace SharpOrm.Builder
{
    public interface IQueryConfig
    {
        Grammar NewGrammar(Query query);
    }
}
